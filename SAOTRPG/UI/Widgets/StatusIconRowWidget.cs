using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// Bundle 11 — compact status-effect icon row. Two visual rows: glyph row
// (per-icon colored) and countdown row (turns remaining beneath each glyph).
// Renders nothing when no statuses are active OR when sidebar width < 12 cols
// (caller should also fall back to existing text status in that case).
public class StatusIconRowWidget : View
{
    private readonly TurnManager _tm;
    private const int MinSidebarWidth = 12;

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

        // Layout: "[B][P][S]" on row 0, " 4  3  ∞ " under it on row 1.
        // 3 cells per icon: '[' glyph ']' = 3 chars + optional space.
        int col = 0;
        const int CellWidth = 4; // "[X] " spacing
        foreach (var icon in icons)
        {
            if (col + 3 > vp.Width) break;

            // Row 0: bracketed glyph in the icon's color.
            Driver!.SetAttribute(Gfx.Attr(Color.DarkGray, Color.Black));
            Move(col, 0);
            Driver!.AddRune(new System.Text.Rune('['));
            Driver!.SetAttribute(Gfx.Attr(icon.Color, Color.Black));
            Driver!.AddRune(new System.Text.Rune(icon.Glyph));
            Driver!.SetAttribute(Gfx.Attr(Color.DarkGray, Color.Black));
            Driver!.AddRune(new System.Text.Rune(']'));

            // Row 1: countdown number centered under the glyph (col + 1).
            // 0 = duration-less status, render as middle dot.
            if (vp.Height > 1)
            {
                string count = icon.Count > 0 ? icon.Count.ToString() : "·";
                if (count.Length > 3) count = "9+"; // safety clamp
                int dx = (3 - count.Length) / 2;
                Driver!.SetAttribute(Gfx.Attr(Color.Gray, Color.Black));
                Move(col + dx, 1);
                foreach (var ch in count)
                    Driver!.AddRune(new System.Text.Rune(ch));
            }

            col += CellWidth;
        }
        return true;
    }
}
