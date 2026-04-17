using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all rapier weapons.
// Rapiers: fast, precise thrusting weapons. Dexterity/Speed-oriented.
public static class RapierDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, StatModifierCollection bonuses, string? specialEffect = null, int range = 1)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Rapier",
            BaseDamage = baseDmg, AttackSpeed = 0, Range = range,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateCopperRapier() => Make("copper_rapier", "Copper Rapier", 95, "Common", 40, 1, 8,
        B().Add(StatType.Attack, 6).Add(StatType.Dexterity, 3).Add(StatType.Speed, 2));

    public static Weapon CreateSteelRapier() => Make("steel_rapier", "Steel Rapier", 250, "Uncommon", 60, 10, 18,
        B().Add(StatType.Attack, 12).Add(StatType.Dexterity, 6).Add(StatType.Speed, 5));

    public static Weapon CreateMythrilRapier() => Make("mythril_rapier", "Mythril Rapier", 750, "Rare", 90, 25, 35,
        B().Add(StatType.Attack, 20).Add(StatType.Dexterity, 10).Add(StatType.Speed, 8));

    public static Weapon CreateAdamantiteRapier() => Make("adamantite_rapier", "Adamantite Rapier", 2400, "Epic", 140, 50, 65,
        B().Add(StatType.Attack, 38).Add(StatType.Dexterity, 15).Add(StatType.Speed, 12));

    public static Weapon CreateCelestialRapier() => Make("celestial_rapier", "Celestial Rapier", 5800, "Legendary", 190, 75, 110,
        B().Add(StatType.Attack, 60).Add(StatType.Dexterity, 20).Add(StatType.Speed, 18));

    // --- Named / SAO Legendary Weapons ---

    public static Weapon CreateChivalricRapier() => Make("chivalric_rapier", "Chivalric Rapier", 180, "Uncommon", 55, 8, 12,
        B().Add(StatType.Attack, 10).Add(StatType.Dexterity, 5));

    public static Weapon CreateWindFleuret() => Make("wind_fleuret", "Wind Fleuret", 350, "Rare", 65, 10, 16,
        B().Add(StatType.Attack, 14).Add(StatType.Speed, 8).Add(StatType.Dexterity, 5), "RushRange+1");

    public static Weapon CreateAgateRapier() => Make("agate_rapier", "Agate Rapier", 600, "Rare", 80, 20, 28,
        B().Add(StatType.Attack, 18).Add(StatType.Dexterity, 8).Add(StatType.Speed, 6));

    public static Weapon CreateLambentLight() => Make("lambent_light", "Lambent Light", 8000, "Legendary", 180, 40, 70,
        B().Add(StatType.Attack, 42).Add(StatType.Speed, 20).Add(StatType.Dexterity, 15), "CritRate+20");

    // ── Hollow Realization Evolution Chain (Rapier) ─────────────────
    // Prima Sabre -> Pentagramme -> Charadrios -> Hexagramme.

    // Prima Sabre, duellist's first true blade — geometry in steel. T1 of the Hexagramme chain.
    public static Weapon CreatePrimaSabre() => Make("prima_sabre", "Prima Sabre", 1700, "Rare", 120, 15, 55,
        B().Add(StatType.Attack, 30).Add(StatType.Dexterity, 10).Add(StatType.Speed, 5));

    // Pentagramme, five-point hex-bound rapier. T2 of the Hexagramme chain.
    public static Weapon CreatePentagramme() => Make("pentagramme", "Pentagramme", 5000, "Epic", 160, 35, 95,
        B().Add(StatType.Attack, 48).Add(StatType.Dexterity, 15).Add(StatType.Speed, 8), "CritRate+10");

    // Charadrios, healer-bird rapier said to lift curses on clean strikes. T3 of the Hexagramme chain.
    public static Weapon CreateCharadrios() => Make("charadrios", "Charadrios", 11500, "Legendary", 210, 60, 135,
        B().Add(StatType.Attack, 68).Add(StatType.Dexterity, 22).Add(StatType.Speed, 14), "CritRate+15");

    // T4 Divine of the Rapier evolution chain.
    public static Weapon CreateHexagramme() => Make("hexagramme", "Hexagramme", 18000, "Divine", 999, 75, 120,
        B().Add(StatType.Attack, 65).Add(StatType.Dexterity, 22).Add(StatType.Speed, 20), "SkillCooldown-1");

    // Post-game successor to Lambent Light. Brighter, faster, unmistakably Asuna's.
    public static Weapon CreateRadiantLight() => Make("radiant_light", "Radiant Light", 24000, "Legendary", 240, 85, 140,
        B().Add(StatType.Attack, 72).Add(StatType.Dexterity, 26).Add(StatType.Speed, 24), "CritRate+20");

    // Yuuki's 11-hit OSS carrier. Endgame reward — combo bonus amplifies multi-hit skills.
    public static Weapon CreateMothersRosario() => Make("mothers_rosario", "Mother's Rosario", 32000, "Legendary", 280, 90, 175,
        B().Add(StatType.Attack, 85).Add(StatType.Dexterity, 30).Add(StatType.Speed, 26).Add(StatType.Agility, 18), "ComboBonus+50");

    // ── Divine Objects ──────────────────────────────────────────────
    // Above Legendary. Hand-placed only. Unbreakable. Bypass block rolls.

    // Fanatio Synthesis Two's Divine Object. Fires concentrated beams of light
    // that pierce nearly anything. Extended reach (Range 2) over normal rapiers.
    public static Weapon CreateHeavenPiercingBlade() => Make("heaven_piercing_blade", "Heaven-Piercing Blade", 42000, "Divine", 999, 82, 165,
        B().Add(StatType.Attack, 82).Add(StatType.Dexterity, 30).Add(StatType.Speed, 22).Add(StatType.Agility, 12), "PiercingBeam+30", range: 2);

    // F43 Undine the Water Maiden drops (Alicization Lycoris Divine Beast tier).
    // Rainfall rapier. Slows on hit — freezing droplets.
    public static Weapon CreateMidnightRain() => Make("midnight_rain", "Midnight Rain", 14500, "Legendary", 200, 42, 118,
        B().Add(StatType.Attack, 60).Add(StatType.Dexterity, 25).Add(StatType.Speed, 18).Add(StatType.Agility, 10), "Freeze+15");

    // ── Integral Factor Series (Rapier entries) ─────────────────────

    // F14 Integral Series rapier. Gusion, demon-duke thruster of the dawn series.
    public static Weapon CreateIntegralGusion() => Make("rap_integral_gusion", "Integral Gusion", 4700, "Epic", 155, 14, 75,
        B().Add(StatType.Attack, 38).Add(StatType.Dexterity, 15).Add(StatType.Speed, 10), "CritRate+10");

    // F25 Nox Series rapier. Nox Gusion — obsidian counterpart, faster snap.
    public static Weapon CreateNoxGusion() => Make("rap_nox_gusion", "Nox Gusion", 7100, "Epic", 170, 25, 88,
        B().Add(StatType.Attack, 44).Add(StatType.Dexterity, 18).Add(StatType.Speed, 14), "Bleed+10");

    // F61 Rosso Series rapier. Rhapsody — crimson dueling-rapier, sings on the thrust.
    public static Weapon CreateRossoRhapsody() => Make("rap_rosso_rhapsody", "Rosso Rhapsody", 13800, "Legendary", 215, 55, 138,
        B().Add(StatType.Attack, 66).Add(StatType.Dexterity, 26).Add(StatType.Speed, 20), "CritRate+20");

    // ── Hollow Fragment Implement System (Rapier) ─────────────────

    // HF F84 implement. Spiralblade — extra auto-attack chance (flavor).
    public static Weapon CreateSpiralbladeRenderingFail() => Make("rap_spiralblade_rendering_fail", "Spiralblade: Rendering Fail", 17500, "Legendary", 210, 80, 150,
        B().Add(StatType.Attack, 75).Add(StatType.Dexterity, 24).Add(StatType.Speed, 20), "ExtraAutoAttack+20");

    // HF F93 implement. Glimmerblade — Banishing Ray; skill charge reduction (flavor).
    public static Weapon CreateGlimmerbladeBanishingRay() => Make("rap_glimmerblade_banishing_ray", "Glimmerblade: Banishing Ray", 24000, "Legendary", 240, 88, 170,
        B().Add(StatType.Attack, 85).Add(StatType.Dexterity, 28).Add(StatType.Speed, 22).Add(StatType.Agility, 15), "SkillChargeReduction+20");

    // ── Hollow Fragment Avatar Weapons (Rapier) ───────────────────
    // Last-Attack Bonus drops from F70+ field bosses. See LootGenerator canon HNM list.

    public static Weapon CreateIshvalcaAvatar() => Make("rap_ishvalca_avatar", "Ishvalca Avatar", 20000, "Legendary", 225, 82, 158,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 26).Add(StatType.Speed, 22).Add(StatType.Agility, 14), "ParryChance+15");

    // ── Lisbeth Rarity 6 Crafted (Rapier) ─────────────────────────

    public static Weapon CreateChampionfoilRadiantChariot() => Make("rap_championfoil_radiant_chariot", "Championfoil: Radiant Chariot", 28500, "Legendary", 250, 86, 170,
        B().Add(StatType.Attack, 82).Add(StatType.Dexterity, 28).Add(StatType.Speed, 24).Add(StatType.Agility, 16), "AttackSpeed+3");

    public static Weapon CreateGlimmerspineSilverBullet() => Make("rap_glimmerspine_silver_bullet", "Glimmerspine: Silver Bullet", 30000, "Legendary", 255, 88, 175,
        B().Add(StatType.Attack, 84).Add(StatType.Dexterity, 30).Add(StatType.Speed, 24), "TrueStrike+15");

    // ── Infinity Moment shop rapiers ────────────────────────────────

    // IM Epic-band shop weapon. Alpine flower foil, precise critical threading.
    public static Weapon CreateEdelweiss() => Make("rap_edelweiss", "Edelweiss", 4600, "Epic", 150, 36, 122,
        B().Add(StatType.Attack, 52).Add(StatType.Dexterity, 20).Add(StatType.Speed, 12), "CritRate+15");

    // IM Legendary-band shop weapon. Night-street duelist estoc, hemorrhage edge.
    public static Weapon CreateNoctisStrasse() => Make("rap_noctis_strasse", "Noctis Strasse", 18800, "Legendary", 220, 76, 158,
        B().Add(StatType.Attack, 74).Add(StatType.Dexterity, 24).Add(StatType.Speed, 20), "Bleed+20");
}
