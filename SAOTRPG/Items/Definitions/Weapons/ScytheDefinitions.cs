using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all scythe weapons.
// Scythes: heavy reach weapons. Slow but long range (2 tiles) and high
// per-hit damage. Strength-scaling. Canon in SAO Last Recollection (Dorothy's
// signature weapon class). Death-associated flavor, often HP-cost skill kits.
public static class ScytheDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Scythe",
            BaseDamage = baseDmg, AttackSpeed = 0, Range = 2,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronScythe() => Make("iron_scythe", "Iron Scythe", 100, "Common", 55, 1, 13,
        B().Add(StatType.Attack, 10).Add(StatType.Strength, 3));

    public static Weapon CreateSteelScythe() => Make("steel_scythe", "Steel Scythe", 250, "Uncommon", 80, 10, 32,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8));

    public static Weapon CreateMythrilScythe() => Make("mythril_scythe", "Mythril Scythe", 800, "Rare", 110, 25, 55,
        B().Add(StatType.Attack, 32).Add(StatType.Strength, 14).Add(StatType.Vitality, 5));

    public static Weapon CreateAdamantiteScythe() => Make("adamantite_scythe", "Adamantite Scythe", 2500, "Epic", 160, 50, 95,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 22).Add(StatType.Vitality, 10));

    public static Weapon CreateCelestialScythe() => Make("celestial_scythe", "Celestial Scythe", 6000, "Legendary", 210, 75, 155,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 28).Add(StatType.Agility, 10));

    // ── Hollow Area Uniques (Scythe) ─────────────────────────────

    // HF F70 Hollow Area. Reaper Scythe — hollow grim warscythe, dark bleed affinity.
    public static Weapon CreateReaperScythe() => Make("scy_reaper_scythe", "Reaper Scythe", 11500, "Epic", 180, 65, 120,
        B().Add(StatType.Attack, 58).Add(StatType.Strength, 22).Add(StatType.Dexterity, 14), "Bleed+25");

    // ── Lisbeth Rarity 6 Crafted (Scythe) ─────────────────────────

    public static Weapon CreateEldarkRadiusSigma() => Make("scy_eldark_radius_sigma", "Eldark Radius Sigma", 31000, "Legendary", 265, 89, 178,
        B().Add(StatType.Attack, 88).Add(StatType.Strength, 28).Add(StatType.Dexterity, 18), "DarknessRending+25");
}
