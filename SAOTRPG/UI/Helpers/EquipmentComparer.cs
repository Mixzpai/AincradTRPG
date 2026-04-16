using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI.Helpers;

// Builds side-by-side stat comparison strings for equipment items.
// Used in the inventory dialog to show what you'd gain/lose by equipping an item.
public static class EquipmentComparer
{
    // Compares an inventory equipment item against whatever is currently
    // equipped in the matching slot. Returns a string like "ATK: 5→12 (+7)  DEF: 3→2 (-1)".
    // Appends ▲ UPGRADE / ▼ DOWNGRADE verdict based on net stat diff.
    // Returns empty string if no comparison is possible.
    public static string BuildComparison(Player player, EquipmentBase item)
    {
        var slot = ResolveSlot(item);
        if (slot == null) return "";

        var equipped = player.Inventory.GetEquipped(slot.Value);

        var newStats = SumStats(item);
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
        if (equipped != null)
            parts.Add($"DUR: {equipped.ItemDurability}→{item.ItemDurability}");

        if (parts.Count == 0)
            return equipped == null ? "(slot empty)" : "";

        string comparison = string.Join("  ", parts);

        int netDiff = CalcNetDiff(newStats, oldStats);
        string verdict = netDiff > 0 ? " ▲ UPGRADE" : netDiff < 0 ? " ▼ DOWNGRADE" : "";
        return comparison + verdict;
    }

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

    // Short display names for stat types — delegates to shared StatFormatter.
    private static string ShortStatName(StatType type) => StatFormatter.Short(type);
}
