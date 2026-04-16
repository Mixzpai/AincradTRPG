namespace SAOTRPG.UI.Helpers;

// Shared formatting helpers for boxed run summary cards.
// Used by both SAOTRPG.UI.DeathScreen and SAOTRPG.UI.VictoryScreen
// to keep stat row formatting consistent.
public static class SummaryFormatter
{
    // ── Box dimensions ────────────────────────────────────────────────
    // Inner box width = 37 chars (including left/right padding).
    // Label column = 17 chars left-aligned, value column = 5 chars right-aligned.
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

    // Formats a play time TimeSpan as a compact string.
    // Examples: "5h 03m", "12m", "&lt;1 min".
    // Shared between VictoryScreen and SaveSlotDialog.
    public static string FormatPlayTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes:D2}m";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}m";
        return "<1 min";
    }
}
