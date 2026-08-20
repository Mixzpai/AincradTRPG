using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Top-right scaled floor overview; each cell = region of tiles.
// Perf: single-pass SampleRegion, buffer cached and only resampled on move/reveal/resize.
public class MinimapView : View
{
    private GameMap _map;
    private readonly Player _player;

    private const char PlayerMarker = '@', EntityMarker = '*', ItemMarker = '!';
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

    // Cached pixel buffer (glyph + foreground). One entry per minimap cell.
    private (char Ch, Color Fg)[,]? _cache;
    private int _cacheW, _cacheH;
    private int _cacheScaleX, _cacheScaleY;
    private int _cachePlayerMx = -1, _cachePlayerMy = -1;
    private bool _cacheDirty = true;

    public void SetMap(GameMap map)
    {
        _map = map;
        _revealedCells.Clear();
        _revealFlashCounter = 0;
        _cache = null;
        _cacheDirty = true;
    }

    public MinimapView(GameMap map, Player player) { _map = map; _player = player; }

    // Every cell of the minimap is painted each pass; skip the framework clear so the
    // previous frame survives for diffing.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        var vp = Viewport;
        int viewW = vp.Width, viewH = vp.Height;
        if (viewW <= 0 || viewH <= 0) return true;

        int scaleX = Math.Max(1, (_map.Width + viewW - 1) / viewW);
        int scaleY = Math.Max(1, (_map.Height + viewH - 1) / viewH);
        int playerMx = _player.X / scaleX, playerMy = _player.Y / scaleY;

        // Rebuild cache on first draw / resize / player cell change; else reuse last buffer if no reveals.
        bool geomChanged = _cache == null || _cacheW != viewW || _cacheH != viewH
                           || _cacheScaleX != scaleX || _cacheScaleY != scaleY;
        bool playerCellChanged = playerMx != _cachePlayerMx || playerMy != _cachePlayerMy;

        if (geomChanged)
        {
            _cache = new (char, Color)[viewW, viewH];
            _cacheW = viewW; _cacheH = viewH;
            _cacheScaleX = scaleX; _cacheScaleY = scaleY;
            _cacheDirty = true;
        }

        if (_map.NewlyRevealed.Count > 0)
        {
            foreach (var (rx, ry) in _map.NewlyRevealed)
                _revealedCells.Add((rx / scaleX, ry / scaleY));
            _revealFlashCounter = RevealFlashDuration;
            _cacheDirty = true;
        }

        if (_cacheDirty || playerCellChanged)
        {
            ResampleAll(viewW, viewH, scaleX, scaleY);
            _cachePlayerMx = playerMx; _cachePlayerMy = playerMy;
            _cacheDirty = false;
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

        if (_revealFlashCounter > 0)
        {
            _revealFlashCounter--;
            if (_revealFlashCounter == 0) _revealedCells.Clear();
        }
        return true;
    }

    private void ResampleAll(int viewW, int viewH, int scaleX, int scaleY)
    {
        for (int vy = 0; vy < viewH; vy++)
        for (int vx = 0; vx < viewW; vx++)
        {
            int mapX = vx * scaleX, mapY = vy * scaleY;
            _cache![vx, vy] = SampleRegion(mapX, mapY, scaleX, scaleY);
        }
    }

