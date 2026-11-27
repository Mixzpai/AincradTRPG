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

        int treeClusters = (int)Math.Round(
            FloorScale.TreeClusters(ctx.FloorNumber, rng) * cfg.TreeDensity * ClusterCountScale);
        for (int i = 0; i < treeClusters; i++)
        {
            if (!TryPickInsideDisk(ctx, 8, 8, HeightBias.High, out int cx, out int cy)) continue;
            int radius = rng.Next(3, 7);
            MapGenerator.PlaceCluster(map, cx, cy, radius, TileType.Tree, 0.55, rng);
            MapGenerator.PlaceCluster(map, cx, cy, radius + 1, TileType.Bush, BushRingFill * cfg.BushDensity, rng);
        }

        int rockClusters = (int)Math.Round(
            FloorScale.RockClusters(ctx.FloorNumber, rng) * cfg.RockDensity * ClusterCountScale);
        for (int i = 0; i < rockClusters; i++)
        {
            if (!TryPickInsideDisk(ctx, 10, 10, HeightBias.Low, out int cx, out int cy)) continue;
            int radius = rng.Next(2, 4);
            MapGenerator.PlaceCluster(map, cx, cy, radius, TileType.Rock, 0.3, rng);
        }
    }

    // Cluster count is cut by one shared factor rather than by re-tuning each biome's densities.
    // A uniform multiplier leaves every declared TreeDensity / RockDensity ratio intact, so the
    // per-biome spread stays the biome's own statement; and cutting the count while keeping the
    // fill gives fewer, denser copses with more open ground between them, where thinning the fill
    // instead would give the same number of ragged half-copses.
    private const double ClusterCountScale = 0.75;

    // Undergrowth fill for the ring one tile outside each tree cluster. Bushes are the densest
    // decoration on the map because the ring is drawn around every cluster. The fill scales with
    // BushDensity with no lower clamp, so a biome that says it has almost no undergrowth gets
    // almost none.
    private const double BushRingFill = 0.09;

    // Which end of the heightmap a cluster prefers. Trees crown the high ground, rocks collect in
    // the low — so a floor reads as a landscape rather than scatter, using the heightmap already
    // computed for terrain.
    private enum HeightBias { None, High, Low }

    // Eight candidates, and the best valid one wins rather than the first. That is what turns a
    // uniform scatter into a bias without ever failing to place a cluster that had a valid spot —
    // a reject-and-retry loop would have to give up somewhere, thinning clusters on floors whose
    // terrain does not suit the preference.
    //
    // Under a bias every candidate costs its two draws whether or not it is chosen, so the draw
    // count is a constant 16 per cluster.
    private static bool TryPickInsideDisk(WorldContext ctx, int marginX, int marginY,
        HeightBias bias, out int cx, out int cy)
    {
        var rng = ctx.Rng;
        var heights = ctx.Heights;
        bool found = false;
        int bestX = 0, bestY = 0;
        float bestScore = float.NegativeInfinity;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            int x = rng.Next(marginX, Math.Max(marginX + 1, ctx.Width - marginX));
            int y = rng.Next(marginY, Math.Max(marginY + 1, ctx.Height - marginY));
            if (!ctx.IsInsideCircle(x, y) || ctx.IsInTownKeepOut(x, y)) continue;

            // No heightmap (towns, F100) or no preference: first valid wins.
            if (heights == null || bias == HeightBias.None) { cx = x; cy = y; return true; }

            float score = bias == HeightBias.High ? heights[x, y] : -heights[x, y];
            if (!found || score > bestScore) { found = true; bestX = x; bestY = y; bestScore = score; }
        }

        cx = bestX; cy = bestY;
        return found;
    }
}
