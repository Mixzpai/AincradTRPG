using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// Floor generation -- terrain layout, structures, and decoration.
// Population (entities, chests) is in MapGenerator.Population.cs.
public static partial class MapGenerator
{
    public static (GameMap Map, List<Room> Rooms) GenerateFloor(int floorNumber, int width = 0, int height = 0)
    {
        // Aincrad scaling: Floor 1 is massive, shrinks as you climb.
        if (width <= 0 || height <= 0)
            (width, height) = FloorScale.GetDimensions(floorNumber);

        UI.DebugLogger.LogGame("MAPGEN", $"GenerateFloor({floorNumber}) — {width}x{height} = {width * height} tiles");
        var genSw = UI.DebugLogger.StartTimer($"GenerateFloor({floorNumber})");

        // Floor 100 — Ruby Palace: a tiny castle with a throne room.
        if (FloorScale.IsCastleFloor(floorNumber))
            return GenerateRubyPalace(floorNumber, width, height);

        var map = new GameMap(width, height);
        var rooms = new List<Room>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int r = Random.Shared.Next(100);
                map.Tiles[x, y].Type = r switch
                {
                    < 60 => TileType.Grass, < 80 => TileType.GrassTall,
                    < 90 => TileType.GrassSparse, < 95 => TileType.Flowers, _ => TileType.Grass,
                };
            }

        BuildJaggedMountainBorder(map);

        int treeClusters = FloorScale.TreeClusters(floorNumber);
        for (int i = 0; i < treeClusters; i++)
        {
            int cx = Random.Shared.Next(8, width - 8), cy = Random.Shared.Next(8, height - 8);
            int radius = Random.Shared.Next(3, 7);
            PlaceCluster(map, cx, cy, radius, TileType.Tree, 0.55);
            PlaceCluster(map, cx, cy, radius + 1, TileType.Bush, 0.15);
        }

        int rockClusters = FloorScale.RockClusters(floorNumber);
        for (int i = 0; i < rockClusters; i++)
        {
            int cx = Random.Shared.Next(10, width - 10), cy = Random.Shared.Next(10, height - 10);
            int radius = Random.Shared.Next(2, 4);
            PlaceCluster(map, cx, cy, radius, TileType.Rock, 0.3);
        }

        // Organic lakes — skip on very small floors where they'd dominate.
        int lakeCount = width >= 80 ? FloorScale.LakeCount(floorNumber) : 0;
        for (int i = 0; i < lakeCount; i++)
        {
            int cx = Random.Shared.Next(20, Math.Max(21, width - 20));
            int cy = Random.Shared.Next(15, Math.Max(16, height - 15));
            int radius = Random.Shared.Next(4, 7);
            GenerateLake(map, cx, cy, radius);
        }

        // Winding river — skip on tiny floors.
        if (width >= 80) GenerateRiver(map, floorNumber % 2 == 0);

        // Biome blending — smooths hard edges between terrain types so
        // forests fade through tall grass, rocks fade through sparse grass,
        // and lake shores have a beach-like fringe.
        BlendBiomes(map);

        int spawnX = width / 2, spawnY = height / 2;

        if (floorNumber == 1)
        {
            // Floor 1: Town of Beginnings — a walled city embedded in the
            // normal wilderness map. Clearings, boss room, paths etc. all
            // generate around it via the normal pipeline + overlap checks.
            var townRect = BuildTownOfBeginnings(map, rooms, spawnX, spawnY);
            map.SafeZone = townRect;
            rooms.Insert(0, new Room(spawnX - 4, spawnY - 4, 9, 9));
        }
        else
        {
            // Floors 2+: small 4-building cross around the spawn plaza.
            ClearArea(map, spawnX, spawnY, 12, TileType.Grass);
            for (int x = spawnX - 3; x <= spawnX + 3; x++)
                for (int y = spawnY - 3; y <= spawnY + 3; y++)
                    if (map.InBounds(x, y) && Random.Shared.Next(5) == 0)
                        map.Tiles[x, y].Type = TileType.Flowers;
            rooms.Add(new Room(spawnX - 5, spawnY - 5, 11, 11));
            BuildTown(map, spawnX, spawnY, rooms);
        }

