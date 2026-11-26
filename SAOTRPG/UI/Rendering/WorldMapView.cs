using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Rendering;

// The full map (WorldMapDialog): the explored floor at a chosen zoom, a cursor the player pans,
// a side panel of every point of interest found so far, and a line naming what is under the
// cursor. Cells come from MinimapView.SampleRegion and names from MinimapView.Legend, so the
// two maps always draw and name a tile the same way.
//
// Lives inside a dialog, so it carries no beneath-a-dialog paint guard (rule 23).
public class WorldMapView : View
{
    // Tiles per cell. A terminal cell is twice as tall as it is wide, so every level after the
    // first covers twice as many rows as columns and the floor keeps its shape; the first level
    // is the main map's own one tile per cell.
    public static readonly (int X, int Y)[] Levels =
        { (1, 1), (1, 2), (2, 4), (3, 6), (4, 8), (6, 12), (8, 16), (12, 24), (16, 32), (24, 48), (32, 64) };

    public const int PanelWidth = 30;
    // Points-of-interest kinds in panel order: the ways off the floor, then who is about, then
    // places to use, then the rest. A kind missing here sorts last.
    private const string KindOrder = ">ΠΩn&S?$AEL◊◆◈+*";
    private const string TerrainKey = "≈≡§%∽~^T\"oP.▪";
    private const int EdgeMargin = 2;   // cells kept between the cursor and the map area's edge
    private const int FastPan = 8;      // cells per keystroke with Shift held

    public readonly record struct Poi(int X, int Y, char Glyph, Color Fg, string Name, bool Cyclable);

    private readonly GameMap _map;
    private readonly Player _player;
    private readonly int _floor;
    private readonly List<Poi> _pois;
    private readonly List<int> _cycle = new();   // indices into _pois that Tab visits, nearest first

    public int Level { get; private set; }
    public int CursorX { get; private set; }
    public int CursorY { get; private set; }
    public int SelectedPoi { get; private set; } = -1;   // index into Pois; -1 when none
    public IReadOnlyList<Poi> Pois => _pois;
    private int _ox, _oy;                                 // world tile at the map area's top-left
    private bool _placed;

    // Composed map cells, reused while nothing that decides them has moved.
    private (char Ch, Color Fg)[,]? _cells;
    private (int Level, int Ox, int Oy, int W, int H) _cellsKey = (-1, 0, 0, 0, 0);

    public WorldMapView(GameMap map, Player player, int floor)
    {
        _map = map;
        _player = player;
        _floor = floor;
        CanFocus = true;
        CursorX = player.X;
        CursorY = player.Y;
        _pois = CollectPois(map, player);
        for (int i = 0; i < _pois.Count; i++)
            if (_pois[i].Cyclable) _cycle.Add(i);
    }

    // Every cell is painted each pass.
    protected override bool OnClearingViewport() => true;

    // ── Points of interest ─────────────────────────────────────────────
    // Landmarks on explored ground, and the occupants in sight now (the same rule the minimap
    // uses). Doors and monsters are counted but not cycled to: neither is somewhere to go.
    private static List<Poi> CollectPois(GameMap map, Player player)
    {
        var list = new List<Poi>();
        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
        {
            if (!map.IsExploredUnchecked(x, y)) continue;
            if (MinimapView.LandmarkRank(map.RefTile(x, y).Type, out char ch, out Color fg) == int.MaxValue) continue;
            list.Add(new Poi(x, y, ch, fg, NameOf(ch), ch != '+'));
        }
        foreach (var e in map.Entities)
        {
            if (ReferenceEquals(e, player) || e.IsDefeated || !map.InBounds(e.X, e.Y)) continue;
            if (!map.IsVisibleUnchecked(e.X, e.Y)) continue;
            int rank = MinimapView.OccupantRank(e, player.Level, out char ch, out Color fg);
            if (rank == int.MaxValue) continue;
            bool monster = e is Monster && e is not Boss;
            string name = e.Name.Length > 0 ? e.Name : NameOf(ch);
            list.Add(new Poi(e.X, e.Y, ch, fg, name, !monster));
        }
        list.Sort((a, b) => Distance(player.X, player.Y, a.X, a.Y).CompareTo(Distance(player.X, player.Y, b.X, b.Y)));
        return list;
    }

