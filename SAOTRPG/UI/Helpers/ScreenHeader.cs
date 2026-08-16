using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Shared pre-game/dialog construction helpers — title+rule, "── Section ──" labels, right-aligned form labels.
// Unifies visual vocabulary across screens.
public static class ScreenHeader
{
    // The screen's centre line, as an absolute column. Use this — not Pos.Center() — as the
    // origin for a form whose label and control columns have to line up with each other.
    //
    // Pos.Center() resolves to (parentWidth - thisView'sWidth) / 2, so it centres the view it
    // is applied to. That makes "Pos.Center() + k" a different column for every view width:
    // columns built from it drift apart, and a control wide enough will land on top of its own
    // label. Pos.Percent(50) ignores the view's width, so "Axis + k" is the same column for
    // every view on the screen.
    public static Pos Axis => Pos.Percent(50);

    // A filled-pip meter — the game's one way of showing a quantity at a glance, so difficulty
    // risk and stat allocation read as the same kind of thing.
    //
    // The distinction is carried by the GLYPH, not by colour: a meter usually lives inside a
    // label string that has a single scheme, so its filled cells cannot be tinted separately
    // from the text around them.
    public static string Meter(int filled, int total)
    {
        filled = Math.Clamp(filled, 0, total);
        return new string(PipFull, filled) + new string(PipEmpty, total - filled);
    }

    public const char PipFull  = '▰';
    public const char PipEmpty = '▱';

    // Top-of-screen title label plus a thin dim rule underneath.
    public static (Label Title, Label Rule) Create(string title, int y = 2, int ruleWidth = 0)
    {
        int width = ruleWidth > 0 ? ruleWidth : title.Length + 4;
        var titleLabel = new Label
        {
            Text = title, X = Pos.Center(), Y = y,
            Width = Dim.Auto(), Height = 1,
            SchemeName = ColorSchemes.TitleName,
        };
        var ruleLabel = new Label
        {
            Text = new string(Hairline, width), X = Pos.Center(), Y = y + 1,
            Width = Dim.Auto(), Height = 1,
            SchemeName = ColorSchemes.DimName,
        };
        return (titleLabel, ruleLabel);
    }

    // Rule glyph. U+2594 sits on the top edge of its cell; U+2500 sits on the centre line, so
    // on this game's 2:1 cells it renders as a mid-weight bar where this reads as a hairline.
    // One constant so every rule on every screen agrees — flip it here to change them all.
    public const char Hairline = '▔';

    // Section divider: an uppercase caption with a hairline running out to the right edge of
    // the content it introduces.
    //
    // Takes its origin and width from the caller so the caption aligns with the column it
    // heads — the screens are centred compositions, and a header anchored to the screen edge
    // would float free of its own content. Replaces the old centred "[ Foo ]", whose brackets
    // are ASCII-era ornament and whose centring meant it lined up with nothing.
    //
    // Two labels rather than one padded string, because the caption and the rule need
    // different attributes and a Label carries a single scheme.
    public static (Label Caption, Label Rule) Section(string text, Pos x, int y, int width)
    {
        string caption = text.ToUpperInvariant();
        var captionLabel = new Label
        {
            Text = caption, X = x, Y = y,
            Width = Dim.Auto(), Height = 1,
            SchemeName = ColorSchemes.TitleName,
        };
        var ruleLabel = new Label
        {
            Text = new string(Hairline, Math.Max(0, width - caption.Length - 1)),
            X = Pos.Right(captionLabel) + 1, Y = y,
            Width = Dim.Auto(), Height = 1,
            SchemeName = ColorSchemes.DimName,
        };
        return (captionLabel, ruleLabel);
    }

    // Key-hint footer. The key and its action get different attributes so the row scans as a
    // key map rather than as a sentence — keys in the accent, actions muted. Keys are
    // lowercase: a lowercase token reads as a keycap, a capitalised one reads as a word.
    //
    // Columns between one key/action pair and the next.
    private const int GapBetweenPairs = 4;

    // Rendered width of a hint row. Call this first when the row needs centring, since the
    // origin depends on the total width: KeyHints(Axis - KeyHintsWidth(h) / 2, y, h).
    public static int KeyHintsWidth(params (string Key, string Action)[] hints)
    {
        int width = 0;
        for (int i = 0; i < hints.Length; i++)
            width += (i == 0 ? 0 : GapBetweenPairs) + hints[i].Key.Length + 1 + hints[i].Action.Length;
        return width;
    }

    // Builds the row at the given origin. y is a Pos so a footer can anchor to the bottom.
    public static List<Label> KeyHints(Pos left, Pos y,
        params (string Key, string Action)[] hints)
    {
        var views = new List<Label>();
        View? previous = null;

        foreach (var (key, action) in hints)
        {
            int lead = previous is null ? 0 : GapBetweenPairs;
            var keyLabel = new Label
            {
                Text = key,
                X = previous is null ? left : Pos.Right(previous) + lead, Y = y,
                Width = Dim.Auto(), Height = 1,
                SchemeName = ColorSchemes.GoldName,
            };
            var actionLabel = new Label
            {
                Text = action,
                X = Pos.Right(keyLabel) + 1, Y = y,
                Width = Dim.Auto(), Height = 1,
                SchemeName = ColorSchemes.DimName,
            };
            views.Add(keyLabel);
            views.Add(actionLabel);
            previous = actionLabel;
        }

        return views;
    }

    // Right-aligned form label used for label/field rows. Caller supplies
    // the X position so the form can live inside a centered virtual column.
    public static Label FormLabel(string text, Pos x, int y, int width) => new()
    {
        Text = text,
        X = x, Y = y,
        Width = width, Height = 1,
        TextAlignment = Alignment.End,
        SchemeName = ColorSchemes.BodyName,
    };
}
