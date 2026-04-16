using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all dagger weapons.
// Daggers: low damage, fast attack speed. Agility/Dexterity-oriented.
public static class DaggerDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Dagger",
            BaseDamage = baseDmg, AttackSpeed = 0, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateRustyDagger() => Make("rusty_dagger", "Rusty Dagger", 50, "Common", 35, 1, 6,
        B().Add(StatType.Attack, 5).Add(StatType.Agility, 4));

    public static Weapon CreateSteelDagger() => Make("steel_dagger", "Steel Dagger", 200, "Uncommon", 55, 8, 14,
        B().Add(StatType.Attack, 10).Add(StatType.Agility, 6).Add(StatType.Dexterity, 4));

    public static Weapon CreateMythrilDagger() => Make("mythril_dagger", "Mythril Dagger", 650, "Rare", 80, 25, 28,
        B().Add(StatType.Attack, 18).Add(StatType.Agility, 10).Add(StatType.Dexterity, 8));

    public static Weapon CreateAdamantiteDagger() => Make("adamantite_dagger", "Adamantite Dagger", 2000, "Epic", 120, 50, 50,
        B().Add(StatType.Attack, 32).Add(StatType.Agility, 15).Add(StatType.Dexterity, 12));

    public static Weapon CreateCelestialDagger() => Make("celestial_dagger", "Celestial Dagger", 5000, "Legendary", 170, 75, 85,
        B().Add(StatType.Attack, 55).Add(StatType.Agility, 20).Add(StatType.Dexterity, 15));

    // --- Named / SAO Legendary Weapons ---

    public static Weapon CreateAssassinDagger() => Make("assassin_dagger", "Assassin Dagger", 500, "Rare", 70, 20, 22,
        B().Add(StatType.Attack, 15).Add(StatType.Agility, 8).Add(StatType.Dexterity, 6), "BackstabDmg+50");

    public static Weapon CreateSnowWarheit() => Make("snow_warheit", "Snow Warheit", 1500, "Epic", 100, 35, 40,
        B().Add(StatType.Attack, 28).Add(StatType.Agility, 12).Add(StatType.Dexterity, 10));

    public static Weapon CreateMateChopper() => Make("mate_chopper", "Mate Chopper", 6000, "Legendary", 150, 35, 55,
        B().Add(StatType.Attack, 35).Add(StatType.Agility, 15), "Bleed+20");

    public static Weapon CreateTheIronMaiden() => Make("iron_maiden_dagger", "The Iron Maiden", 16000, "Legendary", 200, 75, 95,
        B().Add(StatType.Attack, 60).Add(StatType.Agility, 22).Add(StatType.Dexterity, 18), "CritRate+15");

    // Argo the Rat's personal twin daggers. Reward for completing her info-broker chain.
    public static Weapon CreateArgosClaws() => Make("argos_claws", "Argo's Claws", 5200, "Rare", 140, 20, 42,
        B().Add(StatType.Attack, 22).Add(StatType.Agility, 16).Add(StatType.Dexterity, 12), "BackstabDmg+30");

    // Silica's F1-era starter dagger. Plain iron, reliable balance.
    public static Weapon CreateStoutBrave() => Make("stout_brave", "Stout Brave", 400, "Uncommon", 90, 2, 14,
        B().Add(StatType.Attack, 8).Add(StatType.Agility, 4), "CritRate+10");
}
