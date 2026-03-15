using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Skill point allocation dialog — allows distributing skill points into stats.
/// Accessible via P key. Shows live combat stat preview and weapon proficiency.
/// </summary>
public static class StatsDialog
{
    // ── Layout constants ─────────────────────────────────────────────
    private const int DialogWidth  = 56;
    private const int DialogHeight = 26;

    /// <summary>
    /// Stat definition — name, getter, and tooltip describing what it does.
    /// Add new stats here to extend the dialog automatically.
    /// </summary>
    private record StatDef(string Name, Func<Player, int> GetValue, string Effect);

    private static readonly StatDef[] Stats =
    {
        new("Vitality",     p => p.Vitality,     "+10 Max HP per point"),
        new("Strength",     p => p.Strength,     "+2 Attack per point"),
        new("Endurance",    p => p.Endurance,     "+2 Defense per point"),
        new("Dexterity",    p => p.Dexterity,     "+Crit Rate"),
        new("Agility",      p => p.Agility,       "+2 Speed, +Dodge per point"),
        new("Intelligence", p => p.Intelligence,  "+2 Skill Damage per point"),
    };

    public static void Show(Player player, TurnManager? turnManager = null)
    {
        var dialog = new Dialog
        {
            Title = "Allocate Skill Points",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        // ── Available points header ──────────────────────────────────
        var spLabel = new Label
        {
            Text = $"Available Skill Points: {player.SkillPoints}",
            X = 0, Y = 0
        };

        // ── Stat rows — one row per stat with name, value, +1 button, tooltip ──
        var buttons = new List<Button>();
        var valueLabels = new List<Label>();

        for (int i = 0; i < Stats.Length; i++)
        {
            int idx = i;   // closure capture
            int row = i + 2;

            var nameLabel  = new Label { Text = $"{Stats[idx].Name}:",   X = 1,  Y = row, Width = 14 };
            var valLabel   = new Label { Text = $"{Stats[idx].GetValue(player),3}", X = 16, Y = row, Width = 4 };
            var addBtn     = new Button { Text = "+1", X = 21, Y = row, ColorScheme = ColorSchemes.Button };
            var effectLabel = new Label { Text = Stats[idx].Effect,       X = 28, Y = row };

            valueLabels.Add(valLabel);
            buttons.Add(addBtn);

            // ── +1 button handler ────────────────────────────────────
            addBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (player.SkillPoints <= 0) return;

                player.SpendSkillPoints(Stats[idx].Name, 1);

                // Refresh all value labels
                spLabel.Text = $"Available Skill Points: {player.SkillPoints}";
                for (int j = 0; j < Stats.Length; j++)
                    valueLabels[j].Text = $"{Stats[j].GetValue(player),3}";

                // Vitality special: heal to new max
                if (Stats[idx].Name == "Vitality")
                    player.CurrentHealth = Math.Min(player.CurrentHealth + 10, player.MaxHealth);
            };

            dialog.Add(nameLabel, valLabel, addBtn, effectLabel);
        }

        // ── Live combat stats preview ────────────────────────────────
        var combatLabel = new Label
        {
            Text = "",
            X = 1,
            Y = Stats.Length + 3,
            Width = Dim.Fill(1)
        };

        void RefreshCombat() =>
            combatLabel.Text = $"ATK:{player.Attack} DEF:{player.Defense} SPD:{player.Speed} HP:{player.CurrentHealth}/{player.MaxHealth}";
        RefreshCombat();

        // Wire combat refresh to every +1 button
        foreach (var btn in buttons)
            btn.Accepting += (s, e) => RefreshCombat();

        // ── Weapon proficiency section ────────────────────────────────
        int profY = Stats.Length + 5;
        var profHeader = new Label
        {
            Text = "── Weapon Proficiency ──",
            X = 1, Y = profY,
            ColorScheme = ColorSchemes.Gold
        };
        dialog.Add(profHeader);

        if (turnManager != null && turnManager.WeaponKills.Count > 0)
        {
            int row = profY + 1;
            foreach (var (wpnType, kills) in turnManager.WeaponKills)
            {
                var info = turnManager.GetProficiencyInfo(wpnType);
                string progress = info.NextAt > 0
                    ? $"{info.Kills}/{info.NextAt} → {info.NextRank}"
                    : "MAX";
                var profLine = new Label
                {
                    Text = $"  {wpnType,-18} {info.Rank,-14} +{turnManager.GetProficiencyBonus(wpnType)} dmg  ({progress})",
                    X = 1, Y = row,
                    Width = Dim.Fill(1)
                };
                dialog.Add(profLine);
                row++;
                if (row >= DialogHeight - 3) break;  // don't overflow
            }
        }
        else
        {
            var noneLabel = new Label
            {
                Text = "  No weapon kills yet.",
                X = 1, Y = profY + 1,
                ColorScheme = ColorSchemes.Dim
            };
            dialog.Add(noneLabel);
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

        dialog.Add(spLabel, combatLabel, closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }
}
