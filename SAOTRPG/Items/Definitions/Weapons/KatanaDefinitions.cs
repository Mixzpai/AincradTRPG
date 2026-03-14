using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Static registry of all katana weapons.
/// </summary>
public static class KatanaDefinitions
{
    public static Weapon CreateIronKatana() => new()
    {
        Name = "Iron Katana",
        Value = 80,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 5,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 70,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 2)
    };

    public static Weapon CreateSteelKatana() => new()
    {
        Name = "Steel Katana",
        Value = 150,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 15,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 95,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 4)
    };

    public static Weapon CreateMoonlightKatana() => new()
    {
        Name = "Moonlight Katana",
        Value = 250,
        Rarity = "Common",
        ItemDurability = 50,
        RequiredLevel = 25,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 120,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 6)
    };

    public static Weapon CreateCrimsonEdge() => new()
    {
        Name = "Crimson Edge",
        Value = 400,
        Rarity = "Rare",
        ItemDurability = 55,
        RequiredLevel = 35,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 150,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 10)
            .Add(StatType.Dexterity, 3)
    };

    public static Weapon CreateAzureGale() => new()
    {
        Name = "Azure Gale",
        Value = 600,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 45,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 180,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 14)
            .Add(StatType.Dexterity, 4)
    };

    public static Weapon CreateShadowFang() => new()
    {
        Name = "Shadow Fang",
        Value = 900,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 210,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 18)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateBloodSakura() => new()
    {
        Name = "Blood Sakura",
        Value = 1300,
        Rarity = "Epic",
        ItemDurability = 65,
        RequiredLevel = 65,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 240,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 24)
            .Add(StatType.Dexterity, 6)
    };

    public static Weapon CreateDragonClaw() => new()
    {
        Name = "Dragon Claw",
        Value = 1800,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 75,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 270,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 30)
            .Add(StatType.Intelligence, 8)
    };

    public static Weapon CreateStormBringer() => new()
    {
        Name = "Storm Bringer",
        Value = 2400,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 85,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 300,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 36)
            .Add(StatType.Speed, 3)
    };

    public static Weapon CreateMidnightLotus() => new()
    {
        Name = "Midnight Lotus",
        Value = 3000,
        Rarity = "Epic",
        ItemDurability = 75,
        RequiredLevel = 95,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 330,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 42)
            .Add(StatType.Dexterity, 8)
    };

    public static Weapon CreateSenshinHaou() => new()
    {
        Name = "Senshin Haou",
        Value = 4500,
        Rarity = "Legendary",
        ItemDurability = 80,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 380,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 55)
            .Add(StatType.Intelligence, 15)
            .Add(StatType.Dexterity, 10)
    };

    public static Weapon CreateMoonlitBlack() => new()
    {
        Name = "Moonlit Black",
        Value = 5200,
        Rarity = "Legendary",
        ItemDurability = 80,
        RequiredLevel = 110,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 410,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 60)
            .Add(StatType.Intelligence, 18)
            .Add(StatType.Agility, 4)
    };

    public static Weapon CreateBladeOfRecollection() => new()
    {
        Name = "Blade of Recollection",
        Value = 6000,
        Rarity = "Legendary",
        ItemDurability = 85,
        RequiredLevel = 120,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 440,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 68)
            .Add(StatType.Intelligence, 20)
            .Add(StatType.Dexterity, 12)
    };

    public static Weapon CreateMatamonTheBoneEater() => new()
    {
        Name = "Matamon the Bone-Eater",
        Value = 6500,
        Rarity = "Legendary",
        ItemDurability = 85,
        RequiredLevel = 120,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 450,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 70)
            .Add(StatType.Intelligence, 22)
            .Add(StatType.Dexterity, 10)
    };

    public static Weapon CreateKaguraTachi() => new()
    {
        Name = "Kagura Tachi",
        Value = 6200,
        Rarity = "Legendary",
        ItemDurability = 85,
        RequiredLevel = 118,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 435,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 66)
            .Add(StatType.Dexterity, 11)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateSpiritKatanaKagutsuch() => new()
    {
        Name = "Spirit Katana Kagutsuch",
        Value = 6400,
        Rarity = "Legendary",
        ItemDurability = 85,
        RequiredLevel = 119,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 438,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 67)
            .Add(StatType.Intelligence, 19)
            .Add(StatType.Dexterity, 11)
    };

    public static Weapon CreateKarakurenai() => new()
    {
        Name = "Karakurenai",
        Value = 4100,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 100,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 370,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 52)
            .Add(StatType.Dexterity, 9)
    };

    public static Weapon CreateSakuraBlizzard() => new()
    {
        Name = "Sakura Blizzard",
        Value = 3200,
        Rarity = "Epic",
        ItemDurability = 75,
        RequiredLevel = 90,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 335,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 44)
            .Add(StatType.Dexterity, 7)
    };

    public static Weapon CreateNightSkyTachi() => new()
    {
        Name = "Night Sky Tachi",
        Value = 3800,
        Rarity = "Epic",
        ItemDurability = 80,
        RequiredLevel = 98,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 360,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 50)
            .Add(StatType.Intelligence, 12)
    };

    public static Weapon CreateVoidCrescent() => new()
    {
        Name = "Void Crescent",
        Value = 2600,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 80,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 310,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 38)
            .Add(StatType.Agility, 3)
    };

    public static Weapon CreateScarletWind() => new()
    {
        Name = "Scarlet Wind",
        Value = 2200,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 78,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 305,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 36)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateAzureTempest() => new()
    {
        Name = "Azure Tempest",
        Value = 2100,
        Rarity = "Epic",
        ItemDurability = 70,
        RequiredLevel = 76,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 298,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 35)
            .Add(StatType.Dexterity, 5)
    };

    public static Weapon CreateThunderBlossom() => new()
    {
        Name = "Thunder Blossom",
        Value = 1900,
        Rarity = "Epic",
        ItemDurability = 65,
        RequiredLevel = 72,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 288,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 32)
            .Add(StatType.Intelligence, 6)
    };

    public static Weapon CreateSilentReaper() => new()
    {
        Name = "Silent Reaper",
        Value = 1500,
        Rarity = "Epic",
        ItemDurability = 65,
        RequiredLevel = 68,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 275,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 29)
            .Add(StatType.Agility, 3)
    };

    public static Weapon CreateWisteriaFang() => new()
    {
        Name = "Wisteria Fang",
        Value = 1200,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 60,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 255,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 25)
            .Add(StatType.Dexterity, 4)
    };

    public static Weapon CreateFrostPetal() => new()
    {
        Name = "Frost Petal",
        Value = 1000,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 55,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 235,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 22)
            .Add(StatType.Dexterity, 3)
    };

    public static Weapon CreateVermilionDragon() => new()
    {
        Name = "Vermilion Dragon",
        Value = 1400,
        Rarity = "Rare",
        ItemDurability = 60,
        RequiredLevel = 62,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 245,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 24)
            .Add(StatType.Intelligence, 5)
    };

    public static Weapon CreateObsidianHaze() => new()
    {
        Name = "Obsidian Haze",
        Value = 800,
        Rarity = "Rare",
        ItemDurability = 55,
        RequiredLevel = 50,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 220,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 20)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateRadiantMoon() => new()
    {
        Name = "Radiant Moon",
        Value = 700,
        Rarity = "Rare",
        ItemDurability = 55,
        RequiredLevel = 48,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 215,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 18)
            .Add(StatType.Dexterity, 3)
    };

    public static Weapon CreatePhantomLotus() => new()
    {
        Name = "Phantom Lotus",
        Value = 650,
        Rarity = "Rare",
        ItemDurability = 50,
        RequiredLevel = 44,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 205,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 16)
            .Add(StatType.Agility, 2)
    };

    public static Weapon CreateSilverMirage() => new()
    {
        Name = "Silver Mirage",
        Value = 550,
        Rarity = "Rare",
        ItemDurability = 50,
        RequiredLevel = 40,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 195,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 14)
            .Add(StatType.Dexterity, 2)
    };

    public static Weapon CreateBlackstormFang() => new()
    {
        Name = "Blackstorm Fang",
        Value = 480,
        Rarity = "Rare",
        ItemDurability = 50,
        RequiredLevel = 38,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 188,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 13)
            .Add(StatType.Agility, 1)
    };

    public static Weapon CreateDawnPetal() => new()
    {
        Name = "Dawn Petal",
        Value = 420,
        Rarity = "Rare",
        ItemDurability = 45,
        RequiredLevel = 34,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 178,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 12)
            .Add(StatType.Dexterity, 2)
    };

    public static Weapon CreateEchoOfSteel() => new()
    {
        Name = "Echo of Steel",
        Value = 350,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 30,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 165,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 10)
    };

    public static Weapon CreateFallenCherry() => new()
    {
        Name = "Fallen Cherry",
        Value = 280,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 26,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 155,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 9)
    };

    public static Weapon CreateGaleDivider() => new()
    {
        Name = "Gale Divider",
        Value = 230,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 22,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 145,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 8)
    };

    public static Weapon CreateHallowedCrescent() => new()
    {
        Name = "Hallowed Crescent",
        Value = 190,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 18,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 135,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 7)
    };

    public static Weapon CreateStarfallTachi() => new()
    {
        Name = "Starfall Tachi",
        Value = 160,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 14,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 125,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 6)
    };

    public static Weapon CreateBlazingIris() => new()
    {
        Name = "Blazing Iris",
        Value = 130,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 10,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 110,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 5)
    };

    public static Weapon CreateFrozenEclipse() => new()
    {
        Name = "Frozen Eclipse",
        Value = 110,
        Rarity = "Common",
        ItemDurability = 40,
        RequiredLevel = 8,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 100,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 4)
    };

    public static Weapon CreateLunarRequiem() => new()
    {
        Name = "Lunar Requiem",
        Value = 90,
        Rarity = "Common",
        ItemDurability = 35,
        RequiredLevel = 6,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 90,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 3)
    };

    public static Weapon CreateDuskSerenade() => new()
    {
        Name = "Dusk Serenade",
        Value = 70,
        Rarity = "Common",
        ItemDurability = 35,
        RequiredLevel = 4,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 80,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 3)
    };

    public static Weapon CreateCelestialFang() => new()
    {
        Name = "Celestial Fang",
        Value = 60,
        Rarity = "Common",
        ItemDurability = 35,
        RequiredLevel = 3,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 75,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 2)
    };

    public static Weapon CreateEternalSakura() => new()
    {
        Name = "Eternal Sakura",
        Value = 50,
        Rarity = "Common",
        ItemDurability = 30,
        RequiredLevel = 2,
        EquipmentType = "Weapon",
        WeaponType = "Katana",
        BaseDamage = 72,
        AttackSpeed = 1,
        Range = 1,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Attack, 2)
    };
}
