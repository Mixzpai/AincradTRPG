namespace SAOTRPG.Map.Generation;

// Soft-circle disk mask. Disk fills the (square) grid in cell-coords with
// per-floor sin-stack wobble for an organic edge. FOV's aspect correction is a
// separate concern (Shadowcaster scales x-distance by CellAspectRatio).
public static class FloorMask
{
    // Cell aspect — terminal cells render ~2x as tall as wide. Used by Shadowcaster
    // for FOV roundness on screen; the disk itself is true cell-coord circle.
    public const float CellAspectRatio = 2.0f;

    // bool[w,h]; true = inside playable disk. ~1MB at 1000x1000, allocated once.
    public static bool[,] Build(int w, int h, int floorSeed)
    {
        var mask = new bool[w, h];
        float cx = w * 0.5f - 0.5f;
        float cy = h * 0.5f - 0.5f;
        float r0 = Math.Min(w, h) * 0.5f - 2f;

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            float dx = x - cx;
            float dy = y - cy;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            float ang = MathF.Atan2(dy, dx);
            float wobble = MathF.Sin(ang * 3f + floorSeed * 0.001f) * 4f
                         + MathF.Sin(ang * 7f + floorSeed * 0.003f) * 2f
                         + MathF.Sin(ang * 13f + floorSeed * 0.005f) * 1f;

            mask[x, y] = dist <= r0 + wobble;
        }
        return mask;
    }
}
