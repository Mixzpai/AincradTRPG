using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Rendering;

// Top-right scaled floor overview; each cell = region of tiles.
// Perf: two cached layers updated incrementally (see Sample); SampleRegion is the reference.
public class MinimapView : View
{
    private GameMap _map;
    private readonly Player _player;

    public const char PlayerMarker = '@';
    private const char EntityMarker = '*', ItemMarker = '!';
    private const char DoorMarker = '+', StairsMarker = '>', ShrineMarker = 'S', CampfireMarker = '&';
    // A boss is the one occupant worth re-routing around, so it does not share the monster
    // marker. It must not share the mithril vein's glyph either: colour is the only thing
    // separating them and Amber Mono collapses colour.
    private const char BossMarker = 'Ω';
    // A secret shrine used to share the dropped-item marker.
    private const char SecretMarker = '?';
    // An ally, a townsperson and a monster all painted the monster marker, told
    // apart by colour — which is exactly what a theme may collapse. Friend and foe
    // is the one distinction on this widget worth a glyph of its own.
    private const char FriendMarker = 'n';
    // Open-ground landmarks. Prefabs used to reach only town rooms and the boss arena, so a chest
    // or an anvil out in the wilderness did not exist to mark; they do now, and they are exactly
    // what a player crosses a floor for.
    private const char ChestMarker = '$';
    private const char AnvilMarker = 'A';
    private const char EnchantMarker = 'E';
    private const char LoreMarker = 'L';
    // Depleted veins deliberately get no marker — a spent vein is not a destination.
    private const char OreMarker = '◊';        // Iron, matching the main map
    private const char OreMithrilMark = '◆';   // Mithril
    private const char OreDivineMark = '◈';    // Divine
    private const int RevealFlashDuration = 6;

    private readonly HashSet<(int X, int Y)> _revealedCells = new();
    private int _revealFlashCounter;

    // What each minimap cell shows, composed from the samples below. One entry per cell.
    private (char Ch, Color Fg)[,]? _cache;
    private int _cacheW, _cacheH;
    private int _cacheScaleX, _cacheScaleY;

    // Per-cell sample in two layers that change at different rates. The explored layer (any
    // explored tile, best landmark) moves only when tiles are revealed or change type; the visible
    // layer (best occupant or item, any visible tile) is rebuilt every pass, but any-visible only
    // over the cells the visible set can have left or entered. Resampling the whole floor after
    // every reveal cost ~2 ms a pass on F1's million tiles, 3-6x the FOV sweep itself.
    // Ties go to the lowest x, then y: the order SampleRegion scans in.
    private struct Sample
    {
        public bool AnyExplored, AnyVisible;
        public int LandRank, LandX, LandY;
        public char LandCh; public Color LandFg;
        public int MarkRank, MarkX, MarkY;
        public char MarkCh; public Color MarkFg;
    }
    private Sample[,]? _samples;
    private bool _needFull = true;
    private int _syncedExplored, _syncedRevealGen;
    private (int X0, int Y0, int X1, int Y1) _syncedVis = (0, 0, -1, -1);
    // Runtime tile changes since the last pass; mapgen writes bypass the event but precede SetMap.
    private readonly List<(int X, int Y)> _changedTiles = new();

    public void SetMap(GameMap map)
    {
        _map.TileTypeChanged -= OnTileTypeChanged;
        _map = map;
        _map.TileTypeChanged += OnTileTypeChanged;
        _revealedCells.Clear();
        _revealFlashCounter = 0;
        _cache = null;
        _changedTiles.Clear();
    }

    public MinimapView(GameMap map, Player player)
    {
        _map = map;
        _player = player;
        _map.TileTypeChanged += OnTileTypeChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _map.TileTypeChanged -= OnTileTypeChanged;
        base.Dispose(disposing);
    }

    private void OnTileTypeChanged(int x, int y, TileType oldType, TileType newType) => _changedTiles.Add((x, y));

    // Every cell of the minimap is painted each pass; skip the framework clear so the
    // previous frame survives for diffing.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        using var prof = SAOTRPG.Systems.Diagnostics.Profiler.Begin("Draw.Minimap");
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        var vp = Viewport;
        int viewW = vp.Width, viewH = vp.Height - (ShowLegend ? 1 : 0);
        if (!Refresh(viewW, viewH)) return true;
        if (DebugMode.VerifyTiles && CountMismatches() is var bad and > 0)
            DebugLogger.LogGame("VERIFY", $"minimap: {bad} cells differ from a full resample");

