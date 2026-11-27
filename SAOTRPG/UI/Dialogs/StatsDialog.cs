using SAOTRPG.Entities;
using SAOTRPG.UI.Helpers;
using SAOTRPG.Systems.Combat;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.Systems.Progression;
using SAOTRPG.Systems.Skills;
using SAOTRPG.Systems.Turns;
using Skills = SAOTRPG.Systems.Skills;
using SAOTRPG.UI.Widgets;

namespace SAOTRPG.UI.Dialogs;

// Skill point allocation dialog — allows distributing skill points into stats.
// Accessible via P key. Shows live combat stat preview and weapon proficiency.
public static class StatsDialog
{
    // ── Layout constants ─────────────────────────────────────────────

    // Single-column width, and the widest the two-column form is allowed to grow.
    private const int DialogWidth  = 64;
    private const int WideDialogWidth = 116;
    // Below this the second column has nowhere to go and the layout stays single-column, so the
    // reflow depends on the screen width rather than always being on and clipping horizontally.
    private const int TwoColumnMinWidth = 100;

    private static int DialogWidthFor() =>
        Math.Min(WideDialogWidth, Math.Max(DialogWidth, AppHost.App.Screen.Width - 4));
    // Preferred height houses Life Skills / Titles / Guild / Karma / Bargaining / Swimming
    // without squeezing the stat grid; clamped at open time via DialogHeight() for small terminals.
    private const int PreferredDialogHeight = 60;

    // Clamp preferred height to terminal, 4-row margin keeps border inside screen bounds.
    private static int DialogHeight() =>
        Math.Min(PreferredDialogHeight, Math.Max(20, AppHost.App.Screen.Height - 4));

    // What SPD is worth right now, against the monsters actually standing on this floor. Speed is
    // a contest rather than a flat bonus, so a bare number cannot say what it buys — and the mob
    // side is read off the live roster rather than from a copy of MobFactory's own 3 + floor
    // formula, which would be a second copy of that number to keep in step.
    //
    // With nothing alive to pace, the ceiling against a motionless target is shown instead and is
    // labelled as such: a blank would read as "Speed does nothing here".
    private static string SpeedFollowUpNote(Player player, TurnManager? turnManager)
    {
        var live = turnManager?.Map.Monsters;
        if (live != null && live.Count > 0)
        {
            var speeds = live.Where(m => !m.IsDefeated).Select(m => m.BaseSpeed).OrderBy(v => v).ToList();
            if (speeds.Count > 0)
                return $" (+{SpeedContest.FollowUpChance(player.Speed, speeds[speeds.Count / 2])}% follow-up here)";
        }
        return $" (+{SpeedContest.FollowUpChance(player.Speed, 0)}% follow-up vs a still target)";
    }

    // Stat definition — name, getter, and tooltip describing what it does.
    // Add new stats here to extend the dialog automatically.
    private record StatDef(string Name, Func<Player, int> GetValue, string Effect);

    private static readonly StatDef[] Stats =
    {
        new("Vitality",     p => p.Vitality,     "+10 Max HP per point"),
        new("Strength",     p => p.Strength,     "+2 Attack per point"),
        new("Endurance",    p => p.Endurance,     "+2 Defense per point"),
        new("Dexterity",    p => p.Dexterity,     "+Crit Rate"),
        new("Agility",      p => p.Agility,       "+2 Speed (follow-ups), +Dodge"),
        new("Intelligence", p => p.Intelligence,  "+2 Skill Damage per point"),
    };

