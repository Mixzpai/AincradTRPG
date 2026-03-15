using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;

namespace SAOTRPG.UI;

/// <summary>
/// Scaled-down overview of the dungeon floor shown in the top-right panel.
///
/// Each minimap cell represents a region of map tiles. The view auto-scales
/// to fit the full map into whatever viewport size it's given.
///
/// Rendering priority per cell:
///   1. Player marker '@' (bright yellow) — always visible
///   2. Visible entities: monsters '*' (red), NPCs '*' (cyan)
///   3. Items on ground '!' (yellow)
///   4. Terrain glyph — full color if visible, dimmed if explored-only
///   5. Unexplored — blank/black
/// </summary>
public class MinimapView : View
{
    // ── State ────────────────────────────────────────────────────────
    private GameMap _map;
    private readonly Player _player;

    // ── Entity marker symbols ────────────────────────────────────────
    private const char PlayerMarker  = '@';
    private const char EntityMarker  = '*';
    private const char ItemMarker    = '!';

    // ── Fog reveal animation — newly revealed minimap cells pulse white ──
    private readonly HashSet<(int X, int Y)> _revealedCells = new();
    private int _revealFlashCounter;
    private const int RevealFlashDuration = 6;

    /// <summary>Hot-swap the map reference (used on floor change).</summary>
    public void SetMap(GameMap map)
    {
        _map = map;
        _revealedCells.Clear();
        _revealFlashCounter = 0;
    }

    public MinimapView(GameMap map, Player player)
    {
        _map = map;
        _player = player;
    }

    // ══════════════════════════════════════════════════════════════════
    //  RENDERING
    // ══════════════════════════════════════════════════════════════════

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        int viewW = vp.Width;
        int viewH = vp.Height;

        if (viewW <= 0 || viewH <= 0) return true;

        // Scale: each minimap cell covers a region of map tiles
        int scaleX = Math.Max(1, (_map.Width + viewW - 1) / viewW);
        int scaleY = Math.Max(1, (_map.Height + viewH - 1) / viewH);

        int playerMx = _player.X / scaleX;
        int playerMy = _player.Y / scaleY;

        for (int vy = 0; vy < viewH; vy++)
        {
            for (int vx = 0; vx < viewW; vx++)
            {
                int mapX = vx * scaleX;
                int mapY = vy * scaleY;

                char ch;
                Color fg;

                // Player marker — blinks between BrightYellow and White
                if (vx == playerMx && vy == playerMy)
                {
                    ch = PlayerMarker;
                    fg = (TileDefinitions.AnimTick % 6 < 3) ? Color.BrightYellow : Color.White;
                }
                else
                {
                    (ch, fg) = SampleRegion(mapX, mapY, scaleX, scaleY);

                    // Fog reveal animation — newly explored regions flash white
                    if (ch != ' ' && _revealedCells.Contains((vx, vy)))
                        fg = (_revealFlashCounter % 4 < 2) ? Color.White : Color.BrightCyan;
                }

                Driver.SetAttribute(new Terminal.Gui.Attribute(fg, Color.Black));
                Move(vx, vy);
                Driver.AddRune(new System.Text.Rune(ch));
            }
        }

        // Track newly revealed minimap cells by checking map.NewlyRevealed
        if (_map.NewlyRevealed.Count > 0)
        {
            foreach (var (rx, ry) in _map.NewlyRevealed)
            {
                int mvx = rx / scaleX;
                int mvy = ry / scaleY;
                _revealedCells.Add((mvx, mvy));
            }
            _revealFlashCounter = RevealFlashDuration;
        }

        // Tick down reveal flash
        if (_revealFlashCounter > 0)
        {
            _revealFlashCounter--;
            if (_revealFlashCounter == 0)
                _revealedCells.Clear();
        }

