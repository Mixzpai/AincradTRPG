namespace SAOTRPG.Map;

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
        // Fix: ensure the cut quadrant interior corners are walls
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

    // Carve a water stream path between two points.
    internal static void CarveWaterPath(GameMap map, int x1, int y1, int x2, int y2, int width)
    {
        int x = x1, y = y1;
        while (x != x2 || y != y2)
        {
            for (int dx = 0; dx < width; dx++)
            for (int dy = 0; dy < width; dy++)
            {
                int px = x + dx, py = y + dy;
                if (map.InInterior(px, py)
                    && map.Tiles[px, py].Type != TileType.Wall && map.Tiles[px, py].Type != TileType.Door
                    && map.Tiles[px, py].Type != TileType.StairsUp && map.Tiles[px, py].Type != TileType.StairsDown)
                    map.Tiles[px, py].Type = TileType.Water;
            }
            int ddx = Math.Sign(x2 - x), ddy = Math.Sign(y2 - y);
            if (ddx != 0 && ddy != 0)
            {
                if (Random.Shared.Next(2) == 0) x += ddx; else y += ddy;
            }
            else { x += ddx; y += ddy; }
        }
    }

    // Corridor decoration: sconces and cracked floors.
    internal static void DecorateCorridors(GameMap map, int spawnX, int spawnY)
    {
        int w = map.Width, h = map.Height;
        // Sconces near walls adjacent to paths/floors
        for (int x = 1; x < w - 1; x++)
        for (int y = 1; y < h - 1; y++)
        {
            if (map.Tiles[x, y].Type != TileType.Wall) continue;
            if (Math.Abs(x - spawnX) < 8 && Math.Abs(y - spawnY) < 8) continue;
            if (Random.Shared.Next(100) >= 8) continue;
            // Find an adjacent floor/path tile to place the campfire sconce
            int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
            for (int d = 0; d < 4; d++)
            {
                int nx = x + dxs[d], ny = y + dys[d];
                if (!map.InBounds(nx, ny)) continue;
                var t = map.Tiles[nx, ny].Type;
                if ((t == TileType.Path || t == TileType.Floor) && Random.Shared.Next(2) == 0)
                {
                    map.Tiles[nx, ny].Type = TileType.Campfire;
                    break;
                }
            }
        }
        // Cracked floor inside rooms (Floor tiles -> GrassSparse at 3%)
        for (int x = 1; x < w - 1; x++)
        for (int y = 1; y < h - 1; y++)
        {
            if (map.Tiles[x, y].Type == TileType.Floor && Random.Shared.Next(100) < 3)
                map.Tiles[x, y].Type = TileType.GrassSparse;
        }
    }

    // Place water pools around fountains and 0-1 underground streams.
    internal static void PlaceWaterFeatures(GameMap map, int spawnX, int spawnY)
    {
        int w = map.Width, h = map.Height;
        // Pools around fountains
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (map.Tiles[x, y].Type != TileType.Fountain) continue;
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int px = x + dx, py = y + dy;
                if (!map.InInterior(px, py)) continue;
                if (Math.Abs(px - spawnX) < 8 && Math.Abs(py - spawnY) < 8) continue;
                var t = map.Tiles[px, py].Type;
                if ((IsGrassType(t) || t == TileType.Floor || t == TileType.Path) && Random.Shared.NextDouble() < 0.5)
                    map.Tiles[px, py].Type = TileType.Water;
            }
        }
        // 0-1 underground streams
        if (Random.Shared.Next(2) == 0)
        {
            int streamWidth = 1 + Random.Shared.Next(2);
            bool horizontal = Random.Shared.Next(2) == 0;
            int x1, y1, x2, y2;
            if (horizontal)
            {
                x1 = 2; y1 = Random.Shared.Next(10, h - 10);
                x2 = w - 3; y2 = Random.Shared.Next(10, h - 10);
            }
            else
            {
                x1 = Random.Shared.Next(10, w - 10); y1 = 2;
                x2 = Random.Shared.Next(10, w - 10); y2 = h - 3;
            }
            CarveWaterPath(map, x1, y1, x2, y2, streamWidth);
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
