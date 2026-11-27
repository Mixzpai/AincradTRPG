using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;
using SAOTRPG.Map.Lighting;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.UI.Effects;

namespace SAOTRPG.UI.Rendering;

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
// column per step, and recomputing every viewport cell for that would dominate the frame.
// Instead the buffer is memmoved by the camera delta and only *changed* cells are
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
// The erase-and-replace depends on the tile layer covering every cell of the viewport, so
// replaying it wipes whatever the overlays drew last frame; a tile pass that left a cell
// untouched would leak the previous frame's overlay there. The snapshot/replay primitives are
// Gfx.CaptureRegion / BlitRegion for the frame layer and Gfx.BlitGlyphs for the tile layer.
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

    // ── Turn-repaint coalescing ────────────────────────────────────────
    // The map scrolls on every turn, so a turn repaint changes essentially every viewport cell
    // and pays a full-screen flush, a flat cost per drawn frame. Holding the previous frame for a
    // few tens of milliseconds paints fewer of them while a direction key auto-repeats, without
    // changing how fast the player moves: turns still resolve at key rate, only the painting is
    // coalesced.
    //
    // Scoped to TURN dirt only. Dirt with no turn behind it is a UI response (a dialog closing,
    // a toast, look mode) and repaints immediately, so nothing the player pokes feels laggier.
    // Set to 0 to disable.
    private const int MinTurnRepaintIntervalMs = 45;   // ~22 map repaints/s

    // A flag rather than a sentinel timestamp: CaptureFrame can allocate _frameBuf and then
    // return early when the driver yields no snapshot, which would leave a sentinel unstamped
    // while _frameBuf is non-null, and subtracting a long.MinValue sentinel overflows, so the
    // window test would read true forever and freeze the map.
    private bool _hasRepainted;
    private long _lastRepaintMs;
    private bool _repaintTailArmed;

    // Frames whose recompute was coalesced away, for the --perf line.
    private static int s_coalescedFrames;

    public static int DrainCoalescedFrames()
    {
        int n = s_coalescedFrames;
        s_coalescedFrames = 0;
        return n;
    }

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

    // The night exposure the layer was built with. It scales every lit cell and the per-cell scan
    // cannot see it, so it belongs here with the theme. The gain alone is sufficient: below the
    // knee it is a strictly decreasing function of the sky level, so it pins the whole operator,
    // and at 1.0 the operator is the identity whatever the knee and shoulder are.
    private float _tileExposure = 1f;

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

    // A sky step is spread over a few frames. A step of the sky moves every lit cell at once, each
    // by less than a just-noticeable difference (a channel moves at most ~15.7 of 255), so the
    // patch resolves at most a quarter of the viewport's such cells a frame and leaves the rest,
    // stale keys and all, for the next; the resolve and the flush to the terminal spread with it.
    // Any other change, a torch's pool included, is resolved on the frame it happens.
    private const float SubtleLight = 16f;
    private const int LightSpreadFrames = 4;
    private bool _tileDeferred;
    private bool[]? _tileDeferMask;
    private bool _deferTailArmed;
    private static int s_spreadFrames, s_spreadCells;

    // Read-only, for the verification probes: whether cells are still held back, and running totals.
    public bool LightCellsPending => _tileDeferred;
    public static int SpreadFrames => s_spreadFrames;
    public static int SpreadCells => s_spreadCells;

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
        // Quantized distance-from-player band driving the edge vignette. It has to be sampled here:
        // the vignette moves with the player, so without it the incremental patch would carry a
        // cell's old shading to its new distance and the map would dim wrongly as you walk.
        public byte Vignette;
        // View-edge feather depth, 0-9: 0 nothing unseen within two cells, 1 an unseen cell at
        // Chebyshev distance 2, 2-9 an adjacent one. Drives the background's feather, so it is a
        // per-cell render input and has to be sampled here — it reads neighbours' visibility over
        // a 5x5, which Flags (this cell's own) cannot see.
        public byte ViewEdge;
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
            if (Vignette != other.Vignette || ViewEdge != other.ViewEdge) return false;
            return Light.R == other.Light.R && Light.G == other.Light.G && Light.B == other.Light.B;
        }

        // True when the light is the only input that moved, and by no more than a sky step moves it.
        public bool SubtleLightChange(in TileKey other)
        {
            if (Flags != other.Flags || Type != other.Type || Fade != other.Fade) return false;
            if ((Flags & FVisible) == 0 || Vignette != other.Vignette || ViewEdge != other.ViewEdge) return false;
            return MathF.Abs(Light.R - other.Light.R) <= SubtleLight
                   && MathF.Abs(Light.G - other.Light.G) <= SubtleLight
                   && MathF.Abs(Light.B - other.Light.B) <= SubtleLight;
        }
    }

    // ── Layer read-back ────────────────────────────────────────────────────────────────────────
    // The composed colour of each cell, exactly what Gfx.BlitGlyphs puts on screen, exposed for
    // the verification probes so they never reimplement ApplyLighting. The driver's own buffer
    // cannot stand in: under redirected stdio most of its cells hold no grapheme.
    //
    // Read-only and per cell, in the same family as LightingSystem.SourcesLastUpdate — it cannot
    // write the layer or fake a value. The region travels with the camera, so a caller needs
    // LayerRegion to know which map cells these are, and LayerTurn to know when.
    public bool TryGetLayerCell(int vx, int vy, out char ch, out Color fg, out Color bg)
    {
        ch = ' ';
        fg = Color.Black;
        bg = Color.Black;
        if (_tileBuf is null || (uint)vx >= (uint)_tileW || (uint)vy >= (uint)_tileH) return false;
        Gfx.Glyph g = _tileBuf[vy * _tileW + vx];
        ch = g.Ch;
        fg = g.Attr.Foreground;
        bg = g.Attr.Background;
        return true;
    }

    // Which map cells the layer currently describes, and the turn it was resolved against.
    public (int X, int Y, int W, int H) LayerRegion => (_tileOffX, _tileOffY, _tileW, _tileH);
    public int LayerTurn => _tileTurn;

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

    // Instance-level mark, so MapView's own mutators (AddDamageFlash etc.) can dirty
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
    // Clock-driven tiles on screen count too: otherwise the finished-frame blit returns before
    // any tile or overlay work, and campfires, lava and water would animate only while
    // something else kept the frame dirty.
    private bool IsFrameDirty(int width, int height)
    {
        // s_externalTilesFull is tested as well as s_externalDirty: the two are cleared at
        // different points of a painted frame, so a pulse landing between them would otherwise
        // leave the tile layer marked for rebuild on a frame that never repaints.
        //
        // These are structural and are never coalesced: a stale buffer, a resize, an external
        // pulse or a live animation each mean the previous frame is not a valid stand-in.
        if (s_externalDirty || s_externalTilesFull || _animTailDirty) return true;
        if (_timeVaryingCount > 0) return true;
        if (_tileDeferred) return true;
        if (_frameBuf == null) return true;
        if (_frameW != width || _frameH != height) return true;

        bool turnAdvanced = DayNightCycle.CurrentTurn != _tileTurn;
        if (!_frameDirty && !turnAdvanced) return false;

        // Dirt with no turn behind it is a UI response — repaint it now.
        if (!turnAdvanced) return true;

        // A turn repaint, and one landed very recently: hold the previous frame. _frameDirty is
        // left set, so nothing is lost — the next evaluation past the window recomputes.
        if (MinTurnRepaintIntervalMs > 0 && _hasRepainted
            && FrameClock.ElapsedMs - _lastRepaintMs < MinTurnRepaintIntervalMs)
        {
            s_coalescedFrames++;
            ArmRepaintTail();
            return false;
        }

        return true;
    }

    // A coalesced frame is only safe if something is guaranteed to come back for it. The HUD
    // refresh after a move usually redraws on its own, but it does not when no HUD value
    // changed — and then the map would hold a stale position until the next keypress. One
    // pending timeout at a time; the flag is the re-entrancy guard AddOneShotTimeout requires.
    private void ArmRepaintTail()
    {
        if (_repaintTailArmed) return;
        _repaintTailArmed = true;
        AppHost.AddOneShotTimeout(TimeSpan.FromMilliseconds(MinTurnRepaintIntervalMs + 1), () =>
        {
            _repaintTailArmed = false;
            // Only nudge if the frame is still owed a repaint; a draw may have landed already.
            if (_frameDirty || DayNightCycle.CurrentTurn != _tileTurn) SetNeedsDraw();
            return false;
        });
    }

    // Cells held back owe the next frame. A mark made inside this draw pass is cleared when it
    // ends, so the nudge comes from a timeout after it, one at a time.
    private void ArmDeferredRedraw()
    {
        if (_deferTailArmed) return;
        _deferTailArmed = true;
        AppHost.AddOneShotTimeout(TimeSpan.FromMilliseconds(1), () =>
        {
            _deferTailArmed = false;
            if (_tileDeferred) SetNeedsDraw();
            return false;
        });
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
        _tileDeferMask = new bool[n];
        _tileDeferred = false;
        _tileW = width;
        _tileH = height;
        return true;
    }

    // True when the layer has to be rebuilt from scratch rather than patched.
    private bool NeedsFullTileRecompute()
    {
        if (_tileFullDirty || s_externalTilesFull) return true;
        // Also the backstop for the biome wash, which is a whole-viewport input to every cell the
        // tile loop resolves and gets no field of its own here. It cannot change without the map
        // object changing: it is read from BiomeSystem.CurrentGenConfig, which only SetFloor
        // writes, and both routes to SetFloor — entering a floor and the F9 biome hot-reload —
        // build a new GameMap. A config swapped in place would need its own tracking here, or the
        // map would keep painting the previous biome's palette until something else dirtied it.
        if (!ReferenceEquals(_tileMap, _map)) return true;
        // A screen-wide fg boost and a status tint touch every cell; the scan samples neither.
        if (_critScreenFlashRemainingMs > 0 != _tileCritFlash) return true;
        if (_statusTintCurrentColor != _tileStatusTint) return true;
        if (ColorSchemes.Theme.Id != _tileTheme) return true;
        if (ExposureGain != _tileExposure) return true;
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
        if (_timeVaryingCount > 0 || _tileDeferred) return true;
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
        _tileExposure = ExposureGain;
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
        // Stamped only on a successful capture, so a failed one cannot start a coalesce window
        // against a frame that was never stored.
        _lastRepaintMs = FrameClock.ElapsedMs;
        _hasRepainted = true;
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
