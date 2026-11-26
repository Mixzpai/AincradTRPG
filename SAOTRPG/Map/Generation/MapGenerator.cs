
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation;

// Floor terrain/structure/decoration. Entity + chest population lives in MapGenerator.Population.cs.
public static partial class MapGenerator
{
    // Persisted master seed (globalSeed persisted via SaveData) so F9 hot-reload
    // produces the same floor. SetGlobalSeed is called from load path or new-run init.
    public static int CurrentGlobalSeed { get; private set; } = Environment.TickCount;

    public static void SetGlobalSeed(int seed)
    {
        CurrentGlobalSeed = seed;
        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN", $"CurrentGlobalSeed set to {seed}");
    }

    // Per-prefab-name MAX_PER_GAME tracker. Saved via SaveData.PrefabUseCounts;
    // loaded on save-load, cleared on new-run. NOT reset on floor change — per-game cap.
    private static Dictionary<string, int> _prefabUseCounts = new();
    // The ledger before the current floor was charged. A save stores this, because loading rebuilds that floor.
    private static Dictionary<string, int> _floorStartCounts = new();

    public static Dictionary<string, int> GetCurrentPrefabUseCounts() => new(_prefabUseCounts);
    public static Dictionary<string, int> GetFloorStartPrefabUseCounts() => new(_floorStartCounts);

    public static void SetPrefabUseCounts(Dictionary<string, int>? counts) =>
        _prefabUseCounts = _floorStartCounts = counts ?? new();

    // The cap ledger is CHARGED THROUGH WorldContext, not here. A generation takes a copy, spends
    // against it, and commits only when its floor is adopted — so a floor built ahead of the
    // player and then discarded leaves no trace, and two generations can never share the
    // dictionary. There is deliberately no static charge method: the previous one was an
    // unsynchronized Dictionary write reachable from a pass, and eight threads on it lose ~10% of
    // their increments and intermittently corrupt the collection outright.
    private static void CommitPrefabUse(Dictionary<string, int> counts)
    {
        _floorStartCounts = _prefabUseCounts;
        _prefabUseCounts = counts;
    }

    // Floor-generation entry point. Only F100 Ruby Palace bypasses; every other floor
    // (F1 included — SpecialAreaPass stamps the town overlay) runs the full 16-pass pipeline.
    public static (GameMap Map, List<Room> Rooms) GenerateFloor(int floorNumber, int width = 0, int height = 0)
    {
        bool defaultDims = width <= 0 || height <= 0;
        if (defaultDims)
            (width, height) = FloorScale.GetDimensions(floorNumber);

        // A floor built ahead of the player is adopted here, which is what keeps the whole feature
        // out of the transition code: AscendFloor is unchanged and simply finds the work done.
        // Take joins any in-flight build even when it cannot use it, so a generation never runs
        // beside another one. Only the default-dimension form can be served — a caller asking for
        // its own size is asking for a different map.
        if (defaultDims)
        {
            var ready = FloorPrefetch.Take(floorNumber);
            if (ready != null) return ready.Value;
        }

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN", $"GenerateFloor({floorNumber}) — {width}x{height} = {width * height} tiles");

        // F100 Ruby Palace — only floor that bypasses the pipeline.
        if (FloorScale.IsCastleFloor(floorNumber))
            return GenerateRubyPalace(floorNumber, width, height);

        var ctx = BuildContext(floorNumber, width, height, CurrentGlobalSeed);
        var result = Generation.GenerationPipeline.Run(ctx, StandardPasses);
        CommitPrefabUse(ctx.PrefabUseCounts);
        return result;
    }

