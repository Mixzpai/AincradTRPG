using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Projectile + status-trail rendering (FB-454). Cell-stepped tweens for
// arrows / sword arcs, stationary 3-frame fades for DoT motes.
public partial class MapView
{
    private readonly List<Projectile> _projectiles = new();
    private readonly List<StatusTrail> _statusTrails = new();

    // startX/startY → endX/endY at msPerCell. Impact resolves when elapsed
    // crosses last cell; one-shot — caller enqueues on hit emission.
    public void EnqueueProjectile(int sx, int sy, int ex, int ey,
        char glyph, Color color, int msPerCell,
        char? trailGlyph = '·', Color? trailColor = null)
    {
        var p = new Projectile
        {
            StartX = sx, StartY = sy, EndX = ex, EndY = ey,
            Glyph = glyph, Color = color, MsPerCell = msPerCell,
            TrailGlyph = trailGlyph, TrailColor = trailColor ?? color,
            Path = BresenhamPath(sx, sy, ex, ey),
        };
        _projectiles.Add(p);
        DirtyFrame();
    }

    // Arrow convenience — directional glyph from (dx, dy) vector.
    public void EnqueueArrow(int sx, int sy, int ex, int ey, Color color)
    {
        int dx = ex - sx, dy = ey - sy;
        char glyph = (Math.Abs(dx), Math.Abs(dy)) switch
        {
            (0, _) => dy < 0 ? '↑' : '↓',
            (_, 0) => dx < 0 ? '←' : '→',
            _      => (dx > 0) == (dy > 0) ? '\\' : '/',
        };
        EnqueueProjectile(sx, sy, ex, ey, glyph, color, 40, '·', color);
    }

    // Stationary 3-frame fade for bleed / poison / burn motes. Per research
    // §2: ◇→·→blank for SAO-theme bleed (no red blood).
    public void EnqueueStatusTrail(int x, int y, char[] glyphs, Color color, int frameMs = 500)
    {
        if (glyphs.Length == 0) return;
        _statusTrails.Add(new StatusTrail
        { X = x, Y = y, Glyphs = glyphs, Color = color, FrameMs = frameMs });
        DirtyFrame();
    }

    public void ClearProjectiles()
    { _projectiles.Clear(); _statusTrails.Clear(); }

    public bool HasActiveProjectiles => _projectiles.Count > 0 || _statusTrails.Count > 0;

    private static (int X, int Y)[] BresenhamPath(int x0, int y0, int x1, int y1)
    {
        var pts = new List<(int, int)>();
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int cx = x0, cy = y0;
        while (true)
        {
            pts.Add((cx, cy));
            if (cx == x1 && cy == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 <  dx) { err += dx; cy += sy; }
            if (pts.Count > 256) break; // pathological safety
        }
        return pts.ToArray();
    }

    private void RenderProjectiles(int w, int h, int dtMs)
    {
        // Arrows / arcs — advance elapsed, draw head + trail, remove on impact.
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var p = _projectiles[i];
            p.ElapsedMs += dtMs;
            int total = p.Path.Length;
            if (total == 0) { _projectiles.RemoveAt(i); continue; }
            int idx = p.CurrentCellIndex(total);

            if (idx >= total - 1 && p.ElapsedMs >= p.MsPerCell * (total - 1))
            {
                var (fx, fy) = p.Path[total - 1];
                if (_map.InBounds(fx, fy) && _map.IsVisible(fx, fy))
                    DrawGlyph(fx, fy, p.Glyph, Gfx.Attr(p.Color, Color.Black), w, h);
                _projectiles.RemoveAt(i);
                continue;
            }

            var (hx, hy) = p.Path[idx];
            if (_map.InBounds(hx, hy) && _map.IsVisible(hx, hy))
                DrawGlyph(hx, hy, p.Glyph, Gfx.Attr(p.Color, Color.Black), w, h);

            if (idx > 0 && p.TrailGlyph.HasValue)
            {
                var (tx, ty) = p.Path[idx - 1];
                if (_map.InBounds(tx, ty) && _map.IsVisible(tx, ty))
                    DrawGlyph(tx, ty, p.TrailGlyph.Value,
                        Gfx.Attr(p.TrailColor ?? Color.DarkGray, Color.Black), w, h);
            }
        }

        // Stationary status trails — tick, draw current frame glyph, remove when done.
        for (int i = _statusTrails.Count - 1; i >= 0; i--)
        {
            var s = _statusTrails[i];
            s.ElapsedMs += dtMs;
            if (s.ElapsedMs >= s.LifetimeMs) { _statusTrails.RemoveAt(i); continue; }
            if (!_map.InBounds(s.X, s.Y) || !_map.IsVisible(s.X, s.Y)) continue;
            var g = s.CurrentGlyph();
            if (g.HasValue && g.Value != ' ')
                DrawGlyph(s.X, s.Y, g.Value, Gfx.Attr(s.Color, Color.Black), w, h);
        }
    }
}
