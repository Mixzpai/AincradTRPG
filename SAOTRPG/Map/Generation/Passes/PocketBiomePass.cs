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
    // Mid-band floors get pockets sometimes, so a long run through one biome — Forest spans 17
    // floors — is not that many near-identical maps. Band edges keep getting them every time,
    // which is what makes a biome transition read as a blend rather than a switch.
    private const int MidBandChancePct = 35;

    public bool ShouldRun(WorldContext ctx)
    {
        if (ctx.FloorNumber <= 1 || ctx.FloorNumber >= 100) return false;
        if (ctx.Config.PocketCandidates == null || ctx.Config.PocketCandidates.Length == 0) return false;
        if (IsBandEdgeFloor(ctx.FloorNumber)) return true;
        // Decided by a STABLE hash of (seed, floor), never ctx.Rng: ShouldRun has to be pure. A
        // draw here would consume from the stream on floors where the pass then does not run,
        // shifting every later pass by whether a pass was skipped.
        return (uint)Systems.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "pocket-midband")
               % 100 < MidBandChancePct;
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
            // Radius scales with the floor's disk. It used to be a flat 15-25 with a comment
            // reading "sized for 50x28..100x80 floors" — floors are 1000x1000 down to 100x100 now,
            // so that was a speck on an early floor and half the disk on a late one. A fifth of the
            // disk radius keeps a pocket recognisable at both ends. Still exactly ONE draw, so the
            // stream position for every later pass is unchanged.
            int diskRadius = Math.Min(w, h) / 2;
            int baseR = Math.Clamp(diskRadius / 5, 8, 60);
            int r = baseR + ctx.Rng.Next(baseR / 4 + 1);
            seeds.Add((sx, sy, picks[i], r));
        }

        // Cheap Worley via FastNoiseLite.Cellular; also reused as wobble source for
        // seed-distance domain warp so pocket edges aren't perfect circles.
        // Was GlobalSeed ^ a constant, which carries no floor term — every floor's pockets shared
        // one wobble field, so their edges deformed identically. Invisible while only band-edge
        // floors had pockets; obvious once most floors do.
        var wobble = new FastNoiseLite(
            Systems.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "pocket-wobble"));
        wobble.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        wobble.SetFrequency(0.07f);

        // Parallel across rows. Every RNG draw for this pass happened ABOVE (pocket count, seed
        // positions, radii, the shuffle), so the loop body is a pure function of (x, y) against a
        // fixed seed list and each cell writes its own slot — output is identical whatever order
        // rows complete in. `claimed` only feeds the debug line, so it accumulates thread-locally
        // and is summed at the end rather than contended per cell.
        var pocketMap = ctx.PocketBiomeMap!;
        int claimed = 0;
        if ((long)w * h < ParallelCellThreshold)
        {
            for (int y = 0; y < h; y++)
                claimed += ClaimRow(pocketMap, seeds, wobble, y, w);
        }
        else
        {
            System.Threading.Tasks.Parallel.For(0, h, () => 0,
                (y, _, local) => local + ClaimRow(pocketMap, seeds, wobble, y, w),
                local => System.Threading.Interlocked.Add(ref claimed, local));
        }

        UI.DebugLogger.LogGame("POCKET",
            $"floor={ctx.FloorNumber} host={ctx.Biome} pockets={picks.Count} " +
            $"biomes=[{string.Join(",", picks)}] tiles={claimed}");
    }

    // Below this the fork costs more than the work; the high floors are 100x100.
    private const int ParallelCellThreshold = 65536;

    private static int ClaimRow(BiomeType?[,] pocketMap, List<(int X, int Y, BiomeType B, int Radius)> seeds,
        FastNoiseLite wobble, int y, int w)
    {
        int claimed = 0;
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
                    pocketMap[x, y] = seed.B;
                    claimed++;
                    break;
                }
            }
        }
        return claimed;
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
