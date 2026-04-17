using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all two-handed sword weapons.
// Two-Handed Swords: very high damage, slow speed. Strength-oriented.
public static class TwoHandedSwordDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Two-Handed Sword",
            BaseDamage = baseDmg, AttackSpeed = 3, Range = 1,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateIronGreatsword() => Make("iron_greatsword", "Iron Greatsword", 120, "Common", 55, 1, 15,
        B().Add(StatType.Attack, 12).Add(StatType.Strength, 4));

    public static Weapon CreateSteelGreatsword() => Make("steel_greatsword", "Steel Greatsword", 300, "Uncommon", 80, 10, 35,
        B().Add(StatType.Attack, 22).Add(StatType.Strength, 10));

    public static Weapon CreateMythrilGreatsword() => Make("mythril_greatsword", "Mythril Greatsword", 900, "Rare", 120, 25, 60,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 12));

    public static Weapon CreateAdamantiteGreatsword() => Make("adamantite_greatsword", "Adamantite Greatsword", 3000, "Epic", 180, 50, 100,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 20));

    public static Weapon CreateCelestialGreatsword() => Make("celestial_greatsword", "Celestial Greatsword", 7000, "Legendary", 230, 75, 165,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 28).Add(StatType.Agility, 8));

    public static Weapon CreateTyrantDragon() => Make("tyrant_dragon", "Tyrant Dragon", 1500, "Epic", 110, 30, 55,
        B().Add(StatType.Attack, 30).Add(StatType.Strength, 14), "PostMotion-1");

    // ── Hollow Realization Evolution Chain (2H Sword) ───────────────
    // Matter Dissolver -> Titan's Blade -> Ifrit -> Ascalon.

    // Matter Dissolver, greatsword said to thin the air it cleaves. T1 of the Ascalon chain.
    public static Weapon CreateMatterDissolver() => Make("matter_dissolver", "Matter Dissolver", 2200, "Rare", 120, 15, 70,
        B().Add(StatType.Attack, 35).Add(StatType.Strength, 10));

    // Titan's Blade, slab of mountain-iron shaped as a sword. T2 of the Ascalon chain.
    public static Weapon CreateTitansBlade() => Make("titans_blade", "Titan's Blade", 5800, "Epic", 160, 35, 115,
        B().Add(StatType.Attack, 55).Add(StatType.Strength, 15).Add(StatType.Vitality, 8), "CritRate+10");

    // Ifrit, flame-djinn zweihander whose edge glows volcanic. T3 of the Ascalon chain.
    public static Weapon CreateIfrit() => Make("ifrit", "Ifrit", 14000, "Legendary", 210, 60, 155,
        B().Add(StatType.Attack, 75).Add(StatType.Strength, 22).Add(StatType.Dexterity, 14), "Bleed+20");

    // T4 Divine of the Two-Handed Sword evolution chain.
    public static Weapon CreateAscalon() => Make("ascalon", "Ascalon", 22000, "Divine", 999, 80, 180,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 30), "SkillDamage+30");

    // F3 Integral Factor field boss drop. Veined green steel.
    public static Weapon CreateVerdantLord() => Make("verdant_lord", "Verdant Lord", 2600, "Rare", 140, 7, 42,
        B().Add(StatType.Attack, 22).Add(StatType.Strength, 10).Add(StatType.Vitality, 6), "CritHeal+8");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────

    // Hollow Fragment F87 implement. The saint's immortal sword — immune to crits (flavor).
    public static Weapon CreateSaintbladeDurandal() => Make("saintblade_durandal", "Saintblade: Durandal", 22000, "Legendary", 250, 84, 170,
        B().Add(StatType.Attack, 85).Add(StatType.Strength, 30).Add(StatType.Vitality, 15), "CritImmune+100");

    // Hollow Fragment F95 implement. Lancelot's holy greatsword.
    public static Weapon CreateStigmabladeArondight() => Make("stigmablade_arondight", "Stigmablade: Arondight", 28000, "Legendary", 260, 92, 175,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 28).Add(StatType.Dexterity, 22).Add(StatType.Agility, 15), "SkillCooldown-2");

    // Hollow Fragment F96 implement. Sigurd's dragon-slaying blade; pierces defense (flavor).
    public static Weapon CreateDemonbladeGram() => Make("demonblade_gram", "Demonblade: Gram", 30000, "Legendary", 260, 93, 180,
        B().Add(StatType.Attack, 100).Add(StatType.Strength, 35).Add(StatType.Dexterity, 15), "TrueStrike+15");

    // F40 Dracoflame the Elder Wyrm drops (Alicization Lycoris Divine Beast tier).
    // Blood-stream greatsword. Bleed on every swing.
    public static Weapon CreateDemonbladeCrimsonStream() => Make("demonblade_crimson_stream", "Demonblade: Crimson Stream", 16500, "Legendary", 220, 45, 142,
        B().Add(StatType.Attack, 72).Add(StatType.Strength, 28).Add(StatType.Dexterity, 10), "Bleed+30");

    // ── Integral Factor Series (2H Sword entries) ───────────────────

    // F14 Integral Series 2H sword. After Glow — sunset-colored zweihander.
    public static Weapon CreateIntegralAfterGlow() => Make("ths_integral_after_glow", "Integral After Glow", 5100, "Epic", 170, 14, 95,
        B().Add(StatType.Attack, 48).Add(StatType.Strength, 15).Add(StatType.Vitality, 8), "CritRate+10");

    // F25 Nox Series 2H sword. Nox After Glow — eclipsed counterpart, heavier swing.
    public static Weapon CreateNoxAfterGlow() => Make("ths_nox_after_glow", "Nox After Glow", 7800, "Epic", 180, 25, 105,
        B().Add(StatType.Attack, 50).Add(StatType.Strength, 18).Add(StatType.Vitality, 10), "Bleed+15");

    // ── Hollow Area Uniques (2H Sword) ───────────────────────────

    // HF F95 Hollow Area. Saintblade: Ragnarok — apex holy doom-blade, peak rare drop.
    public static Weapon CreateSaintbladeRagnarok() => Make("ths_saintblade_ragnarok", "Saintblade: Ragnarok", 30000, "Legendary", 265, 90, 180,
        B().Add(StatType.Attack, 95).Add(StatType.Strength, 32).Add(StatType.Vitality, 16).Add(StatType.Dexterity, 18), "HolyDamage+25");

    // ── Hollow Fragment Avatar Weapons (2H Sword) ────────────────

    public static Weapon CreateAbsoludiaAvatar() => Make("ths_absoludia_avatar", "Absoludia Avatar", 22500, "Legendary", 245, 84, 170,
        B().Add(StatType.Attack, 84).Add(StatType.Strength, 30).Add(StatType.Dexterity, 16), "ArmorPierce+25");

    // ── Lisbeth Rarity 6 Crafted (2H Sword) ──────────────────────

    public static Weapon CreateOgrebladeOverTheCross() => Make("ths_ogreblade_over_the_cross", "Ogreblade: Over the Cross", 31000, "Legendary", 260, 90, 178,
        B().Add(StatType.Attack, 88).Add(StatType.Strength, 32).Add(StatType.Vitality, 15), "Bleed+25");

    public static Weapon CreateDelivererMajesticLord() => Make("ths_deliverer_majestic_lord", "Deliverer: Majestic Lord", 32500, "Legendary", 265, 92, 182,
        B().Add(StatType.Attack, 92).Add(StatType.Strength, 32).Add(StatType.Dexterity, 18), "HolyDamage+20");

    public static Weapon CreateAmbitiousJuggernaut() => Make("ths_ambitious_juggernaut", "Ambitious Juggernaut", 34000, "Legendary", 275, 93, 185,
        B().Add(StatType.Attack, 94).Add(StatType.Strength, 33).Add(StatType.Vitality, 20), "Uninterruptible+50");

    // ── Infinity Moment shop 2H swords ──────────────────────────────

    // IM Epic-band shop weapon. Alpine flower-motif greatsword, cleaving arcs.
    public static Weapon CreateFasislawine() => Make("ths_fasislawine", "Fasislawine", 5200, "Epic", 165, 36, 132,
        B().Add(StatType.Attack, 58).Add(StatType.Strength, 18).Add(StatType.Dexterity, 10), "Cleave+30");

    // IM Legendary-band shop weapon. White-knight cavalry blade, armor-piercing.
    public static Weapon CreateWiceRitter() => Make("ths_wice_ritter", "Wice Ritter", 19500, "Legendary", 225, 78, 165,
        B().Add(StatType.Attack, 80).Add(StatType.Strength, 26).Add(StatType.Dexterity, 14), "ArmorPierce+20");

    // ── Infinity Moment LAB 2H sword (non-enhanceable) ──────────────

    // IM F92 floor-boss LAB reward. Cross-hilt cathedral greatsword.
    public static Weapon CreateSacredCross()
    {
        var w = Make("ths_sacred_cross", "Sacred Cross", 26000, "Legendary", 250, 85, 178,
            B().Add(StatType.Attack, 92).Add(StatType.Strength, 28).Add(StatType.Dexterity, 14),
            "HolyDamage+25");
        w.IsEnhanceable = false;
        return w;
    }
}
