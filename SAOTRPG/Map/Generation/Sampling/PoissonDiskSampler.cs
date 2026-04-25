namespace SAOTRPG.Map.Generation.Sampling;

// Bridson's grid-accelerated Poisson-disk sampler. Returns points with a
// guaranteed minimum pairwise distance. O(n) via spatial hashing.
public static class PoissonDiskSampler
{
    // minRadius = min spacing between samples; k = attempts per active-list pop (Bridson default 30).
    // acceptTile = optional predicate (e.g. "only walkable tiles").
    public static List<(int X, int Y)> Sample(
        int width, int height, float minRadius, Random rng, int k = 30,
        Func<int, int, bool>? acceptTile = null)
    {
        if (width <= 0 || height <= 0 || minRadius <= 0f)
            return new List<(int, int)>();

        float cellSize = minRadius / MathF.Sqrt(2f);
        int gw = Math.Max(1, (int)MathF.Ceiling(width / cellSize));
        int gh = Math.Max(1, (int)MathF.Ceiling(height / cellSize));
        var grid = new (int X, int Y)?[gw, gh];
        var samples = new List<(int X, int Y)>();
        var active = new List<(int X, int Y)>();

        // Seed: find first accepted starting tile within a bounded retry window.
        int sx = rng.Next(width), sy = rng.Next(height);
        int safety = 0;
        while (acceptTile != null && !acceptTile(sx, sy) && safety++ < 200)
        {
            sx = rng.Next(width);
            sy = rng.Next(height);
        }
        if (acceptTile != null && !acceptTile(sx, sy))
            return samples; // no valid seed found

        var seed = (sx, sy);
        samples.Add(seed);
        active.Add(seed);
        grid[(int)(sx / cellSize), (int)(sy / cellSize)] = seed;

        while (active.Count > 0)
        {
            int idx = rng.Next(active.Count);
            var center = active[idx];
            bool found = false;
            for (int i = 0; i < k; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.Tau);
                float radius = minRadius * (1f + (float)rng.NextDouble());
                int nx = (int)(center.X + radius * MathF.Cos(angle));
                int ny = (int)(center.Y + radius * MathF.Sin(angle));
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (acceptTile != null && !acceptTile(nx, ny)) continue;
                if (!NeighborhoodClear(grid, gw, gh, cellSize, nx, ny, minRadius)) continue;

                var pt = (nx, ny);
                samples.Add(pt);
                active.Add(pt);
                grid[(int)(nx / cellSize), (int)(ny / cellSize)] = pt;
                found = true;
                break;
            }
            if (!found) active.RemoveAt(idx);
        }
        return samples;
    }

    // 5x5 cell window is sufficient because cellSize = minRadius/sqrt(2) guarantees
    // at most one point per cell and any conflicting point lies within 2 cells.
    private static bool NeighborhoodClear((int X, int Y)?[,] grid, int gw, int gh,
        float cellSize, int px, int py, float minRadius)
    {
        int gx = (int)(px / cellSize), gy = (int)(py / cellSize);
        float r2 = minRadius * minRadius;
        for (int dx = -2; dx <= 2; dx++)
        for (int dy = -2; dy <= 2; dy++)
        {
            int ncx = gx + dx, ncy = gy + dy;
            if (ncx < 0 || ncx >= gw || ncy < 0 || ncy >= gh) continue;
            var cell = grid[ncx, ncy];
            if (cell == null) continue;
            float ddx = cell.Value.X - px, ddy = cell.Value.Y - py;
            if (ddx * ddx + ddy * ddy < r2) return false;
        }
        return true;
    }
}
