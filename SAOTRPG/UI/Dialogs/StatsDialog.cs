using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;
using Skills = SAOTRPG.Systems.Skills;

namespace SAOTRPG.UI.Dialogs;

// Skill point allocation dialog — allows distributing skill points into stats.
// Accessible via P key. Shows live combat stat preview and weapon proficiency.
public static class StatsDialog
{
    // ── Layout constants ─────────────────────────────────────────────

    // Dialog width in columns.
    private const int DialogWidth  = 64;
    // Dialog height in rows.
    private const int DialogHeight = 40;

    // Stat definition — name, getter, and tooltip describing what it does.
    // Add new stats here to extend the dialog automatically.
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

    // Opens the skill allocation dialog with live combat stat preview and weapon proficiency.
    public static void Show(Player player, TurnManager? turnManager = null)
    {
        // Resolve any pending proficiency forks first — pending means the
        // player crossed L25/50/75/100 but dismissed (Esc'd) the picker.
        // Each pending entry opens its own modal here before the Stats sheet
        // proper so the displayed stats reflect the fresh fork bonuses.
        if (turnManager != null)
        {
            foreach (var (wpnType, forkLevel) in turnManager.EnumeratePendingForks())
            {
                var (o1, o2) = TurnManager.GetForkOptions(forkLevel);
                int pick = ProficiencyForkDialog.Show(wpnType, forkLevel, o1, o2);
                if (pick == 1 || pick == 2)
                    turnManager.ApplyProficiencyFork(wpnType, forkLevel, pick);
            }
        }

        var dialog = DialogHelper.Create("Allocate Skill Points", DialogWidth, DialogHeight);

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

                // Refresh all value labels — flash changed stat gold
                spLabel.Text = $"Available Skill Points: {player.SkillPoints}";
                for (int j = 0; j < Stats.Length; j++)
                {
                    valueLabels[j].Text = $"{Stats[j].GetValue(player),3}";
                    valueLabels[j].ColorScheme = j == idx ? ColorSchemes.Gold : ColorSchemes.Body;
                }

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

        // ── Lore collection ──────────────────────────────────────────
        if (turnManager != null)
        {
            int loreFound = turnManager.DiscoveredLore.Count;
            int loreTotal = FlavorText.LoreStoneEntries.Length;
            var loreLabel = new Label
            {
                Text = $"Lore Discovered: {loreFound}/{loreTotal}",
                X = 1, Y = Stats.Length + 5,
                ColorScheme = loreFound >= loreTotal ? ColorSchemes.Gold : ColorSchemes.Dim
            };
            dialog.Add(loreLabel);
        }

        // ── Unique Skills section ─────────────────────────────────────
        int usY = Stats.Length + 7;
        dialog.Add(new Label
        {
            Text = "[ Unique Skills ]",
            X = 1, Y = usY,
            ColorScheme = ColorSchemes.Gold,
        });
        int usRow = usY + 1;
        foreach (var kvp in Skills.UniqueSkillSystem.Definitions)
        {
            var def = kvp.Value;
            bool unlocked = Skills.UniqueSkillSystem.Has(kvp.Key);
            string glyph = unlocked ? "★" : "☆";
            string line = unlocked
                ? $"  {glyph} {def.Name} — {def.Description}"
                : $"  {glyph} {def.Name} — [{def.UnlockHint}]";
            if (line.Length > DialogWidth - 4) line = line[..(DialogWidth - 5)] + "…";
            dialog.Add(new Label
            {
                Text = line,
                X = 1, Y = usRow,
                Width = Dim.Fill(1),
                ColorScheme = unlocked ? ColorSchemes.FromColor(def.DisplayColor) : ColorSchemes.Dim,
            });
            usRow++;
        }

        // ── Weapon proficiency section ────────────────────────────────
        int profY = usRow + 1;
        var profHeader = new Label
        {
            Text = "[ Weapon Proficiency ]",
            X = 1, Y = profY,
            ColorScheme = ColorSchemes.Gold
        };
        dialog.Add(profHeader);

        if (turnManager != null && turnManager.WeaponKills.Count > 0)
        {
            int row = profY + 1;
            foreach (var (wpnType, _) in turnManager.WeaponKills)
            {
                dialog.Add(new Label
                {
                    Text = ProficiencyHelper.BuildDetailLineExpanded(turnManager, wpnType),
                    X = 1, Y = row,
                    Width = Dim.Fill(1)
                });
                row++;
                if (row >= DialogHeight - 3) break;
            }
        }
        else
        {
            dialog.Add(new Label
            {
                Text = "  No weapon kills yet.",
                X = 1, Y = profY + 1,
                ColorScheme = ColorSchemes.Dim
            });
        }

        var hintLabel = new Label
        {
            Text = "Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(spLabel, combatLabel, hintLabel);
        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }
}
