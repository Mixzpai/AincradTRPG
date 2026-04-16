using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Sword Skill equip dialog — lists all skills for the current weapon type,
// shows which are unlocked, and lets the player assign 4 to F1-F4 slots.
public static class SwordSkillDialog
{
    private const int DialogWidth = 80, DialogHeight = 26;

    public static void Show(TurnManager tm)
    {
        var wpn = tm.Player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.Weapon)
            as Items.Equipment.Weapon;
        string wtype = wpn?.WeaponType ?? "Unarmed";
        int kills = tm.GetWeaponKills(wtype);
        var allSkills = SwordSkillDatabase.ForWeapon(wtype).ToList();

        // Merge in Unique-Skill-gated OSS when the player has unlocked them.
        var us = Systems.Skills.UniqueSkillSystem.Unlocked;
        if (us.Contains(Systems.Skills.UniqueSkill.DualBlades) && wtype == "One-Handed Sword")
            allSkills.AddRange(SwordSkillDatabase.ForWeapon("Dual Blades"));
        if (us.Contains(Systems.Skills.UniqueSkill.HolySword) && wtype == "One-Handed Sword")
            allSkills.AddRange(SwordSkillDatabase.ForWeapon("Holy Sword"));
        if (us.Contains(Systems.Skills.UniqueSkill.DarknessBlade))
            allSkills.AddRange(SwordSkillDatabase.ForWeapon("Darkness"));
        if (us.Contains(Systems.Skills.UniqueSkill.BlazingEdge))
            allSkills.AddRange(SwordSkillDatabase.ForWeapon("Blazing"));
        if (us.Contains(Systems.Skills.UniqueSkill.FrozenEdge))
            allSkills.AddRange(SwordSkillDatabase.ForWeapon("Frozen"));

        var dialog = DialogHelper.Create($"Sword Skills — {wtype}", DialogWidth, DialogHeight);

        if (allSkills.Count == 0)
        {
            dialog.Add(new Label
            {
                Text = $"No sword skills exist for '{wtype}'. Try a different weapon!",
                X = 2, Y = 2, Width = Dim.Fill(2), ColorScheme = ColorSchemes.Dim,
            });
            DialogHelper.AddCloseFooter(dialog);
            DialogHelper.RunModal(dialog);
            return;
        }

        // ── Skill list ───────────────────────────────────────────────
        var names = new ObservableCollection<string>();
        foreach (var s in allSkills)
        {
            bool unlocked = kills >= s.RequiredProfKills;
            string lockTag = unlocked ? "" : $" [Locked: {s.RequiredProfKills} kills]";
            string cdTag = s.CooldownTurns > 0 ? $" CD:{s.CooldownTurns}T" : "";
            string slot = GetSlotLabel(tm, s);
            names.Add($"{slot}{s.Name,-20} {s.Hits}hit {s.DamageMultiplier:F1}x{cdTag} {s.Type,-8}{lockTag}");
        }

        var header = new Label
        {
            Text = $"Kills: {kills}   |   Proficiency: {tm.GetProficiencyInfo(wtype).Rank}   |   {allSkills.Count} skills total",
            X = 2, Y = 0, Width = Dim.Fill(2), ColorScheme = ColorSchemes.Gold,
        };

        var listView = new ListView
        {
            X = 1, Y = 2, Width = Dim.Fill(1), Height = Dim.Fill(7),
            Source = new ListWrapper<string>(names),
            CanFocus = true,
        };

        var detailLabel = new Label
        {
            Text = "", X = 2, Y = Pos.AnchorEnd(6),
            Width = Dim.Fill(2), Height = 2, ColorScheme = ColorSchemes.Body,
        };

        var slotLabel = new Label
        {
            Text = BuildSlotSummary(tm),
            X = 2, Y = Pos.AnchorEnd(4), Width = Dim.Fill(2), ColorScheme = ColorSchemes.Gold,
        };

