using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Shared construction helpers for pre-game and dialog screens:
// top-level title + rule, "── Section ──" labels, and right-aligned
// form labels. Keeps the visual vocabulary consistent across screens.
public static class ScreenHeader
{
    // Top-of-screen title label plus a thin dim rule underneath.
    public static (Label Title, Label Rule) Create(string title, int y = 2, int ruleWidth = 0)
    {
        int width = ruleWidth > 0 ? ruleWidth : title.Length + 4;
        var titleLabel = new Label
        {
            Text = title, X = Pos.Center(), Y = y,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title,
        };
        var ruleLabel = new Label
        {
            Text = new string('─', width), X = Pos.Center(), Y = y + 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim,
        };
        return (titleLabel, ruleLabel);
    }

    // "[ Foo ]" centered gold label used as a sub-section divider.
    public static Label Section(string text, int y) => new()
    {
        Text = $"[ {text} ]",
        X = Pos.Center(), Y = y,
        Width = Dim.Auto(), Height = 1,
        ColorScheme = ColorSchemes.Gold,
    };

    // Right-aligned form label used for label/field rows. Caller supplies
    // the X position so the form can live inside a centered virtual column.
    public static Label FormLabel(string text, Pos x, int y, int width) => new()
    {
        Text = text,
        X = x, Y = y,
        Width = width, Height = 1,
        TextAlignment = Alignment.End,
        ColorScheme = ColorSchemes.Body,
    };
}
