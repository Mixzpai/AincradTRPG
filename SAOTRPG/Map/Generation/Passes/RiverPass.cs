namespace SAOTRPG.Map.Generation.Passes;

// Dispatches on biome RiverAlgorithm: DrunkardWalk = meander; DownslopeTrace = heightmap
// local-max → nearest edge; None = pass skipped. Gated on width >= 80 (same as LakePass).
public sealed class RiverPass : IGenerationPass
{
    public string Name => "River";
    public bool ShouldRun(WorldContext ctx) =>
        ctx.Config.RiverAlgorithm != RiverAlgorithm.None && ctx.Width >= 80;

    public void Execute(WorldContext ctx)
    {
        var rng = ctx.Rng;
        // Spawn chance gate — biome-tuned, so dry biomes rarely roll rivers.
        if (rng.NextDouble() > ctx.Config.RiverSpawnChance) return;

        switch (ctx.Config.RiverAlgorithm)
        {
            case RiverAlgorithm.DrunkardWalk:
                MapGenerator.GenerateRiver(ctx.Map, ctx.FloorNumber % 2 == 0, rng);
                break;
            case RiverAlgorithm.DownslopeTrace:
                DownslopeTrace(ctx);
                break;
        }
    }

    // Seeds from a high cell, walks down-gradient to the edge, and stamps water
    // through the existing river-tile helper so protected tiles survive.
    private static void DownslopeTrace(WorldContext ctx)
    {
        var map = ctx.Map;
        var heights = ctx.Heights;
        if (heights == null) { MapGenerator.GenerateRiver(map, ctx.FloorNumber % 2 == 0, ctx.Rng); return; }
        int w = ctx.Width, h = ctx.Height;

        // Full-map top-K local-maxima weighted pick, with edge-margin exclusion
        // so rivers don't start adjacent to the border and die in one step.
        var (sx, sy) = PickSeedPeak(heights, w, h, ctx.Rng);

        int cx = sx, cy = sy;
        for (int step = 0; step < w + h; step++)
        {
            MapGenerator.StampRiverTile(map, cx, cy);
            MapGenerator.StampRiverTile(map, cx + 1, cy);

            if (cx <= 2 || cx >= w - 3 || cy <= 2 || cy >= h - 3) break;

            // Gradient descent with a tiny rng nudge to avoid straight lines.
            float hHere = heights[cx, cy];
            int bestDx = 0, bestDy = 1;
            float bestH = heights[cx, cy + 1];
            int[,] dirs = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dirs[d, 0], ny = cy + dirs[d, 1];
                if (nx < 1 || ny < 1 || nx >= w - 1 || ny >= h - 1) continue;
                float hn = heights[nx, ny];
                if (hn < bestH) { bestH = hn; bestDx = dirs[d, 0]; bestDy = dirs[d, 1]; }
            }
            // If we've hit a local minimum, nudge randomly to escape.
            if (bestH >= hHere)
            {
                bestDx = ctx.Rng.Next(-1, 2);
                bestDy = ctx.Rng.Next(-1, 2);
                if (bestDx == 0 && bestDy == 0) bestDy = 1;
            }
            cx += bestDx;
            cy += bestDy;
        }
    }

    // Scans entire heightmap on stride-3 grid excluding a 10-tile edge buffer,
    // collects local maxima, weight-picks one of top-K (K=10) by rank (higher = more likely).
    private static (int X, int Y) PickSeedPeak(float[,] heights, int w, int h, Random rng)
    {
        const int MARGIN = 10;
        var candidates = new List<(int X, int Y, float H)>();
        for (int x = MARGIN; x < w - MARGIN; x += 3)
        for (int y = MARGIN; y < h - MARGIN; y += 3)
        {
            float hv = heights[x, y];
            bool local = true;
            for (int dx = -1; dx <= 1 && local; dx++)
            for (int dy = -1; dy <= 1 && local; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx * 3, ny = y + dy * 3;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (heights[nx, ny] > hv) local = false;
            }
            if (local) candidates.Add((x, y, hv));
        }
        // No candidates met the edge-buffer criterion — fall back to map centre with a warning.
        if (candidates.Count == 0)
        {
            UI.DebugLogger.LogGame("RIVER",
                $"DownslopeTrace no peak candidates in {w}x{h} (margin={MARGIN}) — seeding centre");
            return (w / 2, h / 2);
        }
        candidates.Sort((a, b) => b.H.CompareTo(a.H));
        int topK = Math.Min(10, candidates.Count);
        // Rank-weighted pick: weights are (topK, topK-1, ..., 1), total = topK*(topK+1)/2.
        int totalW = topK * (topK + 1) / 2;
        int r = rng.Next(totalW);
        int acc = 0;
        for (int i = 0; i < topK; i++)
        {
            acc += (topK - i);
            if (r < acc) return (candidates[i].X, candidates[i].Y);
        }
        return (candidates[0].X, candidates[0].Y);
    }
}
