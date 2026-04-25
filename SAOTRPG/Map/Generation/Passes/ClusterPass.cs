namespace SAOTRPG.Map.Generation.Passes;

// Tree/bush + rock clusters. Counts come from FloorScale scaled by biome
// density multipliers (TreeDensity, RockDensity, BushDensity).
public sealed class ClusterPass : IGenerationPass
{
    public string Name => "Cluster";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        var cfg = ctx.Config;
        int width = ctx.Width, height = ctx.Height;

        int treeClusters = (int)Math.Round(FloorScale.TreeClusters(ctx.FloorNumber, rng) * cfg.TreeDensity);
        for (int i = 0; i < treeClusters; i++)
        {
            int cx = rng.Next(8, Math.Max(9, width - 8));
            int cy = rng.Next(8, Math.Max(9, height - 8));
            int radius = rng.Next(3, 7);
            MapGenerator.PlaceCluster(map, cx, cy, radius, TileType.Tree, 0.55, rng);
            MapGenerator.PlaceCluster(map, cx, cy, radius + 1, TileType.Bush, 0.15 * Math.Max(0.25, cfg.BushDensity), rng);
        }

        int rockClusters = (int)Math.Round(FloorScale.RockClusters(ctx.FloorNumber, rng) * cfg.RockDensity);
        for (int i = 0; i < rockClusters; i++)
        {
            int cx = rng.Next(10, Math.Max(11, width - 10));
            int cy = rng.Next(10, Math.Max(11, height - 10));
            int radius = rng.Next(2, 4);
            MapGenerator.PlaceCluster(map, cx, cy, radius, TileType.Rock, 0.3, rng);
        }
    }
}
