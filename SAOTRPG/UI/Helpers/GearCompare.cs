using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI.Helpers;

// Side-by-side stat diff for Inventory/Shop/chest-loot. `+N` green / `-N` red per
// changed row; zero-deltas omitted; type-mismatch → banner only. Inline color markers.
public static class GearCompare
{
    // Inline markers for future richer renderers; current callers use plain text.
    // Kept ASCII-simple so log lines stay readable.
    private const string GainTag = "";
    private const string LossTag = "";

    // Builds a multi-line diff block suitable for a detail panel.
    // `equipped` may be null → all new stats show as gains.
    public static string BuildDiff(BaseItem? selected, BaseItem? equipped)
    {
        if (selected is not EquipmentBase newEq) return "";

        // Weapon-type mismatch: two weapons of different types produce
        // meaningless deltas. Show a banner line and skip per-stat diffs.
        if (newEq is Weapon newW && equipped is Weapon oldW
            && !string.IsNullOrEmpty(newW.WeaponType)
            && !string.IsNullOrEmpty(oldW.WeaponType)
            && newW.WeaponType != oldW.WeaponType)
        {
            return $"(weapon type mismatch: {oldW.WeaponType} -> {newW.WeaponType})";
        }

        // Armor slot mismatch parallels the weapon rule. Most UI call sites
        // already scope by slot but chests/shops can show cross-slot picks.
        if (newEq is Armor newA && equipped is Armor oldA
            && !string.IsNullOrEmpty(newA.ArmorSlot)
            && !string.IsNullOrEmpty(oldA.ArmorSlot)
            && newA.ArmorSlot != oldA.ArmorSlot)
        {
            return $"(armor slot mismatch: {oldA.ArmorSlot} -> {newA.ArmorSlot})";
        }

        var newStats = SumStats(newEq);
        var oldStats = equipped is EquipmentBase oldEq
            ? SumStats(oldEq) : new Dictionary<StatType, int>();

        var lines = new List<string>();

        // Base weapon damage / armor defense — not in Bonuses but visible stats.
        AddBaseDiff(lines, newEq, equipped);

        // Additive stat bonuses from Effects collection.
        foreach (var (stat, newVal) in newStats)
        {
            int oldVal = oldStats.GetValueOrDefault(stat, 0);
            if (newVal == oldVal) continue;
            int diff = newVal - oldVal;
            string tag = diff > 0 ? $"+{diff}" : diff.ToString();
            string label = StatFormatter.Short(stat);
            lines.Add($"{label} {tag}");
        }

        // Stats present on equipped but not on new — pure losses.
        if (equipped is EquipmentBase)
        {
            foreach (var (stat, oldVal) in oldStats)
            {
                if (oldVal == 0 || newStats.ContainsKey(stat)) continue;
                lines.Add($"{StatFormatter.Short(stat)} -{oldVal}");
            }
        }

        if (lines.Count == 0) return equipped == null ? "(new)" : "(no change)";
        return string.Join("  ", lines);
    }

    // Base-damage (weapons) or base-defense (armor) delta — treated like a
    // regular stat line so the caller only has to read BuildDiff's return.
    private static void AddBaseDiff(List<string> lines, EquipmentBase newEq, BaseItem? equipped)
    {
        if (newEq is Weapon w)
        {
            int oldBase = equipped is Weapon ow ? ow.BaseDamage : 0;
            int diff = w.BaseDamage - oldBase;
            if (diff != 0) lines.Add($"DMG {(diff > 0 ? "+" : "")}{diff}");
        }
        else if (newEq is Armor a)
        {
            int oldBase = equipped is Armor oa ? oa.BaseDefense : 0;
            int diff = a.BaseDefense - oldBase;
            if (diff != 0) lines.Add($"DEF {(diff > 0 ? "+" : "")}{diff}");
        }
    }

    // Sums all stat bonuses from an equipment item's effect collection.
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

    // Resolve the currently-equipped slot counterpart for `item` and diff.
    // Callers with the equipped reference can skip this and call BuildDiff directly.
    public static string BuildDiffForPlayer(Player player, BaseItem item)
    {
        if (item is not EquipmentBase eq) return "";
        EquipmentSlot? slot = ResolveSlot(eq);
        if (slot == null) return "";
        var equipped = player.Inventory.GetEquipped(slot.Value);
        return BuildDiff(eq, equipped);
    }

    // Mirror of EquipmentComparer.ResolveSlot — kept local to avoid a
    // reference churn during partial extraction.
    private static EquipmentSlot? ResolveSlot(EquipmentBase item)
    {
        if (item is Weapon) return EquipmentSlot.Weapon;
        if (item is Armor armor)
        {
            return armor.ArmorSlot?.ToLowerInvariant() switch
            {
                "chest"  => EquipmentSlot.Chest,
                "helmet" => EquipmentSlot.Head,
                "boots"  => EquipmentSlot.Feet,
                "shield" => EquipmentSlot.OffHand,
                "legs"   => EquipmentSlot.Legs,
                _        => null,
            };
        }
        return null;
    }
}
