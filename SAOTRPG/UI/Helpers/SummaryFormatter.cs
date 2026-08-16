namespace SAOTRPG.UI.Helpers;

// Shared boxed run-summary card formatters (DeathScreen + VictoryScreen).
public static class SummaryFormatter
{
    // Columns between the two box edges. Every row of the card — borders, blanks, stat rows and
    // the proficiency block in ProficiencyHelper — has to agree on this or the card renders with
    // ragged edges. It previously did: rows shipped at 38, 39, 40 and 41 columns in the same box.
    public const int BoxInner = 36;

    private const int LabelWidth = 17;
    private const int ValueWidth = 5;

    // Row terminator. Exposed so the last row of a card can trim it without the call site
    // spelling out an escape.
    public const char RowBreak = '\n';

    // "  +------------------------------------+"
    public static string Border() => "  +" + new string('-', BoxInner) + "+\n";

    // "  |                                    |"
    public static string BlankRow() => "  |" + new string(' ', BoxInner) + "|\n";

    // Formats a single stat row inside a box-drawing card.
    // Output: "  |  Label:          12345            |\n"
    public static string StatRow(string label, int value) =>
        StatRow(label, value.ToString());

    // Formats a stat row with a string value (difficulty, pace, top weapon, rating…).
    //
    // The value is right-aligned in a narrow column so short values line up with the numeric
    // rows, then the row is padded — or clipped — to the box width. It used to be interpolated
    // with a fixed trailing run of spaces, so any value longer than the 5-column field pushed
    // the right edge out: "Normal" widened the row by one column, "Katana (12 kills)" by twelve.
    public static string StatRow(string label, string value)
    {
        string cell = $"  {label + ":",-LabelWidth}{value,ValueWidth}";
        if (cell.Length > BoxInner) cell = cell[..BoxInner];
        return "  |" + cell.PadRight(BoxInner) + "|\n";
    }

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
