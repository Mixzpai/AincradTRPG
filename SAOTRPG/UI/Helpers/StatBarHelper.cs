using SAOTRPG.Systems.Persistence;

namespace SAOTRPG.UI.Helpers;

// Eighth-block stat bars (HP/MP/SP). 8x sub-cell resolution via `█▉▊▋▌▍▎▏`.
// Falls back to ASCII `[████-----]` style when UserSettings.UseAsciiStatBars
// is true (some monospace fonts misrender the eighth-block range; toggle is
// per-user, not per-save).
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

    // Writes a stat bar of the given cell width, bracketed, with no label: fraction = current/max.
    // The caller applies colour.
    public static void Write(ref LineWriter w, int current, int max, int width)
    {
        w.Append('[');
        if (width <= 0) { w.Append(']'); return; }
        if (max <= 0) { w.Append(' ', width); w.Append(']'); return; }

        if (UserSettings.Current.UseAsciiStatBars)
        {
            // ASCII fallback: '#' fill, '-' empty, for terminals without the block code points.
            int filled = (int)Math.Round(Math.Clamp((double)current / max, 0.0, 1.0) * width);
            w.Append('#', filled);
            w.Append('-', width - filled);
            w.Append(']');
            return;
        }

        double fraction = Math.Clamp((double)current / max, 0.0, 1.0);
        // 8 sub-units per cell → total eighths across the bar.
        int totalEighths = (int)Math.Round(fraction * width * 8);
        int fullCells = totalEighths / 8;
        int remainderEighths = totalEighths % 8;

        // Edge case: a sliver of HP must still show ▏ (not vanish to empty).
        if (current > 0 && fullCells == 0 && remainderEighths == 0)
            remainderEighths = 1;

        for (int i = 0; i < width; i++)
        {
            if (i < fullCells) w.Append(EighthBlocks[8]);
            else if (i == fullCells && remainderEighths > 0) w.Append(EighthBlocks[remainderEighths]);
            else w.Append(' ');
        }
        w.Append(']');
    }

    // Color zone for stat bars. >=50% green, 25-49% yellow, <25% red.
    // Returns BrightGreen even at 100% (the full-tier safe band).
    public static Color ZoneColor(int current, int max)
    {
        // Reinforcement, not the only channel — the bar's fill level already carries the number.
        // Themed so a zone reads the same way as every other Ok/Warn/Crit in the game.
        if (max <= 0) return ColorSchemes.Theme.Muted;
        double pct = Math.Clamp((double)current / max, 0.0, 1.0);
        if (pct >= 0.5) return ColorSchemes.Theme.Ok;
        if (pct >= 0.25) return ColorSchemes.Theme.Warn;

        return ColorSchemes.Theme.Crit;
    }
}
