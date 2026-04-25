using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// Floor terrain/structure/decoration. Entity + chest population lives in MapGenerator.Population.cs.
public static partial class MapGenerator
{
    // Persisted master seed (globalSeed persisted via SaveData) so F9 hot-reload
    // produces the same floor. SetGlobalSeed is called from load path or new-run init.
    public static int CurrentGlobalSeed { get; private set; } = Environment.TickCount;

    public static void SetGlobalSeed(int seed)
    {
        CurrentGlobalSeed = seed;
        UI.DebugLogger.LogGame("MAPGEN", $"CurrentGlobalSeed set to {seed}");
    }

    // Bundle 7: per-prefab-name MAX_PER_GAME tracker. Saved via SaveData.PrefabUseCounts;
    // loaded on save-load, cleared on new-run. NOT reset on floor change — per-game cap.
    private static Dictionary<string, int> _prefabUseCounts = new();

    public static Dictionary<string, int> GetCurrentPrefabUseCounts() => new(_prefabUseCounts);

    public static void SetPrefabUseCounts(Dictionary<string, int>? counts) =>
        _prefabUseCounts = counts ?? new();

    // Returns true if placement is allowed (and increments counter). maxPerGame <= 0 = unlimited.
    public static bool TryIncrementPrefabUse(string name, int maxPerGame)
    {
        if (maxPerGame <= 0) return true;
        int cur = _prefabUseCounts.TryGetValue(name, out int v) ? v : 0;
        if (cur >= maxPerGame) return false;
        _prefabUseCounts[name] = cur + 1;
        return true;
    }

    // Floor-generation entry point. Only F100 Ruby Palace bypasses; every other floor
    // (F1 included — SpecialAreaPass stamps the town overlay) runs the full 16-pass pipeline.
    public static (GameMap Map, List<Room> Rooms) GenerateFloor(int floorNumber, int width = 0, int height = 0)
    {
        if (width <= 0 || height <= 0)
            (width, height) = FloorScale.GetDimensions(floorNumber);

        UI.DebugLogger.LogGame("MAPGEN", $"GenerateFloor({floorNumber}) — {width}x{height} = {width * height} tiles");

        // F100 Ruby Palace — only floor that bypasses the pipeline.
        if (FloorScale.IsCastleFloor(floorNumber))
            return GenerateRubyPalace(floorNumber, width, height);

        var ctx = BuildContext(floorNumber, width, height);
        return Generation.GenerationPipeline.Run(ctx, StandardPasses);
    }

    private static Generation.WorldContext BuildContext(int floorNumber, int width, int height)
    {
        var biome = BiomeSystem.GetBiome(floorNumber);
        var config = Generation.BiomeGenConfigLoader.Get(biome);
        var map = new GameMap(width, height);
        // globalSeed persisted via SaveData.GlobalSeed — SetGlobalSeed from load / new-run init.
        int globalSeed = CurrentGlobalSeed;
        return new Generation.WorldContext(floorNumber, globalSeed, width, height, biome, config, map);
    }

    private static readonly IReadOnlyList<Generation.IGenerationPass> StandardPasses = new Generation.IGenerationPass[]
    {
        new Generation.Passes.SeedDerivationPass(),
        new Generation.Passes.HeightmapPass(),
        new Generation.Passes.PocketBiomePass(),
        new Generation.Passes.BaseTerrainPass(),
        new Generation.Passes.BorderPass(),
        new Generation.Passes.ClusterPass(),
        // Bundle 10 — vein placement seeds Wall tiles before lake/water might wash them.
        new Generation.Passes.OreVeinPlacementPass(),
        new Generation.Passes.LakePass(),
        new Generation.Passes.AquaticDepthPostPass(),
        new Generation.Passes.IcePostWaterPass(),
        new Generation.Passes.RiverPass(),
        new Generation.Passes.BiomeBlendPass(),
        new Generation.Passes.SpecialAreaPass(),
        new Generation.Passes.PrefabPlacementPass(),
        new Generation.Passes.FeatureScatterPass(),
        new Generation.Passes.AmbientOverlayPass(),
        new Generation.Passes.ConnectivityAuditPass(),
    };

    // F100 Ruby Palace: entrance hall, corridor, side chambers, throne room. All stone, no wilderness border.
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
        if (map.InBounds(cx, height - 3)) map.Tiles[cx, height - 3].Type = TileType.Door;
        if (map.InBounds(ehX + 1, ehY + 1)) map.Tiles[ehX + 1, ehY + 1].Type = TileType.Campfire;
        if (map.InBounds(ehX + ehW - 2, ehY + 1)) map.Tiles[ehX + ehW - 2, ehY + 1].Type = TileType.Campfire;

        int corrTop = 5, corrBot = ehY;
        for (int y = corrTop; y <= corrBot; y++)
            for (int dx = -1; dx <= 1; dx++)
                if (map.InBounds(cx + dx, y)) map.Tiles[cx + dx, y].Type = TileType.Floor;

