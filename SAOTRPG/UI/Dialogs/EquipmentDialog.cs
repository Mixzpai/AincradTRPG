using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Equipment overview dialog — shows all equipped gear at a glance with
// per-slot details, stat totals, durability warnings, and unequip support.
public static class EquipmentDialog
{
    private const int FixedHeight = 24, MinWidth = 52, MaxWidth = 84;

    public static void Show(Player player)
    {
        var slots = EquipmentSlotView.DefaultSlotLayout;

        // Measure the widest line so the dialog fits every item name.
        int maxLen = 20;
        foreach (var (slot, _, _, label) in slots)
        {
            var eq = player.Inventory.GetEquipped(slot);
            string itemText = eq != null ? $"{eq.Name} ({eq.Rarity})" : "(empty)";
            int lineLen = 4 + label.Length + 1 + itemText.Length;
            if (lineLen > maxLen) maxLen = lineLen;
        }
        int dialogWidth = Math.Clamp(maxLen + 10, MinWidth, MaxWidth);

        var dialog = DialogHelper.Create("Equipment", dialogWidth, FixedHeight);

        // ── Slot list ────────────────────────────────────────────────
        var slotsHeader = new Label
        {
            Text = "[ Equipped Gear ]", X = 1, Y = 0,
            Width = Dim.Fill(1), ColorScheme = ColorSchemes.Gold,
        };
        var slotView = new EquipmentSlotView(player, dialogWidth - 4)
        {
            X = 1, Y = 1, Width = Dim.Fill(1), Height = EquipmentSlotView.SlotCount,
        };

        // ── Stat totals + durability ─────────────────────────────────
        var statsHeader = new Label
        {
            Text = "[ Stats ]", X = 1, Y = EquipmentSlotView.SlotCount + 2,
            Width = Dim.Fill(1), ColorScheme = ColorSchemes.Gold,
        };
        var statsLabel = new Label
        {
            Text = BuildStatTotals(player), X = 1, Y = EquipmentSlotView.SlotCount + 3,
            Width = Dim.Fill(1), ColorScheme = ColorSchemes.Body,
        };
        var durText = BuildDurabilitySummary(player);
        bool durCritical = durText.StartsWith("[LOW]");
        var durLabel = new Label
        {
            Text = durText, X = 1, Y = EquipmentSlotView.SlotCount + 4,
            Width = Dim.Fill(1),
            ColorScheme = durCritical ? ColorSchemes.Danger : ColorSchemes.Dim,
        };

        // ── Detail + preview lines ───────────────────────────────────
        var detailLabel = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(5),
            Width = Dim.Fill(1), Height = 1, ColorScheme = ColorSchemes.Body,
        };
        var previewLabel = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(1), Height = 1, ColorScheme = ColorSchemes.Dim,
        };

        // ── Unequip button ───────────────────────────────────────────
        var unequipBtn = DialogHelper.CreateButton("Unequip");
        unequipBtn.X = 1;
        unequipBtn.Y = Pos.AnchorEnd(3);

        // ── Slot selection handler ───────────────────────────────────
        slotView.SelectedSlotChanged += (idx) =>
        {
            if (idx < 0 || idx >= slots.Length) { detailLabel.Text = ""; previewLabel.Text = ""; return; }
            var slot = slots[idx].Slot;
            var eq = player.Inventory.GetEquipped(slot);
            detailLabel.Text = eq != null ? BuildItemDetail(eq) : "No item equipped in this slot.";
            previewLabel.Text = eq != null ? BuildUnequipPreview(eq) : "";
        };

        unequipBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int idx = slotView.SelectedIndex;
            if (idx < 0 || idx >= slots.Length) return;
            var slot = slots[idx].Slot;
            var eq = player.Inventory.GetEquipped(slot);
            if (eq == null) return;
            if (!DialogHelper.ConfirmAction("Unequip", eq.Name ?? "")) return;

            bool success = player.Inventory.Unequip(slot, player);
            if (success)
            {
                slotView.SetNeedsDraw();
                detailLabel.Text = "No item equipped in this slot.";
                previewLabel.Text = "";
                statsLabel.Text = BuildStatTotals(player);
                var newDurText = BuildDurabilitySummary(player);
                durLabel.Text = newDurText;
                durLabel.ColorScheme = newDurText.StartsWith("[LOW]") ? ColorSchemes.Danger : ColorSchemes.Dim;
            }
        };

        var hintLabel = new Label
        {
            Text = "Enter: unequip | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        // ── Assemble ─────────────────────────────────────────────────
        dialog.Add(slotsHeader, slotView, statsHeader, statsLabel, durLabel,
            detailLabel, previewLabel, unequipBtn, hintLabel);

        var closeBtn = DialogHelper.AddCloseFooter(dialog);
        closeBtn.Y = Pos.AnchorEnd(3);
        closeBtn.X = Pos.Right(unequipBtn) + 1;

        DialogHelper.RunModal(dialog);
    }

    // Formats a one-line detail string for an equipped item.
    internal static string BuildItemDetail(EquipmentBase eq)
    {
        var parts = new List<string> { $"Lv.{eq.RequiredLevel} {eq.Rarity}" };

        if (eq is Weapon w)
        {
            parts.Add($"DMG: {w.BaseDamage}");
            if (!string.IsNullOrEmpty(w.WeaponType)) parts.Add($"Type: {w.WeaponType}");
            if (w.AttackSpeed > 0) parts.Add($"SPD: {w.AttackSpeed}");
        }
        else if (eq is Armor a)
        {
            parts.Add($"DEF: {a.BaseDefense}");
            if (a.BlockChance > 0) parts.Add($"Block: {a.BlockChance}%");
        }
        else if (eq is Pickaxe pick)
        {
            // Bundle 10 — pickaxe detail emphasizes mining stats; durability appended below as N/M for context.
            if (pick.MiningPower != 0) parts.Add($"Power: +{pick.MiningPower}");
            if (pick.OreQualityBonus != 0) parts.Add($"Quality: +{pick.OreQualityBonus}%");
        }

        var bonuses = new List<string>();
        foreach (var effect in eq.Bonuses.Effects)
        {
            string sign = effect.Potency >= 0 ? "+" : "";
            bonuses.Add($"{ShortStatName(effect.Type)} {sign}{effect.Potency}");
        }
        if (bonuses.Count > 0) parts.Add(string.Join(", ", bonuses));
        // Bundle 10 — Pickaxe shows N/M with critical/low tag; other gear keeps the legacy single-number DUR.
        if (eq is Pickaxe pickDur)
        {
            int max = pickDur.MaxDurability > 0 ? pickDur.MaxDurability : Math.Max(1, pickDur.ItemDurability);
            int cur = Math.Clamp(pickDur.ItemDurability, 0, max);
            double pct = (double)cur / max;
            string tag = cur <= 0 ? "BROKEN" : pct < 0.25 ? "CRITICAL" : pct < 0.50 ? "Low" : "Good";
            parts.Add(cur <= 0 ? "DUR: BROKEN" : $"DUR: {cur}/{max} ({tag})");
        }
        else parts.Add(eq.ItemDurability <= 0 ? "DUR: BROKEN" : $"DUR: {eq.ItemDurability}");
        return string.Join("  |  ", parts);
    }

    // Sums all equipment bonuses and formats a compact summary.
    internal static string BuildStatTotals(Player player)
    {
        var inv = player.Inventory;
        var stats = new (string Label, StatType Type)[]
        {
            ("ATK", StatType.Attack), ("DEF", StatType.Defense),
            ("SPD", StatType.Speed), ("STR", StatType.Strength),
            ("VIT", StatType.Vitality), ("DEX", StatType.Dexterity),
            ("AGI", StatType.Agility),
        };

        var parts = new List<string>();
        foreach (var (label, type) in stats)
        {
            int val = inv.GetTotalEquipmentBonus(type);
            if (val != 0) parts.Add($"{label} +{val}");
        }
        return parts.Count > 0 ? $"Gear: {string.Join("  ", parts)}" : "Gear: (none)";
    }

    // Finds the lowest-durability equipped item and formats a warning.
    internal static string BuildDurabilitySummary(Player player)
    {
        int lowestDur = int.MaxValue;
        string? lowestName = null;

        foreach (var (slot, _, _, _) in EquipmentSlotView.DefaultSlotLayout)
        {
            var eq = player.Inventory.GetEquipped(slot);
            if (eq != null && eq.ItemDurability < lowestDur)
            {
                lowestDur = eq.ItemDurability;
                lowestName = eq.Name;
            }
        }

        if (lowestName == null) return "";
        bool critical = lowestDur <= 12;
        string tag = critical ? "[LOW] " : "";
        string hint = critical ? " — repair soon!" : "";
        return $"{tag}Lowest durability: {lowestName} ({lowestDur}){hint}";
    }

    private static string BuildUnequipPreview(EquipmentBase eq)
    {
        var losses = new List<string>();
        if (eq is Weapon w && w.BaseDamage > 0) losses.Add($"ATK -{w.BaseDamage}");
        if (eq is Armor a && a.BaseDefense > 0) losses.Add($"DEF -{a.BaseDefense}");

        foreach (var effect in eq.Bonuses.Effects)
            if (effect.Potency != 0) losses.Add($"{ShortStatName(effect.Type)} -{Math.Abs(effect.Potency)}");

        string statPart = losses.Count > 0 ? string.Join("  ", losses) : "no stat change";
        return $"Unequip: {statPart}  (item returns to inventory)";
    }

    internal static string ShortStatName(StatType type) => StatFormatter.Short(type);
}
