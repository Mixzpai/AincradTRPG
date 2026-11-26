using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems.Combat;
using SAOTRPG.Systems.RunState;

namespace SAOTRPG.Systems.Turns;

public partial class TurnManager
{
    private List<(int x, int y)>? _explorePath;
    private int _explorePathIndex;
    private (int x, int y) _exploreTarget = (-1, -1);
    private int _exploreLastRevealCount;

    // SCRATCH FOR THE EXPLORE BFS AND THE A* — flat, row-major (y * Width + x), matching every
    // other per-cell grid in the game, and STAMPED rather than cleared.
    //
    // A repath happens on nearly every auto-explore step (needRepath fires whenever the explored
    // count moves, which is what walking into new ground does), and auto-explore steps on a 60 ms
    // timer. Clearing cost more than searching: the BFS cleared a bool[1M] and an int[1M] — 5 MB
    // of memset — and the A* wrote -1 into two int[1M] grids through 2D indexers, two million
    // element writes, on every single call. A generation counter answers "have I touched this
    // cell THIS search" for free.
    private int[]? _searchStamp;      // shared by the BFS and the A*; they never run concurrently
    private int[]? _searchDist;       // BFS distance / A* g-score
    private int[]? _searchFrom;       // A* parent, encoded x * Height + y
    private int _searchGen;
    private readonly Queue<(int x, int y)> _bfsQueue = new(256);
    private readonly PriorityQueue<(int x, int y), int> _astarOpen = new();

    // Grows on demand rather than being sized once: floors shrink with depth today, so a grid
    // allocated on floor 1 happens to fit every later one — but nothing enforces that, and a
    // bigger floor would have indexed past the end.
    private void EnsureSearchGrids(int cells)
    {
        if (_searchStamp != null && _searchStamp.Length >= cells) return;
        _searchStamp = new int[cells];
        _searchDist = new int[cells];
        _searchFrom = new int[cells];
        _searchGen = 0;
    }

    // A stamp equal to the current generation means "seen in this search". Wrapping resets the
    // table, which is the only clear that ever runs.
    private void BeginSearch()
    {
        if (_searchGen == int.MaxValue) { Array.Clear(_searchStamp!); _searchGen = 0; }
        _searchGen++;
    }

    private void ClearExplorePath()
    {
        _explorePath = null; _explorePathIndex = 0;
        _exploreTarget = (-1, -1); _exploreLastRevealCount = 0;
    }

