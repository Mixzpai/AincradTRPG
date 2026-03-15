using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Game over screen — displayed when the player's HP reaches zero.
///
/// Shows a dramatic "YOU DIED" banner, the player's identity line,
/// a boxed run summary with key stats, and a return-to-title button.
///
/// Layout (vertical, centered):
///   ╔══ YOU DIED ══╗   ← Danger red banner
///   Player Name     ← White identity line
///   ┌─ Run Summary ─┐ ← Gray stats card
///   [ Return ]       ← Button
/// </summary>
public static class DeathScreen
{
    // ── Layout constants ────────────────────────────────────────────
    private const int BannerY     = 2;      // Top edge of death banner
    private const int NameY       = 13;     // Player identity line
    private const int SummaryY    = 15;     // Run summary card
    private const int ButtonY     = 28;     // Return button

    // ── Death banner art ────────────────────────────────────────────
    private const string DeathArt = @"
    ╔══════════════════════════════════════╗
    ║                                      ║
    ║          Y O U   D I E D             ║
    ║                                      ║
    ║      Your journey through Aincrad    ║
    ║         has come to an end.          ║
    ║                                      ║
    ╚══════════════════════════════════════╝";

    public static void Show(Window mainWindow, Player player, int floor = 1,
        int kills = 0, int turns = 0, string? killedBy = null, bool hardcore = false,
        TurnManager? turnManager = null, ColoredLogView? logView = null)
    {
        mainWindow.RemoveAll();

        // ── Death banner (bright red) ───────────────────────────────
        var deathLabel = new Label
        {
            Text = DeathArt,
            X = Pos.Center(), Y = BannerY,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Danger
        };

        // ── Hardcore banner ────────────────────────────────────────
        if (hardcore)
        {
            var hcLabel = new Label
            {
                Text = "[ HARDCORE ]  This death is permanent.  There are no second chances.",
                X = Pos.Center(), Y = BannerY - 1,
                Width = Dim.Auto(), Height = 1,
                ColorScheme = ColorSchemes.Danger
            };
            mainWindow.Add(hcLabel);
        }

        // ── Player identity line ────────────────────────────────────
        // ── Cause of death ──────────────────────────────────────────
        string identity = $"{player.FirstName} {player.LastName}  —  Level {player.Level} {player.Title}";
        if (killedBy != null)
            identity += $"\nSlain by {killedBy}";

        var nameLabel = new Label
        {
            Text = identity,
            X = Pos.Center(), Y = NameY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title
        };

        // ── Run summary card ────────────────────────────────────────
        var summaryLabel = new Label
        {
            Text = BuildSummary(player, floor, kills, turns, turnManager),
            X = Pos.Center(), Y = SummaryY,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Body
        };

        // ── Death recap — last 5 combat log entries ──────────────────
        if (logView != null)
        {
            var recentLines = logView.GetRecentEntries(5);
            if (recentLines.Count > 0)
            {
                string recap = "  ┌── Last Moments ──────────────────────┐\n";
                foreach (var line in recentLines)
                {
                    string trimmed = line.Length > 37 ? line[..37] : line;
                    recap += $"  │  {trimmed,-37} │\n";
                }
                recap += "  └─────────────────────────────────────┘";

                var recapLabel = new Label
                {
                    Text = recap,
                    X = Pos.Center(), Y = ButtonY - 4,
                    Width = Dim.Auto(), Height = Dim.Auto(),
                    ColorScheme = ColorSchemes.Dim
                };
                mainWindow.Add(recapLabel);
            }
        }

        // ── Return button or quit message ──────────────────────────
        if (hardcore)
        {
            // Hardcore: no retry — quit button only
            var quitBtn = new Button
            {
                Text = " Quit Game ",
                X = Pos.Center(), Y = ButtonY,
                IsDefault = true,
                ColorScheme = ColorSchemes.Button
            };
            quitBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                Application.RequestStop();
            };
            var quitHint = new Label
            {
                Text = "[ Hardcore mode — no retries ]",
                X = Pos.Center(), Y = ButtonY + 2,
                Width = Dim.Auto(), Height = 1,
                ColorScheme = ColorSchemes.Dim
            };
            mainWindow.Add(deathLabel, nameLabel, summaryLabel, quitBtn, quitHint);
            quitBtn.SetFocus();
        }
        else
        {
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
            mainWindow.Add(deathLabel, nameLabel, summaryLabel, returnBtn, hint);
            returnBtn.SetFocus();
        }
    }

    // ── Summary builder ─────────────────────────────────────────────

    /// <summary>
    /// Builds the boxed run summary string with aligned stat rows.
    /// Easy to extend — just add another StatRow() line.
    /// </summary>
    private static string BuildSummary(Player player, int floor, int kills, int turns,
        TurnManager? turnManager = null)
    {
        int itemCount = player.Inventory.Items.Count;
        string grade = RateRun(floor, kills, turns);

        // Difficulty label
        string diff = "";
        if (turnManager != null)
        {
            string[] diffNames = { "Story", "Very Easy", "Easy", "Normal", "Hard", "Very Hard", "Masochist", "Unwinnable", "Debug" };
            diff = turnManager.Difficulty >= 0 && turnManager.Difficulty < diffNames.Length
                ? diffNames[turnManager.Difficulty] : "?";
        }

        string summary =
            "  ┌──────────────────────────────────┐\n" +
            "  │        ── Run Summary ──          │\n" +
            "  │                                   │\n" +
            StatRow("Floor Reached", floor) +
            StatRow("Enemies Slain", kills) +
            StatRow("Turns Taken",   turns) +
            StatRow("Col Earned",    player.ColOnHand) +
            StatRow("Items Held",    itemCount) +
            StatRow("Level",         player.Level);

        if (diff.Length > 0)
            summary += $"  │  {"Difficulty:",-17}{diff,5}            │\n";

        summary +=
            "  │                                   │\n" +
            $"  │  {"Rating:",-17}{grade,5}            │\n";

        // Weapon proficiency section
        if (turnManager != null && turnManager.WeaponKills.Count > 0)
        {
            summary += "  │                                   │\n" +
                       "  │     ── Weapon Proficiency ──       │\n";
            foreach (var (wpnType, _) in turnManager.WeaponKills)
            {
                var info = turnManager.GetProficiencyInfo(wpnType);
                int bonus = turnManager.GetProficiencyBonus(wpnType);
                summary += $"  │  {wpnType,-16} {info.Rank,-10} +{bonus,2}  │\n";
            }
        }

        summary +=
            "  │                                   │\n" +
            "  └──────────────────────────────────┘";
        return summary;
    }

    // ── Run rating thresholds ─────────────────────────────────────
    // Add new grades by adding a (int minScore, string grade, string comment) tuple
    private static readonly (int MinScore, string Grade, string Comment)[] RunGrades =
    {
        (200, "S", "Legendary!"),
        (120, "A", "Excellent"),
        (60,  "B", "Good run"),
        (30,  "C", "Average"),
        (0,   "D", "Keep trying"),
    };

    /// <summary>Calculates a run rating based on floor depth, kill count, and efficiency.</summary>
    private static string RateRun(int floor, int kills, int turns)
    {
        // Score: floors worth most, kills second, efficiency bonus for fewer turns
        int score = (floor * 20) + (kills * 3) + Math.Max(0, 50 - turns / 10);

        foreach (var (minScore, grade, comment) in RunGrades)
        {
            if (score >= minScore)
                return $"{grade} ({comment})";
        }
        return "D (Keep trying)";
    }

    /// <summary>Formats a single stat row inside the summary card box.</summary>
    private static string StatRow(string label, int value) =>
        $"  │  {label + ":",-17}{value,5}            │\n";
}
