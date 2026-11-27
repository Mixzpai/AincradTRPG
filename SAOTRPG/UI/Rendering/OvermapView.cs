using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.UI.Helpers;
using static SAOTRPG.UI.Helpers.CddaColors;

namespace SAOTRPG.UI.Rendering;

// The sidebar minimap: one cell per 24x24-tile overmap tile (OMT), centred on the player's, which
// is highlighted. An unseen OMT is a grey '#'; an OMT in sight holding three or more monsters is
// a pack, its letter and colour by size; the floor's exit, once its tile is explored, sits on red,
// or, off the window, a red '*' marks the first edge cell in reading order on the line toward it.
// The picture depends on one cell per OMT and the 16-colour palette's highlight rules (Hilite,
// RedBackground); colour alone separates '.' field/sand/snow and 'F' forest/swamp.
public class OvermapView : View
{
    public const int OmtSize = 24;
    private const int HordeVisibilitySize = 3;   // fewest monsters drawn as a pack
    private const string RoadLines = "┼│─└││┌├─┘─┴┐┤┬┼";   // by neighbour mask N=1 E=2 S=4 W=8

    // What an OMT reads as: the overmap terrain nearest to its tiles.
    public enum Kind : byte { Void, Stairs, Labyrinth, Building, Halls, Road, Lake, Lava, River, Forest, Swamp, Sand, Snow, Rock, Field }

    private GameMap _map;
    private readonly Player _player;
    // Per OMT, row-major. Kind is a pure function of the OMT's tiles, redone when one changes
    // type; seen is "any tile explored", and exploration only adds, so reveals merge in.
    private Kind[] _kind = Array.Empty<Kind>();
    private bool[] _seen = Array.Empty<bool>();
    private int[] _hordes = Array.Empty<int>();
    private int _ow, _oh, _target = -1;
    private bool _built;
    private int _syncedExplored, _syncedRevealGen;
    // Runtime tile changes since the last pass; mapgen writes bypass the event but precede SetMap.
    private readonly List<(int X, int Y)> _changed = new();

    public OvermapView(GameMap map, Player player)
    {
        _map = map;
        _player = player;
        CanFocus = false;
        _map.TileTypeChanged += OnTileTypeChanged;
    }

    public void SetMap(GameMap map)
    {
        _map.TileTypeChanged -= OnTileTypeChanged;
        _map = map;
        _map.TileTypeChanged += OnTileTypeChanged;
        _built = false;
        _changed.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _map.TileTypeChanged -= OnTileTypeChanged;
        base.Dispose(disposing);
    }

    private void OnTileTypeChanged(int x, int y, TileType oldType, TileType newType) => _changed.Add((x, y));

    // Every cell is painted each pass; skip the framework clear so the last frame survives for diffing.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        using var prof = Profiler.Begin("Draw.Minimap");
        // A modal above this view has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;
        int w = Viewport.Width, h = Viewport.Height;
        if (w <= 0 || h <= 0) return true;
        Refresh();
        if (DebugMode.VerifyTiles && CountMismatches() is var bad and > 0)
            DebugLogger.LogGame("VERIFY", $"overmap: {bad} OMTs differ from a full reclassify");

