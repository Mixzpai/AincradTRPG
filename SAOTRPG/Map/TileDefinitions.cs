using Terminal.Gui;

namespace SAOTRPG.Map;

// Glyph + colors per tile, position-hash variation, era themes (Verdant 1-5, Stone 6-10, Crimson 11-15, Crystal 16-20, Void 21+).
// RGB-based so LightingSystem's multiplicative blend produces richer tints than ANSI-16.
public static class TileDefinitions
{
    public static int CurrentFloor { get; set; } = 1;
    private static int Era => Math.Min((CurrentFloor - 1) / 5, 4);

    // Biome-config override for the Tree glyph; default '♣' matches pre-Bundle-5 renderer.
    // Set by BiomeSystem.SetFloor from the active BiomeGenConfig.TreeGlyph.
    public static char CurrentTreeGlyph { get; set; } = '♣';

    // RGB palette — richer base colors so lighting multiplication yields warm/cool gradients, not flat tints.
    private static readonly Color GrassGreen    = new(60, 180, 70);
    private static readonly Color GrassBright   = new(90, 220, 100);
    private static readonly Color GrassDim      = new(40, 120, 50);
    private static readonly Color PathSand      = new(200, 180, 120);
    private static readonly Color PathDim       = new(160, 140, 90);
    private static readonly Color FloorStone    = new(160, 155, 145);
    private static readonly Color FloorDim      = new(100, 95, 90);
    private static readonly Color WaterBlue     = new(70, 140, 220);
    private static readonly Color WaterDeepBlue = new(40, 80, 180);
    private static readonly Color WallGray      = new(180, 175, 170);
    private static readonly Color WallDim       = new(120, 115, 110);
    private static readonly Color TreeDark      = new(30, 130, 50);
    private static readonly Color BushGreen     = new(50, 160, 60);
    private static readonly Color MtnGray       = new(150, 150, 155);
    private static readonly Color RockGray      = new(130, 125, 120);
    private static readonly Color LavaOrange    = new(255, 90, 30);
    private static readonly Color FireYellow    = new(255, 200, 80);
    private static readonly Color ShrineViolet  = new(200, 120, 255);
    private static readonly Color FountainCyan  = new(100, 220, 255);
    private static readonly Color GoldBright    = new(255, 220, 80);

    // Bundle 5 biome-native palette additions. Raw Color values (not ColorSchemes)
    // because tile rendering passes Color directly to GetVisual and DrawGlyph.
    private static readonly Color SandTan        = new(210, 180, 120);
    private static readonly Color DuneSandBright = new(230, 200, 140);
    private static readonly Color SnowPale       = new(230, 235, 245);
    private static readonly Color IcePaleBlue    = new(180, 210, 230);
    private static readonly Color CrackedIceDim  = new(140, 180, 210);
    private static readonly Color BasaltDark     = new(60, 55, 65);
    private static readonly Color AshWarmGray    = new(110, 100, 95);
    private static readonly Color MudBrown       = new(90, 70, 50);
    private static readonly Color BogWaterGreen  = new(60, 80, 50);
    private static readonly Color ReedsYellowGrn = new(120, 140, 70);

    // Bundle 10 — ore vein palette. Mineable variants use a slight pulse/contrast shade.
    private static readonly Color OreIronFg          = new(170, 170, 180);
    private static readonly Color OreIronDepletedFg  = new(110, 110, 115);
    private static readonly Color OreMithrilFg       = new(190, 210, 255);
    private static readonly Color OreMithrilDepFg    = new(110, 120, 140);
    private static readonly Color OreDivineFg        = new(255, 200, 80);
    private static readonly Color OreDivineDepFg     = new(140, 120, 80);

