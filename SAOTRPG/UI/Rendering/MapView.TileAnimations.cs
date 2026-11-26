using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Rendering;

// Ambient-tile animation overlay. FOV-gated, capped at TileAnimator.MaxConcurrent per frame.
// Campfire + enchant-shrine variants suppress during combat (TileAnimator.CombatActive).
//
// THE OVERLAY MODULATES BRIGHTNESS AND NEVER THE GLYPH. Every one of the registry's seven entries
// sits on a tile that appears in TileDefinitions.MapLegend, and six of them animate through a rune
// another tile type owns: the enchant shrine drew the gas vent's '¤', the lever drew lava's '-',
// the fountain drew deep water's '≈' (a Swimming-L25 wall), the gas vent drew ordinary ground on
// all four of its frames, and the chest sparkled into ChestOpened's '◇'. The bounty board blanked
// itself outright. A landmark's identity glyph is worth more than the ambience, so the pulse now
// rides the cell's own rendered colour, read back from the composed tile layer.
//
// PATH-D-PORT: the visual contract is "this tile breathes" — same glyph, same hue, lightness
// swinging by AmbientPulse about what the tile layer resolved. The registry still supplies the
// rhythm (Frames.Length x IntervalMs is one cycle) and the priority ordering.
public partial class MapView
{
    // Fixed scratch sized for MaxConcurrent*4 candidates; partial-sort top-N online so
    // there's no per-frame List allocation or full sort.
    private struct AmbientCandidate { public int X, Y, Priority, Dist; public TileType Type; }
    private const int AmbientCapacity = 16;
    private readonly AmbientCandidate[] _ambientScratch = new AmbientCandidate[AmbientCapacity];

    // Peak-to-trough swing of the breath, as a fraction of the cell's own colour. +-12% of a
    // gamma-encoded channel is about +-4 dL* on the bright landmark palette these tiles use —
    // several times the ~1.0 dL* threshold of vision and about the same amplitude as the
    // per-cell palette dither, so it reads as a pulse rather than as a flash.
    private const float AmbientPulse = 0.12f;

    // Below this the repaint would be indistinguishable from the layer already on screen, so the
    // cell is left alone and the write is skipped.
    private const float AmbientPulseDeadband = 0.02f;

    // Registry membership as a flat lookup: the scan asks this of every visible cell in the
    // viewport, and a dictionary hash per cell is the wrong shape for that.
    private static readonly bool[] s_ambientTypes = BuildAmbientTypeGate();

    private static bool[] BuildAmbientTypeGate()
    {
        int n = 0;
        foreach (TileType t in Enum.GetValues<TileType>()) if ((int)t > n) n = (int)t;
        var gate = new bool[n + 1];
        foreach (var kv in TileAnimator.Registry) gate[(int)kv.Key] = true;
        return gate;
    }


    // MEASUREMENT SEAM, same family as MapView.DrainFramesDrawn: what the last painted frame's
    // ambient pass actually wrote, so a check can compare it against its own read of the tile
    // layer rather than against a value this file handed it. Fixed-size, never allocates, and
    // bounded by the density cap the pass already works under.
    private static readonly (int Vx, int Vy, char Ch)[] s_ambientLog =
        new (int, int, char)[TileAnimator.MaxConcurrent];
    private static int s_ambientLogCount;
    private static int s_ambientPulsesTotal;

    public static int LastAmbientPulseCount => s_ambientLogCount;
    public static (int Vx, int Vy, char Ch) LastAmbientPulse(int i) => s_ambientLog[i];

    // Cumulative across the run, so a check can assert the pass reached its subject at all.
    public static int DrainAmbientPulses()
    {
        int n = s_ambientPulsesTotal;
        s_ambientPulsesTotal = 0;
        return n;
    }

    // Better-than: higher priority, or same priority + smaller distance.
    private static bool AmbientBetter(in AmbientCandidate a, in AmbientCandidate b)
        => a.Priority > b.Priority || (a.Priority == b.Priority && a.Dist < b.Dist);

    // Called by RenderOverlays. Scans visible tiles, picks up to MaxConcurrent
    // nearest-to-player animated tiles, breathes the current frame's brightness onto them.
    partial void RenderAmbientTiles(int vpWidth, int vpHeight)
    {
        s_ambientLogCount = 0;
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
            int ti = (int)tile.Type;
            if ((uint)ti >= (uint)s_ambientTypes.Length || !s_ambientTypes[ti]) continue;
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
            float scale = TileAnimator.IsSparkleOverlay(c.Type)
                ? AmbientSparkleScale()
                : AmbientBreathScale(c.Type);
            if (MathF.Abs(scale - 1f) < AmbientPulseDeadband) continue;
            PulseCell(c.X, c.Y, scale, vpWidth, vpHeight);
        }
    }

    // Triangle wave over one full registry cycle (Frames.Length x IntervalMs), so the rhythm the
    // registry declares is preserved even though its frame glyphs are not drawn.
    private static float AmbientBreathScale(TileType type)
    {
        if (!TileAnimator.Registry.TryGetValue(type, out var anim)) return 1f;
        int frames = Math.Max(1, anim.Frames.Length);
        long cycle = (long)frames * Math.Max(1, anim.IntervalMs);
        float phase = (SAOTRPG.UI.Effects.FrameClock.AmbientMs % cycle) / (float)cycle;
        float tri = 1f - 2f * MathF.Abs(phase - 0.5f);
        return 1f + AmbientPulse * (2f * tri - 1f);
    }

    // The chest's glint keeps its own duty cycle — a short lift once every two seconds rather than
    // a continuous breath, so a chest reads as catching the light rather than as flickering.
    private static float AmbientSparkleScale()
        => TileAnimator.ChestSparkle() is null ? 1f : 1f + AmbientPulse * 2f;

    // Repaints one cell with exactly the glyph and hue the tile layer resolved for it, scaled in
    // brightness. Reading the layer back is what makes the identity guarantee structural: there is
    // no second copy of the tile's appearance here to drift from the one on screen.
    private void PulseCell(int mx, int my, float scale, int vpWidth, int vpHeight)
    {
        int vx = MapToVx(mx), vy = MapToVy(my);
        if (vx < 0 || vx >= vpWidth || vy < 0 || vy >= vpHeight) return;
        if (!TryGetLayerCell(vx, vy, out char ch, out Color fg, out Color bg)) return;
        PutCellFast(vx, vy, ch, Gfx.Attr(ScaleChannels(fg, scale), bg));
        if (s_ambientLogCount < s_ambientLog.Length)
            s_ambientLog[s_ambientLogCount++] = (vx, vy, ch);
        s_ambientPulsesTotal++;
    }

    private static Color ScaleChannels(Color c, float scale) => new(
        (byte)Math.Clamp(c.R * scale, 0f, 255f),
        (byte)Math.Clamp(c.G * scale, 0f, 255f),
        (byte)Math.Clamp(c.B * scale, 0f, 255f));
}
