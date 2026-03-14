using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all shield weapons.
/// </summary>
public static class ShieldDefinitions
{
    public static Weapon CreateSmallBuckler() => new()
    {
        Name = "Small Buckler",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 1,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 5)
    };

    public static Weapon CreateCombatShield() => new()
    {
        Name = "Combat Shield",
        Value = 60,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 1,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 6)
    };

    public static Weapon CreateDefensiveBuckler() => new()
    {
        Name = "Defensive Buckler",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 10,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 10)
    };

    public static Weapon CreateShiningBuckler() => new()
    {
        Name = "Shining Buckler",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 15,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 12)
    };

    public static Weapon CreateIronShield() => new()
    {
        Name = "Iron Shield",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 20,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 15)
    };

    public static Weapon CreateSteelShield() => new()
    {
        Name = "Steel Shield",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 25,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 18)
    };

    public static Weapon CreateLightBuckler() => new()
    {
        Name = "Light Buckler",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 30,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 20)
    };

    public static Weapon CreateMoonBuckler() => new()
    {
        Name = "Moon Buckler",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 35,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 22)
    };

    public static Weapon CreateFlameShield() => new()
    {
        Name = "Flame Shield",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 40,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 24)
    };

    public static Weapon CreateSpheneBuckler() => new()
    {
        Name = "Sphene Buckler",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 60,
        RequiredLevel = 45,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 26)
    };

    public static Weapon CreateMarkShield() => new()
    {
        Name = "Mark Shield",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 50,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 28)
    };

    public static Weapon CreateProtectBuckler() => new()
    {
        Name = "Protect Buckler",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 55,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 30)
    };

    public static Weapon CreateBurshimonBuckler() => new()
    {
        Name = "Burshimon Buckler",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 60,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 32)
    };

    public static Weapon CreatePaleMistBuckler() => new()
    {
        Name = "Pale Mist Buckler",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 62,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 34)
    };

    public static Weapon CreateBlastBuckler() => new()
    {
        Name = "Blast Buckler",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 64,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 36)
    };

    public static Weapon CreateHardLightBuckler() => new()
    {
        Name = "Hard-Light Buckler",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 66,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 38)
    };

    public static Weapon CreateKaleidoShield() => new()
    {
        Name = "Kaleido Shield",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 68,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 40)
    };

    public static Weapon CreateDiaShield() => new()
    {
        Name = "Dia Shield",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 70,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 42)
    };

    public static Weapon CreateBloodOnyxBuckler() => new()
    {
        Name = "Blood Onyx Buckler",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 72,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 44)
    };

    public static Weapon CreateUmbrellaShield() => new()
    {
        Name = "Umbrella Shield",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 74,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 46)
    };

    public static Weapon CreateLegionBuckler() => new()
    {
        Name = "Legion Buckler",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 76,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 48)
    };

    public static Weapon CreateGreenBuckler() => new()
    {
        Name = "Green Buckler",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 78,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 50)
    };

    public static Weapon CreateWeissShield() => new()
    {
        Name = "Weiß Shield",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 80,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 52)
    };

    public static Weapon CreateSilverGreyBuckler() => new()
    {
        Name = "Silver Grey Buckler",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 82,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 54)
    };

    public static Weapon CreateNobleShield() => new()
    {
        Name = "Noble Shield",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 84,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 56)
    };

    public static Weapon CreateKeepersShield() => new()
    {
        Name = "Keeper's Shield",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 86,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 58)
    };

    public static Weapon CreateTetraShield() => new()
    {
        Name = "Tetra Shield",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 88,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 60)
    };

    public static Weapon CreateReliefShield() => new()
    {
        Name = "Relief Shield",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 90,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 62)
    };

    public static Weapon CreateScarabShield() => new()
    {
        Name = "Scarab Shield",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 92,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 64)
    };

    public static Weapon CreateGearBuckler() => new()
    {
        Name = "Gear Buckler",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 94,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 66)
    };

    public static Weapon CreateDaisyHeatShield() => new()
    {
        Name = "Daisy Heat Shield",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 96,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 68)
    };

    public static Weapon CreateRuinHawkShield() => new()
    {
        Name = "Ruin Hawk Shield",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 98,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 70)
    };

    public static Weapon CreateCenturionShield() => new()
    {
        Name = "Centurion Shield",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 100,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 72)
    };

    // Fishing-series shield
    public static Weapon CreateLostEpicShield() => new()
    {
        Name = "Lost Epic Shield",
        Value = 300,
        Rarity = "Rare",
        ItemDurability = 70,
        RequiredLevel = 20,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 25)
    };

    // Endgame Integral-series shield
    public static Weapon CreateFermat() => new()
    {
        Name = "Fermat",
        Value = 2000,
        Rarity = "Legendary",
        ItemDurability = 90,
        RequiredLevel = 101,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 80)
    };

    public static Weapon CreateNoxFermat() => new()
    {
        Name = "Nox Fermat",
        Value = 2200,
        Rarity = "Legendary",
        ItemDurability = 90,
        RequiredLevel = 121,
        EquipmentType = "Shield",
        WeaponType = "Shield",
        BaseDamage = 0,
        AttackSpeed = 0,
        Range = 0,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 90)
    };
}
