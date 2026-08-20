using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.Systems;

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
        // BOTH USED TO DRAW ',', which is the Grass and Snow glyph — so the marker that exists to
        // say "there is water here" and the one that says "there is LAVA here" were ordinary
        // ground to anyone whose theme collapses colour, and identical to each other. They sit on
        // walkable land either way, so this is not a walkability lie; it is worse in one respect,
        // because the lava edge is the one a player most needs to read at a glance.
        return map.GetTransitionCode(x, y) switch
        {
            GameMap.TransitionWater => (WaterEdge, Color.Blue),
            GameMap.TransitionLava  => (LavaEdge, Color.Red),
            _                       => ((char, Color)?)null,
        };
    }

    // The land-side edge markers. Named so the switch above and the check below cannot drift —
    // a hand-copied array is what let an earlier sabotage change a drawn glyph unnoticed.
    private const char WaterEdge = ';', LavaEdge = '^';
    public static readonly char[] TransitionGlyphs = { WaterEdge, LavaEdge };

    public static (char Glyph, Color Fg, Color Bg)? GetShorelineVisual(GameMap map, int x, int y, TileType type)
    {
        if (type is not (TileType.Water or TileType.WaterDeep)) return null;
        bool landN = HasLand(map, x, y - 1), landS = HasLand(map, x, y + 1);
        bool landE = HasLand(map, x + 1, y), landW = HasLand(map, x - 1, y);
        int landCount = (landN ? 1 : 0) + (landS ? 1 : 0) + (landE ? 1 : 0) + (landW ? 1 : 0);
        if (landCount == 0) return null;
        // A shoreline sits ON a water cell, and deep water is a Swimming L25 wall. It used to
        // draw '░' and '▒', which are Ice (walkable) and CrackedWall — so a shore read as ground
        // you could stand on. These four are the shore's own, and giving E/W their own halves
        // also makes the edge directional on all four sides instead of two.
        char glyph = landN && !landS ? ShoreNorth
                   : landS && !landN ? ShoreSouth
                   : landE && !landW ? ShoreEast
                   : landW && !landE ? ShoreWest
                   : ShoreCorner;
        return (glyph, Color.BrightCyan, Color.Black);
    }

    private static bool HasLand(GameMap map, int x, int y) =>
        !map.InBounds(x, y) || map.GetTile(x, y).Type is not (TileType.Water or TileType.WaterDeep);

    // Water glyph cycles slowly for ambience. Phase offset by position so adjacent tiles don't
    // all change in sync. Step matches the TileAnimator water overlay so the two layers agree.
    // Pinned by --freeze-anim like every other wall-clock visual.
    // THREE DISJOINT FAMILIES. ResolveWater overrides the static tile glyph for BOTH water
    // depths, and it used to hand them the same set — so shallow water (free from Swimming L1)
    // and deep water (L25, a wall until then) were identical on the primary surface, separated
    // only by hue, which Amber Mono collapses. Lava drew '~' and '-' as well, so a lethal tile
    // read as crossable water. Tildes are shallow, heavy tildes are deep, dashes are lava.
    private static readonly char[] WaterGlyphs = { '~', '∼', '~', '∼' };
    private static readonly char[] DeepWaterGlyphs = { '≈', '≋', '≈', '≋' };
    private const int WaterStepMs = 800;

    public static char GetWaterFlowGlyph(int x, int y, bool deep = false)
    {
        var set = deep ? DeepWaterGlyphs : WaterGlyphs;
        int phase = ((int)(FrameClock.AmbientMs / WaterStepMs) + x * 3 + y * 7) % set.Length;
        return set[phase];
    }

    // Exposed so a check can assert the three families stay disjoint rather than trusting the
    // literals above to be read by eye.
    // The shoreline's glyphs, exposed for the same reason the water sets are: this is a
    // RENDER-TIME OVERRIDE, so sampling TileDefinitions cannot see it. NAMED CONSTANTS, and the
    // switch above reads these — a hand-copied duplicate of the literals is what let a sabotage
    // change the drawn glyph while the checked array stayed put.
    private const char ShoreNorth = '▄', ShoreSouth = '▀', ShoreEast = '▐',
                       ShoreWest = '▌', ShoreCorner = '▞';
    public static readonly char[] ShorelineGlyphs =
        { ShoreNorth, ShoreSouth, ShoreEast, ShoreWest, ShoreCorner };

    public static IReadOnlyList<char> ShallowWaterGlyphSet => WaterGlyphs;
    public static IReadOnlyList<char> DeepWaterGlyphSet => DeepWaterGlyphs;

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
