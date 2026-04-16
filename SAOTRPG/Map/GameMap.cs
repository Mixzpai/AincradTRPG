using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Map;

// 2D tile grid representing one floor of the dungeon. Manages entity placement,
// fog-of-war visibility, line-of-sight raycasting, and exploration tracking.
//
// Performance caches (maintained as invariants, must stay in sync with Tiles):
//   _wallGlyphCache : per-tile connected-wall glyph, lazily computed, invalidated
//                     on wall/CrackedWall/Mountain transitions and on their neighbors.
//   _emissiveTiles  : list of tiles whose TileType emits light (campfire, fountain, …).
//                     Refreshed when the cache is invalidated by SetTileType /
//                     InvalidateEmissive.
//   _itemTiles      : set of coordinates that currently hold ground items. Maintained
//                     by AddItem / RemoveItem. HasItems(x,y) is O(1).
//   _bosses/_monsters/_npcs/_allies : typed entity lists, maintained in
//                     PlaceEntity / RemoveEntity so callers avoid .OfType<T>() scans.
//   _walkableCount / _exploredCount : maintained by map gen + SetExplored /
//                     MarkVisible so GetExplorationPercent is O(1).
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

    // Optional sanctuary rectangle — monsters, traps, chests, dens and
    // danger zones are not placed inside this rect during population.
    // Set by Town of Beginnings on Floor 1; null elsewhere.
    public Room? SafeZone { get; set; }

    private readonly bool[,] _visible;
    private readonly bool[,] _explored;
    private readonly int[,] _visitCounts;
    private readonly int[,] _lastSeenTurn;

    // Wall-glyph cache. '\0' means "not yet computed". Non-null read-through
    // cache: callers should invalidate via InvalidateWallGlyph(x,y) whenever
    // a wall-like tile is created or destroyed.
    private readonly char[,] _wallGlyphCache;

    // Transition-border cache for land tiles that sit next to water or lava.
    // 0 = not computed, 1 = no border, 2 = water border (',', Color.Blue),
    // 3 = lava border (',', Color.Red). Keeps the rendering fast path from
    // doing a 3x3 neighbor scan every frame.
    private readonly byte[,] _transitionCache;
    public const byte TransitionUncomputed = 0;
    public const byte TransitionNone       = 1;
    public const byte TransitionWater      = 2;
    public const byte TransitionLava       = 3;

    // Emissive tile list. Lazily built on first access; rebuilt on invalidation.
    // Rendering / lighting iterates this small list (<~100 entries per floor)
    // instead of scanning the whole map.
    private List<(int X, int Y, TileType Type)>? _emissiveTiles;

    // Item-tile index. Kept in lockstep with Tile.Items via AddItem/RemoveItem.
    // NOTE: direct tile.Items.Add / tile.Items.Remove calls bypass this index.
    // All game code routes through the helpers on this type — see TurnManager.Loot.cs.
    private readonly HashSet<(int X, int Y)> _itemTiles = new();

    // Typed entity buckets. Maintained in PlaceEntity / RemoveEntity.
    public List<Boss> Bosses { get; } = new();
    public List<Monster> Monsters { get; } = new();
    public List<NPC> Npcs { get; } = new();
    public List<Ally> Allies { get; } = new();

    // Exploration counts. _walkableCount is initialized once via
    // RecountWalkableTiles (called by MapGenerator after terrain is final).
    // _exploredCount is incremented in SetExplored / MarkVisible.
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

    // Called by MapGenerator after all terrain is placed, and refreshed any time
    // a Wall/Mountain tile turns walkable at runtime (e.g. CrackedWall destruction).
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
        // Only clear visibility in the region the player can see — not the whole map.
        // On a 500x250 map this avoids clearing 125K bools every turn.
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

    // ------------------------------------------------------------------
    // Entity placement
    // ------------------------------------------------------------------

    // Place an entity on the map at (x, y) and register it in the entity list.
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

    // Relocate an entity from its current tile to (newX, newY).
    public void MoveEntity(Entity entity, int newX, int newY)
    {
        if (InBounds(entity.X, entity.Y)) Tiles[entity.X, entity.Y].Occupant = null;
        entity.X = newX; entity.Y = newY;
        Tiles[newX, newY].Occupant = entity;
    }

    // Remove an entity from its tile and the entity list.
    public void RemoveEntity(Entity entity)
    {
        if (InBounds(entity.X, entity.Y)) Tiles[entity.X, entity.Y].Occupant = null;
        if (Entities.Remove(entity)) RemoveTyped(entity);
    }

    private void AddTyped(Entity entity)
    {
        switch (entity)
        {
            // Boss is a Monster subclass; bucket it as Boss AND as Monster so
            // boss-alive checks and monster iteration both see it (preserves
            // previous semantics where a lone Boss satisfied .OfType<Monster>()
            // as well as `e is Boss` checks).
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

    // ------------------------------------------------------------------
    // Item-tile index
    // ------------------------------------------------------------------

    // Add a ground item at (x, y). Preferred over direct tile.Items.Add because
    // this keeps _itemTiles in sync so HasItems(x,y) is O(1).
    public void AddItem(int x, int y, BaseItem item)
    {
        if (!InBounds(x, y)) return;
        Tiles[x, y].Items.Add(item);
        _itemTiles.Add((x, y));
    }

    // Remove a ground item; mirrors AddItem. Returns true if removed.
    public bool RemoveItem(int x, int y, BaseItem item)
    {
        if (!InBounds(x, y)) return false;
        var tile = Tiles[x, y];
        bool removed = tile.Items.Remove(item);
        if (removed && tile.Items.Count == 0) _itemTiles.Remove((x, y));
        return removed;
    }

    // O(1) query — replaces tile.HasItems in render hot paths.
    public bool HasItemsAt(int x, int y) =>
        InBounds(x, y) && _itemTiles.Contains((x, y));

    // ------------------------------------------------------------------
    // Tile-type mutation + wall / emissive cache invalidation
    // ------------------------------------------------------------------

    // Centralized tile-type setter. Callers that use this get automatic wall
    // and emissive cache invalidation. Direct `map.Tiles[x,y].Type = ...` is
    // still allowed (map generation does this ~100K times before any render),
    // but runtime mutations (trap spring, destroy cracked wall, lever-door
    // toggle, …) MUST go through SetTileType OR call the Invalidate* helpers
    // explicitly — otherwise the caches go stale.
    public void SetTileType(int x, int y, TileType newType)
    {
        if (!InBounds(x, y)) return;
        var oldType = Tiles[x, y].Type;
        if (oldType == newType) return;
        Tiles[x, y].Type = newType;
        OnTileTypeChanged(x, y, oldType, newType);
    }

    // Public hook for code that mutates Tile.Type directly (legacy path).
    // Invalidates the wall glyph at (x,y) + its 4 cardinal neighbors, rebuilds
    // the emissive list if the change crossed the emission boundary, and
    // updates walkable count if walkability for exploration changed.
    public void OnTileTypeChanged(int x, int y, TileType oldType, TileType newType)
    {
        bool wasWallLike = IsWallLikeType(oldType);
        bool isWallLike  = IsWallLikeType(newType);
        if (wasWallLike != isWallLike || wasWallLike || isWallLike)
        {
            // Any wall-like boundary change requires invalidating this tile and
            // its cardinal neighbors (the connected-wall glyph reads them).
            InvalidateWallGlyph(x, y);
        }

        // Emissive list: rebuild lazily on next access if either side was emissive.
        if (IsEmissiveType(oldType) || IsEmissiveType(newType))
            _emissiveTiles = null;

        // Transition borders: a water/lava tile gaining or losing a land neighbor
        // (or a land tile flipping to a cared-about type) may change the border
        // of the 3x3 neighborhood.
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

    // Used by rendering. Returns the connected box-drawing glyph for a wall
    // or cracked-wall tile, computing + memoizing on first access.
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

    // Compute (and memoize) the transition-border code for (x,y). Returns
    // one of the Transition* constants. Non-land tiles always return
    // TransitionNone. Caller translates the code back into (glyph, color).
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

    // Invalidates the transition border at (x,y) and its 3x3 neighborhood —
    // any of those tiles may now have gained or lost a water/lava neighbor.
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

    // Emissive tile accessor. Lazy-built from a full-map scan, then reused
    // until invalidated by OnTileTypeChanged.
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

    // Tile types whose presence affects the transition-border cache: land
    // types that can *show* a border, plus water/lava that *are* the border.
    private static bool IsTransitionRelevant(TileType t) => t switch
    {
        TileType.Floor or TileType.Path or TileType.Grass
            or TileType.GrassSparse or TileType.GrassTall => true,
        TileType.Water or TileType.WaterDeep or TileType.Lava => true,
        _ => false,
    };

    // Mirror of LightingSystem.GetEmission's key set. Kept in sync — if a new
    // emissive TileType is added, add it here and to LightingSystem.GetEmission.
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