    public static (char Glyph, Color Foreground, Color Background) GetVisual(TileType type, int x = 0, int y = 0)
    {
        int hash = (x * 374761393 + y * 668265263) & 0x7FFFFFFF;
        return type switch
        {
            TileType.Grass       => GrassVisual(hash),
            TileType.GrassTall   => TallGrassVisual(hash),
            TileType.GrassSparse => ('.', GrassDim, Color.Black),
            TileType.Path        => PathVisual(hash),
            TileType.Floor       => FloorVisual(hash),
            TileType.Wall        => WallVisual(hash),
            TileType.CrackedWall => ('▒', WallDim, Color.Black),
            TileType.Door        => ('+', GoldBright, Color.Black),
            TileType.StairsUp    => ('>', FountainCyan, Color.Black),
            TileType.StairsDown  => ('<', FloorDim, Color.Black),
            TileType.TrapSpike    => ('.', RockGray, Color.Black),
            TileType.TrapTeleport => ('.', RockGray, Color.Black),
            TileType.TrapPoison   => ('.', GrassDim, Color.Black),
            TileType.TrapAlarm    => ('.', RockGray, Color.Black),
            TileType.Lava         => LavaVisual(hash),
            TileType.Campfire     => CampfireVisual(hash),
            TileType.Chest        => ('◈', GoldBright, Color.Black),
            TileType.ChestOpened  => ('◇', RockGray, Color.Black),
            TileType.Fountain     => ('⊙', FountainCyan, Color.Black),
            TileType.Shrine       => ('☥', ShrineViolet, Color.Black),
            TileType.Pillar       => ('║', WallGray, Color.Black),
            TileType.LoreStone    => ('◆', ShrineViolet, Color.Black),
            TileType.MonumentOfSwordsmen => ('M', GoldBright, Color.Black),
            TileType.DangerZone   => ('.', LavaOrange, Color.Black),
            TileType.Anvil        => ('╬', GoldBright, Color.Black),
            TileType.BountyBoard  => ('▣', FountainCyan, Color.Black),
            TileType.EnchantShrine => ('☥', GoldBright, Color.Black),
            TileType.SecretShrine => ('!', new Color(255, 100, 255), Color.Black),
            TileType.Journal      => ('≡', GoldBright, Color.Black),
            TileType.GasVent      => ('¤', new Color(120, 255, 120), Color.Black),
            TileType.Lever        => ('╥', GoldBright, Color.Black),
            TileType.PressurePlate => ('▫', GoldBright, Color.Black),
            TileType.LabyrinthEntrance => ('Π', FountainCyan, Color.Black),
            TileType.Water     => WaterVisual(hash),
            TileType.WaterDeep => WaterDeepVisual(hash),
            TileType.Tree      => (CurrentTreeGlyph, TreeColor(hash), Color.Black),
            TileType.TreePine  => ('▲', TreeColor(hash), Color.Black),
            TileType.Bush      => ('※', BushColor(hash), Color.Black),
            TileType.Flowers   => FlowerVisual(hash),
            TileType.Mountain  => MountainVisual(hash),
            TileType.Rock      => ('●', RockColor(hash), Color.Black),
            TileType.Sand       => ('.',  SandTan,        Color.Black),
            TileType.DuneSand   => ('~',  DuneSandBright, Color.Black),
            TileType.Snow       => ((hash % 3) == 0 ? ',' : '.', SnowPale, Color.Black),
            TileType.Ice        => ('≈',  IcePaleBlue,    Color.Black),
            TileType.CrackedIce => ('~',  CrackedIceDim,  Color.Black),
            TileType.Basalt     => ('.',  BasaltDark,     Color.Black),
            TileType.Ash        => (':',  AshWarmGray,    Color.Black),
            TileType.Mud        => ('~',  MudBrown,       Color.Black),
            TileType.BogWater   => ('≈',  BogWaterGreen,  Color.Black),
            TileType.Reeds      => ('|',  ReedsYellowGrn, Color.Black),
            // Bundle 10 — ore veins. Divine pulses warm via animation phase; others static.
            TileType.OreVeinIron            => ('◊', OreIronFg,         Color.Black),
            TileType.OreVeinIronDepleted    => ('·', OreIronDepletedFg, Color.Black),
            TileType.OreVeinMithril         => ('◊', OreMithrilFg,      Color.Black),
            TileType.OreVeinMithrilDepleted => ('·', OreMithrilDepFg,   Color.Black),
            TileType.OreVeinDivine          => DivineVeinVisual(hash),
            TileType.OreVeinDivineDepleted  => ('·', OreDivineDepFg,    Color.Black),
            _ => ('?', Color.Magenta, Color.Black),
        };
    }

