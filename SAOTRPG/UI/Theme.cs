using Terminal.Gui;

namespace SAOTRPG.UI;

/// <summary>
/// Centralized color palette, Unicode constants, and reusable ColorSchemes
/// for the entire Aincrad TRPG UI. Every screen pulls from here so the
/// visual language stays consistent across the game.
/// </summary>
public static class Theme
{
    // ═══════════════════════════════════════════════════════════════════
    //  Unicode Glyphs — the visual vocabulary of the game
    // ═══════════════════════════════════════════════════════════════════

    // Box drawing — rounded corners for soft panels
    public const char CornerTL    = '╭';
    public const char CornerTR    = '╮';
    public const char CornerBL    = '╰';
    public const char CornerBR    = '╯';
    public const char HorzLine    = '─';
    public const char VertLine    = '│';

    // Heavy rules — major section breaks
    public const char HeavyHorz   = '━';
    public const char DoubleHorz  = '═';

    // Dotted rules — minor/subtle separators
    public const char DottedHorz  = '┄';

    // Block elements — HP bars, progress fills
    public const char BlockFull   = '█';
    public const char BlockDark   = '▓';
    public const char BlockMed    = '▒';
    public const char BlockLight  = '░';

    // Geometric — indicators, bullets
    public const char Diamond     = '◆';
    public const char DiamondOpen = '◇';
    public const char BulletFull  = '●';
    public const char BulletOpen  = '○';
    public const char SquareFull  = '■';
    public const char SquareOpen  = '□';
    public const char TriRight    = '▶';
    public const char TriLeft     = '◀';
    public const char Star        = '★';
    public const char StarOpen    = '☆';

    // RPG symbols
    public const char Sword       = '⚔';
    public const char Shield      = '⛊';
    public const char Heart       = '♥';
    public const char Sparkle     = '✦';
    public const char SparkleOpen = '✧';
    public const char Cross       = '✚';

    // Arrows — navigation hints
    public const char ArrowRight  = '→';
    public const char ArrowLeft   = '←';
    public const char ArrowUp     = '↑';
    public const char ArrowDown   = '↓';

    // ═══════════════════════════════════════════════════════════════════
    //  Pre-built Unicode Strings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Heavy separator — major section breaks (e.g. between title and menu)</summary>
    public static string HeavyRule(int width) => new string(HeavyHorz, width);

    /// <summary>Light separator — subsection breaks</summary>
    public static string LightRule(int width) => new string(HorzLine, width);

    /// <summary>Dotted separator — minor breaks within a section</summary>
    public static string DottedRule(int width) => new string(DottedHorz, width);

    /// <summary>Double-line separator — decorative / title framing</summary>
    public static string DoubleRule(int width) => new string(DoubleHorz, width);

    // ═══════════════════════════════════════════════════════════════════
    //  HP / Resource Bar Builder
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds an ASCII bar like: [████████▒▒▒▒] 75/100
    /// Uses block characters for a clean, retro RPG feel.
    /// </summary>
    public static string BuildBar(int current, int max, int barWidth = 20)
    {
        if (max <= 0) max = 1;
        current = Math.Clamp(current, 0, max);

        double ratio = (double)current / max;
        int filled = (int)(ratio * barWidth);
        int empty = barWidth - filled;

        return $"[{new string(BlockFull, filled)}{new string(BlockLight, empty)}] {current}/{max}";
    }

