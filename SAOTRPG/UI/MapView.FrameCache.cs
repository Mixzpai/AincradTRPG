using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Whole-frame cache. Tile-loop + overlays + particles are skipped on idle frames
// (cache-hit blits the snapshot); any state change re-flips _dirty true.
public partial class MapView
{
    // Snapshot taken after the recompute pass; replayed verbatim on cache-hit.
    private (System.Text.Rune Ch, Color Fg, Color Bg)[]? _frameBuf;
    private int _frameW, _frameH;
    private bool _frameDirty = true;
    // True if the prior recompute had active animations — forces one cleanup recompute
    // after the last animation tick so the captured snapshot has no stale flashes baked in.
    private bool _animTailDirty;

    // External invalidators (ParticleQueue.Push, ToastQueue.Enqueue, GameMap.OnTileTypeChanged)
    // can't reach a specific MapView, so they raise a static dirty pulse.
    private static volatile bool s_externalDirty = true;
    public static void MarkFrameDirty() => s_externalDirty = true;

    // Instance-level mark — public so MapView's own mutators (AddDamageFlash etc.)
    // can dirty without the static pulse.
    internal void DirtyFrame() => _frameDirty = true;

    // True when a recompute is needed: instance flag, external pulse, animation tail, or no buffer yet.
    private bool IsFrameDirty(int width, int height)
    {
        if (_frameDirty || s_externalDirty || _animTailDirty) return true;
        if (_frameBuf == null) return true;
        if (_frameW != width || _frameH != height) return true;
        return false;
    }

    // Capture the just-rendered viewport from Driver.Contents into _frameBuf.
    // Called at the END of OnDrawingContent on the recompute path.
    private void CaptureFrame(int width, int height)
    {
        using var _scope = Profiler.Begin("MapView.FrameCache.Capture");
        if (_frameBuf == null || _frameW != width || _frameH != height)
        {
            _frameBuf = new (System.Text.Rune, Color, Color)[width * height];
            _frameW = width;
            _frameH = height;
        }

        var driver = View.Driver;
        if (driver == null) return;
        var contents = driver.Contents;
        if (contents == null) return;
        int driverRows = contents.GetLength(0);
        int driverCols = contents.GetLength(1);

        // ViewportToScreen translates view-relative (0,0) to screen-relative origin.
        var origin = ViewportToScreen(new System.Drawing.Point(0, 0));
        for (int vy = 0; vy < height; vy++)
        {
            int sy = origin.Y + vy;
            if (sy < 0 || sy >= driverRows) continue;
            for (int vx = 0; vx < width; vx++)
            {
                int sx = origin.X + vx;
                if (sx < 0 || sx >= driverCols) continue;
                var cell = contents[sy, sx];
                Color fg = cell.Attribute?.Foreground ?? Color.White;
                Color bg = cell.Attribute?.Background ?? Color.Black;
                _frameBuf[vy * width + vx] = (cell.Rune, fg, bg);
            }
        }

        _frameDirty = false;
        s_externalDirty = false;
    }

    // Replay cached cells. Goes through Driver directly so the Rune (incl. non-BMP) survives;
    // collapses adjacent cells with the same attribute into one SetAttribute call.
    private bool TryBlitFrame(int width, int height)
    {
        if (_frameBuf == null || _frameW != width || _frameH != height) return false;
        var driver = View.Driver;
        if (driver == null) return false;

        using var _scope = Profiler.Begin("MapView.FrameCache.Blit");
        var buf = _frameBuf;
        Color lastFg = Color.Black, lastBg = Color.Black;
        bool attrSet = false;
        for (int vy = 0; vy < height; vy++)
        {
            int rowBase = vy * width;
            for (int vx = 0; vx < width; vx++)
            {
                ref var cell = ref buf[rowBase + vx];
                if (!attrSet || cell.Fg != lastFg || cell.Bg != lastBg)
                {
                    driver.SetAttribute(Gfx.Attr(cell.Fg, cell.Bg));
                    lastFg = cell.Fg; lastBg = cell.Bg;
                    attrSet = true;
                }
                Move(vx, vy);
                driver.AddRune(cell.Ch);
            }
        }
        return true;
    }
}
