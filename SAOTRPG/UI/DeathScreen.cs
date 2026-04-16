using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Game over screen — displayed when the player's HP reaches zero.
public static class DeathScreen
{
    private const int BannerY = 2, FlavorY = 11, NameY = 13, SummaryY = 15;
    private const int RecapBoxW = 49, RecapTrimW = 45, RecapCount = 5;
    private static EventHandler<Key>? _keyHandler;

    private const string DeathArt = @"
    ╔══════════════════════════════════════╗
    ║                                      ║
    ║          Y O U   D I E D             ║
    ║                                      ║
    ║      Your journey through Aincrad    ║
    ║         has come to an end.          ║
    ║                                      ║
    ╚══════════════════════════════════════╝";

    // Renders the death screen with banner, summary, recap, and return button.
    public static void Show(Window mainWindow, Player player, int floor = 1,
        int kills = 0, int turns = 0, string? killedBy = null, bool hardcore = false,
        TurnManager? turnManager = null, ColoredLogView? logView = null)
    {
        mainWindow.RemoveAll();
        if (_keyHandler != null) mainWindow.KeyDown -= _keyHandler;

        // Death penalty on Normal mode: lose 25% Col and 10% XP (never drop a level).
        int colLost = 0, xpLost = 0;
        if (!hardcore && player.ColOnHand > 0)
        {
            colLost = player.ColOnHand / 4;
            player.ColOnHand -= colLost;
        }
        if (!hardcore && player.CurrentExperience > 0)
        {
            xpLost = player.CurrentExperience / 10;
            player.CurrentExperience -= xpLost;
        }

        var deathLabel = new Label
        {
            Text = DeathArt, X = Pos.Center(), Y = BannerY,
            Width = Dim.Auto(), Height = Dim.Auto(), ColorScheme = ColorSchemes.Danger
        };

        if (hardcore)
        {
            mainWindow.Add(new Label
            {
                Text = "[ HARDCORE ]  This death is permanent.  There are no second chances.",
                X = Pos.Center(), Y = BannerY - 1,
                Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Danger
            });
        }
        else if (colLost > 0 || xpLost > 0)
        {
            string penalty = $"Death penalty: -{colLost} Col, -{xpLost} XP lost";
            mainWindow.Add(new Label
            {
                Text = penalty, X = Pos.Center(), Y = BannerY - 1,
                Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Danger
            });
        }

        string flavor = FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)];
        var flavorLabel = new Label
        {
            Text = $"\"{flavor}\"", X = Pos.Center(), Y = FlavorY,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
        };

        string identity = $"{player.FirstName} {player.LastName}  —  Level {player.Level} {player.Title}";
        if (killedBy != null) identity += $"\nSlain by {killedBy}";
        var nameLabel = new Label
        {
            Text = identity, X = Pos.Center(), Y = NameY,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Title
        };

        string grade = RunGradeHelper.Rate(floor, kills, turns);
        DeathRecapWriter.WriteRecap(player, floor, kills, turns, killedBy, grade, hardcore,
            turnManager?.TotalColEarned ?? player.ColOnHand, turnManager?.TotalPlayTime);

        LifetimeStats.RecordRun(kills, floor, player.Level, grade,
            turnManager?.TotalPlayTime ?? TimeSpan.Zero,
            turnManager?.TotalColEarned ?? player.ColOnHand, victory: false);

        string summaryText = BuildSummary(player, floor, kills, turns, turnManager);
        var summaryLabel = new Label
        {
            Text = summaryText, X = Pos.Center(), Y = SummaryY,
            Width = Dim.Auto(), Height = Dim.Auto(), ColorScheme = ColorSchemes.Body
        };

        int summaryLines = summaryText.Split('\n').Length;
        int recapStartY = SummaryY + summaryLines + 1;
        int buttonY = recapStartY;

        if (logView != null)
        {
            var recentLines = logView.GetRecentEntries(RecapCount);
            if (recentLines.Count > 0)
            {
                string border = new string('-', RecapBoxW - 4);
                const int headerPrefixLen = 19;
                string recap = $"  +[ Last Moments ]{border[headerPrefixLen..]}+\n";
                foreach (var line in recentLines)
                {
                    string trimmed = line.Length > RecapTrimW ? line[..RecapTrimW] : line;
                    recap += $"  |  {trimmed,-RecapTrimW} |\n";
                }
                recap += $"  +{border}+";

                mainWindow.Add(new Label
                {
                    Text = recap, X = Pos.Center(), Y = recapStartY,
                    Width = Dim.Auto(), Height = Dim.Auto(), ColorScheme = ColorSchemes.Dim
                });
                buttonY = recapStartY + recentLines.Count + 3;
            }
        }

        string tip = PickContextualTip(player, killedBy, floor, turns);
        mainWindow.Add(new Label
        {
            Text = $"Tip: {tip}", X = Pos.Center(), Y = buttonY,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
        });
        buttonY += 2;

        if (hardcore)
        {
            var quitBtn = new Button
            {
                Text = " Quit Game ", X = Pos.Center(), Y = buttonY,
                IsDefault = true, ColorScheme = ColorSchemes.Button
            };
            quitBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };
            var quitHint = new Label
            {
                Text = "[ Hardcore mode — no retries ]",
                X = Pos.Center(), Y = buttonY + 2,
                Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
            };
            mainWindow.Add(deathLabel, flavorLabel, nameLabel, summaryLabel, quitBtn, quitHint);
            quitBtn.SetFocus();
        }
        else
        {
            var returnBtn = new Button
            {
                Text = " Return to Title ", X = Pos.Center(), Y = buttonY,
                IsDefault = true, ColorScheme = ColorSchemes.Button
            };
            returnBtn.Accepting += (s, e) => { e.Cancel = true; TitleScreen.Show(mainWindow); };
            var hint = new Label
            {
                Text = "[ Enter — Title ]  [ R — Restart ]",
                X = Pos.Center(), Y = buttonY + 2,
                Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
            };

            _keyHandler = (s, e) =>
            {
                if (e.KeyCode == KeyCode.R) { e.Handled = true; DifficultyScreen.Show(mainWindow); }
            };
            mainWindow.KeyDown += _keyHandler;

            mainWindow.Add(deathLabel, flavorLabel, nameLabel, summaryLabel, returnBtn, hint);
            returnBtn.SetFocus();
        }
    }

    private static string BuildSummary(Player player, int floor, int kills, int turns,
        TurnManager? turnManager = null)
    {
        int itemCount = player.Inventory.Items.Count;
        string grade = RunGradeHelper.Rate(floor, kills, turns);
        string diff = turnManager != null ? DifficultyData.Get(turnManager.Difficulty).Name : "";

        string summary =
            "  +------------------------------------+\n" +
            "  |        [ Run Summary ]             |\n" +
            "  |                                    |\n" +
            StatRow("Floor Reached", floor) +
            StatRow("Enemies Slain", kills) +
            StatRow("Turns Taken", turns) +
            StatRow("Col Earned", turnManager?.TotalColEarned ?? player.ColOnHand) +
            StatRow("Col on Hand", player.ColOnHand) +
            StatRow("Items Held", itemCount) +
            StatRow("Level", player.Level);

        if (diff.Length > 0) summary += SummaryFormatter.StatRow("Difficulty", diff);

        if (turnManager != null)
        {
            int par = TurnManager.GetFloorPar(floor);
            int floorTurns = turnManager.FloorTurns;
            string parResult = floorTurns <= par ? "FAST" : "SLOW";
            summary += $"  |  {"Floor Pace:",-17}{parResult,5} ({floorTurns,3}/{par,3})│\n";
        }

        if (turnManager?.TopKill is var (topName, topCount))
            summary += SummaryFormatter.StatRow("Top Kill", $"{topName} x{topCount}");

        if (turnManager != null)
        {
            if (turnManager.KillStreak > 0)
                summary += SummaryFormatter.StatRow("Best Streak", $"{turnManager.KillStreak} kills");
            if (turnManager.DodgeStreak > 0)
                summary += SummaryFormatter.StatRow("Dodge Streak", $"{turnManager.DodgeStreak}");

            var topWpn = turnManager.WeaponKills.OrderByDescending(kv => kv.Value).FirstOrDefault();
            if (topWpn.Key != null)
                summary += SummaryFormatter.StatRow("Top Weapon", $"{topWpn.Key} ({topWpn.Value} kills)");
        }

        summary += "  |                                   |\n" + SummaryFormatter.StatRow("Rating", grade);

        if (turnManager != null)
        {
            var time = turnManager.TotalPlayTime;
            string timeStr = time.TotalHours >= 1
                ? $"{(int)time.TotalHours}h {time.Minutes:D2}m"
                : $"{time.Minutes}m {time.Seconds:D2}s";
            summary += SummaryFormatter.StatRow("Play Time", timeStr);
        }

        if (turnManager != null) summary += ProficiencyHelper.BuildBoxRows(turnManager);

        summary += "  |                                   |\n" + "  +------------------------------------+";
        return summary;
    }

    // Shorthand for summary card stat rows.
    private static string StatRow(string label, int value) =>
        SummaryFormatter.StatRow(label, value);

    private static string PickContextualTip(Player player, string? killedBy, int floor, int turns)
    {
        if (Random.Shared.Next(2) == 0)
            return FlavorText.DeathTips[Random.Shared.Next(FlavorText.DeathTips.Length)];

        var pool = new List<string>();

        if (killedBy != null)
        {
            if (killedBy.Contains("Elite") || killedBy.Contains("Champion"))
                pool.Add("Elite and Champion monsters hit much harder — engage cautiously or retreat.");
            if (killedBy.Contains("Boss"))
                pool.Add("Bosses enrage below 30% HP. Save potions for their final phase.");
        }

        if (floor <= 2) pool.Add("Early floors are dangerous at low level. Take your time and avoid risky fights.");
        if (player.Level <= 2 && floor >= 3) pool.Add("You might be under-leveled. Clear more enemies before ascending.");
        if (turns < 30) pool.Add("Rushing through floors is risky. Explore for items and XP before ascending.");

        var wpn = player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.Weapon);
        if (wpn == null) pool.Add("Fighting barehanded is tough. Equip a weapon from your inventory or buy one from a shop.");
        else if (wpn.ItemDurability <= 0) pool.Add("Your weapon was broken! Repair gear at campfires or buy replacements.");

        return pool.Count > 0 ? pool[Random.Shared.Next(pool.Count)]
            : FlavorText.DeathTips[Random.Shared.Next(FlavorText.DeathTips.Length)];
    }
}