        int lcX = cx - 14, lcY = cy - 3, lcW = 9, lcH = 7;
        if (lcX >= 1)
        {
            for (int x = lcX; x < lcX + lcW; x++)
                for (int y = lcY; y < lcY + lcH; y++)
                    if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
            rooms.Add(new Room(lcX, lcY, lcW, lcH));
            for (int x = lcX + lcW; x <= cx - 1; x++)
                if (map.InBounds(x, cy)) map.Tiles[x, cy].Type = TileType.Floor;
            if (map.InBounds(lcX + lcW / 2, lcY + lcH / 2))
                map.Tiles[lcX + lcW / 2, lcY + lcH / 2].Type = TileType.Shrine;
        }

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

        int trW = 17, trH = 9;
        int trX = cx - trW / 2, trY = 2;
        for (int x = trX; x < trX + trW; x++)
            for (int y = trY; y < trY + trH; y++)
                if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
        rooms.Add(new Room(trX, trY, trW, trH));
        if (map.InBounds(cx, trY + trH - 1)) map.Tiles[cx, trY + trH - 1].Type = TileType.Door;
        if (map.InBounds(trX + 2, trY + 1)) map.Tiles[trX + 2, trY + 1].Type = TileType.Campfire;
        if (map.InBounds(trX + trW - 3, trY + 1)) map.Tiles[trX + trW - 3, trY + 1].Type = TileType.Campfire;
        if (map.InBounds(trX + 2, trY + trH - 2)) map.Tiles[trX + 2, trY + trH - 2].Type = TileType.Campfire;
        if (map.InBounds(trX + trW - 3, trY + trH - 2)) map.Tiles[trX + trW - 3, trY + trH - 2].Type = TileType.Campfire;

        for (int y = corrTop + 2; y < corrBot - 1; y += 3)
        {
            if (map.InBounds(cx - 2, y)) map.Tiles[cx - 2, y].Type = TileType.Pillar;
            if (map.InBounds(cx + 2, y)) map.Tiles[cx + 2, y].Type = TileType.Pillar;
        }

        StampWallBorders(map);

        if (map.InBounds(cx, corrBot)) map.Tiles[cx, corrBot].Type = TileType.Door;

