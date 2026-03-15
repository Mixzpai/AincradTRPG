using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all accessories.
/// </summary>
public static class AccessoryDefinitions
{
    public static Accessory CreateRingOfStrength() => new()
    {
        DefinitionId = "ring_of_strength",
        Name = "Ring of Strength",
        Value = 500,
        Rarity = "Uncommon",
        ItemDurability = 100,
        RequiredLevel = 10,
        EquipmentType = "Accessory",
        AccessorySlot = "Ring",
        MaxEquipped = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Strength, 10)
            .Add(StatType.Attack, 5)
    };

    public static Accessory CreateAgilityNecklace() => new()
    {
        DefinitionId = "agility_necklace",
        Name = "Amulet of Agility",
        Value = 450,
        Rarity = "Uncommon",
        ItemDurability = 100,
        RequiredLevel = 8,
        EquipmentType = "Accessory",
        AccessorySlot = "Necklace",
        MaxEquipped = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Agility, 12)
            .Add(StatType.Speed, 5)
    };
}