using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using PlayerInventory = SAOTRPG.Inventory.Core.Inventory;

namespace SAOTRPG.Systems;

// IF-canon Refinement. API for CraftingDialog.ShowRefineMenu + damage calc via
// Equipment.Bonuses. Override-only (occupied = destroy old); Divine cannot refine.
public static class Refinement
{
    // Refinement Ingot cost per socket by rarity (caller pre-consumes N-1; Socket
    // consumes the final 1). Divine=0 (Socket early-returns).
    public static int CostForRarity(string? rarity) => rarity switch
    {
        "Common"    => 1,
        "Uncommon"  => 1,
        "Rare"      => 2,
        "Epic"      => 3,
        "Legendary" => 5,
        "Divine"    => 0, // can't refine at all
        _           => 1,
    };

    // Socket ingot into slot 0..2 (override; Divine forbidden). Consumes 1 ingot;
    // if equipped, Unequip/Re-Equip around Bonuses (matches ApplyEnhancementDelta).
    public static bool Socket(EquipmentBase eq, int slotIdx, string ingotDefId,
                              PlayerInventory inv, IStatModifiable? target = null)
    {
        if (eq == null || inv == null) return false;
        if (eq.Rarity == "Divine") return false;
        if (slotIdx < 0 || slotIdx >= EquipmentBase.RefinementSlotCount) return false;
        if (string.IsNullOrEmpty(ingotDefId)) return false;

        if (inv.CountByDefinitionId(ingotDefId) < 1) return false;
        if (ItemRegistry.Create(ingotDefId) is not Ingot template) return false;

        // Mirror enhancement pattern: Unequip before Bonuses mutation, re-Equip after.
        bool wasEquippedOnTarget = target != null && IsEquippedOn(eq, inv);
        if (wasEquippedOnTarget) eq.Unequip(target!);

        UnapplySlot(eq, slotIdx);

        // Consume ingot; on failure, re-equip and bail.
        if (!inv.ConsumeByDefinitionId(ingotDefId, 1))
        {
            if (wasEquippedOnTarget) eq.Equip(target!);
            return false;
        }
        eq.RefinementSlots[slotIdx] = ingotDefId;
        ApplyIngotBonuses(eq, template);

        if (wasEquippedOnTarget) eq.Equip(target!);

        // Invalidate cache so Player.Attack picks up the new socket.
        inv.InvalidateStatCache();
        return true;
    }

    // True if `eq` is currently in any of `inv`'s equipment slots.
    private static bool IsEquippedOn(EquipmentBase eq, PlayerInventory inv)
    {
        foreach (EquipmentSlot s in Enum.GetValues(typeof(EquipmentSlot)))
            if (ReferenceEquals(inv.GetEquipped(s), eq)) return true;
        return false;
    }

    // Preview summary: flat (StatType, potency) list + display string like
    // "Attack +25, Defense -5".
    public static (List<(StatType Stat, int Value)> Stats, string DisplayText)
        GetBonusSummary(EquipmentBase eq)
    {
        var totals = new Dictionary<StatType, int>();
        if (eq != null)
        {
            for (int i = 0; i < EquipmentBase.RefinementSlotCount; i++)
            {
                var ingot = GetSlotIngot(eq, i);
                if (ingot == null) continue;
                Add(totals, ingot.PrimaryStat,   ingot.PrimaryBonus);
                Add(totals, ingot.SecondaryStat, ingot.SecondaryBonus);
                if (ingot.ThirdStat.HasValue)  Add(totals, ingot.ThirdStat.Value,  ingot.ThirdBonus);
                if (ingot.FourthStat.HasValue) Add(totals, ingot.FourthStat.Value, ingot.FourthBonus);
            }
        }
        var list = totals.Select(kv => (kv.Key, kv.Value)).ToList();
        string display = list.Count == 0
            ? "(no refinements)"
            : string.Join(", ", list.Select(t => $"{t.Key} {(t.Value >= 0 ? "+" : "")}{t.Value}"));
        return (list, display);
    }

