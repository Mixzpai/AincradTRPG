using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Centralized color scheme definitions used across all UI screens.
// Keeps visual consistency and makes palette changes easy.
public static class ColorSchemes
{
    // ── Core UI palettes ─────────────────────────────────────────────

    // Standard button style: gray text, white-on-dark when focused.
    // All interactive buttons: gray idle, bright gold on focus.
    // Unified with MenuButton so every button in the game highlights
    // the same way — no more white vs yellow inconsistency.
    public static readonly ColorScheme Button = new()
    {
        Normal   = Gfx.Attr(Color.Gray,         Color.Black),
        Focus    = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotNormal = Gfx.Attr(Color.Gray,         Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightYellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,    Color.Black)
    };

    // Bright gold accent for highlights, dividers, titles.
    public static readonly ColorScheme Gold = new()
    {
        Normal   = Gfx.Attr(Color.BrightYellow, Color.Black),
        Focus    = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotNormal = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightYellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,    Color.Black)
    };

    // Muted gray for secondary text, footers, dim labels.
    public static readonly ColorScheme Dim = new()
    {
        Normal   = Gfx.Attr(Color.DarkGray, Color.Black),
        Focus    = Gfx.Attr(Color.DarkGray, Color.Black),
        HotNormal = Gfx.Attr(Color.DarkGray, Color.Black),
        HotFocus  = Gfx.Attr(Color.DarkGray, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Standard body text — gray on black.
    public static readonly ColorScheme Body = new()
    {
        Normal   = Gfx.Attr(Color.Gray, Color.Black),
        Focus    = Gfx.Attr(Color.Gray, Color.Black),
        HotNormal = Gfx.Attr(Color.Gray, Color.Black),
        HotFocus  = Gfx.Attr(Color.Gray, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Bright white for emphasis — titles, important labels.
    public static readonly ColorScheme Title = new()
    {
        Normal   = Gfx.Attr(Color.White, Color.Black),
        Focus    = Gfx.Attr(Color.White, Color.Black),
        HotNormal = Gfx.Attr(Color.White, Color.Black),
        HotFocus  = Gfx.Attr(Color.White, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Danger/death red for alerts.
    public static readonly ColorScheme Danger = new()
    {
        Normal   = Gfx.Attr(Color.BrightRed, Color.Black),
        Focus    = Gfx.Attr(Color.BrightRed, Color.Black),
        HotNormal = Gfx.Attr(Color.BrightRed, Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightRed, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Success green for positive feedback — equip, purchase, heal.
    public static readonly ColorScheme Success = new()
    {
        Normal   = Gfx.Attr(Color.BrightGreen, Color.Black),
        Focus    = Gfx.Attr(Color.BrightGreen, Color.Black),
        HotNormal = Gfx.Attr(Color.BrightGreen, Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightGreen, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,   Color.Black)
    };

    // ── Dialog palettes ───────────────────────────────────────────────

    // Black-background dialog — matches the game's terminal look.
    public static readonly ColorScheme Dialog = new()
    {
        Normal   = Gfx.Attr(Color.Gray,      Color.Black),
        Focus    = Gfx.Attr(Color.White,      Color.Black),
        HotNormal = Gfx.Attr(Color.Gray,      Color.Black),
        HotFocus  = Gfx.Attr(Color.White,     Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,  Color.Black)
    };

    // ── Menu-specific palettes ───────────────────────────────────────

    // Menu frame border — warm yellow.
    public static readonly ColorScheme MenuFrame = new()
    {
        Normal   = Gfx.Attr(Color.Yellow, Color.Black),
        Focus    = Gfx.Attr(Color.Yellow, Color.Black),
        HotNormal = Gfx.Attr(Color.Yellow, Color.Black),
        HotFocus  = Gfx.Attr(Color.Yellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };

    // Menu button — gray idle, bright gold on focus.
    public static readonly ColorScheme MenuButton = new()
    {
        Normal   = Gfx.Attr(Color.Gray,         Color.Black),
        Focus    = Gfx.Attr(Color.BrightYellow, Color.Black),
        HotNormal = Gfx.Attr(Color.Gray,         Color.Black),
        HotFocus  = Gfx.Attr(Color.BrightYellow, Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray,    Color.Black)
    };

    // ── Selection palettes ────────────────────────────────────────────

    // Tier / radio selection: white idle, black-on-gold when focused
    // so the highlighted option is always clearly visible.
    public static readonly ColorScheme TierRadio = new()
    {
        Normal    = Gfx.Attr(Color.White,        Color.Black),
        Focus     = Gfx.Attr(Color.Black,        Color.BrightYellow),
        HotNormal = Gfx.Attr(Color.White,        Color.Black),
        HotFocus  = Gfx.Attr(Color.Black,        Color.BrightYellow),
        Disabled  = Gfx.Attr(Color.DarkGray,     Color.Black),
    };

    // ── Dynamic palette helpers ─────────────────────────────────────

    // Creates a non-interactive scheme with the given foreground on black.
    // Useful for one-off labels where a predefined scheme doesn't exist.
    public static ColorScheme FromColor(Color fg) => new()
    {
        Normal    = Gfx.Attr(fg,             Color.Black),
        Focus     = Gfx.Attr(fg,             Color.Black),
        HotNormal = Gfx.Attr(fg,             Color.Black),
        HotFocus  = Gfx.Attr(fg,             Color.Black),
        Disabled  = Gfx.Attr(Color.DarkGray, Color.Black)
    };
}
