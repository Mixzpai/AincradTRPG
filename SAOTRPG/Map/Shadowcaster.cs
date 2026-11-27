using System.Runtime.CompilerServices;

namespace SAOTRPG.Map;

// Symmetric recursive shadowcasting.
// Used for player FOV and per-light-source propagation.
//
// This is the hottest loop on the turn path: a player FOV sweep at the runtime radius visits tens
// of thousands of cells, and it runs on every keystroke. Four things shape the code:
//
//   * The sight-blocking table is read directly as a span rather than through a predicate
//     delegate, and the sink is a generic struct rather than a callback delegate, so a visited
//     cell costs no indirect call.
//   * A row is split into an off-map prefix, the on-map middle and an off-map suffix, so the
//     bounds test does not run per cell.
//   * The middle is walked as runs of same-blocking cells. Nothing inside a run can change the
//     sector, so only a run's first cell needs the full per-cell body and the rest is one
//     contiguous reveal range; most reveals are run tails.
//   * An open row's continuation at the next depth — the tail call of the recursive form — is a
//     loop, and everything a cardinal holds constant travels in one context struct, so a child
//     row is a five-argument call instead of twelve and the open-ground recursion does not go
//     `radius` frames deep per cardinal.
//
// The reveal set and the reveal order are both part of the contract: the order matters because
// GameMap stamps a last-seen turn and appends to a visible-cell list as it goes.
//
// A run's tail is handed to the sink as one call (IFovRunSink), so the sink can hoist the row
// index, fill spans and skip explored stretches. A per-cell sink still sees the same sequence
// through an adapter.
public static class Shadowcaster
{
    // A struct sink lets the JIT devirtualize and inline the per-cell reveal. Implementations
    // may assume (x, y) is on the map: the sweep drops off-map cells itself.
    public interface IFovSink { void Reveal(int x, int y); }

    // A sink that takes a run's tail whole. RevealRow(y, x0, x1) stands for Reveal(x0, y),
    // Reveal(x0 + 1, y) ... Reveal(x1, y) in that order, RevealColumn(x, y0, y1) for the same
    // along y; both ranges are inclusive and never empty. A run's first cell still arrives
    // through Reveal, because a child sector may be swept between it and the tail.
    public interface IFovRunSink : IFovSink
    {
        void RevealRow(int y, int x0, int x1);
        void RevealColumn(int x, int y0, int y1);
    }

    // Precomputed so the inner loop multiplies instead of dividing. Derived from the constant, so
    // changing CellAspectRatio still works — it just stops being bit-exact for values whose square
    // is not a power of two, which is a rounding difference at the radius boundary only.
    private static readonly float InvAspectSq =
        1f / (Generation.FloorMask.CellAspectRatio * Generation.FloorMask.CellAspectRatio);

    // Above this the per-depth column bounds go on the heap instead of the stack. Nothing in the
    // game asks for a radius near it (player FOV is ~190, a light source at most 16), so the heap
    // arm exists only so a large radius cannot blow the stack.
    private const int MaxStackRadius = 1024;

    // Compute FOV from (ox, oy) up to a Euclidean distance of `radius`, reading sight-blocking
    // from a row-major (y * width + x) table where true blocks. Off the map blocks, matching the
    // sentinel Wall a map returns there. Aspect-correction: x-distance is scaled down by
    // Generation.FloorMask.CellAspectRatio so the in-cell ellipse renders as a visual circle on
    // screen (cells are ~2x as tall as wide).
    public static void Compute<TSink>(int ox, int oy, int radius,
        ReadOnlySpan<bool> opaque, int width, int height, ref TSink sink)
        where TSink : struct, IFovSink
    {
        // The sink travels by value through the adapter and is copied back, so the caller's
        // struct holds the sweep's writes exactly as a direct ref would have.
        var runs = new PerCellRuns<TSink> { Sink = sink };
        ComputeRuns(ox, oy, radius, opaque, width, height, ref runs);
        sink = runs.Sink;
    }

