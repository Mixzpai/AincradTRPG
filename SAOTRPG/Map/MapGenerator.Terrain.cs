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
        int width = RiverWidth(ctx);

        while (pos < limit)
        {
            // Stamp `width` tiles across the cross axis; protected tiles survive the stamp.
            for (int i = 0; i < width; i++)
            {
                int rx = horizontal ? pos : cross + i;
                int ry = horizontal ? cross + i : pos;
                if (!ctx.IsInTownKeepOut(rx, ry)) StampRiverTile(map, rx, ry);
            }

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

    // Tiles a river is stamped across, widening on the cross axis. Both river algorithms were
    // hardcoded to 2 while every biome JSON carried a riverMinWidth nothing read, so an Aquatic
    // canal ran the same width as a Ruins trickle. Consumes no rng, so honouring the config moves
    // water tiles without shifting the stream position for any later pass.
    // A live river algorithm with a width below 1 is a contradictory config: clamped, and logged
    // rather than silently generating nothing.
    internal static int RiverWidth(Generation.WorldContext ctx)
    {
        int width = ctx.Config.RiverMinWidth;
        if (width >= 1) return width;
        UI.DebugLogger.LogGame("RIVER",
            $"biome={ctx.Biome} algorithm={ctx.Config.RiverAlgorithm} but riverMinWidth={width} — clamped to 1");
        return 1;
    }

    internal static void StampRiverTile(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        if (IsProtectedFromWater(map.Tiles[x, y].Type)) return;
        map.Tiles[x, y].Type = TileType.Water;
    }

    // ── Lake: CA noise → N× 5-neighbor smoothing → organic contiguous shape. Interior tiles → WaterDeep.
    //
    // `seedPct` and `caPasses` come from the biome (they were hardcoded 45 and 4 while every biome
    // JSON declared its own pair and nothing read them). Both change the SHAPE of a lake and
    // neither changes how much rng is drawn: the seed loop's `&&` short-circuits, so `rng.Next(100)`
    // fires once per IN-CIRCLE cell whatever the density is, and the smoothing rule draws nothing
    // at all. That is what lets this be honoured without shifting the stream for any later pass.
    // More seed density and more passes both mean a larger, more solid lake — and a solid lake
    // interior is WaterDeep, which blocks movement below Swimming L25, so this knob is a
    // reachability input and not only a cosmetic one.
    internal static void GenerateLake(GameMap map, int cx, int cy, int radius, Random rng,
                                      int seedPct, int caPasses)
    {
        int d = radius * 2 + 1;
        bool[,] grid = new bool[d, d];

        // Seed: random fill inside a circle at the biome's density.
        for (int dx = 0; dx < d; dx++)
        for (int dy = 0; dy < d; dy++)
        {
            int distSq = (dx - radius) * (dx - radius) + (dy - radius) * (dy - radius);
            grid[dx, dy] = distSq <= radius * radius && rng.Next(100) < seedPct;
        }

        // Smooth: N iterations of the 5-neighbor rule.
        for (int iter = 0; iter < caPasses; iter++)
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
}
