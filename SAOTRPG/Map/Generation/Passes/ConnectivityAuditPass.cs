using SAOTRPG.Systems;

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
        var reachState = FloodFill(map, ctx.SpawnX, ctx.SpawnY);
        int drilledCount = DrillOrphans(map, reachState, ctx.SpawnX, ctx.SpawnY);
        if (drilledCount > 0)
            UI.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} drilled={drilledCount} orphan_tiles");

        // Step 2 — critical path: how far the player must walk to the archway the boss guards.
        // Measured as a RATIO of the floor's own eccentricity, not against the map diagonal. The
        // walkable disk shrinks with floor while the map does not, so a diagonal-based floor was
        // unreachable and this warned on every floor ever generated — which is the same as never
        // warning. Real ratios measure 0.49-0.70 across the climb; 0.35 catches a trivial path.
        var (sx, sy, dExit, dMax) = MeasureCriticalPath(map, ctx.SpawnX, ctx.SpawnY);
        if (sx >= 0)
        {
            double ratio = dMax > 0 ? (double)dExit / dMax : 0;
            if (ratio < CriticalPathFloor)
                UI.DebugLogger.LogGame("MAPGEN",
                    $"ConnectivityAudit floor={ctx.FloorNumber} WARN critpath_too_short " +
                    $"dist={dExit} eccentricity={dMax} ratio={ratio:F2} min={CriticalPathFloor:F2}");
        }
        else
        {
            UI.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} WARN no_exit_found");
        }

        // Step 3 — trap anti-frustration. F1 gets a huge 100-tile radius (town-only effectively);
        // other floors sweep the SAME box FeatureScatterPass refuses to scatter into. It used to be
        // 5, i.e. strictly inside that exclusion, so it could only ever catch a hazard placed by a
        // path that bypasses AcceptTile — and at 5 it missed those too. Measured over 80 floor/seed
        // pairs before the change: zero evictions, while hazards sat 7-8 tiles from spawn.
        int radius = ctx.FloorNumber == 1 ? 100 : FeatureScatterPass.SpawnHazardExclusion;
        int evicted = EvictTrapsNearSpawn(map, ctx.SpawnX, ctx.SpawnY, radius);
        // Always clear traps on stairs.
        if (sx >= 0) EvictTrapAt(map, sx, sy);
        if (evicted > 0)
            UI.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} evicted={evicted} traps_in_radius={radius}");

        UI.DebugLogger.LogGame("MAPGEN",
            $"ConnectivityAudit PASS floor={ctx.FloorNumber}");

        map.RecountWalkableTiles();
    }

    // Reachability state, row-major like every other per-cell grid in the codebase.
    //   0      never reached
    //   1      reached from spawn
    //   N >= 2 claimed by orphan region N
    private const int StateUnreached = 0;
    private const int StateSpawn = 1;

    // 4-directional BFS using the same walkability rule as GameMap exploration.
    private static int[] FloodFill(GameMap map, int sx, int sy)
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
            int cx = cur % w, cy = cur / w;
            if (cx > 0)     TryVisit(map, state, queue, ref tail, cur - 1, cx - 1, cy, StateSpawn);
            if (cx < w - 1) TryVisit(map, state, queue, ref tail, cur + 1, cx + 1, cy, StateSpawn);
            if (cy > 0)     TryVisit(map, state, queue, ref tail, cur - w, cx, cy - 1, StateSpawn);
            if (cy < h - 1) TryVisit(map, state, queue, ref tail, cur + w, cx, cy + 1, StateSpawn);
        }
        return state;
    }

    private static void TryVisit(GameMap map, int[] state, int[] queue, ref int tail,
        int idx, int x, int y, int mark)
    {
        if (state[idx] != StateUnreached) return;
        if (IsAuditBlocking(map.Tiles[x, y].Type)) return;
        state[idx] = mark;
        queue[tail++] = idx;
    }

    // Connect every orphaned REGION to the reachable world with exactly ONE corridor each.
    //
    // This used to stride-sample individual orphan TILES and carve from each, marking only the
    // corridor it cut as reached. That is fine for scattered pockets and disastrous for one big
    // sealed region: a floor walled off behind a boss arena was drilled 50-77 times through the
    // same wall. Flooding the region first and cutting once gives it a doorway instead.
    //
    // The carve may breach a wall (see CarveStraightPath's breachWalls) because a region sealed BY
    // a wall is precisely the case this exists for -- without it the carve stepped over the wall
    // unchanged, carved both sides, and counted a success it had not made.
    private static int DrillOrphans(GameMap map, int[] state, int spawnX, int spawnY)
    {
        int w = map.Width, h = map.Height;
        int drilled = 0;
        int region = StateSpawn;
        var queue = new int[w * h];

        for (int ox = 1; ox < w - 1; ox++)
        for (int oy = 1; oy < h - 1; oy++)
        {
            int seed = oy * w + ox;
            if (state[seed] != StateUnreached) continue;
            if (IsAuditBlocking(map.Tiles[ox, oy].Type)) continue;

            // Claim the region under its own id before carving, so the corridor count is
            // one-per-region and the destination search can tell it apart from the world.
            region++;
            int head = 0, tail = 0;
            queue[tail++] = seed;
            state[seed] = region;
            while (head < tail)
            {
                int cur = queue[head++];
                int cx = cur % w, cy = cur / w;
                if (cx > 0)     TryVisit(map, state, queue, ref tail, cur - 1, cx - 1, cy, region);
                if (cx < w - 1) TryVisit(map, state, queue, ref tail, cur + 1, cx + 1, cy, region);
                if (cy > 0)     TryVisit(map, state, queue, ref tail, cur - w, cx, cy - 1, region);
                if (cy < h - 1) TryVisit(map, state, queue, ref tail, cur + w, cx, cy + 1, region);
            }

            if (!TryFindNearestReached(state, ox, oy, w, h, region, out int rx, out int ry))
                continue;
            MapGenerator.CarveStraightPath(map, rx, ry, ox, oy, breachWalls: true);
            drilled++;
        }
        return drilled;
    }

    // Excludes the region currently being drilled: its tiles are marked but NOT yet connected, so
    // treating one as a destination would carve a corridor from the region to itself and leave it
    // sealed. Every LOWER region id is already joined, because each is drilled as it is claimed.
    private static bool TryFindNearestReached(int[] state, int ox, int oy, int w, int h,
        int currentRegion, out int rx, out int ry)
    {
        // Walks the PERIMETER of each expanding box, not the whole square. The previous form
        // iterated all (2r+1)^2 cells per radius and `continue`d everything off the edge, which is
        // O(R^3) work for an O(R^2) search — and it runs once per orphan region. Examination order
        // is preserved exactly (top row left-to-right, then the two side columns per row, then the
        // bottom row), so the same tile is still found first and the carve is unchanged.
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

    // The exit that matters is the farthest REACHABLE LabyrinthEntrance. Returns it together with
    // its distance and the floor's eccentricity, all from ONE distance flood.
    // This used to say the exit is "the one the floor boss is posted on, the same choice
    // MapGenerator.FindBossPost makes" — both halves are stale. FindBossPost was deleted, and the
    // rule that replaced it is that the floor boss stands on the labyrinth's StairsUp, so an
    // overworld carries an archway and no boss at all.
    private const double CriticalPathFloor = 0.35;

    private static (int x, int y, int dExit, int dMax) MeasureCriticalPath(GameMap map, int spawnX, int spawnY)
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
        int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
        while (head < tail)
        {
            int cur = queue[head++];
            int cx = cur % w, cy = cur / w;
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int ni = ny * w + nx;
                if (dist[ni] >= 0) continue;
                if (IsAuditBlocking(map.Tiles[nx, ny].Type)) continue;
                dist[ni] = dist[cur] + 1;
                queue[tail++] = ni;
            }
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
    // Public so Tools/SeedProbe's offline reachability check uses the SAME definition the runtime
    // audit does. Duplicating the list there would let the two drift, and the offline check would
    // then be asserting something the game does not believe.
    public static bool IsAuditBlocking(TileType t) =>
        t is TileType.Wall
            or TileType.CrackedWall
            or TileType.Mountain
            or TileType.Tree
            or TileType.TreePine
            or TileType.Rock
            or TileType.WaterDeep
            or TileType.Lava;
}
