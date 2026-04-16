using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all two-handed sword weapons.
// Two-Handed Swords: very high damage, slow speed. Strength-oriented.
public static class TwoHandedSwordDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Two-Handed Sword",
            BaseDamage = baseDmg, AttackSpeed = 3, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronGreatsword() => Make("iron_greatsword", "Iron Greatsword", 120, "Common", 55, 1, 15,
        B().Add(StatType.Attack, 12).Add(StatType.Strength, 4));

    public static Weapon CreateSteelGreatsword() => Make("steel_greatsword", "Steel Greatsword", 300, "Uncommon", 80, 10, 35,
        B().Add(StatType.Attack, 22).Add(StatType.Strength, 10));

    public static Weapon CreateMythrilGreatsword() => Make("mythril_greatsword", "Mythril Greatsword", 900, "Rare", 120, 25, 60,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 12));

    public static Weapon CreateAdamantiteGreatsword() => Make("adamantite_greatsword", "Adamantite Greatsword", 3000, "Epic", 180, 50, 100,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 20));

    public static Weapon CreateCelestialGreatsword() => Make("celestial_greatsword", "Celestial Greatsword", 7000, "Legendary", 230, 75, 165,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 28).Add(StatType.Agility, 8));

    public static Weapon CreateTyrantDragon() => Make("tyrant_dragon", "Tyrant Dragon", 1500, "Epic", 110, 30, 55,
        B().Add(StatType.Attack, 30).Add(StatType.Strength, 14), "PostMotion-1");

    public static Weapon CreateAscalon() => Make("ascalon", "Ascalon", 22000, "Legendary", 250, 80, 180,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 30), "SkillDamage+30");

    // F3 Integral Factor field boss drop. Veined green steel.
    public static Weapon CreateVerdantLord() => Make("verdant_lord", "Verdant Lord", 2600, "Rare", 140, 7, 42,
        B().Add(StatType.Attack, 22).Add(StatType.Strength, 10).Add(StatType.Vitality, 6), "CritHeal+8");
}
