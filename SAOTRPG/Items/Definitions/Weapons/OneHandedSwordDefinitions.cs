using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all one-handed sword weapons.
/// </summary>
public static class OneHandedSwordDefinitions
{
    public static Weapon CreateBronzeSword() => new()
    {
        Name = "Bronze Sword",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 65,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnnealBlade() => new()
    {
        Name = "Anneal Blade",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAzureSkyBlade() => new()
    {
        Name = "Azure Sky Blade",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBrassSword() => new()
    {
        Name = "Brass Sword",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSteelSword() => new()
    {
        Name = "Steel Sword",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateStoutBrand() => new()
    {
        Name = "Stout Brand",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 140,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSwordOfEventide() => new()
    {
        Name = "Sword Of Eventide",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGreenishSword() => new()
    {
        Name = "Greenish Sword",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 170,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateElvenStoutSword() => new()
    {
        Name = "Elven Stout Sword",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateUnbreakable() => new()
    {
        Name = "Unbreakable",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 200,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateInnovation() => new()
    {
        Name = "Innovation",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateImperial() => new()
    {
        Name = "Imperial",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLordSword() => new()
    {
        Name = "Lord Sword",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateTwinBlade() => new()
    {
        Name = "Twin Blade",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 250,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSageSword() => new()
    {
        Name = "Säge Sword",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 66,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePlateBlade() => new()
    {
        Name = "Plate Blade",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSacredSword() => new()
    {
        Name = "Sacred Sword",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 70,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateObsidianBlade() => new()
    {
        Name = "Obsidian Blade",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLionFang() => new()
    {
        Name = "Lion Fang",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateZanmato() => new()
    {
        Name = "Zanmato",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSparkSword() => new()
    {
        Name = "Spark Sword",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 320,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePaleRider() => new()
    {
        Name = "Pale Rider",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRedEdgeBlade() => new()
    {
        Name = "Red Edge Blade",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 340,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSplendidSword() => new()
    {
        Name = "Splendid Sword",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 84,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHorizonSword() => new()
    {
        Name = "Horizon Sword",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateEraserSword() => new()
    {
        Name = "Eraser Sword",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 88,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateIsildur() => new()
    {
        Name = "Isildur",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateForestBlade() => new()
    {
        Name = "Forest Blade",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 92,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCrowWingBlade() => new()
    {
        Name = "Crow Wing Blade",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 94,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 400,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDaisyHeatSword() => new()
    {
        Name = "Daisy Heat Sword",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 96,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRuinHawkSword() => new()
    {
        Name = "Ruin Hawk Sword",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCenturionSword() => new()
    {
        Name = "Centurion Sword",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Sword",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
}
