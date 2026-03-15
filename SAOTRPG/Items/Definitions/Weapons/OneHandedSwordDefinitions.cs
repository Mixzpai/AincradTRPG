using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;


/// Static registry of all one-handed sword weapons.
public static class OneHandedSwordDefinitions
{
    public static Weapon CreateIronSword() => new()
    {
        DefinitionId = "iron_sword",
        Name = "Iron Sword",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 50,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 10,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 8)
    };

    public static Weapon CreateSteelSword() => new()
    {
        DefinitionId = "steel_sword",
        Name = "Steel Sword",
        Value = 250,
        Rarity = "Uncommon",
        ItemDurability = 75,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 25,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 15)
            .Add(StatType.Strength, 5)
    };

    // TODO: Add more one-handed swords here
}
