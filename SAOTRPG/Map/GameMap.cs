using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// 2D tile grid (flat Tile[] row-major) for one dungeon floor: entity placement, fog-of-war, LOS, exploration.
// Caches (wallGlyph/emissive/itemTiles/typed-entities/walkableCount) kept in sync via SetTileType + Add/Remove helpers.
public class GameMap
{
    private static Tile MakeOutOfBounds() => new(TileType.Wall);

    public int Width { get; }
    public int Height { get; }
    private readonly Tile[] _tiles;

    // Indexer shim for `map.Tiles[x, y].Type = ...` mapgen idiom. Returns ref Tile so
    // direct field mutation through the indexer mutates the underlying array.
    public readonly struct TileAccessor
    {
        private readonly GameMap _map;
        public TileAccessor(GameMap map) { _map = map; }
        public ref Tile this[int x, int y] => ref _map._tiles[y * _map.Width + x];
    }
    public TileAccessor Tiles => new(this);
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

    // Circular playable disk for non-town floors. Set by GenerationPipeline before
    // passes run. Null on F1/F48 hand-built towns and F100 Ruby Palace.
    public bool[,]? CircleMask { get; set; }

    public bool IsInsideCircle(int x, int y) =>
        CircleMask is null
        || ((uint)x < (uint)CircleMask.GetLength(0)
            && (uint)y < (uint)CircleMask.GetLength(1)
            && CircleMask[x, y]);

    // Bundle 10 — strikes remaining per ore tile. Seeded by OreVeinPlacementPass,
    // decremented by mining strike handler, removed on depletion.
    public Dictionary<(int X, int Y), int> VeinStrikesRemaining { get; } = new();

    // True once any animated tile (Lava/Campfire/OreVeinDivine) exists anywhere on this floor.
    // Coarse: never flips back to false on tile removal — false-positive 50ms timer ticks are harmless.
    public bool HasAnimatedTiles { get; private set; }

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

    // Emissive tile list + parallel position-set for O(1) remove. Lazily built, then incremental.
    // Bucket grid (64-cell tiles) for spatial culling around the player.
    private List<(int X, int Y, TileType Type)>? _emissiveTiles;
    private HashSet<(int X, int Y)>? _emissivePositions;
    private Dictionary<(int Bx, int By), List<(int X, int Y, TileType Type)>>? _emissiveBuckets;
    public const int EmissiveBucketShift = 6; // 64-cell buckets

    // Sparse item storage — Items field hoisted off Tile struct. Lazy-allocated on first add.
    // _itemsAt.Keys IS the set of tiles with items (replaces former _itemTiles HashSet).
    private Dictionary<(int X, int Y), List<BaseItem>>? _itemsAt;
    private static readonly List<BaseItem> EmptyItems = new(0);

    // Sparse linked-door storage — extremely rare (lever/pressure plate tiles only).
    private Dictionary<(int X, int Y), (int X, int Y)>? _linkedDoorAt;

    // Public render-side iteration of item tiles. Empty-collection fallback when no items dict allocated.
    public IReadOnlyCollection<(int X, int Y)> ItemTiles =>
        _itemsAt != null ? (IReadOnlyCollection<(int X, int Y)>)_itemsAt.Keys
                         : Array.Empty<(int X, int Y)>();

    // Feature indexes for overlay rendering — built lazily on first access, kept incremental
    // by OnTileTypeChanged. Iteration becomes O(N_features) instead of O(viewport).
    private List<(int X, int Y)>? _gasVents;
    private List<(int X, int Y)>? _shrines;
    public IReadOnlyList<(int X, int Y)> GasVents { get { EnsureFeatureIndexes(); return _gasVents!; } }
    public IReadOnlyList<(int X, int Y)> Shrines { get { EnsureFeatureIndexes(); return _shrines!; } }

