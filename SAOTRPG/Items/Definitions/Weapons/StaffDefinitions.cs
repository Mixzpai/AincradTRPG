using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all staff weapons.
// Staves: low damage, intelligence-oriented. Boosts skill damage.
public static class StaffDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Staff",
            BaseDamage = baseDmg, AttackSpeed = 1, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateApprenticeStaff() => Make("apprentice_staff", "Apprentice Staff", 75, "Common", 35, 1, 5,
        B().Add(StatType.Attack, 3).Add(StatType.Intelligence, 6));

    public static Weapon CreateOakStaff() => Make("oak_staff", "Oak Staff", 220, "Uncommon", 55, 8, 12,
        B().Add(StatType.Attack, 8).Add(StatType.Intelligence, 10));

    // --- Tier 3 ---

    public static Weapon CreateMythrilStaff() => Make("mythril_staff", "Mythril Staff", 700, "Rare", 80, 25, 22,
        B().Add(StatType.Attack, 14).Add(StatType.Intelligence, 18));

    // --- Tier 4 ---

    public static Weapon CreateAdamantiteStaff() => Make("adamantite_staff", "Adamantite Staff", 2100, "Epic", 130, 50, 38,
        B().Add(StatType.Attack, 25).Add(StatType.Intelligence, 30));

    // --- Tier 5 ---

    public static Weapon CreateCelestialStaff() => Make("celestial_staff", "Celestial Staff", 5200, "Legendary", 175, 75, 60,
        B().Add(StatType.Attack, 40).Add(StatType.Intelligence, 45));
}
