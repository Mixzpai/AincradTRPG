using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all katana weapons.
// Katanas: balanced damage/speed, agility-oriented. SAO signature weapon class.
public static class KatanaDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, int attackSpeed, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Katana",
            BaseDamage = baseDmg, AttackSpeed = attackSpeed, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronKatana() => Make("iron_katana", "Iron Katana", 110, "Common", 45, 1, 11, 0,
        B().Add(StatType.Attack, 9).Add(StatType.Agility, 3));

    public static Weapon CreateSteelKatana() => Make("steel_katana", "Steel Katana", 270, "Uncommon", 70, 10, 26, 0,
        B().Add(StatType.Attack, 16).Add(StatType.Agility, 6).Add(StatType.Speed, 3));

    public static Weapon CreateMythrilKatana() => Make("mythril_katana", "Mythril Katana", 820, "Rare", 95, 25, 48, 1,
        B().Add(StatType.Attack, 28).Add(StatType.Agility, 10).Add(StatType.Speed, 5));

    public static Weapon CreateAdamantiteKatana() => Make("adamantite_katana", "Adamantite Katana", 2700, "Epic", 155, 50, 82, 1,
        B().Add(StatType.Attack, 48).Add(StatType.Agility, 16).Add(StatType.Speed, 10));

    public static Weapon CreateCelestialKatana() => Make("celestial_katana", "Celestial Katana", 6200, "Legendary", 200, 75, 135, 1,
        B().Add(StatType.Attack, 72).Add(StatType.Agility, 22).Add(StatType.Speed, 15));

    public static Weapon CreateKarakurenai() => Make("karakurenai", "Karakurenai", 1300, "Epic", 100, 25, 40, 1,
        B().Add(StatType.Attack, 25).Add(StatType.Agility, 12).Add(StatType.Strength, 6), "BackstabDmg+50");

    public static Weapon CreateMasamune() => Make("masamune", "Masamune", 20000, "Legendary", 250, 80, 150, 1,
        B().Add(StatType.Attack, 78).Add(StatType.Agility, 25).Add(StatType.Speed, 18), "CritRate+20");

    // F2 hidden miniboss drop. Violet-black, drinks HP from what it cuts.
    public static Weapon CreateSoulEater() => Make("soul_eater", "Soul Eater", 2400, "Rare", 120, 5, 30, 1,
        B().Add(StatType.Attack, 16).Add(StatType.Strength, 6).Add(StatType.Agility, 6), "CritHeal+10");

    // Klein's shadow-stitcher upgrade. Awarded by befriending him through F1-F30.
    public static Weapon CreateKagenui() => Make("kagenui", "Kagenui", 7800, "Epic", 180, 35, 82, 1,
        B().Add(StatType.Attack, 42).Add(StatType.Agility, 15).Add(StatType.Dexterity, 10), "BackstabDmg+25");
}
