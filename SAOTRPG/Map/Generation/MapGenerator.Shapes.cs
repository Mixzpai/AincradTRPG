namespace SAOTRPG.Map.Generation;

// Varied room shape builders for map generation.
public static partial class MapGenerator
{
    internal static void BuildCircularRoom(GameMap map, int cx, int cy, int radius)
    {
        int r2 = radius * radius;
        int outerR2 = (radius + 1) * (radius + 1);
        for (int x = cx - radius - 1; x <= cx + radius + 1; x++)
        for (int y = cy - radius - 1; y <= cy + radius + 1; y++)
        {
            if (!map.InInterior(x, y)) continue;
            int dx = x - cx, dy = y - cy;
            int d2 = dx * dx + dy * dy;
            if (d2 <= r2) map.Tiles[x, y].Type = TileType.Floor;
            else if (d2 <= outerR2) map.Tiles[x, y].Type = TileType.Wall;
        }
        // Door at south
        if (map.InBounds(cx, cy + radius + 1))
            map.Tiles[cx, cy + radius + 1].Type = TileType.Door;
    }

    internal static void BuildLRoom(GameMap map, int sx, int sy, int w, int h)
    {
        int cutW = w / 2, cutH = h / 2;
        for (int x = sx; x < sx + w; x++)
        for (int y = sy; y < sy + h; y++)
        {
            if (!map.InInterior(x, y)) continue;
            // Cut away top-right quadrant
            if (x >= sx + w - cutW && y < sy + cutH)
            {
                map.Tiles[x, y].Type = TileType.Wall;
                continue;
            }
            bool edge = x == sx || x == sx + w - 1 || y == sy || y == sy + h - 1
                || (x == sx + w - cutW && y < sy + cutH)
                || (y == sy + cutH - 1 && x >= sx + w - cutW);
            // Also wall the inner L corner edges
            if (x == sx + w - cutW - 1 && y < sy + cutH) edge = false; // keep floor
            map.Tiles[x, y].Type = edge ? TileType.Wall : TileType.Floor;
        }
        // Fix cut-quadrant interior corners as walls.
        for (int x = sx + w - cutW; x < sx + w; x++)
        for (int y = sy; y < sy + cutH; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Wall;
        // Re-lay walls on L-shape border
        for (int x = sx; x < sx + w - cutW; x++)
            if (map.InBounds(x, sy)) map.Tiles[x, sy].Type = TileType.Wall;
        for (int y = sy; y < sy + h; y++)
            if (map.InBounds(sx, y)) map.Tiles[sx, y].Type = TileType.Wall;
        for (int x = sx; x < sx + w; x++)
            if (map.InBounds(x, sy + h - 1)) map.Tiles[x, sy + h - 1].Type = TileType.Wall;
        for (int y = sy + cutH; y < sy + h; y++)
            if (map.InBounds(sx + w - 1, y)) map.Tiles[sx + w - 1, y].Type = TileType.Wall;
        for (int x = sx + w - cutW; x <= sx + w - 1; x++)
            if (map.InBounds(x, sy + cutH)) map.Tiles[x, sy + cutH].Type = TileType.Wall;
        for (int y = sy; y <= sy + cutH; y++)
            if (map.InBounds(sx + w - cutW, y)) map.Tiles[sx + w - cutW, y].Type = TileType.Wall;
        // Door at bottom center
        int doorX = sx + w / 2;
        if (map.InBounds(doorX, sy + h - 1))
            map.Tiles[doorX, sy + h - 1].Type = TileType.Door;
    }

    internal static void BuildCrossRoom(GameMap map, int cx, int cy, int armLength, int armWidth)
    {
        int halfW = armWidth / 2;
        // Horizontal arm
        for (int x = cx - armLength; x <= cx + armLength; x++)
        for (int y = cy - halfW; y <= cy + halfW; y++)
        {
            if (!map.InInterior(x, y)) continue;
            bool edge = x == cx - armLength || x == cx + armLength || y == cy - halfW || y == cy + halfW;
            map.Tiles[x, y].Type = edge ? TileType.Wall : TileType.Floor;
        }
        // Vertical arm
        for (int x = cx - halfW; x <= cx + halfW; x++)
        for (int y = cy - armLength; y <= cy + armLength; y++)
        {
            if (!map.InInterior(x, y)) continue;
            bool edge = x == cx - halfW || x == cx + halfW || y == cy - armLength || y == cy + armLength;
            // Don't overwrite floor from horizontal arm with wall
            if (map.Tiles[x, y].Type == TileType.Floor && !edge) continue;
            if (map.Tiles[x, y].Type == TileType.Floor && edge)
            {
                // Check if this tile is interior of the other arm — keep as floor
                bool inHArm = x >= cx - armLength && x <= cx + armLength && y >= cy - halfW && y <= cy + halfW;
                if (inHArm) continue;
            }
            map.Tiles[x, y].Type = edge ? TileType.Wall : TileType.Floor;
        }
        // Remove walls at arm intersections (interior cross)
        for (int x = cx - halfW + 1; x < cx + halfW; x++)
        for (int y = cy - halfW + 1; y < cy + halfW; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
        // Door at south arm tip
        if (map.InBounds(cx, cy + armLength))
            map.Tiles[cx, cy + armLength].Type = TileType.Door;
    }

    // Corridor decoration: sconces and cracked floors.
    // `campfireBudget` caps the wall-sconce campfires so the biome config's MinCampfires /
    // MaxCampfires hold. Uncapped, the 8%-per-Wall roll scales with Wall count while the map
    // shrinks, so sconce density would rise with depth; a campfire is a rest, cook and sleep-XP
    // point, so that would run the difficulty curve backwards.
    //
    // The budget suppresses the write only. Every rng draw still happens in the same order, so no
    // other feature on the floor moves.
    internal static void DecorateCorridors(GameMap map, int spawnX, int spawnY, Random rng,
        int campfireBudget = int.MaxValue)
    {
        int w = map.Width, h = map.Height;
        int firesPlaced = 0;
        var tiles = map.Tiles;

        // The two sweeps below have to run x-outer, because the rng draw order is part of the
        // floor. That is the worst order for GameMap's row-major tile grid, so they read a
        // column-major byte snapshot instead, which makes every read contiguous — the same
        // table-over-tile-grid trade the connectivity audit makes. It is filled row-major, which
        // is the order the tiles are actually stored in.
        // The snapshot is written back to whenever a tile is, so both sweeps see exactly what the
        // live grid would have shown them.
        var snap = new byte[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            snap[x * h + y] = (byte)tiles[x, y].Type;

        const byte Wall = (byte)TileType.Wall;
        const byte Path = (byte)TileType.Path;
        const byte Floor = (byte)TileType.Floor;

        // Sconces near walls adjacent to paths/floors
        for (int x = 1; x < w - 1; x++)
        {
            int b1 = x * h, b0 = b1 - h, b2 = b1 + h;
            bool nearSpawnX = Math.Abs(x - spawnX) < 8;
            for (int y = 1; y < h - 1; y++)
            {
                if (snap[b1 + y] != Wall) continue;
                if (nearSpawnX && Math.Abs(y - spawnY) < 8) continue;
                if (rng.Next(100) >= 8) continue;
                // Find an adjacent floor/path tile to place the campfire sconce, in
                // left/right/up/down order.
                if (TrySconce(map, snap, tiles, b0 + y, x - 1, y, rng, campfireBudget, ref firesPlaced)) continue;
                if (TrySconce(map, snap, tiles, b2 + y, x + 1, y, rng, campfireBudget, ref firesPlaced)) continue;
                if (TrySconce(map, snap, tiles, b1 + y - 1, x, y - 1, rng, campfireBudget, ref firesPlaced)) continue;
                TrySconce(map, snap, tiles, b1 + y + 1, x, y + 1, rng, campfireBudget, ref firesPlaced);
            }
        }
        // Cracked floor inside rooms (Floor tiles -> GrassSparse at 3%)
        for (int x = 1; x < w - 1; x++)
        {
            int b1 = x * h;
            for (int y = 1; y < h - 1; y++)
            {
                if (snap[b1 + y] == Floor && rng.Next(100) < 3)
                    tiles[x, y].Type = TileType.GrassSparse;
            }
        }

        // Returns true when this direction claimed the roll — i.e. it was a path/floor tile, which
        // ends the search whether or not the coin came up placing.
        static bool TrySconce(GameMap map, byte[] snap, GameMap.TileAccessor tiles, int idx,
            int nx, int ny, Random rng, int budget, ref int firesPlaced)
        {
            if (!map.InBounds(nx, ny)) return false;
            byte t = snap[idx];
            if (t != Path && t != Floor) return false;
            if (rng.Next(2) != 0) return false;
            if (firesPlaced < budget)
            {
                tiles[nx, ny].Type = TileType.Campfire;
                snap[idx] = (byte)TileType.Campfire;
                firesPlaced++;
            }
            return true;
        }
    }

    // Place water pools around fountains. Rivers come from RiverPass's drainage.
    internal static void PlaceWaterFeatures(GameMap map, int spawnX, int spawnY, Random rng)
    {
        int w = map.Width, h = map.Height;
        // Pools around fountains.
        //
        // The census runs row-major — the order GameMap stores tiles in — and the fountains are
        // then visited in column-major order. That ordering is load-bearing: each fountain's pool
        // draws from the stream and may write over ground a later fountain would otherwise have
        // claimed. Scanning row-major and sorting afterwards costs one small list and keeps the
        // scan a linear read.
        var fountains = new List<int>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (map.Tiles[x, y].Type == TileType.Fountain) fountains.Add(x * h + y);
        fountains.Sort();
        foreach (int key in fountains)
        {
            int x = key / h, y = key - x * h;
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int px = x + dx, py = y + dy;
                if (!map.InInterior(px, py)) continue;
                if (Math.Abs(px - spawnX) < 8 && Math.Abs(py - spawnY) < 8) continue;
                var t = map.Tiles[px, py].Type;
                if ((IsGrassType(t) || t == TileType.Floor || t == TileType.Path) && rng.NextDouble() < 0.5)
                    map.Tiles[px, py].Type = TileType.Water;
            }
        }
    }

    // Add pillars and campfires to boss stair room and spawn chamber.
    internal static void DecorateStairRooms(GameMap map, int spawnX, int spawnY, int bossX, int bossY, int bossW, int bossH)
    {
        // Boss room — 4 symmetrical pillars inside (only on Floor tiles).
        int bcx = bossX + bossW / 2, bcy = bossY + bossH / 2;
        StampCornerPillars(map, bcx, bcy, 3, 2, requireFloor: true);
        // Campfire rest point near boss room entrance
        if (map.InBounds(bcx - 1, bossY + bossH))
            map.Tiles[bcx - 1, bossY + bossH].Type = TileType.Campfire;

        // Spawn chamber — 4 pillars at corners (±3 from center), unconditional.
        StampCornerPillars(map, spawnX, spawnY, 3, 3, requireFloor: false);
        // 2 torch campfires flanking spawn
        if (map.InBounds(spawnX - 4, spawnY))
            map.Tiles[spawnX - 4, spawnY].Type = TileType.Campfire;
        if (map.InBounds(spawnX + 4, spawnY))
            map.Tiles[spawnX + 4, spawnY].Type = TileType.Campfire;
    }

    // Stamp 4 pillars at corners (cx±dx, cy±dy). requireFloor preserves walls.
    private static void StampCornerPillars(GameMap map, int cx, int cy, int dx, int dy, bool requireFloor)
    {
        (int X, int Y)[] corners = { (cx - dx, cy - dy), (cx + dx, cy - dy), (cx - dx, cy + dy), (cx + dx, cy + dy) };
        foreach (var (x, y) in corners)
        {
            if (!map.InBounds(x, y)) continue;
            if (requireFloor && map.Tiles[x, y].Type != TileType.Floor) continue;
            map.Tiles[x, y].Type = TileType.Pillar;
        }
    }
}
