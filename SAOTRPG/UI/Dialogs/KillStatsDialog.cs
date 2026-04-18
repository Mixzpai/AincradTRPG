using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Run statistics dialog — shows kill count, weapon proficiency, floor
// progress, difficulty, play time, and run grade. Accessible via K key.
public static class KillStatsDialog
{
    private const int DialogWidth  = 56;
    private const int DialogHeight = 28;

    public static void Show(Player player, TurnManager turnManager)
    {
        var dialog = DialogHelper.Create("Run Statistics", DialogWidth, DialogHeight);
        int y = 0;

        // Player identity
        dialog.Add(new Label
        {
            Text = $"{player.FirstName} {player.LastName}  —  Lv.{player.Level}",
            X = Pos.Center(), Y = y++, ColorScheme = ColorSchemes.Title,
        });
        dialog.Add(new Label
        {
            Text = player.Title, X = Pos.Center(), Y = y++, ColorScheme = ColorSchemes.Dim,
        });
        y++;

        // ── Run Summary ──
        dialog.Add(ScreenHeader.Section("Run Summary", y++));
        dialog.Add(MakeStat("Floor", $"{turnManager.CurrentFloor}", y++));
        dialog.Add(MakeStat("Kills", $"{turnManager.KillCount}", y++));
        dialog.Add(MakeStat("Turns", $"{turnManager.TurnCount}", y++));
        dialog.Add(MakeStat("Col",   $"{player.ColOnHand}", y++));
        dialog.Add(MakeStat("Items", $"{player.Inventory.Items.Count}", y++));

        // Difficulty
        var tier = DifficultyData.Get(turnManager.Difficulty);
        dialog.Add(new Label
        {
            Text = $"  Diff     {tier.Name}",
            X = 1, Y = y++, ColorScheme = ColorSchemes.FromColor(tier.ThemeColor),
        });

        // Play time
        var elapsed = turnManager.TotalPlayTime;
        string timeStr = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m"
            : $"{elapsed.Minutes}m {elapsed.Seconds:D2}s";
        dialog.Add(MakeStat("Time", timeStr, y++));

        // Grade
        string grade = RunGradeHelper.Rate(turnManager.CurrentFloor, turnManager.KillCount, turnManager.TurnCount);
        dialog.Add(new Label
        {
            Text = $"  Rating   {grade}", X = 1, Y = y++, ColorScheme = ColorSchemes.Gold,
        });

        // Day/night phase
        dialog.Add(MakeStat("Phase", $"{SAOTRPG.Map.DayNightCycle.PhaseName}", y++));
        y++;

        // ── Weapon Proficiency ──
        dialog.Add(ScreenHeader.Section("Weapon Proficiency", y++));
        if (turnManager.WeaponKills.Count > 0)
        {
            foreach (var (wpnType, _) in turnManager.WeaponKills)
            {
                if (y >= DialogHeight - 3) break;
                dialog.Add(new Label
                {
                    Text = ProficiencyHelper.BuildDetailLine(turnManager, wpnType),
                    X = 1, Y = y++,
                });
            }
        }
        else
        {
            dialog.Add(new Label
            {
                Text = "  No weapon kills yet.", X = 1, Y = y++, ColorScheme = ColorSchemes.Dim,
            });
        }

        var hintLabel = new Label
        {
            Text = "Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };
        dialog.Add(hintLabel);

        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }

    // Right-aligned stat value for a clean column look.
    private static Label MakeStat(string label, string value, int y) => new()
    {
        Text = $"  {label,-8} {value}", X = 1, Y = y,
    };
}
