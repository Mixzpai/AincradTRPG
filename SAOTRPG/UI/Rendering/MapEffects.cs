using SAOTRPG.Map;
using SAOTRPG.UI.Effects;

namespace SAOTRPG.UI.Rendering;

// Terrain and tile visual helpers: connected walls, water/shoreline styling,
// aggro range check, ambient sound text.
public static class MapEffects
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

    // A LILY PAD IS NOT A ROCK. This drew '●', which is Rock's own glyph — so one cell in ten of
    // every pond read as a boulder sitting in the water, and under a theme that collapses hue
    // there was nothing left to tell them apart. '○' is authored by no tile type, sits in the
    // same Geometric Shapes block as the '●'/'◆'/'◇' the table already uses (so it measures the
    // same one column), and reads as a flat pad rather than a solid mass.
    private const char LilyPad = '○';

    // Shallow water only, and its own declared family: this is a RENDER-TIME OVERRIDE, so nothing
    // sampling TileDefinitions can see it. Named, and the check reads the name.
    public static readonly char[] WaterOverlayGlyphs = { LilyPad };

    public static (char Glyph, Color Color)? GetWaterLilyPad(int x, int y)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        return hash % 10 == 0 ? (LilyPad, Color.Green) : null;
    }

    // ── The room tint, applied as a TRANSFORM of the cell's own colour ───────────────────────
    // The tint used to be a straight substitution gated on the foreground being exactly
    // Color.Gray. Swept across all five eras and 11,520 positions, the Floor palette produces
    // that colour ZERO times - every entry is an authored `new Color(...)` - so the tint half of
    // per-room theming never once reached the screen, and the only half that did was a glyph
    // override painting Ash's ':' and Path's '∙' onto dungeon floors.
    //
    // Substituting the tint outright would also throw away the cell's palette dither, which is
    // the anti-banding work the spread cap exists to protect. Taking the tint's CHROMATICITY at
    // the cell's OWN lightness keeps both: the room reads in its own hue, the dither survives
    // intact, and no lightness measurement anywhere in the renderer moves. TileVerifyProbe
    // --clarity asserts that invariant directly.
    //
    // PATH-D-PORT: the visual contract is "one room, one hue, unchanged brightness". It is a pure
    // function of (cell colour, room colour), both already sampled into TileKey, so it adds no
    // new per-cell input.
    //
    // MEMOISED because the conversion is six transcendentals and this runs per resolved cell. The
    // live domain is tiny - one era's Floor palette against four tints - so a direct-mapped table
    // of 32 answers holds all of it and costs one array read.
    private const int TintCacheSize = 32;
    private static readonly (int Key, Color Value)[] s_tintCache = new (int, Color)[TintCacheSize];

    public static Color TintAtOwnLightness(Color fg, Color tint)
    {
        int key = unchecked(((fg.R << 16 | fg.G << 8 | fg.B) * 486187739)
                          ^ (tint.R << 16 | tint.G << 8 | tint.B));
        if (key == 0) key = 1;                       // 0 is the empty-slot marker
        int slot = (key >>> 5) & (TintCacheSize - 1);
        ref var entry = ref s_tintCache[slot];
        if (entry.Key == key) return entry.Value;
        var mixed = Map.TileDefinitions.WithLightness(tint, Map.TileDefinitions.LightnessOf(fg));
        entry = (key, mixed);
        return mixed;
    }

    public static bool ShouldShowAggroIndicator(GameMap map, int monsterX, int monsterY, int playerX, int playerY) =>
        Math.Abs(monsterX - playerX) + Math.Abs(monsterY - playerY) <= AggroIndicatorRange
        && map.IsVisible(monsterX, monsterY);

    // Per-room tint for dungeon floors, keyed to an 8x8 region of the map so one room shares one
    // hue. TINT ONLY — it used to override the GLYPH as well, handing Floor cells ':' (which is
    // Ash) and '∙' (which is Path, one cell in four of it). A room that renders as a different
    // terrain is a worse trade than a room that renders in a different colour, because the map's
    // meaning is carried by its runes and its hue is whatever the active theme leaves of it.
    public static Color? GetRoomTheme(int x, int y, TileType type)
    {
        if (type != TileType.Floor) return null;
        int regionHash = (x / 8 * 48271 + y / 8 * 96137) & 0x7FFFFFFF;
        return (regionHash % 7) switch
        {
            0 or 4 => Color.Gray,
            1 or 5 => Color.BrightYellow,
            2      => Color.BrightCyan,
            3      => Color.BrightGreen,
            _      => null,
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
