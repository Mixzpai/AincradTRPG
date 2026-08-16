using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Two-layer frame cache over a damage-tracked tile layer.
//
//   tile layer  — every viewport cell resolved from map state, held in _tileBuf.
//   frame layer — the finished frame, overlays included.
//
// A fully idle frame blits the frame layer and paints nothing at all. Any live effect keeps
// the frame dirty every frame while the tiles under it barely move, so those frames replay
// the tile layer — which also erases the previous frame's overlays — and run only the overlay
// passes over it.
//
// _tileBuf is authoritative: the tile loop, the incremental patch pass and the verifier all
// write it, and it is replayed to the driver with Gfx.BlitGlyphs. Nothing reads the layer
// back off the driver, so an overdrawn or clipped cell can never leak into the cache.
//
// The layer is kept current incrementally. Holding a movement key scrolls the camera one
// column per step, and recomputing all ~28,756 cells for that costs more than the terminal
// does. Instead the buffer is memmoved by the camera delta and only *changed* cells are
// resolved. What changed is established two ways:
//
//   * per cell, by comparing the inputs the resolve reads (TileKey) against the values it
//     read last time — exact, and impossible to leave stale by forgetting to notify;
//   * for the few inputs too expensive to sample per cell (occupants, dropped items, hit
//     flashes) by marking those cells, which are always a handful.
//
// Inputs that repaint the whole viewport at once — a tile-type change, a screen-wide crit
// flash, a status tint — fall back to a full recompute rather than being tracked.
//
// PATH-D-PORT: the erase-and-replace only works because the tile layer covers every cell of
// the viewport, so replaying it wipes whatever the overlays drew last frame. A renderer whose
// tile pass can leave cells untouched needs an explicit overlay-region clear instead. The
// snapshot/replay primitives are Gfx.CaptureRegion / BlitRegion for the frame layer and
// Gfx.BlitGlyphs for the tile layer; the damage tracking above is renderer-independent.
public partial class MapView
{
    // Stores driver Cells directly, so capture and replay are plain struct copies — no
    // grapheme re-parsing, no allocation. Only ever snapshotted off the driver and replayed.
    private Cell[]? _frameBuf;
    private int _frameW, _frameH;
    private bool _frameDirty = true;

    // True if the prior recompute had active animations — forces one cleanup recompute
    // after the last animation tick so the captured snapshot has no stale flashes baked in.
    private bool _animTailDirty;

    // ── Tile layer ─────────────────────────────────────────────────────
    // Glyphs rather than driver Cells: a Cell holds its grapheme as a string, so an array of
    // them pays a write barrier per store and cannot be memmoved — and this one is written
    // and scrolled every frame.
    private Gfx.Glyph[]? _tileBuf;
    // Per-cell record of what the resolve read, parallel to _tileBuf.
    private TileKey[]? _tileKey;
    // Cells marked by a source that is not sampled per cell. Cleared every patch.
    private bool[]? _tileDamage;
    private int _tileW, _tileH;
    private bool _tileDirty = true;
    // The same one-frame tail as _animTailDirty, covering the effects the tile layer reads.
    private bool _tileTailDirty;
    // Set when something moved that the per-cell scan cannot see. Consumed by the next paint.
    private bool _tileFullDirty = true;

    // Whole-layer inputs as of the last render. Any difference forces a full recompute:
    // each of these either remaps every cell or repaints every cell.
    private GameMap? _tileMap;
    private int _tileOffX, _tileOffY;
    // The active theme recolours every tile through ApplyLighting, so it is a whole-viewport
    // input the per-cell damage tracker cannot see — it belongs with the other values the layer
    // records having been built with, or a theme switch would leave the map on the old palette
    // until something else happened to dirty each cell.
    private ThemeId _tileTheme = ThemeId.Default;
    private bool _tileCritFlash;
    private Color? _tileStatusTint;

    // Turn the layer was rendered against. Everything the per-cell scan samples — the visible
    // set, the light grid, the memory fade — moves only when a turn is taken, and a turn can
    // pass with nothing else marking the layer: a step into a wall, a monster's turn while the
    // player rests, an auto-explore tick. Comparing the counter closes all of those at once.
    private int _tileTurn = -1;

    // Cells whose glyph or colour is wall-clock driven, counted by the last scan. They are
    // damaged unconditionally on every pass, so this is only a diagnostic and a gate: while
    // any is on screen the frame stays dirty, which is what lets campfires, lava and water
    // animate on otherwise-idle frames.
    private int _timeVaryingCount;

    // Set by the scan: true when an animated tile (lava, campfire, divine ore) is actually
    // on screen. Gates the 50ms repaint timer so floors that merely contain water somewhere
    // don't repaint continuously.
    private bool _animatedTileInView;

