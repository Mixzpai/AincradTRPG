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

    // ── Hollow Realization Evolution Chain (Mace) ───────────────────
    // Lunatic Press -> Nemesis -> Yggdrasil -> Mjolnir.

    // Lunatic Press, moon-forged bludgeon whose weight breaks plate. T1 of the Mjolnir chain.
    public static Weapon CreateLunaticPress() => Make("lunatic_press", "Lunatic Press", 2000, "Rare", 120, 15, 65,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 10));

    // Nemesis, retribution-mace that answers every sin with a crack of bone. T2 of the Mjolnir chain.
    public static Weapon CreateNemesis() => Make("nemesis", "Nemesis", 5600, "Epic", 160, 35, 108,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 15).Add(StatType.Vitality, 8), "StunChance+10");

    // Yggdrasil, world-tree haft bound in living root. T3 of the Mjolnir chain.
    public static Weapon CreateYggdrasil() => Make("yggdrasil", "Yggdrasil", 13000, "Legendary", 210, 60, 148,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Vitality, 14), "StunChance+15");

    // T4 Divine of the Mace evolution chain.
    public static Weapon CreateMjolnir() => Make("mjolnir", "Mjolnir", 20000, "Divine", 999, 80, 140,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 25).Add(StatType.Vitality, 20), "StunChance+25");

    // Lisbeth's signature smithing mace. Heavier than it looks; built to last.
    public static Weapon CreateMaceOfLord() => Make("mace_of_lord", "Mace of Lord", 4800, "Rare", 220, 20, 52,
        B().Add(StatType.Attack, 28).Add(StatType.Strength, 14).Add(StatType.Vitality, 6), "DurabilityBonus+50");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────

    // Hollow Fragment F77 implement. Healer-god's rod; steady life regen (flavor).
    public static Weapon CreateMaceOfAsclepius() => Make("mace_of_asclepius", "Mace of Asclepius", 13500, "Legendary", 190, 73, 135,
        B().Add(StatType.Attack, 62).Add(StatType.Vitality, 25).Add(StatType.Strength, 15).Add(StatType.Defense, 15), "HPRegen+3");

    // Hollow Fragment F79 implement. Coiled eternity; periodic barrier procs (flavor).
    public static Weapon CreateInfiniteOuroboros() => Make("infinite_ouroboros", "Infinite Ouroboros", 14500, "Legendary", 200, 75, 140,
        B().Add(StatType.Attack, 65).Add(StatType.Vitality, 28).Add(StatType.Defense, 18), "Barrier+20");

    // Hollow Fragment F88 implement. Heavenly mace; resists interrupts (flavor).
    public static Weapon CreateStarmaceElysium() => Make("starmace_elysium", "Starmace: Elysium", 21000, "Legendary", 240, 84, 158,
        B().Add(StatType.Attack, 75).Add(StatType.Vitality, 30).Add(StatType.Defense, 25).Add(StatType.Strength, 15), "Uninterruptible+50");

    // F38 Obsidian the Black Knight drops (Alicization Lycoris Divine Beast tier).
    // Thorn-covered bludgeon. Heavy hit, bleed proc on thorns.
    public static Weapon CreateCactusBludgeon() => Make("cactus_bludgeon", "Cactus Bludgeon", 13500, "Legendary", 205, 36, 112,
        B().Add(StatType.Attack, 58).Add(StatType.Strength, 22).Add(StatType.Vitality, 18), "Bleed+15");

    // ── Hollow Fragment Avatar Weapons (Mace) ────────────────────

    public static Weapon CreateIjelfurAvatar() => Make("mce_ijelfur_avatar", "Ijelfur Avatar", 20500, "Legendary", 235, 82, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Strength, 24).Add(StatType.Vitality, 18), "Stun+15");

    // ── Lisbeth Rarity 6 Crafted (Mace) ──────────────────────────

    public static Weapon CreateDictatorsPunisher() => Make("mce_dictators_punisher", "Dictator's Punisher", 29500, "Legendary", 260, 88, 172,
        B().Add(StatType.Attack, 84).Add(StatType.Strength, 26).Add(StatType.Vitality, 20), "Stun+20");

    public static Weapon CreatePhotonHammerXPSmasher() => Make("mce_photon_hammer_xp_smasher", "Photon Hammer: XP Smasher", 30500, "Legendary", 260, 90, 175,
        B().Add(StatType.Attack, 86).Add(StatType.Strength, 28).Add(StatType.Vitality, 18), "XPBonus+20");
}
