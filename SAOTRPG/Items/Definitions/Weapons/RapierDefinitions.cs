using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all rapier weapons.
/// </summary>
public static class RapierDefinitions
{
    public static Weapon CreateBronzeRapier() => new()
    {
        Name = "Bronze Rapier",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 1,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 65,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAnnealRapier() => new()
    {
        Name = "Anneal Rapier",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateStackedRapier() => new()
    {
        Name = "Stacked Rapier",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBrassFleuret() => new()
    {
        Name = "Brass Fleuret",
        Value = 200,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSteelRapier() => new()
    {
        Name = "Steel Rapier",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateWindFleuret() => new()
    {
        Name = "Wind Fleuret",
        Value = 300,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 140,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateEstoc() => new()
    {
        Name = "Estoc",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSpikeFleuret() => new()
    {
        Name = "Spike Fleuret",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 170,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLeoRapier() => new()
    {
        Name = "Leo Rapier",
        Value = 450,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRoseoFleuret() => new()
    {
        Name = "Roseo Fleuret",
        Value = 500,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 200,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSharpStrike() => new()
    {
        Name = "Sharp Strike",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGerberaFleuret() => new()
    {
        Name = "Gerbera Fleuret",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateEleganteRapier() => new()
    {
        Name = "Elegante Rapier",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGreenRapier() => new()
    {
        Name = "Green Rapier",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 250,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateAgateRapier() => new()
    {
        Name = "Agate Rapier",
        Value = 750,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 66,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRoseFleuret() => new()
    {
        Name = "Rose Fleuret",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDrillRapier() => new()
    {
        Name = "Drill Rapier",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 70,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateUndine() => new()
    {
        Name = "Undine",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDelphinium() => new()
    {
        Name = "Delphinium",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateKamuy() => new()
    {
        Name = "Kamuy",
        Value = 1000,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateQueenRapier() => new()
    {
        Name = "Queen Rapier",
        Value = 1050,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 320,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSacrifice() => new()
    {
        Name = "Sacrifice",
        Value = 1100,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateSilverGreyRapier() => new()
    {
        Name = "Silver Grey Rapier",
        Value = 1150,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 340,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNightmare() => new()
    {
        Name = "Nightmare",
        Value = 1200,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 84,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateThornRapier() => new()
    {
        Name = "Thorn Rapier",
        Value = 1250,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateBlueHearts() => new()
    {
        Name = "Blue Hearts",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 88,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePainCollector() => new()
    {
        Name = "Pain Collector",
        Value = 1350,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePrairieRapier() => new()
    {
        Name = "Prairie Rapier",
        Value = 1400,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 92,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateTrenchRapier() => new()
    {
        Name = "Trench Rapier",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 94,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 400,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDaisyHeatRapier() => new()
    {
        Name = "Daisy Heat Rapier",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 96,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRuinHawkRapier() => new()
    {
        Name = "Ruin Hawk Rapier",
        Value = 1550,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCenturionRapier() => new()
    {
        Name = "Centurion Rapier",
        Value = 1600,
        Rarity = "Epic",
        ItemDurability = 40,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Fishing-series rapier
    public static Weapon CreateLostEpicRapier() => new()
    {
        Name = "Lost Epic Rapier",
        Value = 300,
        Rarity = "Rare",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 130,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    // Endgame series – Integral / Nox / Lux / Rosso / Yasha / Gaou / Machina / Gale / Rex / Lava
    public static Weapon CreateArcAngel() => new()
    {
        Name = "Arc Angel",
        Value = 2000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 101,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 500,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateNoxArcAngel() => new()
    {
        Name = "Nox Arc Angel",
        Value = 2200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 121,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 550,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateDominion() => new()
    {
        Name = "Dominion",
        Value = 2400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 141,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 600,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateRossoDominion() => new()
    {
        Name = "Rosso Dominion",
        Value = 2600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 161,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 650,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreatePrincipality() => new()
    {
        Name = "Principality",
        Value = 2800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 181,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 700,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaouPrincipality() => new()
    {
        Name = "Gaou Principality",
        Value = 3000,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 201,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 750,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
                
    public static Weapon CreateVirtue() => new()
    {
        Name = "Virtue",
        Value = 3200,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 221,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 800,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateGaleVirtue() => new()
    {
        Name = "Gale Virtue",
        Value = 3400,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 241,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 850,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateCherubim() => new()
    {
        Name = "Cherubim",
        Value = 3600,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 261,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 900,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };

    public static Weapon CreateLavaCherubim() => new()
    {
        Name = "Lava Cherubim",
        Value = 3800,
        Rarity = "Legendary",
        ItemDurability = 40,
        RequiredLevel = 281,
        EquipmentType = "Weapon",
        WeaponType = "One-Handed Rapier",
        BaseDamage = 950,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 0)
    };
}
