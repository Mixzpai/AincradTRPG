using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all spear weapons.
/// </summary>
public static class SpearDefinitions
{
    public static Weapon CreateBronzeLance() => new()
    {
        Name = "Bronze Lance",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 65,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnnealLance() => new()
    {
        Name = "Anneal Lance",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSharpSpear() => new()
    {
        Name = "Sharp Spear",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBrassSpear() => new()
    {
        Name = "Brass Spear",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSteelLance() => new()
    {
        Name = "Steel Lance",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateKeithLance() => new()
    {
        Name = "Keith Lance",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 140,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateIcebergLance() => new()
    {
        Name = "Iceberg Lance",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAquaAuraSpear() => new()
    {
        Name = "Aqua Aura Spear",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 170,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRavenLance() => new()
    {
        Name = "Raven Lance",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCrowSpear() => new()
    {
        Name = "Crow Spear",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 200,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGrimLance() => new()
    {
        Name = "Grim Lance",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCaravanSpear() => new()
    {
        Name = "Caravan Spear",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateKarmaLance() => new()
    {
        Name = "Karma Lance",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateFluegel() => new()
    {
        Name = "Fluegel",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 250,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCalamity() => new()
    {
        Name = "Calamity",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 66,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateValhalla() => new()
    {
        Name = "Valhalla",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnubisSpear() => new()
    {
        Name = "Anubis Spear",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 70,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateApocalypse() => new()
    {
        Name = "Apocalypse",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateForkJavelin() => new()
    {
        Name = "Fork Javelin",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHienjyumonji() => new()
    {
        Name = "Hienjyumonji",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateChaosLance() => new()
    {
        Name = "Chaos Lance",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 320,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateImpulseJavelin() => new()
    {
        Name = "Impulse Javelin",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHeatTrident() => new()
    {
        Name = "Heat Trident",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 340,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateArcSpear() => new()
    {
        Name = "Arc Spear",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 84,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCliffMaker() => new()
    {
        Name = "Cliff Maker",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGraveMarker() => new()
    {
        Name = "Grave Marker",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 88,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGrandSpear() => new()
    {
        Name = "Grand Spear",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateJadeAuraSpear() => new()
    {
        Name = "Jade Aura Spear",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 92,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBanditLance() => new()
    {
        Name = "Bandit Lance",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 94,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 400,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDaisyHeatLance() => new()
    {
        Name = "Daisy Heat Lance",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 96,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRuinHawkSpear() => new()
    {
        Name = "Ruin Hawk Spear",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCenturionLance() => new()
    {
        Name = "Centurion Lance",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Fishing-series spear
    public static Weapon CreateLostEpicLance() => new()
    {
        Name = "Lost Epic Lance",
        Value = 300,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 130,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Endgame series – Gluttony / Greed / Envy / Lust / Ira lines
    public static Weapon CreateGluttony() => new()
    {
        Name = "Gluttony",
        Value = 2000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 101,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 500,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNoxGluttony() => new()
    {
        Name = "Nox Gluttony",
        Value = 2200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 121,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 550,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGreed() => new()
    {
        Name = "Greed",
        Value = 2400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 141,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 600,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRossoGreed() => new()
    {
        Name = "Rosso Greed",
        Value = 2600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 161,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 650,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateEnvy() => new()
    {
        Name = "Envy",
        Value = 2800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 181,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 700,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaouEnvy() => new()
    {
        Name = "Gaou Envy",
        Value = 3000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 201,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 750,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLust() => new()
    {
        Name = "Lust",
        Value = 3200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 221,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 800,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaleLust() => new()
    {
        Name = "Gale Lust",
        Value = 3400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 241,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 850,
        AttackSpeed = 1,
        Range = 2,                                                                              
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateIra() => new()
    {
        Name = "Ira",
        Value = 3600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 261,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 900,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLavaIra() => new()
    {
        Name = "Lava Ira",
        Value = 3800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 281,
        EquipmentType = "Weapon",
        WeaponType = "Spear",
        BaseDamage = 950,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
}
