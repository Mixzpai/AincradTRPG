namespace SAOTRPG.UI.Helpers;

// Shared boxed run-summary card formatters (DeathScreen + VictoryScreen).
public static class SummaryFormatter
{
    // Inner box 37 cols; label 17 left, value 5 right.
    private const int LabelWidth = 17;
    private const int ValueWidth = 5;

    // Formats a single stat row inside a box-drawing card.
    // Output: "  |  Label:          12345            |\n"
    public static string StatRow(string label, int value) =>
        $"  |  {label + ":",-LabelWidth}{value,ValueWidth}            |\n";

    // Formats a stat row with a string value (for difficulty, pace, rating, etc.).
    // Output: "  |  Label:          value            |\n"
    public static string StatRow(string label, string value) =>
        $"  |  {label + ":",-LabelWidth}{value,ValueWidth}            |\n";

    // Compact TimeSpan → "5h 03m" / "12m" / "<1 min" (shared with SaveSlotDialog).
    public static string FormatPlayTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes:D2}m";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}m";
        return "<1 min";
    }
}
