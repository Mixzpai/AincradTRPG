using SAOTRPG.Entities;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    private List<(int x, int y)>? _explorePath;
    private int _explorePathIndex;
    private (int x, int y) _exploreTarget = (-1, -1);
    private int _exploreLastRevealCount;
    private bool[,]? _bfsVisited;
    private int[,]? _bfsDist;
    private int[,]? _astarG;
    private int[,]? _astarFrom;

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
                _log.Log(FlavorText.AutoExploreEnemyFlavors[Random.Shared.Next(FlavorText.AutoExploreEnemyFlavors.Length)]);
                ClearExplorePath(); return false;
            }
        }

        if (_map.GetTile(_player.X, _player.Y).HasItems)
        {
            _log.Log(FlavorText.AutoExploreItemFlavors[Random.Shared.Next(FlavorText.AutoExploreItemFlavors.Length)]);
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
                _log.Log(FlavorText.AutoExploreDoneFlavors[Random.Shared.Next(FlavorText.AutoExploreDoneFlavors.Length)]);
                ClearExplorePath(); return false;
            }
            var path = AStarPath(_player.X, _player.Y, target.x, target.y);
            if (path == null || path.Count == 0)
            {
                _log.Log(FlavorText.AutoExploreDoneFlavors[Random.Shared.Next(FlavorText.AutoExploreDoneFlavors.Length)]);
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
            var tile = _map.GetTile(px, py);
            if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player)) return true;
        }
        return false;
    }

    private (int x, int y) FindBestExploreTarget()
    {
        int w = _map.Width, h = _map.Height;
        _bfsVisited ??= new bool[w, h];
        _bfsDist ??= new int[w, h];
        Array.Clear(_bfsVisited, 0, _bfsVisited.Length);
        Array.Clear(_bfsDist, 0, _bfsDist.Length);

        var queue = new Queue<(int x, int y)>(256);
        queue.Enqueue((_player.X, _player.Y));
        _bfsVisited[_player.X, _player.Y] = true;

        (int x, int y) best = (-1, -1), fallback = (-1, -1);
        int bestScore = int.MinValue, bestDist = int.MaxValue, fallbackDist = int.MaxValue;

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            int cd = _bfsDist[cx, cy];

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
                if (!_map.InBounds(nx, ny) || _bfsVisited[nx, ny]) continue;
                var tile = _map.GetTile(nx, ny);
                if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player)) continue;
                _bfsVisited[nx, ny] = true;
                _bfsDist[nx, ny] = cd + 1;
                queue.Enqueue((nx, ny));
            }
        }
        return best.x != -1 ? best : fallback;
    }

    private List<(int x, int y)>? AStarPath(int sx, int sy, int gx, int gy)
    {
        int w = _map.Width, h = _map.Height;
        _astarG ??= new int[w, h];
        _astarFrom ??= new int[w, h];
        for (int i = 0; i < w; i++)
        for (int j = 0; j < h; j++)
        { _astarG[i, j] = -1; _astarFrom[i, j] = -1; }

        var open = new PriorityQueue<(int x, int y), int>();
        _astarG[sx, sy] = 0;
        open.Enqueue((sx, sy), Heuristic(sx, sy, gx, gy));

        while (open.Count > 0)
        {
            var (cx, cy) = open.Dequeue();
            int cg = _astarG[cx, cy];
            if (cx == gx && cy == gy) return ReconstructPath(sx, sy, gx, gy);

            for (int ndx = -1; ndx <= 1; ndx++)
            for (int ndy = -1; ndy <= 1; ndy++)
            {
                if (ndx == 0 && ndy == 0) continue;
                int nx = cx + ndx, ny = cy + ndy;
                if (!_map.InBounds(nx, ny)) continue;
                if (nx != gx || ny != gy)
                {
                    var tile = _map.GetTile(nx, ny);
                    if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player)) continue;
                }
                int ng = cg + 1;
                int visits = _map.GetVisitCount(nx, ny);
                if (visits > 2) ng += visits / 2;
                if (_astarG[nx, ny] == -1 || ng < _astarG[nx, ny])
                {
                    _astarG[nx, ny] = ng;
                    _astarFrom[nx, ny] = cx * h + cy;
                    open.Enqueue((nx, ny), ng + Heuristic(nx, ny, gx, gy));
                }
            }
        }
        return null;
    }

    private static int Heuristic(int ax, int ay, int bx, int by) =>
        Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));

    private List<(int x, int y)> ReconstructPath(int sx, int sy, int gx, int gy)
    {
        int h = _map.Height;
        var path = new List<(int x, int y)>();
        int cx = gx, cy = gy;
        while (cx != sx || cy != sy)
        {
            path.Add((cx, cy));
            int encoded = _astarFrom![cx, cy];
            cx = encoded / h; cy = encoded % h;
        }
        path.Reverse();
        return path;
    }

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
                if (!_map.InBounds(nx2, ny2) || !_map.IsExplored(nx2, ny2)) continue;
                var t = _map.GetTile(nx2, ny2).Type;
                if (t is TileType.Floor or TileType.Path or TileType.Door) exploredFloorNeighbors++;
            }
            if (exploredFloorNeighbors >= 2) score += exploredFloorNeighbors;
        }
        score -= _map.GetVisitCount(x, y);
        return score;
    }

    private bool IsAdjacentToUnexplored(int x, int y)
    {
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
            _log.LogSystem(FlavorText.FloorCompleteMessages[Random.Shared.Next(FlavorText.FloorCompleteMessages.Length)]);
        }
    }
}
