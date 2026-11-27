namespace SAOTRPG.Map.Generation.Passes;

// Final audit pass: flood-fill + drill orphans, critical-path guard, trap eviction near spawn.
// Progressive relaxation — never infinite-regens; imperfect floors are logged and accepted.
public sealed class ConnectivityAuditPass : IGenerationPass
{
    public string Name => "ConnectivityAudit";
    public bool ShouldRun(WorldContext ctx) => true;

    public void Execute(WorldContext ctx)
    {
        // In-situ rescue (drill orphans, evict traps); unrepairable constraints
        // log a warning and accept — no regen loop.
        var map = ctx.Map;

        // Step 1 — flood-fill from spawn, drill orphan walkable tiles.
        byte[] blocked;
        int walkable;
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.BuildTable"))
            blocked = BuildBlockingTable(map, out walkable);

        int[] reachState;
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.FloodFill"))
            reachState = FloodFill(map, blocked, ctx.SpawnX, ctx.SpawnY);
        int drilledCount;
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.DrillOrphans"))
            drilledCount = DrillOrphans(map, blocked, reachState, ctx.SpawnX, ctx.SpawnY);
        if (drilledCount > 0)
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} drilled={drilledCount} orphan_tiles");

        // Step 2 — critical path: how far the player must walk to the archway. Taken as a ratio
        // of the floor's own eccentricity, not against the map diagonal: the walkable disk shrinks
        // with floor while the map does not, so a diagonal-based threshold could never be met.
        // Drilling carves corridors, so the table it read would be stale for the distance pass.
        // DrillOrphans keeps it current instead: a carve is the only thing that mutates the map
        // inside that pass, and it reports the cells it passed over, so exactly those are refreshed.

        // Drilling does not guarantee connectivity, so verify and rescue what matters. A straight
        // carve can fail outright and leave chests, anvils, journals and campfires visible and
        // unreachable. Bare terrain nooks are left alone; they read as natural relief and cost the
        // player nothing.
        // A stranded room also counts as something worth reaching, even when it holds nothing. On
        // an open floor the empty pockets left behind are small nooks behind boulders, but a cave
        // can strand a large room, plainly visible from the passage outside, because one rock
        // cluster fell across the neck.
        //
        // A share of the walkable area with an absolute floor, never a flat count, so the meaning
        // survives the floor shrinking from 1000x1000 to 100x100. The threshold sits well above
        // the largest pocket an open floor produces, which keeps this from touching them.
        int minStranded = Math.Max(MinStrandedCells, (int)(walkable * StrandedShare));

