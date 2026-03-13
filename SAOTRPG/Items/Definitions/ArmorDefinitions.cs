using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all armor.
/// </summary>
public static class ArmorDefinitions
{
    public static Armor CreateLeatherChest() => new()
    {
        Name = "Leather Chestplate",
        Value = 80,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Armor",
        ArmorSlot = "Chest",
        BaseDefense = 5,
        Weight = 5,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 5)
            .Add(StatType.Vitality, 2)
    };

    public static Armor CreateIronHelmet() => new()
    {
        Name = "Iron Helmet",
        Value = 120,
        Rarity = "Common",
        ItemDurability = 50,
        RequiredLevel = 5,
        EquipmentType = "Armor",
        ArmorSlot = "Helmet",
        BaseDefense = 8,
        Weight = 8,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 8)
    };
}