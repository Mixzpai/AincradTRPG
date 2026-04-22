using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Victory screen — shown on Floor 100 clear. Gold banner, identity line, boxed summary card, Return button.
public static class VictoryScreen
{
    // ── Layout constants ────────────────────────────────────────────
    private const int BannerY     = 2;   // banner art top
    private const int NameY       = 14;  // identity line
    private const int SummaryY    = 16;  // summary card top

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

    // Renders the victory screen — banner, player identity, final summary, and return button.
    public static void Show(Window mainWindow, Player player,
        int kills, int turns, TurnManager? turnManager = null)
    {
        mainWindow.RemoveAll();

        // ── Victory banner (gold/yellow) ──────────────────────────────
        var bannerLabel = new Label
        {
            Text = VictoryArt,
            X = Pos.Center(), Y = BannerY,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Gold,
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

        // ── Lifetime stats — persistent cross-run tracking ─────────
        string grade0 = RunGradeHelper.RateVictory(100, kills, turns);
        LifetimeStats.RecordRun(kills, 100, player.Level, grade0,
            turnManager?.TotalPlayTime ?? TimeSpan.Zero,
            turnManager?.TotalColEarned ?? player.ColOnHand, victory: true,
            playerName: $"{player.FirstName} {player.LastName}".Trim(),
            turnCount: turns);

        // ── Final summary card ────────────────────────────────────────
        string summaryText = BuildSummary(player, kills, turns, turnManager);
        var summaryLabel = new Label
        {
            Text = summaryText,
            X = Pos.Center(), Y = SummaryY,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Body
        };

        // ── Dynamic button Y based on summary height ─────────────────
        int summaryLines = summaryText.Split('\n').Length;
        int buttonY = SummaryY + summaryLines + 1;

        var returnBtn = new Button
        {
            Text = " Return to Title ",
            X = Pos.Center(), Y = buttonY,
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
            X = Pos.Center(), Y = buttonY + 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        mainWindow.Add(bannerLabel, nameLabel, summaryLabel, returnBtn, hint);
        returnBtn.SetFocus();
    }

    // Builds the boxed final summary card with stats, play time, difficulty, and victory rating.
    private static string BuildSummary(Player player, int kills, int turns,
        TurnManager? turnManager)
    {
        int itemCount = player.Inventory.Items.Count;

        // Play time (uses shared formatter for consistency with save slot display)
        string playTime = "";
        if (turnManager != null)
            playTime = SummaryFormatter.FormatPlayTime(turnManager.TotalPlayTime);

        // Difficulty
        string diff = "";
        if (turnManager != null)
            diff = DifficultyData.Get(turnManager.Difficulty).Name;

        // Dynamic victory rating — S minimum, S+ for efficient clears
        string grade = RunGradeHelper.RateVictory(100, kills, turns);

        string summary =
            "  +------------------------------------+\n" +
            "  |      [ Final Summary ]             |\n" +
            "  |                                    |\n" +
            StatRow("Floors Cleared", 100) +
            StatRow("Enemies Slain",  kills) +
            StatRow("Turns Taken",    turns) +
            StatRow("Col Earned",     player.ColOnHand) +
            StatRow("Items Held",     itemCount) +
            StatRow("Final Level",    player.Level);

        if (playTime.Length > 0)
            summary += SummaryFormatter.StatRow("Play Time", playTime);
        if (diff.Length > 0)
            summary += SummaryFormatter.StatRow("Difficulty", diff);

        // Floor par result (last floor)
        if (turnManager != null)
        {
            int par = TurnManager.GetFloorPar(100);
            int floorTurns = turnManager.FloorTurns;
            string parResult = floorTurns <= par ? "FAST" : "SLOW";
            summary += $"  |  {"Final Pace:",-17}{parResult,5} ({floorTurns,3}/{par,3})│\n";
        }

        summary +=
            "  |                                   |\n" +
            SummaryFormatter.StatRow("Rating", grade);

        // Weapon proficiency (parity with DeathScreen)
        if (turnManager != null)
            summary += ProficiencyHelper.BuildBoxRows(turnManager);

        summary +=
            "  |                                   |\n" +
            "  +------------------------------------+";
        return summary;
    }

    // Delegates to SummaryFormatter for consistent stat row formatting.
    private static string StatRow(string label, int value) =>
        SummaryFormatter.StatRow(label, value);
}