        int rescued;
        var opened = new List<int>();
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.RescueContent"))
            rescued = RescueStrandedContent(map, blocked, ctx.SpawnX, ctx.SpawnY, minStranded, opened);
        if (rescued > 0)
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} tunnelled to {rescued} stranded content pocket(s)");
            // Same trade as the drill above: a tunnel is the only thing that mutated the map, it
            // reports the cells it opened, and refreshing those is what a full rebuild would give.
            // The table deliberately stays frozen while the rescue runs — the pocket scan inside
            // reads it, and feeding it a route opened part way through would change which pockets
            // it finds.
            var lut = BlockingLut;
            var tiles = map.Tiles;
            int mw = map.Width;
            foreach (int i in opened)
            {
                int ty = i / mw, tx = i - ty * mw;
                blocked[i] = lut[(int)tiles[tx, ty].Type];
            }
        }

        int sx, sy, dExit, dMax;
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.CriticalPath"))
            (sx, sy, dExit, dMax) = MeasureCriticalPath(map, blocked, ctx.SpawnX, ctx.SpawnY);
        if (sx >= 0)
        {
            double ratio = dMax > 0 ? (double)dExit / dMax : 0;
            if (ratio < CriticalPathFloor)
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                    $"ConnectivityAudit floor={ctx.FloorNumber} WARN critpath_too_short " +
                    $"dist={dExit} eccentricity={dMax} ratio={ratio:F2} min={CriticalPathFloor:F2}");
        }
        else
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} WARN no_exit_found");
        }

        // Step 3 — trap anti-frustration. F1 gets a huge 100-tile radius (town-only effectively);
        // other floors sweep the same box FeatureScatterPass refuses to scatter into, which
        // catches any hazard placed by a path that bypasses AcceptTile.
        int radius = ctx.FloorNumber == 1 ? 100 : FeatureScatterPass.SpawnHazardExclusion;
        int evicted;
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.EvictTraps"))
            evicted = EvictTrapsNearSpawn(map, ctx.SpawnX, ctx.SpawnY, radius);
        // Always clear traps on stairs.
        if (sx >= 0) EvictTrapAt(map, sx, sy);
        if (evicted > 0)
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} evicted={evicted} traps_in_radius={radius}");

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
            $"ConnectivityAudit PASS floor={ctx.FloorNumber}");

        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Audit.Recount"))
            map.RecountWalkableTiles();
    }

    // A 1-byte blocking table, because all three traversals here are random access, and probing
    // `map.Tiles[x, y].Type` reads a 16-byte struct through a 2D indexer; a BFS probing four
    // neighbours per cell does millions of them. Same reasoning as `GameMap._opaque`, which
    // exists for exactly this on the sight-blocking path.
    //
    // Built y-outer on purpose: the sequential writes into this table matter more than the
    // strided reads out of the tile grid. That is the opposite of what the 2D indexer suggests,
    // so keep the loop order.
    private static byte[] BuildBlockingTable(GameMap map, out int walkable)
    {
        int w = map.Width, h = map.Height;
        var blocked = new byte[w * h];
        var lut = BlockingLut;
        var tiles = map.Tiles;
        int open = 0;
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                byte b = lut[(int)tiles[x, y].Type];
                blocked[rowBase + x] = b;
                if (b == 0) open++;
            }
        }
        walkable = open;
        return blocked;
    }

    // Reachability state, row-major like every other per-cell grid in the codebase.
    //   0      never reached
    //   1      reached from spawn
    //   N >= 2 claimed by orphan region N
    private const int StateUnreached = 0;
    private const int StateSpawn = 1;

    // 4-directional BFS using the same walkability rule as GameMap exploration.
    private static int[] FloodFill(GameMap map, byte[] blocked, int sx, int sy)
    {
        int w = map.Width, h = map.Height;
        var state = new int[w * h];
        if (!map.InBounds(sx, sy)) return state;
        var queue = new int[w * h];
        int head = 0, tail = 0;
        int start = sy * w + sx;
        queue[tail++] = start;
        state[start] = StateSpawn;
        while (head < tail)
        {
            int cur = queue[head++];
            // One division, not two: `cur % w` and `cur / w` on the same operands is a div plus a
            // multiply-subtract, and this runs once per reachable cell on a million-cell floor.
            int cy = cur / w, cx = cur - cy * w;
            if (cx > 0)     TryVisit(blocked, state, queue, ref tail, cur - 1, StateSpawn);
            if (cx < w - 1) TryVisit(blocked, state, queue, ref tail, cur + 1, StateSpawn);
            if (cy > 0)     TryVisit(blocked, state, queue, ref tail, cur - w, StateSpawn);
            if (cy < h - 1) TryVisit(blocked, state, queue, ref tail, cur + w, StateSpawn);
        }
        return state;
    }

    // Takes a flat index: the table is indexed the same way the state grid is.
    private static void TryVisit(byte[] blocked, int[] state, int[] queue, ref int tail,
        int idx, int mark)
    {
        if (state[idx] != StateUnreached) return;
        if (blocked[idx] != 0) return;
        state[idx] = mark;
        queue[tail++] = idx;
    }

    // Connect every orphaned region to the reachable world with exactly one corridor each.
    //
    // Flooding the region first and cutting once gives it a single doorway; carving from each
    // orphan tile would drill one big sealed region (a floor walled off behind a boss arena, say)
    // dozens of times through the same wall.
    //
    // The carve may breach a wall (see CarveStraightPath's breachWalls) because a region sealed by
    // a wall is precisely the case this exists for -- without it the carve would step over the
    // wall unchanged, carve both sides, and count a success it had not made.
    // This carves corridors as it goes, so a table built once and left alone would be stale from
    // the first carve onward and the next region's flood would treat a corridor just opened as
    // wall. The carve reports the cells it crossed and exactly those are refreshed straight after.
    private static int DrillOrphans(GameMap map, byte[] blocked, int[] state, int spawnX, int spawnY)
    {
        int w = map.Width, h = map.Height;
        int drilled = 0;
        int region = StateSpawn;
        var queue = new int[w * h];
        var touched = new List<int>(64);
        var lut = BlockingLut;
        var tiles = map.Tiles;

        for (int ox = 1; ox < w - 1; ox++)
        for (int oy = 1; oy < h - 1; oy++)
        {
            int seed = oy * w + ox;
            if (state[seed] != StateUnreached) continue;
            if (blocked[seed] != 0) continue;

            // Claim the region under its own id before carving, so the corridor count is
            // one-per-region and the destination search can tell it apart from the world.
            region++;
            int head = 0, tail = 0;
            queue[tail++] = seed;
            state[seed] = region;
            while (head < tail)
            {
                int cur = queue[head++];
                int cy = cur / w, cx = cur - cy * w;
                if (cx > 0)     TryVisit(blocked, state, queue, ref tail, cur - 1, region);
                if (cx < w - 1) TryVisit(blocked, state, queue, ref tail, cur + 1, region);
                if (cy > 0)     TryVisit(blocked, state, queue, ref tail, cur - w, region);
                if (cy < h - 1) TryVisit(blocked, state, queue, ref tail, cur + w, region);
            }

            if (!TryFindNearestReached(state, ox, oy, w, h, region, out int rx, out int ry))
                continue;
            touched.Clear();
            MapGenerator.CarveStraightPath(map, rx, ry, ox, oy, breachWalls: true, touched);
            // Refresh the table over exactly the cells the carve crossed. Nothing else in this
            // pass writes a tile, so the table stays byte-for-byte what a full rebuild would give.
            foreach (int i in touched)
            {
                int ty = i / w, tx = i - ty * w;
                blocked[i] = lut[(int)tiles[tx, ty].Type];
            }
            drilled++;
        }
        return drilled;
    }

    // Excludes the region currently being drilled: its tiles are marked but not yet connected, so
    // treating one as a destination would carve a corridor from the region to itself and leave it
    // sealed. Every lower region id is already joined, because each is drilled as it is claimed.
    private static bool TryFindNearestReached(int[] state, int ox, int oy, int w, int h,
        int currentRegion, out int rx, out int ry)
    {
        // Accepts any already-claimed cell, not only a spawn-reachable one, and that is
        // deliberate. Requiring StateSpawn looks more correct — a chain of orphans need not reach
        // spawn — but the perimeter search would then have to expand past every neighbouring
        // orphan before it finds the main body, once per region, with hundreds of regions per
        // floor, and it still would not make every content pocket reachable.
        // RescueStrandedContent is what closes that hole.
        //
        // Walks the perimeter of each expanding box, not the whole square, so the search is
        // O(R^2) rather than O(R^3) — and it runs once per orphan region. Examination order (top
        // row left-to-right, then the two side columns per row, then the bottom row) decides which
        // tile is found first, and so where the carve goes.
        for (int radius = 1; radius < Math.Max(w, h); radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int y = oy + dy;
                if (y < 0 || y >= h) continue;
                int rowBase = y * w;

                if (dy == -radius || dy == radius)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = ox + dx;
                        if (x < 0 || x >= w) continue;
                        int st = state[rowBase + x];
                        if (st != StateUnreached && st != currentRegion) { rx = x; ry = y; return true; }
                    }
                    continue;
                }

                int xl = ox - radius;
                if (xl >= 0)
                {
                    int st = state[rowBase + xl];
                    if (st != StateUnreached && st != currentRegion) { rx = xl; ry = y; return true; }
                }
                int xr = ox + radius;
                if (xr < w)
                {
                    int st = state[rowBase + xr];
                    if (st != StateUnreached && st != currentRegion) { rx = xr; ry = y; return true; }
                }
            }
        }
        rx = -1; ry = -1; return false;
    }

    // Shortest acceptable ratio of the exit's distance to the floor's eccentricity; below it the
    // critical path is logged as trivially short. The exit that matters is the farthest reachable
    // LabyrinthEntrance, and MeasureCriticalPath returns it together with its distance and the
    // eccentricity, all from one distance flood. An overworld carries an archway and no boss: the
    // floor boss stands on the labyrinth's StairsUp.
    private const double CriticalPathFloor = 0.35;

    // What counts as a stranded room rather than a nook, as a share of the walkable area with an
    // absolute floor for the smallest disks. See the call site.
    private const int MinStrandedCells = 200;
    private const double StrandedShare = 0.0012;

    // Content a player is meant to reach. A pocket holding none of this is scenery.
    // Table-driven for the same reason the blocking test is, and derived from the predicate so the
    // two cannot drift.
    private static readonly bool[] WorthReachingLut = BuildWorthReachingLut();

    private static bool[] BuildWorthReachingLut()
    {
        int max = 0;
        foreach (TileType t in Enum.GetValues<TileType>()) if ((int)t > max) max = (int)t;
        var lut = new bool[max + 1];
        foreach (TileType t in Enum.GetValues<TileType>()) lut[(int)t] = IsWorthReachingCore(t);
        return lut;
    }

    private static bool IsWorthReaching(TileType t)
    {
        int i = (int)t;
        return (uint)i < (uint)WorthReachingLut.Length && WorthReachingLut[i];
    }

    private static bool IsWorthReachingCore(TileType t) =>
        t is TileType.Chest or TileType.Anvil or TileType.EnchantShrine or TileType.BountyBoard
          or TileType.Shrine or TileType.SecretShrine or TileType.Fountain
          or TileType.Journal or TileType.LoreStone or TileType.MonumentOfSwordsmen
          or TileType.Campfire or TileType.StairsUp or TileType.LabyrinthEntrance
          or TileType.OreVeinIron or TileType.OreVeinMithril or TileType.OreVeinDivine;

    // Re-flood from spawn, then tunnel to any unreachable pocket that holds something. The tunnel
    // is a BFS through blocking tiles as well as open ones, so unlike a straight carve it cannot
    // fail when a route exists at all — and it opens only the cells on the route it found.
    private static int RescueStrandedContent(GameMap map, byte[] blocked, int spawnX, int spawnY,
        int minStranded, List<int> opened)
    {
        int w = map.Width, h = map.Height;
        var reach = FloodFill(map, blocked, spawnX, spawnY);

        var seen = new bool[w * h];
        var queue = new int[w * h];
        int rescued = 0;

        // The tunnel BFS needs a predecessor grid the size of the map. Allocated and cleared once
        // here rather than per pocket: the tunnel resets only the cells it actually visited, so
        // the cost scales with the region it explored instead of with the floor.
        var prev = new int[w * h];
        Array.Fill(prev, -2);
        var tunnelQueue = new int[w * h];

        for (int oy = 1; oy < h - 1; oy++)
        for (int ox = 1; ox < w - 1; ox++)
        {
            int seed = oy * w + ox;
            if (seen[seed] || reach[seed] != StateUnreached || blocked[seed] != 0) continue;

            // Claim the pocket and note whether anything in it is worth reaching.
            int head = 0, tail = 0;
            queue[tail++] = seed;
            seen[seed] = true;
            bool worth = false;
            int size = 0;
            int anchorX = ox, anchorY = oy;
            while (head < tail)
            {
                int cur = queue[head++];
                size++;
                int cy = cur / w, cx = cur - cy * w;
                if (IsWorthReaching(map.Tiles[cx, cy].Type)) { worth = true; anchorX = cx; anchorY = cy; }
                void Step(int ni, int nx, int ny)
                {
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) return;
                    if (seen[ni] || blocked[ni] != 0) return;
                    seen[ni] = true;
                    queue[tail++] = ni;
                }
                Step(cur - 1, cx - 1, cy); Step(cur + 1, cx + 1, cy);
                Step(cur - w, cx, cy - 1); Step(cur + w, cx, cy + 1);
            }
            if (!worth && size < minStranded) continue;
            if (TunnelToReachable(map, reach, anchorX, anchorY, prev, tunnelQueue, opened)) rescued++;
        }
        return rescued;
    }

    // BFS outward through walls until a spawn-reachable cell is met, then open the route. Costs a
    // second grid, but it only ever runs for a pocket that holds content — a handful per floor.
    private static bool TunnelToReachable(GameMap map, int[] reach, int fromX, int fromY,
        int[] prev, int[] queue, List<int> opened)
    {
        int w = map.Width, h = map.Height;
        int head = 0, tail = 0;
        int start = fromY * w + fromX;
        queue[tail++] = start;
        prev[start] = -1;
        bool found = false;

        while (head < tail)
        {
            int cur = queue[head++];
            if (reach[cur] == StateSpawn && cur != start)
            {
                // Walk the route back, opening every blocking tile on it.
                for (int step = cur; step != -1; step = prev[step])
                {
                    int py = step / w, px = step - py * w;
                    if (IsAuditBlocking(map.Tiles[px, py].Type))
                    {
                        map.SetTileType(px, py, TileType.Path);
                        opened.Add(step);
                    }
                }
                found = true;
                break;
            }
            int cy = cur / w, cx = cur - cy * w;
            void Step(int ni, int nx, int ny)
            {
                if (nx < 1 || ny < 1 || nx >= w - 1 || ny >= h - 1) return;
                if (prev[ni] != -2) return;
                prev[ni] = cur;
                queue[tail++] = ni;
            }
            Step(cur - 1, cx - 1, cy); Step(cur + 1, cx + 1, cy);
            Step(cur - w, cx, cy - 1); Step(cur + w, cx, cy + 1);
        }

        // Hand the shared grid back in the state a freshly allocated one would be in. Only the
        // cells this search actually enqueued were written, and the queue is the list of them.
        for (int i = 0; i < tail; i++) prev[queue[i]] = -2;
        return found;
    }

    private static (int x, int y, int dExit, int dMax) MeasureCriticalPath(GameMap map, byte[] blocked, int spawnX, int spawnY)
    {
        int w = map.Width, h = map.Height;
        if (!map.InBounds(spawnX, spawnY)) return (-1, -1, -1, 0);
        var dist = new int[w * h];
        Array.Fill(dist, -1);
        var queue = new int[w * h];
        int head = 0, tail = 0;
        int startIdx = spawnY * w + spawnX;
        queue[tail++] = startIdx;
        dist[startIdx] = 0;
        int hLimit = w * (h - 1);
        while (head < tail)
        {
            int cur = queue[head++];
            int cy = cur / w, cx = cur - cy * w;
            int nd = dist[cur] + 1;
            // Unrolled: left, right, up, down. A BFS assigns each cell its shortest distance
            // whatever order neighbours are examined in.
            if (cx > 0)     Step(dist, blocked, queue, ref tail, cur - 1, nd);
            if (cx < w - 1) Step(dist, blocked, queue, ref tail, cur + 1, nd);
            if (cur >= w)   Step(dist, blocked, queue, ref tail, cur - w, nd);
            if (cur < hLimit) Step(dist, blocked, queue, ref tail, cur + w, nd);
        }

        static void Step(int[] dist, byte[] blocked, int[] queue, ref int tail, int ni, int nd)
        {
            if (dist[ni] >= 0) return;
            if (blocked[ni] != 0) return;
            dist[ni] = nd;
            queue[tail++] = ni;
        }

        int bx = -1, by = -1, dExit = -1, dMax = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (dist[i] < 0) continue;
                if (dist[i] > dMax) dMax = dist[i];
                if (map.Tiles[x, y].Type != TileType.LabyrinthEntrance) continue;
                if (dist[i] > dExit) { dExit = dist[i]; bx = x; by = y; }
            }
        return (bx, by, dExit, dMax);
    }

    private static int EvictTrapsNearSpawn(GameMap map, int sx, int sy, int radius)
    {
        int w = map.Width, h = map.Height;
        int evicted = 0;
        int r2 = radius * radius;
        int x0 = Math.Max(0, sx - radius), x1 = Math.Min(w - 1, sx + radius);
        int y0 = Math.Max(0, sy - radius), y1 = Math.Min(h - 1, sy + radius);
        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++)
        {
            int ddx = x - sx, ddy = y - sy;
            if (ddx * ddx + ddy * ddy > r2) continue;
            if (EvictTrapAt(map, x, y)) evicted++;
        }
        return evicted;
    }

    private static bool EvictTrapAt(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return false;
        var t = map.Tiles[x, y].Type;
        if (t is TileType.TrapSpike or TileType.TrapTeleport
            or TileType.TrapPoison or TileType.TrapAlarm or TileType.GasVent
            or TileType.TrapWeb or TileType.TrapMagnet or TileType.TrapRune)
        {
            map.Tiles[x, y].Type = TileType.Grass;
            return true;
        }
        return false;
    }

    // Blocks audit flood-fill/BFS — impassable tiles only (walls/mountains/deep water/lava);
    // hazard-walkables (Mud/BogWater/Ice/Sand/etc.) stay reachable for routing.
    // Public so the labyrinth and population floods, and the verification probes, share the
    // definition the runtime audit uses rather than copies that could drift.
    public static bool IsAuditBlocking(TileType t)
    {
        int i = (int)t;
        return (uint)i < (uint)BlockingLut.Length && BlockingLut[i] != 0;
    }

    // The predicate above is asked millions of times per floor — once per neighbour probe in three
    // BFS traversals, plus once per cell in each blocking-table build. A table indexed by tile type
    // turns an eight-way pattern match into one load. Derived from the predicate below rather than
    // hand-listed, so the two cannot drift apart.
    private static readonly byte[] BlockingLut = BuildBlockingLut();

    private static byte[] BuildBlockingLut()
    {
        int max = 0;
        foreach (TileType t in Enum.GetValues<TileType>()) if ((int)t > max) max = (int)t;
        var lut = new byte[max + 1];
        foreach (TileType t in Enum.GetValues<TileType>())
            lut[(int)t] = IsAuditBlockingCore(t) ? (byte)1 : (byte)0;
        return lut;
    }

    private static bool IsAuditBlockingCore(TileType t) =>
        t is TileType.Wall
            or TileType.CrackedWall
            or TileType.Mountain
            or TileType.Tree
            or TileType.TreePine
            or TileType.Rock
            or TileType.WaterDeep
            or TileType.Lava;
}
