using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all spear weapons.
// Spears: moderate damage, reach (Range 2). Balanced stats.
public static class SpearDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Spear",
            BaseDamage = baseDmg, AttackSpeed = 1, Range = 2,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateWoodenSpear() => Make("wooden_spear", "Wooden Spear", 70, "Common", 40, 1, 9,
        B().Add(StatType.Attack, 7).Add(StatType.Dexterity, 2));

    public static Weapon CreateIronSpear() => Make("iron_spear", "Iron Spear", 230, "Uncommon", 65, 8, 21,
        B().Add(StatType.Attack, 13).Add(StatType.Dexterity, 5).Add(StatType.Strength, 3));

    // --- Tier 3 ---

    public static Weapon CreateMythrilSpear() => Make("mythril_spear", "Mythril Spear", 780, "Rare", 95, 25, 40,
        B().Add(StatType.Attack, 24).Add(StatType.Dexterity, 10).Add(StatType.Strength, 6));

    // --- Tier 4 ---

    public static Weapon CreateAdamantiteSpear() => Make("adamantite_spear", "Adamantite Spear", 2500, "Epic", 150, 50, 72,
        B().Add(StatType.Attack, 42).Add(StatType.Dexterity, 14).Add(StatType.Strength, 10));

    // --- Tier 5 ---

    public static Weapon CreateCelestialSpear() => Make("celestial_spear", "Celestial Spear", 5800, "Legendary", 200, 75, 120,
        B().Add(StatType.Attack, 65).Add(StatType.Dexterity, 20).Add(StatType.Strength, 15));

    // --- Named Weapons ---

    public static Weapon CreateGuiltyThorn() => Make("guilty_thorn", "Guilty Thorn", 800, "Epic", 80, 20, 30,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "SlowOnHit+30");

    public static Weapon CreateAnubisSpear() => Make("anubis_spear", "Anubis Spear", 1400, "Rare", 110, 35, 50,
        B().Add(StatType.Attack, 32).Add(StatType.Dexterity, 10));

    public static Weapon CreateCaladbolg() => Make("caladbolg", "Caladbolg", 18000, "Legendary", 250, 80, 135,
        B().Add(StatType.Attack, 72).Add(StatType.Dexterity, 22).Add(StatType.Strength, 18), "RushRange+1");
}
