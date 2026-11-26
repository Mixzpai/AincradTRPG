namespace SAOTRPG.Map.Generation.Passes;

// A river runs from high ground down the land to the edge of the disk, the way water drains.
// Both algorithms follow the same drainage; DrunkardWalk adds a meander on top, DownslopeTrace
// runs straight down it. Without a heightmap, DrunkardWalk's old edge-to-edge walk is the
// fallback. Gated on width >= 80 (same as LakePass).
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

        if (ctx.Heights == null)
        {
            MapGenerator.GenerateRiver(ctx.Map, ctx.FloorNumber % 2 == 0, rng, ctx);
            return;
        }
        Drain(ctx, meander: ctx.Config.RiverAlgorithm == RiverAlgorithm.DrunkardWalk);
    }

    // How deep a depression on the river's course must be before it pools into a lake, and how
    // much the river may pool in all (a share of the map), so one wide basin cannot drown the
    // floor: a budget per pond let a long course pool again and again into one sheet (measured,
    // F44 Aquatic, 34% of a viewport).
    private const float PondDepth = 0.03f;
    private const double MaxPondShare = 0.0015;

    private static void Drain(WorldContext ctx, bool meander)
    {
        var heights = ctx.Heights!;
        int w = ctx.Width, h = ctx.Height;
        var (next, filled) = Drainage(ctx, heights);

        var (sx, sy) = PickSeedPeak(heights, w, h, ctx.Rng, ctx);
        int at = sy * w + sx;
        if (next[at] == Unreached)
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("RIVER",
                $"floor={ctx.FloorNumber} source ({sx},{sy}) is not on the drainage — no river");
            return;
        }

        int width = MapGenerator.RiverWidth(ctx);
        int pondBudget = Math.Max(16, (int)(w * h * MaxPondShare));
        var pooled = new bool[w * h];
        int wobble = 0, steps = 0;
        // The course is a tree path, so it ends at the rim; the cap only guards a malformed field.
        for (int guard = 0; at >= 0 && guard < w * h; guard++, steps++)
        {
            int cx = at % w, cy = at / w;
            if (pondBudget > 0 && filled[at] - heights[cx, cy] > PondDepth && !pooled[at])
                pondBudget -= Pool(ctx, heights, filled, at, pondBudget, pooled);

            // The meander: the old walk's drift, as an offset across the course that never strays
            // more than two cells from the valley floor.
            int ox = 0, oy = 0;
            if (meander)
            {
                int drift = ctx.Rng.Next(5);
                if (drift == 0) wobble = Math.Max(-2, wobble - 1);
                else if (drift == 1) wobble = Math.Min(2, wobble + 1);
                int n = next[at];
                bool alongX = n >= 0 && Math.Abs(n % w - cx) > 0;
                if (alongX) oy = wobble; else ox = wobble;
            }
            Brush(ctx, cx + ox, cy + oy, width);
            at = next[at];
        }
        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("RIVER",
            $"floor={ctx.FloorNumber} drained from ({sx},{sy}) over {steps} cells, meander={meander}");
    }

    // A width x width square on the course, so a wide river is wide whichever way it turns.
    private static void Brush(WorldContext ctx, int cx, int cy, int width)
    {
        int lo = -(width - 1) / 2, hi = width / 2;
        for (int dy = lo; dy <= hi; dy++)
        for (int dx = lo; dx <= hi; dx++)
        {
            int x = cx + dx, y = cy + dy;
            if (ctx.IsInsideCircle(x, y) && !ctx.IsInTownKeepOut(x, y)) MapGenerator.StampRiverTile(ctx.Map, x, y);
        }
    }

    // Fills the depression the course crosses, up to the level water would spill at: the cells
    // around it that sit below their filled height by the same pond. Capped, so a vast flat
    // basin becomes a lake rather than a sea. Returns the cells it took.
    private static int Pool(WorldContext ctx, float[,] heights, float[] filled, int start, int max, bool[] pooled)
    {
        int w = ctx.Width, h = ctx.Height;
        var queue = new Queue<int>();
        queue.Enqueue(start);
        pooled[start] = true;
        int count = 0;
        while (queue.Count > 0 && count < max)
        {
            int c = queue.Dequeue();
            int x = c % w, y = c / w;
            if (!ctx.IsInTownKeepOut(x, y)) MapGenerator.StampRiverTile(ctx.Map, x, y);
            count++;
            for (int k = 0; k < 4; k++)
            {
                int n = Neighbour(k, c, x, y, w, h);
                if (n < 0 || pooled[n]) continue;
                int nx = n % w, ny = n / w;
                if (!ctx.IsInsideCircle(nx, ny) || filled[n] - heights[nx, ny] <= PondDepth) continue;
                pooled[n] = true;
                queue.Enqueue(n);
            }
        }
        return count;
    }

    private const int Unreached = -2, Rim = -1;

    // For every in-disk cell, the neighbour its water leaves by. Priority-flood from the rim: the
    // lowest drained cell is taken next, and each neighbour it reaches drains into it at the
    // higher of the two heights, which fills a depression instead of trapping a trace in it.
    // A bucket queue over quantized heights keeps this linear (a 1000x1000 floor is ~785k cells).
    // FIRST IN, FIRST OUT within a bucket: a filled basin is flat, and a stack there grew the
    // drainage depth-first into snaking chains, so a river crossing a basin wandered all over it
    // (a 7,283-cell course on F44). A queue drains a flat by the shortest route from its spill.
    internal static (int[] Next, float[] Filled) Drainage(WorldContext ctx, float[,] heights)
    {
        int w = ctx.Width, h = ctx.Height, n = w * h;
        const int Buckets = 4096;
        var next = new int[n];
        var filled = new float[n];
        var head = new int[Buckets];
        var tail = new int[Buckets];
        var link = new int[n];
        Array.Fill(next, Unreached);
        Array.Fill(head, -1);
        Array.Fill(tail, -1);

        void Push(int c, float level)
        {
            int b = Math.Clamp((int)(level * (Buckets - 1)), 0, Buckets - 1);
            link[c] = -1;
            if (tail[b] < 0) head[b] = c; else link[tail[b]] = c;
            tail[b] = c;
        }

        // The rim: in-disk cells beside the disk's edge or the map's.
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!ctx.IsInsideCircle(x, y)) continue;
            bool rim = x == 0 || y == 0 || x == w - 1 || y == h - 1
                       || !ctx.IsInsideCircle(x - 1, y) || !ctx.IsInsideCircle(x + 1, y)
                       || !ctx.IsInsideCircle(x, y - 1) || !ctx.IsInsideCircle(x, y + 1);
            if (!rim) continue;
            int c = y * w + x;
            next[c] = Rim;
            filled[c] = heights[x, y];
            Push(c, filled[c]);
        }

        for (int b = 0; b < Buckets; b++)
        {
            while (head[b] >= 0)
            {
                int c = head[b];
                head[b] = link[c];
                if (head[b] < 0) tail[b] = -1;
                int x = c % w, y = c / w;
                for (int k = 0; k < 4; k++)
                {
                    int nb = Neighbour(k, c, x, y, w, h);
                    if (nb < 0 || next[nb] != Unreached) continue;
                    int nx = nb % w, ny = nb / w;
                    if (!ctx.IsInsideCircle(nx, ny)) continue;
                    next[nb] = c;
                    filled[nb] = MathF.Max(heights[nx, ny], filled[c]);
                    Push(nb, filled[nb]);
                }
            }
        }
        return (next, filled);
    }

    // The k-th of a cell's four neighbours, or -1 past the map's edge. Not an iterator: this runs
    // four times for every cell of a floor.
    private static int Neighbour(int k, int c, int x, int y, int w, int h) => k switch
    {
        0 => x > 0 ? c - 1 : -1,
        1 => x < w - 1 ? c + 1 : -1,
        2 => y > 0 ? c - w : -1,
        _ => y < h - 1 ? c + w : -1,
    };

    // Scans entire heightmap on stride-3 grid excluding a 10-tile edge buffer,
    // collects local maxima, weight-picks one of top-K (K=10) by rank (higher = more likely).
    private static (int X, int Y) PickSeedPeak(float[,] heights, int w, int h, Random rng, WorldContext ctx)
    {
        const int MARGIN = 10;
        var candidates = new List<(int X, int Y, float H)>();
        for (int x = MARGIN; x < w - MARGIN; x += 3)
        for (int y = MARGIN; y < h - MARGIN; y += 3)
        {
            if (!ctx.IsInsideCircle(x, y)) continue;
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
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("RIVER",
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
