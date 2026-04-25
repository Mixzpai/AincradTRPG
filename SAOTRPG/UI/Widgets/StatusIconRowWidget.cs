using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// Bundle 13 (Item 9) — abbreviated status row.
// Two visual rows: abbrev row "[BLD] [PSN] [STN]" + countdown row "  3    5    1".
// Each cell width = bracket+abbrev+bracket+space; longest abbrev is "REGN"/"SHRN" (4 chars → 7-cell cell).
// Width fallback: when sidebar < 24 cells, render single-letter form (B/P/S/...) to avoid wrap.
public class StatusIconRowWidget : View
{
    private readonly TurnManager _tm;
    private const int MinSidebarWidth = 12;
    private const int CompactBelowWidth = 24;

    public StatusIconRowWidget(TurnManager tm)
    {
        _tm = tm;
        CanFocus = false;
        ColorScheme = ColorSchemes.Body;
    }

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        if (vp.Width < MinSidebarWidth || vp.Height <= 0) return true;

        // Blank both lines so stale glyphs from prior status sets don't smear.
        for (int row = 0; row < vp.Height; row++)
        {
            Driver!.SetAttribute(Gfx.Attr(Color.Black, Color.Black));
            Move(0, row);
            for (int c = 0; c < vp.Width; c++) Driver!.AddRune(new System.Text.Rune(' '));
        }

        var icons = StatusIconMap.Collect(_tm);
        if (icons.Count == 0) return true;

        bool compact = vp.Width < CompactBelowWidth;
        int col = 0;
        foreach (var icon in icons)
        {
            string label = compact ? icon.Abbrev.Substring(0, 1) : icon.Abbrev;
            int cellWidth = label.Length + 3; // '[' + label + ']' + ' '
            if (col + cellWidth - 1 > vp.Width) break;

            // Row 0: bracketed abbrev in the icon's color.
            Driver!.SetAttribute(Gfx.Attr(Color.DarkGray, Color.Black));
            Move(col, 0);
            Driver!.AddRune(new System.Text.Rune('['));
            Driver!.SetAttribute(Gfx.Attr(icon.Color, Color.Black));
            for (int i = 0; i < label.Length; i++)
                Driver!.AddRune(new System.Text.Rune(label[i]));
            Driver!.SetAttribute(Gfx.Attr(Color.DarkGray, Color.Black));
            Driver!.AddRune(new System.Text.Rune(']'));

            // Row 1: countdown, centered under the abbrev (label-width window).
            // 0 = duration-less, render as middle dot.
            if (vp.Height > 1)
            {
                string count = icon.Count > 0 ? icon.Count.ToString() : "·";
                if (count.Length > 3) count = "9+";
                int dx = Math.Max(0, (label.Length - count.Length) / 2) + 1; // +1 for the leading '['
                Driver!.SetAttribute(Gfx.Attr(Color.Gray, Color.Black));
                Move(col + dx, 1);
                foreach (var ch in count)
                    Driver!.AddRune(new System.Text.Rune(ch));
            }

            col += cellWidth;
        }
        return true;
    }
}
