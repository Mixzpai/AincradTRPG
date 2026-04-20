namespace SAOTRPG.Map;

// BSP labyrinth: separate denser-than-overworld map, wall-filled, carved into rooms + L-corridors.
// Southmost room = LabyrinthEntrance (exit); Northmost = boss room with StairsUp.
public static partial class MapGenerator
{
    private const int LabWidth  = 80;
    private const int LabHeight = 60;
    private const int MinLeaf   = 10;

    // Returns (map, rooms) with rooms[0]=entrance, rooms[^1]=boss room.
    public static (GameMap Map, List<Room> Rooms) GenerateLabyrinth(int floor)
    {
        var map = new GameMap(LabWidth, LabHeight);

        // Fill everything with Wall — rooms and corridors are carved out.
        for (int x = 0; x < LabWidth; x++)
        for (int y = 0; y < LabHeight; y++)
            map.Tiles[x, y].Type = TileType.Wall;

        // BSP partition into leaf nodes.
        var leaves = new List<(int X, int Y, int W, int H)>();
        SplitBsp(2, 2, LabWidth - 4, LabHeight - 4, leaves);

        // Carve a room inside each leaf.
        var rooms = new List<Room>();
        foreach (var (lx, ly, lw, lh) in leaves)
        {
            int rw = Random.Shared.Next(Math.Max(4, lw / 2), lw - 1);
            int rh = Random.Shared.Next(Math.Max(4, lh / 2), lh - 1);
            int rx = lx + Random.Shared.Next(1, Math.Max(2, lw - rw));
            int ry = ly + Random.Shared.Next(1, Math.Max(2, lh - rh));
            CarveRoom(map, rx, ry, rw, rh);
            rooms.Add(new Room(rx, ry, rw, rh));
        }

        // Connect every pair of adjacent rooms with an L-shaped corridor.
        for (int i = 0; i < rooms.Count - 1; i++)
            CarveCorridorL(map, rooms[i].CenterX, rooms[i].CenterY,
                                rooms[i + 1].CenterX, rooms[i + 1].CenterY);

        // Sort by Y so southernmost = entrance, northernmost = boss room.
        rooms.Sort((a, b) => b.CenterY.CompareTo(a.CenterY));

        // Entrance (south) — LabyrinthEntrance tile to exit back to overworld.
        var entrance = rooms[0];
        map.Tiles[entrance.CenterX, entrance.CenterY].Type = TileType.LabyrinthEntrance;

        // Boss room (north) — StairsUp guarded by the boss.
        var bossRoom = rooms[^1];
        map.Tiles[bossRoom.CenterX, bossRoom.CenterY].Type = TileType.StairsUp;

        // Scatter campfire light sources for atmosphere.
        for (int i = 1; i < rooms.Count - 1; i++)
        {
            if (Random.Shared.Next(3) == 0)
            {
                int cx = rooms[i].X + 1, cy = rooms[i].Y + 1;
                if (map.InBounds(cx, cy) && map.Tiles[cx, cy].Type == TileType.Floor)
                    map.Tiles[cx, cy].Type = TileType.Campfire;
            }
        }

        // Traps in corridors — spike traps on random floor tiles.
        int trapCount = 3 + floor * 2;
        for (int i = 0; i < trapCount; i++)
        {
            int tx = Random.Shared.Next(3, LabWidth - 3);
            int ty = Random.Shared.Next(3, LabHeight - 3);
            if (map.Tiles[tx, ty].Type == TileType.Floor)
            {
                // Don't trap the entrance or boss room centers.
                if (Math.Abs(tx - entrance.CenterX) < 3 && Math.Abs(ty - entrance.CenterY) < 3) continue;
                if (Math.Abs(tx - bossRoom.CenterX) < 3 && Math.Abs(ty - bossRoom.CenterY) < 3) continue;
                map.Tiles[tx, ty].Type = TileType.TrapSpike;
            }
        }

        map.RecountWalkableTiles();
        return (map, rooms);
    }

    // ── BSP helpers ──────────────────────────────────────────────────

    private static void SplitBsp(int x, int y, int w, int h, List<(int, int, int, int)> leaves)
    {
        if (w < MinLeaf * 2 && h < MinLeaf * 2)
        {
            leaves.Add((x, y, w, h));
            return;
        }

        bool splitH = w < MinLeaf * 2 ? true
                     : h < MinLeaf * 2 ? false
                     : Random.Shared.Next(2) == 0;

        if (splitH)
        {
            int splitY = y + Random.Shared.Next(MinLeaf, Math.Max(MinLeaf + 1, h - MinLeaf));
            SplitBsp(x, y, w, splitY - y, leaves);
            SplitBsp(x, splitY, w, y + h - splitY, leaves);
        }
        else
        {
            int splitX = x + Random.Shared.Next(MinLeaf, Math.Max(MinLeaf + 1, w - MinLeaf));
            SplitBsp(x, y, splitX - x, h, leaves);
            SplitBsp(splitX, y, x + w - splitX, h, leaves);
        }
    }

    private static void CarveRoom(GameMap map, int rx, int ry, int rw, int rh)
    {
        for (int x = rx; x < rx + rw && x < map.Width; x++)
        for (int y = ry; y < ry + rh && y < map.Height; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
    }

    private static void CarveCorridorL(GameMap map, int x1, int y1, int x2, int y2)
    {
        // L-shaped: go horizontal first, then vertical.
        int cx = x1, cy = y1;
        int sx = x1 < x2 ? 1 : -1;
        while (cx != x2)
        {
            if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = TileType.Floor;
            cx += sx;
        }
        int sy = y1 < y2 ? 1 : -1;
        while (cy != y2)
        {
            if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = TileType.Floor;
            cy += sy;
        }
        if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = TileType.Floor;
    }
}
