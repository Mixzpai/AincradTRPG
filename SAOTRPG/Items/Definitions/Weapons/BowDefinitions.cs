using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all bow weapons.
/// </summary>
public static class BowDefinitions
{
    public static Weapon CreateBronzeBow() => new()
    {
        Name = "Bronze Bow",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 65,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnnealBow() => new()
    {
        Name = "Anneal Bow",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHunterBow() => new()
    {
        Name = "Hunter Bow",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBrassBow() => new()
    {       
        Name = "Brass Bow",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSteelBow() => new()
    {
        Name = "Steel Bow",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAssaultBow() => new()
    {
        Name = "Assault Bow",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 140,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePurpleBow() => new()
    {
        Name = "Purple Bow",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSharpShooter() => new()
    {
        Name = "Sharp Shooter",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 170,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGoldShooter() => new()
    {
        Name = "Gold Shooter",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateJackalBow() => new()
    {
        Name = "Jackal Bow",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 200,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMetalShooter() => new()
    {
        Name = "Metal Shooter",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSkyShooter() => new()
    {
        Name = "Sky Shooter",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMoonShooter() => new()
    {
        Name = "Moon Shooter",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateWhiteLily() => new()
    {
        Name = "White Lily",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 250,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCaptainShooter() => new()
    {
        Name = "Captain Shooter",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 66,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHephaestusShooter() => new()
    {
        Name = "Hephaestus Shooter",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateWingShooter() => new()
    {
        Name = "Wing Shooter",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 70,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateEspoirBow() => new()
    {
        Name = "Espoir Bow",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateFreesia() => new()
    {
        Name = "Freesia",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateZuikaku() => new()
    {
        Name = "Zuikaku",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBlackLily() => new()
    {
        Name = "Black Lily",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 320,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateQuakeShooter() => new()
    {
        Name = "Quake Shooter",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSilverGreyShooter() => new()
    {
        Name = "Silver Grey Shooter",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 340,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateExileShooter() => new()
    {
        Name = "Exile Shooter",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 84,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDuskShooter() => new()
    {
        Name = "Dusk Shooter",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRadiusShooter() => new()
    {
        Name = "Radius Shooter",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 88,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHepaticusBow() => new()
    {
        Name = "Hepaticus Bow",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBindthornShooter() => new()
    {
        Name = "Bindthorn Shooter",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 92,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateTearsOfBlood() => new()
    {
        Name = "Tears of Blood",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 94,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 400,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDaisyHeatShooter() => new()
    {
        Name = "Daisy Heat Shooter",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 96,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRuinHawkBow() => new()
    {
        Name = "Ruin Hawk Bow",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCenturionShooter() => new()
    {
        Name = "Centurion Shooter",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Fishing-series bow
    public static Weapon CreateLostEpicBow() => new()
    {
        Name = "Lost Epic Bow",
        Value = 300,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 130,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Endgame series – Radgrid line
    public static Weapon CreateRadgrid() => new()
    {
        Name = "Radgrid",
        Value = 2000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 101,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 500,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNoxRadgrid() => new()
    {
        Name = "Nox Radgrid",
        Value = 2200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 121,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 550,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSigrun() => new()
    {
        Name = "Sigrun",
        Value = 2400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 141,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 600,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRossoSigrun() => new()
    {
        Name = "Rosso Sigrun",
        Value = 2600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 161,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 650,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateReginleifr() => new()
    {
        Name = "Reginleifr",
        Value = 2800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 181,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 700,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaouReginleifr() => new()
    {
        Name = "Gaou Reginleifr",
        Value = 3000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 201,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 750,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateTanngnidr() => new()
    {
        Name = "Tanngnidr",
        Value = 3200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 221,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 800,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaleTanngnidr() => new()
    {
        Name = "Gale Tanngnidr",
        Value = 3400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 241,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 850,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSveid() => new()
    {
        Name = "Sveid",
        Value = 3600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 261,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 900,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLavaSveid() => new()
    {
        Name = "Lava Sveid",
        Value = 3800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 281,
        EquipmentType = "Weapon",
        WeaponType = "Bow",
        BaseDamage = 950,
        AttackSpeed = 1,
        Range = 3,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
}
