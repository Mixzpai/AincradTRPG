using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;


// Static registry of all one-handed sword weapons.
public static class OneHandedSwordDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "One-Handed Sword",
            BaseDamage = baseDmg, AttackSpeed = 1, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronSword() => Make("iron_sword", "Iron Sword", 100, "Common", 50, 1, 10,
        B().Add(StatType.Attack, 8));

    public static Weapon CreateSteelSword() => Make("steel_sword", "Steel Sword", 250, "Uncommon", 75, 10, 25,
        B().Add(StatType.Attack, 15).Add(StatType.Strength, 5));

    public static Weapon CreateMythrilSword() => Make("mythril_sword", "Mythril Sword", 800, "Rare", 100, 25, 45,
        B().Add(StatType.Attack, 25).Add(StatType.Strength, 8));

    public static Weapon CreateAdamantiteSword() => Make("adamantite_sword", "Adamantite Sword", 2500, "Epic", 150, 50, 80,
        B().Add(StatType.Attack, 45).Add(StatType.Strength, 15).Add(StatType.Dexterity, 8));

    public static Weapon CreateCelestialBlade() => Make("celestial_blade", "Celestial Blade", 6000, "Legendary", 200, 75, 130,
        B().Add(StatType.Attack, 70).Add(StatType.Strength, 20).Add(StatType.Agility, 10));

    // --- Named / SAO Legendary Weapons ---

    public static Weapon CreateAnnealBlade() => Make("anneal_blade", "Anneal Blade", 200, "Rare", 60, 5, 14,
        B().Add(StatType.Attack, 12).Add(StatType.Strength, 3));

    public static Weapon CreateSwordBreaker() => Make("sword_breaker", "Sword Breaker", 400, "Uncommon", 70, 15, 20,
        B().Add(StatType.Attack, 10).Add(StatType.Dexterity, 5), "ParryChance+10");

    public static Weapon CreateQueensKnightsword() => Make("queens_knightsword", "Queen's Knightsword", 1200, "Epic", 100, 20, 30,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "ComboBonus+50");

    public static Weapon CreateAzureSkyBlade() => Make("azure_sky_blade", "Azure Sky Blade", 1800, "Rare", 120, 30, 50,
        B().Add(StatType.Attack, 30).Add(StatType.Agility, 8));

    public static Weapon CreateElucidator() => Make("elucidator", "Elucidator", 10000, "Legendary", 200, 50, 90,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 18).Add(StatType.Agility, 10), "SkillCooldown-1");

    public static Weapon CreateDarkRepulser() => Make("dark_repulser", "Dark Repulser", 10000, "Legendary", 200, 50, 85,
        B().Add(StatType.Attack, 48).Add(StatType.Dexterity, 12).Add(StatType.Strength, 12), "CritHeal+5");

    public static Weapon CreateLiberator() => Make("liberator", "Liberator", 15000, "Legendary", 250, 75, 140,
        B().Add(StatType.Attack, 65).Add(StatType.Strength, 25), "BlockChance+15");

    public static Weapon CreateTyrfing() => Make("tyrfing", "Tyrfing", 20000, "Legendary", 250, 80, 160,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 20).Add(StatType.Dexterity, 15), "SkillDamage+25");

    // F3 Elf War reward. Dark-elven steel, faint twilight glow.
    public static Weapon CreateSwordOfEventide() => Make("sword_of_eventide", "Sword of Eventide", 2200, "Rare", 120, 6, 28,
        B().Add(StatType.Attack, 16).Add(StatType.Agility, 6).Add(StatType.Dexterity, 4), "CritRate+10");

    // Alternate F3-4 Elf War reward. Dark blue blade with a boar-tusk guard.
    public static Weapon CreateBlueBoar() => Make("blue_boar", "Blue Boar", 1800, "Rare", 110, 8, 32,
        B().Add(StatType.Attack, 18).Add(StatType.Strength, 8), "BackstabDmg+30");

    // Drop from Laughing Coffin-aligned PKers. Bloody red, jagged.
    public static Weapon CreateCrimsonLongsword() => Make("crimson_longsword", "Crimson Longsword", 4200, "Epic", 150, 25, 62,
        B().Add(StatType.Attack, 34).Add(StatType.Strength, 12).Add(StatType.Dexterity, 6), "Bleed+15");
}
