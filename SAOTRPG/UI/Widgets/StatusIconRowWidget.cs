using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// Abbreviated status row.
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
        SchemeName = ColorSchemes.BodyName;
    }

    // Children repaint every cell they own, and the framework clear would otherwise blank
    // this widget during the window where it skips painting beneath a dialog.
    protected override bool OnClearingViewport() => true;

    // How many icons fit, leaving room for the "+N" overflow marker when they do not all fit.
    //
    // The draw loop used to `break` on the first icon too wide for the row, so a narrow sidebar
    // simply stopped drawing and said nothing — and the tray got much longer once timed buffs
    // took a row per stat and fatigue added its own. A player could be poisoned, bleeding and
    // exhausted with only the first two visible.
    //
    // Pure and internal so it can be checked directly: the failure mode is arithmetic, and
    // driving a real widget to observe it would need a live driver.
    public static int VisibleCount(IReadOnlyList<StatusIconMap.StatusIcon> icons,
                                     int width, bool compact)
    {
        static int CellWidth(StatusIconMap.StatusIcon icon, bool compact)
            => (compact ? 1 : icon.Abbrev.Length) + 3;

        int used = 0, fits = 0;
        foreach (var icon in icons)
        {
            int w = CellWidth(icon, compact);
            if (used + w - 1 > width) break;
            used += w;
            fits++;
        }
        if (fits >= icons.Count) return fits;

        // Give cells back until the marker has room. Dropping one icon can lengthen the marker
        // (9 -> 10), so the width is recomputed each time rather than assumed.
        while (fits > 0)
        {
            int usedByFits = 0;
            for (int i = 0; i < fits; i++) usedByFits += CellWidth(icons[i], compact);
            int markerWidth = $"+{icons.Count - fits}".Length + 1;
            if (usedByFits + markerWidth <= width) break;
            fits--;
        }
        return fits;
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        var vp = Viewport;
        if (vp.Width < MinSidebarWidth || vp.Height <= 0) return true;

        // Batched rather than the framework's SetAttribute/Move/AddRune trio, which re-parses
        // the grapheme and allocates per cell. Identical output: same glyphs, attributes and
        // columns — the explicit x cursor reproduces AddRune's implicit column advance.
        var batch = Gfx.Begin(this);
        var blank = Gfx.Attr(Color.Black, Color.Black);
        var bracket = Gfx.Attr(Color.DarkGray, Color.Black);

        // Blank both lines so stale glyphs from prior status sets don't smear.
        for (int row = 0; row < vp.Height; row++)
            for (int c = 0; c < vp.Width; c++) batch.Put(c, row, ' ', blank);

        var icons = StatusIconMap.Collect(_tm);
        if (icons.Count == 0) return true;

        bool compact = vp.Width < CompactBelowWidth;
        int col = 0;

        // OVERFLOW IS DISCLOSED, NOT SWALLOWED — see VisibleCount.
        int drawable = VisibleCount(icons, vp.Width, compact);

        for (int idx = 0; idx < drawable; idx++)
        {
            var icon = icons[idx];
            string label = compact ? icon.Abbrev.Substring(0, 1) : icon.Abbrev;
            int cellWidth = label.Length + 3; // '[' + label + ']' + ' '

            // Row 0: bracketed abbrev in the icon's color.
            var iconAttr = Gfx.Attr(icon.Color, Color.Black);
            int x = col;
            batch.Put(x++, 0, '[', bracket);
            for (int i = 0; i < label.Length; i++) batch.Put(x++, 0, label[i], iconAttr);
            batch.Put(x, 0, ']', bracket);

            // Row 1: countdown, centered under the abbrev (label-width window).
            // 0 = duration-less, render as middle dot.
            if (vp.Height > 1)
            {
                string count = icon.Count > 0 ? icon.Count.ToString() : "·";
                if (count.Length > 3) count = "9+";
                int dx = Math.Max(0, (label.Length - count.Length) / 2) + 1; // +1 for the leading '['
                var countAttr = Gfx.Attr(Color.Gray, Color.Black);
                for (int i = 0; i < count.Length; i++)
                    batch.Put(col + dx + i, 1, count[i], countAttr);
            }

            col += cellWidth;
        }

        // The marker itself, in the dim bracket colour so it reads as chrome rather than as
        // another status. Drawn on the abbreviation row; the count row stays blank under it.
        int hidden = icons.Count - drawable;
        if (hidden > 0)
        {
            string marker = $"+{hidden}";
            var moreAttr = Gfx.Attr(Color.Gray, Color.Black);
            for (int i = 0; i < marker.Length && col + i < vp.Width; i++)
                batch.Put(col + i, 0, marker[i], moreAttr);
        }
        return true;
    }
}