    // Opens the skill allocation dialog with live combat stat preview and weapon proficiency.
    public static void Show(Player player, TurnManager? turnManager = null)
    {
        // Resolve pending proficiency forks first (L25/50/75/100 thresholds the player Esc'd past).
        // Each opens its own modal so displayed stats reflect the fresh fork bonuses.
        // Talents owed from earlier level-ups, re-offering the exact three that level rolled.
        if (turnManager != null)
        {
            foreach (var pending in turnManager.EnumeratePendingTalents())
            {
                var picked = TalentPickDialog.Show(pending.Choices);
                if (picked != null) turnManager.ResolveTalentPick(pending.Id, picked);
            }
        }

        if (turnManager != null)
        {
            foreach (var (wpnType, forkLevel) in turnManager.EnumeratePendingForks())
            {
                var (o1, o2) = TurnManager.GetForkOptions(forkLevel, wpnType);
                int pick = ProficiencyForkDialog.Show(wpnType, forkLevel, o1, o2);
                if (pick == 1 || pick == 2)
                    turnManager.ApplyProficiencyFork(wpnType, forkLevel, pick);
            }
        }

        int dlgH = DialogHeight();
        int dlgW = DialogWidthFor();
        // Two columns halve the height: the stat grid and Unique Skills on the left, everything
        // below them on the right. That is what lets the whole sheet fit a 30-row terminal.
        bool twoCol = dlgW >= TwoColumnMinWidth;
        int colW = twoCol ? (dlgW - 5) / 2 : dlgW - 4;
        int rightX = twoCol ? colW + 3 : 1;
        var dialog = DialogHelper.Create("Allocate Skill Points", dlgW, dlgH);

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
            var addBtn     = new PlainButton { Text = "+1", X = 21, Y = row, SchemeName = ColorSchemes.ButtonName,
                                           ShadowStyle = null };
            var effectLabel = new Label { Text = Stats[idx].Effect,       X = 28, Y = row };

            valueLabels.Add(valLabel);
            buttons.Add(addBtn);

            // ── +1 button handler ────────────────────────────────────
            addBtn.Accepting += (s, e) =>
            {
                e.Handled = true;
                if (player.SkillPoints <= 0) return;

                player.SpendSkillPoints(Stats[idx].Name, 1);

                // Refresh all value labels — flash changed stat gold
                spLabel.Text = $"Available Skill Points: {player.SkillPoints}";
                for (int j = 0; j < Stats.Length; j++)
                {
                    valueLabels[j].Text = $"{Stats[j].GetValue(player),3}";
                    valueLabels[j].SchemeName = j == idx ? ColorSchemes.GoldName : ColorSchemes.BodyName;
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

        void RefreshCombat()
        {
            // SPD is the raw derived stat with no fatigue penalty: it has to agree with the formula
            // the Guide states for SPD, and fatigue charges attack and defence only.
            combatLabel.Text = $"ATK:{player.Attack} DEF:{player.Defense} SPD:{player.Speed}"
                             + SpeedFollowUpNote(player, turnManager)
                             + $" HP:{player.CurrentHealth}/{player.MaxHealth}";
        }
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
                SchemeName = loreFound >= loreTotal ? ColorSchemes.GoldName : ColorSchemes.DimName
            };
            dialog.Add(loreLabel);
        }

        // ── Unique Skills section ─────────────────────────────────────
        int usY = Stats.Length + 7;
        dialog.Add(new Label
        {
            Text = "[ Unique Skills ]",
            X = 1, Y = usY,
            SchemeName = ColorSchemes.GoldName,
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
            if (line.Length > colW) line = line[..(colW - 1)] + "…";
            dialog.Add(new Label
            {
                Text = line,
                X = 1, Y = usRow,
                Width = Dim.Fill(1),
            }.WithScheme(unlocked ? ColorSchemes.FromColor(def.DisplayColor) : ColorSchemes.Dim));
            usRow++;
        }

        // ── Weapon proficiency section ────────────────────────────────
        // Second column when there is width for one, otherwise straight on below.
        int profY = twoCol ? 2 : usRow + 1;
        var profHeader = new Label
        {
            Text = "[ Weapon Proficiency ]",
            X = rightX, Y = profY,
            SchemeName = ColorSchemes.GoldName
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
                    X = rightX, Y = row,
                    Width = colW
                });
                row++;
                // Stop just before the life-skills section kicks in.
                if (row >= profY + 6) break;
            }
            profEndRow = row;
        }
        else
        {
            dialog.Add(new Label
            {
                Text = "  No weapon kills yet.",
                X = rightX, Y = profY + 1,
                SchemeName = ColorSchemes.DimName
            });
            profEndRow = profY + 2;
        }

        // ── Life Skills section ───────────────────────────────────────
        int lsY = profEndRow + 1;
        dialog.Add(new Label
        {
            Text = "[ Life Skills ]",
            X = rightX, Y = lsY,
            SchemeName = ColorSchemes.GoldName,
        });
        int lsRow = lsY + 1;
        foreach (var skill in Enum.GetValues<LifeSkillType>())
        {
            int lvl = player.LifeSkills.GetLevel(skill);
            var (cur, nxt) = player.LifeSkills.GetLevelProgress(skill);
            string bar = BuildProgressBar(cur, nxt, 10);
            string bonus = ActiveBonusSummary(player.LifeSkills, skill);
            string line = $"  {SAOTRPG.Systems.Skills.LifeSkillSystem.Label(skill),-8} L{lvl,2}/99 {bar} "
                + $"{cur}/{Math.Max(1, nxt)}  {bonus}";
            if (line.Length > colW) line = line[..(colW - 1)] + "…";
            dialog.Add(new Label
            {
                Text = line,
                X = rightX, Y = lsRow,
                Width = colW,
                SchemeName = lvl > 1 ? ColorSchemes.BodyName : ColorSchemes.DimName,
            });
            lsRow++;
        }

        // ── Active Title section ──────────────────────────────────────
        int tY = lsRow + 1;
        dialog.Add(new Label
        {
            Text = "[ Active Title ]",
            X = rightX, Y = tY,
            SchemeName = ColorSchemes.GoldName,
        });
        string titleText;
        Scheme titleScheme;
        if (player.ActiveTitleId != null
            && MilestoneRegistry.ById.TryGetValue(player.ActiveTitleId, out var activeDef))
        {
            titleText = $"  ★ {activeDef.Name} — {activeDef.Description}";
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
            X = rightX, Y = tY + 1,
            Width = colW,
        }.WithScheme(titleScheme));
        // One snapshot rather than one IsMilestoneUnlocked per row: that helper re-reads and
        // re-deserializes lifetime_stats.json on every call.
        var unlockedIds = new HashSet<string>(LifetimeStats.Load().UnlockedMilestones,
                                              StringComparer.Ordinal);
        int unlockedCount = 0, totalTitles = 0;
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Reward != RewardType.EquippableTitle) continue;
            totalTitles++;
            if (unlockedIds.Contains(m.Id)) unlockedCount++;
        }
        dialog.Add(new Label
        {
            Text = $"  Titles unlocked: {unlockedCount}/{totalTitles}",
            X = rightX, Y = tY + 2,
            SchemeName = ColorSchemes.DimName,
        });

        // ── Guild Affiliation + Karma section ─────────────────────────
        int gY = tY + 4;
        dialog.Add(new Label
        {
            Text = "[ Guild & Karma ]",
            X = rightX, Y = gY,
            SchemeName = ColorSchemes.GoldName,
        });
        string karmaTier = SAOTRPG.Systems.Social.KarmaSystem.TierLabel(player.Karma);
        string karmaLine = $"  Karma: {player.Karma,+4} [{karmaTier}]";
        dialog.Add(new Label
        {
            Text = karmaLine, X = rightX, Y = gY + 1, Width = colW,
            SchemeName = karmaTier switch
            {
                "Honorable" => ColorSchemes.GoldName,
                "Outlaw"    => ColorSchemes.DangerName,
                "Shady"     => ColorSchemes.DimName,
                _           => ColorSchemes.BodyName,
            },
        });
        string guildName = SAOTRPG.Systems.Social.GuildSystem.ActiveGuildDisplayName(player);
        string guildPerk = SAOTRPG.Systems.Social.GuildSystem.ActiveGuildPerkFlavor(player);
        dialog.Add(new Label
        {
            Text = $"  Guild: {guildName}", X = rightX, Y = gY + 2,
            Width = colW,
            SchemeName = player.ActiveGuildId == Systems.Story.Faction.None
                ? ColorSchemes.DimName : ColorSchemes.BodyName,
        });
        if (!string.IsNullOrEmpty(guildPerk))
        {
            string perkLine = $"    Perk: {guildPerk}";
            if (perkLine.Length > colW) perkLine = perkLine[..(colW - 1)] + "…";
            dialog.Add(new Label
            {
                Text = perkLine, X = rightX, Y = gY + 3, Width = colW,
                SchemeName = ColorSchemes.DimName,
            });
        }
        // Anchored, not placed after the guild block: gY + 4 lies below the clamped height, and
        // this button is the only route to the guild roster.
        var rosterBtn = DialogHelper.CreateButton("View All Guilds");
        rosterBtn.X = rightX; rosterBtn.Y = Pos.AnchorEnd(4);
        rosterBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            GuildRosterDialog.Show(player);
        };
        dialog.Add(rosterBtn);

        // No hint label here: AddCloseFooter already draws an esc hint on AnchorEnd(1), and a
        // second one would occupy the same row.
        dialog.Add(spLabel, combatLabel);
        DialogHelper.AddCloseFooter(dialog);
        DialogHelper.DiscloseClippedContent(dialog);
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
    private static string ActiveBonusSummary(LifeSkillSystem ls, LifeSkillType skill)
    {
        int lvl = ls.GetLevel(skill);
        if (lvl < 10) return "";
        int milestone = lvl >= 99 ? 99 : lvl >= 50 ? 50 : lvl >= 25 ? 25 : 10;
        return "— " + SAOTRPG.Systems.Skills.LifeSkillSystem.MilestoneBonusDescription(skill, milestone);
    }
}