    private static readonly char[] GrassGlyphs = { '.', ',', '\'', '`' };

    // Era-indexed palettes; inner[i] array length = N in original `hash % N switch` (byte-identical output).
    // Eras: 0 Verdant, 1 Stone, 2 Crimson, 3 Crystal, 4 Void.
    private static readonly Color[][] GrassColors =
    {
        /* Era 0 */ new[] { GrassGreen, GrassBright, GrassDim },
        /* Era 1 */ new[] { GrassGreen, GrassDim, GrassDim },
        /* Era 2 */ new[] { GrassDim, new Color(100, 80, 70), new Color(100, 80, 70) },
        /* Era 3 */ new[] { new Color(80, 200, 200), GrassGreen, GrassGreen },
        /* Era 4 */ new[] { new Color(100, 70, 130), GrassDim, GrassDim },
    };

    private static (char, Color, Color) GrassVisual(int hash)
        => (GrassGlyphs[hash % GrassGlyphs.Length],
            GrassColors[Era][hash % GrassColors[Era].Length],
            Color.Black);

    private static readonly Color[][] TallGrassColors =
    {
        /* Era 0 */ new[] { GrassBright, GrassGreen },
        /* Era 1 */ new[] { GrassGreen },
        /* Era 2 */ new[] { GrassGreen, GrassDim },
        /* Era 3 */ new[] { new Color(80, 200, 200), GrassGreen },
        /* Era 4 */ new[] { GrassDim },
    };

    private static (char, Color, Color) TallGrassVisual(int hash)
        => ('"', TallGrassColors[Era][hash % TallGrassColors[Era].Length], Color.Black);

    private static (char, Color, Color) WaterVisual(int hash)
        => ((hash % 3) == 0 ? '≈' : '~', WaterBlue, Color.Black);

    private static (char, Color, Color) WaterDeepVisual(int hash)
        => ('≈', WaterDeepBlue, Color.Black);

    private static readonly Color[][] PathColors =
    {
        /* Era 0 */ new[] { PathSand, PathDim },
        /* Era 1 */ new[] { RockGray, PathDim },
        /* Era 2 */ new[] { PathSand, new Color(180, 100, 80) },
        /* Era 3 */ new[] { new Color(140, 200, 200), PathSand },
        /* Era 4 */ new[] { new Color(120, 100, 140), PathDim },
    };

    private static (char, Color, Color) PathVisual(int hash)
        => ((hash % 4) == 0 ? '∙' : '·',
            PathColors[Era][hash % PathColors[Era].Length],
            Color.Black);

    // Floor palette mirrors original: eras 0/1/3/4 mod 2, era 2 mod 3.
    private static readonly Color[][] FloorColors =
    {
        /* Era 0 */ new[] { FloorStone, FloorDim },
        /* Era 1 */ new[] { FloorDim, RockGray },
        /* Era 2 */ new[] { new Color(180, 100, 90), FloorDim, FloorDim },
        /* Era 3 */ new[] { new Color(140, 180, 180), FloorStone },
        /* Era 4 */ new[] { new Color(130, 100, 150), FloorDim },
    };

    private static (char, Color, Color) FloorVisual(int hash)
        => ((hash % 8) == 0 ? '·' : '.',
            FloorColors[Era][hash % FloorColors[Era].Length],
            Color.Black);

