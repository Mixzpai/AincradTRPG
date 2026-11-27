namespace SAOTRPG.Map.Generation.Passes;

// Boundary mountain ring: stamps a 2-tile mountain ring along the disk edge. Every pipeline
// floor has a circle mask; floor 100 never enters the pipeline.
public sealed class BorderPass : IGenerationPass
{
    public string Name => "Border";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        if (ctx.CircleMask is null)
            throw new InvalidOperationException($"BorderPass on floor {ctx.FloorNumber} has no circle mask");

        // Stamp Mountain on every out-of-disk cell that touches an in-disk
        // neighbor, plus the 2-tile ring just outside. Re-stamps over BaseTerrainPass's
        // Mountain default to densify the boundary visual.
        //
        // Writing Mountain over Mountain is a no-op, so a cell already carrying it is skipped
        // before the neighbour scan runs at all. The guard is exact by construction: it only ever
        // skips a write that would not have changed the tile. BaseTerrainPass stamps the same
        // Mountain default over a superset of this ring, but the guard does not depend on that
        // pass ordering holding.
        //
        // y-outer here, unlike the terrain passes: this branch draws no rng, so the visit order is
        // free, and row-major matches GameMap's own tile layout.
        int w = ctx.Width, h = ctx.Height;
        var map = ctx.Map;
        var mask = ctx.CircleMask;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (mask[x, y]) continue;
            if (map.Tiles[x, y].Type == TileType.Mountain) continue;
            // Range 1 is contained in range 2, so a single range-2 scan covers both rings.
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
