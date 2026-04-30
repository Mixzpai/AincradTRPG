using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI.Helpers;

// Builds side-by-side stat comparison strings for equipment items.
// Used in the inventory dialog to show what you'd gain/lose by equipping an item.
public static class EquipmentComparer
{
    // Comparison verdict — used for color-coding in the UI.
    public enum CompareResult { Neutral, Upgrade, Downgrade }

    // Returns the verdict for the comparison without building the full string.
    // Cheaper than BuildComparison when only the color indicator is needed.
    public static CompareResult GetVerdict(Player player, EquipmentBase item)
    {
        var slot = ResolveSlot(item);
        if (slot == null) return CompareResult.Neutral;
        var equipped = player.Inventory.GetEquipped(slot.Value);
        if (equipped == null) return CompareResult.Upgrade;

        int netDiff = CalcNetDiff(SumStats(item), SumStats(equipped));
        return netDiff > 0 ? CompareResult.Upgrade : netDiff < 0 ? CompareResult.Downgrade : CompareResult.Neutral;
    }

    // Calculates the net stat difference between new and old stat dictionaries.
    // Positive = upgrade, negative = downgrade, zero = sidegrade.
    private static int CalcNetDiff(Dictionary<StatType, int> newStats, Dictionary<StatType, int> oldStats)
    {
        int netDiff = 0;
        foreach (var (stat, newVal) in newStats)
            netDiff += newVal - oldStats.GetValueOrDefault(stat, 0);
        foreach (var (stat, oldVal) in oldStats)
            if (!newStats.ContainsKey(stat)) netDiff -= oldVal;
        return netDiff;
    }

    // Resolves which EquipmentSlot an item belongs in based on its type properties.
    // Simplified version — doesn't need the full slot resolver.
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

    // Sums all stat bonuses from an equipment item's effects.
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
}
