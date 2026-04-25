using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Map;

// 2D tile grid for one dungeon floor: entity placement, fog-of-war, LOS, exploration.
// Caches (wallGlyph/emissive/itemTiles/typed-entities/walkableCount) kept in sync via SetTileType + Add/Remove helpers.
public class GameMap
{
    private static readonly Tile OutOfBounds = new(TileType.Wall);

    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }
    public List<Entity> Entities { get; } = new();
    public HashSet<(int X, int Y)> NewlyRevealed { get; } = new();
    public HashSet<(int X, int Y)> OpenedDoors { get; } = new();
    public int ExploredTileCount { get; private set; }
    public LightingSystem Lighting { get; }

    // Sanctuary rect — no monsters/traps/chests/dens/danger zones placed inside.
    // Set by Town of Beginnings on Floor 1; null elsewhere.
    public Room? SafeZone { get; set; }

    // Heightmap data for the Shift+G debug overlay. Populated by HeightmapPass
    // on generation. UI-thread-only; not threadsafe; not serialized.
    public float[,]? DebugHeights { get; set; }

    // Bundle 10 — strikes remaining per ore tile. Seeded by OreVeinPlacementPass,
    // decremented by mining strike handler, removed on depletion.
    public Dictionary<(int X, int Y), int> VeinStrikesRemaining { get; } = new();

    private readonly bool[,] _visible;
    private readonly bool[,] _explored;
    private readonly int[,] _visitCounts;
    private readonly int[,] _lastSeenTurn;

    // Wall-glyph cache; '\0' = uncomputed. Invalidate via InvalidateWallGlyph when wall-like tiles change.
    private readonly char[,] _wallGlyphCache;

    // Transition-border cache for land tiles adjacent to water/lava.
    // 0=uncomputed, 1=none, 2=water (',', Blue), 3=lava (',', Red). Avoids per-frame 3x3 neighbor scans.
    private readonly byte[,] _transitionCache;
    public const byte TransitionUncomputed = 0;
    public const byte TransitionNone       = 1;
    public const byte TransitionWater      = 2;
    public const byte TransitionLava       = 3;

    // Emissive tile list — lazily built, small (<~100/floor). Avoids full-map scans per frame.
    private List<(int X, int Y, TileType Type)>? _emissiveTiles;

    // Item-tile index, kept in lockstep with Tile.Items via AddItem/RemoveItem.
    // Direct tile.Items.Add/Remove bypasses this index — always go through these helpers.
    private readonly HashSet<(int X, int Y)> _itemTiles = new();

    // Typed entity buckets. Maintained in PlaceEntity / RemoveEntity.
    public List<Boss> Bosses { get; } = new();
    public List<Monster> Monsters { get; } = new();
    public List<NPC> Npcs { get; } = new();
    public List<Ally> Allies { get; } = new();

    // Exploration counts. _walkableCount seeded by RecountWalkableTiles after mapgen; _exploredCount in SetExplored/MarkVisible.
    private int _walkableCount;
    private int _exploredCount;

    public GameMap(int width, int height)
    {
        Width = width; Height = height;
        Tiles = new Tile[width, height];
        _visible = new bool[width, height];
        _explored = new bool[width, height];
        _visitCounts = new int[width, height];
        _lastSeenTurn = new int[width, height];
        _wallGlyphCache = new char[width, height];
        _transitionCache = new byte[width, height];
        Lighting = new LightingSystem(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Tiles[x, y] = new Tile(TileType.Wall);
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    // Strictly inside the map (one-tile border excluded). Used by mapgen.
    public bool InInterior(int x, int y) => x > 0 && y > 0 && x < Width - 1 && y < Height - 1;
    public bool IsVisible(int x, int y) => InBounds(x, y) && _visible[x, y];
    public bool IsExplored(int x, int y) => InBounds(x, y) && _explored[x, y];
    public Tile GetTile(int x, int y) => InBounds(x, y) ? Tiles[x, y] : OutOfBounds;
    public void IncrementVisit(int x, int y) { if (InBounds(x, y)) _visitCounts[x, y]++; }
    public int GetVisitCount(int x, int y) => InBounds(x, y) ? _visitCounts[x, y] : 0;
    public int GetLastSeenTurn(int x, int y) => InBounds(x, y) ? _lastSeenTurn[x, y] : 0;

    public void SetExplored(int x, int y)
    {
        if (InBounds(x, y) && !_explored[x, y])
        {
            _explored[x, y] = true;
            ExploredTileCount++;
            if (IsWalkableForExploration(Tiles[x, y].Type)) _exploredCount++;
        }
    }

    // Called by MapGenerator after terrain placement; refreshed on runtime walkability changes (e.g. CrackedWall destroy).
    public void RecountWalkableTiles()
    {
        _walkableCount = 0;
        _exploredCount = 0;
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            if (!IsWalkableForExploration(Tiles[x, y].Type)) continue;
            _walkableCount++;
            if (_explored[x, y]) _exploredCount++;
        }
    }

    private static bool IsWalkableForExploration(TileType t) =>
        t != TileType.Wall && t != TileType.Mountain;

    public int GetExplorationPercent()
    {
        return _walkableCount > 0
            ? (int)Math.Round(100.0 * _exploredCount / _walkableCount)
            : 0;
    }

    public void UpdateVisibility(int playerX, int playerY, int radius = 80)
    {
        // Clear visibility only in the player's visible region (avoids 125K bool clear on 500x250 map).
        int clearR = radius + 2;
        int x0 = Math.Max(0, playerX - clearR), x1 = Math.Min(Width, playerX + clearR + 1);
        int y0 = Math.Max(0, playerY - clearR), y1 = Math.Min(Height, playerY + clearR + 1);
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
                _visible[x, y] = false;

        NewlyRevealed.Clear();
        Shadowcaster.Compute(playerX, playerY, radius, BlocksSight, MarkVisible);
        Lighting.Update(this, playerX, playerY);
    }

    // Public opacity test used by LightingSystem for light propagation.
    public bool IsOpaque(int x, int y) => BlocksSight(x, y);

    private void MarkVisible(int x, int y)
    {
        if (!InBounds(x, y)) return;
        _visible[x, y] = true;
        _lastSeenTurn[x, y] = DayNightCycle.CurrentTurn;
        if (!_explored[x, y])
        {
            _explored[x, y] = true;
            ExploredTileCount++;
            if (IsWalkableForExploration(Tiles[x, y].Type)) _exploredCount++;
            NewlyRevealed.Add((x, y));
        }
    }

    private bool BlocksSight(int x, int y)
    {
        if (!InBounds(x, y)) return true;
        var t = Tiles[x, y].Type;
        if (t is TileType.Wall or TileType.Mountain or TileType.Tree or TileType.TreePine or TileType.Rock)
            return true;
        if (t == TileType.Door && !OpenedDoors.Contains((x, y))) return true;
        return false;
    }

    // --- Entity placement ---

    public void PlaceEntity(Entity entity, int x, int y)
    {
        entity.X = x; entity.Y = y;
        Tiles[x, y].Occupant = entity;
        if (!Entities.Contains(entity))
        {
            Entities.Add(entity);
            AddTyped(entity);
        }
        if (entity is Mob mob) { mob.SpawnX = x; mob.SpawnY = y; }
    }

    public void MoveEntity(Entity entity, int newX, int newY)
    {
        if (InBounds(entity.X, entity.Y)) Tiles[entity.X, entity.Y].Occupant = null;
        entity.X = newX; entity.Y = newY;
        Tiles[newX, newY].Occupant = entity;
    }

    public void RemoveEntity(Entity entity)
    {
        if (InBounds(entity.X, entity.Y)) Tiles[entity.X, entity.Y].Occupant = null;
        if (Entities.Remove(entity)) RemoveTyped(entity);
    }

    private void AddTyped(Entity entity)
    {
        switch (entity)
        {
            // Boss is a Monster subclass — bucket into both so boss-alive checks and monster iteration see it.
            case Boss b:     Bosses.Add(b);   Monsters.Add(b); break;
            case Monster m:  Monsters.Add(m); break;
            case NPC n:      Npcs.Add(n);     break;
            case Ally a:     Allies.Add(a);   break;
        }
    }

    private void RemoveTyped(Entity entity)
    {
        switch (entity)
        {
            case Boss b:     Bosses.Remove(b);   Monsters.Remove(b); break;
            case Monster m:  Monsters.Remove(m); break;
            case NPC n:      Npcs.Remove(n);     break;
            case Ally a:     Allies.Remove(a);   break;
        }
    }

    // --- Item-tile index ---

    // Preferred over direct tile.Items.Add — keeps _itemTiles in sync so HasItems is O(1).
    public void AddItem(int x, int y, BaseItem item)
    {
        if (!InBounds(x, y)) return;
        Tiles[x, y].Items.Add(item);
        _itemTiles.Add((x, y));
    }

    public bool RemoveItem(int x, int y, BaseItem item)
    {
        if (!InBounds(x, y)) return false;
        var tile = Tiles[x, y];
        bool removed = tile.Items.Remove(item);
        if (removed && tile.Items.Count == 0) _itemTiles.Remove((x, y));
        return removed;
    }

    // O(1) render-hot-path query.
    public bool HasItemsAt(int x, int y) =>
        InBounds(x, y) && _itemTiles.Contains((x, y));

    // --- Tile mutation + cache invalidation ---

    // Centralized setter with auto wall/emissive invalidation. Direct Tiles[x,y].Type = ... is allowed in mapgen,
    // but runtime mutations MUST use this OR call Invalidate* helpers or caches go stale.
    public void SetTileType(int x, int y, TileType newType)
    {
        if (!InBounds(x, y)) return;
        var oldType = Tiles[x, y].Type;
        if (oldType == newType) return;
        Tiles[x, y].Type = newType;
        OnTileTypeChanged(x, y, oldType, newType);
    }

    // Hook for direct Tile.Type mutations: invalidates wall glyph + 4 cardinals, rebuilds emissive if boundary crossed,
    // updates walkable count on walkability change.
    public void OnTileTypeChanged(int x, int y, TileType oldType, TileType newType)
    {
        bool wasWallLike = IsWallLikeType(oldType);
        bool isWallLike  = IsWallLikeType(newType);
        if (wasWallLike != isWallLike || wasWallLike || isWallLike)
        {
            // Wall boundary change — invalidate this tile + cardinals (connected-glyph reads neighbors).
            InvalidateWallGlyph(x, y);
        }

        // Emissive list rebuilds lazily on next access.
        if (IsEmissiveType(oldType) || IsEmissiveType(newType))
            _emissiveTiles = null;

        // Transition borders in 3x3 neighborhood may shift on land/water/lava boundary changes.
        if (IsTransitionRelevant(oldType) || IsTransitionRelevant(newType))
            InvalidateTransition(x, y);

        bool wasWalkable = IsWalkableForExploration(oldType);
        bool isWalkable  = IsWalkableForExploration(newType);
        if (wasWalkable != isWalkable)
        {
            if (isWalkable)
            {
                _walkableCount++;
                if (_explored[x, y]) _exploredCount++;
            }
            else
            {
                _walkableCount--;
                if (_explored[x, y]) _exploredCount--;
            }
        }
    }

    // Invalidate the wall-glyph cache at (x,y) and its 4 cardinal neighbors.
    public void InvalidateWallGlyph(int x, int y)
    {
        if (InBounds(x, y))         _wallGlyphCache[x, y]     = '\0';
        if (InBounds(x - 1, y))     _wallGlyphCache[x - 1, y] = '\0';
        if (InBounds(x + 1, y))     _wallGlyphCache[x + 1, y] = '\0';
        if (InBounds(x, y - 1))     _wallGlyphCache[x, y - 1] = '\0';
        if (InBounds(x, y + 1))     _wallGlyphCache[x, y + 1] = '\0';
    }

    // Returns connected box-drawing glyph for a wall/cracked-wall, memoized on first access.
    public char GetWallGlyph(int x, int y)
    {
        if (!InBounds(x, y)) return '\u25CB';
        char cached = _wallGlyphCache[x, y];
        if (cached != '\0') return cached;

        bool n = IsWallLikeAt(x, y - 1);
        bool s = IsWallLikeAt(x, y + 1);
        bool e = IsWallLikeAt(x + 1, y);
        bool w = IsWallLikeAt(x - 1, y);
        char g = (n, s, e, w) switch
        {
            (true, true, true, true)     => '\u253C',
            (true, true, true, false)    => '\u251C',
            (true, true, false, true)    => '\u2524',
            (true, false, true, true)    => '\u2534',
            (false, true, true, true)    => '\u252C',
            (true, true, false, false)   => '\u2502',
            (false, false, true, true)   => '\u2500',
            (true, false, true, false)   => '\u2514',
            (true, false, false, true)   => '\u2518',
            (false, true, true, false)   => '\u250C',
            (false, true, false, true)   => '\u2510',
            (true, false, false, false)  => '\u2502',
            (false, true, false, false)  => '\u2502',
            (false, false, true, false)  => '\u2500',
            (false, false, false, true)  => '\u2500',
            _                            => '\u25CB',
        };
        _wallGlyphCache[x, y] = g;
        return g;
    }

    private bool IsWallLikeAt(int x, int y) =>
        InBounds(x, y) && IsWallLikeType(Tiles[x, y].Type);

    // Compute + memoize transition-border code at (x,y). Non-land tiles return TransitionNone.
    public byte GetTransitionCode(int x, int y)
    {
        if (!InBounds(x, y)) return TransitionNone;
        byte cached = _transitionCache[x, y];
        if (cached != TransitionUncomputed) return cached;

        var t = Tiles[x, y].Type;
        if (t is not (TileType.Floor or TileType.Path or TileType.Grass
            or TileType.GrassSparse or TileType.GrassTall))
        {
            _transitionCache[x, y] = TransitionNone;
            return TransitionNone;
        }

        byte result = TransitionNone;
        for (int dx = -1; dx <= 1 && result == TransitionNone; dx++)
        for (int dy = -1; dy <= 1 && result == TransitionNone; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!InBounds(nx, ny)) continue;
            var adj = Tiles[nx, ny].Type;
            if (adj is TileType.Water or TileType.WaterDeep) result = TransitionWater;
            else if (adj == TileType.Lava) result = TransitionLava;
        }
        _transitionCache[x, y] = result;
        return result;
    }

    // Invalidates transition cache at (x,y) + 3x3 neighborhood.
    public void InvalidateTransition(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            int nx = x + dx, ny = y + dy;
            if (InBounds(nx, ny)) _transitionCache[nx, ny] = TransitionUncomputed;
        }
    }

    private static bool IsWallLikeType(TileType t) =>
        t is TileType.Wall or TileType.CrackedWall or TileType.Mountain;

    // Lazy-built from full-map scan, reused until invalidated by OnTileTypeChanged.
    public IReadOnlyList<(int X, int Y, TileType Type)> EmissiveTiles
    {
        get
        {
            if (_emissiveTiles != null) return _emissiveTiles;
            var list = new List<(int, int, TileType)>();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var t = Tiles[x, y].Type;
                if (IsEmissiveType(t)) list.Add((x, y, t));
            }
            _emissiveTiles = list;
            return list;
        }
    }

    // Types that affect transition-border cache: land types that show a border + water/lava that are the border.
    private static bool IsTransitionRelevant(TileType t) => t switch
    {
        TileType.Floor or TileType.Path or TileType.Grass
            or TileType.GrassSparse or TileType.GrassTall => true,
        TileType.Water or TileType.WaterDeep or TileType.Lava => true,
        _ => false,
    };

    // Mirrors LightingSystem.GetEmission — add new emissives here AND there.
    private static bool IsEmissiveType(TileType t) => t switch
    {
        TileType.Campfire          => true,
        TileType.Lava              => true,
        TileType.Shrine            => true,
        TileType.EnchantShrine     => true,
        TileType.Fountain          => true,
        TileType.StairsUp          => true,
        TileType.LoreStone         => true,
        TileType.GasVent           => true,
        TileType.Anvil             => true,
        TileType.Door              => true,
        TileType.LabyrinthEntrance => true,
        _                          => false,
    };
}
