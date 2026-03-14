using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all two-handed sword weapons.
/// </summary>
public static class TwoHandedSwordDefinitions
{
    public static Weapon CreateIronGreatsword() => new()
    {
        Name = "Iron Greatsword",
        Value = 90,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 5,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 85,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 3)
    };

    public static Weapon CreateSteelClaymore() => new()
    {
        Name = "Steel Claymore",
        Value = 170,
        Rarity = "Common",
        ItemDurability = 50,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 115,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 6)
    };

    public static Weapon CreateMoonlightGreatsword() => new()
    {
        Name = "Moonlight Greatsword",
        Value = 280,
        Rarity = "Common",
        ItemDurability = 55,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 145,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 9)
    };

    public static Weapon CreateCrimsonBreaker() => new()
    {
        Name = "Crimson Breaker",
        Value = 450,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 180,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 14)
            .Add(StatType.Dexterity, 3)
    };

    public static Weapon CreateAzureColossus() => new()
    {
        Name = "Azure Colossus",
        Value = 680,
        Rarity = "Rare",
        ItemDurability = 65,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 18)
            .Add(StatType.Dexterity, 4)
    };

    public static Weapon CreateShadowBastion() => new()
    {
        Name = "Shadow Bastion",
        Value = 1000,
        Rarity = "Rare",
        ItemDurability = 65,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 245,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 22)
            .Add(StatType.Defense, 3)
    };

    public static Weapon CreateBloodReaver() => new()
    {
        Name = "Blood Reaver",
        Value = 1450,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 65,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 280,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 28)
            .Add(StatType.Dexterity, 6)
    };

    public static Weapon CreateDragonCleaver() => new()
    {
        Name = "Dragon Cleaver",
        Value = 2000,
        Rarity = "Epic",
        ItemDurability = 75,
        RequiredLevel = 75,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 315,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 34)
            .Add(StatType.Intelligence, 8)
    };

    public static Weapon CreateStormrend() => new()
    {
        Name = "Stormrend",
        Value = 2700,
        Rarity = "Epic",
        ItemDurability = 75,
        RequiredLevel = 85,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 345,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 40)
            .Add(StatType.Speed, 3)
    };

    public static Weapon CreateMidnightColossus() => new()
    {
        Name = "Midnight Colossus",
        Value = 3300,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 95,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 375,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 46)
            .Add(StatType.Dexterity, 8)
    };

    public static Weapon CreateAbyssalGreatsword() => new()
    {
        Name = "Abyssal Greatsword",
        Value = 4800,
        Rarity = "Legendary",
        ItemDurability = 85,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 430,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 60)
            .Add(StatType.Intelligence, 15)
            .Add(StatType.Dexterity, 10)
    };

    public static Weapon CreateEclipseColossus() => new()
    {
        Name = "Eclipse Colossus",
        Value = 5400,
        Rarity = "Legendary",
        ItemDurability = 85,
        RequiredLevel = 110,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 460,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 66)
            .Add(StatType.Intelligence, 18)
            .Add(StatType.Defense, 5)
    };

    public static Weapon CreateBladeOfOblivion() => new()
    {
        Name = "Blade of Oblivion",
        Value = 6200,
        Rarity = "Legendary",
        ItemDurability = 90,
        RequiredLevel = 120,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 490,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 72)
            .Add(StatType.Intelligence, 20)
            .Add(StatType.Dexterity, 12)
    };

    public static Weapon CreateLionheartClaymore() => new()
    {
        Name = "Lionheart Claymore",
        Value = 6400,
        Rarity = "Legendary",
        ItemDurability = 90,
        RequiredLevel = 120,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 495,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 74)
            .Add(StatType.Intelligence, 18)
            .Add(StatType.Defense, 6)
    };

    public static Weapon CreateTitansResolve() => new()
    {
        Name = "Titan's Resolve",
        Value = 6100,
        Rarity = "Legendary",
        ItemDurability = 90,
        RequiredLevel = 118,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 485,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 70)
            .Add(StatType.Defense, 8)
    };

    public static Weapon CreateDragonboneCleaver() => new()
    {
        Name = "Dragonbone Cleaver",
        Value = 5900,
        Rarity = "Legendary",
        ItemDurability = 90,
        RequiredLevel = 117,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 482,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 69)
            .Add(StatType.Intelligence, 19)
    };

    public static Weapon CreateStormHerald() => new()
    {
        Name = "Storm Herald",
        Value = 4300,
        Rarity = "Epic",
        ItemDurability = 85,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 420,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 58)
            .Add(StatType.Speed, 2)
    };

    public static Weapon CreateNightfallColossus() => new()
    {
        Name = "Nightfall Colossus",
        Value = 3500,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 95,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 390,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 50)
            .Add(StatType.Dexterity, 7)
    };

    public static Weapon CreateVoidbreaker() => new()
    {
        Name = "Voidbreaker",
        Value = 2900,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 372,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 46)
            .Add(StatType.Defense, 4)
    };

    public static Weapon CreateScarletJuggernaut() => new()
    {
        Name = "Scarlet Juggernaut",
        Value = 2600,
        Rarity = "Epic",
        ItemDurability = 75,
        RequiredLevel = 86,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 42)
            .Add(StatType.Intelligence, 10)
    };

    public static Weapon CreateAzureBehemoth() => new()
    {
        Name = "Azure Behemoth",
        Value = 2300,
        Rarity = "Epic",
        ItemDurability = 75,
        RequiredLevel = 82,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 350,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 40)
            .Add(StatType.Dexterity, 5)
    };

    public static Weapon CreateThunderColossus() => new()
    {
        Name = "Thunder Colossus",
        Value = 2100,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 338,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 37)
            .Add(StatType.Intelligence, 7)
    };

    public static Weapon CreateSilentTitan() => new()
    {
        Name = "Silent Titan",
        Value = 1800,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 74,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 325,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 34)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateWisteriaGreatblade() => new()
    {
        Name = "Wisteria Greatblade",
        Value = 1500,
        Rarity = "Rare",
        ItemDurability = 65,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 305,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 30)
            .Add(StatType.Dexterity, 4)
    };

    public static Weapon CreateFrostbrandColossus() => new()
    {
        Name = "Frostbrand Colossus",
        Value = 1300,
        Rarity = "Rare",
        ItemDurability = 65,
        RequiredLevel = 64,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 290,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 27)
            .Add(StatType.Dexterity, 3)
    };

    public static Weapon CreateVermilionColossus() => new()
    {
        Name = "Vermilion Colossus",
        Value = 1200,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 275,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 24)
            .Add(StatType.Intelligence, 5)
    };

    public static Weapon CreateObsidianColossus() => new()
    {
        Name = "Obsidian Colossus",
        Value = 1050,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 56,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 260,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 22)
            .Add(StatType.Defense, 3)
    };

    public static Weapon CreateRadiantColossus() => new()
    {
        Name = "Radiant Colossus",
        Value = 950,
        Rarity = "Rare",
        ItemDurability = 55,
        RequiredLevel = 52,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 245,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 20)
            .Add(StatType.Dexterity, 3)
    };

    public static Weapon CreatePhantomColossus() => new()
    {
        Name = "Phantom Colossus",
        Value = 850,
        Rarity = "Rare",
        ItemDurability = 55,
        RequiredLevel = 48,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 230,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 18)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateSilverGreatblade() => new()
    {
        Name = "Silver Greatblade",
        Value = 760,
        Rarity = "Rare",
        ItemDurability = 50,
        RequiredLevel = 44,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 16)
    };

    public static Weapon CreateBlackstormColossus() => new()
    {
        Name = "Blackstorm Colossus",
        Value = 680,
        Rarity = "Rare",
        ItemDurability = 50,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 205,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 15)
            .Add(StatType.Speed, 1)
    };

    public static Weapon CreateDawnbreaker() => new()
    {
        Name = "Dawnbreaker",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 50,
        RequiredLevel = 36,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 195,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 14)
    };

    public static Weapon CreateEchoingGreatsword() => new()
    {
        Name = "Echoing Greatsword",
        Value = 520,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 32,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 185,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 12)
    };

    public static Weapon CreateFallenColossus() => new()
    {
        Name = "Fallen Colossus",
        Value = 460,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 28,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 173,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 11)
    };

    public static Weapon CreateGaleGreatsword() => new()
    {
        Name = "Gale Greatsword",
        Value = 400,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 24,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 163,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 10)
    };

    public static Weapon CreateHallowedGreatsword() => new()
    {
        Name = "Hallowed Greatsword",
        Value = 340,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 20,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 150,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 9)
    };

    public static Weapon CreateStarfallGreatsword() => new()
    {
        Name = "Starfall Greatsword",
        Value = 290,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 16,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 138,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 8)
    };

    public static Weapon CreateBlazingColossus() => new()
    {
        Name = "Blazing Colossus",
        Value = 240,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 12,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 7)
    };

    public static Weapon CreateFrozenColossus() => new()
    {
        Name = "Frozen Colossus",
        Value = 210,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 118,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 6)
    };

    public static Weapon CreateLunarGreatsword() => new()
    {
        Name = "Lunar Greatsword",
        Value = 180,
        Rarity = "Common",
        ItemDurability = 35,
        RequiredLevel = 8,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 5)
    };

    public static Weapon CreateDuskbreaker() => new()
    {
        Name = "Duskbreaker",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 35,
        RequiredLevel = 6,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 102,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 4)
    };

    public static Weapon CreateCelestialGreatblade() => new()
    {
        Name = "Celestial Greatblade",
        Value = 120,
        Rarity = "Common",
        ItemDurability = 35,
        RequiredLevel = 4,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 94,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 3)
    };

    public static Weapon CreateEternalGreatsword() => new()
    {
        Name = "Eternal Greatsword",
        Value = 100,
        Rarity = "Common",
        ItemDurability = 30,
        RequiredLevel = 3,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 88,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 3)
    };

    public static Weapon CreateBeginnersZweihander() => new()
    {
        Name = "Beginner's Zweihander",
        Value = 80,
        Rarity = "Common",
        ItemDurability = 30,
        RequiredLevel = 2,
        EquipmentType = "Weapon",
        WeaponType = "Two-Handed Sword",
        BaseDamage = 82,
        AttackSpeed = 1,
        Range = 2,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 2)
    };
}
