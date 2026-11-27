namespace SAOTRPG.UI.Helpers;

// How the game screen's sidebar splits its height between its three cards: MAP (the minimap and
// the rows under it), STATUS (the stats panel and the status icons) and LOG. Pure, so every
// height can be evaluated without a terminal.
//
// Spare height goes to the minimap first, up to seven overmap rows, then to the log, which
// hands LogShareToMinimap of its rows back to the minimap. On a short screen the stats panel gives
// rows back down to its minimum, then the log down to LogFloorLines, before the minimap drops
// below MinimapMin. Every card stays on screen down to the 120x30 minimum terminal.
public static class SidebarLayout
{
    public const int CardChrome = 2;          // a card's top and bottom border
    public const int MinimapMin = 3, MinimapMax = 7;
    public const int LogMinLines = 6, LogFloorLines = 2;

    // The share of the log's rows that goes to the minimap once it has its seven: a tall screen
    // gets a taller overmap rather than a wall of log.
    public const float LogShareToMinimap = 0.15f;

    public readonly record struct Heights(int MapCard, int Minimap, int StatusCard, int Stats, int LogCard);

    // height: the sidebar's rows. statsWant / statsMin: the stats panel's full and smallest
    // heights. footerRows: rows under the minimap inside its card. iconRows: status icon rows.
    // statsFit: the rows the panel really paints at a height (its sections come in whole
    // chunks), so a row it cannot use goes to the minimap instead of sitting blank.
    public static Heights For(int height, int statsWant, int statsMin, int footerRows, int iconRows,
                              Func<int, int>? statsFit = null)
    {
        int mapFixed = CardChrome + footerRows, statusFixed = CardChrome + iconRows;
        int logCard = CardChrome + LogMinLines;
        int stats = Math.Max(statsMin, statsWant);
        int Spare() => height - mapFixed - statusFixed - stats - logCard;

        if (Spare() < MinimapMin) stats = Math.Max(statsMin, stats - (MinimapMin - Spare()));
        if (Spare() < MinimapMin)
            logCard = Math.Max(CardChrome + LogFloorLines, logCard - (MinimapMin - Spare()));
        if (statsFit != null) stats = Math.Max(statsMin, Math.Min(stats, statsFit(stats)));

        int minimap = Math.Clamp(Spare(), 1, MinimapMax);
        int mapCard = mapFixed + minimap, statusCard = statusFixed + stats;
        logCard = Math.Max(CardChrome + 1, height - mapCard - statusCard);
        if (minimap == MinimapMax)
        {
            int give = Math.Min((int)(logCard * LogShareToMinimap), logCard - (CardChrome + LogMinLines));
            if (give > 0) { minimap += give; mapCard += give; logCard -= give; }
        }
        return new Heights(mapCard, minimap, statusCard, stats, logCard);
    }
}