    public bool AutoExploreStep()
    {
        if (_player.IsDefeated) return false;

        if (_player.CurrentHealth <= _player.MaxHealth / 4)
        {
            _log.LogCombat("Auto-explore stopped — HP critically low!");
            ClearExplorePath(); return false;
        }

        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster) continue;
            int d = Math.Max(Math.Abs(entity.X - _player.X), Math.Abs(entity.Y - _player.Y));
            if (d <= 6 && _map.IsVisible(entity.X, entity.Y))
            {
                _log.Log(FlavorText.AutoExploreEnemyFlavors[RunRng.Next(FlavorText.AutoExploreEnemyFlavors.Length)]);
                ClearExplorePath(); return false;
            }
        }

        if (_map.HasItemsAt(_player.X, _player.Y))
        {
            _log.Log(Input.KeyHints.Resolve(
                FlavorText.AutoExploreItemFlavors[RunRng.Next(FlavorText.AutoExploreItemFlavors.Length)]));
            ClearExplorePath(); return false;
        }

        bool needRepath = _explorePath == null
            || _explorePathIndex >= _explorePath.Count
            || _map.ExploredTileCount != _exploreLastRevealCount
            || IsPathBlocked();

        if (needRepath)
        {
            var target = FindBestExploreTarget();
            if (target.x == -1)
            {
                _log.Log(FlavorText.AutoExploreDoneFlavors[RunRng.Next(FlavorText.AutoExploreDoneFlavors.Length)]);
                ClearExplorePath(); return false;
            }
            var path = AStarPath(_player.X, _player.Y, target.x, target.y);
            if (path == null || path.Count == 0)
            {
                _log.Log(FlavorText.AutoExploreDoneFlavors[RunRng.Next(FlavorText.AutoExploreDoneFlavors.Length)]);
                ClearExplorePath(); return false;
            }
            _explorePath = path; _explorePathIndex = 0;
            _exploreTarget = target; _exploreLastRevealCount = _map.ExploredTileCount;
        }

        var next = _explorePath![_explorePathIndex++];
        ProcessPlayerMove(next.x - _player.X, next.y - _player.Y);
        return true;
    }

    private bool IsPathBlocked()
    {
        if (_explorePath == null) return true;
        for (int i = _explorePathIndex; i < _explorePath.Count; i++)
        {
            var (px, py) = _explorePath[i];
            if (!_map.InBounds(px, py)) return true;
            ref var tile = ref _map.RefTile(px, py);
            if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player)) return true;
        }
        return false;
    }

    private (int x, int y) FindBestExploreTarget()
    {
        int w = _map.Width, h = _map.Height;
        EnsureSearchGrids(w * h);
        BeginSearch();
        int[] stamp = _searchStamp!, dist = _searchDist!;
        int gen = _searchGen;

        var queue = _bfsQueue;
        queue.Clear();
        queue.Enqueue((_player.X, _player.Y));
        stamp[_player.Y * w + _player.X] = gen;
        dist[_player.Y * w + _player.X] = 0;

        (int x, int y) best = (-1, -1), fallback = (-1, -1);
        int bestScore = int.MinValue, bestDist = int.MaxValue, fallbackDist = int.MaxValue;

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            int cd = dist[cy * w + cx];

            if (IsAdjacentToUnexplored(cx, cy))
            {
                int score = ScoreExploreCandidate(cx, cy);
                if (score > 0)
                {
                    if (score > bestScore || (score == bestScore && cd < bestDist))
                    { best = (cx, cy); bestScore = score; bestDist = cd; }
                }
                else if (best.x == -1 && cd < fallbackDist)
                { fallback = (cx, cy); fallbackDist = cd; }
                if (best.x != -1 && cd > bestDist * 3 / 2) break;
            }

            for (int ndx = -1; ndx <= 1; ndx++)
            for (int ndy = -1; ndy <= 1; ndy++)
            {
                if (ndx == 0 && ndy == 0) continue;
                int nx = cx + ndx, ny = cy + ndy;
                if (!_map.InBounds(nx, ny)) continue;
                int ni = ny * w + nx;
                if (stamp[ni] == gen) continue;
                ref var tile = ref _map.RefTile(nx, ny);
                if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player))
                {
                    // Stamp the REJECTION too. A blocked cell is never enqueued either way, so the
                    // flood is unchanged -- but without this every wall beside the frontier is
                    // fetched out of the 16 MB tile grid once per adjacent cell, up to eight times.
                    // Nothing mutates the map during the search, so one answer per cell is enough.
                    stamp[ni] = gen;
                    continue;
                }
                stamp[ni] = gen;
                dist[ni] = cd + 1;
                queue.Enqueue((nx, ny));
            }
        }
        return best.x != -1 ? best : fallback;
    }

    private List<(int x, int y)>? AStarPath(int sx, int sy, int gx, int gy)
    {
        int w = _map.Width, h = _map.Height;
        EnsureSearchGrids(w * h);
        BeginSearch();
        int[] stamp = _searchStamp!, g = _searchDist!, from = _searchFrom!;
        int gen = _searchGen;

        var open = _astarOpen;
        open.Clear();
        int si = sy * w + sx;
        stamp[si] = gen; g[si] = 0; from[si] = -1;
        open.Enqueue((sx, sy), Heuristic(sx, sy, gx, gy));

        while (open.Count > 0)
        {
            var (cx, cy) = open.Dequeue();
            int cg = g[cy * w + cx];
            if (cx == gx && cy == gy) return ReconstructPath(sx, sy, gx, gy, from, w);

            for (int ndx = -1; ndx <= 1; ndx++)
            for (int ndy = -1; ndy <= 1; ndy++)
            {
                if (ndx == 0 && ndy == 0) continue;
                int nx = cx + ndx, ny = cy + ndy;
                if (!_map.InBounds(nx, ny)) continue;
                if (nx != gx || ny != gy)
                {
                    ref var tile = ref _map.RefTile(nx, ny);
                    if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player)) continue;
                }
                int ng = cg + 1;
                int visits = _map.GetVisitCount(nx, ny);
                if (visits > 2) ng += visits / 2;
                int ni = ny * w + nx;
                // An unstamped cell is the old `== -1` sentinel: never reached in this search.
                if (stamp[ni] != gen || ng < g[ni])
                {
                    stamp[ni] = gen;
                    g[ni] = ng;
                    from[ni] = cx * h + cy;
                    open.Enqueue((nx, ny), ng + Heuristic(nx, ny, gx, gy));
                }
            }
        }
        return null;
    }

    private static int Heuristic(int ax, int ay, int bx, int by) =>
        Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));

    private List<(int x, int y)> ReconstructPath(int sx, int sy, int gx, int gy, int[] from, int w)
    {
        int h = _map.Height;
        var path = new List<(int x, int y)>();
        int cx = gx, cy = gy;
        while (cx != sx || cy != sy)
        {
            path.Add((cx, cy));
            int encoded = from[cy * w + cx];
            cx = encoded / h; cy = encoded % h;
        }
        path.Reverse();
        return path;
    }

    // Up to 64 grid probes per frontier cell. The inner ring is skipped for an already-explored
    // neighbour, and RefTile avoids copying a 16-byte tile out of a multi-megabyte grid to read
    // one field. Same probes, same order, same score.
    private int ScoreExploreCandidate(int x, int y)
    {
        int score = 0;
        for (int ndx = -1; ndx <= 1; ndx++)
        for (int ndy = -1; ndy <= 1; ndy++)
        {
            if (ndx == 0 && ndy == 0) continue;
            int ux = x + ndx, uy = y + ndy;
            if (!_map.InBounds(ux, uy) || _map.IsExplored(ux, uy)) continue;

            int exploredFloorNeighbors = 0;
            for (int dx2 = -1; dx2 <= 1; dx2++)
            for (int dy2 = -1; dy2 <= 1; dy2++)
            {
                if (dx2 == 0 && dy2 == 0) continue;
                int nx2 = ux + dx2, ny2 = uy + dy2;
                if (!_map.InBounds(nx2, ny2) || !_map.IsExploredUnchecked(nx2, ny2)) continue;
                var t = _map.RefTile(nx2, ny2).Type;
                if (t is TileType.Floor or TileType.Path or TileType.Door) exploredFloorNeighbors++;
            }
            if (exploredFloorNeighbors >= 2) score += exploredFloorNeighbors;
        }
        score -= _map.GetVisitCount(x, y);
        return score;
    }

    // Evaluated for EVERY cell the explore BFS dequeues, which is most of the reachable floor on
    // every repath. Away from the border all eight neighbours are in bounds by construction, so
    // the interior arm drops eight redundant bounds tests (IsExplored bounds-checks again inside).
    // The result is a plain OR over the eight, so visiting them in a different order cannot change
    // it.
    private bool IsAdjacentToUnexplored(int x, int y)
    {
        if (x > 0 && y > 0 && x < _map.Width - 1 && y < _map.Height - 1)
        {
            return !_map.IsExploredUnchecked(x - 1, y - 1) || !_map.IsExploredUnchecked(x, y - 1)
                || !_map.IsExploredUnchecked(x + 1, y - 1) || !_map.IsExploredUnchecked(x - 1, y)
                || !_map.IsExploredUnchecked(x + 1, y)     || !_map.IsExploredUnchecked(x - 1, y + 1)
                || !_map.IsExploredUnchecked(x, y + 1)     || !_map.IsExploredUnchecked(x + 1, y + 1);
        }

        for (int ndx = -1; ndx <= 1; ndx++)
        for (int ndy = -1; ndy <= 1; ndy++)
        {
            if (ndx == 0 && ndy == 0) continue;
            int nx = x + ndx, ny = y + ndy;
            if (_map.InBounds(nx, ny) && !_map.IsExplored(nx, ny)) return true;
        }
        return false;
    }

    private void CheckFloorCompletion()
    {
        if (_floorFullyExplored) return;
        if (_map.GetExplorationPercent() >= 100)
        {
            _floorFullyExplored = true;
            _log.LogSystem(FlavorText.FloorCompleteMessages[RunRng.Next(FlavorText.FloorCompleteMessages.Length)]);
        }
    }
}
