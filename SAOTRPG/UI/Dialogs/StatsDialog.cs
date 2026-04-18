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
    // Dialog height in rows. Bumped to house the FB-050 Life Skills,
    // FB-058 Titles, and FB-063 Guild/Karma sections without squeezing
    // the existing stat grid.
    private const int DialogHeight = 58;

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

        int profEndRow = profY + 1;
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
                // Stop just before the life-skills section kicks in.
                if (row >= DialogHeight - 18) break;
            }
            profEndRow = row;
        }
        else
        {
            dialog.Add(new Label
            {
                Text = "  No weapon kills yet.",
                X = 1, Y = profY + 1,
                ColorScheme = ColorSchemes.Dim
            });
            profEndRow = profY + 2;
        }

        // ── FB-050 Life Skills section ────────────────────────────────
        int lsY = profEndRow + 1;
        dialog.Add(new Label
        {
            Text = "[ Life Skills ]",
            X = 1, Y = lsY,
            ColorScheme = ColorSchemes.Gold,
        });
        int lsRow = lsY + 1;
        foreach (var skill in Enum.GetValues<Systems.LifeSkillType>())
        {
            int lvl = player.LifeSkills.GetLevel(skill);
            var (cur, nxt) = player.LifeSkills.GetLevelProgress(skill);
            string bar = BuildProgressBar(cur, nxt, 10);
            string bonus = ActiveBonusSummary(player.LifeSkills, skill);
            string line = $"  {Systems.LifeSkillSystem.Label(skill),-8} L{lvl,2}/99 {bar} "
                + $"{cur}/{Math.Max(1, nxt)}  {bonus}";
            if (line.Length > DialogWidth - 4) line = line[..(DialogWidth - 5)] + "…";
            dialog.Add(new Label
            {
                Text = line,
                X = 1, Y = lsRow,
                Width = Dim.Fill(1),
                ColorScheme = lvl > 1 ? ColorSchemes.Body : ColorSchemes.Dim,
            });
            lsRow++;
        }

        // ── FB-058 Active Title section ───────────────────────────────
        int tY = lsRow + 1;
        dialog.Add(new Label
        {
            Text = "[ Active Title ]",
            X = 1, Y = tY,
            ColorScheme = ColorSchemes.Gold,
        });
        string titleText;
        ColorScheme titleScheme;
        if (player.ActiveTitleId != null
            && Systems.TitleSystem.Titles.TryGetValue(player.ActiveTitleId, out var activeDef))
        {
            titleText = $"  ★ {activeDef.DisplayName} — {activeDef.Description}";
            titleScheme = ColorSchemes.Gold;
        }
        else
        {
            titleText = "  (none equipped — visit the Monument of Swordsmen on F1 to choose)";
            titleScheme = ColorSchemes.Dim;
        }
        if (titleText.Length > DialogWidth - 4) titleText = titleText[..(DialogWidth - 5)] + "…";
        dialog.Add(new Label
        {
            Text = titleText,
            X = 1, Y = tY + 1,
            Width = Dim.Fill(1),
            ColorScheme = titleScheme,
        });
        int unlockedCount = player.UnlockedTitleIds.Count;
        int totalTitles = Systems.TitleSystem.Titles.Count;
        dialog.Add(new Label
        {
            Text = $"  Titles unlocked: {unlockedCount}/{totalTitles}",
            X = 1, Y = tY + 2,
            ColorScheme = ColorSchemes.Dim,
        });

        // ── FB-063 Guild Affiliation + Karma section ──────────────────
        int gY = tY + 4;
        dialog.Add(new Label
        {
            Text = "[ Guild & Karma ]",
            X = 1, Y = gY,
            ColorScheme = ColorSchemes.Gold,
        });
        string karmaTier = Systems.KarmaSystem.TierLabel(player.Karma);
        string karmaLine = $"  Karma: {player.Karma,+4} [{karmaTier}]";
        dialog.Add(new Label
        {
            Text = karmaLine, X = 1, Y = gY + 1, Width = Dim.Fill(1),
            ColorScheme = karmaTier switch
            {
                "Honorable" => ColorSchemes.Gold,
                "Outlaw"    => ColorSchemes.Danger,
                "Shady"     => ColorSchemes.Dim,
                _           => ColorSchemes.Body,
            },
        });
        string guildName = Systems.GuildSystem.ActiveGuildDisplayName(player);
        string guildPerk = Systems.GuildSystem.ActiveGuildPerkFlavor(player);
        dialog.Add(new Label
        {
            Text = $"  Guild: {guildName}", X = 1, Y = gY + 2,
            Width = Dim.Fill(1),
            ColorScheme = player.ActiveGuildId == Systems.Story.Faction.None
                ? ColorSchemes.Dim : ColorSchemes.Body,
        });
        if (!string.IsNullOrEmpty(guildPerk))
        {
            string perkLine = $"    Perk: {guildPerk}";
            if (perkLine.Length > DialogWidth - 4) perkLine = perkLine[..(DialogWidth - 5)] + "…";
            dialog.Add(new Label
            {
                Text = perkLine, X = 1, Y = gY + 3, Width = Dim.Fill(1),
                ColorScheme = ColorSchemes.Dim,
            });
        }
        var rosterBtn = DialogHelper.CreateButton("View All Guilds");
        rosterBtn.X = 1; rosterBtn.Y = gY + 4;
        rosterBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            GuildRosterDialog.Show(player);
        };
        dialog.Add(rosterBtn);

        var hintLabel = new Label
        {
            Text = "Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(spLabel, combatLabel, hintLabel);
        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.RunModal(dialog);
    }

    // Simple horizontal XP bar: [#####-----]. Width is the inner-bar length.
    private static string BuildProgressBar(int current, int total, int width)
    {
        if (total <= 0) return "[" + new string('#', width) + "]";
        int filled = Math.Clamp(current * width / total, 0, width);
        return "[" + new string('#', filled) + new string('·', width - filled) + "]";
    }

    // One-line summary of the current active milestone bonus for a skill.
    // Shown alongside the XP bar so the player sees exactly what they have.
    private static string ActiveBonusSummary(Systems.LifeSkillSystem ls, Systems.LifeSkillType skill)
    {
        int lvl = ls.GetLevel(skill);
        if (lvl < 10) return "";
        int milestone = lvl >= 99 ? 99 : lvl >= 50 ? 50 : lvl >= 25 ? 25 : 10;
        return "— " + Systems.LifeSkillSystem.MilestoneBonusDescription(skill, milestone);
    }
}