        int scaleX = _cacheScaleX, scaleY = _cacheScaleY;
        int playerMx = _player.X / scaleX, playerMy = _player.Y / scaleY;

        if (_map.NewlyRevealed.Count > 0)
        {
            foreach (var (rx, ry) in _map.NewlyRevealed)
                _revealedCells.Add((rx / scaleX, ry / scaleY));
            _revealFlashCounter = RevealFlashDuration;
        }

        // Blit cache + overlay player marker + reveal flash tint.
        // Batched: every cell of the pane is written each pass, and PutCell would resolve the
        // screen origin and walk the clip region — under a lock — once per cell.
        var batch = Gfx.Begin(this);
        for (int vy = 0; vy < viewH; vy++)
        for (int vx = 0; vx < viewW; vx++)
        {
            char ch; Color fg;
            if (vx == playerMx && vy == playerMy)
            {
                ch = PlayerMarker;
                fg = Color.BrightYellow;
            }
            else
            {
                (ch, fg) = _cache![vx, vy];
                if (ch != ' ' && _revealedCells.Contains((vx, vy)))
                    fg = Color.BrightCyan;
            }
            // The minimap samples world terrain, so it takes the same world transform the map
            // does — otherwise a themed map sat beside a full-colour minimap of itself.
            batch.Put(vx, vy, ch, Gfx.Attr(MapView.OverlayColor(fg), Color.Black));
        }

        if (ShowLegend) MinimapLegendRow.Paint(batch, viewH, viewW, FooterMarks);

