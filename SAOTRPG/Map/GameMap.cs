using SAOTRPG.Entities;

namespace SAOTRPG.Map;

public class GameMap
{
    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }
    public List<Entity> Entities { get; } = new();

    // Fog of war
    private readonly bool[,] _visible;
    private readonly bool[,] _explored;

    /// <summary>
    /// Tiles that were newly explored this visibility update.
    /// Cleared at the start of each UpdateVisibility call.
    /// Used by MapView for room-reveal flash effect.
    /// </summary>
    public HashSet<(int X, int Y)> NewlyRevealed { get; } = new();

    // Visit tracking for heatmap rendering
    private readonly int[,] _visitCounts;

    private static readonly Tile OutOfBounds = new(TileType.Wall);

    public GameMap(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        _visible = new bool[width, height];
        _explored = new bool[width, height];
        _visitCounts = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Tiles[x, y] = new Tile(TileType.Wall);
    }

    public bool IsVisible(int x, int y) => InBounds(x, y) && _visible[x, y];
    public bool IsExplored(int x, int y) => InBounds(x, y) && _explored[x, y];
    public void SetExplored(int x, int y) { if (InBounds(x, y)) _explored[x, y] = true; }

    /// <summary>Increment the visit counter for a tile (called when player steps on it).</summary>
    public void IncrementVisit(int x, int y) { if (InBounds(x, y)) _visitCounts[x, y]++; }

    /// <summary>Returns how many times the player has visited this tile.</summary>
    public int GetVisitCount(int x, int y) => InBounds(x, y) ? _visitCounts[x, y] : 0;

    /// <summary>Returns the percentage of walkable tiles that have been explored (0-100).</summary>
    public int GetExplorationPercent()
    {
        int walkable = 0, explored = 0;
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            if (Tiles[x, y].Type != TileType.Wall && Tiles[x, y].Type != TileType.Mountain)
            {
                walkable++;
                if (_explored[x, y]) explored++;
            }
        }
        return walkable > 0 ? (int)Math.Round(100.0 * explored / walkable) : 0;
    }

    /// <summary>
    /// Returns 0.0 (center) to 1.0+ (edge/beyond) for visibility falloff.
    /// </summary>
    public double GetVisibilityFalloff(int x, int y, int playerX, int playerY, int radius)
    {
        // Aspect ratio: terminal chars are ~2x taller than wide
        double dx = (x - playerX) * 0.5;  // compress X so circle looks round
        double dy = y - playerY;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        return dist / radius;
    }

    public void UpdateVisibility(int playerX, int playerY, int radius = 18)
    {
        // Clear current visibility and newly-revealed set
        Array.Clear(_visible);
        NewlyRevealed.Clear();

        // Wide scan area — X needs to be wider due to aspect ratio compensation
        int scanX = radius * 2;
        int scanY = radius;

        for (int x = playerX - scanX; x <= playerX + scanX; x++)
        {
            for (int y = playerY - scanY; y <= playerY + scanY; y++)
            {
                if (!InBounds(x, y)) continue;

                double falloff = GetVisibilityFalloff(x, y, playerX, playerY, radius);
                if (falloff <= 1.0)
                {
                    // Raycast to check line-of-sight (walls block vision)
                    if (HasLineOfSight(playerX, playerY, x, y))
                    {
                        _visible[x, y] = true;
                        if (!_explored[x, y])
                        {
                            _explored[x, y] = true;
                            NewlyRevealed.Add((x, y));
                        }
                    }
                }
            }
        }

        // Always see the tile you're on
        if (InBounds(playerX, playerY))
        {
            _visible[playerX, playerY] = true;
            if (!_explored[playerX, playerY])
            {
                _explored[playerX, playerY] = true;
                NewlyRevealed.Add((playerX, playerY));
            }
        }
    }

    /// <summary>Set of door positions that have been opened. Closed doors block LOS.</summary>
    public HashSet<(int X, int Y)> OpenedDoors { get; } = new();

    private bool HasLineOfSight(int x0, int y0, int x1, int y1)
    {
        // Bresenham line — stop if we hit a wall before reaching target
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        int cx = x0, cy = y0;
        while (true)
        {
            // Reached target
            if (cx == x1 && cy == y1) return true;

            // Check if current tile blocks sight (but not the starting tile)
            if ((cx != x0 || cy != y0) && InBounds(cx, cy))
            {
                var t = Tiles[cx, cy].Type;
                if (t is TileType.Wall or TileType.Mountain or TileType.Tree or TileType.TreePine or TileType.Rock)
                    return false;
                // Closed doors block LOS; opened doors don't
                if (t == TileType.Door && !OpenedDoors.Contains((cx, cy)))
                    return false;
            }

            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; cx += sx; }
            if (e2 <= dx) { err += dx; cy += sy; }
        }
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public Tile GetTile(int x, int y) => InBounds(x, y) ? Tiles[x, y] : OutOfBounds;

    public Entity? GetEntityAt(int x, int y) => GetTile(x, y).Occupant;

    public void PlaceEntity(Entity entity, int x, int y)
    {
        entity.X = x;
        entity.Y = y;
        Tiles[x, y].Occupant = entity;
        if (!Entities.Contains(entity))
            Entities.Add(entity);

        // Record spawn position for aggro leashing
        if (entity is Entities.Mob mob)
        {
            mob.SpawnX = x;
            mob.SpawnY = y;
        }
    }

    public void MoveEntity(Entity entity, int newX, int newY)
    {
        if (InBounds(entity.X, entity.Y))
            Tiles[entity.X, entity.Y].Occupant = null;

        entity.X = newX;
        entity.Y = newY;
        Tiles[newX, newY].Occupant = entity;
    }

    public void RemoveEntity(Entity entity)
    {
        if (InBounds(entity.X, entity.Y))
            Tiles[entity.X, entity.Y].Occupant = null;

        Entities.Remove(entity);
    }
}
