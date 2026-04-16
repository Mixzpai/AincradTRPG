using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

// Static registry of all accessories.
public static class AccessoryDefinitions
{
    private static Accessory Make(string id, string name, int value, string rarity,
        int requiredLevel, string slot, int maxEquipped,
        StatModifierCollection bonuses, int durability = 100)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = requiredLevel,
            EquipmentType = "Accessory", AccessorySlot = slot,
            MaxEquipped = maxEquipped, Bonuses = bonuses,
        };

    public static Accessory CreateRingOfStrength() => Make("ring_of_strength", "Ring of Strength",
        500, "Uncommon", 10, "Ring", 2,
        new StatModifierCollection().Add(StatType.Strength, 10).Add(StatType.Attack, 5));

    public static Accessory CreateAgilityNecklace() => Make("agility_necklace", "Amulet of Agility",
        450, "Uncommon", 8, "Necklace", 1,
        new StatModifierCollection().Add(StatType.Agility, 12).Add(StatType.Speed, 5));

    public static Accessory CreateGuardianRing() => Make("guardian_ring", "Guardian Ring",
        400, "Uncommon", 5, "Ring", 2,
        new StatModifierCollection().Add(StatType.Defense, 8).Add(StatType.Vitality, 5));

    public static Accessory CreateScholarsPendant() => Make("scholars_pendant", "Scholar's Pendant",
        550, "Rare", 8, "Necklace", 1,
        new StatModifierCollection().Add(StatType.Intelligence, 10).Add(StatType.SkillDamage, 5));

    public static Accessory CreateSwiftBand() => Make("swift_band", "Swift Band",
        380, "Uncommon", 6, "Ring", 2,
        new StatModifierCollection().Add(StatType.Speed, 8).Add(StatType.Agility, 6));

    public static Accessory CreateVitalityCharm() => Make("vitality_charm", "Vitality Charm",
        600, "Rare", 12, "Necklace", 1,
        new StatModifierCollection().Add(StatType.Vitality, 8));
}
