using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all mace weapons.
// Maces: moderate damage, slow speed. Strength/Vitality-oriented.
public static class MaceDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Mace",
            BaseDamage = baseDmg, AttackSpeed = 2, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateWoodenClub() => Make("wooden_club", "Wooden Club", 60, "Common", 40, 1, 9,
        B().Add(StatType.Attack, 7).Add(StatType.Strength, 2));

    public static Weapon CreateIronMace() => Make("iron_mace", "Iron Mace", 240, "Uncommon", 65, 8, 22,
        B().Add(StatType.Attack, 14).Add(StatType.Strength, 6).Add(StatType.Vitality, 3));

    public static Weapon CreateMythrilMace() => Make("mythril_mace", "Mythril Mace", 800, "Rare", 100, 25, 42,
        B().Add(StatType.Attack, 25).Add(StatType.Strength, 10).Add(StatType.Vitality, 5));

    public static Weapon CreateAdamantiteMace() => Make("adamantite_mace", "Adamantite Mace", 2600, "Epic", 160, 50, 75,
        B().Add(StatType.Attack, 45).Add(StatType.Strength, 16).Add(StatType.Vitality, 10));

    public static Weapon CreateCelestialMace() => Make("celestial_mace", "Celestial Mace", 6000, "Legendary", 210, 75, 125,
        B().Add(StatType.Attack, 70).Add(StatType.Strength, 22).Add(StatType.Vitality, 15));

    public static Weapon CreateMinotaurWarhammer() => Make("minotaur_warhammer", "Warhammer of the Minotaur", 1600, "Epic", 120, 35, 52,
        B().Add(StatType.Attack, 30).Add(StatType.Strength, 14), "StunChance+15");

    public static Weapon CreateMjolnir() => Make("mjolnir", "Mjolnir", 20000, "Legendary", 250, 80, 140,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 25).Add(StatType.Vitality, 20), "StunChance+25");

    // Lisbeth's signature smithing mace. Heavier than it looks; built to last.
    public static Weapon CreateMaceOfLord() => Make("mace_of_lord", "Mace of Lord", 4800, "Rare", 220, 20, 52,
        B().Add(StatType.Attack, 28).Add(StatType.Strength, 14).Add(StatType.Vitality, 6), "DurabilityBonus+50");
}
