namespace SAOTRPG.Systems;

// Date → seasonal event: field boss spawns (Nicholas/Christmas canon LN Vol 2).
// Other events stubbed — see DEFERRED.md.
public enum SeasonalEvent
{
    None,
    NewYear,     // Jan 1–3
    Valentine,   // Feb 10–17
    WhiteDay,    // Mar 14
    Tanabata,    // Jul 7
    Summer,      // Jul 15 – Aug 31
    Halloween,   // Oct 20 – Nov 3
    Tsukimi,     // ~Sep 15–18
    Christmas,   // Dec 20–26
}

public static class SeasonalEvents
{
    // Returns the currently-active seasonal event based on real-world date.
    // Called at floor-enter and game-load — not every frame.
    public static SeasonalEvent GetActive(DateTime? now = null)
    {
        var d = now ?? DateTime.Now;
        int m = d.Month, day = d.Day;

        if (m == 12 && day >= 20 && day <= 26) return SeasonalEvent.Christmas;
        if (m == 1 && day >= 1 && day <= 3)    return SeasonalEvent.NewYear;
        if (m == 2 && day >= 10 && day <= 17)  return SeasonalEvent.Valentine;
        if (m == 3 && day == 14)               return SeasonalEvent.WhiteDay;
        if (m == 7 && day == 7)                return SeasonalEvent.Tanabata;
        if ((m == 7 && day >= 15) || m == 8)   return SeasonalEvent.Summer;
        if (m == 9 && day >= 15 && day <= 18)  return SeasonalEvent.Tsukimi;
        if ((m == 10 && day >= 20) || (m == 11 && day <= 3)) return SeasonalEvent.Halloween;
        return SeasonalEvent.None;
    }

    public static string DisplayName(SeasonalEvent e) => e switch
    {
        SeasonalEvent.Christmas => "Christmas Eve",
        SeasonalEvent.NewYear   => "New Year",
        SeasonalEvent.Valentine => "Valentine's Day",
        SeasonalEvent.WhiteDay  => "White Day",
        SeasonalEvent.Tanabata  => "Tanabata Star Festival",
        SeasonalEvent.Summer    => "Summer Festival",
        SeasonalEvent.Tsukimi   => "Tsukimi Moon-Viewing",
        SeasonalEvent.Halloween => "Halloween",
        _                        => "",
    };
}
