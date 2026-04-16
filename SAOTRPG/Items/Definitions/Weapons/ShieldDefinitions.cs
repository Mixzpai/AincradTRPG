using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all shield items.
// Shields are Armor (OffHand slot) with BlockChance.
// Defense/Vitality-oriented.
public static class ShieldDefinitions
{
    public static Armor CreateWoodenShield() => new()
    {
        DefinitionId = "wooden_shield",
        Name = "Wooden Shield",
        Value = 65,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 1,
        EquipmentType = "Armor",
        ArmorSlot = "Shield",
        BaseDefense = 4,
        Weight = 6,
        BlockChance = 10,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 4)
    };

    public static Armor CreateIronShield() => new()
    {
        DefinitionId = "iron_shield",
        Name = "Iron Shield",
        Value = 210,
        Rarity = "Uncommon",
        ItemDurability = 70,
        RequiredLevel = 8,
        EquipmentType = "Armor",
        ArmorSlot = "Shield",
        BaseDefense = 10,
        Weight = 10,
        BlockChance = 18,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 10)
            .Add(StatType.Vitality, 4)
    };
}
