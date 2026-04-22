using Terminal.Gui;
using SAOTRPG.Map;

namespace SAOTRPG.UI.Helpers;

// Terrain and tile visual helpers: connected walls, water/shoreline styling,
// aggro range check, room theming, ambient sound text.
public static partial class MapEffects
{
    private const int AggroIndicatorRange = 6, AmbientSoundRadius = 3, LargeWaterBodyThreshold = 10;

    // Delegates to GameMap's cached wall-glyph lookup (GameMap owns cache + invalidation).
    // Avoids per-frame neighbor scans once a wall has been drawn.
    public static char GetConnectedWallGlyph(GameMap map, int x, int y) =>
        map.GetWallGlyph(x, y);

    public static (char Glyph, Color Color)? GetTransitionBorder(GameMap map, int x, int y, TileType type)
    {
        // `type` is redundant with map.Tiles[x,y].Type; kept for caller stability.
        // Cached code already bakes in the land-type check.
        _ = type;
        return map.GetTransitionCode(x, y) switch
        {
            GameMap.TransitionWater => (',', Color.Blue),
            GameMap.TransitionLava  => (',', Color.Red),
            _                       => ((char, Color)?)null,
        };
    }

    public static (char Glyph, Color Fg, Color Bg)? GetShorelineVisual(GameMap map, int x, int y, TileType type)
    {
        if (type is not (TileType.Water or TileType.WaterDeep)) return null;
        bool landN = HasLand(map, x, y - 1), landS = HasLand(map, x, y + 1);
        bool landE = HasLand(map, x + 1, y), landW = HasLand(map, x - 1, y);
        int landCount = (landN ? 1 : 0) + (landS ? 1 : 0) + (landE ? 1 : 0) + (landW ? 1 : 0);
        if (landCount == 0) return null;
        char glyph = landN && !landS ? '▄' : landS && !landN ? '▀' : landCount >= 2 ? '░' : '▒';
        return (glyph, Color.BrightCyan, Color.Black);
    }

    private static bool HasLand(GameMap map, int x, int y) =>
        !map.InBounds(x, y) || map.GetTile(x, y).Type is not (TileType.Water or TileType.WaterDeep);

    // Water glyph cycles per turn for a ripple effect. Phase offset by position
    // so adjacent tiles don't all change in sync.
    private static readonly char[] WaterGlyphs = { '~', '-', '~', '-' };
    public static int AnimationTurn { get; set; } // set from TurnManager each turn

    public static char GetWaterFlowGlyph(int x, int y)
    {
        int phase = (AnimationTurn + x * 3 + y * 7) % WaterGlyphs.Length;
        return WaterGlyphs[phase];
    }

    public static (char Glyph, Color Color)? GetWaterLilyPad(int x, int y)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        return hash % 10 == 0 ? ('●', Color.Green) : null;
    }

    public static bool ShouldShowAggroIndicator(GameMap map, int monsterX, int monsterY, int playerX, int playerY) =>
        Math.Abs(monsterX - playerX) + Math.Abs(monsterY - playerY) <= AggroIndicatorRange
        && map.IsVisible(monsterX, monsterY);

    public static (Color? Tint, char? Glyph) GetRoomTheme(int x, int y, TileType type)
    {
        if (type != TileType.Floor) return (null, null);
        int regionHash = (x / 8 * 48271 + y / 8 * 96137) & 0x7FFFFFFF;
        return (regionHash % 7) switch
        {
            0 => (Color.Gray, null),
            1 => (Color.BrightYellow, null),
            2 => (Color.BrightCyan, '\u00B7'),
            3 => (Color.BrightGreen, null),
            4 => (Color.Gray, ':'),
            5 => (Color.BrightYellow, '\u2219'),
            _ => (null, null),
        };
    }

    public static string? GetAmbientSoundText(GameMap map, int playerX, int playerY)
    {
        if (Random.Shared.Next(100) != 0) return null;
        bool nearWater = false, nearCampfire = false, nearWind = false;
        int waterCount = 0;

        for (int dx = -AmbientSoundRadius; dx <= AmbientSoundRadius; dx++)
        for (int dy = -AmbientSoundRadius; dy <= AmbientSoundRadius; dy++)
        {
            int nx = playerX + dx, ny = playerY + dy;
            if (!map.InBounds(nx, ny)) continue;
            var t = map.GetTile(nx, ny).Type;
            if (t is TileType.Water or TileType.WaterDeep) { nearWater = true; waterCount++; }
            if (t == TileType.Campfire) nearCampfire = true;
            if (t is TileType.Grass or TileType.GrassTall or TileType.Mountain) nearWind = true;
        }

        if (nearWater && waterCount >= LargeWaterBodyThreshold && Random.Shared.Next(2) == 0)
            return null;

        if (nearCampfire)
        {
            string[] lines = {
                "...the campfire crackles softly.", "...embers drift upward in the still air.",
                "...warmth radiates from the nearby fire.",
            };
            return lines[Random.Shared.Next(lines.Length)];
        }
        if (nearWater)
        {
            string[] lines = {
                "...water laps gently against the shore.", "...you hear a faint trickling sound.",
                "...a soft current murmurs nearby.",
            };
            return lines[Random.Shared.Next(lines.Length)];
        }
        if (nearWind)
        {
            string[] lines = {
                "...a cool breeze rustles through the area.", "...wind whispers across the floor.",
                "...the air shifts quietly around you.",
            };
            return lines[Random.Shared.Next(lines.Length)];
        }
        return null;
    }
}
