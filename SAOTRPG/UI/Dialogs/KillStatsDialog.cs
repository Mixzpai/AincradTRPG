using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Run statistics dialog — shows kill count, weapon proficiency, floor progress,
/// and player title. Accessible via K key.
/// </summary>
public static class KillStatsDialog
{
    // ── Layout constants ─────────────────────────────────────────────
    private const int DialogWidth  = 52;
    private const int DialogHeight = 24;

    public static void Show(Player player, TurnManager turnManager)
    {
        var dialog = new Dialog
        {
            Title = "Run Statistics",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        int y = 0;

        // ── Player identity ──────────────────────────────────────────
        dialog.Add(new Label
        {
            Text = $"{player.FirstName} {player.LastName}  —  Lv.{player.Level} {player.Title}",
            X = 1, Y = y++,
            ColorScheme = ColorSchemes.Title
        });
        y++;

        // ── Run summary ──────────────────────────────────────────────
        dialog.Add(new Label { Text = "── Run Summary ──", X = 1, Y = y++, ColorScheme = ColorSchemes.Gold });
        dialog.Add(new Label { Text = $"  Floor:   {turnManager.CurrentFloor}", X = 1, Y = y++ });
        dialog.Add(new Label { Text = $"  Kills:   {turnManager.KillCount}", X = 1, Y = y++ });
        dialog.Add(new Label { Text = $"  Turns:   {turnManager.TurnCount}", X = 1, Y = y++ });
        dialog.Add(new Label { Text = $"  Col:     {player.ColOnHand}", X = 1, Y = y++ });
        dialog.Add(new Label { Text = $"  Items:   {player.Inventory.Items.Count}", X = 1, Y = y++ });

        // Difficulty label
        string[] diffNames = { "Story", "Very Easy", "Easy", "Normal", "Hard", "Very Hard", "Masochist", "Unwinnable", "Debug" };
        string diff = turnManager.Difficulty >= 0 && turnManager.Difficulty < diffNames.Length
            ? diffNames[turnManager.Difficulty] : "?";
        dialog.Add(new Label { Text = $"  Diff:    {diff}{(turnManager.IsHardcore ? " [HC]" : "")}", X = 1, Y = y++ });
        y++;

        // ── Weapon proficiency ───────────────────────────────────────
        dialog.Add(new Label { Text = "── Weapon Proficiency ──", X = 1, Y = y++, ColorScheme = ColorSchemes.Gold });

        if (turnManager.WeaponKills.Count > 0)
        {
            foreach (var (wpnType, kills) in turnManager.WeaponKills)
            {
                if (y >= DialogHeight - 3) break;
                var info = turnManager.GetProficiencyInfo(wpnType);
                int bonus = turnManager.GetProficiencyBonus(wpnType);
                string progress = info.NextAt > 0
                    ? $"{info.Kills}/{info.NextAt}"
                    : "MAX";
                dialog.Add(new Label
                {
                    Text = $"  {wpnType,-16} {info.Rank,-14} +{bonus} dmg  [{progress}]",
                    X = 1, Y = y++
                });
            }
        }
        else
        {
            dialog.Add(new Label
            {
                Text = "  No weapon kills yet.",
                X = 1, Y = y++,
                ColorScheme = ColorSchemes.Dim
            });
        }

        // ── Close button ─────────────────────────────────────────────
        var closeBtn = new Button
        {
            Text = " Close ",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(1),
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };
        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        dialog.Add(closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }
}