    /// <summary>
    /// Returns a label like "HP" colored by health percentage.
    /// Use this for choosing which ColorScheme to apply to HP displays.
    /// </summary>
    public static HealthState GetHealthState(int current, int max)
    {
        if (max <= 0) return HealthState.Critical;
        double ratio = (double)current / max;
        if (ratio > 0.6) return HealthState.Healthy;
        if (ratio > 0.3) return HealthState.Warning;
        return HealthState.Critical;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Color Palette — consistent across every screen
    // ═══════════════════════════════════════════════════════════════════

    // All backgrounds are pure black for that classic terminal RPG feel.
    private static readonly Color Bg = Color.Black;

    // --- Core UI Colors ---
    public static readonly ColorScheme Title = MakeScheme(Color.White, Bg);
    public static readonly ColorScheme Gold = MakeScheme(Color.BrightYellow, Bg);
    public static readonly ColorScheme Dim = MakeScheme(Color.DarkGray, Bg);
    public static readonly ColorScheme Subtitle = MakeScheme(Color.Gray, Bg);
    public static readonly ColorScheme Body = MakeScheme(Color.White, Bg);

    // --- Interactive Elements ---
    /// <summary>Menu buttons — gray normally, bright yellow when focused with diamond glyph</summary>
    public static readonly ColorScheme MenuButton = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray, Bg),
        Focus    = new Terminal.Gui.Attribute(Color.BrightYellow, Bg),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Bg),
        HotFocus  = new Terminal.Gui.Attribute(Color.BrightYellow, Bg),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Bg)
    };

    /// <summary>Frame borders — yellow tint for decorative frames (title screen menu, etc.)</summary>
    public static readonly ColorScheme Frame = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Yellow, Bg),
        Focus    = new Terminal.Gui.Attribute(Color.BrightYellow, Bg),
        HotNormal = new Terminal.Gui.Attribute(Color.Yellow, Bg),
        HotFocus  = new Terminal.Gui.Attribute(Color.BrightYellow, Bg),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Bg)
    };

    /// <summary>Subtle frame borders — for gameplay panels (combat log, stats, action bar)</summary>
    public static readonly ColorScheme FrameSubtle = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray, Bg),
        Focus    = new Terminal.Gui.Attribute(Color.Gray, Bg),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Bg),
        HotFocus  = new Terminal.Gui.Attribute(Color.White, Bg),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Bg)
    };

    /// <summary>Small inline buttons (+/- stat allocation, etc.)</summary>
    public static readonly ColorScheme SmallButton = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray, Bg),
        Focus    = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Bg),
        HotFocus  = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Bg)
    };

    /// <summary>Text input fields</summary>
    public static readonly ColorScheme Input = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        Focus    = new Terminal.Gui.Attribute(Color.BrightYellow, Color.DarkGray),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        HotFocus  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.DarkGray),
        Disabled  = new Terminal.Gui.Attribute(Color.Gray, Color.DarkGray)
    };

    // --- Combat Log Colors ---
    public static readonly ColorScheme LogGeneral = MakeScheme(Color.White, Bg);
    public static readonly ColorScheme LogCombat = MakeScheme(Color.BrightRed, Bg);
    public static readonly ColorScheme LogSystem = MakeScheme(Color.BrightYellow, Bg);
    public static readonly ColorScheme LogLoot = MakeScheme(Color.BrightGreen, Bg);

    // --- HP State Colors ---
    public static readonly ColorScheme HpHealthy = MakeScheme(Color.BrightGreen, Bg);
    public static readonly ColorScheme HpWarning = MakeScheme(Color.BrightYellow, Bg);
    public static readonly ColorScheme HpCritical = MakeScheme(Color.BrightRed, Bg);

    // --- Rarity Colors ---
    public static readonly ColorScheme RarityCommon = MakeScheme(Color.Gray, Bg);
    public static readonly ColorScheme RarityUncommon = MakeScheme(Color.BrightGreen, Bg);
    public static readonly ColorScheme RarityRare = MakeScheme(Color.BrightCyan, Bg);
    public static readonly ColorScheme RarityEpic = MakeScheme(Color.BrightMagenta, Bg);
    public static readonly ColorScheme RarityLegendary = MakeScheme(Color.BrightYellow, Bg);

    // --- Stat Colors — for visual grouping in stats panels ---
    public static readonly ColorScheme StatLabel = MakeScheme(Color.Gray, Bg);
    public static readonly ColorScheme StatValue = MakeScheme(Color.White, Bg);
    public static readonly ColorScheme StatHeader = MakeScheme(Color.BrightYellow, Bg);
    public static readonly ColorScheme StatPositive = MakeScheme(Color.BrightGreen, Bg);

    // --- Error / Feedback ---
    public static readonly ColorScheme Error = MakeScheme(Color.BrightRed, Bg);
    public static readonly ColorScheme Success = MakeScheme(Color.BrightGreen, Bg);
    public static readonly ColorScheme Info = MakeScheme(Color.BrightCyan, Bg);

    // --- Defeated / Disabled state ---
    public static readonly ColorScheme Defeated = MakeScheme(Color.DarkGray, Bg);

    // ═══════════════════════════════════════════════════════════════════
    //  Main Window Base Scheme
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Applied to the root Window — dark base that lets everything else pop</summary>
    public static readonly ColorScheme WindowBase = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.DarkGray, Bg),
        Focus    = new Terminal.Gui.Attribute(Color.Gray, Bg),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Bg),
        HotFocus  = new Terminal.Gui.Attribute(Color.White, Bg),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Bg)
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Rarity Lookup
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns the ColorScheme for a given rarity string.</summary>
    public static ColorScheme GetRarityScheme(string? rarity)
    {
        return (rarity?.ToLower()) switch
        {
            "uncommon"  => RarityUncommon,
            "rare"      => RarityRare,
            "epic"      => RarityEpic,
            "legendary" => RarityLegendary,
            _           => RarityCommon
        };
    }

    /// <summary>Returns the rarity display prefix glyph.</summary>
    public static char GetRarityGlyph(string? rarity)
    {
        return (rarity?.ToLower()) switch
        {
            "uncommon"  => DiamondOpen,
            "rare"      => Diamond,
            "epic"      => Sparkle,
            "legendary" => Star,
            _           => BulletOpen
        };
    }

    /// <summary>Returns the HP ColorScheme for a given health state.</summary>
    public static ColorScheme GetHpScheme(HealthState state)
    {
        return state switch
        {
            HealthState.Healthy  => HpHealthy,
            HealthState.Warning  => HpWarning,
            HealthState.Critical => HpCritical,
            _                    => HpHealthy
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Wires up diamond glyph focus indicators on a set of buttons.</summary>
    public static void AttachDiamondFocus(params Button[] buttons)
    {
        foreach (var btn in buttons)
        {
            btn.HasFocusChanged += (s, e) =>
            {
                if (s is Button b)
                    b.IsDefault = e.NewValue;
            };
        }
    }

    /// <summary>Shorthand — creates a flat (non-interactive) color scheme with same color in all states.</summary>
    private static ColorScheme MakeScheme(Color fg, Color bg)
    {
        var attr = new Terminal.Gui.Attribute(fg, bg);
        return new ColorScheme
        {
            Normal    = attr,
            Focus     = attr,
            HotNormal = attr,
            HotFocus  = attr,
            Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, bg)
        };
    }
}

/// <summary>Health percentage tiers for color-coded HP displays.</summary>
public enum HealthState
{
    Healthy,
    Warning,
    Critical
}
