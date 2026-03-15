using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

/// <summary>
/// Centralized color scheme definitions used across all UI screens.
/// Keeps visual consistency and makes palette changes easy.
/// </summary>
public static class ColorSchemes
{
    // ── Core UI palettes ─────────────────────────────────────────────

    /// <summary>Standard button style: gray text, white-on-dark when focused.</summary>
    public static readonly ColorScheme Button = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray,         Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.White,        Color.DarkGray),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray,        Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.White,       Color.DarkGray),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray,    Color.Black)
    };

    /// <summary>Bright gold accent for highlights, dividers, titles.</summary>
    public static readonly ColorScheme Gold = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray,    Color.Black)
    };

    /// <summary>Muted gray for secondary text, footers, dim labels.</summary>
    public static readonly ColorScheme Dim = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    /// <summary>Standard body text — gray on black.</summary>
    public static readonly ColorScheme Body = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    /// <summary>Bright white for emphasis — titles, important labels.</summary>
    public static readonly ColorScheme Title = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.White, Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.White, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.White, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    /// <summary>Danger/death red for alerts.</summary>
    public static readonly ColorScheme Danger = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    // ── Dialog palettes ───────────────────────────────────────────────

    /// <summary>Black-background dialog — matches the game's terminal look.</summary>
    public static readonly ColorScheme Dialog = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray,      Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.White,      Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray,      Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.White,     Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray,  Color.Black)
    };

    // ── Menu-specific palettes ───────────────────────────────────────

    /// <summary>Menu frame border — warm yellow.</summary>
    public static readonly ColorScheme MenuFrame = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    /// <summary>Menu button — gray idle, bright gold on focus.</summary>
    public static readonly ColorScheme MenuButton = new()
    {
        Normal   = new Terminal.Gui.Attribute(Color.Gray,         Color.Black),
        Focus    = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray,         Color.Black),
        HotFocus  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Disabled  = new Terminal.Gui.Attribute(Color.DarkGray,    Color.Black)
    };
}
