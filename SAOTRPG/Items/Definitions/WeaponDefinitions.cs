using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all weapons.
/// </summary>
public static class WeaponDefinitions
{
    public static Weapon CreateIronSword() => new()
    {
        Name = "Iron Sword",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 50,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "Sword",
        BaseDamage = 10,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 8)
    };

    public static Weapon CreateSteelSword() => new()
    {
        Name = "Steel Sword",
        Value = 250,
        Rarity = "Uncommon",
        ItemDurability = 75,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "Sword",
        BaseDamage = 25,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 15)
            .Add(StatType.Strength, 5)
    };
}