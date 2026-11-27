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
        // Each edge has its own glyph, distinct from the ground's (',' is Grass and Snow), so the
        // water and lava markers read without colour and never as each other — the lava edge is
        // the one a player most needs to read at a glance.
        return map.GetTransitionCode(x, y) switch
        {
            GameMap.TransitionWater => (WaterEdge, Color.Blue),
            GameMap.TransitionLava  => (LavaEdge, Color.Red),
            _                       => ((char, Color)?)null,
        };
    }

    // The land-side edge markers. Named so the switch above and the exported array below cannot
    // drift apart.
    private const char WaterEdge = ';', LavaEdge = '^';
    public static readonly char[] TransitionGlyphs = { WaterEdge, LavaEdge };

    public static (char Glyph, Color Fg, Color Bg)? GetShorelineVisual(GameMap map, int x, int y, TileType type)
    {
        if (type is not (TileType.Water or TileType.WaterDeep)) return null;
        bool landN = HasLand(map, x, y - 1), landS = HasLand(map, x, y + 1);
        bool landE = HasLand(map, x + 1, y), landW = HasLand(map, x - 1, y);
        int landCount = (landN ? 1 : 0) + (landS ? 1 : 0) + (landE ? 1 : 0) + (landW ? 1 : 0);
        if (landCount == 0) return null;
        // A shoreline sits on a water cell, and deep water is a wall until Swimming L25, so the
        // shore draws glyphs of its own rather than a walkable tile's ('░' is Ice, '▒' CrackedWall).
        // Each side has its own half block, so the edge is directional on all four sides.
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
    // Three disjoint families: tildes are shallow water (free from Swimming L1), heavy tildes are
    // deep water (a wall until L25), dashes are lava. ResolveWater overrides the static glyph for
    // both water depths, so the families must differ by rune, not only by hue, which a theme such
    // as Amber Mono collapses.
    private static readonly char[] WaterGlyphs = { '~', '∼', '~', '∼' };
    private static readonly char[] DeepWaterGlyphs = { '≈', '≋', '≈', '≋' };
    private const int WaterStepMs = 800;

    public static char GetWaterFlowGlyph(int x, int y, bool deep = false)
    {
        var set = deep ? DeepWaterGlyphs : WaterGlyphs;
        int phase = ((int)(FrameClock.AmbientMs / WaterStepMs) + x * 3 + y * 7) % set.Length;
        return set[phase];
    }

    // The shoreline's glyphs. This is a render-time override, so sampling TileDefinitions cannot
    // see it; the switch above reads these named constants, so the exported array cannot drift
    // from what is drawn. These and the water sets below are exposed so the verification probes
    // can assert the glyph families stay disjoint.
    private const char ShoreNorth = '▄', ShoreSouth = '▀', ShoreEast = '▐',
                       ShoreWest = '▌', ShoreCorner = '▞';
    public static readonly char[] ShorelineGlyphs =
        { ShoreNorth, ShoreSouth, ShoreEast, ShoreWest, ShoreCorner };

    public static IReadOnlyList<char> ShallowWaterGlyphSet => WaterGlyphs;
    public static IReadOnlyList<char> DeepWaterGlyphSet => DeepWaterGlyphs;

    // A lily pad must not read as a rock ('●' is Rock's glyph). '○' is authored by no tile type,
    // sits in the same Geometric Shapes block as the '●'/'◆'/'◇' the table already uses (so it is
    // one column wide), and reads as a flat pad rather than a solid mass.
    private const char LilyPad = '○';

    // Shallow water only, and its own declared family: this is a render-time override, so nothing
    // sampling TileDefinitions can see it. Exported for the verification probes.
    public static readonly char[] WaterOverlayGlyphs = { LilyPad };

    public static (char Glyph, Color Color)? GetWaterLilyPad(int x, int y)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        return hash % 10 == 0 ? (LilyPad, Color.Green) : null;
    }

    // ── The room tint, applied as a transform of the cell's own colour ───────────────────────
    // Takes the tint's chromaticity at the cell's own lightness: the room reads in its own hue,
    // the cell's palette dither (the anti-banding spread) survives intact, and no lightness in the
    // renderer moves. Substituting the tint outright would throw the dither away.
    //
    // The picture depends on one room, one hue, unchanged brightness. It is a pure function of
    // (cell colour, room colour), both already sampled into TileKey, so it adds no new per-cell input.
    //
    // Memoised because the conversion is six transcendentals and this runs per resolved cell. The
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
    // hue. Tint only, never the glyph: Floor drawn as ':' or '∙' would read as Ash or Path, and
    // the map's meaning is carried by its runes while its hue is whatever the active theme leaves.
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
