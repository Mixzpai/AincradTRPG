using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all dagger weapons.
/// </summary>
public static class DaggerDefinitions
{
    public static Weapon CreateBronzeDagger() => new()
    {
        Name = "Bronze Dagger",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 65,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnnealDagger() => new()
    {
        Name = "Anneal Dagger",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDark() => new()
    {
        Name = "Dark",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateKukri() => new()
    {
        Name = "Kukri",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSteelDagger() => new()
    {
        Name = "Steel Dagger",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBlueStilet() => new()
    {
        Name = "Blue Stilet",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 140,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRedLight() => new()
    {
        Name = "Red Light",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCrossKnife() => new()
    {
        Name = "Cross Knife",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 170,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBullstar() => new()
    {
        Name = "Bullstar",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateKnightDagger() => new()
    {
        Name = "Knight Dagger",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 200,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateScarletPain() => new()
    {
        Name = "Scarlet Pain",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAssassinDagger() => new()
    {
        Name = "Assassin Dagger",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateViolet() => new()
    {
        Name = "Violet",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateFinDagger() => new()
    {
        Name = "Fin Dagger",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 250,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateArmBreaker() => new()
    {
        Name = "Arm Breaker",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 66,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateOdyssey() => new()
    {
        Name = "Odyssey",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateStingDagger() => new()
    {
        Name = "Sting Dagger",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 70,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateYamasachi() => new()
    {
        Name = "Yamasachi",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateTempleDagger() => new()
    {
        Name = "Temple Dagger",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateShirayuki() => new()
    {
        Name = "Shirayuki",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateKagutsuchi() => new()
    {
        Name = "Kagutsuchi",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 320,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateForbiddenDagger() => new()
    {
        Name = "Forbidden Dagger",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateIgniteKnife() => new()
    {
        Name = "Ignite Knife",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 340,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMirageDagger() => new()
    {
        Name = "Mirage Dagger",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 84,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHighlandDagger() => new()
    {
        Name = "Highland Dagger",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBlueMetalKnife() => new()
    {
        Name = "Blue Metal Knife",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 88,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSwordBreaker() => new()
    {
        Name = "Sword Breaker",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateEmeraldReef() => new()
    {
        Name = "Emerald Reef",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 92,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLavaKnife() => new()
    {
        Name = "Lava Knife",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 94,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 400,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDaisyHeatDagger() => new()
    {
        Name = "Daisy Heat Dagger",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 96,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRuinHawkKnife() => new()
    {
        Name = "Ruin Hawk Knife",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCenturionKnife() => new()
    {
        Name = "Centurion Knife",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Fishing-series dagger
    public static Weapon CreateLostEpicDagger() => new()
    {
        Name = "Lost Epic Dagger",
        Value = 300,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 130,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Endgame series – Nocturne line
    public static Weapon CreateNocturne() => new()
    {
        Name = "Nocturne",
        Value = 2000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 101,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 500,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNoxNocturne() => new()
    {
        Name = "Nox Nocturne",
        Value = 2200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 121,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 550,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRhapsody() => new()
    {
        Name = "Rhapsody",
        Value = 2400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 141,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 600,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRossoRhapsody() => new()
    {
        Name = "Rosso Rhapsody",
        Value = 2600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 161,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 650,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateOratorio() => new()
    {
        Name = "Oratorio",
        Value = 2800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 181,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 700,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaouOratorio() => new()
    {
        Name = "Gaou Oratorio",
        Value = 3000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 201,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 750,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCadenza() => new()
    {
        Name = "Cadenza",
        Value = 3200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 221,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 800,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaleCadenza() => new()
    {
        Name = "Gale Cadenza",
        Value = 3400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 241,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 850,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMinuet() => new()
    {
        Name = "Minuet",
        Value = 3600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 261,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 900,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLavaMinuet() => new()
    {
        Name = "Lava Minuet",
        Value = 3800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 281,
        EquipmentType = "Weapon",
        WeaponType = "Dagger",
        BaseDamage = 950,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
}
