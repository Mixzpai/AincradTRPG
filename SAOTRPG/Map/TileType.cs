using Terminal.Gui;

namespace SAOTRPG.Map;

public enum TileType
{
    // Ground
    Grass,
    GrassTall,
    GrassSparse,
    Path,
    Floor,

    // Structures
    Wall,
    Door,
    StairsUp,
    StairsDown,  // revisit previous floors

    // Hazards
    TrapSpike,      // Damages player when stepped on
    TrapTeleport,   // Teleports player to random location
    Lava,           // Deals damage each turn player stands on it

    // Special
    Campfire,       // Heals player when stepped on
    Fountain,       // Free heal (limited uses per floor)
    Shrine,         // Temporary ATK/DEF buff
    Pillar,         // Reveals nearby map area

    // Nature
    Water,
    WaterDeep,
    Tree,
    TreePine,
    Bush,
    Mountain,
    Rock,
    Flowers
}

public static class TileDefinitions
{
    // Animation tick — incremented each render frame for animated tiles
    public static int AnimTick { get; set; }

    // Seeded per-tile visual variation using position hash
    public static (char Glyph, Color Foreground, Color Background) GetVisual(TileType type, int x = 0, int y = 0)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;

        return type switch
        {
            // Ground — all use '.' but different greens/grays
            TileType.Grass       => GrassVisual(hash),
            TileType.GrassTall   => TallGrassVisual(hash),
            TileType.GrassSparse => ('.', Color.DarkGray, Color.Black),
            TileType.Path        => ('.', PathColor(hash), Color.Black),
            TileType.Floor       => ('.', FloorColor(hash), Color.Black),

            // Structures — unique glyphs, no variation
            TileType.Wall       => WallVisual(hash),
            TileType.Door       => ('▌', Color.BrightYellow, Color.DarkGray),
            TileType.StairsUp   => ('≫', Color.BrightCyan,   Color.DarkGray),
            TileType.StairsDown => ('≪', Color.Gray,         Color.DarkGray),

            // Traps — subtle visual hints (look like floor but slightly off)
            TileType.TrapSpike    => ('⚠', Color.DarkGray,  Color.Black),
            TileType.TrapTeleport => ('◎', Color.DarkGray,  Color.Black),
            TileType.Lava         => LavaVisual(hash),

            // Campfire — animated warm glow
            TileType.Campfire     => CampfireVisual(hash),

            // Landmarks
            TileType.Fountain     => FountainVisual(hash),
            TileType.Shrine       => ShrineVisual(hash),
            TileType.Pillar       => ('║', Color.BrightCyan,    Color.Black),

            // Water — animated '~' only
            TileType.Water     => WaterVisual(hash),
            TileType.WaterDeep => WaterDeepVisual(hash),

            // Vegetation — Unicode species glyphs
            TileType.Tree     => ('♣', TreeColor(hash),  Color.Black),
            TileType.TreePine => TreePineVisual(hash),
            TileType.Bush     => ('♠', BushColor(hash),  Color.Black),
            TileType.Flowers  => FlowerVisual(hash),

            // Terrain — Unicode mountain glyphs
            TileType.Mountain => MountainVisual(hash),
            TileType.Rock     => ('o', RockColor(hash),     Color.Black),

            _ => ('?', Color.Magenta, Color.Black),
        };
    }

    // ── Grass: '.' with color variation ──
    private static (char, Color, Color) GrassVisual(int hash)
    {
        int v = hash % 8;
        Color fg = v switch
        {
            0 or 1 or 2 or 3 => Color.Green,
            4 or 5            => Color.BrightGreen,
            _                 => Color.DarkGray,
        };
        return ('.', fg, Color.Black);
    }

    // ── Tall grass: '"' — denser feel ──
    private static (char, Color, Color) TallGrassVisual(int hash)
    {
        Color fg = (hash % 3) switch
        {
            0 => Color.Green,
            1 => Color.BrightGreen,
            _ => Color.Green,
        };
        return ('"', fg, Color.Black);
    }

    // ── Water: animated with depth shading — shallow ░, deep ▒ ──
    private static (char, Color, Color) WaterVisual(int hash)
    {
        int phase = (AnimTick + hash) % 4;
        // Alternate between flow glyph and depth shade
        char glyph = (phase < 2) ? '░' : '~';
        Color fg = phase switch
        {
            0 => Color.BrightBlue,
            1 => Color.Blue,
            2 => Color.BrightCyan,
            _ => Color.Blue,
        };
        return (glyph, fg, Color.Black);
    }

    private static (char, Color, Color) WaterDeepVisual(int hash)
    {
        int phase = (AnimTick + hash) % 3;
        // Deep water uses denser block element
        char glyph = (phase == 0) ? '▒' : '~';
        Color fg = phase switch
        {
            0 => Color.Blue,
            1 => Color.DarkGray,
            _ => Color.Blue,
        };
        return (glyph, fg, Color.Black);
    }

    // ── Color-only helpers ──
    private static Color PathColor(int hash) => (hash % 3) switch
    {
        0 => Color.BrightYellow,
        1 => Color.Yellow,
        _ => Color.BrightYellow,
    };

    private static Color FloorColor(int hash) => (hash % 3) switch
    {
        0 => Color.Gray,
        1 => Color.DarkGray,
        _ => Color.Gray,
    };

    // ── Wall: inner walls use ▓, edge walls overridden to █ by MapView ──
    private static (char, Color, Color) WallVisual(int hash)
    {
        Color fg = (hash % 4) switch
        {
            0 or 1 => Color.White,
            2      => Color.Gray,
            _      => Color.DarkGray,
        };
        return ('▓', fg, Color.Black);
    }

    private static Color TreeColor(int hash) => (hash % 3) switch
    {
        0 => Color.Green,
        1 => Color.BrightGreen,
        _ => Color.Green,
    };

    // ── Pine tree: ▲ with green/dark variation ──
    private static (char, Color, Color) TreePineVisual(int hash)
    {
        Color fg = (hash % 3) switch
        {
            0 or 1 => Color.Green,
            _      => Color.DarkGray,
        };
        return ('▲', fg, Color.Black);
    }

    private static Color PineColor(int hash) => (hash % 3) switch
    {
        0 or 1 => Color.Green,
        _      => Color.DarkGray,
    };

    private static Color BushColor(int hash) => (hash % 2) switch
    {
        0 => Color.Green,
        _ => Color.BrightGreen,
    };

    // ── Mountain: ▲ (large) and △ (small) with color variation ──
    private static (char, Color, Color) MountainVisual(int hash)
    {
        char glyph = (hash % 3) == 0 ? '△' : '▲';
        Color fg = (hash % 4) switch
        {
            0 => Color.Gray,
            1 => Color.White,
            2 => Color.DarkGray,
            _ => Color.Gray,
        };
        return (glyph, fg, Color.Black);
    }

    private static Color RockColor(int hash) => (hash % 2) switch
    {
        0 => Color.DarkGray,
        _ => Color.Gray,
    };

    // ── Campfire: animated warm glow ──
    private static (char, Color, Color) CampfireVisual(int hash)
    {
        int phase = (AnimTick + hash) % 3;
        Color fg = phase switch
        {
            0 => Color.BrightYellow,
            1 => Color.BrightRed,
            _ => Color.Yellow,
        };
        return ('&', fg, Color.Black);
    }

    // ── Fountain: animated blue sparkle with ring glyph ──
    private static (char, Color, Color) FountainVisual(int hash)
    {
        int phase = (AnimTick + hash) % 4;
        char glyph = phase < 2 ? '⊙' : '◉';
        Color fg = phase switch
        {
            0 => Color.BrightCyan,
            1 => Color.BrightBlue,
            2 => Color.Cyan,
            _ => Color.BrightCyan,
        };
        return (glyph, fg, Color.Black);
    }

    // ── Shrine: animated mystical glyph ──
    private static (char, Color, Color) ShrineVisual(int hash)
    {
        int phase = (AnimTick + hash) % 4;
        char glyph = phase < 2 ? '☥' : '✦';
        Color fg = phase switch
        {
            0 => Color.BrightMagenta,
            1 => Color.Magenta,
            2 => Color.BrightMagenta,
            _ => Color.BrightCyan,
        };
        return (glyph, fg, Color.Black);
    }

    // ── Flowers: Unicode floral glyphs with animated colors ──
    private static readonly Color[] FlowerColors =
    {
        Color.BrightYellow, Color.BrightRed, Color.BrightMagenta,
        Color.BrightCyan
    };

    private static readonly char[] FlowerGlyphs = { '✿', '❀', '✻' };

    private static (char, Color, Color) FlowerVisual(int hash)
    {
        int colorIdx = (hash + AnimTick / 3) % FlowerColors.Length;
        char glyph = FlowerGlyphs[hash % FlowerGlyphs.Length];
        return (glyph, FlowerColors[colorIdx], Color.Black);
    }

    // ── Lava: animated block elements simulating bubbling magma ──
    private static (char, Color, Color) LavaVisual(int hash)
    {
        int phase = (AnimTick + hash) % 6;
        char glyph = phase switch
        {
            0 or 1 => '▓',
            2 or 3 => '▒',
            _      => '░',
        };
        Color fg = phase switch
        {
            0 => Color.BrightRed,
            1 => Color.BrightYellow,
            2 => Color.Red,
            3 => Color.BrightRed,
            4 => Color.BrightYellow,
            _ => Color.Red,
        };
        return (glyph, fg, Color.DarkGray);
    }
}