        // The window spans -(w/2) .. w-(w/2)-1 OMTs across the player's, and the same down.
        int pox = _player.X / OmtSize, poy = _player.Y / OmtSize, cx = w / 2, cy = h / 2;
        CountHordes();
        int target = MissionTarget();
        int tdx = target >= 0 ? target % _ow - pox : 0, tdy = target >= 0 ? target / _ow - poy : 0;
        bool inWindow = target >= 0 && tdx >= -cx && tdx < w - cx && tdy >= -cy && tdy < h - cy;
        var (mx, my) = target >= 0 && !inWindow ? EdgeMarker(w, h, tdx, tdy) : (-1, -1);
        var batch = Gfx.Begin(this);
        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int ox = pox + vx - cx, oy = poy + vy - cy;
            int i = (uint)ox < (uint)_ow && (uint)oy < (uint)_oh ? oy * _ow + ox : -1;
            // The edge marker is drawn over whatever the cell holds, seen or not.
            var (ch, fg) = vx == mx && vy == my ? ('*', Red) : Cell(ox, oy, i);
            Color? bg = null;
            if (i >= 0 && i == target) (fg, bg) = RedBackground(fg);
            if (vx == cx && vy == cy) (fg, bg) = Hilite(fg, bg);
            batch.Put(vx, vy, ch, Attr(fg, bg));
        }
        return true;
    }

    // The minimap takes the same world transform as the map it summarises. A null background is
    // the terminal's black.
    private static Attribute Attr(Color fg, Color? bg) =>
        Gfx.Attr(MapView.OverlayColor(fg), bg is { } b ? MapView.OverlayColor(b) : Color.Black);

    // The palette's blue-background variant of a colour. A colour that already has a background
    // has none and takes white on blue, and blue on blue is drawn black on blue.
    public static (Color Fg, Color? Bg) Hilite(Color fg, Color? bg) =>
        bg is not null ? (White, Blue) : fg == Blue ? (Black, Blue) : (fg, Blue);

    // The palette's red-background variant: red on red is drawn black, and black on red dark grey.
    public static (Color Fg, Color? Bg) RedBackground(Color fg) =>
        fg == Red ? (Black, Red) : fg == Black ? (DarkGray, Red) : (fg, Red);

    // A pack's letter and colour by its size, from the fewest drawn (three) up.
    public static (char Ch, Color Fg) Pack(int size) => size switch
    {
        < 5 => ('z', LightGreen),
        < 13 => ('Z', LightGreen),
        < 27 => ('z', Green),
        < 47 => ('Z', Green),
        < 73 => ('z', Yellow),
        < 106 => ('Z', Yellow),
        < 147 => ('z', LightRed),
        < 195 => ('Z', LightRed),
        < 251 => ('z', Red),
        _ => ('Z', Red),
    };

    // Brings the OMT layers up to date with the map. Public so the verification probes can drive it.
    public void Refresh()
    {
        if (!_built)
        {
            _ow = (_map.Width + OmtSize - 1) / OmtSize;
            _oh = (_map.Height + OmtSize - 1) / OmtSize;
            _kind = new Kind[_ow * _oh];
            _seen = new bool[_ow * _oh];
            _hordes = new int[_ow * _oh];
            for (int oy = 0; oy < _oh; oy++)
            for (int ox = 0; ox < _ow; ox++)
                _kind[oy * _ow + ox] = Classify(_map, ox, oy);
            RescanSeen();
            _target = -1;
            _changed.Clear();
            _built = true;
            return;
        }
        SyncSeen();
        foreach (var (x, y) in _changed)
            _kind[y / OmtSize * _ow + x / OmtSize] = Classify(_map, x / OmtSize, y / OmtSize);
        if (_changed.Count > 0) _target = -1;
        _changed.Clear();
    }

    // The reference: every OMT reclassified and rescanned from scratch, compared with the layers.
    // A full floor's worth of tile reads, so only --verify-tiles and the verification probes call it.
    public int CountMismatches()
    {
        int bad = 0;
        for (int oy = 0; oy < _oh; oy++)
        for (int ox = 0; ox < _ow; ox++)
        {
            int i = oy * _ow + ox;
            if (_kind[i] != Classify(_map, ox, oy) || _seen[i] != AnyExplored(ox, oy)) bad++;
        }
        return bad;
    }

    // The latest sweep's reveals merge in only when they account for every tile explored since
    // the last pass; a missed sweep or a reveal from outside one (SetExplored) forces a rescan.
    private void SyncSeen()
    {
        int explored = _map.ExploredTileCount, gen = _map.RevealGeneration;
        int delta = explored - _syncedExplored;
        bool merge = gen == _syncedRevealGen ? delta == 0 : delta == _map.NewlyRevealed.Count;
        if (!merge) RescanSeen();
        else if (delta > 0)
            foreach (var (x, y) in _map.NewlyRevealed) _seen[y / OmtSize * _ow + x / OmtSize] = true;
        _syncedExplored = explored;
        _syncedRevealGen = gen;
    }

    private void RescanSeen()
    {
        Array.Clear(_seen);
        for (int y = 0; y < _map.Height; y++)
        for (int x = 0; x < _map.Width; x++)
            if (_map.IsExploredUnchecked(x, y)) _seen[y / OmtSize * _ow + x / OmtSize] = true;
        _syncedExplored = _map.ExploredTileCount;
        _syncedRevealGen = _map.RevealGeneration;
    }

    // Whether any tile of the OMT is explored, or, given a type, any explored tile of that type.
    private bool AnyExplored(int ox, int oy, TileType? type = null)
    {
        int x1 = Math.Min(_map.Width, (ox + 1) * OmtSize), y1 = Math.Min(_map.Height, (oy + 1) * OmtSize);
        for (int y = oy * OmtSize; y < y1; y++)
        for (int x = ox * OmtSize; x < x1; x++)
            if (_map.IsExploredUnchecked(x, y) && (type is not { } t || _map.RefTile(x, y).Type == t)) return true;
        return false;
    }

    // Every live monster per OMT; a pack is drawn only where the OMT is in sight (InSight).
    private void CountHordes()
    {
        Array.Clear(_hordes);
        foreach (var e in _map.Entities)
            if (e is Monster && !e.IsDefeated && _map.InBounds(e.X, e.Y))
                _hordes[e.Y / OmtSize * _ow + e.X / OmtSize]++;
    }

    // Whether any tile of the OMT is in view this turn.
    private bool InSight(int ox, int oy)
    {
        int x1 = Math.Min(_map.Width, (ox + 1) * OmtSize), y1 = Math.Min(_map.Height, (oy + 1) * OmtSize);
        for (int y = oy * OmtSize; y < y1; y++)
        for (int x = ox * OmtSize; x < x1; x++)
            if (_map.IsVisibleUnchecked(x, y)) return true;
        return false;
    }

    // The mission target is the floor's way up — the stairs, else the labyrinth archway — once
    // the player has explored its tile. -1 until then.
    private int MissionTarget()
    {
        if (_target >= 0) return _target;
        foreach (var (kind, tile) in Exits)
            for (int i = 0; i < _kind.Length; i++)
                if (_kind[i] == kind && _seen[i] && AnyExplored(i % _ow, i / _ow, tile))
                    return _target = i;
        return -1;
    }

    private static readonly (Kind, TileType)[] Exits =
        { (Kind.Stairs, TileType.StairsUp), (Kind.Labyrinth, TileType.LabyrinthEntrance) };

    // Where a target (dx, dy) OMTs from the centre, off a w x h window, is marked: the first edge
    // cell in reading order that the line from the centre to it passes through, or (-1, -1). The
    // line steps one OMT at a time along its longer axis, moving across once its error passes zero.
    // Public and pure for the verification probes.
    public static (int X, int Y) EdgeMarker(int w, int h, int dx, int dy)
    {
        int left = -(w / 2), top = -(h / 2), right = w + left - 1, bottom = h + top - 1;
        int sx = Math.Sign(dx), sy = Math.Sign(dy), ax = Math.Abs(dx) * 2, ay = Math.Abs(dy) * 2;
        int x = 0, y = 0, t = 0, bx = 0, by = 0;
        bool found = false;

        void Consider(int px, int py)
        {
            if (px < left || px > right || py < top || py > bottom) return;
            if (px != left && px != right && py != top && py != bottom) return;
            if (found && (py > by || (py == by && px >= bx))) return;
            (bx, by, found) = (px, py, true);
        }

        if (ax == ay)
            while (x != dx) { x += sx; y += sy; Consider(x, y); }
        else if (ax > ay)
            while (x != dx)
            {
                if (t > 0) { y += sy; t -= ax; }
                x += sx;
                t += ay;
                Consider(x, y);
            }
        else
            while (y != dy)
            {
                if (t > 0) { x += sx; t -= ay; }
                y += sy;
                t += ax;
                Consider(x, y);
            }
        return found ? (bx - left, by - top) : (-1, -1);
    }

    private (char Ch, Color Fg) Cell(int ox, int oy, int i)
    {
        if (i < 0) return (' ', Black);
        Kind k = _kind[i];
        if (k == Kind.Void) return (' ', Black);
        if (!_seen[i]) return ('#', DarkGray);   // unseen is always a grey '#'
        if (_hordes[i] >= HordeVisibilitySize && InSight(ox, oy)) return Pack(_hordes[i]);
        if (k == Kind.Road)
        {
            int m = (IsRoad(ox, oy - 1) ? 1 : 0) | (IsRoad(ox + 1, oy) ? 2 : 0)
                  | (IsRoad(ox, oy + 1) ? 4 : 0) | (IsRoad(ox - 1, oy) ? 8 : 0);
            return (RoadLines[m], DarkGray);
        }
        var look = LookOf(k);
        return (look.Ch, look.Fg);
    }

    private bool IsRoad(int ox, int oy) =>
        (uint)ox < (uint)_ow && (uint)oy < (uint)_oh && _kind[oy * _ow + ox] == Kind.Road;

    // Each kind's symbol and colour, and what it means.
    private static (char Ch, Color Fg, string Colour, string Meaning) LookOf(Kind k) => k switch
    {
        Kind.Stairs    => ('>', Yellow, "yellow", "Stairs up, the way off this floor"),
        Kind.Labyrinth => ('Π', Yellow, "yellow", "The labyrinth archway"),
        Kind.Building  => ('^', LightGreen, "light green", "Buildings"),
        Kind.Halls     => ('L', LightBlue, "light blue", "Labyrinth halls"),
        Kind.Road      => ('│', DarkGray, "grey", "A road; the line follows where it runs"),
        Kind.Lake      => ('#', Blue, "blue", "A lake"),
        Kind.Lava      => ('~', Red, "red", "Lava"),
        Kind.River     => ('R', Blue, "blue", "A river or stream"),
        Kind.Forest    => ('F', Green, "green", "Forest"),
        Kind.Swamp     => ('F', Cyan, "cyan", "Swamp"),
        Kind.Sand      => ('.', Yellow, "yellow", "Sand"),
        Kind.Snow      => ('.', White, "white", "Snow and ice"),
        Kind.Rock      => ('%', DarkGray, "grey", "Rock and mountain"),
        Kind.Field     => ('.', Brown, "brown", "Open field"),
        _              => (' ', Black, "", ""),
    };

    // The terrain legend, declared once so the Player Guide renders it ({{OVERMAP_LEGEND}}).
    public static IEnumerable<(char Glyph, string Colour, string Meaning)> Legend =>
        Enum.GetValues<Kind>().Where(k => k != Kind.Void).Select(k => { var l = LookOf(k); return (l.Ch, l.Colour, l.Meaning); });

    // The OMT's terrain from the tiles inside the floor's disc, most telling first. Thresholds are
    // shares of those tiles, read off generated ground.
    public static Kind Classify(GameMap map, int ox, int oy)
    {
        int x1 = Math.Min(map.Width, (ox + 1) * OmtSize), y1 = Math.Min(map.Height, (oy + 1) * OmtSize);
        int n = 0, water = 0, lava = 0, trees = 0, swamp = 0, sand = 0, snow = 0, rock = 0, wall = 0, built = 0, path = 0;
        bool stairs = false, arch = false;
        for (int y = oy * OmtSize; y < y1; y++)
        for (int x = ox * OmtSize; x < x1; x++)
        {
            if (!map.IsInsideCircle(x, y)) continue;
            n++;
            switch (map.RefTile(x, y).Type)
            {
                case TileType.StairsUp: stairs = true; break;
                case TileType.LabyrinthEntrance: arch = true; break;
                case TileType.Water: case TileType.WaterDeep: water++; break;
                case TileType.BogWater: case TileType.Mud: case TileType.Reeds: swamp++; break;
                case TileType.Lava: lava++; break;
                case TileType.Tree: case TileType.TreePine: case TileType.Bush: trees++; break;
                case TileType.Sand: case TileType.DuneSand: sand++; break;
                case TileType.Snow: case TileType.Ice: case TileType.CrackedIce: snow++; break;
                case TileType.Mountain: case TileType.Rock: case TileType.Basalt: rock++; break;
                case TileType.Wall: case TileType.CrackedWall: wall++; break;
                case TileType.Floor: case TileType.Door: case TileType.Pillar: built++; break;
                case TileType.Path: path++; break;
            }
        }
        if (n == 0) return Kind.Void;
        if (stairs) return Kind.Stairs;
        if (arch) return Kind.Labyrinth;
        if (!map.HasSky) return wall * 8 >= n * 7 ? Kind.Rock : Kind.Halls;
        if (wall > 0 && built > 0 && (wall + built) * 4 >= n) return Kind.Building;
        if (path >= OmtSize) return Kind.Road;            // a road's width across the OMT
        if (water * 2 >= n) return Kind.Lake;
        if (lava * 16 >= n) return Kind.Lava;
        if (water * 8 >= n) return Kind.River;
        // Generated trees are sparse, so a wooded eighth reads as forest.
        if (trees * 8 >= n || swamp * 4 >= n) return swamp > trees ? Kind.Swamp : Kind.Forest;
        if (sand * 2 >= n) return Kind.Sand;
        if (snow * 2 >= n) return Kind.Snow;
        if ((rock + wall) * 2 >= n) return Kind.Rock;
        return Kind.Field;
    }
}