    // A legend meaning as a list entry: "An unopened chest" -> "Unopened chest".
    public static string NameOf(char glyph)
    {
        string m = MinimapView.Meaning(glyph) ?? glyph.ToString();
        int comma = m.IndexOf(',');
        if (comma > 0) m = m[..comma];
        foreach (var article in new[] { "A ", "An ", "The " })
            if (m.StartsWith(article, StringComparison.Ordinal)) { m = m[article.Length..]; break; }
        return m.Length > 0 ? char.ToUpperInvariant(m[0]) + m[1..] : m;
    }

    // Moves, since every step may be diagonal.
    public static int Distance(int x0, int y0, int x1, int y1) => Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));

    // Eight-way compass letters, as the HUD's danger compass writes them.
    public static string Bearing(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return "here";
        int ax = Math.Abs(dx), ay = Math.Abs(dy);
        string ns = dy < 0 ? "N" : "S", ew = dx < 0 ? "W" : "E";
        if (ax > 2 * ay) return ew;
        if (ay > 2 * ax) return ns;
        return ns + ew;
    }

    // ── Geometry ───────────────────────────────────────────────────────
    private int MapW => Viewport.Width >= PanelWidth + 20 ? Viewport.Width - PanelWidth - 1 : Viewport.Width;
    private int MapH => Math.Max(0, Viewport.Height - 1);
    private bool HasPanel => Viewport.Width >= PanelWidth + 20;

    private static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    // The closest level at which the explored region (or the whole floor, when nothing is
    // explored) fits the map area.
    private int OpeningLevel(int w, int h)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (int y = 0; y < _map.Height; y++)
        for (int x = 0; x < _map.Width; x++)
        {
            if (!_map.IsExploredUnchecked(x, y)) continue;
            if (x < x0) x0 = x; if (x > x1) x1 = x;
            if (y < y0) y0 = y; if (y > y1) y1 = y;
        }
        int spanX = x1 < 0 ? _map.Width : x1 - x0 + 1, spanY = x1 < 0 ? _map.Height : y1 - y0 + 1;
        for (int i = 1; i < Levels.Length; i++)
            if (Levels[i].X * (w - 2 * EdgeMargin) >= spanX && Levels[i].Y * (h - 2 * EdgeMargin) >= spanY) return i;
        return Levels.Length - 1;
    }

    // The coarsest level offered at the current size.
    public int MaxLevel => FitLevel(Math.Max(1, MapW), Math.Max(1, MapH));

    // The coarsest level worth offering: the first that shows the whole floor.
    private int FitLevel(int w, int h)
    {
        for (int i = 0; i < Levels.Length; i++)
            if (Levels[i].X * w >= _map.Width && Levels[i].Y * h >= _map.Height) return i;
        return Levels.Length - 1;
    }

    private void CenterOnCursor()
    {
        var (lx, ly) = Levels[Level];
        _ox = CursorX - (MapW / 2) * lx;
        _oy = CursorY - (MapH / 2) * ly;
    }

    // Scrolls just enough to keep the cursor EdgeMargin cells inside the map area.
    private void KeepCursorInView()
    {
        var (lx, ly) = Levels[Level];
        int m = Math.Min(EdgeMargin, Math.Max(0, Math.Min(MapW, MapH) / 2 - 1));
        int vx = FloorDiv(CursorX - _ox, lx), vy = FloorDiv(CursorY - _oy, ly);
        if (vx < m) _ox -= (m - vx) * lx;
        else if (vx > MapW - 1 - m) _ox += (vx - (MapW - 1 - m)) * lx;
        if (vy < m) _oy -= (m - vy) * ly;
        else if (vy > MapH - 1 - m) _oy += (vy - (MapH - 1 - m)) * ly;
    }

    // ── Input ──────────────────────────────────────────────────────────
    protected override bool OnKeyDown(Key key)
    {
        switch (Keybinds.Resolve(InputContext.WorldMap, key))
        {
            case GameAction.WorldMapZoomIn:    Zoom(-1); return Handled(key);
            case GameAction.WorldMapZoomOut:   Zoom(+1); return Handled(key);
            case GameAction.WorldMapCenter:    MoveCursorTo(_player.X, _player.Y, center: true); SelectedPoi = -1; return Handled(key);
            case GameAction.WorldMapNextPoint: CyclePoi(+1); return Handled(key);
            case GameAction.WorldMapPrevPoint: CyclePoi(-1); return Handled(key);
        }

        // The key that opened the map closes it, as the Bestiary's does.
        GameAction onMap = Keybinds.Resolve(InputContext.Map, key);
        if (onMap == GameAction.OpenWorldMap)
        {
            key.Handled = true;
            AppHost.App.RequestStop();
            return true;
        }

        // Panning takes the map's own movement keys, Shift for a long stride as it is sprint there.
        GameAction dir = onMap;
        if (!Keybinds.IsDirection(dir)) dir = Keybinds.ResolveDirectionLoose(InputContext.Map, key);
        if (Keybinds.IsDirection(dir))
        {
            var (dx, dy) = Keybinds.Delta(dir);
            int stride = key.IsShift ? FastPan : 1;
            Pan(dx * stride, dy * stride);
            return Handled(key);
        }
        return base.OnKeyDown(key);
    }

    private bool Handled(Key key)
    {
        key.Handled = true;
        SetNeedsDraw();
        return true;
    }

    // Moves the cursor by whole cells at the current zoom, clamped to the floor.
    public void Pan(int dCells, int dRows)
    {
        var (lx, ly) = Levels[Level];
        MoveCursorTo(CursorX + dCells * lx, CursorY + dRows * ly, center: false);
        SelectedPoi = -1;
    }

    private void MoveCursorTo(int x, int y, bool center)
    {
        CursorX = Math.Clamp(x, 0, _map.Width - 1);
        CursorY = Math.Clamp(y, 0, _map.Height - 1);
        if (center) CenterOnCursor(); else KeepCursorInView();
    }

    public void Zoom(int step)
    {
        int fit = FitLevel(Math.Max(1, MapW), Math.Max(1, MapH));
        int next = Math.Clamp(Level + step, 0, Math.Max(fit, 0));
        if (next == Level) return;
        Level = next;
        CenterOnCursor();
    }

    public void CyclePoi(int step)
    {
        if (_cycle.Count == 0) return;
        int at = SelectedPoi < 0 ? -1 : _cycle.IndexOf(SelectedPoi);
        at = at < 0 ? (step > 0 ? 0 : _cycle.Count - 1) : ((at + step) % _cycle.Count + _cycle.Count) % _cycle.Count;
        SelectedPoi = _cycle[at];
        MoveCursorTo(_pois[SelectedPoi].X, _pois[SelectedPoi].Y, center: false);
    }

    // ── Painting ───────────────────────────────────────────────────────
    protected override bool OnDrawingContent(DrawContext? context)
    {
        int w = MapW, h = MapH;
        if (w <= 0 || h <= 0) return true;
        if (!_placed)
        {
            Level = Math.Min(OpeningLevel(w, h), FitLevel(w, h));
            CenterOnCursor();
            _placed = true;
        }

        var theme = ColorSchemes.Theme;
        var batch = Gfx.Begin(this);
        ComposeCells(w, h);
        var (lx, ly) = Levels[Level];
        int pvx = FloorDiv(_player.X - _ox, lx), pvy = FloorDiv(_player.Y - _oy, ly);
        int cvx = FloorDiv(CursorX - _ox, lx), cvy = FloorDiv(CursorY - _oy, ly);
        var cursorAttr = Gfx.Attr(theme.Base, theme.Accent);

        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            var (ch, fg) = _cells![vx, vy];
            if (vx == pvx && vy == pvy) { ch = MinimapView.PlayerMarker; fg = Color.BrightYellow; }
            var attr = vx == cvx && vy == cvy ? cursorAttr : Gfx.Attr(MapView.OverlayColor(fg), theme.Base);
            batch.Put(vx, vy, ch, attr);
        }

        if (HasPanel)
        {
            var rule = Gfx.Attr(theme.Muted, theme.Base);
            for (int y = 0; y < h; y++) batch.Put(w, y, '│', rule);
            PaintPanel(batch, w + 2, PanelWidth - 1, h);
        }
        PaintInfoLine(batch, h, Viewport.Width);
        return true;
    }

    // Each cell is the minimap's sample of its block. The sampler marks unexplored blocks with a
    // black blank; an explored block with nothing to show comes back blank in a terrain colour,
    // and is drawn as open ground so the explored region reads at every zoom.
    private void ComposeCells(int w, int h)
    {
        var key = (Level, _ox, _oy, w, h);
        if (_cells != null && _cellsKey == key) return;
        _cells = new (char, Color)[w, h];
        _cellsKey = key;
        var (lx, ly) = Levels[Level];
        for (int vy = 0; vy < h; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int x0 = _ox + vx * lx, y0 = _oy + vy * ly;
            if (x0 + lx <= 0 || y0 + ly <= 0 || x0 >= _map.Width || y0 >= _map.Height) { _cells[vx, vy] = (' ', Color.Black); continue; }
            int cx0 = Math.Max(0, x0), cy0 = Math.Max(0, y0);
            var (ch, fg) = MinimapView.SampleRegion(_map, _player.Level, cx0, cy0,
                Math.Min(x0 + lx, _map.Width) - cx0, Math.Min(y0 + ly, _map.Height) - cy0);
            if (ch == ' ' && fg != Color.Black) ch = '.';
            _cells[vx, vy] = (ch, fg);
        }
    }

    private void PaintPanel(Gfx.Batch b, int x, int width, int h)
    {
        var t = ColorSchemes.Theme;
        Attribute emph = Gfx.Attr(t.Emph, t.Base, TextStyle.Bold), body = Gfx.Attr(t.Body, t.Base),
                  muted = Gfx.Attr(t.Muted, t.Base), accent = Gfx.Attr(t.Accent, t.Base);
        int y = 0;
        void Blank(int row) { for (int i = 0; i < width; i++) b.Put(x + i, row, ' ', body); }
        int Text(int col, int row, string s, Attribute a)
        {
            int n = 0;
            for (; n < s.Length && col + n < width; n++) b.Put(x + col + n, row, s[n], a);
            return col + n;
        }
        void Row(Action<int> paint) { if (y >= h) return; Blank(y); paint(y); y++; }
        void Section(string caption)
        {
            Row(r =>
            {
                int end = Text(0, r, caption.ToUpperInvariant(), emph);
                for (int i = end + 1; i < width; i++) b.Put(x + i, r, ScreenHeader.Hairline, muted);
            });
        }

        Row(r => Text(0, r, $"FLOOR {_floor}", emph));
        Row(r => Text(0, r, TextHelpers.Truncate(SAOTRPG.Systems.World.BiomeSystem.DisplayName, width), body));
        Row(r => { int c = Text(0, r, "Explored ", muted); Text(c, r, $"{_map.GetExplorationPercent()}%", body); });
        Row(r => { int c = Text(0, r, "You at ", muted); Text(c, r, $"{_player.X}, {_player.Y}", body); });
        Row(_ => { });

        // One row per kind, the way off the floor first, with its count and the nearest one.
        Section("Points of interest");
        var groups = _pois.GroupBy(p => p.Glyph)
            .OrderBy(g => KindOrder.IndexOf(g.Key) is var i and >= 0 ? i : KindOrder.Length)
            .ToList();
        char selected = SelectedPoi >= 0 ? _pois[SelectedPoi].Glyph : '\0';
        var terrain = MinimapView.Legend.Where(e => TerrainKey.Contains(e.Glyph)).ToList();
        int terrainRows = 2 + (terrain.Count + 1) / 2;          // blank, caption, two per row
        int avail = h - y;
        bool withTerrain = avail >= Math.Max(1, groups.Count) + terrainRows;
        int groupRows = withTerrain ? Math.Max(1, groups.Count) : avail;
        if (groups.Count == 0) Row(r => Text(0, r, "Nothing found yet", muted));
        for (int gi = 0; gi < groups.Count && gi < groupRows; gi++)
        {
            // A list the panel cannot fit says so (rule 220).
            if (gi == groupRows - 1 && groups.Count > groupRows)
            {
                int rest = groups.Count - gi;
                Row(r => Text(0, r, $"+{rest} more kinds", muted));
                break;
            }
            var g = groups[gi];
            var near = g.First();   // _pois is nearest-first
            int n = g.Count();
            string name = g.Key == '*' ? "Monsters in sight" : (n == 1 ? near.Name : NameOf(g.Key));
            string where = $"{Bearing(near.X - _player.X, near.Y - _player.Y)} {Distance(_player.X, _player.Y, near.X, near.Y)}";
            string count = n > 1 ? $" ×{n}" : "";
            Row(r =>
            {
                b.Put(x, r, g.Key, Gfx.Attr(MapView.OverlayColor(near.Fg), t.Base));
                int room = width - 2 - where.Length - 1 - count.Length;
                int c = Text(2, r, TextHelpers.Truncate(name, Math.Max(1, room)), g.Key == selected ? accent : body);
                Text(c, r, count, muted);
                Text(width - where.Length, r, where, muted);
            });
        }

        // The terrain half of the legend, only when all of it fits; the Player Guide has it whole.
        if (withTerrain)
        {
            Row(_ => { });
            Section("Terrain");
            int colW = width / 2;
            for (int i = 0; i < terrain.Count; i += 2)
            {
                int i0 = i;
                Row(r =>
                {
                    for (int k = 0; k < 2 && i0 + k < terrain.Count; k++)
                    {
                        char tg = terrain[i0 + k].Glyph;
                        b.Put(x + k * colW, r, tg, muted);
                        Text(k * colW + 2, r, TextHelpers.Truncate(NameOf(tg), colW - 3), muted);
                    }
                });
            }
        }
        while (y < h) { Blank(y); y++; }
    }

    // Bottom row: where the cursor is, what is there, how far, and the zoom.
    private void PaintInfoLine(Gfx.Batch b, int row, int width)
    {
        var t = ColorSchemes.Theme;
        Attribute body = Gfx.Attr(t.Body, t.Base), muted = Gfx.Attr(t.Muted, t.Base), accent = Gfx.Attr(t.Accent, t.Base);
        for (int i = 0; i < width; i++) b.Put(i, row, ' ', body);

        var (lx, ly) = Levels[Level];
        string zoom = lx == 1 && ly == 1 ? "1 tile per cell" : $"{lx}×{ly} tiles per cell";
        int col = 0;
        void Put(string s, Attribute a) { for (int i = 0; i < s.Length && col < width - zoom.Length - 2; i++) b.Put(col++, row, s[i], a); }

        Put($"{CursorX}, {CursorY}  ", muted);
        Put(DescribeCursor(), SelectedPoi >= 0 ? accent : body);
        int d = Distance(_player.X, _player.Y, CursorX, CursorY);
        if (d > 0) Put($"  ·  {d} {Bearing(CursorX - _player.X, CursorY - _player.Y)} of you", muted);
        for (int i = 0; i < zoom.Length; i++) b.Put(width - zoom.Length + i, row, zoom[i], muted);
    }

    // The cursor's own tile: the point selected, else its mark, else its ground.
    public string DescribeCursor()
    {
        if (SelectedPoi >= 0 && _pois[SelectedPoi].X == CursorX && _pois[SelectedPoi].Y == CursorY)
            return _pois[SelectedPoi].Name;
        if (CursorX == _player.X && CursorY == _player.Y) return "You";
        if (!_map.IsExplored(CursorX, CursorY)) return "Unexplored";
        var (ch, _) = MinimapView.SampleRegion(_map, _player.Level, CursorX, CursorY, 1, 1);
        string what = ch == ' ' ? "Open ground" : NameOf(ch);
        return _map.IsVisible(CursorX, CursorY) ? what + ", in sight" : what;
    }
}