        map.RecountWalkableTiles();
        return (map, rooms);
    }

    // Re-stamps walls around every floor tile bordering non-floor/non-door (Ruby Palace room outlines).
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

    // Place a room shape (plain/circular/L/cross) at (cx,cy) with 2-tile buffer. No-op on overlap.
    internal static bool PlaceClearing(GameMap map, List<Room> rooms,
        List<(int x, int y)> clearings, int cx, int cy, Random rng)
    {
        const int halfSize = 5;
        if (!IsAreaFree(rooms, cx, cy, halfSize, halfSize)) return false;

        int shapeRoll = rng.Next(100);
        if (shapeRoll < 40)
        {
            ClearArea(map, cx, cy, 3, TileType.Grass);
            rooms.Add(new Room(cx - 3, cy - 3, 7, 7));
        }
        else if (shapeRoll < 65)
        {
            int radius = 3 + rng.Next(0, 2);
            BuildCircularRoom(map, cx, cy, radius);
            int d = radius + 1;
            rooms.Add(new Room(cx - d, cy - d, d * 2 + 1, d * 2 + 1));
        }
        else if (shapeRoll < 85)
        {
            int w = 7 + rng.Next(0, 3), h = 7 + rng.Next(0, 3);
            int lx = cx - w / 2, ly = cy - h / 2;
            BuildLRoom(map, lx, ly, w, h);
            rooms.Add(new Room(lx, ly, w, h));
        }
        else
        {
            int armLen = 3 + rng.Next(0, 2), armW = 3;
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

    // Mountain perimeter: single walker invoked 4x with different coord mappers.
    internal static void BuildJaggedMountainBorder(GameMap map, Random rng)
    {
        int w = map.Width, h = map.Height;
        WalkMountainEdge(map, w, (i, j) => (i, j), rng);
        WalkMountainEdge(map, w, (i, j) => (i, h - 1 - j), rng);
        WalkMountainEdge(map, h, (i, j) => (j, i), rng);
        WalkMountainEdge(map, h, (i, j) => (w - 1 - j, i), rng);
    }

    private static void WalkMountainEdge(GameMap map, int length, Func<int, int, (int X, int Y)> toCoord, Random rng)
    {
        const int maxThick = 4;
        int t = 1 + rng.Next(2);
        for (int i = 0; i < length; i++)
        {
            if (rng.Next(3) == 0)
                t = Math.Clamp(t + rng.Next(-1, 2), 1, maxThick);
            for (int j = 0; j < t; j++)
            {
                var (x, y) = toCoord(i, j);
                if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Mountain;
            }
        }
    }

    // 5x5 walled building via BuildStructure; rewrites default south door to requested side. doorSide: 0=N,1=E,2=S,3=W.
    private static void BuildTownBuilding(GameMap map, int cx, int cy, TileType feature, int doorSide)
    {
        int sx = cx - 2, sy = cy - 2;
        BuildStructure(map, sx, sy, 5, 5);

        if (doorSide != 2) map.Tiles[sx + 2, sy + 4].Type = TileType.Wall;

        var (doorX, doorY) = doorSide switch
        {
            0 => (sx + 2, sy),
            1 => (sx + 4, sy + 2),
            2 => (sx + 2, sy + 4),
            _ => (sx,     sy + 2),
        };
        if (map.InBounds(doorX, doorY)) map.Tiles[doorX, doorY].Type = TileType.Door;
        if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = feature;
    }

    // Cardinal building layout: spawn offset, feature tile, door side (0=N,1=E,2=S,3=W, facing plaza).
    private static readonly (int Dx, int Dy, TileType Feature, int DoorSide)[] TownBuildings =
    {
        ( 0, -8, TileType.BountyBoard, 2),
        ( 8,  0, TileType.Fountain,    3),
        ( 0,  8, TileType.Shrine,      0),
        (-8,  0, TileType.Anvil,       1),
    };

    // Spawn-town prefab: 4 cardinal buildings around spawn, connected by straight paths, 4 boundary campfires.
    internal static void BuildTown(GameMap map, int spawnX, int spawnY, List<Room> rooms)
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

    // Scatters trees/bushes over grass; skips spawn clearing + paths.
    internal static void ScatterGrassFoliage(GameMap map, int spawnX, int spawnY, int trees, int bushes, Random rng)
    {
        int w = map.Width, h = map.Height;
        const int townRadius = 14;
        for (int i = 0; i < trees; i++)
        {
            int x = rng.Next(3, w - 3), y = rng.Next(3, h - 3);
            if (Math.Abs(x - spawnX) < townRadius && Math.Abs(y - spawnY) < townRadius) continue;
            if (!IsGrassType(map.Tiles[x, y].Type)) continue;
            map.Tiles[x, y].Type = TileType.Tree;
        }
        for (int i = 0; i < bushes; i++)
        {
            int x = rng.Next(3, w - 3), y = rng.Next(3, h - 3);
            if (Math.Abs(x - spawnX) < townRadius && Math.Abs(y - spawnY) < townRadius) continue;
            if (!IsGrassType(map.Tiles[x, y].Type)) continue;
            map.Tiles[x, y].Type = TileType.Bush;
        }
    }

    internal static void ScatterHazards(GameMap map, int count, int spawnX, int spawnY, Random rng)
    {
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(8, map.Width - 8), y = rng.Next(8, map.Height - 8);
            if (!IsGrassType(map.Tiles[x, y].Type) && map.Tiles[x, y].Type != TileType.Path) continue;
            if (Math.Abs(x - spawnX) < 8 && Math.Abs(y - spawnY) < 8) continue;
            map.Tiles[x, y].Type = rng.Next(6) switch
            {
                0 => TileType.TrapTeleport, 1 => TileType.TrapPoison,
                2 => TileType.TrapAlarm, _ => TileType.TrapSpike,
            };
        }
    }

    internal static void ScatterVents(GameMap map, int count, int spawnX, int spawnY, Random rng)
    {
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(10, map.Width - 10), y = rng.Next(10, map.Height - 10);
            if (Math.Abs(x - spawnX) < 8 && Math.Abs(y - spawnY) < 8) continue;
            if (IsGrassType(map.Tiles[x, y].Type) || map.Tiles[x, y].Type == TileType.Floor)
                map.Tiles[x, y].Type = TileType.GasVent;
        }
    }

    // Repeatedly rolls an interior position; stamps `feature` where `accept` passes.
    internal static void ScatterFeature(GameMap map, int count, int maxTries,
        int edgeMargin, TileType feature, Func<int, int, bool> accept, Random rng)
    {
        for (int i = 0; i < count; i++)
        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            int x = rng.Next(edgeMargin, map.Width - edgeMargin);
            int y = rng.Next(edgeMargin, map.Height - edgeMargin);
            if (!accept(x, y)) continue;
            map.Tiles[x, y].Type = feature;
            break;
        }
    }

    // BFS from spawn; carves rescue paths to any unreachable clearing/room center.
    internal static void EnsureConnectivity(GameMap map, List<Room> rooms,
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
        for (int i = 1; i < clearings.Count; i++)
        {
            var (cx, cy) = clearings[i];
            if (!map.InBounds(cx, cy) || reached[cx, cy]) continue;
            CarveStraightPath(map, sx, sy, cx, cy);
        }
        foreach (var rm in rooms)
        {
            int cx = rm.X + rm.Width / 2, cy = rm.Y + rm.Height / 2;
            if (!map.InBounds(cx, cy) || reached[cx, cy]) continue;
            CarveStraightPath(map, sx, sy, cx, cy);
        }
    }
}
