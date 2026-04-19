using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using PlayerInventory = SAOTRPG.Inventory.Core.Inventory;

namespace SAOTRPG.Systems;

// IF-canon Refinement helper. Public API consumed by the UI layer
// (CraftingDialog.ShowRefineMenu) and by the damage-calc pipeline via
// Equipment.Bonuses folding. Override-only: socketing into an occupied slot
// destroys the previous ingot's effect and consumes one new ingot from
// inventory. Divine-rarity equipment cannot be refined (sealed endgame gear).
public static class Refinement
{
    // Per-rarity refinement material (Red Hot Ore) cost to socket ONE ingot.
    // Divine is intentionally 0 because Divine gear is sealed — Socket()
    // early-returns on Divine, so this cost is never paid.
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

    // Socket an ingot into the given refinement slot. Returns true on success.
    //   - Divine equipment cannot be refined (sealed endgame).
    //   - slotIdx must be 0..2.
    //   - If the slot is already occupied, the old ingot's bonuses are removed
    //     and destroyed (override-only — no return-to-inventory).
    //   - Consumes ONE ingot of ingotDefId from inv. Red Hot Ore mat cost is
    //     handled by the caller (CraftingDialog) via ConsumeByDefinitionId.
    //   - If `target` is non-null and `eq` is currently equipped on target,
    //     the helper Unequip/Re-Equips around the Bonuses mutation so the
    //     player's BaseAttack/BaseDefense stay consistent with Bonuses
    //     (same pattern CraftingDialog.ApplyEnhancementDelta uses).
    public static bool Socket(EquipmentBase eq, int slotIdx, string ingotDefId,
                              PlayerInventory inv, IStatModifiable? target = null)
    {
        if (eq == null || inv == null) return false;
        if (eq.Rarity == "Divine") return false;
        if (slotIdx < 0 || slotIdx >= EquipmentBase.RefinementSlotCount) return false;
        if (string.IsNullOrEmpty(ingotDefId)) return false;

        // Verify the ingot exists in inventory and is actually an Ingot.
        if (inv.CountByDefinitionId(ingotDefId) < 1) return false;
        if (ItemRegistry.Create(ingotDefId) is not Ingot template) return false;

        // If target is supplied and the equipment is worn on them, match the
        // enhancement pattern: Unequip before mutating Bonuses, re-Equip after.
        bool wasEquippedOnTarget = target != null && IsEquippedOn(eq, inv);
        if (wasEquippedOnTarget) eq.Unequip(target!);

        // Remove the old ingot's bonuses (if any) from the equipment.
        UnapplySlot(eq, slotIdx);

        // Consume 1 ingot from inventory and write the new slot.
        if (!inv.ConsumeByDefinitionId(ingotDefId, 1))
        {
            if (wasEquippedOnTarget) eq.Equip(target!);
            return false;
        }
        eq.RefinementSlots[slotIdx] = ingotDefId;
        ApplyIngotBonuses(eq, template);

        if (wasEquippedOnTarget) eq.Equip(target!);

        // Invalidate the cached equipment-bonus aggregate so Player.Attack
        // etc. see the new socket immediately.
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

    // Public summary for the preview UI. Returns the flat list of
    // (StatType, potency) tuples contributed by all currently-socketed ingots
    // on `eq`, plus a display string like "Attack +25, Defense -5".
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

    // Sum socketed ingot bonuses across all equipped slots (weapon, offhand,
    // armor pieces, accessories). Exposed for debug/telemetry. The normal
    // damage-calc path does NOT call this — ingot bonuses are folded into
    // each EquipmentBase.Bonuses directly (see ApplyIngotBonuses) so
    // Inventory.GetTotalEquipmentBonus picks them up automatically.
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

    // Re-apply all slotted ingots' bonuses to an equipment after it has been
    // reconstructed from save data. Call this ONCE per equipment after
    // EnhancementLevel has been replayed.
    internal static void RehydrateBonuses(EquipmentBase eq)
    {
        for (int i = 0; i < EquipmentBase.RefinementSlotCount; i++)
        {
            var ingot = GetSlotIngot(eq, i);
            if (ingot != null) ApplyIngotBonuses(eq, ingot);
        }
    }

    // Look up the Ingot template object for the DefId stored in a slot.
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

    // Remove the currently-socketed ingot's bonuses from eq.Bonuses. Used when
    // overriding an occupied slot. Mutates eq.RefinementSlots[slotIdx] → null.
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

    // Remove the first StatEffect matching (type, potency) from a collection.
    // This pairs with ApplyIngotBonuses' StatModifierCollection.Add calls so
    // that we undo exactly what we added without touching base equipment
    // bonuses or enhancement-level bonuses.
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