    // The pipeline half of GenerateFloor with the ledger commit left to the caller, so a floor can
    // be built ahead of time and only charge its caps if it is actually adopted.
    //
    // SAFE OFF THE UI THREAD because it WRITES no shared state: the context owns everything the
    // passes produce, including the cap ledger. What it reads — the seed, the biome table and the
    // prefab library — is immutable once populated, and FloorPrefetch.Request warms the two lazy
    // caches before queueing any work. It draws nothing from the gameplay stream either; SeedProbe
    // section 38 asserts that across all 100 floors, because a draw here would race the turn loop.
    // Note this does NOT handle floors that bypass the pipeline: F100 has no detached form, and
    // Request refuses it.
    internal static (GameMap Map, List<Room> Rooms, Generation.WorldContext Ctx) GenerateFloorDetached(
        int floorNumber, int width, int height, int seed)
    {
        var ctx = BuildContext(floorNumber, width, height, seed);
        var (map, rooms) = Generation.GenerationPipeline.Run(ctx, StandardPasses);
        return (map, rooms, ctx);
    }

    // Adopt a detached generation: its scoped ledger becomes the game's.
    internal static void AdoptDetached(Dictionary<string, int> counts) => CommitPrefabUse(counts);

    // `seed` is EXPLICIT rather than read from the static here, because a detached build runs on a
    // worker: reading CurrentGlobalSeed inside the build lets a new run's seed land mid-generation,
    // so the floor would be built from one seed while the prefetch recorded another. Harmless today
    // (a seed change goes through Invalidate, which discards the result) but the two must not be
    // able to disagree. The synchronous path passes the same static it always read.
    private static Generation.WorldContext BuildContext(int floorNumber, int width, int height, int seed)
    {
        var biome = BiomeSystem.GetBiome(floorNumber);
        var config = Generation.BiomeGenConfigLoader.Get(biome);
        var map = new GameMap(width, height);
        return new Generation.WorldContext(floorNumber, seed, width, height, biome, config, map)
        {
            // A copy, so a generation that is never adopted cannot advance the game's caps.
            PrefabUseCounts = new Dictionary<string, int>(_prefabUseCounts),
        };
    }