    private void EnsureFeatureIndexes()
    {
        if (_gasVents != null) return;
        _gasVents = new List<(int, int)>();
        _shrines = new List<(int, int)>();
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            var t = _tiles[Idx(x, y)].Type;
            if (t == TileType.GasVent) _gasVents.Add((x, y));
            else if (IsShrineLikeType(t)) _shrines.Add((x, y));
        }
    }

    private static bool IsShrineLikeType(TileType t) =>
        t is TileType.Shrine or TileType.EnchantShrine or TileType.Fountain;

    private static void RemovePosFromList(List<(int X, int Y)> list, int x, int y)
    {
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i].X == x && list[i].Y == y) { list.RemoveAt(i); return; }
    }

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
        _tiles = new Tile[width * height];
        _visible = new bool[width, height];
        _explored = new bool[width, height];
        _visitCounts = new int[width, height];
        _lastSeenTurn = new int[width, height];
        _wallGlyphCache = new char[width, height];
        _transitionCache = new byte[width, height];
        Lighting = new LightingSystem(width, height);
        // TileType.Grass is 0 → struct default would be Grass; explicitly seed Wall to match prior behavior
        // (mapgen overwrites; rare runtime allocation paths still expect a Wall floor).
        for (int i = 0; i < _tiles.Length; i++)
            _tiles[i] = new Tile(TileType.Wall);
    }

    private int Idx(int x, int y) => y * Width + x;

    // Hot-path: no bounds check. Caller validates via InBounds.
    public ref Tile RefTile(int x, int y) => ref _tiles[y * Width + x];

    // Returns a copy. Out-of-bounds yields a fresh Wall sentinel (matches pre-refactor OutOfBounds semantics).
    public Tile GetTile(int x, int y) => InBounds(x, y) ? _tiles[y * Width + x] : MakeOutOfBounds();

    public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
    // Strictly inside the map (one-tile border excluded). Used by mapgen.
    public bool InInterior(int x, int y) => x > 0 && y > 0 && x < Width - 1 && y < Height - 1;
    public bool IsVisible(int x, int y) => InBounds(x, y) && _visible[x, y];
    public bool IsExplored(int x, int y) => InBounds(x, y) && _explored[x, y];
    public void IncrementVisit(int x, int y) { if (InBounds(x, y)) _visitCounts[x, y]++; }
    public int GetVisitCount(int x, int y) => InBounds(x, y) ? _visitCounts[x, y] : 0;
    public int GetLastSeenTurn(int x, int y) => InBounds(x, y) ? _lastSeenTurn[x, y] : 0;

    public void SetExplored(int x, int y)
    {
        if (InBounds(x, y) && !_explored[x, y])
        {
            _explored[x, y] = true;
            ExploredTileCount++;
            if (IsWalkableForExploration(_tiles[Idx(x, y)].Type)) _exploredCount++;
        }
    }

    // Called by MapGenerator after terrain placement; refreshed on runtime walkability changes (e.g. CrackedWall destroy).
    public void RecountWalkableTiles()
    {
        _walkableCount = 0;
        _exploredCount = 0;
        bool anyAnimated = false;
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            var t = _tiles[Idx(x, y)].Type;
            if (!anyAnimated && TileDefinitions.IsAnimated(t)) anyAnimated = true;
            if (!IsWalkableForExploration(t)) continue;
            _walkableCount++;
            if (_explored[x, y]) _exploredCount++;
        }
        HasAnimatedTiles = anyAnimated;
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
        using var _ = Profiler.Begin("Visibility.Update");
        // Clear visibility only in the player's visible region (avoids 125K bool clear on 500x250 map).
        int clearR = radius + 2;
        int x0 = Math.Max(0, playerX - clearR), x1 = Math.Min(Width, playerX + clearR + 1);
        int y0 = Math.Max(0, playerY - clearR), y1 = Math.Min(Height, playerY + clearR + 1);
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
                _visible[x, y] = false;

        NewlyRevealed.Clear();
        using (Profiler.Begin("FOV.Compute"))
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
            if (IsWalkableForExploration(_tiles[Idx(x, y)].Type)) _exploredCount++;
            NewlyRevealed.Add((x, y));
        }
    }

    private bool BlocksSight(int x, int y)
    {
        if (!InBounds(x, y)) return true;
        var t = _tiles[Idx(x, y)].Type;
        if (t is TileType.Wall or TileType.Mountain or TileType.Tree or TileType.TreePine or TileType.Rock)
            return true;
        if (t == TileType.Door && !OpenedDoors.Contains((x, y))) return true;
        return false;
    }

    // --- Entity placement ---

    public void PlaceEntity(Entity entity, int x, int y)
    {
        entity.X = x; entity.Y = y;
        _tiles[Idx(x, y)].Occupant = entity;
        if (!Entities.Contains(entity))
        {
            Entities.Add(entity);
            AddTyped(entity);
        }
        if (entity is Mob mob) { mob.SpawnX = x; mob.SpawnY = y; }
    }

    public void MoveEntity(Entity entity, int newX, int newY)
    {
        if (InBounds(entity.X, entity.Y)) _tiles[Idx(entity.X, entity.Y)].Occupant = null;
        entity.X = newX; entity.Y = newY;
        _tiles[Idx(newX, newY)].Occupant = entity;
    }

    public void RemoveEntity(Entity entity)
    {
        if (InBounds(entity.X, entity.Y)) _tiles[Idx(entity.X, entity.Y)].Occupant = null;
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

    // --- Sparse Items API (replaces former Tile.Items list) ---

    // O(1) check; lazy-dict friendly. Replaces former Tile.HasItems property + _itemTiles HashSet.
    public bool HasItemsAt(int x, int y) => _itemsAt != null && _itemsAt.ContainsKey((x, y));

    public IReadOnlyList<BaseItem> GetItemsAt(int x, int y)
    {
        if (_itemsAt != null && _itemsAt.TryGetValue((x, y), out var list)) return list;
        return EmptyItems;
    }

    // Mutable access for callers that iterate-and-remove (Loot pickup loop).
    public List<BaseItem>? GetItemsListOrNull(int x, int y)
    {
        if (_itemsAt != null && _itemsAt.TryGetValue((x, y), out var list)) return list;
        return null;
    }

    public int GetItemCountAt(int x, int y) =>
        _itemsAt != null && _itemsAt.TryGetValue((x, y), out var list) ? list.Count : 0;

    // Lazy-allocates the sparse map + the per-tile list on first use.
    public void AddItem(int x, int y, BaseItem item)
    {
        if (!InBounds(x, y)) return;
        _itemsAt ??= new Dictionary<(int X, int Y), List<BaseItem>>();
        if (!_itemsAt.TryGetValue((x, y), out var list))
        {
            list = new List<BaseItem>();
            _itemsAt[(x, y)] = list;
        }
        list.Add(item);
    }

    // Drops the dict entry when last item removed → HasItemsAt stays accurate.
    public bool RemoveItem(int x, int y, BaseItem item)
    {
        if (!InBounds(x, y) || _itemsAt == null) return false;
        if (!_itemsAt.TryGetValue((x, y), out var list)) return false;
        bool removed = list.Remove(item);
        if (removed && list.Count == 0) _itemsAt.Remove((x, y));
        return removed;
    }

    // --- Sparse LinkedDoor API (lever/pressure plate → door coord) ---

    public (int X, int Y)? GetLinkedDoor(int x, int y) =>
        _linkedDoorAt != null && _linkedDoorAt.TryGetValue((x, y), out var d) ? d : null;

    public void SetLinkedDoor(int x, int y, int dx, int dy)
    {
        _linkedDoorAt ??= new Dictionary<(int X, int Y), (int X, int Y)>();
        _linkedDoorAt[(x, y)] = (dx, dy);
    }

    // --- Tile mutation + cache invalidation ---

    // Centralized setter with auto wall/emissive invalidation. Direct RefTile(x,y).Type = ... is allowed in mapgen,
    // but runtime mutations MUST use this OR call Invalidate* helpers or caches go stale.
    public void SetTileType(int x, int y, TileType newType)
    {
        if (!InBounds(x, y)) return;
        ref var tile = ref _tiles[Idx(x, y)];
        var oldType = tile.Type;
        if (oldType == newType) return;
        tile.Type = newType;
        OnTileTypeChanged(x, y, oldType, newType);
    }

    // Direct mutation of TrapHidden — the only non-Type field that runtime writes flip frequently.
    // Trap-hidden flag flips the rendered tile type (Floor → real trap), so visual cache must invalidate.
    public void SetTrapHidden(int x, int y, bool hidden)
    {
        if (!InBounds(x, y)) return;
        ref var t = ref _tiles[Idx(x, y)];
        if (t.TrapHidden == hidden) return;
        t.TrapHidden = hidden;
        TileVisualInvalidated?.Invoke(x, y);
    }

    // Cache-invalidation event for per-cell visual changes that don't flip the tile Type.
    // Subscribers (TileVisualCache) should drop the slot at (x, y).
    public event Action<int, int>? TileVisualInvalidated;

    // Subscribers (TileVisualCache, frame-cache) listen here to invalidate per-cell state.
    public event Action<int, int, TileType, TileType>? TileTypeChanged;

    // Hook for direct Tile.Type mutations: invalidates wall glyph + 4 cardinals, rebuilds emissive if boundary crossed,
    // updates walkable count on walkability change. Wave 2 caches subscribe to this surface.
    public void OnTileTypeChanged(int x, int y, TileType oldType, TileType newType)
    {
        // === H12 visual cache === fires before internal caches so external subscribers see post-tile-flip state.
        TileTypeChanged?.Invoke(x, y, oldType, newType);
        // === H3 frame cache === Tile mutation invalidates the cached frame buffer.
        UI.MapView.MarkFrameDirty();

        // Sticky-true once any animated tile appears. Wave 1 — drives 50ms timer gate.
        if (!HasAnimatedTiles && TileDefinitions.IsAnimated(newType))
            HasAnimatedTiles = true;

        bool wasWallLike = IsWallLikeType(oldType);
        bool isWallLike  = IsWallLikeType(newType);
        if (wasWallLike != isWallLike || wasWallLike || isWallLike)
        {
            // Wall boundary change — invalidate this tile + cardinals (connected-glyph reads neighbors).
            InvalidateWallGlyph(x, y);
        }

        // Emissive list incremental update; rebuild fully only if not yet built.
        bool wasEmissive = IsEmissiveType(oldType);
        bool isEmissive = IsEmissiveType(newType);
        if ((wasEmissive || isEmissive) && _emissiveTiles != null)
        {
            if (wasEmissive) RemoveEmissiveAt(x, y, oldType);
            if (isEmissive) AddEmissiveAt(x, y, newType);
        }

        // Feature index incremental update — only if already built; otherwise lazy build covers it.
        if (_gasVents != null)
        {
            if (oldType == TileType.GasVent) RemovePosFromList(_gasVents, x, y);
            if (newType == TileType.GasVent) _gasVents.Add((x, y));
            bool wasShrine = IsShrineLikeType(oldType);
            bool isShrine = IsShrineLikeType(newType);
            if (wasShrine) RemovePosFromList(_shrines!, x, y);
            if (isShrine) _shrines!.Add((x, y));
        }

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
        if (!InBounds(x, y)) return '○';
        char cached = _wallGlyphCache[x, y];
        if (cached != '\0') return cached;

        bool n = IsWallLikeAt(x, y - 1);
        bool s = IsWallLikeAt(x, y + 1);
        bool e = IsWallLikeAt(x + 1, y);
        bool w = IsWallLikeAt(x - 1, y);
        char g = (n, s, e, w) switch
        {
            (true, true, true, true)     => '┼',
            (true, true, true, false)    => '├',
            (true, true, false, true)    => '┤',
            (true, false, true, true)    => '┴',
            (false, true, true, true)    => '┬',
            (true, true, false, false)   => '│',
            (false, false, true, true)   => '─',
            (true, false, true, false)   => '└',
            (true, false, false, true)   => '┘',
            (false, true, true, false)   => '┌',
            (false, true, false, true)   => '┐',
            (true, false, false, false)  => '│',
            (false, true, false, false)  => '│',
            (false, false, true, false)  => '─',
            (false, false, false, true)  => '─',
            _                            => '○',
        };
        _wallGlyphCache[x, y] = g;
        return g;
    }

    private bool IsWallLikeAt(int x, int y) =>
        InBounds(x, y) && IsWallLikeType(_tiles[Idx(x, y)].Type);

    // Compute + memoize transition-border code at (x,y). Non-land tiles return TransitionNone.
    public byte GetTransitionCode(int x, int y)
    {
        if (!InBounds(x, y)) return TransitionNone;
        byte cached = _transitionCache[x, y];
        if (cached != TransitionUncomputed) return cached;

        var t = _tiles[Idx(x, y)].Type;
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
            var adj = _tiles[Idx(nx, ny)].Type;
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

    // Lazy full-map scan once; subsequent updates incremental via OnTileTypeChanged.
    public IReadOnlyList<(int X, int Y, TileType Type)> EmissiveTiles
    {
        get
        {
            EnsureEmissiveBuilt();
            return _emissiveTiles!;
        }
    }

    private void EnsureEmissiveBuilt()
    {
        if (_emissiveTiles != null) return;
        _emissiveTiles = new List<(int, int, TileType)>();
        _emissivePositions = new HashSet<(int, int)>();
        _emissiveBuckets = new Dictionary<(int, int), List<(int, int, TileType)>>();
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            var t = _tiles[Idx(x, y)].Type;
            if (IsEmissiveType(t)) AddEmissiveAt(x, y, t);
        }
    }

    private void AddEmissiveAt(int x, int y, TileType t)
    {
        if (_emissiveTiles == null) return;
        if (!_emissivePositions!.Add((x, y))) return;
        var entry = (x, y, t);
        _emissiveTiles.Add(entry);
        var key = (x >> EmissiveBucketShift, y >> EmissiveBucketShift);
        if (!_emissiveBuckets!.TryGetValue(key, out var bucket))
        {
            bucket = new List<(int, int, TileType)>();
            _emissiveBuckets[key] = bucket;
        }
        bucket.Add(entry);
    }

    private void RemoveEmissiveAt(int x, int y, TileType t)
    {
        if (_emissiveTiles == null) return;
        if (!_emissivePositions!.Remove((x, y))) return;
        for (int i = _emissiveTiles.Count - 1; i >= 0; i--)
        {
            var e = _emissiveTiles[i];
            if (e.X == x && e.Y == y) { _emissiveTiles.RemoveAt(i); break; }
        }
        var key = (x >> EmissiveBucketShift, y >> EmissiveBucketShift);
        if (_emissiveBuckets!.TryGetValue(key, out var bucket))
        {
            for (int i = bucket.Count - 1; i >= 0; i--)
            {
                var e = bucket[i];
                if (e.X == x && e.Y == y) { bucket.RemoveAt(i); break; }
            }
            if (bucket.Count == 0) _emissiveBuckets.Remove(key);
        }
    }

    // Iterate emissives whose bucket intersects the player ROI; bucketRadius is in 64-cell buckets.
    public IEnumerable<(int X, int Y, TileType Type)> EmissiveTilesNear(int playerX, int playerY, int bucketRadius)
    {
        EnsureEmissiveBuilt();
        int bx = playerX >> EmissiveBucketShift;
        int by = playerY >> EmissiveBucketShift;
        for (int dby = -bucketRadius; dby <= bucketRadius; dby++)
        for (int dbx = -bucketRadius; dbx <= bucketRadius; dbx++)
        {
            if (_emissiveBuckets!.TryGetValue((bx + dbx, by + dby), out var bucket))
            {
                for (int i = 0; i < bucket.Count; i++) yield return bucket[i];
            }
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
