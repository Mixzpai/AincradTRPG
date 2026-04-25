namespace SAOTRPG.Map.Generation.Passes;

// Softens tile edges (forest→tall-grass, rocks/shores→sparse). Byte snapshot replaces 4-byte enum,
// CircleMask gate skips non-disk tiles.
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

    // Byte-snapshot variant. Drops 4MB→1MB at 1000x1000; CircleMask gate skips off-disk tiles.
    private static void BlendBiomes(GameMap map, Random rng, bool[,]? circleMask,
        float forestFringeChance, float rockFringeChance, float shoreFringeChance)
    {
        int w = map.Width, h = map.Height;
        var snapshot = new byte[w, h];
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            snapshot[x, y] = (byte)map.Tiles[x, y].Type;

        for (int x = 1; x < w - 1; x++)
        for (int y = 1; y < h - 1; y++)
        {
            if (circleMask != null && !circleMask[x, y]) continue;
            var t = (TileType)snapshot[x, y];
            if (t != TileType.Grass) continue;

            int trees = 0, rocks = 0, water = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var n = (TileType)snapshot[x + dx, y + dy];
                if (n is TileType.Tree or TileType.TreePine or TileType.Bush) trees++;
                if (n is TileType.Rock or TileType.Mountain) rocks++;
                if (n is TileType.Water or TileType.WaterDeep) water++;
            }

            if (trees >= 3 && rng.NextDouble() < forestFringeChance)
                map.Tiles[x, y].Type = TileType.GrassTall;
            else if (rocks >= 2 && rng.NextDouble() < rockFringeChance)
                map.Tiles[x, y].Type = TileType.GrassSparse;
            else if (water >= 2 && rng.NextDouble() < shoreFringeChance)
                map.Tiles[x, y].Type = TileType.GrassSparse;
        }
    }
}
