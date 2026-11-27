using System.Collections.ObjectModel;
using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Helpers;
using SAOTRPG.Systems.Skills;
using SAOTRPG.Systems.Turns;

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
                X = 2, Y = 2, Width = Dim.Fill(2), SchemeName = ColorSchemes.DimName,
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
            X = 2, Y = 0, Width = Dim.Fill(2), SchemeName = ColorSchemes.GoldName,
        };

        var listView = new ListView
        {
            X = 1, Y = 2, Width = Dim.Fill(1), Height = Dim.Fill(7),
            Source = new ListWrapper<string>(names),
            CanFocus = true,
            // Type-ahead off — it swallows the 1-4 slot-assign digits. ListView.OnKeyDown eats a
            // printable rune before KeyDown is raised, so one arrow key would kill them for the
            // rest of the session — and arrowing to a skill is how you pick one to assign.
            KeystrokeNavigator = null,
        };

        var detailLabel = new Label
        {
            Text = "", X = 2, Y = Pos.AnchorEnd(6),
            Width = Dim.Fill(2), Height = 2, SchemeName = ColorSchemes.BodyName,
        };

        var slotLabel = new Label
        {
            Text = BuildSlotSummary(tm),
            X = 2, Y = Pos.AnchorEnd(4), Width = Dim.Fill(2), SchemeName = ColorSchemes.GoldName,
        };

        var hint = new Label
        {
            Text = "Enter: assign to next free slot  |  1-4: assign to that slot  |  Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), SchemeName = ColorSchemes.DimName,
        };

        listView.ValueChanged += (s, e) =>
        {
            int idx = listView.SelectedItem ?? -1;
            if (idx < 0 || idx >= allSkills.Count) return;
            var skill = allSkills[idx];
            bool unlocked = kills >= skill.RequiredProfKills;
            string status = unlocked ? "UNLOCKED" : $"Requires {skill.RequiredProfKills} kills ({skill.RequiredProfKills - kills} more)";
            string delay = skill.PostMotionDelay > 0 ? $"  Post-motion: {skill.PostMotionDelay}T vulnerability" : "";
            detailLabel.Text = $"{skill.Description}\n{status}{delay}";
            detailLabel.SchemeName = unlocked ? ColorSchemes.BodyName : ColorSchemes.DimName;
        };

        // Assign to next available slot on Enter.
        // Enter is Command.Accept; Activated answers Command.Activate (Space). An unhandled
        // Accept bubbles to the dialog's default button — Close — so the advertised
        // "Enter: assign to slot" would shut the dialog. Claiming it stops the bubble.
        void AssignSelected() => AppHost.App.Invoke(() =>
            TryAssignSkill(tm, allSkills, listView.SelectedItem ?? 0, kills, -1,
                names, slotLabel, detailLabel, wtype));

        listView.Accepting += (s, e) => { e.Handled = true; AssignSelected(); };
        listView.Activated += (s, e) => AssignSelected();

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
                TryAssignSkill(tm, allSkills, listView.SelectedItem ?? 0, kills, slot,
                    names, slotLabel, detailLabel, wtype);
                e.Handled = true;
            }
        };

        dialog.Add(header, listView, detailLabel, slotLabel, hint);
        DialogHelper.AddCloseFooter(dialog);
        listView.SetFocus();
        DialogHelper.SelectFirstRow(listView);
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
            detailLabel.SchemeName = ColorSchemes.DangerName;
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
        detailLabel.SchemeName = ColorSchemes.GoldName;
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

    private static readonly GameAction[] SlotActions =
    {
        GameAction.SwordSkill1, GameAction.SwordSkill2,
        GameAction.SwordSkill3, GameAction.SwordSkill4,
    };

    // Slot tags sit in a fixed column ahead of the skill name, so the tag and the blank that
    // stands in for "not equipped" are both padded to the widest bound key. F1 is two characters
    // and a rebind to Shift+M is seven; a tag that grew would shift every name in the list.
    private static int SlotTagWidth() =>
        SlotActions.Max(a => KeyHints.Key(a).Length) + 3;

    private static string GetSlotLabel(TurnManager tm, SwordSkill skill)
    {
        int width = SlotTagWidth();
        int slots = Math.Min(tm.EquippedSkills.Length, SlotActions.Length);
        for (int i = 0; i < slots; i++)
            if (tm.EquippedSkills[i]?.Id == skill.Id)
                return $"[{KeyHints.Key(SlotActions[i])}] ".PadRight(width);
        return new string(' ', width);
    }

    private static string BuildSlotSummary(TurnManager tm)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < SlotActions.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            string name = tm.EquippedSkills[i]?.Name ?? "---";
            sb.Append('[').Append(KeyHints.Key(SlotActions[i])).Append("] ")
              .Append(i == SlotActions.Length - 1 ? name : name.PadRight(16));
        }
        return sb.ToString();
    }
}
