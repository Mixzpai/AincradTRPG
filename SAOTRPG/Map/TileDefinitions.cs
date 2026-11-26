using SAOTRPG.UI.Effects;

namespace SAOTRPG.Map;

// Glyph + colors per tile, position-hash variation, era themes (Verdant 1-5, Stone 6-10, Crimson 11-15, Crystal 16-20, Void 21+).
// RGB-based so LightingSystem's multiplicative blend produces richer tints than ANSI-16.
public static class TileDefinitions
{
    public static int CurrentFloor { get; set; } = 1;
    private static int Era => Math.Min((CurrentFloor - 1) / 5, 4);

    // Biome-config override for the Tree glyph; default '♣' is the generic shrub.
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

    // Biome-native palette additions. Raw Color values (not ColorSchemes)
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

    // Ore vein palette. Mineable variants use a slight pulse/contrast shade.
    //
    // Iron and Mithril were both '◊' and told apart by hue alone — the one place on the map where
    // an information channel had no second cue. Mithril is '◆' (filled) now, so the valuable vein
    // reads without colour. Divine keeps its own '◈'/'◊' pulse.
    private static readonly Color OreIronFg          = new(170, 170, 180);
    private static readonly Color OreIronDepletedFg  = new(110, 110, 115);
    private static readonly Color OreMithrilFg       = new(190, 210, 255);
    private static readonly Color OreMithrilDepFg    = new(110, 120, 140);
    private static readonly Color OreDivineFg        = new(255, 200, 80);
    private static readonly Color OreDivineDepFg     = new(140, 120, 80);

    // Wall clock driving animated tile phases; pinned by --freeze-anim via AmbientMs.
    private static long AnimClockMs => FrameClock.AmbientMs;

    // Tiles whose visual depends on the wall clock: they bypass the per-cell visual cache and
    // drive the realtime repaint gate when in the viewport.
    //
    // Water and WaterDeep are deliberately absent: their visuals here are pure functions of the
    // position hash with no clock input, so listing them only cost work — every water cell
    // skipped the visual cache and held the repaint gate open. Water's clock dependency comes
    // from MapView.ResolveWater instead, and the frame cache tracks it separately.
    public static bool IsAnimated(TileType type) =>
        type is TileType.Lava or TileType.Campfire or TileType.OreVeinDivine;

