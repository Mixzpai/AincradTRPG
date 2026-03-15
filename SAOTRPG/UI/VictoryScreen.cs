using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Victory screen — displayed when the player clears Floor 100 of Aincrad.
///
/// Shows a triumphant banner, the player's final title and identity,
/// a boxed run summary with key stats, and a return-to-title button.
///
/// Layout (vertical, centered):
///   ╔══ AINCRAD CLEARED ══╗  ← Gold banner
///   Player Name             ← White identity line
///   ┌─ Final Summary ─┐    ← Stats card
///   [ Return ]              ← Button
/// </summary>
public static class VictoryScreen
{
    // ── Layout constants ────────────────────────────────────────────
    private const int BannerY     = 2;
    private const int NameY       = 14;
    private const int SummaryY    = 16;
    private const int ButtonY     = 29;

    // ── Victory banner art ──────────────────────────────────────────
    private const string VictoryArt = @"
    ╔══════════════════════════════════════╗
    ║                                      ║
    ║    A I N C R A D   C L E A R E D     ║
    ║                                      ║
    ║      You did it. All 100 floors.     ║
    ║    The death game is finally over.   ║
    ║       You are free.                  ║
    ║                                      ║
    ╚══════════════════════════════════════╝";

    public static void Show(Window mainWindow, Player player,
        int kills, int turns)
    {
        mainWindow.RemoveAll();

        // ── Victory banner (gold/yellow) ──────────────────────────────
        var bannerLabel = new Label
        {
            Text = VictoryArt,
            X = Pos.Center(), Y = BannerY,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Title
        };

        // ── Player identity line ──────────────────────────────────────
        string identity = $"{player.FirstName} {player.LastName}  —  Level {player.Level} {player.Title}";
        var nameLabel = new Label
        {
            Text = identity,
            X = Pos.Center(), Y = NameY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title
        };

        // ── Final summary card ────────────────────────────────────────
        var summaryLabel = new Label
        {
            Text = BuildSummary(player, kills, turns),
            X = Pos.Center(), Y = SummaryY,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Body
        };

        // ── Return button ─────────────────────────────────────────────
        var returnBtn = new Button
        {
            Text = " Return to Title ",
            X = Pos.Center(), Y = ButtonY,
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };
        returnBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            TitleScreen.Show(mainWindow);
        };
        var hint = new Label
        {
            Text = "[ Press Enter to continue ]",
            X = Pos.Center(), Y = ButtonY + 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        mainWindow.Add(bannerLabel, nameLabel, summaryLabel, returnBtn, hint);
        returnBtn.SetFocus();
    }

    private static string BuildSummary(Player player, int kills, int turns)
    {
        int itemCount = player.Inventory.Items.Count;

        return
            "  ┌──────────────────────────────────┐\n" +
            "  │      ── Final Summary ──          │\n" +
            "  │                                   │\n" +
            StatRow("Floors Cleared", 100) +
            StatRow("Enemies Slain",  kills) +
            StatRow("Turns Taken",    turns) +
            StatRow("Col Earned",     player.ColOnHand) +
            StatRow("Items Held",     itemCount) +
            StatRow("Final Level",    player.Level) +
            "  │                                   │\n" +
            $"  │  {"Rating:",-17}{"S+ (Liberator!)",5}  │\n" +
            "  │                                   │\n" +
            "  └──────────────────────────────────┘";
    }

    private static string StatRow(string label, int value) =>
        $"  │  {label + ":",-17}{value,5}            │\n";
}
