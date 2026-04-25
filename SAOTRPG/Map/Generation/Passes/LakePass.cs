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
            int cx = rng.Next(20, Math.Max(21, width - 20));
            int cy = rng.Next(15, Math.Max(16, height - 15));
            int radius = rng.Next(4, 7);
            MapGenerator.GenerateLake(map, cx, cy, radius, rng);
        }

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
}
