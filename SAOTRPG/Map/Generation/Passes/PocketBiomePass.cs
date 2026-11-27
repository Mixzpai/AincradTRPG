using SAOTRPG.Systems.World;
using SAOTRPG.ThirdParty;

namespace SAOTRPG.Map.Generation.Passes;

// Stamps 1-2 climatically-close sub-biome pockets: always on a band-edge floor, and on a stable
// fraction of the interior ones. Consumed by BaseTerrainPass via ctx.PocketBiomeMap lookup.
//
// The tile that pass stamps is the pocket's only expression. The biome tint, the ambient
// particles, the tree/rock/bush densities, the lakes, the rivers, the feature quotas, the trap
// pools, the ore table and the entry text all read the host config, and GameMap carries no
// per-cell biome — so a pocket is a palette, not a region.
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
        // Decided by a stable hash of (seed, floor), never ctx.Rng: ShouldRun has to be pure. A
        // draw here would consume from the stream on floors where the pass then does not run,
        // shifting every later pass by whether a pass was skipped.
        return (uint)SAOTRPG.Systems.RunState.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "pocket-midband")
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

        // SpecialAreaPass town/boss overlays (which run later) stamp over any
        // pocket tiles that collide with safe zones, so a pocket never has to avoid them here.
        int diskRadius = Math.Min(w, h) / 2;
        if (diskRadius < MinPocketRadius * 3)
        {
            ctx.PocketBiomeMap = null;
            return;
        }

        int baseR = Math.Max(MinPocketRadius, (int)(diskRadius * PocketRadiusDiskFraction));
        var seeds = new List<(int X, int Y, BiomeType B, int Radius)>(picks.Count);
        for (int i = 0; i < picks.Count; i++)
        {
            int r = baseR + ctx.Rng.Next(baseR / 4 + 1);
            var (sx, sy) = PickSeed(ctx, r, w, h);
            seeds.Add((sx, sy, picks[i], r));
        }

        // Domain warp for the seed-distance test, so a pocket edge is not a perfect circle. The
        // deformation is small against the radius, so this softens an edge rather than shaping
        // one. Seeded per floor, so each floor's pocket edges deform differently.
        var wobble = new FastNoiseLite(
            SAOTRPG.Systems.RunState.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "pocket-wobble"));
        wobble.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        wobble.SetFrequency(0.07f);

        // Parallel across rows. Every RNG draw for this pass happened above (pocket count, seed
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

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("POCKET",
            $"floor={ctx.FloorNumber} host={ctx.Biome} pockets={picks.Count} " +
            $"biomes=[{string.Join(",", picks)}] tiles={claimed}");
    }

    // Below this the fork costs more than the work; the high floors are 100x100.
    private const int ParallelCellThreshold = 65536;

    // A pocket's radius is a share of the floor's own disk radius and nothing else: an absolute
    // ceiling would switch the scaling off on the largest disks, the floors it matters most for.
    //
    // The share is deliberately modest. Feature and entity placement is gated on
    // `MapGenerator.IsGrassType`, which accepts none of the biome-native ground tiles (Sand,
    // DuneSand, Snow, Ice, Basalt, Ash, Mud, Reeds), so a pocket whose biome stamps those is a
    // placement dead zone and a grass pocket on a floor made of them is a placement magnet.
    // Larger pockets would redistribute the floor's content, not just its terrain.
    private const double PocketRadiusDiskFraction = 0.163;
    private const int MinPocketRadius = 8;

    // Candidate seed positions tried per pocket. Every candidate costs its two draws whether or
    // not it is taken, so the stream position for every later pass is a constant.
    private const int SeedCandidates = 8;

    // The playable area is the inscribed disk, not the square that contains it: a quarter of that
    // square is outside the disk outright, and a seed near the rim loses most of its pocket to the
    // mountain ring.
    //
    // Candidates are tested against the real mask rather than a reconstructed circle, because
    // FloorMask wobbles the radius by up to 7 tiles and a rebuilt circle disagrees with the map.
    // A candidate wins when it and its four half-radius axis probes are all inside, which bounds
    // how much of the pocket the ring can eat without pinning every pocket to the middle. The
    // first acceptable candidate wins rather than the best, so the result stays uniform over the
    // eroded disk instead of collecting at its centre.
    private static (int X, int Y) PickSeed(WorldContext ctx, int r, int w, int h)
    {
        int probe = Math.Max(1, r / 2);
        int fallbackX = w / 2, fallbackY = h / 2;
        bool haveFallback = false;
        int bestX = fallbackX, bestY = fallbackY;
        bool found = false;
        for (int i = 0; i < SeedCandidates; i++)
        {
            int x = ctx.Rng.Next(w), y = ctx.Rng.Next(h);
            if (found) continue;
            if (!ctx.IsInsideCircle(x, y)) continue;
            if (!haveFallback) { fallbackX = x; fallbackY = y; haveFallback = true; }
            if (!ctx.IsInsideCircle(x - probe, y) || !ctx.IsInsideCircle(x + probe, y)
                || !ctx.IsInsideCircle(x, y - probe) || !ctx.IsInsideCircle(x, y + probe)) continue;
            bestX = x; bestY = y; found = true;
        }
        return found ? (bestX, bestY) : (fallbackX, fallbackY);
    }

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