    private static readonly Color[][] WallColors =
    {
        /* Era 0 */ new[] { WallGray, new Color(200, 195, 185), WallDim },
        /* Era 1 */ new[] { WallGray, WallDim, WallDim },
        /* Era 2 */ new[] { new Color(180, 100, 90), WallDim, WallDim },
        /* Era 3 */ new[] { new Color(140, 200, 220), WallGray, WallGray },
        /* Era 4 */ new[] { new Color(160, 100, 180), WallDim, WallDim },
    };

    private static (char, Color, Color) WallVisual(int hash)
        => ('▓', WallColors[Era][hash % WallColors[Era].Length], Color.Black);

    private static readonly Color[][] TreeColors =
    {
        /* Era 0 */ new[] { GrassGreen, TreeDark, GrassBright },
        /* Era 1 */ new[] { TreeDark, GrassDim },
        /* Era 2 */ new[] { GrassDim, TreeDark },
        /* Era 3 */ new[] { new Color(60, 180, 180), TreeDark },
        /* Era 4 */ new[] { GrassDim, new Color(100, 60, 120) },
    };

    private static Color TreeColor(int hash) => TreeColors[Era][hash % TreeColors[Era].Length];

    private static readonly Color[][] BushColors =
    {
        /* Era 0 */ new[] { BushGreen, GrassBright },
        /* Era 1 */ new[] { BushGreen },
        /* Era 2 */ new[] { BushGreen, GrassDim },
        /* Era 3 */ new[] { new Color(70, 190, 180), BushGreen },
        /* Era 4 */ new[] { GrassDim, new Color(120, 80, 150) },
    };

    private static Color BushColor(int hash) => BushColors[Era][hash % BushColors[Era].Length];

    private static Color RockColor(int hash) => (hash % 2) == 0 ? RockGray : WallDim;

    private static (char, Color, Color) MountainVisual(int hash)
    {
        char glyph = (hash % 3) == 0 ? '△' : '▲';
        Color fg = (hash % 3) switch { 0 => MtnGray, 1 => WallGray, _ => WallDim };
        return (glyph, fg, Color.Black);
    }

    private static readonly Color[] FlowerColors =
    {
        new(255, 220, 80),   // gold
        new(255, 100, 100),  // rose
        new(220, 120, 255),  // violet
        new(120, 220, 255),  // sky
    };
    private static readonly char[] FlowerGlyphs = { '✿', '❀', '✻' };

    private static (char, Color, Color) FlowerVisual(int hash)
        => (FlowerGlyphs[hash % FlowerGlyphs.Length],
            FlowerColors[hash % FlowerColors.Length],
            Color.Black);

    // Lava pulses between orange and yellow using the animation turn counter.
    private static (char, Color, Color) LavaVisual(int hash)
    {
        int phase = (UI.Helpers.MapEffects.AnimationTurn + hash) % 4;
        char glyph = phase < 2 ? '~' : '-';
        // Pulse color between orange and bright yellow
        Color c = phase % 2 == 0 ? LavaOrange : new Color(255, 160, 40);
        return (glyph, c, Color.Black);
    }

    // Campfire flickers between glyphs and warm colors.
    private static (char, Color, Color) CampfireVisual(int hash)
    {
        int phase = (UI.Helpers.MapEffects.AnimationTurn + hash) % 3;
        char glyph = phase == 0 ? '&' : phase == 1 ? '*' : '&';
        Color c = phase == 1 ? new Color(255, 150, 50) : FireYellow;
        return (glyph, c, Color.Black);
    }

    // Bundle 10 — Divine vein pulses between '◈' (cool) and '◊' (warm) to telegraph rarity.
    private static (char, Color, Color) DivineVeinVisual(int hash)
    {
        int phase = (UI.Helpers.MapEffects.AnimationTurn + hash) % 4;
        char glyph = phase < 2 ? '◈' : '◊';
        Color c = phase % 2 == 0 ? OreDivineFg : new Color(255, 230, 130);
        return (glyph, c, Color.Black);
    }
}
