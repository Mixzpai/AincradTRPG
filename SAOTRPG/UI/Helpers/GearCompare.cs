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

    // Bundle 13 (Item 10) — multi-line diff for the bottom 4-row compare panel.
    // Returns up to 4 lines: (1) header "vs <equipped name>" or "(no equipped)";
    // (2) base + stat deltas; (3) special effects diff; (4) net verdict tag.
    // Returns empty array when slot has no equipped counterpart and item is not EquipmentBase
    // — caller hides the panel.
    public static string[] BuildMultiLineDiffForPlayer(Player player, BaseItem item)
    {
        if (item is not EquipmentBase eq) return Array.Empty<string>();
        EquipmentSlot? slot = ResolveSlot(eq);
        if (slot == null) return Array.Empty<string>();
        var equipped = player.Inventory.GetEquipped(slot.Value);
        if (equipped == null) return Array.Empty<string>();

        var lines = new List<string>(4);
        string equippedName = TextHelpers.Truncate(equipped.Name ?? "", 40);
        lines.Add($"vs equipped: {equippedName}");

        // Type-mismatch banner — render alone, no stat lines.
        if (eq is Weapon nw && equipped is Weapon ow
            && !string.IsNullOrEmpty(nw.WeaponType)
            && !string.IsNullOrEmpty(ow.WeaponType)
            && nw.WeaponType != ow.WeaponType)
        {
            lines.Add($"  weapon type mismatch: {ow.WeaponType} -> {nw.WeaponType}");
            return lines.ToArray();
        }
        if (eq is Armor na && equipped is Armor oa
            && !string.IsNullOrEmpty(na.ArmorSlot)
            && !string.IsNullOrEmpty(oa.ArmorSlot)
            && na.ArmorSlot != oa.ArmorSlot)
        {
            lines.Add($"  armor slot mismatch: {oa.ArmorSlot} -> {na.ArmorSlot}");
            return lines.ToArray();
        }

        var statDiff = BuildDiff(eq, equipped);
        lines.Add(string.IsNullOrEmpty(statDiff) ? "  (no change)" : "  " + statDiff);

        string special = BuildSpecialDiff(eq, equipped);
        if (!string.IsNullOrEmpty(special)) lines.Add("  " + special);

        var verdict = EquipmentComparer.GetVerdict(player, eq);
        string tag = verdict switch
        {
            EquipmentComparer.CompareResult.Upgrade   => "[UPGRADE]",
            EquipmentComparer.CompareResult.Downgrade => "[DOWNGRADE]",
            _                                          => "[SIDEGRADE]",
        };
        if (lines.Count < 4) lines.Add(tag);
        return lines.ToArray();
    }

    // Concise summary of special-effect deltas (e.g., "Bleed proc gained", "Lifesteal lost").
    // Sources from EquipmentBase.ParsedEffects so works for weapons + armor uniformly.
    private static string BuildSpecialDiff(EquipmentBase newEq, EquipmentBase oldEq)
    {
        var newSpecials = (newEq.ParsedEffects ?? Array.Empty<EquipmentSpecialEffect>())
            .Select(s => s.ToString() ?? "").Where(s => s.Length > 0).ToHashSet();
        var oldSpecials = (oldEq.ParsedEffects ?? Array.Empty<EquipmentSpecialEffect>())
            .Select(s => s.ToString() ?? "").Where(s => s.Length > 0).ToHashSet();
        var gained = newSpecials.Except(oldSpecials).ToList();
        var lost   = oldSpecials.Except(newSpecials).ToList();
        if (gained.Count == 0 && lost.Count == 0) return "";
        var parts = new List<string>();
        if (gained.Count > 0) parts.Add("+" + string.Join("/", gained.Select(s => TextHelpers.Truncate(s, 20))));
        if (lost.Count > 0)   parts.Add("-" + string.Join("/", lost.Select(s => TextHelpers.Truncate(s, 20))));
        return string.Join("  ", parts);
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