        // Quadrant-based clearing distribution: 2 per quadrant guarantees
        // rooms spread across the whole map instead of random clustering.
        var clearings = new List<(int x, int y)> { (spawnX, spawnY) };
        var quadrants = new (int xMin, int xMax, int yMin, int yMax)[]
        {
            (15,            width / 2 - 6, 15,             height / 2 - 6), // NW
            (width / 2 + 6, width - 15,    15,             height / 2 - 6), // NE
            (15,            width / 2 - 6, height / 2 + 6, height - 15),    // SW
            (width / 2 + 6, width - 15,    height / 2 + 6, height - 15),    // SE
        };
        foreach (var q in quadrants)
        {
            if (q.xMax <= q.xMin || q.yMax <= q.yMin) continue;
            int countInQuad = FloorScale.ClearingsPerQuad(floorNumber);
            for (int i = 0; i < countInQuad; i++)
            {
                // Try up to 6 candidate positions before giving up — avoids
                // room overlap and keeps layouts clean.
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    int cx = Random.Shared.Next(q.xMin, q.xMax);
                    int cy = Random.Shared.Next(q.yMin, q.yMax);
                    if (PlaceClearing(map, rooms, clearings, cx, cy)) break;
                }
            }
        }

        for (int i = 0; i < clearings.Count - 1; i++)
            CarvePath(map, clearings[i].x, clearings[i].y, clearings[i + 1].x, clearings[i + 1].y);
        if (clearings.Count > 2)
            CarvePath(map, clearings[^1].x, clearings[^1].y, clearings[0].x, clearings[0].y);

        // Boss room — rotates through the 4 corners by floor number so every
        // run feels different and the map isn't always skewed to bottom-right.
        // Sizes shrink on smaller floors to fit within the map.
        int bossW = Math.Min(13, width / 4);
        int bossH = Math.Min(11, height / 4);
        int bossQuadrant = (floorNumber - 1) % 4;
        int edgePad = Math.Max(6, bossW + 2);
        int bossX = bossQuadrant switch
        {
            0 => Math.Max(edgePad, width - edgePad - bossW),
            1 => edgePad,
            2 => edgePad,
            _ => Math.Max(edgePad, width - edgePad - bossW),
        };
        int bossY = bossQuadrant switch
        {
            0 => Math.Max(edgePad, height - edgePad - bossH),
            1 => Math.Max(edgePad, height - edgePad - bossH),
            2 => edgePad,
            _ => edgePad,
        };
        BuildStructure(map, bossX, bossY, bossW, bossH);
        // Rotate the door to the side facing spawn so the path walks straight in.
        if (bossQuadrant <= 1)
        {
            // Boss is south of spawn — move door to top wall
            map.Tiles[bossX + bossW / 2, bossY].Type = TileType.Door;
            map.Tiles[bossX + bossW / 2, bossY + bossH - 1].Type = TileType.Wall;
        }
        // Quadrants 2/3 (boss north of spawn): default south door from BuildStructure is already correct
        rooms.Add(new Room(bossX, bossY, bossW, bossH));
        // Labyrinth entrance — the player must enter this to reach the
        // floor boss and the actual stairs to the next Aincrad floor.
        map.Tiles[bossX + bossW / 2, bossY + bossH / 2].Type = TileType.LabyrinthEntrance;
        CarveStraightPath(map, spawnX, spawnY, bossX + bossW / 2, bossY + bossH / 2);

        // Scatter trees/bushes EARLY so later feature placement (which only
        // targets grass) can't be overwritten. Excludes spawn area and path tiles.
        ScatterGrassFoliage(map, spawnX, spawnY,
            FloorScale.ScatteredTrees(floorNumber),
            FloorScale.ScatteredBushes(floorNumber));

        for (int i = 1; i < clearings.Count; i++)
        {
            if (Random.Shared.Next(3) == 0) continue;
            int fx = clearings[i].x + Random.Shared.Next(-2, 3);
            int fy = clearings[i].y + Random.Shared.Next(-2, 3);
            if (map.InBounds(fx, fy) && IsGrassType(map.Tiles[fx, fy].Type))
                map.Tiles[fx, fy].Type = TileType.Campfire;
        }

        // Lone Pillar landmark, far from town

        for (int attempt = 0; attempt < 20; attempt++)
        {
            int lx = Random.Shared.Next(15, width - 15), ly = Random.Shared.Next(15, height - 15);
            if (IsGrassType(map.Tiles[lx, ly].Type)
                && Math.Abs(lx - spawnX) > 20 && Math.Abs(ly - spawnY) > 20)
            { map.Tiles[lx, ly].Type = TileType.Pillar; break; }
        }

        int lavaPools = FloorScale.LavaPools(floorNumber);
        for (int i = 0; i < lavaPools; i++)
        {
            int lx = Random.Shared.Next(12, width - 12), ly = Random.Shared.Next(12, height - 12);
            if (Math.Abs(lx - spawnX) < 10 && Math.Abs(ly - spawnY) < 10) continue;
            PlaceCluster(map, lx, ly, Random.Shared.Next(1, 3), TileType.Lava, 0.5);
        }

        int loreCount = 1 + Random.Shared.Next(0, 2);
        ScatterFeature(map, loreCount, 20, 12, TileType.LoreStone,
            (x, y) => !(Math.Abs(x - spawnX) < 12 && Math.Abs(y - spawnY) < 12)
                && (IsGrassType(map.Tiles[x, y].Type) || map.Tiles[x, y].Type == TileType.Floor));

        // Journals — 2-3 per floor with lore text
        int journalCount = 2 + Random.Shared.Next(0, 2);
        ScatterFeature(map, journalCount, 20, 10, TileType.Journal,
            (x, y) => !(Math.Abs(x - spawnX) < 10 && Math.Abs(y - spawnY) < 10)
                && (IsGrassType(map.Tiles[x, y].Type) || map.Tiles[x, y].Type == TileType.Floor));

        // Enchant shrine — 1 per floor
        ScatterFeature(map, 1, 20, 15, TileType.EnchantShrine,
            (x, y) => (IsGrassType(map.Tiles[x, y].Type) || map.Tiles[x, y].Type == TileType.Floor)
                && Math.Abs(x - spawnX) > 12 && Math.Abs(y - spawnY) > 12);

        int dangerClusters = FloorScale.DangerClusters(floorNumber);
        for (int i = 0; i < dangerClusters; i++)
        {
            int dx = Random.Shared.Next(15, width - 15), dy = Random.Shared.Next(15, height - 15);
            if (Math.Abs(dx - spawnX) < 10 && Math.Abs(dy - spawnY) < 10) continue;
            PlaceCluster(map, dx, dy, 2, TileType.DangerZone, 0.5);
        }

        if (clearings.Count > 3)
        {
            var (mbx, mby) = clearings[clearings.Count / 2];
            int mbRoomX = mbx - 4, mbRoomY = mby - 3;
            BuildStructure(map, mbRoomX, mbRoomY, 9, 7);
            rooms.Add(new Room(mbRoomX, mbRoomY, 9, 7));
        }

        // Secret rooms — 1-2 per floor, each behind a cracked wall with a chest
        int secretCount = 1 + Random.Shared.Next(0, 2);
        for (int si = 0; si < secretCount && clearings.Count > 2; si++)
        {
            int ci = Random.Shared.Next(1, clearings.Count);
            var (scx, scy) = clearings[ci];
            // Pick a random side to attach the secret room
            int side = Random.Shared.Next(4);
            int srX = scx + (side == 0 ? 6 : side == 1 ? -9 : -2);
            int srY = scy + (side == 2 ? 6 : side == 3 ? -7 : -2);
            if (srX < 3 || srX + 5 >= width - 3 || srY < 3 || srY + 5 >= height - 3) continue;
            for (int sx = srX; sx < srX + 5; sx++)
                for (int sy = srY; sy < srY + 5; sy++)
                    map.Tiles[sx, sy].Type = (sx == srX || sx == srX + 4 || sy == srY || sy == srY + 4)
                        ? TileType.Wall : TileType.Floor;
            // Cracked wall entrance on the side facing the clearing
            int crackX = side switch { 0 => srX, 1 => srX + 4, _ => srX + 2 };
            int crackY = side switch { 2 => srY, 3 => srY + 4, _ => srY + 2 };
            map.Tiles[crackX, crackY].Type = TileType.CrackedWall;
            map.Tiles[srX + 2, srY + 2].Type = TileType.Chest;
        }

        ScatterHazards(map, FloorScale.TrapCount(floorNumber), spawnX, spawnY);
        ScatterVents(map, FloorScale.VentCount(floorNumber), spawnX, spawnY);

        // Lever/pressure plate puzzles — 1-2 per floor, each linked to a sealed door
        int puzzleCount = 1 + Random.Shared.Next(0, 2);
        for (int pi = 0; pi < puzzleCount; pi++)
        {
            // Find a wall tile to place a sealed door
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int dx = Random.Shared.Next(15, width - 15), dy = Random.Shared.Next(15, height - 15);
                if (Math.Abs(dx - spawnX) < 12 && Math.Abs(dy - spawnY) < 12) continue;
                if (map.Tiles[dx, dy].Type != TileType.Wall) continue;
                // Check adjacent floor on at least one side
                bool hasFloor = (map.InBounds(dx - 1, dy) && map.Tiles[dx - 1, dy].Type == TileType.Floor)
                             || (map.InBounds(dx + 1, dy) && map.Tiles[dx + 1, dy].Type == TileType.Floor)
                             || (map.InBounds(dx, dy - 1) && map.Tiles[dx, dy - 1].Type == TileType.Floor)
                             || (map.InBounds(dx, dy + 1) && map.Tiles[dx, dy + 1].Type == TileType.Floor);
                if (!hasFloor) continue;
                // Place a lever or pressure plate nearby
                bool useLever = Random.Shared.Next(2) == 0;
                for (int la = 0; la < 20; la++)
                {
                    int lx = dx + Random.Shared.Next(-8, 9), ly = dy + Random.Shared.Next(-8, 9);
                    if (!map.InBounds(lx, ly)) continue;
                    var lt = map.Tiles[lx, ly].Type;
                    if (lt != TileType.Floor && !IsGrassType(lt)) continue;
                    map.Tiles[lx, ly].Type = useLever ? TileType.Lever : TileType.PressurePlate;
                    map.Tiles[lx, ly].LinkedDoor = (dx, dy);
                    break;
                }
                break;
            }
        }

        DecorateCorridors(map, spawnX, spawnY);
        PlaceWaterFeatures(map, spawnX, spawnY);
        DecorateStairRooms(map, spawnX, spawnY, bossX, bossY, bossW, bossH);

        // Connectivity validation — rescue any unreachable clearing OR room
        EnsureConnectivity(map, rooms, clearings);

        UI.DebugLogger.EndTimer($"GenerateFloor({floorNumber})", genSw);
        UI.DebugLogger.LogGame("MAPGEN", $"  {rooms.Count} rooms, {clearings.Count} clearings");
        // Freeze the initial walkable count for GetExplorationPercent — no
        // runtime tile is counted at map-gen time because _explored is all false.
        map.RecountWalkableTiles();
        return (map, rooms);
    }

    // Floor 100 — The Ruby Palace. A small castle layout: entrance hall,
    // corridors, side chambers, and a grand throne room where the final
    // boss awaits. No wilderness, no mountain border, all stone.
    private static (GameMap Map, List<Room> Rooms) GenerateRubyPalace(int floor, int width, int height)
    {
        var map = new GameMap(width, height);
        var rooms = new List<Room>();

        // Fill with wall
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map.Tiles[x, y].Type = TileType.Wall;

        int cx = width / 2, cy = height / 2;

        // Entrance hall (bottom center)
        int ehW = 11, ehH = 7;
        int ehX = cx - ehW / 2, ehY = height - ehH - 2;
        for (int x = ehX; x < ehX + ehW; x++)
            for (int y = ehY; y < ehY + ehH; y++)
                if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
        rooms.Add(new Room(ehX, ehY, ehW, ehH));
        // Entrance door
        if (map.InBounds(cx, height - 3)) map.Tiles[cx, height - 3].Type = TileType.Door;
        // Campfire torches in entrance
        if (map.InBounds(ehX + 1, ehY + 1)) map.Tiles[ehX + 1, ehY + 1].Type = TileType.Campfire;
        if (map.InBounds(ehX + ehW - 2, ehY + 1)) map.Tiles[ehX + ehW - 2, ehY + 1].Type = TileType.Campfire;

        // Central corridor connecting entrance to throne room
        int corrTop = 5, corrBot = ehY;
        for (int y = corrTop; y <= corrBot; y++)
            for (int dx = -1; dx <= 1; dx++)
                if (map.InBounds(cx + dx, y)) map.Tiles[cx + dx, y].Type = TileType.Floor;

        // Left chamber
        int lcX = cx - 14, lcY = cy - 3, lcW = 9, lcH = 7;
        if (lcX >= 1)
        {
            for (int x = lcX; x < lcX + lcW; x++)
                for (int y = lcY; y < lcY + lcH; y++)
                    if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
            rooms.Add(new Room(lcX, lcY, lcW, lcH));
            // Connect to corridor
            for (int x = lcX + lcW; x <= cx - 1; x++)
                if (map.InBounds(x, cy)) map.Tiles[x, cy].Type = TileType.Floor;
            if (map.InBounds(lcX + lcW / 2, lcY + lcH / 2))
                map.Tiles[lcX + lcW / 2, lcY + lcH / 2].Type = TileType.Shrine;
        }

        // Right chamber
        int rcX = cx + 6, rcY = cy - 3, rcW = 9, rcH = 7;
        if (rcX + rcW < width - 1)
        {
            for (int x = rcX; x < rcX + rcW; x++)
                for (int y = rcY; y < rcY + rcH; y++)
                    if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
            rooms.Add(new Room(rcX, rcY, rcW, rcH));
            for (int x = cx + 1; x < rcX; x++)
                if (map.InBounds(x, cy)) map.Tiles[x, cy].Type = TileType.Floor;
            if (map.InBounds(rcX + rcW / 2, rcY + rcH / 2))
                map.Tiles[rcX + rcW / 2, rcY + rcH / 2].Type = TileType.Chest;
        }

        // Throne room (top center) — the final boss arena
        int trW = 17, trH = 9;
        int trX = cx - trW / 2, trY = 2;
        for (int x = trX; x < trX + trW; x++)
            for (int y = trY; y < trY + trH; y++)
                if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
        rooms.Add(new Room(trX, trY, trW, trH));
        // Throne room door
        if (map.InBounds(cx, trY + trH - 1)) map.Tiles[cx, trY + trH - 1].Type = TileType.Door;
        // Campfire torches flanking the throne
        if (map.InBounds(trX + 2, trY + 1)) map.Tiles[trX + 2, trY + 1].Type = TileType.Campfire;
        if (map.InBounds(trX + trW - 3, trY + 1)) map.Tiles[trX + trW - 3, trY + 1].Type = TileType.Campfire;
        if (map.InBounds(trX + 2, trY + trH - 2)) map.Tiles[trX + 2, trY + trH - 2].Type = TileType.Campfire;
        if (map.InBounds(trX + trW - 3, trY + trH - 2)) map.Tiles[trX + trW - 3, trY + trH - 2].Type = TileType.Campfire;

        // Pillars lining the corridor
        for (int y = corrTop + 2; y < corrBot - 1; y += 3)
        {
            if (map.InBounds(cx - 2, y)) map.Tiles[cx - 2, y].Type = TileType.Pillar;
            if (map.InBounds(cx + 2, y)) map.Tiles[cx + 2, y].Type = TileType.Pillar;
        }

        // Walls around rooms (re-stamp borders)
        StampWallBorders(map);

        // Place doors at corridor/room junctions
        if (map.InBounds(cx, corrBot)) map.Tiles[cx, corrBot].Type = TileType.Door;

        map.RecountWalkableTiles();
        return (map, rooms);
    }

    // Re-stamps wall tiles around every floor tile that borders non-floor/non-door.
    // Used by Ruby Palace to give carved rooms proper wall outlines.
    private static void StampWallBorders(GameMap map)
    {
        int w = map.Width, h = map.Height;
        var isOpen = new bool[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                var t = map.Tiles[x, y].Type;
                isOpen[x, y] = t == TileType.Floor || t == TileType.Door
                    || t == TileType.Campfire || t == TileType.Shrine
                    || t == TileType.Chest || t == TileType.Pillar;
            }
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (isOpen[x, y]) continue;
                bool adj = false;
                for (int dx = -1; dx <= 1 && !adj; dx++)
                    for (int dy = -1; dy <= 1 && !adj; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && isOpen[nx, ny])
                            adj = true;
                    }
                if (adj) map.Tiles[x, y].Type = TileType.Wall;
            }
    }

    // Attempts to place a room shape (plain/circular/L/cross) at (cx, cy)
    // with a 2-tile spacing buffer against existing rooms. Returns false
    // (no-op) if the area overlaps an existing room.
    private static bool PlaceClearing(GameMap map, List<Room> rooms,
        List<(int x, int y)> clearings, int cx, int cy)
    {
        const int halfSize = 5; // conservative half-extent for overlap test
        if (!IsAreaFree(rooms, cx, cy, halfSize, halfSize)) return false;

        int shapeRoll = Random.Shared.Next(100);
        if (shapeRoll < 40)
        {
            ClearArea(map, cx, cy, 3, TileType.Grass);
            rooms.Add(new Room(cx - 3, cy - 3, 7, 7));
        }
        else if (shapeRoll < 65)
        {
            int radius = 3 + Random.Shared.Next(0, 2);
            BuildCircularRoom(map, cx, cy, radius);
            int d = radius + 1;
            rooms.Add(new Room(cx - d, cy - d, d * 2 + 1, d * 2 + 1));
        }
        else if (shapeRoll < 85)
        {
            int w = 7 + Random.Shared.Next(0, 3), h = 7 + Random.Shared.Next(0, 3);
            int lx = cx - w / 2, ly = cy - h / 2;
            BuildLRoom(map, lx, ly, w, h);
            rooms.Add(new Room(lx, ly, w, h));
        }
        else
        {
            int armLen = 3 + Random.Shared.Next(0, 2), armW = 3;
            BuildCrossRoom(map, cx, cy, armLen, armW);
            int d = armLen + 1;
            rooms.Add(new Room(cx - d, cy - d, d * 2 + 1, d * 2 + 1));
        }
        clearings.Add((cx, cy));
        return true;
    }

    // Bounding box non-overlap test with a 2-tile spacing buffer.
    private static bool IsAreaFree(List<Room> rooms, int cx, int cy, int halfW, int halfH)
    {
        const int buffer = 2;
        int l = cx - halfW, r = cx + halfW, t = cy - halfH, b = cy + halfH;
        foreach (var rm in rooms)
        {
            int rl = rm.X - buffer, rr = rm.X + rm.Width + buffer;
            int rt = rm.Y - buffer, rb = rm.Y + rm.Height + buffer;
            if (l < rr && r > rl && t < rb && b > rt) return false;
        }
        return true;
    }

    // Mountain perimeter with drifting thickness along each edge — a single
    // walker is invoked four times with different coord mappers so every
    // edge feels like a natural mountain range rather than a ruled line.
    private static void BuildJaggedMountainBorder(GameMap map)
    {
        int w = map.Width, h = map.Height;
        WalkMountainEdge(map, w, (i, j) => (i, j));             // Top
        WalkMountainEdge(map, w, (i, j) => (i, h - 1 - j));     // Bottom
        WalkMountainEdge(map, h, (i, j) => (j, i));             // Left
        WalkMountainEdge(map, h, (i, j) => (w - 1 - j, i));     // Right
    }

    private static void WalkMountainEdge(GameMap map, int length, Func<int, int, (int X, int Y)> toCoord)
    {
        const int maxThick = 4;
        int t = 1 + Random.Shared.Next(2);
        for (int i = 0; i < length; i++)
        {
            if (Random.Shared.Next(3) == 0)
                t = Math.Clamp(t + Random.Shared.Next(-1, 2), 1, maxThick);
            for (int j = 0; j < t; j++)
            {
                var (x, y) = toCoord(i, j);
                if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Mountain;
            }
        }
    }

    // 5x5 walled town building: reuses BuildStructure for the walls/floor,
    // then rewrites the default south door into the requested side and
    // stamps the feature tile at the exact center.
    // doorSide: 0=N, 1=E, 2=S, 3=W.
    private static void BuildTownBuilding(GameMap map, int cx, int cy, TileType feature, int doorSide)
    {
        int sx = cx - 2, sy = cy - 2;
        BuildStructure(map, sx, sy, 5, 5);

        // BuildStructure always places a south door — revert it if we need another side.
        if (doorSide != 2) map.Tiles[sx + 2, sy + 4].Type = TileType.Wall;

        var (doorX, doorY) = doorSide switch
        {
            0 => (sx + 2, sy),       // N
            1 => (sx + 4, sy + 2),   // E
            2 => (sx + 2, sy + 4),   // S
            _ => (sx,     sy + 2),   // W
        };
        if (map.InBounds(doorX, doorY)) map.Tiles[doorX, doorY].Type = TileType.Door;
        if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = feature;
    }

    // Cardinal building layout: offset from spawn, feature tile, and which
    // wall the door sits on (0=N, 1=E, 2=S, 3=W, facing the central plaza).
    private static readonly (int Dx, int Dy, TileType Feature, int DoorSide)[] TownBuildings =
    {
        ( 0, -8, TileType.BountyBoard, 2),
        ( 8,  0, TileType.Fountain,    3),
        ( 0,  8, TileType.Shrine,      0),
        (-8,  0, TileType.Anvil,       1),
    };

    // Lay out the spawn-town prefab — four 5x5 walled buildings in a cardinal
    // cross around spawn, each with a door facing the central plaza and
    // connected via a straight path. Four boundary campfires mark the
    // town edge as a visual frame.
    private static void BuildTown(GameMap map, int spawnX, int spawnY, List<Room> rooms)
    {
        foreach (var (dx, dy, feature, door) in TownBuildings)
        {
            int cx = spawnX + dx, cy = spawnY + dy;
            BuildTownBuilding(map, cx, cy, feature, door);
            rooms.Add(new Room(cx - 2, cy - 2, 5, 5));
            CarveStraightPath(map, spawnX, spawnY, spawnX + Math.Sign(dx) * 6, spawnY + Math.Sign(dy) * 6);
        }

        (int dx, int dy)[] corners = { (-11, -11), (11, -11), (-11, 11), (11, 11) };
        foreach (var (dx, dy) in corners)
            if (map.InBounds(spawnX + dx, spawnY + dy))
                map.Tiles[spawnX + dx, spawnY + dy].Type = TileType.Campfire;
    }

    // Scatters individual trees and bushes over open grass, skipping the
    // spawn clearing and path tiles to avoid obliterating structured areas.
    private static void ScatterGrassFoliage(GameMap map, int spawnX, int spawnY, int trees, int bushes)
    {
        int w = map.Width, h = map.Height;
        const int townRadius = 14;
        for (int i = 0; i < trees; i++)
        {
            int x = Random.Shared.Next(3, w - 3), y = Random.Shared.Next(3, h - 3);
            if (Math.Abs(x - spawnX) < townRadius && Math.Abs(y - spawnY) < townRadius) continue;
            if (!IsGrassType(map.Tiles[x, y].Type)) continue;
            map.Tiles[x, y].Type = TileType.Tree;
        }
        for (int i = 0; i < bushes; i++)
        {
            int x = Random.Shared.Next(3, w - 3), y = Random.Shared.Next(3, h - 3);
            if (Math.Abs(x - spawnX) < townRadius && Math.Abs(y - spawnY) < townRadius) continue;
            if (!IsGrassType(map.Tiles[x, y].Type)) continue;
            map.Tiles[x, y].Type = TileType.Bush;
        }
    }

    // Trap scatter: pick (x,y); if tile ok and far from spawn, roll variant.
    private static void ScatterHazards(GameMap map, int count, int spawnX, int spawnY)
    {
        for (int i = 0; i < count; i++)
        {
            int x = Random.Shared.Next(8, map.Width - 8), y = Random.Shared.Next(8, map.Height - 8);
            if (!IsGrassType(map.Tiles[x, y].Type) && map.Tiles[x, y].Type != TileType.Path) continue;
            if (Math.Abs(x - spawnX) < 8 && Math.Abs(y - spawnY) < 8) continue;
            map.Tiles[x, y].Type = Random.Shared.Next(6) switch
            {
                0 => TileType.TrapTeleport, 1 => TileType.TrapPoison,
                2 => TileType.TrapAlarm, _ => TileType.TrapSpike,
            };
        }
    }

    // Vent scatter: pick (x,y), distance check first, then tile type.
    private static void ScatterVents(GameMap map, int count, int spawnX, int spawnY)
    {
        for (int i = 0; i < count; i++)
        {
            int x = Random.Shared.Next(10, map.Width - 10), y = Random.Shared.Next(10, map.Height - 10);
            if (Math.Abs(x - spawnX) < 8 && Math.Abs(y - spawnY) < 8) continue;
            if (IsGrassType(map.Tiles[x, y].Type) || map.Tiles[x, y].Type == TileType.Floor)
                map.Tiles[x, y].Type = TileType.GasVent;
        }
    }

    // Repeatedly rolls an interior position; stamps `feature` where `accept` passes.
    private static void ScatterFeature(GameMap map, int count, int maxTries,
        int edgeMargin, TileType feature, Func<int, int, bool> accept)
    {
        for (int i = 0; i < count; i++)
        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            int x = Random.Shared.Next(edgeMargin, map.Width - edgeMargin);
            int y = Random.Shared.Next(edgeMargin, map.Height - edgeMargin);
            if (!accept(x, y)) continue;
            map.Tiles[x, y].Type = feature;
            break;
        }
    }

    // BFS flood-fill from spawn over walkable tiles. Carves rescue paths to
    // any unreachable clearing OR room center so no region is ever orphaned.
    // Walled rooms count as reachable as long as their door is — the BFS
    // walks through Door tiles, so the interior is automatically included.
    private static void EnsureConnectivity(GameMap map, List<Room> rooms,
        List<(int x, int y)> clearings)
    {
        if (clearings.Count == 0) return;
        int w = map.Width, h = map.Height;
        var reached = new bool[w, h];
        var queue = new Queue<(int, int)>();
        var (sx, sy) = clearings[0];
        queue.Enqueue((sx, sy));
        reached[sx, sy] = true;
        int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (!map.InBounds(nx, ny) || reached[nx, ny]) continue;
                var t = map.Tiles[nx, ny].Type;
                if (t == TileType.Wall || t == TileType.Mountain || t == TileType.Tree
                    || t == TileType.TreePine || t == TileType.Rock || t == TileType.WaterDeep
                    || t == TileType.Lava) continue;
                reached[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }
        // Rescue any isolated clearing
        for (int i = 1; i < clearings.Count; i++)
        {
            var (cx, cy) = clearings[i];
            if (!map.InBounds(cx, cy) || reached[cx, cy]) continue;
            CarveStraightPath(map, sx, sy, cx, cy);
        }
        // Rescue any isolated room center (catches walled rooms + boss)
        foreach (var rm in rooms)
        {
            int cx = rm.X + rm.Width / 2, cy = rm.Y + rm.Height / 2;
            if (!map.InBounds(cx, cy) || reached[cx, cy]) continue;
            CarveStraightPath(map, sx, sy, cx, cy);
        }
    }
}
