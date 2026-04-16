namespace SAOTRPG.Map;

// Symmetric recursive shadowcasting — Albert Ford's algorithm
// (https://www.albertford.com/shadowcasting/). Properties it guarantees:
// Used for both the player's FOV and per-light-source propagation.
public static class Shadowcaster
{
    public delegate bool BlockingPredicate(int x, int y);
    public delegate void VisibilityCallback(int x, int y);

    // Compute FOV from (ox, oy) up to a Euclidean distance of .
    // Invokes  for the origin and every tile proven visible.
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

    // Mutable struct passed by ref — StartSlope is rewritten when a wall→floor
    // transition happens inside the loop, narrowing the visible sector for
    // the rest of that row and the recursive sub-scans.
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
            bool withinRadius = dx * dx + dy * dy <= rSq;

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

    // Banker-style rounding variants used by Albert Ford to bound columns:
    //   round_ties_up(n)   = floor(n + 0.5)
    //   round_ties_down(n) = ceil(n - 0.5)
    private static int RoundTiesUp(double n)   => (int)Math.Floor(n + 0.5);
    private static int RoundTiesDown(double n) => (int)Math.Ceiling(n - 0.5);
}