    // Inputs the cell resolve reads, sampled per cell and compared against the previous pass.
    // 20 bytes: four payload bytes plus the light triple, padded.
    private struct TileKey
    {
        // Sampled inputs. A difference in any of these means the cell must be resolved again.
        public byte Flags;
        // Quantized distance-from-player band driving the edge vignette. It HAS to be sampled here:
        // the vignette moves with the player, so without it the incremental patch would carry a
        // cell's old shading to its new distance and the map would dim wrongly as you walk.
        public byte Vignette;
        // What the last resolve found at this cell. These are outputs, not inputs, so they are
        // excluded from the comparison — they exist to catch the *disappearance* of something
        // the scan cannot cheaply test for.
        public byte State;
        public byte Type;
        // Quantized memory-fade step; 0 for visible cells.
        public byte Fade;
        public LightingSystem.LightRgb Light;

        public const byte FInBounds = 1;
        public const byte FExplored = 2;
        public const byte FVisible = 4;
        public const byte FTrapHidden = 8;

        public const byte SOccupant = 1;
        public const byte SItems = 2;
        public const byte SHitFlash = 4;

        // Inputs only. Light is compared for visible cells alone: nothing else reads it, and
        // an ambient refill outside the field of view would otherwise churn memory tiles.
        public bool InputsMatch(in TileKey other)
        {
            if (Flags != other.Flags || Type != other.Type || Fade != other.Fade) return false;
            // Vignette rides in the visible-only branch with Light: memory tiles bypass
            // ApplyLighting for their own palette, so they are never vignetted and comparing it
            // there would churn them every time the player takes a step.
            if ((Flags & FVisible) == 0) return true;
            if (Vignette != other.Vignette) return false;
            return Light.R == other.Light.R && Light.G == other.Light.G && Light.B == other.Light.B;
        }
    }

    // Above this share of the viewport, tracking costs as much as recomputing.
    private const int TimeVaryingPatchDivisor = 4;

    // External invalidators (ParticleQueue.Push, ToastQueue.Enqueue, DialogHelper) can't reach
    // a specific MapView, so they raise a static dirty pulse.
    private static volatile bool s_externalDirty = true;
    // The subset that also invalidates the tile layer wholesale — a tile-type change moves the
    // wall-glyph, transition and shoreline caches, none of which the per-cell scan samples.
    private static volatile bool s_externalTilesFull = true;

    public static void MarkFrameDirty() => s_externalDirty = true;

    public static void MarkTilesDirty()
    {
        s_externalDirty = true;
        s_externalTilesFull = true;
    }

    // Instance-level mark — public so MapView's own mutators (AddDamageFlash etc.) can dirty
    // without the static pulse. Marks the tile layer for a scan, not for a full recompute:
    // the scan establishes what actually changed.
    internal void DirtyFrame()
    {
        _frameDirty = true;
        _tileDirty = true;
    }

    // Overlay-only invalidation: the frame must be repainted but the tile layer is untouched.
    internal void DirtyOverlays() => _frameDirty = true;

    // Forces the next paint to rebuild the whole tile layer. For changes the per-cell scan
    // cannot see — a new map, or the door set the terrain resolve reads.
    internal void DirtyTilesFull()
    {
        _frameDirty = true;
        _tileDirty = true;
        _tileFullDirty = true;
    }

    // Marks one map cell for re-resolve on the next paint. Translated to viewport coordinates
    // at paint time, against the camera as it is then.
    private readonly List<(int X, int Y)> _tileDamagePoints = new();

    private void DamageMapCell(int mx, int my) => _tileDamagePoints.Add((mx, my));

    // Effective viewport origin. Shake is folded in here rather than treated separately, so a
    // shake step is just a small scroll and rides the same memmove as ordinary movement.
    private int EffOffsetX => _camera.OffsetX - ShakeOffsetX;
    private int EffOffsetY => _camera.OffsetY - ShakeOffsetY;

    // True when a recompute is needed: instance flag, external pulse, animation tail, or no
    // buffer yet.
    //
    // Clock-driven tiles on screen count too. Without that the finished-frame blit returns
    // before any tile or overlay work, so campfires, lava and water only animated while
    // something else happened to keep the frame dirty.
    private bool IsFrameDirty(int width, int height)
    {
        // s_externalTilesFull is tested as well as s_externalDirty: the two are cleared at
        // different points of a painted frame, so a pulse landing between them would otherwise
        // leave the tile layer marked for rebuild on a frame that never repaints.
        if (_frameDirty || s_externalDirty || s_externalTilesFull || _animTailDirty) return true;
        if (_timeVaryingCount > 0) return true;
        if (DayNightCycle.CurrentTurn != _tileTurn) return true;
        if (_frameBuf == null) return true;
        if (_frameW != width || _frameH != height) return true;
        return false;
    }

    // Allocates or resizes the tile-layer buffers. Returns true when they were replaced,
    // which means their contents are meaningless and the layer has to be rebuilt.
    private bool EnsureTileBuffers(int width, int height)
    {
        if (_tileBuf != null && _tileW == width && _tileH == height) return false;

        int n = width * height;
        _tileBuf = new Gfx.Glyph[n];
        _tileKey = new TileKey[n];
        _tileDamage = new bool[n];
        _tileW = width;
        _tileH = height;
        return true;
    }

