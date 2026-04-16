using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all axe weapons.
// Axes: high damage, slow attack speed. Strength-oriented.
public static class AxeDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Axe",
            BaseDamage = baseDmg, AttackSpeed = 2, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateHandAxe() => Make("hand_axe", "Hand Axe", 90, "Common", 45, 1, 12,
        B().Add(StatType.Attack, 10).Add(StatType.Strength, 3));

    public static Weapon CreateBattleAxe() => Make("battle_axe", "Battle Axe", 280, "Uncommon", 70, 10, 30,
        B().Add(StatType.Attack, 18).Add(StatType.Strength, 8));

    public static Weapon CreateMythrilAxe() => Make("mythril_axe", "Mythril Axe", 850, "Rare", 110, 25, 55,
        B().Add(StatType.Attack, 30).Add(StatType.Strength, 10));

    public static Weapon CreateAdamantiteAxe() => Make("adamantite_axe", "Adamantite Axe", 2800, "Epic", 170, 50, 95,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 18));

    public static Weapon CreateCelestialAxe() => Make("celestial_axe", "Celestial Axe", 6500, "Legendary", 220, 75, 155,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 25).Add(StatType.Vitality, 10));

    public static Weapon CreatePaleEdge() => Make("pale_edge", "Pale Edge", 400, "Rare", 65, 15, 22,
        B().Add(StatType.Attack, 14).Add(StatType.Strength, 6).Add(StatType.Agility, 4));

    public static Weapon CreateOuroboros() => Make("ouroboros", "Ouroboros", 20000, "Legendary", 250, 80, 170,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 28).Add(StatType.Vitality, 15), "AoERadius+1");

    // Agil's starter broadaxe. Reliable through the lower floors.
    public static Weapon CreateSamuraiAxe() => Make("samurai_axe", "Samurai Axe", 900, "Uncommon", 130, 8, 38,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "DurabilityBonus+25");

    // Agil's 'falling-lotus' battleaxe. Earned through a mid-game befriend quest.
    public static Weapon CreateOchigaitou() => Make("ochigaitou", "Ochigaitou", 9500, "Epic", 210, 42, 105,
        B().Add(StatType.Attack, 52).Add(StatType.Strength, 22).Add(StatType.Vitality, 8), "KnockbackChance+30");
}
