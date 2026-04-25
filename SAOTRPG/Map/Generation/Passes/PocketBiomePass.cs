using SAOTRPG.Systems;
using SAOTRPG.ThirdParty;

namespace SAOTRPG.Map.Generation.Passes;

// Stamps 1-2 climatically-close sub-biome pockets onto band-edge floors only.
// Consumed by BaseTerrainPass via ctx.PocketBiomeMap lookup. Interior band floors skipped.
public sealed class PocketBiomePass : IGenerationPass
{
    public string Name => "PocketBiome";

    // F1 is band-start (not band-edge); F100 bypasses the pipeline entirely via
    // GenerateRubyPalace. Otherwise run only when the next floor's biome differs.
    public bool ShouldRun(WorldContext ctx)
    {
        if (ctx.FloorNumber <= 1 || ctx.FloorNumber >= 100) return false;
        if (ctx.Config.PocketCandidates == null || ctx.Config.PocketCandidates.Length == 0) return false;
        return IsBandEdgeFloor(ctx.FloorNumber);
    }

    // Band-edge iff the next floor's biome differs from this floor's biome.
    public static bool IsBandEdgeFloor(int floor)
    {
        if (floor < 1 || floor >= 100) return false;
        return BiomeSystem.GetBiome(floor) != BiomeSystem.GetBiome(floor + 1);
    }

    public void Execute(WorldContext ctx)
    {
        int w = ctx.Width, h = ctx.Height;
        ctx.PocketBiomeMap = new BiomeType?[w, h];

        // 1-2 pockets per band-edge floor.
        int pocketCount = 1 + ctx.Rng.Next(2);
        var picks = PickPocketBiomes(ctx, pocketCount);
        if (picks.Count == 0) { ctx.PocketBiomeMap = null; return; }

        // Random interior Voronoi seeds; margin keeps pockets off the mountain border
        // and SpecialAreaPass town/boss overlays (which run later at position 12) will
        // stamp over any pocket tiles that collide with safe zones.
        int margin = 12;
        if (w <= margin * 2 + 4 || h <= margin * 2 + 4)
        {
            ctx.PocketBiomeMap = null;
            return;
        }

        var seeds = new List<(int X, int Y, BiomeType B, int Radius)>(picks.Count);
        for (int i = 0; i < picks.Count; i++)
        {
            int sx = ctx.Rng.Next(margin, w - margin);
            int sy = ctx.Rng.Next(margin, h - margin);
            // 15-25 tile radius — diameter ~30-50 tiles, sized for 50x28..100x80 floors.
            int r = 15 + ctx.Rng.Next(11);
            seeds.Add((sx, sy, picks[i], r));
        }

        // Cheap Worley via FastNoiseLite.Cellular; also reused as wobble source for
        // seed-distance domain warp so pocket edges aren't perfect circles.
        var wobble = new FastNoiseLite(ctx.GlobalSeed ^ 0x0D0C0C0E);
        wobble.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        wobble.SetFrequency(0.07f);

        int claimed = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float ox = x + wobble.GetNoise(x, y) * 4.0f;
            float oy = y + wobble.GetNoise(x + 1000, y + 1000) * 4.0f;
            foreach (var seed in seeds)
            {
                float dx = ox - seed.X, dy = oy - seed.Y;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d <= seed.Radius)
                {
                    ctx.PocketBiomeMap[x, y] = seed.B;
                    claimed++;
                    break;
                }
            }
        }

        UI.DebugLogger.LogGame("POCKET",
            $"floor={ctx.FloorNumber} host={ctx.Biome} pockets={picks.Count} " +
            $"biomes=[{string.Join(",", picks)}] tiles={claimed}");
    }

    // Chooses up to N distinct pocket biomes from Config.PocketCandidates.
    // Parses string → BiomeType; silently drops unknown names.
    private static List<BiomeType> PickPocketBiomes(WorldContext ctx, int n)
    {
        var pool = new List<BiomeType>();
        foreach (var name in ctx.Config.PocketCandidates)
        {
            if (Enum.TryParse<BiomeType>(name, ignoreCase: true, out var b) && b != ctx.Biome)
                pool.Add(b);
        }
        if (pool.Count == 0) return new();
        // Fisher-Yates shuffle then take first n.
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = ctx.Rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int take = Math.Min(n, pool.Count);
        return pool.GetRange(0, take);
    }
}