        return true;
    }

    // ══════════════════════════════════════════════════════════════════
    //  REGION SAMPLING
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Samples a rectangular region of map tiles to determine what the
    /// minimap cell should display. Checks exploration state, entities,
    /// items, and terrain in priority order.
    /// </summary>
    private (char ch, Color fg) SampleRegion(int startX, int startY, int width, int height)
    {
        bool anyExplored = false;
        bool anyVisible  = false;

        // Center tile coordinates for terrain sampling
        int cx = Math.Min(startX + width / 2, _map.Width - 1);
        int cy = Math.Min(startY + height / 2, _map.Height - 1);

        // ── Pass 1: Check exploration/visibility state ──────────────
        for (int x = startX; x < startX + width && x < _map.Width; x++)
        for (int y = startY; y < startY + height && y < _map.Height; y++)
        {
            if (_map.IsExplored(x, y)) anyExplored = true;
            if (_map.IsVisible(x, y))  anyVisible  = true;
        }

        // Unexplored regions are blank
        if (!anyExplored)
            return (' ', Color.Black);

        // ── Pass 2: Check for visible entities/items ────────────────
        if (anyVisible)
        {
            for (int x = startX; x < startX + width && x < _map.Width; x++)
            for (int y = startY; y < startY + height && y < _map.Height; y++)
            {
                if (!_map.IsVisible(x, y)) continue;
                var tile = _map.GetTile(x, y);

                // Entity markers — bosses pulse, monsters blink threat
                if (tile.Occupant != null && !tile.Occupant.IsDefeated)
                {
                    if (tile.Occupant is Boss)
                    {
                        // Boss: pulsing bright red '◆'
                        Color bossColor = (TileDefinitions.AnimTick % 4 < 2)
                            ? Color.BrightRed : Color.Red;
                        return ('◆', bossColor);
                    }
                    if (tile.Occupant is Monster m)
                    {
                        // Threat-colored dot: bright red if higher level, red if even, dark red if weaker
                        Color threatColor = (m.Level - _player.Level) switch
                        {
                            >= 3 => (TileDefinitions.AnimTick % 4 < 2)
                                ? Color.BrightRed : Color.BrightYellow,  // Dangerous — pulsing warning
                            >= 0 => Color.Red,
                            _    => Color.DarkGray,  // Weaker — dim
                        };
                        return (EntityMarker, threatColor);
                    }
                    if (tile.Occupant is NPC)
                        return (EntityMarker, Color.BrightCyan);
                }

                // Item markers
                if (tile.HasItems)
                    return (ItemMarker, Color.BrightYellow);
            }
        }

        // ── Pass 3: Terrain from center tile ────────────────────────
        if (!_map.InBounds(cx, cy))
            return (' ', Color.Black);

        var centerTile = _map.GetTile(cx, cy);
        char glyph = GetTerrainGlyph(centerTile.Type);
        Color color = anyVisible
            ? GetTerrainColor(centerTile.Type)
            : Color.DarkGray;   // Dim explored-but-not-visible areas

        return (glyph, color);
    }

    // ══════════════════════════════════════════════════════════════════
    //  TERRAIN LOOKUP TABLES
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Maps tile types to simplified minimap glyphs (Unicode).</summary>
    private static char GetTerrainGlyph(TileType type) => type switch
    {
        TileType.Wall                                                       => '▪',
        TileType.Mountain                                                   => '^',
        TileType.Water or TileType.WaterDeep                                => '≈',
        TileType.Tree or TileType.TreePine                                  => 'T',
        TileType.Bush                                                       => '*',
        TileType.StairsUp                                                   => '◊',
        TileType.Door                                                       => '+',
        TileType.Path or TileType.Floor                                     => '.',
        TileType.Rock                                                       => 'o',
        TileType.Campfire                                                   => '&',
        TileType.Fountain                                                   => 'F',
        TileType.Shrine                                                     => 'S',
        TileType.Pillar                                                     => 'P',
        TileType.Lava                                                       => '≈',
        TileType.TrapSpike or TileType.TrapTeleport                         => '.',
        TileType.Grass or TileType.GrassTall or TileType.GrassSparse
            or TileType.Flowers                                             => ' ',
        _                                                                   => ' ',
    };

    /// <summary>Maps tile types to minimap foreground colors.</summary>
    private static Color GetTerrainColor(TileType type) => type switch
    {
        TileType.Wall                                                       => Color.White,
        TileType.Mountain                                                   => Color.Gray,
        TileType.Water or TileType.WaterDeep                                => Color.BrightBlue,
        TileType.Tree or TileType.TreePine or TileType.Bush                 => Color.Green,
        TileType.StairsUp                                                   => Color.BrightCyan,
        TileType.Door or TileType.Path                                      => Color.BrightYellow,
        TileType.Floor                                                      => Color.Gray,
        TileType.Rock                                                       => Color.DarkGray,
        TileType.Flowers                                                    => Color.BrightMagenta,
        TileType.Campfire                                                   => Color.Yellow,
        TileType.Fountain                                                   => Color.BrightCyan,
        TileType.Shrine                                                     => Color.BrightMagenta,
        TileType.Pillar                                                     => Color.White,
        TileType.Lava                                                       => Color.BrightRed,
        _                                                                   => Color.DarkGray,
    };
}
