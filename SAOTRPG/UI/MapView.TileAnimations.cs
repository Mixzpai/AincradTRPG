using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Ambient-tile animation overlay. FOV-gated, capped at TileAnimator.MaxConcurrent per frame.
// Campfire + enchant-shrine variants suppress during combat (TileAnimator.CombatActive).
public partial class MapView
{
    // Fixed scratch sized for MaxConcurrent*4 candidates; partial-sort top-N online so
    // there's no per-frame List allocation or full sort.
    private struct AmbientCandidate { public int X, Y, Priority, Dist; public TileType Type; }
    private const int AmbientCapacity = 16;
    private readonly AmbientCandidate[] _ambientScratch = new AmbientCandidate[AmbientCapacity];

    // Better-than: higher priority, or same priority + smaller distance.
    private static bool AmbientBetter(in AmbientCandidate a, in AmbientCandidate b)
        => a.Priority > b.Priority || (a.Priority == b.Priority && a.Dist < b.Dist);

    // Called by RenderOverlays. Scans visible tiles, picks up to MaxConcurrent
    // nearest-to-player animated tiles, overlays the current frame glyph.
    partial void RenderAmbientTiles(int vpWidth, int vpHeight)
    {
        if (!TileAnimator.Enabled) return;

        int count = 0;
        int px = _player.X, py = _player.Y;
        int worstIdx = -1;

        for (int vy = 0; vy < vpHeight; vy++)
        for (int vx = 0; vx < vpWidth; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;
            var tile = _map.GetTile(mx, my);
            if (!TileAnimator.Registry.TryGetValue(tile.Type, out var anim)) continue;
            if (TileAnimator.CombatActive && TileAnimator.IsCombatSuppressed(tile.Type)) continue;
            int dist = Math.Abs(mx - px) + Math.Abs(my - py);

            var newCand = new AmbientCandidate { X = mx, Y = my, Type = tile.Type, Priority = anim.Priority, Dist = dist };
            if (count < AmbientCapacity)
            {
                _ambientScratch[count] = newCand;
                count++;
                worstIdx = -1; // recompute next time we hit capacity
            }
            else
            {
                if (worstIdx < 0)
                {
                    worstIdx = 0;
                    for (int i = 1; i < count; i++)
                        if (AmbientBetter(_ambientScratch[worstIdx], _ambientScratch[i])) worstIdx = i;
                }
                if (AmbientBetter(newCand, _ambientScratch[worstIdx]))
                {
                    _ambientScratch[worstIdx] = newCand;
                    worstIdx = -1;
                }
            }
        }

        if (count == 0) return;

        // Selection-sort top-MaxConcurrent: cheap when count ≤ AmbientCapacity (16).
        int n = Math.Min(TileAnimator.MaxConcurrent, count);
        for (int i = 0; i < n; i++)
        {
            int best = i;
            for (int j = i + 1; j < count; j++)
                if (AmbientBetter(_ambientScratch[j], _ambientScratch[best])) best = j;
            if (best != i) (_ambientScratch[i], _ambientScratch[best]) = (_ambientScratch[best], _ambientScratch[i]);
        }

        for (int i = 0; i < n; i++)
        {
            ref var c = ref _ambientScratch[i];
            if (TileAnimator.IsSparkleOverlay(c.Type))
            {
                var s = TileAnimator.ChestSparkle();
                if (s.HasValue)
                    DrawGlyph(c.X, c.Y, s.Value.Glyph, Gfx.Attr(s.Value.Color, Color.Black), vpWidth, vpHeight);
                continue;
            }
            var (glyph, color) = TileAnimator.CurrentFrame(c.Type);
            if (glyph == ' ') continue;
            DrawGlyph(c.X, c.Y, glyph, Gfx.Attr(color, Color.Black), vpWidth, vpHeight);
        }
    }
}
