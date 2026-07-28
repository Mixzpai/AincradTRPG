using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Centralized color scheme definitions used across all UI screens.
// Keeps visual consistency and makes palette changes easy.
// Static palettes are registered with Terminal.Gui's SchemeManager at startup (RegisterAll)
// under "SAO."-prefixed names so views can bind via SchemeName; dynamic one-offs (FromColor,
// fade-scaled schemes) are applied directly with View.SetScheme and never registered.
public static class ColorSchemes
{
    // ── SchemeManager registration names ────────────────────────────

    public const string ButtonName        = "SAO.Button";
    public const string GoldName          = "SAO.Gold";
    public const string DimName           = "SAO.Dim";
    public const string BodyName          = "SAO.Body";
    public const string TitleName         = "SAO.Title";
    public const string DangerName        = "SAO.Danger";
    public const string SuccessName       = "SAO.Success";
    public const string DialogName        = "SAO.Dialog";
    public const string MenuButtonName    = "SAO.MenuButton";
    public const string TierRadioName     = "SAO.TierRadio";
    public const string ListSelectionName = "SAO.ListSelection";

    // Register every static palette with SchemeManager. Call once at startup,
    // after AppHost.Start() and before any UI construction.
    public static void RegisterAll()
    {
        SchemeManager.AddScheme(ButtonName,        Button);
        SchemeManager.AddScheme(GoldName,          Gold);
        SchemeManager.AddScheme(DimName,           Dim);
        SchemeManager.AddScheme(BodyName,          Body);
        SchemeManager.AddScheme(TitleName,         Title);
        SchemeManager.AddScheme(DangerName,        Danger);
        SchemeManager.AddScheme(SuccessName,       Success);
        SchemeManager.AddScheme(DialogName,        Dialog);
        SchemeManager.AddScheme(MenuButtonName,    MenuButton);
        SchemeManager.AddScheme(TierRadioName,     TierRadio);
        SchemeManager.AddScheme(ListSelectionName, ListSelection);
    }

    // ── Core UI palettes ─────────────────────────────────────────────

    // Standard button: gray idle, BrightYellow focus — unified with MenuButton for consistency.
    public static readonly Scheme Button = new()
    {
        Normal   = Gfx.Attr(Color.Gray,         Color.Black),
        Focus    = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotNormal = Gfx.Attr(Color.Gray,         Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightYellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,    Color.Black)
    };

    // Bright gold accent for highlights, dividers, titles.
    public static readonly Scheme Gold = new()
    {
        Normal   = Gfx.Attr(Color.BrightYellow, Color.Black),
        Focus    = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotNormal = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightYellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,    Color.Black)
    };

    // Muted gray for secondary text, footers, dim labels.
    public static readonly Scheme Dim = new()
    {
        Normal   = Gfx.Attr(Color.DarkGray, Color.Black),
        Focus    = Gfx.Attr(Color.DarkGray, Color.Black),
        HotNormal = Gfx.Attr(Color.DarkGray, Color.Black),
        HotFocus  = Gfx.Attr(Color.DarkGray, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Standard body text — gray on black.
    public static readonly Scheme Body = new()
    {
        Normal   = Gfx.Attr(Color.Gray, Color.Black),
        Focus    = Gfx.Attr(Color.Gray, Color.Black),
        HotNormal = Gfx.Attr(Color.Gray, Color.Black),
        HotFocus  = Gfx.Attr(Color.Gray, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Bright white for emphasis — titles, important labels.
    public static readonly Scheme Title = new()
    {
        Normal   = Gfx.Attr(Color.White, Color.Black),
        Focus    = Gfx.Attr(Color.White, Color.Black),
        HotNormal = Gfx.Attr(Color.White, Color.Black),
        HotFocus  = Gfx.Attr(Color.White, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Danger/death red for alerts.
    public static readonly Scheme Danger = new()
    {
        Normal   = Gfx.Attr(Color.BrightRed, Color.Black),
        Focus    = Gfx.Attr(Color.BrightRed, Color.Black),
        HotNormal = Gfx.Attr(Color.BrightRed, Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightRed, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Success green for positive feedback — equip, purchase, heal.
    public static readonly Scheme Success = new()
    {
        Normal   = Gfx.Attr(Color.BrightGreen, Color.Black),
        Focus    = Gfx.Attr(Color.BrightGreen, Color.Black),
        HotNormal = Gfx.Attr(Color.BrightGreen, Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightGreen, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,   Color.Black)
    };

    // ── Dialog palettes ───────────────────────────────────────────────

    // Black-background dialog — matches the game's terminal look.
    public static readonly Scheme Dialog = new()
    {
        Normal   = Gfx.Attr(Color.Gray,      Color.Black),
        Focus    = Gfx.Attr(Color.White,      Color.Black),
        HotNormal = Gfx.Attr(Color.Gray,      Color.Black),
        HotFocus  = Gfx.Attr(Color.White,     Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,  Color.Black)
    };

    // ── Menu-specific palettes ───────────────────────────────────────

    // Menu button — gray idle, bright gold on focus.
    public static readonly Scheme MenuButton = new()
    {
        Normal   = Gfx.Attr(Color.Gray,         Color.Black),
        Focus    = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotNormal = Gfx.Attr(Color.Gray,         Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightYellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,    Color.Black)
    };

    // ── Selection palettes ────────────────────────────────────────────

    // Tier / radio: white idle, black-on-gold focus so highlight is unmissable.
    public static readonly Scheme TierRadio = new()
    {
        Normal    = Gfx.Attr(Color.White,        Color.Black),
        Focus     = Gfx.Attr(Color.Black,        Color.BrightYellow),
        HotNormal = Gfx.Attr(Color.White,        Color.Black),
        HotFocus  = Gfx.Attr(Color.Black,        Color.BrightYellow),
        Disabled  = Gfx.Attr(Color.DarkGray,     Color.Black),
    };

    // ListView selection: dim gray rows, bright-inverted under cursor.
    // Used by Player Guide topic list + any vertically-scrolling picker.
    public static readonly Scheme ListSelection = new()
    {
        Normal    = Gfx.Attr(Color.Gray,         Color.Black),
        Focus     = Gfx.Attr(Color.Black,        Color.BrightYellow),
        HotNormal = Gfx.Attr(Color.Gray,         Color.Black),
        HotFocus  = Gfx.Attr(Color.Black,        Color.BrightYellow),
        Disabled  = Gfx.Attr(Color.DarkGray,     Color.Black),
    };

    // ── Dynamic palette helpers ─────────────────────────────────────

    // Non-interactive scheme with `fg` on black — for one-off labels.
    // Dynamic: apply with View.SetScheme, never registered with SchemeManager.
    public static Scheme FromColor(Color fg) => new()
    {
        Normal    = Gfx.Attr(fg,             Color.Black),
        Focus     = Gfx.Attr(fg,             Color.Black),
        HotNormal = Gfx.Attr(fg,             Color.Black),
        HotFocus  = Gfx.Attr(fg,             Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };
}
