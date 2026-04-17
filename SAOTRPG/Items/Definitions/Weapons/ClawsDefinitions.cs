using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all claw weapons.
// Claws: dual-fisted slashing weapons. Low per-hit damage but very fast
// attacks, agility-scaling. Canon in SAO Lost Song (Salamander/Gnome/Cait Sith
// martial style). Natural Agility + lower Durability than traditional weapons.
public static class ClawsDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Claws",
            BaseDamage = baseDmg, AttackSpeed = 3, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronClaws() => Make("iron_claws", "Iron Claws", 100, "Common", 40, 1, 7,
        B().Add(StatType.Attack, 5).Add(StatType.Agility, 4));

    public static Weapon CreateSteelClaws() => Make("steel_claws", "Steel Claws", 250, "Uncommon", 60, 10, 18,
        B().Add(StatType.Attack, 10).Add(StatType.Agility, 7));

    public static Weapon CreateMythrilClaws() => Make("mythril_claws", "Mythril Claws", 800, "Rare", 85, 25, 32,
        B().Add(StatType.Attack, 18).Add(StatType.Agility, 12).Add(StatType.Dexterity, 5));

    public static Weapon CreateAdamantiteClaws() => Make("adamantite_claws", "Adamantite Claws", 2500, "Epic", 130, 50, 58,
        B().Add(StatType.Attack, 32).Add(StatType.Agility, 20).Add(StatType.Strength, 8));

    public static Weapon CreateCelestialClaws() => Make("celestial_claws", "Celestial Claws", 6000, "Legendary", 175, 75, 95,
        B().Add(StatType.Attack, 52).Add(StatType.Agility, 28).Add(StatType.Dexterity, 12));
}