    // True when the layer has to be rebuilt from scratch rather than patched.
    private bool NeedsFullTileRecompute()
    {
        if (_tileFullDirty || s_externalTilesFull) return true;
        // Also the backstop for the BIOME WASH, which is a whole-viewport input to every cell the
        // tile loop resolves and gets no field of its own here. It cannot change without the map
        // object changing: it is read from BiomeSystem.CurrentGenConfig, which only SetFloor
        // writes, and both routes to SetFloor — entering a floor and the F9 biome hot-reload —
        // build a new GameMap. Add explicit tracking the moment a config can be swapped in place,
        // or the map keeps painting the previous biome's palette until something else dirties it.
        if (!ReferenceEquals(_tileMap, _map)) return true;
        // A screen-wide fg boost and a status tint touch every cell; the scan samples neither.
        if (_critScreenFlashRemainingMs > 0 != _tileCritFlash) return true;
        if (_statusTintCurrentColor != _tileStatusTint) return true;
        if (ColorSchemes.Theme.Id != _tileTheme) return true;
        // Camera jumped further than the viewport — floor change, teleport. Nothing to reuse.
        if (Math.Abs(EffOffsetX - _tileOffX) >= _tileW) return true;
        if (Math.Abs(EffOffsetY - _tileOffY) >= _tileH) return true;
        // A lava field or a lake: patching every cell costs what recomputing does.
        if (_timeVaryingCount > _tileW * _tileH / TimeVaryingPatchDivisor) return true;
        return false;
    }

    // True when the layer has to be re-scanned. False means nothing at all has happened that
    // could move a tile cell, and the existing buffer is replayed untouched.
    private bool NeedsTileScan()
    {
        if (_tileDirty || s_externalDirty || _tileTailDirty) return true;
        if (_timeVaryingCount > 0) return true;
        if (_tileDamagePoints.Count > 0) return true;
        if (EffOffsetX != _tileOffX || EffOffsetY != _tileOffY) return true;
        if (DayNightCycle.CurrentTurn != _tileTurn) return true;
        return false;
    }

    // Records the whole-layer inputs this render used and clears the pending marks.
    private void CommitTileState()
    {
        _tileMap = _map;
        _tileOffX = EffOffsetX;
        _tileOffY = EffOffsetY;
        _tileTheme = ColorSchemes.Theme.Id;
        _tileCritFlash = _critScreenFlashRemainingMs > 0;
        _tileStatusTint = _statusTintCurrentColor;
        _tileTurn = DayNightCycle.CurrentTurn;
        _tileDirty = false;
        _tileFullDirty = false;
        s_externalTilesFull = false;
        _tileDamagePoints.Clear();
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

    // Replay cached cells. Returns false when there is no usable snapshot, which sends
    // the caller down the recompute path.
    private bool TryBlitFrame(int width, int height)
    {
        if (_frameBuf == null || _frameW != width || _frameH != height) return false;

        using var _scope = Profiler.Begin("MapView.FrameCache.Blit");
        return Gfx.BlitRegion(this, _frameBuf, width, height);
    }

    // Memmoves a viewport-sized buffer by the camera delta. A cell that was showing map
    // column mx at viewport column vx shows it at vx - dx once the camera has moved by dx,
    // so the copy runs in the direction that keeps overlapping rows intact.
    private static void ShiftViewport<T>(T[] buf, int w, int h, int dx, int dy)
    {
        int copyW = w - Math.Abs(dx);
        int copyH = h - Math.Abs(dy);
        if (copyW <= 0 || copyH <= 0) return;

        int srcX = Math.Max(0, dx);
        int srcY0 = Math.Max(0, dy);
        int dstX = srcX - dx;

        if (dy > 0)
        {
            for (int i = 0; i < copyH; i++)
            {
                int srcY = srcY0 + i;
                Array.Copy(buf, srcY * w + srcX, buf, (srcY - dy) * w + dstX, copyW);
            }
        }
        else
        {
            for (int i = copyH - 1; i >= 0; i--)
            {
                int srcY = srcY0 + i;
                Array.Copy(buf, srcY * w + srcX, buf, (srcY - dy) * w + dstX, copyW);
            }
        }
    }

    // Marks the band of viewport cells the scroll exposed — they hold whatever the memmove
    // left behind, and their keys are just as meaningless.
    private static void MarkExposedBand(bool[] dmg, int w, int h, int dx, int dy)
    {
        if (dx != 0)
        {
            int x0 = dx > 0 ? w - dx : 0;
            int x1 = dx > 0 ? w : -dx;
            if (x0 < 0) x0 = 0;
            if (x1 > w) x1 = w;
            for (int vy = 0; vy < h; vy++)
            for (int vx = x0; vx < x1; vx++)
                dmg[vy * w + vx] = true;
        }

        if (dy != 0)
        {
            int y0 = dy > 0 ? h - dy : 0;
            int y1 = dy > 0 ? h : -dy;
            if (y0 < 0) y0 = 0;
            if (y1 > h) y1 = h;
            for (int vy = y0; vy < y1; vy++)
                Array.Fill(dmg, true, vy * w, w);
        }
    }
}
