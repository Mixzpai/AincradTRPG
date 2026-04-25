using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation.Passes;

// Final audit pass: flood-fill + drill orphans, critical-path + lake guards, trap eviction near spawn.
// Progressive relaxation — never infinite-regens; imperfect floors are logged and accepted.
public sealed class ConnectivityAuditPass : IGenerationPass
{
    public string Name => "ConnectivityAudit";
    public bool ShouldRun(WorldContext ctx) => true;

    public void Execute(WorldContext ctx)
    {
        // In-situ rescue (drill orphans, evict traps); unrepairable constraints
        // log a warning and accept — no regen loop.
        int w = ctx.Width, h = ctx.Height;
        var map = ctx.Map;

        // Step 1 — flood-fill from spawn, drill orphan walkable tiles.
        var reached = FloodFill(map, ctx.SpawnX, ctx.SpawnY);
        int drilledCount = DrillOrphans(map, reached, ctx.SpawnX, ctx.SpawnY);
        if (drilledCount > 0)
            UI.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} drilled={drilledCount} orphan_tiles");

        // Step 2 — critical path check (BFS distance spawn -> stairs down).
        var (sx, sy) = FindStairsDown(map);
        if (sx >= 0)
        {
            int dist = BfsDist(map, ctx.SpawnX, ctx.SpawnY, sx, sy);
            double diag = Math.Sqrt((double)w * w + (double)h * h);
            double minDist = 0.6 * diag;
            if (dist < 0 || dist < minDist)
                UI.DebugLogger.LogGame("MAPGEN",
                    $"ConnectivityAudit floor={ctx.FloorNumber} WARN critpath_too_short dist={dist} min={minDist:F0}");
        }
        else
        {
            UI.DebugLogger.LogGame("MAPGEN",
                $"ConnectivityAudit floor={ctx.FloorNumber} WARN no_stairs_down_found");
        }

        // Step 3 — lake guard (Brogue's 85% rule).
        if (ctx.PreLakeWalkableCount > 0)
        {
            int postWalkable = CountWalkable(map);
            double ratio = (double)postWalkable / ctx.PreLakeWalkableCount;
            if (ratio < 0.85)
                UI.DebugLogger.LogGame("MAPGEN",
                    $"ConnectivityAudit floor={ctx.FloorNumber} WARN lake_broke_reachability pre={ctx.PreLakeWalkableCount} post={postWalkable} ratio={ratio:F2}");
        }

        // Step 4 — trap anti-frustration. F1 gets a huge 100-tile radius
        // (town-only effectively); other floors get a 5-tile spawn buffer.
        int radius = ctx.FloorNumber == 1 ? 100 : 5;
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

    // 4-directional BFS using the same walkability rule as GameMap exploration.
    private static bool[,] FloodFill(GameMap map, int sx, int sy)
    {
        int w = map.Width, h = map.Height;
        var reached = new bool[w, h];
        if (!map.InBounds(sx, sy)) return reached;
        var q = new Queue<(int x, int y)>();
        q.Enqueue((sx, sy));
        reached[sx, sy] = true;
        int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (!map.InBounds(nx, ny) || reached[nx, ny]) continue;
                if (IsAuditBlocking(map.Tiles[nx, ny].Type)) continue;
                reached[nx, ny] = true;
                q.Enqueue((nx, ny));
            }
        }
        return reached;
    }

    // Walk every tile; for each orphaned walkable tile not reached, carve an
    // L-shaped corridor to the nearest reachable tile via CarveStraightPath.
    private static int DrillOrphans(GameMap map, bool[,] reached, int spawnX, int spawnY)
    {
        int w = map.Width, h = map.Height;
        int drilled = 0;
        // Stride-sample orphans so we don't drill thousands of redundant corridors
        // through one reachable pocket; mark each drilled corridor as reached on the fly.
        for (int x = 1; x < w - 1; x += 3)
        for (int y = 1; y < h - 1; y += 3)
        {
            if (reached[x, y]) continue;
            if (IsAuditBlocking(map.Tiles[x, y].Type)) continue;
            // Find nearest reached tile (box search outward).
            if (!TryFindNearestReached(reached, x, y, w, h, out int rx, out int ry))
                continue;
            MapGenerator.CarveStraightPath(map, rx, ry, x, y);
            drilled++;
            // Mark the corridor as reached so subsequent iterations don't redrill.
            MarkCorridor(reached, rx, ry, x, y, w, h);
        }
        return drilled;
    }

    private static bool TryFindNearestReached(bool[,] reached, int ox, int oy, int w, int h,
        out int rx, out int ry)
    {
        for (int radius = 1; radius < Math.Max(w, h); radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int y = oy + dy;
                if (y < 0 || y >= h) continue;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                    int x = ox + dx;
                    if (x < 0 || x >= w) continue;
                    if (reached[x, y]) { rx = x; ry = y; return true; }
                }
            }
        }
        rx = -1; ry = -1; return false;
    }

    private static void MarkCorridor(bool[,] reached, int x1, int y1, int x2, int y2, int w, int h)
    {
        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy;
        int x = x1, y = y1;
        while (true)
        {
            if (x >= 0 && x < w && y >= 0 && y < h) reached[x, y] = true;
            if (x == x2 && y == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    private static (int x, int y) FindStairsDown(GameMap map)
    {
        for (int x = 0; x < map.Width; x++)
        for (int y = 0; y < map.Height; y++)
            if (map.Tiles[x, y].Type == TileType.StairsDown) return (x, y);
        return (-1, -1);
    }

    private static int BfsDist(GameMap map, int ax, int ay, int bx, int by)
    {
        if (!map.InBounds(ax, ay) || !map.InBounds(bx, by)) return -1;
        var dist = new Dictionary<(int, int), int>();
        var q = new Queue<(int x, int y)>();
        q.Enqueue((ax, ay));
        dist[(ax, ay)] = 0;
        int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();
            int curDist = dist[(cx, cy)];
            if (cx == bx && cy == by) return curDist;
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (!map.InBounds(nx, ny) || dist.ContainsKey((nx, ny))) continue;
                if (IsAuditBlocking(map.Tiles[nx, ny].Type)) continue;
                dist[(nx, ny)] = curDist + 1;
                q.Enqueue((nx, ny));
            }
        }
        return -1;
    }

    private static int CountWalkable(GameMap map)
    {
        int w = map.Width, h = map.Height;
        int c = 0;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = map.Tiles[x, y].Type;
            if (t != TileType.Wall && t != TileType.Mountain) c++;
        }
        return c;
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
            or TileType.TrapPoison or TileType.TrapAlarm or TileType.GasVent)
        {
            map.Tiles[x, y].Type = TileType.Grass;
            return true;
        }
        return false;
    }

    // Blocks audit flood-fill/BFS — impassable tiles only (walls/mountains/deep water/lava);
    // hazard-walkables (Mud/BogWater/Ice/Sand/etc.) stay reachable for routing.
    private static bool IsAuditBlocking(TileType t) =>
        t is TileType.Wall
            or TileType.CrackedWall
            or TileType.Mountain
            or TileType.Tree
            or TileType.TreePine
            or TileType.Rock
            or TileType.WaterDeep
            or TileType.Lava;
}
