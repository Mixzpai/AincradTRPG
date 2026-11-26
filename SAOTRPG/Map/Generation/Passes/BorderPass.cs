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
        //
        // WRITING MOUNTAIN OVER MOUNTAIN IS A NO-OP, so a cell already carrying it can be skipped
        // before the neighbour scan runs at all. It removes ~215,000 twenty-five-cell scans per
        // 1000x1000 floor, and the guard is exact by construction: it only ever skips a write that
        // would not have changed the tile.
        //
        // MEASURED, and worth knowing: the skip fires on EVERY out-of-disk cell — 0 write
        // candidates across 24 floors x 4 seeds — because BaseTerrainPass stamps the same Mountain
        // default over a strict superset of this ring, so this branch has no reachable write at
        // all. Deleting the pass outright is therefore available and is the dev's call; the guard
        // above stays correct either way, and does not depend on that pass ordering holding.
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
