using Terminal.Gui;
using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

// Bundle 11 — eighth-block stat bars (HP/MP/SP). 8x sub-cell resolution via
// `█▉▊▋▌▍▎▏`. Falls back to ASCII `[████-----]` style when
// UserSettings.UseAsciiStatBars is true (some monospace fonts misrender the
// eighth-block range; toggle is per-user, not per-save).
public static class StatBarHelper
{
    // Trailing-edge eighths in increasing fill order. Index 0 = empty cell,
    // index 8 = full block. The smaller-block glyphs grow leftward visually.
    private static readonly char[] EighthBlocks =
    {
        ' ',   // 0/8
        '▏', // ▏ 1/8
        '▎', // ▎ 2/8
        '▍', // ▍ 3/8
        '▌', // ▌ 4/8
        '▋', // ▋ 5/8
        '▊', // ▊ 6/8
        '▉', // ▉ 7/8
        '█', // █ 8/8
    };

    // Build a stat bar of the given pixel-cell width. fraction = current/max.
    // Returns the bracketed string (no leading label). Caller applies color.
    public static string Build(int current, int max, int width)
    {
        if (width <= 0) return "[]";
        if (max <= 0) return "[" + new string(' ', width) + "]";

        if (UserSettings.Current.UseAsciiStatBars)
            return BuildAscii(current, max, width);

        double fraction = Math.Clamp((double)current / max, 0.0, 1.0);
        // 8 sub-units per cell → total eighths across the bar.
        int totalEighths = (int)Math.Round(fraction * width * 8);
        int fullCells = totalEighths / 8;
        int remainderEighths = totalEighths % 8;

        // Edge case: a sliver of HP must still show ▏ (not vanish to empty).
        if (current > 0 && fullCells == 0 && remainderEighths == 0)
            remainderEighths = 1;

        var bar = new char[width];
        for (int i = 0; i < width; i++)
        {
            if (i < fullCells) bar[i] = EighthBlocks[8];
            else if (i == fullCells && remainderEighths > 0)
                bar[i] = EighthBlocks[remainderEighths];
            else bar[i] = ' ';
        }
        return "[" + new string(bar) + "]";
    }

    // ASCII fallback — '#' fill + '-' empty. Pure ASCII so terminals lacking
    // eighth-block / full-block code points still render correctly.
    private static string BuildAscii(int current, int max, int width)
    {
        int filled = (int)Math.Round(Math.Clamp((double)current / max, 0.0, 1.0) * width);
        return "[" + new string('#', filled) + new string('-', width - filled) + "]";
    }

    // Color zone for stat bars. >=50% green, 25-49% yellow, <25% red.
    // Returns BrightGreen even at 100% (the full-tier safe band).
    public static Color ZoneColor(int current, int max)
    {
        if (max <= 0) return Color.DarkGray;
        double pct = Math.Clamp((double)current / max, 0.0, 1.0);
        if (pct >= 0.5) return Color.BrightGreen;
        if (pct >= 0.25) return Color.BrightYellow;
        return Color.BrightRed;
    }
}
