namespace SAOTRPG.Map.Generation.Passes;

// Softens tile edges (forest→tall-grass, rocks/shores→sparse). Reads a byte snapshot rather than
// the 4-byte enum grid; CircleMask gate skips non-disk tiles.
public sealed class BiomeBlendPass : IGenerationPass
{
    public string Name => "BiomeBlend";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        // Forest fringe tracks TreeDensity (0..~2.5 nominally); rock fringe tracks
        // RockDensity; shore fringe is high when lakes are enabled.
        float forestFringeChance = Math.Clamp(ctx.Config.TreeDensity / 2.5f, 0.1f, 0.9f);
        float rockFringeChance   = Math.Clamp(ctx.Config.RockDensity / 2.5f, 0.1f, 0.9f);
        float shoreFringeChance  = ctx.Config.WaterLakesEnabled ? 0.5f : 0.2f;
        BlendBiomes(ctx.Map, ctx.Rng, ctx.CircleMask,
            forestFringeChance, rockFringeChance, shoreFringeChance);
    }

    // Each neighbour contributes to at most one of the three tallies, and eight neighbours cannot
    // overflow a byte field — so the three counts ride in one int and the 3x3 scan becomes eight
    // adds with no branches. The table is indexed by tile type, which is what turns three
    // `is A or B or C` pattern tests per neighbour into one load.
    private const int Tree  = 0x000001;
    private const int Rock  = 0x000100;
    private const int Water = 0x010000;

    private static readonly int[] NeighborClass = BuildNeighborClass();

    private static int[] BuildNeighborClass()
    {
        int max = 0;
        foreach (TileType t in Enum.GetValues<TileType>()) if ((int)t > max) max = (int)t;
        var cls = new int[max + 1];
        foreach (TileType t in Enum.GetValues<TileType>())
        {
            int v = 0;
            if (t is TileType.Tree or TileType.TreePine or TileType.Bush) v += Tree;
            if (t is TileType.Rock or TileType.Mountain) v += Rock;
            if (t is TileType.Water or TileType.WaterDeep) v += Water;
            cls[(int)t] = v;
        }
        return cls;
    }

    // One byte a cell in the snapshot; CircleMask gate skips off-disk tiles.
    //
    // The snapshot is flat and column-major (x*h + y), which is the layout the 3x3 scan below wants
    // — the inner loop walks y, so each of the three sampled columns is a contiguous run. It is
    // filled row-major, because that is the order GameMap stores tiles in and streaming the large
    // tile grid matters more than the strided writes into the small table.
    private static void BlendBiomes(GameMap map, Random rng, bool[,]? circleMask,
        float forestFringeChance, float rockFringeChance, float shoreFringeChance)
    {
        int w = map.Width, h = map.Height;
        var snapshot = new byte[w * h];
        var tiles = map.Tiles;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            snapshot[x * h + y] = (byte)tiles[x, y].Type;

        var cls = NeighborClass;
        for (int x = 1; x < w - 1; x++)
        {
            int b0 = (x - 1) * h, b1 = x * h, b2 = (x + 1) * h;
            for (int y = 1; y < h - 1; y++)
            {
                if (circleMask != null && !circleMask[x, y]) continue;
                if (snapshot[b1 + y] != (byte)TileType.Grass) continue;

                int acc = cls[snapshot[b0 + y - 1]] + cls[snapshot[b0 + y]] + cls[snapshot[b0 + y + 1]]
                        + cls[snapshot[b1 + y - 1]]                        + cls[snapshot[b1 + y + 1]]
                        + cls[snapshot[b2 + y - 1]] + cls[snapshot[b2 + y]] + cls[snapshot[b2 + y + 1]];
                int trees = acc & 0xFF, rocks = (acc >> 8) & 0xFF, water = (acc >> 16) & 0xFF;

                if (trees >= 3 && rng.NextDouble() < forestFringeChance)
                    tiles[x, y].Type = TileType.GrassTall;
                else if (rocks >= 2 && rng.NextDouble() < rockFringeChance)
                    tiles[x, y].Type = TileType.GrassSparse;
                else if (water >= 2 && rng.NextDouble() < shoreFringeChance)
                    tiles[x, y].Type = TileType.GrassSparse;
            }
        }
    }
}
