namespace SAOTRPG.Map.Generation;

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

    // Tiles a river is stamped across, widening on the cross axis, from the biome's riverMinWidth.
    // Consumes no rng, so the width moves water tiles without shifting the stream position for
    // any later pass.
    // A live river algorithm with a width below 1 is a contradictory config: clamped, and logged
    // rather than silently generating nothing.
    internal static int RiverWidth(Generation.WorldContext ctx)
    {
        int width = ctx.Config.RiverMinWidth;
        if (width >= 1) return width;
        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("RIVER",
            $"biome={ctx.Biome} algorithm={ctx.Config.RiverAlgorithm} but riverMinWidth={width} — clamped to 1");
        return 1;
    }

    internal static void StampRiverTile(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        if (IsProtectedFromWater(map.Tiles[x, y].Type)) return;
        map.Tiles[x, y].Type = TileType.Water;
    }

    // ── Lake. With a heightmap: the basin under (cx, cy), filled lowest cell first until it holds
    // its share of the lake's circle (BasinShare of `seedPct`), so the water takes the ground's shape and
    // the shore follows a contour. Without one: random fill at `seedPct` density. Either way
    // `caPasses` rounds of the 5-of-9 rule smooth the shore, and an interior cell is WaterDeep,
    // which blocks movement below Swimming L25: a reachability input, not only a cosmetic one.
    // The basin path draws nothing from the stream; the random fill draws once per cell of its
    // circle.
    internal static void GenerateLake(GameMap map, int cx, int cy, int radius, Random rng,
                                      int seedPct, int caPasses, float[,]? heights = null)
    {
        int d = radius * 2 + 1;
        // Flat byte grids indexed dx*d + dy, and a ping-pong pair rather than a fresh allocation
        // per smoothing iteration: a large floor places dozens of lakes, each running caPasses.
        var grid = new byte[d * d];
        var next = new byte[d * d];
        int r2 = radius * radius;

        if (heights != null) FloodBasin(map, heights, cx, cy, radius, seedPct, grid, d);
        else
        {
            // The && short-circuits, so a cell outside the circle costs no draw.
            for (int dx = 0; dx < d; dx++)
            {
                int bx = dx * d;
                int ddx = dx - radius, ddx2 = ddx * ddx;
                for (int dy = 0; dy < d; dy++)
                {
                    int ddy = dy - radius;
                    grid[bx + dy] = ddx2 + ddy * ddy <= r2 && rng.Next(100) < seedPct ? (byte)1 : (byte)0;
                }
            }
        }

        // Smooth: N iterations of the 5-neighbor rule (the 3x3 count includes the centre).
        for (int iter = 0; iter < caPasses; iter++)
        {
            // The border ring is never written, and the buffer being reused still holds the
            // previous generation, so it has to be cleared exactly as a fresh array would be.
            Array.Clear(next);
            for (int dx = 1; dx < d - 1; dx++)
            {
                int b1 = dx * d, b0 = b1 - d, b2 = b1 + d;
                for (int dy = 1; dy < d - 1; dy++)
                {
                    int n = grid[b0 + dy - 1] + grid[b0 + dy] + grid[b0 + dy + 1]
                          + grid[b1 + dy - 1] + grid[b1 + dy] + grid[b1 + dy + 1]
                          + grid[b2 + dy - 1] + grid[b2 + dy] + grid[b2 + dy + 1];
                    next[b1 + dy] = n >= 5 ? (byte)1 : (byte)0;
                }
            }
            (grid, next) = (next, grid);
        }

        // Stamp onto the map.
        var tiles = map.Tiles;
        for (int dx = 0; dx < d; dx++)
        {
            int b1 = dx * d;
            for (int dy = 0; dy < d; dy++)
            {
                if (grid[b1 + dy] == 0) continue;
                int mx = cx - radius + dx, my = cy - radius + dy;
                if (!map.InBounds(mx, my)) continue;
                if (IsProtectedFromWater(tiles[mx, my].Type)) continue;

                // Interior (all 4 cardinal neighbors are also water in the grid) → deep.
                bool interior = dx > 0 && dx < d - 1 && dy > 0 && dy < d - 1
                    && grid[b1 - d + dy] != 0 && grid[b1 + d + dy] != 0
                    && grid[b1 + dy - 1] != 0 && grid[b1 + dy + 1] != 0;
                tiles[mx, my].Type = interior ? TileType.WaterDeep : TileType.Water;
            }
        }
    }

    // How much of its circle a lake fills, from the biome's seed density. The ramp follows the
    // random-fill path, where 5-of-9 smoothing erases a lake seeded under ~30% and fills the
    // circle above ~70%, so each biome keeps the same amount of water either way: Aquatic full
    // and Dark dry.
    private static double BasinShare(int seedPct) => Math.Clamp((seedPct - 30) / 40.0, 0.0, 1.0);

    // And a lake stops at its spill level whatever its share: once the lowest cell left on the
    // shore is this far above the lake's floor, the water would run out rather than climb. Without
    // it a full share would fill the whole circle and make the lake a perfect disc.
    private const float MaxLakeRise = 0.12f;

    // Fills the lake's grid from (cx, cy) outward, always taking the lowest cell on the shore next,
    // within the lake's circle and around anything water may not cover.
    private static void FloodBasin(GameMap map, float[,] heights, int cx, int cy, int radius,
                                   int seedPct, byte[] grid, int d)
    {
        int r2 = radius * radius;
        int target = (int)(Math.PI * r2 * BasinShare(seedPct));
        if (target < 1) return;
        var shore = new PriorityQueue<int, float>();
        var seen = new bool[d * d];
        int start = radius * d + radius;
        if (!map.InBounds(cx, cy)) return;
        shore.Enqueue(start, heights[cx, cy]);
        seen[start] = true;
        float spill = heights[cx, cy] + MaxLakeRise;
        int filled = 0;
        while (filled < target && shore.TryDequeue(out int g, out float level))
        {
            if (level > spill) break;
            int gx = g / d, gy = g - gx * d;
            int mx = cx - radius + gx, my = cy - radius + gy;
            if (IsProtectedFromWater(map.Tiles[mx, my].Type)) continue;
            grid[g] = 1;
            filled++;
            for (int k = 0; k < 4; k++)
            {
                int nx = gx + (k == 0 ? -1 : k == 1 ? 1 : 0), ny = gy + (k == 2 ? -1 : k == 3 ? 1 : 0);
                int ddx = nx - radius, ddy = ny - radius;
                if (ddx * ddx + ddy * ddy > r2) continue;
                int n = nx * d + ny;
                int wx = cx + ddx, wy = cy + ddy;
                if (seen[n] || !map.InBounds(wx, wy)) continue;
                seen[n] = true;
                shore.Enqueue(n, heights[wx, wy]);
            }
        }
    }
}
