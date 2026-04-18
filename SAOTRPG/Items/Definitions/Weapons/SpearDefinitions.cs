using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all spear weapons.
// Spears: moderate damage, reach (Range 2). Balanced stats.
public static class SpearDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Spear",
            BaseDamage = baseDmg, AttackSpeed = 1, Range = 2,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateWoodenSpear() => Make("wooden_spear", "Wooden Spear", 70, "Common", 40, 1, 9,
        B().Add(StatType.Attack, 7).Add(StatType.Dexterity, 2));

    public static Weapon CreateIronSpear() => Make("iron_spear", "Iron Spear", 230, "Uncommon", 65, 8, 21,
        B().Add(StatType.Attack, 13).Add(StatType.Dexterity, 5).Add(StatType.Strength, 3));

    // --- Tier 3 ---

    public static Weapon CreateMythrilSpear() => Make("mythril_spear", "Mythril Spear", 780, "Rare", 95, 25, 40,
        B().Add(StatType.Attack, 24).Add(StatType.Dexterity, 10).Add(StatType.Strength, 6));

    // --- Tier 4 ---

    public static Weapon CreateAdamantiteSpear() => Make("adamantite_spear", "Adamantite Spear", 2500, "Epic", 150, 50, 72,
        B().Add(StatType.Attack, 42).Add(StatType.Dexterity, 14).Add(StatType.Strength, 10));

    // --- Tier 5 ---

    public static Weapon CreateCelestialSpear() => Make("celestial_spear", "Celestial Spear", 5800, "Legendary", 200, 75, 120,
        B().Add(StatType.Attack, 65).Add(StatType.Dexterity, 20).Add(StatType.Strength, 15));

    // --- Named Weapons ---

    public static Weapon CreateGuiltyThorn() => Make("guilty_thorn", "Guilty Thorn", 800, "Epic", 80, 20, 30,
        B().Add(StatType.Attack, 20).Add(StatType.Strength, 8), "SlowOnHit+30");

    public static Weapon CreateAnubisSpear() => Make("anubis_spear", "Anubis Spear", 1400, "Rare", 110, 35, 50,
        B().Add(StatType.Attack, 32).Add(StatType.Dexterity, 10));

    // ── Hollow Realization Evolution Chain (Spear) ──────────────────
    // Heart Piercer -> Trishula -> Vijaya -> Caladbolg.

    // Heart Piercer, a pikeman's long-point — unerring thrust at range. T1 of the Caladbolg chain.
    public static Weapon CreateHeartPiercer() => Make("heart_piercer", "Heart Piercer", 1800, "Rare", 120, 15, 58,
        B().Add(StatType.Attack, 32).Add(StatType.Dexterity, 10));

    // Trishula, three-pointed trident of the destroyer. T2 of the Caladbolg chain.
    public static Weapon CreateTrishula() => Make("trishula", "Trishula", 5200, "Epic", 160, 35, 100,
        B().Add(StatType.Attack, 50).Add(StatType.Dexterity, 15).Add(StatType.Strength, 8), "CritRate+10");

    // Vijaya, Indra's victory-spear; never turns aside from true north. T3 of the Caladbolg chain.
    public static Weapon CreateVijaya() => Make("vijaya", "Vijaya", 12500, "Legendary", 210, 60, 140,
        B().Add(StatType.Attack, 70).Add(StatType.Dexterity, 22).Add(StatType.Strength, 14), "SkillCooldown-1");

    // T4 Divine of the Spear evolution chain.
    public static Weapon CreateCaladbolg() => Make("caladbolg", "Caladbolg", 18000, "Divine", 999, 80, 135,
        B().Add(StatType.Attack, 72).Add(StatType.Dexterity, 22).Add(StatType.Strength, 18), "RushRange+1");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────

    // Hollow Fragment F83 implement. Cú Chulainn's barbed spear; strikes true (flavor).
    public static Weapon CreateDemonspearGaeBolg() => Make("demonspear_gae_bolg", "Demonspear: Gae Bolg", 16500, "Legendary", 200, 79, 150,
        B().Add(StatType.Attack, 72).Add(StatType.Dexterity, 20).Add(StatType.Strength, 15), "TrueStrike+10");

    // Hollow Fragment F91 implement. King Arthur's holy lance; sustaining regen (flavor).
    public static Weapon CreateSaintspearRhongomyniad() => Make("saintspear_rhongomyniad", "Saintspear: Rhongomyniad", 23000, "Legendary", 230, 86, 163,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 25).Add(StatType.Vitality, 18), "HPRegen+5");

    // Hollow Fragment F98 implement. Odin's spear; returns SP with each strike (flavor).
    public static Weapon CreateGodspearGungnir() => Make("godspear_gungnir", "Godspear: Gungnir", 35000, "Legendary", 270, 94, 185,
        B().Add(StatType.Attack, 95).Add(StatType.Strength, 22).Add(StatType.Dexterity, 25).Add(StatType.Agility, 18).Add(StatType.Vitality, 15), "SPRegen+3");

    // ── Integral Factor Series (Spear entries) ──────────────────────

    // F61 Rosso Series spear. Sigrun — valkyrie-spear of the red-victory.
    public static Weapon CreateRossoSigrun() => Make("spr_rosso_sigrun", "Rosso Sigrun", 14200, "Legendary", 220, 55, 142,
        B().Add(StatType.Attack, 70).Add(StatType.Dexterity, 22).Add(StatType.Strength, 16), "RushRange+1");

    // ── Hollow Fragment Avatar Weapons (Spear) ────────────────────

    public static Weapon CreateAsleigeonAvatar() => Make("spr_asleigeon_avatar", "Asleigeon Avatar", 21500, "Legendary", 230, 82, 160,
        B().Add(StatType.Attack, 80).Add(StatType.Dexterity, 24).Add(StatType.Strength, 18), "TrueStrike+20");

    // ── Lisbeth Rarity 6 Crafted (Spear) ─────────────────────────

    public static Weapon CreateHeavenslanceElpisOrder() => Make("spr_heavenslance_elpis_order", "Heavenslance: Elpis Order", 30500, "Legendary", 260, 88, 175,
        B().Add(StatType.Attack, 84).Add(StatType.Dexterity, 26).Add(StatType.Strength, 18).Add(StatType.Vitality, 20), "HPRegen+5");

    // ── Infinity Moment shop spears ─────────────────────────────────

    // IM Epic-band shop weapon. Straight-thrust cavalry pike, heavy stab stack.
    public static Weapon CreateFoaStoss() => Make("spr_foa_stoss", "Foa Stoss", 5000, "Epic", 160, 36, 125,
        B().Add(StatType.Attack, 55).Add(StatType.Dexterity, 18).Add(StatType.Strength, 12), "ThrustDmg+20");

    // IM Legendary-band shop weapon. Cresting-wave polearm, frost spray.
    public static Weapon CreateWaveSchneider() => Make("spr_wave_schneider", "Wave Schneider", 20500, "Legendary", 225, 78, 163,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 24).Add(StatType.Strength, 16), "FrostDamage+25");

    // ── Infinity Moment LAB spear (non-enhanceable) ─────────────────

    // IM F98 floor-boss LAB reward. Moon-whisper spear; power waxes at night.
    public static Weapon CreateLunaticRoof()
    {
        var w = Make("spr_lunatic_roof", "Lunatic Roof", 27500, "Legendary", 255, 87, 185,
            B().Add(StatType.Attack, 96).Add(StatType.Dexterity, 28).Add(StatType.Strength, 20).Add(StatType.Agility, 14),
            "Lunacy+30");
        w.IsEnhanceable = false;
        return w;
    }

    // ── Memory Defrag Originals (Spear entries) ────────────────────────

    // MD Epic. Oceanic trident-spear, brine-iron tip.
    public static Weapon CreateNeoAtlantis() => Make("spr_neo_atlantis", "Neo Atlantis", 5800, "Epic", 165, 50, 120,
        B().Add(StatType.Attack, 55).Add(StatType.Dexterity, 18).Add(StatType.Strength, 12), "FrostDamage+15");

    // ── Alicization Lycoris Normal Raid (Spear) — Epic F70-85 ────────

    // Heavenstriker — skyward-point ascending spear, holy motif.
    public static Weapon CreateHeavenstriker() => Make("spr_heavenstriker", "Heavenstriker", 7000, "Epic", 175, 71, 128,
        B().Add(StatType.Attack, 60).Add(StatType.Dexterity, 20).Add(StatType.Strength, 14), "HolyDamage+15");

    // ── Alicization Lycoris Extreme Raid (Spear) — Legendary F85-95 ──

    // Arctic Pillar — frost-bound spear shaft, freeze on thrust.
    public static Weapon CreateArcticPillar() => Make("spr_arctic_pillar", "Arctic Pillar", 19200, "Legendary", 222, 85, 158,
        B().Add(StatType.Attack, 80).Add(StatType.Dexterity, 24).Add(StatType.Strength, 14), "Freeze+25");

    // ── Alicization Lycoris Relic Boss Drops (Spear) — Legendary F80-95 ─

    // Frostpeak — glacial-tip relic spear, matches AL Relic band.
    public static Weapon CreateFrostpeak() => Make("spr_frostpeak", "Frostpeak", 18500, "Legendary", 218, 82, 152,
        B().Add(StatType.Attack, 76).Add(StatType.Dexterity, 22).Add(StatType.Strength, 14), "Freeze+25");

    // ── SAO Lost Song Top-Tier (Spear) — Legendary F80-95 ──────────
    // Brave Song — heroic-call polearm, combo-extender.
    public static Weapon CreateBraveSong() => Make("spr_brave_song", "Brave Song", 19500, "Legendary", 225, 84, 158,
        B().Add(StatType.Attack, 80).Add(StatType.Dexterity, 24).Add(StatType.Strength, 14), "ComboBonus+40");

    // ── SAO Lost Song Mythological (Spear) — Legendary F75-99 ──────

    // Divine Laevateinn — Surtr's flame-spear, burn on thrust.
    public static Weapon CreateDivineLaevateinn() => Make("spr_divine_laevateinn", "Divine Laevateinn", 20500, "Legendary", 230, 86, 165,
        B().Add(StatType.Attack, 84).Add(StatType.Dexterity, 24).Add(StatType.Strength, 16), "Burn+30");

    // Elder's Trident — water-element trident of the elders.
    public static Weapon CreateEldersTrident() => Make("spr_elders_trident", "Elder's Trident", 19000, "Legendary", 220, 82, 155,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 22).Add(StatType.Strength, 14), "Freeze+20");
}
