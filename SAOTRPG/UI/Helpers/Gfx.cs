using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Terminal.Gui rendering shim: Gfx.Attr/PutCell delegate to Driver (static) and View.Move (instance, view-relative).
// Upgrade-isolation point — a future Driver→DrawContext migration only touches this file.
public static class Gfx
{
    // Create a color attribute from foreground + background colors.
    public static Terminal.Gui.Attribute Attr(Color fg, Color bg)
        => new Terminal.Gui.Attribute(fg, bg);

    // Shorthand: fg on black background (most common case).
    public static Terminal.Gui.Attribute Attr(Color fg)
        => new Terminal.Gui.Attribute(fg, Color.Black);

    // Write a single character to the terminal at view-relative (x, y).
    public static void PutCell(View view, int x, int y, char ch, Color fg, Color bg)
    {
        View.Driver!.SetAttribute(Attr(fg, bg));
        view.Move(x, y);
        View.Driver!.AddRune(new System.Text.Rune(ch));
    }

    // Write a single character using a pre-built attribute.
    public static void PutCell(View view, int x, int y, char ch, Terminal.Gui.Attribute attr)
    {
        View.Driver!.SetAttribute(attr);
        view.Move(x, y);
        View.Driver!.AddRune(new System.Text.Rune(ch));
    }
}
