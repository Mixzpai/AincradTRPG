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
            if (!TryPickInsideDisk(ctx, 8, 8, out int cx, out int cy)) continue;
            int radius = rng.Next(3, 7);
            MapGenerator.PlaceCluster(map, cx, cy, radius, TileType.Tree, 0.55, rng);
            MapGenerator.PlaceCluster(map, cx, cy, radius + 1, TileType.Bush, 0.15 * Math.Max(0.25, cfg.BushDensity), rng);
        }

        int rockClusters = (int)Math.Round(FloorScale.RockClusters(ctx.FloorNumber, rng) * cfg.RockDensity);
        for (int i = 0; i < rockClusters; i++)
        {
            if (!TryPickInsideDisk(ctx, 10, 10, out int cx, out int cy)) continue;
            int radius = rng.Next(2, 4);
            MapGenerator.PlaceCluster(map, cx, cy, radius, TileType.Rock, 0.3, rng);
        }
    }

    // Up to 8 retries; cluster spread is naturally clipped because PlaceCluster
    // only writes to interior tiles (Mountain stamps stay put outside the disk).
    private static bool TryPickInsideDisk(WorldContext ctx, int marginX, int marginY,
        out int cx, out int cy)
    {
        var rng = ctx.Rng;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            int x = rng.Next(marginX, Math.Max(marginX + 1, ctx.Width - marginX));
            int y = rng.Next(marginY, Math.Max(marginY + 1, ctx.Height - marginY));
            if (ctx.IsInsideCircle(x, y) && !ctx.IsInTownKeepOut(x, y)) { cx = x; cy = y; return true; }
        }
        cx = cy = 0;
        return false;
    }
}
