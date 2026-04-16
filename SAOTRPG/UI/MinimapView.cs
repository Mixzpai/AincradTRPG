using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Scaled-down overview of the dungeon floor shown in the top-right panel.
// Each minimap cell represents a region of map tiles.
//
// Performance notes:
//   * SampleRegion collapses what used to be three sequential full passes
//     over the region (explored/visible check, then entities/items, then
//     terrain landmarks) into a single pass that gathers every signal.
//   * The per-cell output (glyph + color + revealed flag) is cached and
//     only resampled when the player moves, new tiles are revealed, or
//     the viewport resizes. Frames with no NewlyRevealed entries reuse
//     last frame's pixel buffer.
public class MinimapView : View
{
    private GameMap _map;
    private readonly Player _player;

    private const char PlayerMarker = '@', EntityMarker = '*', ItemMarker = '!';
    private const char DoorMarker = '+', StairsMarker = '>', ShrineMarker = '*', CampfireMarker = '*';
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

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        int viewW = vp.Width, viewH = vp.Height;
        if (viewW <= 0 || viewH <= 0) return true;

        int scaleX = Math.Max(1, (_map.Width + viewW - 1) / viewW);
        int scaleY = Math.Max(1, (_map.Height + viewH - 1) / viewH);
        int playerMx = _player.X / scaleX, playerMy = _player.Y / scaleY;

        // Resize / rebuild the cache on first draw, resolution change, or player
        // crossing a cell boundary. Otherwise, if nothing was newly revealed we
        // can skip the resample entirely and reuse last frame's buffer.
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
            Driver!.SetAttribute(Gfx.Attr(fg, Color.Black));
            Move(vx, vy);
            Driver!.AddRune(new System.Text.Rune(ch));
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

    // Single-pass region sampler. Walks the sub-rectangle once, collecting:
    //   * the FIRST visible entity/item (identical first-match semantics to
    //     the original 3-pass implementation — iteration order x-then-y),
    //   * the FIRST explored landmark tile (stairs/door/shrine/…),
    //   * whether the region has any explored / any visible tile.
    // Priority at the end: entity/item > landmark > center-tile terrain.
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
                    if (occ is Ally) { entityCh = EntityMarker; entityFg = Color.BrightGreen; haveEntity = true; }
                    else if (occ is Boss) { entityCh = '◆'; entityFg = Color.BrightRed; haveEntity = true; }
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
                    else if (occ is NPC) { entityCh = EntityMarker; entityFg = Color.BrightCyan; haveEntity = true; }
                }
                if (!haveEntity && tile.HasItems)
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
                    case TileType.Campfire:          landmarkCh = CampfireMarker; landmarkFg = Color.Yellow;        haveLandmark = true; break;
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

    private static char GetTerrainGlyph(TileType type) => type switch
    {
        TileType.Wall => '▪', TileType.Mountain => '^',
        TileType.Water or TileType.WaterDeep => '≈',
        TileType.Tree or TileType.TreePine => 'T', TileType.Bush => '*',
        TileType.StairsUp => '◊', TileType.Door => '+',
        TileType.Path or TileType.Floor => '.', TileType.Rock => 'o',
        TileType.Campfire => '&', TileType.Fountain => 'F',
        TileType.Shrine => 'S', TileType.Pillar => 'P', TileType.Lava => '≈',
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
