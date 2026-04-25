namespace SAOTRPG.Map;

// Symmetric recursive shadowcasting (Albert Ford — albertford.com/shadowcasting/).
// Used for player FOV and per-light-source propagation.
public static class Shadowcaster
{
    public delegate bool BlockingPredicate(int x, int y);
    public delegate void VisibilityCallback(int x, int y);

    // Compute FOV from (ox, oy) up to a Euclidean distance of `radius`. Aspect-correction:
    // x-distance is scaled down by Generation.FloorMask.CellAspectRatio so the in-cell
    // ellipse renders as a visual circle on screen (cells are ~2x as tall as wide).
    public static void Compute(int ox, int oy, int radius,
        BlockingPredicate isBlocking, VisibilityCallback reveal)
    {
        reveal(ox, oy);
        int rSq = radius * radius;
        for (int cardinal = 0; cardinal < 4; cardinal++)
        {
            var first = new Row(1, -1.0, 1.0);
            Scan(ref first, cardinal, ox, oy, radius, rSq, isBlocking, reveal);
        }
    }

    // Mutable struct passed by ref — StartSlope is rewritten on wall→floor transition to narrow the sector.
    private struct Row
    {
        public int Depth;
        public double StartSlope;
        public double EndSlope;
        public Row(int depth, double start, double end) { Depth = depth; StartSlope = start; EndSlope = end; }
        public Row Next() => new(Depth + 1, StartSlope, EndSlope);
    }

    private static void Scan(ref Row row, int cardinal, int ox, int oy, int radius, int rSq,
        BlockingPredicate isBlocking, VisibilityCallback reveal)
    {
        if (row.Depth > radius) return;

        int minCol = RoundTiesUp(row.Depth * row.StartSlope);
        int maxCol = RoundTiesDown(row.Depth * row.EndSlope);

        bool? prevWasWall = null;

        for (int col = minCol; col <= maxCol; col++)
        {
            var (tx, ty) = Transform(cardinal, row.Depth, col, ox, oy);

            bool isWall = isBlocking(tx, ty);
            int dx = tx - ox, dy = ty - oy;
            // Aspect-corrected: shrink x-distance so screen-cells (taller than wide) form a visual circle.
            float adx = dx / Generation.FloorMask.CellAspectRatio;
            bool withinRadius = adx * adx + dy * dy <= rSq;

            if (withinRadius && (isWall || IsSymmetric(ref row, col)))
                reveal(tx, ty);

            if (prevWasWall == true && !isWall)
                row.StartSlope = Slope(row.Depth, col);

            if (prevWasWall == false && isWall)
            {
                var next = row.Next();
                next.EndSlope = Slope(row.Depth, col);
                Scan(ref next, cardinal, ox, oy, radius, rSq, isBlocking, reveal);
            }

            prevWasWall = isWall;
        }

        if (prevWasWall == false)
        {
            var next = row.Next();
            Scan(ref next, cardinal, ox, oy, radius, rSq, isBlocking, reveal);
        }
    }

    private static (int X, int Y) Transform(int cardinal, int row, int col, int ox, int oy)
        => cardinal switch
        {
            0 => (ox + col, oy - row), // N
            1 => (ox + row, oy + col), // E
            2 => (ox + col, oy + row), // S
            _ => (ox - row, oy + col), // W
        };

    private static double Slope(int depth, int col) => (2.0 * col - 1.0) / (2.0 * depth);

    private static bool IsSymmetric(ref Row row, int col)
        => col >= row.Depth * row.StartSlope
        && col <= row.Depth * row.EndSlope;

    // Banker rounding for column bounds: up = floor(n+0.5), down = ceil(n-0.5).
    private static int RoundTiesUp(double n)   => (int)Math.Floor(n + 0.5);
    private static int RoundTiesDown(double n) => (int)Math.Ceiling(n - 0.5);
}