        if (_revealFlashCounter > 0)
        {
            _revealFlashCounter--;
            if (_revealFlashCounter == 0) _revealedCells.Clear();
        }
        return true;
    }

    // A row under the map for MinimapLegendRow: the marks a route is planned around.
    public bool ShowLegend { get; init; }

    private static readonly (char Glyph, Color Fg, string Word)[] FooterMarks =
    {
        (PlayerMarker, Color.BrightYellow, "you"), (EntityMarker, Color.Red, "foe"),
        (FriendMarker, Color.BrightGreen, "ally"), (ItemMarker, Color.BrightYellow, "item"),
        (StairsMarker, Color.BrightCyan, "exit"),
    };

    // Brings the cache up to date with the map for a viewW x viewH pane; false when there is
    // nothing to draw into. Public so TileVerifyProbe can drive it without a terminal.
    public bool Refresh(int viewW, int viewH)
    {
        if (viewW <= 0 || viewH <= 0) return false;
        int scaleX = Math.Max(1, (_map.Width + viewW - 1) / viewW);
        int scaleY = Math.Max(1, (_map.Height + viewH - 1) / viewH);
        if (_cache == null || _cacheW != viewW || _cacheH != viewH
            || _cacheScaleX != scaleX || _cacheScaleY != scaleY)
        {
            _cache = new (char, Color)[viewW, viewH];
            _samples = new Sample[viewW, viewH];
            _cacheW = viewW; _cacheH = viewH;
            _cacheScaleX = scaleX; _cacheScaleY = scaleY;
            _needFull = true;
        }

        SyncExplored(scaleX, scaleY);
        SyncVisible(scaleX, scaleY);
        Compose(scaleX, scaleY);
        _needFull = false;
        return true;
    }

    // The reference: every cell resampled from scratch, compared with the cache. ~2 ms on F1, so
    // only --verify-tiles and TileVerifyProbe call it.
    public int CountMismatches()
    {
        if (_cache == null) return 0;
        int bad = 0;
        for (int vy = 0; vy < _cacheH; vy++)
        for (int vx = 0; vx < _cacheW; vx++)
            if (_cache[vx, vy] != SampleRegion(_map, _player.Level, vx * _cacheScaleX, vy * _cacheScaleY, _cacheScaleX, _cacheScaleY))
                bad++;
        return bad;
    }

    // Exploration only ever adds tiles, so the latest sweep's reveals can be merged in. That is
    // exact only when they account for every tile explored since the last pass: a missed sweep or
    // a reveal from outside the sweep (SetExplored) forces a full resample instead.
    private void SyncExplored(int sx, int sy)
    {
        var samples = _samples!;
        int explored = _map.ExploredTileCount, gen = _map.RevealGeneration;
        int delta = explored - _syncedExplored;
        bool merge = gen == _syncedRevealGen ? delta == 0 : delta == _map.NewlyRevealed.Count;

        if (_needFull || !merge)
        {
            for (int vy = 0; vy < _cacheH; vy++)
            for (int vx = 0; vx < _cacheW; vx++)
                ResetExplored(ref samples[vx, vy]);
            for (int y = 0; y < _map.Height; y++)
            for (int x = 0; x < _map.Width; x++)
                if (_map.IsExploredUnchecked(x, y)) MergeExplored(x, y, sx, sy);
        }
        else
        {
            if (delta > 0)
                foreach (var (x, y) in _map.NewlyRevealed) MergeExplored(x, y, sx, sy);
            // A changed tile can lose its landmark, which no merge can undo: rescan its cell.
            foreach (var (tx, ty) in _changedTiles)
            {
                int vx = tx / sx, vy = ty / sy;
                ResetExplored(ref samples[vx, vy]);
                int x1 = Math.Min(_map.Width, (vx + 1) * sx), y1 = Math.Min(_map.Height, (vy + 1) * sy);
                for (int y = vy * sy; y < y1; y++)
                for (int x = vx * sx; x < x1; x++)
                    if (_map.IsExploredUnchecked(x, y)) MergeExplored(x, y, sx, sy);
            }
        }
        _changedTiles.Clear();
        _syncedExplored = explored;
        _syncedRevealGen = gen;
    }

    private static void ResetExplored(ref Sample s)
    {
        s.AnyExplored = false;
        s.LandRank = int.MaxValue;
    }

    private void MergeExplored(int x, int y, int sx, int sy)
    {
        ref var s = ref _samples![x / sx, y / sy];
        s.AnyExplored = true;
        int rank = LandmarkRank(_map.RefTile(x, y).Type, out char ch, out Color fg);
        if (!Beats(rank, x, y, s.LandRank, s.LandX, s.LandY)) return;
        s.LandRank = rank; s.LandX = x; s.LandY = y; s.LandCh = ch; s.LandFg = fg;
    }

    // Occupants and items show only where visible, so they are re-marked from the entity and item
    // lists each pass. Any-visible can only have changed inside the box visible at the last pass
    // or the box visible now, so only cells touching either are rescanned.
    private void SyncVisible(int sx, int sy)
    {
        var samples = _samples!;
        for (int vy = 0; vy < _cacheH; vy++)
        for (int vx = 0; vx < _cacheW; vx++)
            samples[vx, vy].MarkRank = int.MaxValue;

        foreach (var e in _map.Entities)
        {
            if (e.IsDefeated || !_map.InBounds(e.X, e.Y) || !_map.IsVisibleUnchecked(e.X, e.Y)) continue;
            if (!ReferenceEquals(_map.RefTile(e.X, e.Y).Occupant, e)) continue;
            int rank = OccupantRank(e, _player.Level, out char ch, out Color fg);
            Mark(e.X, e.Y, rank, ch, fg, sx, sy);
        }
        foreach (var (x, y) in _map.ItemTiles)
            if (_map.InBounds(x, y) && _map.IsVisibleUnchecked(x, y))
                Mark(x, y, 3, ItemMarker, Color.BrightYellow, sx, sy);

        var vis = _map.LastSweepBounds;
        for (int vy = 0; vy < _cacheH; vy++)
        for (int vx = 0; vx < _cacheW; vx++)
        {
            int x0 = vx * sx, y0 = vy * sy, x1 = x0 + sx - 1, y1 = y0 + sy - 1;
            if (!_needFull && !Touches(_syncedVis, x0, y0, x1, y1) && !Touches(vis, x0, y0, x1, y1)) continue;
            samples[vx, vy].AnyVisible = AnyVisibleIn(vis, x0, y0, x1, y1);
        }
        _syncedVis = vis;
    }

    private void Mark(int x, int y, int rank, char ch, Color fg, int sx, int sy)
    {
        ref var s = ref _samples![x / sx, y / sy];
        if (!Beats(rank, x, y, s.MarkRank, s.MarkX, s.MarkY)) return;
        s.MarkRank = rank; s.MarkX = x; s.MarkY = y; s.MarkCh = ch; s.MarkFg = fg;
    }

    private static bool Touches((int X0, int Y0, int X1, int Y1) box, int x0, int y0, int x1, int y1)
        => box.Y1 >= 0 && x0 <= box.X1 && x1 >= box.X0 && y0 <= box.Y1 && y1 >= box.Y0;

    // Every visible cell lies inside the last sweep's box (GameMap.LastSweepBounds).
    private bool AnyVisibleIn((int X0, int Y0, int X1, int Y1) box, int x0, int y0, int x1, int y1)
    {
        if (!Touches(box, x0, y0, x1, y1)) return false;
        int ax = Math.Max(x0, box.X0), bx = Math.Min(Math.Min(x1, box.X1), _map.Width - 1);
        int ay = Math.Max(y0, box.Y0), by = Math.Min(Math.Min(y1, box.Y1), _map.Height - 1);
        for (int y = ay; y <= by; y++)
        for (int x = ax; x <= bx; x++)
            if (_map.IsVisibleUnchecked(x, y)) return true;
        return false;
    }

    private void Compose(int sx, int sy)
    {
        var samples = _samples!;
        for (int vy = 0; vy < _cacheH; vy++)
        for (int vx = 0; vx < _cacheW; vx++)
        {
            ref var s = ref samples[vx, vy];
            if (!s.AnyExplored) { _cache![vx, vy] = (' ', Color.Black); continue; }
            if (s.MarkRank != int.MaxValue) { _cache![vx, vy] = (s.MarkCh, s.MarkFg); continue; }
            if (s.LandRank != int.MaxValue) { _cache![vx, vy] = (s.LandCh, s.LandFg); continue; }
            int cx = Math.Min(vx * sx + sx / 2, _map.Width - 1);
            int cy = Math.Min(vy * sy + sy / 2, _map.Height - 1);
            var type = _map.RefTile(cx, cy).Type;
            _cache![vx, vy] = (GetTerrainGlyph(type), s.AnyVisible ? GetTerrainColor(type) : Color.DarkGray);
        }
    }

    // Lower rank wins; on a tie, the candidate SampleRegion's x-major scan would meet first.
    private static bool Beats(int rank, int x, int y, int bestRank, int bestX, int bestY)
        => rank < bestRank || (rank == bestRank && rank != int.MaxValue && (x < bestX || (x == bestX && y < bestY)));

    // Single-pass sampler: best-ranked visible entity/item, best-ranked explored landmark,
    // any-explored/any-visible flags. Priority: entity/item > landmark > center-tile terrain.
    // The reference for the incremental cache, and the full map's sampler at any scale.
    public static (char ch, Color fg) SampleRegion(GameMap map, int playerLevel, int startX, int startY, int width, int height)
    {
        bool anyExplored = false, anyVisible = false;

        char entityCh = ' '; Color entityFg = Color.Black; int entityRank = int.MaxValue;
        (char Ch, Color Fg) landmark = (' ', Color.Black); int landmarkRank = int.MaxValue;

        int cx = Math.Min(startX + width / 2, map.Width - 1);
        int cy = Math.Min(startY + height / 2, map.Height - 1);

        int xMax = Math.Min(startX + width, map.Width);
        int yMax = Math.Min(startY + height, map.Height);

        for (int x = startX; x < xMax; x++)
        for (int y = startY; y < yMax; y++)
        {
            bool explored = map.IsExplored(x, y);
            if (!explored) continue;
            anyExplored = true;
            bool visible = map.IsVisible(x, y);
            if (visible) anyVisible = true;

            // Defer the tile fetch until we actually need it — but we always
            // need it inside the loop (entity/landmark checks), so grab it once.
            var tile = map.GetTile(x, y);

            // Best-ranked visible entity wins: boss, then monster, then ally/NPC, then item.
            if (entityRank > 0 && visible)
            {
                var occupant = tile.Occupant;
                if (occupant != null && !occupant.IsDefeated)
                {
                    int r = OccupantRank(occupant, playerLevel, out char oc, out Color of);
                    if (r < entityRank) { entityRank = r; entityCh = oc; entityFg = of; }
                }
                if (entityRank > 3 && map.HasItemsAt(x, y))
                { entityCh = ItemMarker; entityFg = Color.BrightYellow; entityRank = 3; }
            }

            // Best-ranked explored landmark wins (lowest rank); on a tie, the first the scan meets.
            if (landmarkRank > 0)
            {
                int rank = LandmarkRank(tile.Type, out char landmarkCh, out Color landmarkFg);
                if (rank < landmarkRank) { landmarkRank = rank; landmark = (landmarkCh, landmarkFg); }
            }
        }

        if (!anyExplored) return (' ', Color.Black);

        if (entityRank != int.MaxValue) return (entityCh, entityFg);
        if (landmarkRank != int.MaxValue) return landmark;

        if (!map.InBounds(cx, cy)) return (' ', Color.Black);
        var centerTile = map.GetTile(cx, cy);
        char glyph = GetTerrainGlyph(centerTile.Type);
        Color color = anyVisible ? GetTerrainColor(centerTile.Type) : Color.DarkGray;
        return (glyph, color);
    }

    // The landmark a tile type marks, and its rank (lower wins); int.MaxValue when none.
    public static int LandmarkRank(TileType type, out char landmarkCh, out Color landmarkFg)
    {
        landmarkCh = ' '; landmarkFg = Color.Black; int rank = int.MaxValue;
        switch (type)
        {
            case TileType.StairsUp:          landmarkCh = StairsMarker;   landmarkFg = Color.BrightCyan;    rank = 0; break;
            case TileType.LabyrinthEntrance: landmarkCh = 'Π';            landmarkFg = Color.BrightCyan;    rank = 0; break;
            case TileType.Door:              landmarkCh = DoorMarker;     landmarkFg = Color.BrightYellow;  rank = 1; break;
            case TileType.Shrine:
            case TileType.Fountain:          landmarkCh = ShrineMarker;   landmarkFg = Color.BrightMagenta; rank = 2; break;
            case TileType.SecretShrine:      landmarkCh = SecretMarker;   landmarkFg = Color.BrightMagenta; rank = 3; break;
            case TileType.Campfire:          landmarkCh = CampfireMarker; landmarkFg = Color.Yellow;        rank = 4; break;
            // Ore veins rank below navigation: a vein is a detour. Depleted veins get no
            // marker. Per-tier glyphs, mirroring the main map, because Amber Mono
            // collapses the colour that would otherwise be the only tier cue.
            case TileType.OreVeinIron:       landmarkCh = OreMarker;      landmarkFg = Color.Gray;          rank = 5; break;
            case TileType.OreVeinMithril:    landmarkCh = OreMithrilMark; landmarkFg = Color.BrightCyan;    rank = 6; break;
            case TileType.OreVeinDivine:     landmarkCh = OreDivineMark;  landmarkFg = Color.BrightMagenta; rank = 7; break;
            case TileType.Chest:             landmarkCh = ChestMarker;    landmarkFg = Color.BrightYellow;  rank = 8; break;
            case TileType.Anvil:             landmarkCh = AnvilMarker;    landmarkFg = Color.BrightYellow;  rank = 9; break;
            case TileType.EnchantShrine:     landmarkCh = EnchantMarker;  landmarkFg = Color.BrightYellow;  rank = 10; break;
            case TileType.LoreStone:
            case TileType.Journal:           landmarkCh = LoreMarker;     landmarkFg = Color.BrightCyan;    rank = 11; break;
        }
        return rank;
    }

    // The marker an occupant paints, and its rank (lower wins); int.MaxValue when it paints none.
    public static int OccupantRank(Entity occ, int playerLevel, out char entityCh, out Color entityFg)
    {
        entityCh = ' '; entityFg = Color.Black;
        if (occ is Boss) { entityCh = BossMarker; entityFg = Color.BrightRed; return 0; }
        if (occ is Monster m)
        {
            Color threatColor = (m.Level - playerLevel) switch
            {
                >= 3 => Color.BrightRed,
                >= 0 => Color.Red,
                _    => Color.DarkGray,
            };
            entityCh = EntityMarker; entityFg = threatColor; return 1;
        }
        if (occ is Ally) { entityCh = FriendMarker; entityFg = Color.BrightGreen; return 2; }
        if (occ is NPC) { entityCh = FriendMarker; entityFg = Color.BrightCyan; return 2; }
        return int.MaxValue;
    }

    // PATH-D-PORT: this widget's visual contract is ONE GLYPH PER MEANING, and nothing here may
    // rely on colour to separate two things — an accessibility theme is allowed to collapse hue
    // (Amber Mono maps the world to luma). A renderer swap must keep every mark distinguishable
    // in monochrome. `Legend` below is the canonical list, checked in BOTH directions by
    // ContentProbe: every legend glyph reaches a paint site, every painted glyph is explained,
    // and no glyph carries two meanings.
    // THE MINIMAP'S OWN LEGEND, declared once so the Player Guide can render it rather than
    // transcribe it. Every previous hand-written glyph list in this codebase had drifted by the
    // time anyone checked — the status tray's key was wrong on three letters — so this one is
    // generated from the start.
    //
    // Only the glyphs a player needs to READ a route are listed: the terrain that stops or slows
    // them, and the landmarks worth walking to.
    public static readonly (char Glyph, string Meaning)[] Legend =
    {
        ('@', "You"),
        ('*', "A monster"),
        ('n', "An ally or NPC"),
        ('!', "A dropped item"),
        ('Ω', "A boss — the floor boss, or a field elite"),
        ('>', "Stairs up — the way off this floor"),
        ('\u03a0', "The labyrinth archway"),
        ('+', "A door"),
        ('S', "A shrine or fountain"),
        ('?', "A secret shrine"),
        ('&', "A campfire — rest and cook here"),
        ('$', "An unopened chest"),
        ('A', "An anvil — repair and smith here"),
        ('E', "An enchanting shrine"),
        ('L', "A lore stone or journal"),
        ('\u25ca', "An iron ore vein"),
        ('\u25c6', "A mithril ore vein"),
        ('\u25c8', "A divine ore vein, the richest tier"),
        ('\u2248', "Shallow water — crossable from Swimming L1"),
        ('\u2261', "Deep water — needs Swimming L25"),
        ('\u00a7', "Lava — it will hurt"),
        ('%', "Corrupted ground — it burns you every step"),
        ('\u223d', "Bogwater — wading it poisons you"),
        ('~', "Mud — it slows you"),
        ('^', "Mountain"),
        ('T', "Trees"),
        ('"', "Bushes"),
        ('o', "Loose rock"),
        ('P', "A pillar"),
        ('.', "Open ground"),
        ('\u25aa', "Wall"),
    };

    // What a legend glyph means, up to its first clause ("Shallow water"); null when unlisted.
    public static string? Meaning(char glyph)
    {
        foreach (var (g, meaning) in Legend)
            if (g == glyph)
            {
                int cut = meaning.IndexOf(" — ", StringComparison.Ordinal);
                return cut < 0 ? meaning : meaning[..cut];
            }
        return null;
    }

    private static char GetTerrainGlyph(TileType type) => type switch
    {
        TileType.Wall => '▪', TileType.Mountain => '^',
        // Shallow and deep must differ: WaterDeep is a Swimming L25 wall, and a player reading
        // the minimap to plan a route needs to know which water they can actually cross. They
        // shared one glyph and leaned on colour — which Amber Mono collapses to luma.
        TileType.Water => '≈',
        TileType.WaterDeep => '≡',
        TileType.Tree or TileType.TreePine => 'T', TileType.Bush => '"',
        TileType.StairsUp => StairsMarker, TileType.Door => DoorMarker,
        TileType.Path or TileType.Floor => '.', TileType.Rock => 'o',
        TileType.Campfire => CampfireMarker, TileType.Fountain => ShrineMarker,
        TileType.Shrine => ShrineMarker, TileType.Pillar => 'P', TileType.Lava => '§',
        // HAZARDS THE ROUTE-PLANNING SURFACE USED TO LEAVE BLANK. Lava was marked and these three
        // were not, so a player plotting a path across the minimap saw clear ground where
        // corrupted ground would burn them every step and bogwater would poison them. Same marks
        // as the main map, so the two surfaces read alike.
        TileType.DangerZone => '%', TileType.BogWater => '∽', TileType.Mud => '~',
        TileType.TrapSpike or TileType.TrapTeleport => '.',
        TileType.Grass or TileType.GrassTall or TileType.GrassSparse or TileType.Flowers => ' ',
        _ => ' ',
    };

    private static Color GetTerrainColor(TileType type) => type switch
    {
        TileType.Wall => Color.White, TileType.Mountain => Color.Gray,
        TileType.Water or TileType.WaterDeep => Color.BrightBlue,
        TileType.Tree or TileType.TreePine or TileType.Bush => Color.Green,
        TileType.StairsUp => Color.BrightCyan,
        TileType.Door or TileType.Path => Color.BrightYellow, TileType.Floor => Color.Gray,
        TileType.Rock => Color.DarkGray, TileType.Flowers => Color.BrightMagenta,
        TileType.Campfire => Color.Yellow, TileType.Fountain => Color.BrightCyan,
        TileType.Shrine => Color.BrightMagenta, TileType.Pillar => Color.White,
        TileType.Lava => Color.BrightRed,
        _ => Color.DarkGray,
    };
}