        var hint = new Label
        {
            Text = "Enter: assign to slot  |  1-4: assign to specific slot  |  Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        listView.SelectedItemChanged += (s, e) =>
        {
            int idx = listView.SelectedItem;
            if (idx < 0 || idx >= allSkills.Count) return;
            var skill = allSkills[idx];
            bool unlocked = kills >= skill.RequiredProfKills;
            string status = unlocked ? "UNLOCKED" : $"Requires {skill.RequiredProfKills} kills ({skill.RequiredProfKills - kills} more)";
            string delay = skill.PostMotionDelay > 0 ? $"  Post-motion: {skill.PostMotionDelay}T vulnerability" : "";
            detailLabel.Text = $"{skill.Description}\n{status}{delay}";
            detailLabel.ColorScheme = unlocked ? ColorSchemes.Body : ColorSchemes.Dim;
        };

        // Assign to next available slot on Enter
        listView.OpenSelectedItem += (s, e) =>
        {
            Application.Invoke(() => TryAssignSkill(tm, allSkills, listView.SelectedItem, kills, -1,
                names, slotLabel, detailLabel, wtype));
        };

        // Direct slot assignment with 1-4 keys
        listView.KeyDown += (s, e) =>
        {
            int slot = e.KeyCode switch
            {
                KeyCode.D1 => 0, KeyCode.D2 => 1, KeyCode.D3 => 2, KeyCode.D4 => 3,
                _ => -1,
            };
            if (slot >= 0)
            {
                TryAssignSkill(tm, allSkills, listView.SelectedItem, kills, slot,
                    names, slotLabel, detailLabel, wtype);
                e.Handled = true;
            }
        };

        dialog.Add(header, listView, detailLabel, slotLabel, hint);
        DialogHelper.AddCloseFooter(dialog);
        listView.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    private static void TryAssignSkill(TurnManager tm, IReadOnlyList<SwordSkill> allSkills,
        int selectedIdx, int kills, int targetSlot,
        ObservableCollection<string> names, Label slotLabel, Label detailLabel, string wtype)
    {
        if (selectedIdx < 0 || selectedIdx >= allSkills.Count) return;
        var skill = allSkills[selectedIdx];
        if (kills < skill.RequiredProfKills)
        {
            detailLabel.Text = $"Not yet unlocked! Need {skill.RequiredProfKills - kills} more kills.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        // Find target slot: specific or first empty/replace existing
        if (targetSlot < 0)
        {
            // Check if already equipped — if so, unequip
            for (int i = 0; i < tm.EquippedSkills.Length; i++)
            {
                if (tm.EquippedSkills[i]?.Id == skill.Id)
                {
                    tm.EquippedSkills[i] = null;
                    RefreshNames(names, allSkills, kills, tm, wtype);
                    slotLabel.Text = BuildSlotSummary(tm);
                    detailLabel.Text = $"{skill.Name} removed from F{i + 1}.";
                    return;
                }
            }
            // Find first empty
            targetSlot = Array.IndexOf(tm.EquippedSkills, null);
            if (targetSlot < 0) targetSlot = 0; // overwrite first slot if all full
        }

        // Unequip if already in another slot
        for (int i = 0; i < tm.EquippedSkills.Length; i++)
            if (tm.EquippedSkills[i]?.Id == skill.Id) tm.EquippedSkills[i] = null;

        tm.EquippedSkills[targetSlot] = skill;
        RefreshNames(names, allSkills, kills, tm, wtype);
        slotLabel.Text = BuildSlotSummary(tm);
        detailLabel.Text = $"{skill.Name} assigned to F{targetSlot + 1}!";
        detailLabel.ColorScheme = ColorSchemes.Gold;
    }

    private static void RefreshNames(ObservableCollection<string> names,
        IReadOnlyList<SwordSkill> allSkills, int kills, TurnManager tm, string wtype)
    {
        for (int i = 0; i < allSkills.Count; i++)
        {
            var s = allSkills[i];
            bool unlocked = kills >= s.RequiredProfKills;
            string lockTag = unlocked ? "" : $" [Locked: {s.RequiredProfKills} kills]";
            string cdTag = s.CooldownTurns > 0 ? $" CD:{s.CooldownTurns}T" : "";
            string slot = GetSlotLabel(tm, s);
            names[i] = $"{slot}{s.Name,-20} {s.Hits}hit {s.DamageMultiplier:F1}x{cdTag} {s.Type,-8}{lockTag}";
        }
    }

    private static string GetSlotLabel(TurnManager tm, SwordSkill skill)
    {
        for (int i = 0; i < tm.EquippedSkills.Length; i++)
            if (tm.EquippedSkills[i]?.Id == skill.Id) return $"[F{i + 1}] ";
        return "     ";
    }

    private static string BuildSlotSummary(TurnManager tm)
    {
        var parts = new string[4];
        for (int i = 0; i < 4; i++)
            parts[i] = tm.EquippedSkills[i]?.Name ?? "---";
        return $"[F1] {parts[0],-16} [F2] {parts[1],-16} [F3] {parts[2],-16} [F4] {parts[3]}";
    }
}
