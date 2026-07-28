using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Two-layer frame cache.
//
//   tile layer  — snapshotted after the tile loop, before the first overlay paints.
//   frame layer — the finished frame, overlays included.
//
// A fully idle frame blits the frame layer and paints nothing. Most non-idle frames change
// only overlays: any live effect keeps the frame dirty every frame while the tiles under it
// stay static. Those frames blit the tile layer instead — which also erases the previous
// frame's overlays — and run the overlay passes over it, skipping the tile loop entirely.
// The tile loop is an order of magnitude more expensive than a blit.
//
// PATH-D-PORT: the erase-and-replace only works because the tile loop paints every cell of
// the viewport, so replaying its snapshot wipes whatever the overlays drew last frame. A
// renderer whose tile pass can leave cells untouched needs an explicit overlay-region clear
// instead. The snapshot/replay primitives themselves are Gfx.CaptureRegion / Gfx.BlitRegion.
public partial class MapView
{
    // Both snapshots store driver Cells directly, so capture and replay are plain struct
    // copies — no grapheme re-parsing, no allocation.
    private Cell[]? _frameBuf;
    private int _frameW, _frameH;
    private bool _frameDirty = true;

    private Cell[]? _tileBuf;
    private int _tileW, _tileH;
    private bool _tileDirty = true;

    // True if the prior recompute had active animations — forces one cleanup recompute
    // after the last animation tick so the captured snapshot has no stale flashes baked in.
    private bool _animTailDirty;
    // The same one-frame tail for the tile layer, covering the effects the tile loop itself
    // reads (hit flash, crit flash, status tint, shake).
    private bool _tileTailDirty;

    // Set by the tile loop: true when an animated tile (lava, campfire, divine ore) is
    // actually on screen. Gates the 50ms repaint timer so floors that merely contain
    // water somewhere don't repaint continuously.
    private bool _animatedTileInView;

    // Viewport positions of on-screen tiles whose glyph or colour is wall-clock driven —
    // the animated types plus water, which is excluded from _animatedTileInView because its
    // TileDefinitions visual is a pure position hash while MapView.ResolveWater overrides the
    // glyph from a 2.5Hz clock phase.
    //
    // These few cells are re-resolved over a replayed tile layer rather than refusing the
    // replay: a single campfire in the Town of Beginnings plaza otherwise disables the whole
    // optimisation, and a measured frame averaged under 10 such cells. Recorded positions stay
    // valid across a replay because anything that could move them — a camera move, a tile-type
    // change, a visibility update — invalidates the tile layer and forces the full loop.
    private readonly List<(int Vx, int Vy)> _timeVaryingCells = new();

    // Above this share of the viewport, patching costs as much as recomputing, so the full
    // loop runs instead (a lava field, or standing in a lake).
    private const int TimeVaryingPatchDivisor = 4;

    private bool CanPatchTimeVaryingCells(int width, int height)
        => _timeVaryingCells.Count <= width * height / TimeVaryingPatchDivisor;

    // External invalidators (ParticleQueue.Push, ToastQueue.Enqueue, GameMap.OnTileTypeChanged)
    // can't reach a specific MapView, so they raise a static dirty pulse.
    private static volatile bool s_externalDirty = true;
    public static void MarkFrameDirty() => s_externalDirty = true;

    // Instance-level mark — public so MapView's own mutators (AddDamageFlash etc.)
    // can dirty without the static pulse. Invalidates both layers: most state changes
    // move an entity or a tile, and over-invalidating costs one tile loop, not correctness.
    internal void DirtyFrame()
    {
        _frameDirty = true;
        _tileDirty = true;
    }

    // Overlay-only invalidation: the frame must be repainted but the tile loop would
    // produce identical output. Only valid for state the tile loop does not read.
    internal void DirtyOverlays() => _frameDirty = true;

    // True when a recompute is needed: instance flag, external pulse, animation tail, or no buffer yet.
    //
    // Clock-driven tiles on screen count too. Without that the finished-frame blit returns before
    // any tile or overlay work, so campfires, lava and water only animated while something else
    // happened to keep the frame dirty. The cost is the tile-layer path (~2.2ms) instead of a
    // frame blit (~0.7ms) on those frames, and a terminal flush on the frames where a phase
    // actually turns over — which is why the animation intervals are deliberately slow.
    private bool IsFrameDirty(int width, int height)
    {
        if (_frameDirty || s_externalDirty || _animTailDirty) return true;
        if (_timeVaryingCells.Count > 0) return true;
        if (_frameBuf == null) return true;
        if (_frameW != width || _frameH != height) return true;
        return false;
    }

    // True when the tile loop has to run. The external pulse counts here too because it
    // carries tile-type changes; it is cleared by CaptureFrame at the end of the same pass,
    // so both layers read one consistent value per frame.
    private bool IsTileLayerDirty(int width, int height)
    {
        if (_tileDirty || s_externalDirty || _tileTailDirty) return true;
        if (_tileBuf == null) return true;
        if (_tileW != width || _tileH != height) return true;
        return false;
    }

    // Capture the just-rendered viewport into _frameBuf.
    // Called at the END of OnDrawingContent on the recompute path.
    // The snapshot is only considered valid if the driver actually yielded one, so a
    // failed capture leaves the cache dirty and the next frame recomputes.
    private void CaptureFrame(int width, int height)
    {
        using var _scope = Profiler.Begin("MapView.FrameCache.Capture");
        if (_frameBuf == null || _frameW != width || _frameH != height)
        {
            _frameBuf = new Cell[width * height];
            _frameW = width;
            _frameH = height;
        }

        if (!Gfx.CaptureRegion(this, _frameBuf, width, height)) return;

        _frameDirty = false;
        s_externalDirty = false;
    }

    // Capture the tile layer. Called immediately after the tile loop, before any overlay
    // has painted, so the buffer holds nothing but tiles at this point.
    private void CaptureTileLayer(int width, int height)
    {
        using var _scope = Profiler.Begin("MapView.TileLayer.Capture");
        if (_tileBuf == null || _tileW != width || _tileH != height)
        {
            _tileBuf = new Cell[width * height];
            _tileW = width;
            _tileH = height;
        }

        if (!Gfx.CaptureRegion(this, _tileBuf, width, height)) return;

        _tileDirty = false;
    }

    // Replay cached cells. Returns false when there is no usable snapshot, which sends
    // the caller down the recompute path.
    private bool TryBlitFrame(int width, int height)
    {
        if (_frameBuf == null || _frameW != width || _frameH != height) return false;

        using var _scope = Profiler.Begin("MapView.FrameCache.Blit");
        return Gfx.BlitRegion(this, _frameBuf, width, height);
    }

    // Replay the tile layer over the whole viewport. Because the tile loop covers every
    // cell, this also erases the previous frame's overlays. Returns false when there is no
    // usable snapshot, which sends the caller back to the tile loop.
    private bool TryBlitTileLayer(int width, int height)
    {
        if (_tileBuf == null || _tileW != width || _tileH != height) return false;

        using var _scope = Profiler.Begin("MapView.TileLayer.Blit");
        return Gfx.BlitRegion(this, _tileBuf, width, height);
    }
}