    // Single-pass sampler: first visible entity/item (x-then-y), first explored landmark,
    // any-explored/any-visible flags. Priority: entity/item > landmark > center-tile terrain.
    private (char ch, Color fg) SampleRegion(int startX, int startY, int width, int height)
    {
        bool anyExplored = false, anyVisible = false;

        char entityCh = ' '; Color entityFg = Color.Black; bool haveEntity = false;
        char landmarkCh = ' '; Color landmarkFg = Color.Black; bool haveLandmark = false;

        int cx = Math.Min(startX + width / 2, _map.Width - 1);
        int cy = Math.Min(startY + height / 2, _map.Height - 1);

        int xMax = Math.Min(startX + width, _map.Width);
        int yMax = Math.Min(startY + height, _map.Height);

        for (int x = startX; x < xMax; x++)
        for (int y = startY; y < yMax; y++)
        {
            bool explored = _map.IsExplored(x, y);
            if (!explored) continue;
            anyExplored = true;
            bool visible = _map.IsVisible(x, y);
            if (visible) anyVisible = true;

            // Defer the tile fetch until we actually need it — but we always
            // need it inside the loop (entity/landmark checks), so grab it once.
            var tile = _map.GetTile(x, y);

            // First-match-wins entity/item lookup on visible tiles.
            if (!haveEntity && visible)
            {
                var occ = tile.Occupant;
                if (occ != null && !occ.IsDefeated)
                {
                    if (occ is Ally) { entityCh = FriendMarker; entityFg = Color.BrightGreen; haveEntity = true; }
                    else if (occ is Boss) { entityCh = BossMarker; entityFg = Color.BrightRed; haveEntity = true; }
                    else if (occ is Monster m)
                    {
                        Color threatColor = (m.Level - _player.Level) switch
                        {
                            >= 3 => Color.BrightRed,
                            >= 0 => Color.Red,
                            _    => Color.DarkGray,
                        };
                        entityCh = EntityMarker; entityFg = threatColor; haveEntity = true;
                    }
                    else if (occ is NPC) { entityCh = FriendMarker; entityFg = Color.BrightCyan; haveEntity = true; }
                }
                if (!haveEntity && _map.HasItemsAt(x, y))
                { entityCh = ItemMarker; entityFg = Color.BrightYellow; haveEntity = true; }
            }

            // First-match-wins landmark lookup on any explored tile.
            if (!haveLandmark)
            {
                switch (tile.Type)
                {
                    case TileType.StairsUp:          landmarkCh = StairsMarker;   landmarkFg = Color.BrightCyan;    haveLandmark = true; break;
                    case TileType.LabyrinthEntrance: landmarkCh = 'Π';            landmarkFg = Color.BrightCyan;    haveLandmark = true; break;
                    case TileType.Door:              landmarkCh = DoorMarker;     landmarkFg = Color.BrightYellow;  haveLandmark = true; break;
                    case TileType.Shrine:
                    case TileType.Fountain:          landmarkCh = ShrineMarker;   landmarkFg = Color.BrightMagenta; haveLandmark = true; break;
                    case TileType.SecretShrine:      landmarkCh = SecretMarker;            landmarkFg = Color.BrightMagenta; haveLandmark = true; break;
                    case TileType.Campfire:          landmarkCh = CampfireMarker; landmarkFg = Color.Yellow;        haveLandmark = true; break;
                    // Ore veins are worth crossing a floor for and could not be spotted at a
                    // glance. Listed last so a shrine or door sharing the sampled block still wins
                    // the cell — those are navigation, an ore vein is a detour. Depleted veins get
                    // no marker: a spent vein is not a destination.
                    // Per-tier glyphs, mirroring the main map. FB-736 gave all three ONE marker
                    // separated only by colour — the same mistake FB-738 fixed for water, and
                    // Amber Mono collapses exactly that colour. A player deciding whether a vein
                    // is worth crossing a floor for has to be able to read the tier.
                    case TileType.OreVeinIron:       landmarkCh = OreMarker;      landmarkFg = Color.Gray;          haveLandmark = true; break;
                    case TileType.OreVeinMithril:    landmarkCh = OreMithrilMark; landmarkFg = Color.BrightCyan;    haveLandmark = true; break;
                    case TileType.OreVeinDivine:     landmarkCh = OreDivineMark;  landmarkFg = Color.BrightMagenta; haveLandmark = true; break;
                    case TileType.Chest:             landmarkCh = ChestMarker;    landmarkFg = Color.BrightYellow;  haveLandmark = true; break;
                    case TileType.Anvil:             landmarkCh = AnvilMarker;    landmarkFg = Color.BrightYellow;  haveLandmark = true; break;
                    case TileType.EnchantShrine:     landmarkCh = EnchantMarker;  landmarkFg = Color.BrightYellow;  haveLandmark = true; break;
                    case TileType.LoreStone:
                    case TileType.Journal:           landmarkCh = LoreMarker;     landmarkFg = Color.BrightCyan;    haveLandmark = true; break;
                }
            }
        }

        if (!anyExplored) return (' ', Color.Black);

        if (haveEntity) return (entityCh, entityFg);
        if (haveLandmark) return (landmarkCh, landmarkFg);

        if (!_map.InBounds(cx, cy)) return (' ', Color.Black);
        var centerTile = _map.GetTile(cx, cy);
        char glyph = GetTerrainGlyph(centerTile.Type);
        Color color = anyVisible ? GetTerrainColor(centerTile.Type) : Color.DarkGray;
        return (glyph, color);
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
