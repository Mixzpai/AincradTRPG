using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Equipment overview dialog — shows all equipped gear at a glance.
/// Accessible via T key. Allows viewing stats and unequipping items.
/// </summary>
public static class EquipmentDialog
{
    private const int FixedHeight = 22;
    private const int MinWidth = 48;
    private const int MaxWidth = 80;

    public static void Show(Player player)
    {
        var slots = EquipmentSlotView.DefaultSlotLayout;

        // ── Measure content for dynamic width ───────────────────────────
        int maxLen = 20;
        foreach (var (slot, _, _, label) in slots)
        {
            var eq = player.Inventory.GetEquipped(slot);
            string itemText = eq != null ? $"{eq.Name} ({eq.Rarity})" : "(empty)";
            int lineLen = 4 + label.Length + 1 + itemText.Length;
            if (lineLen > maxLen) maxLen = lineLen;
        }
        int dialogWidth = Math.Clamp(maxLen + 10, MinWidth, MaxWidth);

        var dialog = new Dialog
        {
            Title = "Equipment",
            Width = dialogWidth,
            Height = FixedHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        // ── Custom slot list view with colored icons ────────────────────
        var slotView = new EquipmentSlotView(player, dialogWidth - 4)
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = EquipmentSlotView.SlotCount
        };

        // ── Divider ─────────────────────────────────────────────────────
        var divider = new Label
        {
            Text = new string('-', dialogWidth - 4),
            X = 0, Y = EquipmentSlotView.SlotCount,
            ColorScheme = ColorSchemes.Dim
        };

        // ── Detail label (item info for selected slot) ──────────────────
        var detailLabel = new Label
        {
            Text = "",
            X = 0, Y = EquipmentSlotView.SlotCount + 1,
            Width = Dim.Fill(),
            Height = 2,
            ColorScheme = ColorSchemes.Dialog
        };

        // ── Stat totals row ─────────────────────────────────────────────
        var statsLabel = new Label
        {
            Text = BuildStatTotals(player),
            X = 0, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(),
            ColorScheme = ColorSchemes.Gold
        };

        // ── Durability summary ──────────────────────────────────────────
        var durLabel = new Label
        {
            Text = BuildDurabilitySummary(player),
            X = 0, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            ColorScheme = ColorSchemes.Dim
        };

        // ── Buttons ─────────────────────────────────────────────────────
        var unequipBtn = new Button
        {
            Text = " Unequip ",
            X = 0, Y = Pos.AnchorEnd(2),
            ColorScheme = ColorSchemes.Button
        };
        var closeBtn = new Button
        {
            Text = " Close ",
            X = Pos.Right(unequipBtn) + 1, Y = Pos.AnchorEnd(2),
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };

        // ── Selection change — show item details ────────────────────────
        slotView.SelectedSlotChanged += (idx) =>
        {
            if (idx < 0 || idx >= slots.Length) { detailLabel.Text = ""; return; }
            var slot = slots[idx].Slot;
            var eq = player.Inventory.GetEquipped(slot);
            detailLabel.Text = eq != null ? BuildItemDetail(eq) : "No item equipped in this slot.";
        };

        // ── Unequip button ──────────────────────────────────────────────
        unequipBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int idx = slotView.SelectedIndex;
            if (idx < 0 || idx >= slots.Length) return;

            var slot = slots[idx].Slot;
            var eq = player.Inventory.GetEquipped(slot);
            if (eq == null) return;

            bool success = player.Inventory.Unequip(slot, player);
            if (success)
            {
                slotView.SetNeedsDraw();
                detailLabel.Text = "No item equipped in this slot.";
                statsLabel.Text = BuildStatTotals(player);
                durLabel.Text = BuildDurabilitySummary(player);
            }
        };

        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // ── Assemble ────────────────────────────────────────────────────
        dialog.Add(slotView, divider, detailLabel, statsLabel, durLabel,
            unequipBtn, closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }

    // ── Item detail string ──────────────────────────────────────────────
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

        var bonuses = new List<string>();
        foreach (var effect in eq.Bonuses.Effects)
        {
            string sign = effect.Potency >= 0 ? "+" : "";
            bonuses.Add($"{ShortStatName(effect.Type)} {sign}{effect.Potency}");
        }
        if (bonuses.Count > 0) parts.Add(string.Join(", ", bonuses));

        parts.Add($"DUR: {eq.ItemDurability}");

        return string.Join("  |  ", parts);
    }

    // ── Stat totals from all equipped gear ───────────────────────────────
    internal static string BuildStatTotals(Player player)
    {
        var inv = player.Inventory;
        var stats = new (string Label, StatType Type)[]
        {
            ("ATK", StatType.Attack),
            ("DEF", StatType.Defense),
            ("SPD", StatType.Speed),
            ("STR", StatType.Strength),
            ("VIT", StatType.Vitality),
            ("DEX", StatType.Dexterity),
            ("AGI", StatType.Agility),
        };

        var parts = new List<string>();
        foreach (var (label, type) in stats)
        {
            int val = inv.GetTotalEquipmentBonus(type);
            if (val != 0)
                parts.Add($"{label} +{val}");
        }

        return parts.Count > 0
            ? $"Gear: {string.Join("  ", parts)}"
            : "Gear: (none)";
    }

    // ── Durability summary ──────────────────────────────────────────────
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

        return lowestName != null
            ? $"Lowest durability: {lowestName} ({lowestDur})"
            : "";
    }

    // ── Short stat name ─────────────────────────────────────────────────
    internal static string ShortStatName(StatType type) => type switch
    {
        StatType.Attack       => "ATK",
        StatType.Defense      => "DEF",
        StatType.Speed        => "SPD",
        StatType.Health       => "HP",
        StatType.Strength     => "STR",
        StatType.Vitality     => "VIT",
        StatType.Endurance    => "END",
        StatType.Dexterity    => "DEX",
        StatType.Agility      => "AGI",
        StatType.Intelligence => "INT",
        _                     => type.ToString()
    };
}
