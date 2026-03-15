namespace SAOTRPG.UI.Helpers;

/// <summary>
/// Builds Unicode progress bars for HUD display.
/// Used for HP, XP, satiety, and any future stat gauges.
///
/// Gradient mode uses █▓▒░ block elements for a smooth analog-meter feel:
///   Full cells = █, trailing edge = ▓/▒/░ based on fractional fill.
/// Flat mode uses simple fill/empty characters (legacy).
/// </summary>
public static class BarBuilder
{
    /// <summary>
    /// Renders a flat bar: [||||........]
    /// </summary>
    public static string Build(int current, int max, int width = 16, char fillChar = '|', char emptyChar = '.')
    {
        if (max <= 0) return "[" + new string(emptyChar, width) + "]";

        int filled = (int)Math.Round((double)current / max * width);
        filled = Math.Clamp(filled, 0, width);

        return "[" + new string(fillChar, filled) + new string(emptyChar, width - filled) + "]";
    }

    /// <summary>
    /// Renders a gradient bar using █▓▒░ block elements.
    /// The filled portion uses solid blocks, with a smooth trailing edge.
    /// Format: [████▓░░░░░]
    /// </summary>
    public static string BuildGradient(int current, int max, int width = 16)
    {
        if (max <= 0) return "[" + new string('░', width) + "]";

        // Calculate fill as a fraction (0.0 to 1.0) then map to sub-cell precision
        double fraction = Math.Clamp((double)current / max, 0.0, 1.0);
        double fillExact = fraction * width;
        int fullCells = (int)fillExact;
        double remainder = fillExact - fullCells;

        var bar = new char[width];

        for (int i = 0; i < width; i++)
        {
            if (i < fullCells)
                bar[i] = '█';  // Full block
            else if (i == fullCells && fullCells < width)
            {
                // Trailing edge — fractional fill using block elements
                bar[i] = remainder switch
                {
                    >= 0.75 => '▓',   // 75%+ filled
                    >= 0.50 => '▒',   // 50%+ filled
                    >= 0.25 => '░',   // 25%+ filled
                    _       => '░'    // Nearly empty but not zero
                };
                // If the bar is essentially empty at this cell, use empty glyph
                if (remainder < 0.05 && fullCells == 0 && current > 0)
                    bar[i] = '░';
                else if (remainder < 0.05)
                    bar[i] = '·';
            }
            else
                bar[i] = '·';  // Empty — middle dot for cleaner look
        }

        return "[" + new string(bar) + "]";
    }

    /// <summary>
    /// Renders an XP gradient bar.
    /// </summary>
    public static string BuildXp(int current, int required, int width = 10)
        => BuildGradient(current, required, width);

    /// <summary>
    /// Renders an HP gradient bar.
    /// </summary>
    public static string BuildHp(int current, int max, int width = 16)
        => BuildGradient(current, max, width);
}
