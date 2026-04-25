using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.UI;
using PlayerInventory = SAOTRPG.Inventory.Core.Inventory;

namespace SAOTRPG.Systems;

// Bundle 9 — Divine Awakening. Sister Selka (F65) upgrades a Divine weapon up to
// ◈3, folding BaseDamage*15% per level into Bonuses.Attack (additive w/ Refinement).
public static class DivineAwakening
{
    // Awakening cap (◈1/◈2/◈3). Referenced by Weapon.CanAwaken.
    public const int MaxLevel = 3;

    // Flat percent-of-BaseDamage Attack bonus granted per awakening level.
    public const int DamagePercentPerLevel = 15;

    // Per-level material cost table. Keys are DefIds from ItemRegistry.
    // Lv1: mithril ingot x3 · Lv2: divine fragment x1 · Lv3: primordial shard x1.
    public static IReadOnlyDictionary<int, IReadOnlyDictionary<string, int>> CostPerLevel { get; }
        = new Dictionary<int, IReadOnlyDictionary<string, int>>
        {
            [1] = new Dictionary<string, int> { ["mithril_ingot"] = 3 },
            [2] = new Dictionary<string, int> { ["divine_fragment"] = 1 },
            [3] = new Dictionary<string, int> { ["primordial_shard"] = 1 },
        };

    // Flat Attack bonus the weapon currently contributes from awakening.
    // Lv0 → 0, Lv1 → 15% BaseDamage, Lv2 → 30%, Lv3 → 45%.
    public static int ComputeBonusAttack(Weapon weapon)
    {
        if (weapon == null) return 0;
        return weapon.BaseDamage * DamagePercentPerLevel * weapon.AwakeningLevel / 100;
    }

    // Bump AwakeningLevel by one: consume materials, fold delta into Bonuses.Attack
    // via Unequip/mutate/Re-Equip (mirror Refinement.Socket); cache invalidated so Player.Attack re-reads bonus.
    public static void Awaken(Weapon weapon, Player player)
    {
        if (weapon == null || player == null) return;
        if (!weapon.CanAwaken) return;

        int nextLevel = weapon.AwakeningLevel + 1;
        if (!CostPerLevel.TryGetValue(nextLevel, out var costs)) return;

        var inv = player.Inventory;
        foreach (var (defId, count) in costs)
            if (inv.CountByDefinitionId(defId) < count) return;

        bool wasEquipped = IsEquippedOn(weapon, inv);
        if (wasEquipped) weapon.Unequip(player);

        foreach (var (defId, count) in costs)
        {
            if (!inv.ConsumeByDefinitionId(defId, count))
            {
                if (wasEquipped) weapon.Equip(player);
                return;
            }
        }

        int oldBonus = ComputeBonusAttack(weapon);
        weapon.AwakeningLevel = nextLevel;
        int newBonus = ComputeBonusAttack(weapon);
        int delta = newBonus - oldBonus;
        if (delta != 0) weapon.Bonuses.Add(StatType.Attack, delta);

        if (wasEquipped) weapon.Equip(player);
        inv.InvalidateStatCache();

        // Bundle 13 — fire awakening banner + particle hook (Wave 2 consumes AwakeningParticleLevel).
        DivineObtainBanner.TriggerAwakening(weapon, nextLevel);
    }

    // True if `w` currently occupies any equipment slot in `inv`.
    private static bool IsEquippedOn(Weapon w, PlayerInventory inv)
    {
        foreach (EquipmentSlot s in Enum.GetValues(typeof(EquipmentSlot)))
            if (ReferenceEquals(inv.GetEquipped(s), w)) return true;
        return false;
    }
}
