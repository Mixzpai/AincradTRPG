namespace SAOTRPG.Map.Generation.Passes;

// Boundary mountain ring. Town floors (F1/F48) preserve the legacy jagged-walker
// border; non-town floors stamp a 2-tile mountain ring along the disk edge.
public sealed class BorderPass : IGenerationPass
{
    public string Name => "Border";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        if (ctx.CircleMask is null)
        {
            // Towns + F100 — unchanged: legacy jagged 4-edge walker.
            MapGenerator.BuildJaggedMountainBorder(ctx.Map, ctx.Rng);
            return;
        }

        // Non-town: stamp Mountain on every out-of-disk cell that touches an in-disk
        // neighbor, plus the 2-tile ring just outside. Re-stamps over BaseTerrainPass's
        // Mountain default to densify the boundary visual.
        int w = ctx.Width, h = ctx.Height;
        var map = ctx.Map;
        var mask = ctx.CircleMask;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (mask[x, y]) continue;
            // RANGE 1 IS CONTAINED IN RANGE 2 — the old `NeighborInside(1) || NeighborInside(2)`
            // re-probed the same nine cells inside the twenty-five, on every out-of-disk cell of
            // the map (~215,000 on a 1000x1000 floor). Strictly redundant, so the result is
            // identical.
            if (NeighborInside(mask, x, y, 2)) map.Tiles[x, y].Type = TileType.Mountain;
        }
    }

    // True if any tile within Chebyshev `range` of (x,y) is inside the disk.
    private static bool NeighborInside(bool[,] mask, int x, int y, int range)
    {
        int w = mask.GetLength(0), h = mask.GetLength(1);
        for (int dx = -range; dx <= range; dx++)
        for (int dy = -range; dy <= range; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;
            if (mask[nx, ny]) return true;
        }
        return false;
    }
}