    private static readonly IReadOnlyList<Generation.IGenerationPass> StandardPasses = new Generation.IGenerationPass[]
    {
        new Generation.Passes.SeedDerivationPass(),
        new Generation.Passes.HeightmapPass(),
        new Generation.Passes.PocketBiomePass(),
        new Generation.Passes.BaseTerrainPass(),
        new Generation.Passes.BorderPass(),
        new Generation.Passes.ClusterPass(),
        // Vein placement seeds Wall tiles before lake/water might wash them.
        new Generation.Passes.OreVeinPlacementPass(),
        new Generation.Passes.LakePass(),
        new Generation.Passes.AquaticDepthPostPass(),
        new Generation.Passes.RiverPass(),
        // After the river, so river water freezes too.
        new Generation.Passes.IcePostWaterPass(),
        new Generation.Passes.BiomeBlendPass(),
        new Generation.Passes.SpecialAreaPass(),
        // The three prefab passes run most-specific first, because open ground is scarce on a
        // cave biome and whoever asks first gets the pick of it: the floor's own canon set-piece,
        // then the biome's landmark, then the general weighted draw working around both.
        new Generation.Passes.CanonOverlayPass(),
        new Generation.Passes.SignaturePrefabPass(),
        new Generation.Passes.PrefabPlacementPass(),
        new Generation.Passes.FeatureScatterPass(),
        // AmbientOverlayPass was here and is gone. It seeded biome particles straight into
        // ParticleQueue — a render system — from inside generation, read a UI setting to decide
        // how many, and drew ~500k rng values doing it. The atmosphere is maintained around the
        // camera by MapView now; generation has no opinion about it.
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
        // The way out of Aincrad. GameWon fires only from AscendFloor, which is reachable only by
        // stepping on StairsUp -- and the palace had none, so the game could not be finished at all.
        // Placed at the throne itself, which is also where the player-clone is posted.
        if (map.InBounds(cx, trY + trH / 2)) map.Tiles[cx, trY + trH / 2].Type = TileType.StairsUp;
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
                // EXITS COUNT AS OPEN. Omitting them meant a StairsUp written above was judged
                // "not open", found to touch floor, and WALLED OVER — the tile was created and
                // destroyed within the same function. Same shape as the archway that FeatureScatter
                // carved away with its own approach road: a whitelist that does not know about a
                // tile silently deletes it.
                isOpen[x, y] = t == TileType.Floor || t == TileType.Door
                    || t == TileType.Campfire || t == TileType.Shrine
                    || t == TileType.Chest || t == TileType.Pillar
                    || t == TileType.StairsUp || t == TileType.StairsDown
                    || t == TileType.LabyrinthEntrance;
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

    // Blocking table for EnsureConnectivity, indexed by tile type. DERIVED from the predicate
    // below rather than hand-listed, so the two cannot drift; a table load replaces a seven-way
    // pattern match run once per cell of the floor.
    // NOTE this list is NOT the same as ConnectivityAuditPass.IsAuditBlocking — that one also
    // treats CrackedWall as blocking. Kept separate deliberately.
    private static readonly byte[] ConnectivityBlockingLut = BuildConnectivityBlockingLut();

    private static byte[] BuildConnectivityBlockingLut()
    {
        int max = 0;
        foreach (TileType t in Enum.GetValues<TileType>()) if ((int)t > max) max = (int)t;
        var lut = new byte[max + 1];
        foreach (TileType t in Enum.GetValues<TileType>())
            lut[(int)t] = t is TileType.Wall or TileType.Mountain or TileType.Tree
                or TileType.TreePine or TileType.Rock or TileType.WaterDeep or TileType.Lava
                ? (byte)1 : (byte)0;
        return lut;
    }

    // BFS from spawn; carves rescue paths to any unreachable clearing/room center.
    internal static void EnsureConnectivity(GameMap map, List<Room> rooms,
        List<(int x, int y)> clearings)
    {
        if (clearings.Count == 0) return;
        int w = map.Width, h = map.Height;

        // FLAT ARRAYS AND A FLAT BLOCKING TABLE. This was a bool[w, h], a Queue<(int, int)> and a
        // `map.Tiles[nx, ny].Type` probe per neighbour — a 16-byte struct through a 2D indexer, in
        // random order, four times per visited cell. Measured on a 1000x1000 floor: 2M random
        // probes cost 169.3 ms that way against 13.3 ms through a byte table. Same shape, and the
        // same fix, as ConnectivityAuditPass.FloodFill.
        //
        // Output-neutral by construction: this BFS draws no rng and only ever ASKS which cells are
        // reachable. The carve decisions below read `reached`, whose contents do not depend on how
        // it is stored. Proven anyway by diffing floor signatures.
        var blocked = new byte[w * h];
        var lut = ConnectivityBlockingLut;
        var tiles = map.Tiles;
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
                blocked[rowBase + x] = lut[(int)tiles[x, y].Type];
        }

        var reached = new bool[w * h];
        var queue = new int[w * h];
        int head = 0, tail = 0;
        var (sx, sy) = clearings[0];
        if (!map.InBounds(sx, sy)) return;
        int startIdx = sy * w + sx;
        queue[tail++] = startIdx;
        reached[startIdx] = true;
        while (head < tail)
        {
            int cur = queue[head++];
            // One division rather than a div/mod pair on the same operands; this runs once per
            // reachable cell, which is most of a million-cell floor.
            int cy = cur / w, cx = cur - cy * w;
            if (cx > 0)     Visit(cur - 1);
            if (cx < w - 1) Visit(cur + 1);
            if (cy > 0)     Visit(cur - w);
            if (cy < h - 1) Visit(cur + w);

            void Visit(int ni)
            {
                if (reached[ni] || blocked[ni] != 0) return;
                reached[ni] = true;
                queue[tail++] = ni;
            }
        }
        for (int i = 1; i < clearings.Count; i++)
        {
            var (cx, cy) = clearings[i];
            if (!map.InBounds(cx, cy) || reached[cy * w + cx]) continue;
            CarveStraightPath(map, sx, sy, cx, cy);
        }
        foreach (var rm in rooms)
        {
            int cx = rm.X + rm.Width / 2, cy = rm.Y + rm.Height / 2;
            if (!map.InBounds(cx, cy) || reached[cy * w + cx]) continue;
            CarveStraightPath(map, sx, sy, cx, cy);
        }
    }
}
