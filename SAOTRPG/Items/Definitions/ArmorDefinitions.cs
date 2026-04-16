using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

// Static registry of all armor.
public static class ArmorDefinitions
{
    private static Armor Make(string id, string name, int value, string rarity,
        int durability, int requiredLevel, string slot, int baseDefense, int weight,
        StatModifierCollection bonuses, int blockChance = 0)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = requiredLevel,
            EquipmentType = "Armor", ArmorSlot = slot,
            BaseDefense = baseDefense, Weight = weight,
            BlockChance = blockChance, Bonuses = bonuses,
        };

    public static Armor CreateLeatherChest() => Make("leather_chestplate", "Leather Chestplate",
        80, "Common", 40, 1, "Chest", 5, 5,
        new StatModifierCollection().Add(StatType.Defense, 5).Add(StatType.Vitality, 2));

    public static Armor CreateIronHelmet() => Make("iron_helmet", "Iron Helmet",
        120, "Common", 50, 5, "Helmet", 8, 8,
        new StatModifierCollection().Add(StatType.Defense, 8));

    // ── Tier 2: Steel (Floors 10-25) ─────────────────────────────────

    public static Armor CreateSteelChest() => Make("steel_chestplate", "Steel Chestplate",
        300, "Uncommon", 80, 10, "Chest", 15, 7,
        new StatModifierCollection().Add(StatType.Defense, 15).Add(StatType.Vitality, 5));

    public static Armor CreateSteelHelmet() => Make("steel_helmet", "Steel Helmet",
        250, "Uncommon", 70, 10, "Helmet", 12, 6,
        new StatModifierCollection().Add(StatType.Defense, 12).Add(StatType.Endurance, 3));

    public static Armor CreateSteelBoots() => Make("steel_boots", "Steel Greaves",
        220, "Uncommon", 60, 10, "Boots", 10, 4,
        new StatModifierCollection().Add(StatType.Defense, 10).Add(StatType.Agility, 3));

    // ── Tier 3: Mythril (Floors 25-50) ───────────────────────────────

    public static Armor CreateMythrilChest() => Make("mythril_chestplate", "Mythril Chestplate",
        900, "Rare", 120, 25, "Chest", 30, 5,
        new StatModifierCollection().Add(StatType.Defense, 30).Add(StatType.Vitality, 10).Add(StatType.Speed, 3));

    public static Armor CreateMythrilHelmet() => Make("mythril_helmet", "Mythril Helm",
        750, "Rare", 100, 25, "Helmet", 24, 4,
        new StatModifierCollection().Add(StatType.Defense, 24).Add(StatType.Endurance, 6));

    public static Armor CreateMythrilBoots() => Make("mythril_boots", "Mythril Sabatons",
        650, "Rare", 90, 25, "Boots", 20, 3,
        new StatModifierCollection().Add(StatType.Defense, 20).Add(StatType.Agility, 8).Add(StatType.Speed, 5));

    // ── Tier 4: Adamantite (Floors 50-75) ────────────────────────────

    public static Armor CreateAdamantiteChest() => Make("adamantite_chestplate", "Adamantite Breastplate",
        2800, "Epic", 180, 50, "Chest", 55, 6,
        new StatModifierCollection().Add(StatType.Defense, 55).Add(StatType.Vitality, 18).Add(StatType.Endurance, 10));

    public static Armor CreateAdamantiteHelmet() => Make("adamantite_helmet", "Adamantite Helm",
        2200, "Epic", 150, 50, "Helmet", 42, 5,
        new StatModifierCollection().Add(StatType.Defense, 42).Add(StatType.Endurance, 8));

    public static Armor CreateAdamantiteBoots() => Make("adamantite_boots", "Adamantite Sabatons",
        1900, "Epic", 130, 50, "Boots", 35, 4,
        new StatModifierCollection().Add(StatType.Defense, 35).Add(StatType.Agility, 14).Add(StatType.Speed, 8));

    // ── Tier 5: Celestial (Floors 75-100) ────────────────────────────

    public static Armor CreateCelestialChest() => Make("celestial_chestplate", "Celestial Cuirass",
        7000, "Epic", 250, 75, "Chest", 90, 5,
        new StatModifierCollection().Add(StatType.Defense, 90).Add(StatType.Vitality, 25).Add(StatType.Endurance, 15).Add(StatType.Speed, 5));

    public static Armor CreateCelestialHelmet() => Make("celestial_helmet", "Celestial Crown",
        5500, "Epic", 200, 75, "Helmet", 70, 3,
        new StatModifierCollection().Add(StatType.Defense, 70).Add(StatType.Endurance, 12).Add(StatType.Intelligence, 8));

    public static Armor CreateCelestialBoots() => Make("celestial_boots", "Celestial Greaves",
        4800, "Epic", 180, 75, "Boots", 58, 3,
        new StatModifierCollection().Add(StatType.Defense, 58).Add(StatType.Agility, 20).Add(StatType.Speed, 12));
}
