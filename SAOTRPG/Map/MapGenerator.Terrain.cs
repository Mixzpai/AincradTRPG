namespace SAOTRPG.Map;

// Terrain helpers: CA lakes, drunk-walk rivers, neighbor-smoothing biome blend.
// Stamp terrain onto existing GameMap without disturbing structures or features.
public static partial class MapGenerator
{
    // ── River: drunk-walk with drift from source edge to opposite, meanders laterally but always progresses.
    // horizontal=true → L→R, false → T→B. ctx supplied so river skips town keep-out tiles.
    internal static void GenerateRiver(GameMap map, bool horizontal, Random rng, Generation.WorldContext ctx)
    {
        int w = map.Width, h = map.Height;

        // Start on a random point along the source edge.
        int pos, cross;
        if (horizontal)
        {
            pos = 3;                                       // x starts left
            cross = rng.Next(15, h - 15);                  // y random
        }
        else
        {
            pos = 3;                                       // y starts top
            cross = rng.Next(15, w - 15);                  // x random
        }

        int limit = horizontal ? w - 3 : h - 3;

        while (pos < limit)
        {
            // Stamp river tiles (1-2 wide depending on hash).
            int rx = horizontal ? pos : cross;
            int ry = horizontal ? cross : pos;
            int rx2 = rx + (horizontal ? 0 : 1);
            int ry2 = ry + (horizontal ? 1 : 0);
            if (!ctx.IsInTownKeepOut(rx,  ry))  StampRiverTile(map, rx,  ry);
            if (!ctx.IsInTownKeepOut(rx2, ry2)) StampRiverTile(map, rx2, ry2);

            // Advance along the primary axis.
            pos++;

            // Drift along the cross axis with a slight bias.
            int drift = rng.Next(5);
            if (drift == 0)      cross = Math.Max(5, cross - 1);
            else if (drift == 1) cross = Math.Min((horizontal ? h : w) - 5, cross + 1);
        }
    }

    // Tiles water must never overwrite — structures, features, paths, mountains.
    private static bool IsProtectedFromWater(TileType t) =>
        t is TileType.Wall or TileType.Floor or TileType.Door
        or TileType.StairsUp or TileType.StairsDown or TileType.LabyrinthEntrance
        or TileType.Campfire or TileType.Anvil or TileType.BountyBoard
        or TileType.Shrine or TileType.EnchantShrine or TileType.Fountain
        or TileType.Pillar or TileType.Chest or TileType.LoreStone
        or TileType.Path or TileType.Mountain or TileType.Journal
        or TileType.Lever or TileType.PressurePlate;

    internal static void StampRiverTile(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        if (IsProtectedFromWater(map.Tiles[x, y].Type)) return;
        map.Tiles[x, y].Type = TileType.Water;
    }

    // ── Lake: CA noise → 4x 5-neighbor smoothing → organic contiguous shape. Interior tiles → WaterDeep.
    internal static void GenerateLake(GameMap map, int cx, int cy, int radius, Random rng)
    {
        int d = radius * 2 + 1;
        bool[,] grid = new bool[d, d];

        // Seed: random fill inside a circle at 45% density.
        for (int dx = 0; dx < d; dx++)
        for (int dy = 0; dy < d; dy++)
        {
            int distSq = (dx - radius) * (dx - radius) + (dy - radius) * (dy - radius);
            grid[dx, dy] = distSq <= radius * radius && rng.Next(100) < 45;
        }

        // Smooth: 4 iterations of the 5-neighbor rule.
        for (int iter = 0; iter < 4; iter++)
        {
            var next = new bool[d, d];
            for (int dx = 1; dx < d - 1; dx++)
            for (int dy = 1; dy < d - 1; dy++)
            {
                int neighbors = 0;
                for (int nx = -1; nx <= 1; nx++)
                for (int ny = -1; ny <= 1; ny++)
                    if (grid[dx + nx, dy + ny]) neighbors++;
                next[dx, dy] = neighbors >= 5;
            }
            grid = next;
        }

        // Stamp onto the map.
        for (int dx = 0; dx < d; dx++)
        for (int dy = 0; dy < d; dy++)
        {
            if (!grid[dx, dy]) continue;
            int mx = cx - radius + dx, my = cy - radius + dy;
            if (!map.InBounds(mx, my)) continue;
            if (IsProtectedFromWater(map.Tiles[mx, my].Type)) continue;

            // Interior (all 4 cardinal neighbors are also water in the grid) → deep.
            bool interior = dx > 0 && dx < d - 1 && dy > 0 && dy < d - 1
                && grid[dx - 1, dy] && grid[dx + 1, dy] && grid[dx, dy - 1] && grid[dx, dy + 1];
            map.Tiles[mx, my].Type = interior ? TileType.WaterDeep : TileType.Water;
        }
    }

    // ── Biome blend: post-pass that softens edges (GrassTall near forests, GrassSparse near rocks/water).
    // Fringe conversion chances are biome-tuned via BiomeBlendPass (Bundle 5).
    internal static void BlendBiomes(GameMap map, Random rng,
        float forestFringeChance, float rockFringeChance, float shoreFringeChance)
    {
        int w = map.Width, h = map.Height;

        // Work on a snapshot so we don't feed back mid-pass.
        var snapshot = new TileType[w, h];
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            snapshot[x, y] = map.Tiles[x, y].Type;

        for (int x = 1; x < w - 1; x++)
        for (int y = 1; y < h - 1; y++)
        {
            var t = snapshot[x, y];
            if (t != TileType.Grass) continue;

            int trees = 0, rocks = 0, water = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var n = snapshot[x + dx, y + dy];
                if (n is TileType.Tree or TileType.TreePine or TileType.Bush) trees++;
                if (n is TileType.Rock or TileType.Mountain) rocks++;
                if (n is TileType.Water or TileType.WaterDeep) water++;
            }

            // Forest fringe → tall grass.
            if (trees >= 3 && rng.NextDouble() < forestFringeChance)
                map.Tiles[x, y].Type = TileType.GrassTall;
            // Rocky fringe → sparse grass.
            else if (rocks >= 2 && rng.NextDouble() < rockFringeChance)
                map.Tiles[x, y].Type = TileType.GrassSparse;
            // Shoreline fringe → sparse grass (beach feel).
            else if (water >= 2 && rng.NextDouble() < shoreFringeChance)
                map.Tiles[x, y].Type = TileType.GrassSparse;
        }
    }
}