    // Debug/telemetry: sum ingot bonuses across equipped slots.
    // Damage-calc path uses EquipmentBase.Bonuses folding instead.
    public static Dictionary<StatType, int> SumEquipmentBonuses(PlayerInventory inv)
    {
        var totals = new Dictionary<StatType, int>();
        if (inv == null) return totals;
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            var eq = inv.GetEquipped(slot);
            if (eq == null) continue;
            foreach (var (stat, val) in GetBonusSummary(eq).Stats)
                Add(totals, stat, val);
        }
        return totals;
    }

    // ── Re-hydration helpers used by SaveManager on load ─────────────────

    // Re-apply slotted ingot bonuses post-load. Call ONCE after EnhancementLevel replay.
    internal static void RehydrateBonuses(EquipmentBase eq)
    {
        for (int i = 0; i < EquipmentBase.RefinementSlotCount; i++)
        {
            var ingot = GetSlotIngot(eq, i);
            if (ingot != null) ApplyIngotBonuses(eq, ingot);
        }
    }

    internal static Ingot? GetSlotIngot(EquipmentBase eq, int slotIdx)
    {
        if (eq == null) return null;
        if (slotIdx < 0 || slotIdx >= EquipmentBase.RefinementSlotCount) return null;
        string? defId = eq.RefinementSlots[slotIdx];
        if (string.IsNullOrEmpty(defId)) return null;
        return ItemRegistry.Create(defId) as Ingot;
    }

    // ── Internal stat-folding implementation ─────────────────────────────

    private static void ApplyIngotBonuses(EquipmentBase eq, Ingot ingot)
    {
        eq.Bonuses.Add(ingot.PrimaryStat,   ingot.PrimaryBonus);
        eq.Bonuses.Add(ingot.SecondaryStat, ingot.SecondaryBonus);
        if (ingot.ThirdStat.HasValue)  eq.Bonuses.Add(ingot.ThirdStat.Value,  ingot.ThirdBonus);
        if (ingot.FourthStat.HasValue) eq.Bonuses.Add(ingot.FourthStat.Value, ingot.FourthBonus);
    }

    // Remove socketed ingot's bonuses; sets slot = null. Used on override.
    private static void UnapplySlot(EquipmentBase eq, int slotIdx)
    {
        var oldIngot = GetSlotIngot(eq, slotIdx);
        if (oldIngot == null)
        {
            eq.RefinementSlots[slotIdx] = null;
            return;
        }
        RemoveFirst(eq.Bonuses, oldIngot.PrimaryStat,   oldIngot.PrimaryBonus);
        RemoveFirst(eq.Bonuses, oldIngot.SecondaryStat, oldIngot.SecondaryBonus);
        if (oldIngot.ThirdStat.HasValue)
            RemoveFirst(eq.Bonuses, oldIngot.ThirdStat.Value,  oldIngot.ThirdBonus);
        if (oldIngot.FourthStat.HasValue)
            RemoveFirst(eq.Bonuses, oldIngot.FourthStat.Value, oldIngot.FourthBonus);
        eq.RefinementSlots[slotIdx] = null;
    }

    // Remove first StatEffect matching (type, potency). Undoes exactly what
    // ApplyIngotBonuses added without touching base/enhancement bonuses.
    private static void RemoveFirst(StatModifierCollection coll, StatType type, int potency)
    {
        for (int i = 0; i < coll.Effects.Count; i++)
        {
            var e = coll.Effects[i];
            if (e.Type == type && e.Potency == potency && e.Duration == 0 && !e.IsPercentage)
            {
                coll.Effects.RemoveAt(i);
                return;
            }
        }
    }

    private static void Add(Dictionary<StatType, int> totals, StatType stat, int val)
    {
        if (val == 0) return;
        totals[stat] = totals.GetValueOrDefault(stat) + val;
    }
}
