namespace SAOTRPG.Map.Generation;

// Soft-circle disk mask. Disk fills the (square) grid in cell-coords with
// per-floor sin-stack wobble for an organic edge. FOV's aspect correction is a
// separate concern (Shadowcaster scales x-distance by CellAspectRatio).
public static class FloorMask
{
    // Cell aspect — terminal cells render ~2x as tall as wide. Used by Shadowcaster
    // for FOV roundness on screen; the disk itself is true cell-coord circle.
    public const float CellAspectRatio = 2.0f;

    // Fanned out across columns for the same reason HeightmapField.Materialize is: the per-cell
    // work is one atan2 plus three sines, it reads nothing mutable, and each cell writes its own
    // slot — so the result is bit-identical whatever the scheduling. This runs BEFORE the first
    // pass and so appears in no per-pass timer, which is why it went unmeasured; it was the single
    // largest line item in floor generation.
    // Small maps stay serial: the fork costs more than the work below this size.
    private const int ParallelCellThreshold = 65536;

    // bool[w,h]; true = inside playable disk. ~1MB at 1000x1000, allocated once.
    public static bool[,] Build(int w, int h, int floorSeed)
    {
        var mask = new bool[w, h];
        float cx = w * 0.5f - 0.5f;
        float cy = h * 0.5f - 0.5f;
        float r0 = Math.Min(w, h) * 0.5f - 2f;

        // Phases are loop-invariant; they were being recomputed from the seed on every cell.
        float p1 = floorSeed * 0.001f, p2 = floorSeed * 0.003f, p3 = floorSeed * 0.005f;

        if ((long)w * h < ParallelCellThreshold)
        {
            for (int x = 0; x < w; x++) BuildColumn(mask, x, h, cx, cy, r0, p1, p2, p3);
            return mask;
        }
        System.Threading.Tasks.Parallel.For(0, w, x => BuildColumn(mask, x, h, cx, cy, r0, p1, p2, p3));
        return mask;
    }

    private static void BuildColumn(bool[,] mask, int x, int h,
        float cx, float cy, float r0, float p1, float p2, float p3)
    {
        // dx and dx*dx do not depend on y.
        float dx = x - cx;
        float dx2 = dx * dx;
        for (int y = 0; y < h; y++)
        {
            float dy = y - cy;
            float dist = MathF.Sqrt(dx2 + dy * dy);

            float ang = MathF.Atan2(dy, dx);
            float wobble = MathF.Sin(ang * 3f + p1) * 4f
                         + MathF.Sin(ang * 7f + p2) * 2f
                         + MathF.Sin(ang * 13f + p3) * 1f;

            mask[x, y] = dist <= r0 + wobble;
        }
    }
}
