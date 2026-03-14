using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all mace weapons.
/// </summary>
public static class MaceDefinitions
{
    public static Weapon CreateBronzeMace() => new()
    {
        Name = "Bronze Mace",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 65,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnnealHammer() => new()
    {
        Name = "Anneal Hammer",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateWarHammer() => new()
    {
        Name = "War Hammer",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBrassMace() => new()
    {
        Name = "Brass Mace",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSteelMace() => new()
    {
        Name = "Steel Mace",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLinariaHammer() => new()
    {
        Name = "Linaria Hammer",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 140,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateHardMace() => new()
    {
        Name = "Hard Mace",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAbyssHammer() => new()
    {
        Name = "Abyss Hammer",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 170,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSmash() => new()
    {
        Name = "Smash",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBindingRod() => new()
    {
        Name = "Binding Rod",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 200,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNigellusHammer() => new()
    {
        Name = "Nigellus Hammer",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateJudgeMace() => new()
    {
        Name = "Judge Mace",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBabel() => new()
    {
        Name = "Babel",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCastleMace() => new()
    {
        Name = "Castle Mace",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 250,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSpikeHammer() => new()
    {
        Name = "Spike Hammer",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 66,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSkyscraper() => new()
    {
        Name = "Skyscraper",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMontagne() => new()
    {
        Name = "Montagne",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 70,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateVictorMace() => new()
    {
        Name = "Victor Mace",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBabylon() => new()
    {
        Name = "Babylon",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAooni() => new()
    {
        Name = "Aooni",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRoyalMace() => new()
    {
        Name = "Royal Mace",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 320,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateOracleHammer() => new()
    {
        Name = "Oracle Hammer",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRedSledgeHammer() => new()
    {
        Name = "Red Sledge Hammer",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 340,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGraceMace() => new()
    {
        Name = "Grace Mace",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 84,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateWoodPecker() => new()
    {
        Name = "Wood Pecker",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGravityHammer() => new()
    {
        Name = "Gravity Hammer",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 88,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMagnaHammer() => new()
    {
        Name = "Magna Hammer",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMountainCrusher() => new()
    {
        Name = "Mountain Crusher",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 92,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateFlintHammer() => new()
    {
        Name = "Flint Hammer",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 94,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 400,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDaisyHeatHammer() => new()
    {
        Name = "Daisy Heat Hammer",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 96,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRuinHawkHammer() => new()
    {
        Name = "Ruin Hawk Hammer",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCenturionHammer() => new()
    {
        Name = "Centurion Hammer",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Fishing-series mace
    public static Weapon CreateLostEpicMace() => new()
    {
        Name = "Lost Epic Mace",
        Value = 300,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 130,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Endgame series – Stingray line
    public static Weapon CreateStingray() => new()
    {
        Name = "Stingray",
        Value = 2000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 101,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 500,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNoxStingray() => new()
    {
        Name = "Nox Stingray",
        Value = 2200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 121,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 550,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAlbatross() => new()
    {
        Name = "Albatross",
        Value = 2400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 141,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 600,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRossoAlbatross() => new()
    {
        Name = "Rosso Albatross",
        Value = 2600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 161,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 650,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePorcupine() => new()
    {
        Name = "Porcupine",
        Value = 2800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 181,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 700,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaouPorcupine() => new()
    {
        Name = "Gaou Porcupine",
        Value = 3000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 201,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 750,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateMudskipper() => new()
    {
        Name = "Mudskipper",
        Value = 3200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 221,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 800,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaleMudskipper() => new()
    {
        Name = "Gale Mudskipper",
        Value = 3400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 241,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 850,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateFalcon() => new()
    {
        Name = "Falcon",
        Value = 3600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 261,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 900,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLavaFalcon() => new()
    {
        Name = "Lava Falcon",
        Value = 3800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 281,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Mace",
        BaseDamage = 950,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
                                            }
