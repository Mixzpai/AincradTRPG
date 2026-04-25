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

        // Snapshot pre-lake walkable count so ConnectivityAuditPass can enforce
        // Brogue's 85%-post-lake-walkable reachability guard.
        int preLake = 0;
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var t = map.Tiles[x, y].Type;
            if (t != TileType.Wall && t != TileType.Mountain) preLake++;
        }
        ctx.PreLakeWalkableCount = preLake;

        int lakeCount = FloorScale.LakeCount(ctx.FloorNumber, rng);
        for (int i = 0; i < lakeCount; i++)
        {
            if (!TryPickLakeCenter(ctx, width, height, out int cx, out int cy)) continue;
            int radius = rng.Next(4, 7);
            MapGenerator.GenerateLake(map, cx, cy, radius, rng);
        }

        // Lake stamping is naturally clipped — protected/Mountain tiles outside the disk
        // resist water, and the GenerateLake helper skips IsProtectedFromWater tiles.

        // Bundle 5: Swamp lakes become BogWater. Ice biome water conversion is handled by IcePostWaterPass
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

    // Up to 8 retries for an in-disk center; lakes that fail all retries are skipped.
    private static bool TryPickLakeCenter(WorldContext ctx, int width, int height,
        out int cx, out int cy)
    {
        var rng = ctx.Rng;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            int x = rng.Next(20, Math.Max(21, width - 20));
            int y = rng.Next(15, Math.Max(16, height - 15));
            if (ctx.IsInsideCircle(x, y)) { cx = x; cy = y; return true; }
        }
        cx = cy = 0;
        return false;
    }
}