    // PATH-D-PORT: the authored appearance of every tile, and the boundary between a LOGICAL tile
    // and a terminal cell. Two inputs a port must preserve: the position hash, which is what makes
    // a field of grass look varied without storing per-cell data, and the wall-clock reads inside
    // LavaVisual/CampfireVisual/the divine ore, whose step constants are deliberately slow because
    // every phase turnover costs a terminal flush — a renderer without that cost can raise them.
    // ONE GLYPH PER MEANING, because colour is the only other channel and an accessibility theme
    // may collapse it (Amber Mono maps the world to luma). Two merges here are deliberate and
    // must stay: every trap draws as open ground ON PURPOSE, and the '.'/',' ground family is one
    // meaning — walkable open terrain — in several terrain flavours separated by hue alone, which
    // is decoration rather than information. '~' is likewise one meaning: shallow walkable water
    // and loose dry ground are all crossable at no cost. What may never merge is a pair that
    // differs in whether the player can walk it.
    // THE MAP'S OWN LEGEND, declared once so the Player Guide renders it rather than describing
    // it in prose. The Guide called the chest a "gold diamond" and it had stopped being one — a
    // written-out glyph is a claim that survives only until somebody edits the table above.
    // Landmarks and hazards only: the terrain a player walks over needs no key.
    public static readonly (TileType Type, string Meaning)[] MapLegend =
    {
        (TileType.Chest,                "An unopened chest"),
        (TileType.ChestOpened,          "A chest you have already emptied"),
        (TileType.StairsUp,             "Stairs up — the way off this floor"),
        (TileType.LabyrinthEntrance,    "The labyrinth archway"),
        (TileType.Door,                 "A door"),
        (TileType.CrackedWall,          "A cracked wall — break it for a hidden room"),
        (TileType.Lever,                "A lever, linked to a wall or door"),
        (TileType.PressurePlate,        "A pressure plate"),
        (TileType.Shrine,               "A shrine"),
        (TileType.SecretShrine,         "A secret shrine"),
        (TileType.EnchantShrine,        "An enchanting shrine"),
        (TileType.Fountain,             "A fountain"),
        (TileType.Anvil,                "An anvil — repair, enhance, evolve"),
        (TileType.BountyBoard,          "A bounty board"),
        (TileType.MonumentOfSwordsmen,  "The Monument of Swordsmen"),
        (TileType.LoreStone,            "A lore stone"),
        (TileType.Journal,              "A journal page"),
        (TileType.Campfire,             "A campfire — rest and cook here, once"),
        (TileType.Ash,                  "Ash — burnt ground, and what a spent campfire leaves"),
        (TileType.GasVent,              "A gas vent"),
        (TileType.OreVeinIron,          "An iron ore vein"),
        (TileType.OreVeinMithril,       "A mithril ore vein"),
        (TileType.OreVeinDivine,        "A divine ore vein, the richest tier"),
        (TileType.Water,                "Shallow water — crossable from Swimming L1"),
        (TileType.WaterDeep,            "Deep water — needs Swimming L25"),
        (TileType.Lava,                 "Lava — it will hurt"),
        (TileType.DangerZone,           "Corrupted ground — it burns you every step"),
        (TileType.BogWater,             "Bogwater — wading it poisons you"),
        (TileType.Mud,                  "Mud — it slows you for a turn or two"),
        (TileType.Ice,                  "Ice"),
        (TileType.CrackedIce,           "Cracked ice"),
        (TileType.Mountain,             "Mountain — impassable"),
        (TileType.Rock,                 "A boulder"),
        (TileType.Pillar,               "A pillar"),
    };

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
            TileType.TrapWeb      => ('.', GrassDim, Color.Black),
            TileType.TrapMagnet   => ('.', RockGray, Color.Black),
            TileType.TrapRune     => ('.', RockGray, Color.Black),
            TileType.Lava         => LavaVisual(hash),
            TileType.Campfire     => CampfireVisual(hash),
            TileType.Chest        => ('▤', GoldBright, Color.Black),
            TileType.ChestOpened  => ('◇', RockGray, Color.Black),
            TileType.Fountain     => ('⊙', FountainCyan, Color.Black),
            TileType.Shrine       => ('☥', ShrineViolet, Color.Black),
            TileType.Pillar       => ('║', WallGray, Color.Black),
            TileType.LoreStone    => ('‡', ShrineViolet, Color.Black),
            TileType.MonumentOfSwordsmen => ('M', GoldBright, Color.Black),
            // CORRUPTED GROUND DAMAGES YOU AND CAN KILL — "corrupted ground" is a recorded
            // killer name — and it drew an orange '.', which is the open-ground glyph. Under a
            // theme that collapses colour it was ordinary floor. Traps disguise themselves on
            // purpose; this is not a trap, it is terrain that hurts, and it should read as such.
            TileType.DangerZone   => ('%', LavaOrange, Color.Black),
            TileType.Anvil        => ('╬', GoldBright, Color.Black),
            TileType.BountyBoard  => ('▣', FountainCyan, Color.Black),
            TileType.EnchantShrine => ('✦', GoldBright, Color.Black),
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
            TileType.Ice        => ('░',  IcePaleBlue,    Color.Black),
            TileType.CrackedIce => ('▚',  CrackedIceDim,  Color.Black),
            TileType.Basalt     => ('.',  BasaltDark,     Color.Black),
            TileType.Ash        => (':',  AshWarmGray,    Color.Black),
            TileType.Mud        => ('~',  MudBrown,       Color.Black),
            // POISONS ON ENTRY. FB-742 moved this onto '~' reasoning that bog water is shallow walkable
    // water — right about walkability, wrong about harm. Same tilde family, because it IS water,
    // but its own mark: wading it costs you 1 + floor/2 damage a turn.
    TileType.BogWater   => ('\u223d',  BogWaterGreen,  Color.Black),
            TileType.Reeds      => ('|',  ReedsYellowGrn, Color.Black),
            // Ore veins. Divine pulses warm via animation phase; others static.
            TileType.OreVeinIron            => ('◊', OreIronFg,         Color.Black),
            TileType.OreVeinIronDepleted    => ('·', OreIronDepletedFg, Color.Black),
            TileType.OreVeinMithril         => ('◆', OreMithrilFg,      Color.Black),
            TileType.OreVeinMithrilDepleted => ('·', OreMithrilDepFg,   Color.Black),
            TileType.OreVeinDivine          => DivineVeinVisual(hash),
            TileType.OreVeinDivineDepleted  => ('·', OreDivineDepFg,    Color.Black),
            _ => ('?', Color.Magenta, Color.Black),
        };
    }

    // Per-cell colour variation is a DITHER, not a texture.
    // The position hash picks one colour from a row per cell, so a row's LIGHTNESS RANGE is noise
    // inside a field of one tile type. Measured in CIE L* before this cap: Grass era 0 spanned
    // 34.2 dL*, Tree era 0 31.0, Wall era 0 30.2, Floor era 0 23.5, Mountain 23.0 - on surfaces
    // covering ~88% of an overworld floor, against a ~1.0 dL* threshold of vision, a renderer
    // tuned to 0.79 dL* vignette rungs and a 0.46 dL* colour quantizer.
    //
    // WHAT IS PRESERVED IS THE ROW'S MEAN, NOT EVERY MEASUREMENT TAKEN AGAINST IT. A row wider
    // than the cap is compressed toward its own mean lightness, so a field of one terrain keeps
    // its average brightness - but a measurement that takes a MAXIMUM does move, by half the
    // compression: a row of span S comes back at span PaletteSpreadCap, so its brightest colour
    // falls (S - cap)/2 dL* and its darkest rises the same. Widest here is Grass era 0 at
    // (34.2 - 8)/2 = 13.1 dL* on the brightest blade. TileVerifyProbe --clarity reports the
    // per-row before/after from PaletteCompressions rather than restating these numbers, because
    // a figure quoted in a comment is stale the moment a palette row is edited.
    //
    // THE DITHER MUST SURVIVE. The hash is doing real anti-banding work on the foreground, so the
    // cap is a compression and never a collapse - 8 dL* is still ten times the vignette rung.
    //
    // Chromaticity is held EXACTLY: a scale applied in LINEAR rgb moves the colour along its own
    // ray from black, which is a pure lightness move. Hue and era identity cannot drift.
    public const double PaletteSpreadCap = 8.0;

    // WHAT THE CAP ACTUALLY DID, recorded as it runs rather than described afterwards. Every
    // compressed row appends (before, after) here, so a check can report the real effect and
    // notice a palette edit that silently changes it. Appended from static field initialisers
    // only - Damp is never called at runtime - so this is written once, during type init, and
    // read-only from then on.
    private static readonly List<(double Before, double After)> s_compressions = new();

    public static IReadOnlyList<(double Before, double After)> PaletteCompressions => s_compressions;

    private static Color[] Damp(params Color[] row)
    {
        if (row.Length < 2) return row;
        double lo = double.MaxValue, hi = double.MinValue, sum = 0;
        foreach (var c in row)
        {
            double l = LStarOf(c);
            sum += l;
            if (l < lo) lo = l;
            if (l > hi) hi = l;
        }
        double span = hi - lo;
        if (span <= PaletteSpreadCap) return row;
        double mean = sum / row.Length, k = PaletteSpreadCap / span;
        var damped = new Color[row.Length];
        for (int i = 0; i < row.Length; i++)
            damped[i] = WithLightness(row[i], mean + (LStarOf(row[i]) - mean) * k);
        double dlo = double.MaxValue, dhi = double.MinValue;
        foreach (var c in damped)
        {
            double l = LStarOf(c);
            if (l < dlo) dlo = l;
            if (l > dhi) dhi = l;
        }
        s_compressions.Add((span, dhi - dlo));
        return damped;
    }

    private static double SrgbToLinear(double channel)
    {
        channel /= 255.0;
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static byte LinearToSrgb(double u)
    {
        u = Math.Clamp(u, 0.0, 1.0);
        double v = u <= 0.0031308 ? u * 12.92 : 1.055 * Math.Pow(u, 1.0 / 2.4) - 0.055;
        return (byte)Math.Clamp(Math.Round(v * 255.0), 0.0, 255.0);
    }

    private static double LuminanceOf(Color c)
        => 0.2126 * SrgbToLinear(c.R) + 0.7152 * SrgbToLinear(c.G) + 0.0722 * SrgbToLinear(c.B);

    private static double LStarOf(Color c) => LightnessOf(c);

    // The two colour operations the dither is built out of, made public because the renderer's
    // room tint needs EXACTLY them - a tint that preserves a cell's own lightness is the same
    // move the cap makes, run in the other direction. One implementation, so the two cannot
    // disagree about what "lightness" means.
    public static double LightnessOf(Color c)
    {
        double y = LuminanceOf(c);
        return y > 0.008856 ? 116.0 * Math.Cbrt(y) - 16.0 : 903.3 * y;
    }

    public static Color WithLightness(Color c, double targetL)
    {
        double y0 = LuminanceOf(c);
        if (y0 <= 0.0) return c;
        double yt = targetL > 8.0 ? Math.Pow((targetL + 16.0) / 116.0, 3.0) : targetL / 903.3;
        double k = yt / y0;
        return new Color(LinearToSrgb(SrgbToLinear(c.R) * k),
                         LinearToSrgb(SrgbToLinear(c.G) * k),
                         LinearToSrgb(SrgbToLinear(c.B) * k));
    }

    private static readonly char[] GrassGlyphs = { '.', ',', '\'', '`' };

    // Era-indexed palettes; inner[i] array length = N in original `hash % N switch` (byte-identical output).
    // Eras: 0 Verdant, 1 Stone, 2 Crimson, 3 Crystal, 4 Void.
    private static readonly Color[][] GrassColors =
    {
        /* Era 0 */ Damp(GrassGreen, GrassBright, GrassDim),
        /* Era 1 */ Damp(GrassGreen, GrassDim, GrassDim),
        /* Era 2 */ Damp(GrassDim, new Color(100, 80, 70), new Color(100, 80, 70)),
        /* Era 3 */ Damp(new Color(80, 200, 200), GrassGreen, GrassGreen),
        /* Era 4 */ Damp(new Color(100, 70, 130), GrassDim, GrassDim),
    };

    private static (char, Color, Color) GrassVisual(int hash)
        => (GrassGlyphs[hash % GrassGlyphs.Length],
            GrassColors[Era][hash % GrassColors[Era].Length],
            Color.Black);

    private static readonly Color[][] TallGrassColors =
    {
        /* Era 0 */ Damp(GrassBright, GrassGreen),
        /* Era 1 */ Damp(GrassGreen),
        /* Era 2 */ Damp(GrassGreen, GrassDim),
        /* Era 3 */ Damp(new Color(80, 200, 200), GrassGreen),
        /* Era 4 */ Damp(GrassDim),
    };

    private static (char, Color, Color) TallGrassVisual(int hash)
        => ('"', TallGrassColors[Era][hash % TallGrassColors[Era].Length], Color.Black);

    // A third of shallow-water cells used to draw the DEEP-water glyph, which is a Swimming L25
    // gate — a route-planning lie on the tile a player is deciding whether to cross.
    private static (char, Color, Color) WaterVisual(int hash)
        => ((hash % 3) == 0 ? '∼' : '~', WaterBlue, Color.Black);

    private static (char, Color, Color) WaterDeepVisual(int hash)
        => ('≈', WaterDeepBlue, Color.Black);

    private static readonly Color[][] PathColors =
    {
        /* Era 0 */ Damp(PathSand, PathDim),
        /* Era 1 */ Damp(RockGray, PathDim),
        /* Era 2 */ Damp(PathSand, new Color(180, 100, 80)),
        /* Era 3 */ Damp(new Color(140, 200, 200), PathSand),
        /* Era 4 */ Damp(new Color(120, 100, 140), PathDim),
    };

    private static (char, Color, Color) PathVisual(int hash)
        => ((hash % 4) == 0 ? '∙' : '·',
            PathColors[Era][hash % PathColors[Era].Length],
            Color.Black);

    // Floor palette mirrors original: eras 0/1/3/4 mod 2, era 2 mod 3.
    private static readonly Color[][] FloorColors =
    {
        /* Era 0 */ Damp(FloorStone, FloorDim),
        /* Era 1 */ Damp(FloorDim, RockGray),
        /* Era 2 */ Damp(new Color(180, 100, 90), FloorDim, FloorDim),
        /* Era 3 */ Damp(new Color(140, 180, 180), FloorStone),
        /* Era 4 */ Damp(new Color(130, 100, 150), FloorDim),
    };

    private static (char, Color, Color) FloorVisual(int hash)
        => ((hash % 8) == 0 ? '·' : '.',
            FloorColors[Era][hash % FloorColors[Era].Length],
            Color.Black);

    private static readonly Color[][] WallColors =
    {
        /* Era 0 */ Damp(WallGray, new Color(200, 195, 185), WallDim),
        /* Era 1 */ Damp(WallGray, WallDim, WallDim),
        /* Era 2 */ Damp(new Color(180, 100, 90), WallDim, WallDim),
        /* Era 3 */ Damp(new Color(140, 200, 220), WallGray, WallGray),
        /* Era 4 */ Damp(new Color(160, 100, 180), WallDim, WallDim),
    };

    private static (char, Color, Color) WallVisual(int hash)
        => ('▓', WallColors[Era][hash % WallColors[Era].Length], Color.Black);

    private static readonly Color[][] TreeColors =
    {
        /* Era 0 */ Damp(GrassGreen, TreeDark, GrassBright),
        /* Era 1 */ Damp(TreeDark, GrassDim),
        /* Era 2 */ Damp(GrassDim, TreeDark),
        /* Era 3 */ Damp(new Color(60, 180, 180), TreeDark),
        /* Era 4 */ Damp(GrassDim, new Color(100, 60, 120)),
    };

    private static Color TreeColor(int hash) => TreeColors[Era][hash % TreeColors[Era].Length];

    private static readonly Color[][] BushColors =
    {
        /* Era 0 */ Damp(BushGreen, GrassBright),
        /* Era 1 */ Damp(BushGreen),
        /* Era 2 */ Damp(BushGreen, GrassDim),
        /* Era 3 */ Damp(new Color(70, 190, 180), BushGreen),
        /* Era 4 */ Damp(GrassDim, new Color(120, 80, 150)),
    };

    private static Color BushColor(int hash) => BushColors[Era][hash % BushColors[Era].Length];

    private static readonly Color[] RockColors = Damp(RockGray, WallDim);

    private static Color RockColor(int hash) => RockColors[hash % RockColors.Length];

    private static readonly Color[] MountainColors = Damp(MtnGray, WallGray, WallDim);

    private static (char, Color, Color) MountainVisual(int hash)
    {
        char glyph = (hash % 3) == 0 ? '△' : '▲';
        return (glyph, MountainColors[hash % MountainColors.Length], Color.Black);
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

    // Step lengths for the clock-driven tile visuals. Deliberately slow: these tiles are
    // re-resolved on idle frames now, so every phase turnover writes cells and costs a terminal
    // flush. They also read as a nervous flicker at anything much quicker. The `+ hash` offsets
    // below keep neighbouring tiles out of lockstep so the field breathes instead of pulsing.
    private const int LavaStepMs     = 500;
    private const int CampfireStepMs = 600;
    private const int DivineStepMs   = 600;

    // Lava pulses between orange and yellow.
    private static (char, Color, Color) LavaVisual(int hash)
    {
        // Dashes, not tildes: lava drew the shallow-water glyph for half its phases, so a lethal
        // blocking tile read as water a player could wade across.
        int phase = ((int)(AnimClockMs / LavaStepMs) + hash) % 4;
        char glyph = phase < 2 ? '-' : '=';
        // Pulse color between orange and bright yellow
        Color c = phase % 2 == 0 ? LavaOrange : new Color(255, 160, 40);
        return (glyph, c, Color.Black);
    }

    // Campfire flickers between glyphs and warm colors.
    private static (char, Color, Color) CampfireVisual(int hash)
    {
        int phase = ((int)(AnimClockMs / CampfireStepMs) + hash) % 3;
        char glyph = phase == 0 ? '&' : phase == 1 ? '*' : '&';
        Color c = phase == 1 ? new Color(255, 150, 50) : FireYellow;
        return (glyph, c, Color.Black);
    }

    // Divine vein pulses between '◈' (cool) and '◊' (warm).
    private static (char, Color, Color) DivineVeinVisual(int hash)
    {
        // Glyph held constant at '◈' and the pulse carried by colour alone: it used to alternate
        // to '◊', which is Iron's, so for half of every cycle a divine vein and an iron vein were
        // the same glyph in different hues — a colour-only distinction on the most valuable tile
        // on the map.
        int phase = ((int)(AnimClockMs / DivineStepMs) + hash) % 4;
        Color c = phase % 2 == 0 ? OreDivineFg : new Color(255, 230, 130);
        return ('◈', c, Color.Black);
    }

    // MEASUREMENT SEAM, same family as LightingSystem.SourcesLastUpdate: every position-hashed
    // colour row exactly as GetVisual reads it. A check brings its own CIE L* and asserts the
    // spread against PaletteSpreadCap - the game reports what it produced, never the metric.
    // Declared last so every row above it has been initialised.
    public static readonly (string Name, Color[] Colors)[] PaletteRows =
    {
        ("Grass era0", GrassColors[0]),          ("Grass era1", GrassColors[1]),
        ("Grass era2", GrassColors[2]),          ("Grass era3", GrassColors[3]),
        ("Grass era4", GrassColors[4]),
        ("GrassTall era0", TallGrassColors[0]),  ("GrassTall era1", TallGrassColors[1]),
        ("GrassTall era2", TallGrassColors[2]),  ("GrassTall era3", TallGrassColors[3]),
        ("GrassTall era4", TallGrassColors[4]),
        ("Path era0", PathColors[0]),            ("Path era1", PathColors[1]),
        ("Path era2", PathColors[2]),            ("Path era3", PathColors[3]),
        ("Path era4", PathColors[4]),
        ("Floor era0", FloorColors[0]),          ("Floor era1", FloorColors[1]),
        ("Floor era2", FloorColors[2]),          ("Floor era3", FloorColors[3]),
        ("Floor era4", FloorColors[4]),
        ("Wall era0", WallColors[0]),            ("Wall era1", WallColors[1]),
        ("Wall era2", WallColors[2]),            ("Wall era3", WallColors[3]),
        ("Wall era4", WallColors[4]),
        ("Tree era0", TreeColors[0]),            ("Tree era1", TreeColors[1]),
        ("Tree era2", TreeColors[2]),            ("Tree era3", TreeColors[3]),
        ("Tree era4", TreeColors[4]),
        ("Bush era0", BushColors[0]),            ("Bush era1", BushColors[1]),
        ("Bush era2", BushColors[2]),            ("Bush era3", BushColors[3]),
        ("Bush era4", BushColors[4]),
        ("Mountain", MountainColors),            ("Rock", RockColors),
    };
}
