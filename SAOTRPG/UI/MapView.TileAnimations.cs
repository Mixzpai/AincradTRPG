using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Ambient-tile animation overlay. FOV-gated, capped at TileAnimator.MaxConcurrent per frame.
// Campfire + enchant-shrine variants suppress during combat (TileAnimator.CombatActive).
public partial class MapView
{
    // Called by RenderOverlays. Scans visible tiles, picks up to MaxConcurrent
    // nearest-to-player animated tiles, overlays the current frame glyph.
    partial void RenderAmbientTiles(int vpWidth, int vpHeight)
    {
        if (!TileAnimator.Enabled) return;

        var candidates = new List<(int X, int Y, TileType Type, int Priority, int Dist)>();
        int px = _player.X, py = _player.Y;

        for (int vy = 0; vy < vpHeight; vy++)
        for (int vx = 0; vx < vpWidth; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            if (!TileAnimator.Registry.TryGetValue(tile.Type, out var anim)) continue;
            if (TileAnimator.CombatActive && TileAnimator.IsCombatSuppressed(tile.Type)) continue;
            int dist = Math.Abs(mx - px) + Math.Abs(my - py);
            candidates.Add((mx, my, tile.Type, anim.Priority, dist));
        }

        if (candidates.Count == 0) return;

        // Priority first (chest > terminal > hearth > ...), then nearest-to-player.
        candidates.Sort((a, b) =>
        {
            int c = b.Priority.CompareTo(a.Priority);
            return c != 0 ? c : a.Dist.CompareTo(b.Dist);
        });

        int n = Math.Min(TileAnimator.MaxConcurrent, candidates.Count);
        for (int i = 0; i < n; i++)
        {
            var (mx, my, type, _, _) = candidates[i];
            if (TileAnimator.IsSparkleOverlay(type))
            {
                var s = TileAnimator.ChestSparkle();
                if (s.HasValue)
                    DrawGlyph(mx, my, s.Value.Glyph, Gfx.Attr(s.Value.Color, Color.Black), vpWidth, vpHeight);
                continue;
            }
            var (glyph, color) = TileAnimator.CurrentFrame(type);
            if (glyph == ' ') continue;
            DrawGlyph(mx, my, glyph, Gfx.Attr(color, Color.Black), vpWidth, vpHeight);
        }
    }
}
