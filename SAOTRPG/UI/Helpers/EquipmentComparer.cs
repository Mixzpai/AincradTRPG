using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI.Helpers;

/// <summary>
/// Builds side-by-side stat comparison strings for equipment items.
/// Used in the inventory dialog to show what you'd gain/lose by equipping an item.
/// </summary>
public static class EquipmentComparer
{
    /// <summary>
    /// Compares an inventory equipment item against whatever is currently
    /// equipped in the matching slot. Returns a string like "ATK: 5 → 12 (+7)  DEF: 3 → 2 (-1)".
    /// Returns empty string if no comparison is possible.
    /// </summary>
    public static string BuildComparison(Player player, EquipmentBase item)
    {
        var slot = ResolveSlot(item);
        if (slot == null) return "";

        var equipped = player.Inventory.GetEquipped(slot.Value);

        // Get stat bonuses from the new item
        var newStats = SumStats(item);

        // Get stat bonuses from the currently equipped item (or zeros)
        var oldStats = equipped != null ? SumStats(equipped) : new Dictionary<StatType, int>();

        // Build diff strings for each stat that differs
        var parts = new List<string>();

        foreach (var (stat, newVal) in newStats)
        {
            int oldVal = oldStats.GetValueOrDefault(stat, 0);
            if (newVal != oldVal)
            {
                int diff = newVal - oldVal;
                string sign = diff > 0 ? $"+{diff}" : $"{diff}";
                string label = ShortStatName(stat);
                parts.Add($"{label}: {oldVal}→{newVal} ({sign})");
            }
        }

        // Check for stats the old item had that the new one doesn't
        if (equipped != null)
        {
            foreach (var (stat, oldVal) in oldStats)
            {
                if (oldVal != 0 && !newStats.ContainsKey(stat))
                {
                    string label = ShortStatName(stat);
                    parts.Add($"{label}: {oldVal}→0 (-{oldVal})");
                }
            }
        }

        // Durability comparison
        int newDur = item.ItemDurability;
        int oldDur = equipped?.ItemDurability ?? 0;
        if (equipped != null)
            parts.Add($"DUR: {oldDur}→{newDur}");

        return parts.Count > 0 ? string.Join("  ", parts) : equipped == null ? "(slot empty)" : "";
    }

    /// <summary>
    /// Resolves which EquipmentSlot an item belongs in based on its type properties.
    /// Simplified version — doesn't need the full slot resolver.
    /// </summary>
    private static EquipmentSlot? ResolveSlot(EquipmentBase item)
    {
        if (item is Weapon) return EquipmentSlot.Weapon;
        if (item is Armor armor)
        {
            return armor.ArmorSlot?.ToLowerInvariant() switch
            {
                "chest"   => EquipmentSlot.Chest,
                "helmet"  => EquipmentSlot.Head,
                "boots"   => EquipmentSlot.Feet,
                "shield"  => EquipmentSlot.OffHand,
                "legs"    => EquipmentSlot.Legs,
                _         => null
            };
        }
        return null;
    }

    /// <summary>Sums all stat bonuses from an equipment item's effects.</summary>
    private static Dictionary<StatType, int> SumStats(EquipmentBase item)
    {
        var stats = new Dictionary<StatType, int>();
        foreach (var effect in item.Bonuses.Effects)
        {
            stats.TryGetValue(effect.Type, out int current);
            stats[effect.Type] = current + effect.Potency;
        }
        return stats;
    }

    /// <summary>Short display names for stat types.</summary>
    private static string ShortStatName(StatType type) => type switch
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
