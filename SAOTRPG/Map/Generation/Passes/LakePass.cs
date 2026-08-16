using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation.Passes;

// Organic CA lakes. Gated on biome WaterLakesEnabled + width >= 80 (small
// floors would drown under a single lake).
public sealed class LakePass : IGenerationPass
{
    public string Name => "Lake";
    public bool ShouldRun(WorldContext ctx) => ctx.Config.WaterLakesEnabled && ctx.Width >= 80;
    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        int width = ctx.Width, height = ctx.Height;

        var (seedPct, caPasses) = LakeShape(ctx);

        int lakeCount = FloorScale.LakeCount(ctx.FloorNumber, rng);
        for (int i = 0; i < lakeCount; i++)
        {
            if (!TryPickLakeCenter(ctx, width, height, out int cx, out int cy)) continue;
            int radius = rng.Next(4, 7);
            MapGenerator.GenerateLake(map, cx, cy, radius, rng, seedPct, caPasses);
        }

        // Lake stamping is naturally clipped — protected/Mountain tiles outside the disk
        // resist water, and the GenerateLake helper skips IsProtectedFromWater tiles.

        // Swamp lakes become BogWater. Ice biome water conversion is handled by IcePostWaterPass
        // (needs edge detection of Snow/Ice neighbors, so it runs as a separate pass).
        if (ctx.Biome == BiomeType.Swamp)
        {
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var t = map.Tiles[x, y].Type;
                if (t == TileType.Water || t == TileType.WaterDeep)
                    map.Tiles[x, y].Type = TileType.BogWater;
            }
        }
    }

    // The biome's lake-shape pair. This pass only runs when WaterLakesEnabled, so a seed density of
    // 0 here means "lakes on, but every lake empty" — a contradictory config, clamped and logged
    // rather than silently generating nothing. 0 smoothing passes is legal, if speckly: it is the
    // raw noise field, which is a look a biome may legitimately want.
    private static (int SeedPct, int CaPasses) LakeShape(WorldContext ctx)
    {
        int seedPct = ctx.Config.WaterSeedPct;
        int caPasses = ctx.Config.WaterCAPasses;
        if (seedPct < 1 || seedPct > 99)
        {
            int clamped = Math.Clamp(seedPct, 1, 99);
            UI.DebugLogger.LogGame("LAKE",
                $"biome={ctx.Biome} has waterLakesEnabled but waterSeedPct={seedPct} — clamped to {clamped}");
            seedPct = clamped;
        }
        if (caPasses < 0)
        {
            UI.DebugLogger.LogGame("LAKE",
                $"biome={ctx.Biome} waterCAPasses={caPasses} is negative — clamped to 0");
            caPasses = 0;
        }
        return (seedPct, caPasses);
    }

    // Up to 8 retries for an in-disk center; lakes that fail all retries are skipped.
    private static bool TryPickLakeCenter(WorldContext ctx, int width, int height,
        out int cx, out int cy)
    {
        var rng = ctx.Rng;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            int x = rng.Next(20, Math.Max(21, width - 20));
            int y = rng.Next(15, Math.Max(16, height - 15));
            if (ctx.IsInsideCircle(x, y) && !ctx.IsInTownKeepOut(x, y)) { cx = x; cy = y; return true; }
        }
        cx = cy = 0;
        return false;
    }
}