    // Replays a run's tail one cell at a time, ascending, so a per-cell sink sees the sequence
    // the per-cell sweep produced. A default interface method would do the same job but boxes
    // a struct on every call and loses its writes.
    private struct PerCellRuns<TSink> : IFovRunSink where TSink : struct, IFovSink
    {
        public TSink Sink;
        public void Reveal(int x, int y) => Sink.Reveal(x, y);
        public void RevealRow(int y, int x0, int x1)
        {
            for (int x = x0; x <= x1; x++) Sink.Reveal(x, y);
        }
        public void RevealColumn(int x, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++) Sink.Reveal(x, y);
        }
    }

    // The sweep proper. Same contract as Compute, with a run's tail delivered whole.
    public static void ComputeRuns<TSink>(int ox, int oy, int radius,
        ReadOnlySpan<bool> opaque, int width, int height, ref TSink sink)
        where TSink : struct, IFovRunSink
    {
        if ((uint)ox < (uint)width && (uint)oy < (uint)height) sink.Reveal(ox, oy);
        if (radius < 1) return;

        // The east/west cones own every cell with |dx| > |dy|, and the ellipse reaches |dx| =
        // radius x aspect there, so they sweep to ReachX; stopping them at depth `radius` like the
        // north/south pair would clip the visual circle to a square.
        int reachX = ReachX(radius);
        int rSq = radius * radius;
        Span<int> nsMax = radius + 1 <= MaxStackRadius ? stackalloc int[radius + 1] : new int[radius + 1];
        Span<int> ewMax = reachX + 1 <= MaxStackRadius ? stackalloc int[reachX + 1] : new int[reachX + 1];
        BuildColumnBounds(radius, reachX, rSq, nsMax, ewMax);

        // The cardinal picks a fixed line and a fixed step, so the 4-way transform is resolved
        // once per cardinal rather than per cell: a row is a map row (along x, north/south) or a
        // map column (along y, east/west).
        scoped var ctx = new ScanCtx { Opaque = opaque, Width = width };
        for (int cardinal = 0; cardinal < 4; cardinal++)
        {
            bool alongX = cardinal == 0 || cardinal == 2;
            ctx.AlongX = alongX;
            ctx.Rad = alongX ? nsMax : ewMax;
            ctx.MaxDepth = alongX ? radius : reachX;
            ctx.Origin = alongX ? ox : oy;
            ctx.Limit = alongX ? width : height;
            ctx.Cross = alongX ? height : width;
            ctx.LineBase = alongX ? oy : ox;
            ctx.LineStep = cardinal == 0 || cardinal == 3 ? -1 : 1;
            Scan(ref ctx, ref sink, 1, -1.0, 1.0);
        }
    }

    // The farthest |dx| the ellipse admits on the origin's own row: the depth the east/west
    // cones sweep to. Found by testing the same float expression the bounds use.
    public static int ReachX(int radius)
    {
        int rSq = radius * radius;
        int x = (int)(radius * Generation.FloorMask.CellAspectRatio);
        while (x > 0 && (float)(x * x) * InvAspectSq > rSq) x--;
        while ((float)((x + 1) * (x + 1)) * InvAspectSq <= rSq) x++;
        return x;
    }

    // The widest column the aspect-corrected ellipse admits at each depth — one table for a row
    // swept along x (north/south, depth |dy| up to radius) and one for a row swept along y
    // (east/west, depth |dx| up to ReachX). This stands in for a per-cell
    // `dx*dx*InvAspectSq + dy*dy <= rSq`: along a row one of dx/dy is pinned to +/-depth, so the
    // test collapses to a constant range on the column index.
    //
    // The bound is found by testing that float expression itself rather than by solving it, so
    // the admitted set is identical and not merely close, whatever the aspect constant rounds to.
    // The expression is monotone in |col| and the bound is monotone in depth, so one decrementing
    // walk finds every entry. The re-solve arms are unreachable at an aspect of 2.0 (the bound
    // never drops below zero before the last depth) but live for an aspect under 1.0.
    private static void BuildColumnBounds(int radius, int reachX, int rSq, Span<int> nsMax, Span<int> ewMax)
    {
        int ns = -1, ew = -1;
        for (int d = 1; d <= radius; d++)
        {
            float fixedNs = (float)(d * d);
            if (ns < 0)
            {
                double est = (rSq - (double)fixedNs) / InvAspectSq;
                ns = est <= 0 ? 0 : (int)Math.Sqrt(est);
                while ((float)((ns + 1) * (ns + 1)) * InvAspectSq + fixedNs <= rSq) ns++;
            }
            while (ns >= 0 && (float)(ns * ns) * InvAspectSq + fixedNs > rSq) ns--;
            nsMax[d] = ns;
        }

        for (int d = 1; d <= reachX; d++)
        {
            float fixedEw = (float)(d * d) * InvAspectSq;
            if (ew < 0)
            {
                double est = rSq - (double)fixedEw;
                ew = est <= 0 ? 0 : (int)Math.Sqrt(est);
                while (fixedEw + (float)((ew + 1) * (ew + 1)) <= rSq) ew++;
            }
            while (ew >= 0 && fixedEw + (float)(ew * ew) > rSq) ew--;
            ewMax[d] = ew;
        }
    }

    // Everything one cardinal's sweep holds constant: the table, the column bound per depth, and
    // the axis mapping. Origin/Limit index the row's own axis; LineBase/LineStep/Cross the other.
    private ref struct ScanCtx
    {
        public ReadOnlySpan<bool> Opaque;
        public Span<int> Rad;
        public int MaxDepth, Width, Origin, Limit, Cross, LineBase, LineStep;
        public bool AlongX;
    }

    // One sector: rows from `depth` outward between startSlope and endSlope. startSlope is
    // rewritten on a wall→floor transition to narrow the sector; a floor→wall transition spawns
    // a child sector (a recursive call); an open row continues at the next depth (the loop).
    private static void Scan<TSink>(ref ScanCtx ctx, ref TSink sink, int depth,
        double startSlope, double endSlope)
        where TSink : struct, IFovRunSink
    {
        int maxDepth = ctx.MaxDepth, width = ctx.Width;
        ReadOnlySpan<bool> opaque = ctx.Opaque;
        bool alongX = ctx.AlongX;
        int origin = ctx.Origin, inLo = -origin, inHi = ctx.Limit - 1 - origin;

        for (; depth <= maxDepth; depth++)
        {
            double symLo = depth * startSlope;
            double symHi = depth * endSlope;

            // Banker rounding for column bounds: up = floor(n+0.5), down = ceil(n-0.5).
            int minCol = (int)Math.Floor(symLo + 0.5);
            int maxCol = (int)Math.Ceiling(symHi - 0.5);
            if (minCol > maxCol) return;              // empty row: nothing follows from it

            // The symmetry test is `col >= depth*StartSlope && col <= depth*EndSlope`. For an
            // integer col that is exactly `col >= ceil(lo) && col <= floor(hi)`, so it becomes
            // two integer compares that only move when startSlope does — which happens at a
            // wall→floor transition, not per cell.
            int symLoI = (int)Math.Ceiling(symLo);
            int symHiI = (int)Math.Floor(symHi);
            int rad = ctx.Rad[depth];
            int revLo = symLoI > -rad ? symLoI : -rad;
            int revHi = symHiI < rad ? symHiI : rad;

            // A line off the map blocks along its whole length: no reveal survives, no slope
            // narrows, and the continuation needs a floor. Nothing to do.
            int fixedLine = ctx.LineBase + ctx.LineStep * depth;
            if ((uint)fixedLine >= (uint)ctx.Cross) return;

            // -1 unknown, 0 floor, 1 wall. A three-state int rather than a bool?, whose lifted
            // comparisons cost two tests each, per cell.
            int prev = -1;

            // Columns that land on the map, so the middle loop needs no per-cell bounds test.
            int midLo = minCol > inLo ? minCol : inLo;
            int midHi = maxCol < inHi ? maxCol : inHi;
            if (minCol < midLo) prev = 1;             // off-map prefix: every cell blocks

            int col = midLo;
            while (col <= midHi)
            {
                int i = origin + col;
                bool isWall;
                int runEnd;

                // Last column of the run of cells that share this cell's blocking state. Along
                // x the row is contiguous, so IndexOf runs vectorized; along y it strides by
                // width.
                if (alongX)
                {
                    int idx = fixedLine * width + i;
                    isWall = opaque[idx];
                    int k = opaque.Slice(idx, midHi - col + 1).IndexOf(!isWall);
                    runEnd = k < 0 ? midHi : col + k - 1;
                }
                else
                {
                    int idx = i * width + fixedLine;
                    isWall = opaque[idx];
                    int c = col + 1, j = idx + width;
                    while (c <= midHi && opaque[j] == isWall) { c++; j += width; }
                    runEnd = c - 1;
                }

                // The run's first cell takes the full body. Reveal comes before the narrowing:
                // the transition cell is admitted by the sector as it stood when the cell was
                // reached.
                if (isWall ? (col >= -rad && col <= rad) : (col >= revLo && col <= revHi))
                {
                    if (alongX) sink.Reveal(i, fixedLine); else sink.Reveal(fixedLine, i);
                }
                if (prev == 1 && !isWall)
                {
                    startSlope = Slope(depth, col);
                    symLoI = (int)Math.Ceiling(depth * startSlope);
                    revLo = symLoI > -rad ? symLoI : -rad;
                }
                else if (prev == 0 && isWall)
                {
                    Scan(ref ctx, ref sink, depth + 1, startSlope, Slope(depth, col));
                }
                prev = isWall ? 1 : 0;

                // The rest of the run cannot narrow the sector or spawn a child row, so it is
                // one contiguous range intersected with the reveal window, handed over whole.
                int lo = isWall ? -rad : revLo;
                int hi = isWall ?  rad : revHi;
                int a = col + 1 > lo ? col + 1 : lo;
                int b = runEnd < hi ? runEnd : hi;
                if (a <= b)
                {
                    if (alongX) sink.RevealRow(fixedLine, origin + a, origin + b);
                    else sink.RevealColumn(fixedLine, origin + a, origin + b);
                }

                col = runEnd + 1;
            }

            // Off-map suffix: every cell blocks, so at most one floor→wall transition fires.
            int sufLo = minCol > inHi + 1 ? minCol : inHi + 1;
            if (sufLo <= maxCol)
            {
                if (prev == 0) Scan(ref ctx, ref sink, depth + 1, startSlope, Slope(depth, sufLo));
                prev = 1;
            }

            // The row ended on floor: the sector continues one depth further, narrowed as it
            // stands. Ended on wall (or empty): this sector is done.
            if (prev != 0) return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Slope(int depth, int col) => (2.0 * col - 1.0) / (2.0 * depth);
}
