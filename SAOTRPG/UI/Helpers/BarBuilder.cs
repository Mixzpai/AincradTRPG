namespace SAOTRPG.UI.Helpers;

// Unicode HUD progress bars (HP/XP/satiety). Gradient = █ + ▓/▒/░ trailing edge; flat = fill/empty.
public static class BarBuilder
{
    // ── Gradient trailing-edge cutoffs (block-element selection for the trailing cell) ──
    // ▓ ≥0.75, ▒ ≥0.50, ░ ≥0.25.
    private const double GradientHigh   = 0.75;
    private const double GradientMid    = 0.50;
    private const double GradientLow    = 0.25;

    // Flat bar: [||||........]. Simpler than BuildGradient when block elements aren't needed.
    public static string Build(int current, int max, int width = 16, char fillChar = '|', char emptyChar = '.')
    {
        if (max <= 0) return "[" + new string(emptyChar, width) + "]";

        int filled = (int)Math.Round((double)current / max * width);
        filled = Math.Clamp(filled, 0, width);

        return "[" + new string(fillChar, filled) + new string(emptyChar, width - filled) + "]";
    }

    // Gradient █▓▒░ bar; visual examples (w=10): 100%→[██████████], 50%→[█████·····], 5%→[░·········].
    public static string BuildGradient(int current, int max, int width = 16)
    {
        if (width <= 0) width = 16; // Guard against invalid width
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
                // If current > 0 with no full cells, always show '░' (alive = visible)
                if (current > 0 && fullCells == 0 && remainder < GradientLow)
                    bar[i] = '░';
                else
                    bar[i] = remainder switch
                    {
                        >= GradientHigh => '▓',   // Three-quarter block
                        >= GradientMid  => '▒',   // Half block
                        >= GradientLow  => '░',   // Quarter block
                        _               => '·'    // Empty
                    };
            }
            else
                bar[i] = '·';  // Empty — middle dot for cleaner look
        }

        return "[" + new string(bar) + "]";
    }

    // Renders an XP gradient bar. Convenience wrapper for the HUD action bar.
    // Default width is narrower (10) since XP is secondary info.
    public static string BuildXp(int current, int required, int width = 10)
        => BuildGradient(current, required, width);

    // Renders an HP gradient bar. Convenience wrapper for the HUD action bar.
    // Default width is wider (16) since HP is the primary survival gauge.
    public static string BuildHp(int current, int max, int width = 16)
        => BuildGradient(current, max, width);
}
