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

    // ── Hollow Realization Evolution Chain (Katana) ─────────────────
    // Matamon -> Shishi-Otoshi -> Shichishito -> Masamune.

    // Matamon, oni-marked first-blade, smoldering faintly red. T1 of the Masamune chain.
    public static Weapon CreateMatamon() => Make("matamon", "Matamon", 1900, "Rare", 120, 15, 60, 0,
        B().Add(StatType.Attack, 35).Add(StatType.Agility, 10));

    // Shishi-Otoshi, deer-startler — balance strike that disarms poise. T2 of the Masamune chain.
    public static Weapon CreateShishiOtoshi() => Make("shishi_otoshi", "Shishi-Otoshi", 5400, "Epic", 160, 35, 105, 1,
        B().Add(StatType.Attack, 55).Add(StatType.Agility, 15).Add(StatType.Speed, 8), "CritRate+10");

    // Shichishito, seven-branched blade granting seven angles of strike. T3 of the Masamune chain.
    public static Weapon CreateShichishito() => Make("shichishito", "Shichishito", 13000, "Legendary", 210, 60, 145, 1,
        B().Add(StatType.Attack, 72).Add(StatType.Agility, 22).Add(StatType.Speed, 14), "CritRate+15");

    // T4 Divine of the Katana evolution chain.
    public static Weapon CreateMasamune() => Make("masamune", "Masamune", 20000, "Divine", 999, 80, 150, 1,
        B().Add(StatType.Attack, 78).Add(StatType.Agility, 25).Add(StatType.Speed, 18), "CritRate+20");

    // F2 hidden miniboss drop. Violet-black, drinks HP from what it cuts.
    public static Weapon CreateSoulEater() => Make("soul_eater", "Soul Eater", 2400, "Rare", 120, 5, 30, 1,
        B().Add(StatType.Attack, 16).Add(StatType.Strength, 6).Add(StatType.Agility, 6), "CritHeal+10");

    // Klein's shadow-stitcher upgrade. Awarded by befriending him through F1-F30.
    public static Weapon CreateKagenui() => Make("kagenui", "Kagenui", 7800, "Epic", 180, 35, 82, 1,
        B().Add(StatType.Attack, 42).Add(StatType.Agility, 15).Add(StatType.Dexterity, 10), "BackstabDmg+25");

    // ── Hollow Fragment / Infinity Moment Legendaries ──────────────

    // Hollow Fragment F80 implement. Oni-slaying blade, sheds crimson mist.
    public static Weapon CreateJatoOnikirimaru() => Make("jato_onikirimaru", "Jato: Onikiri-maru", 15000, "Legendary", 190, 76, 145, 1,
        B().Add(StatType.Attack, 68).Add(StatType.Dexterity, 22).Add(StatType.Agility, 15), "Bleed+20");

    // Hollow Fragment F95 implement. Susanoo's cloud-splitter; SP regen on evade (flavor).
    public static Weapon CreateShintoAmaNoMurakumo() => Make("shinto_ama_no_murakumo", "Shinto: Ama-no-Murakumo", 26000, "Legendary", 240, 90, 170, 1,
        B().Add(StatType.Attack, 82).Add(StatType.Dexterity, 25).Add(StatType.Agility, 20).Add(StatType.Speed, 15), "EvadeRegen+10");

    // Hollow Area endgame find. The Yato-grade Masamune — cuts without sound.
    public static Weapon CreateYatoMasamune() => Make("yato_masamune", "Yato: Masamune", 32000, "Legendary", 250, 95, 175, 1,
        B().Add(StatType.Attack, 90).Add(StatType.Strength, 20).Add(StatType.Dexterity, 25).Add(StatType.Agility, 15), "Bleed+30");

    // F49 Shadowstep Assassin drops (Alicization Lycoris Divine Beast tier).
    // Stealth katana. Bleed-on-hit, favors sudden strikes.
    public static Weapon CreateMidnightSun() => Make("midnight_sun", "Midnight Sun", 17000, "Legendary", 220, 48, 145, 1,
        B().Add(StatType.Attack, 70).Add(StatType.Dexterity, 22).Add(StatType.Agility, 18).Add(StatType.Speed, 12), "Bleed+20");

    // ── Integral Factor Series (Katana entries) ─────────────────────

    // F85→F87 Yasha Series katana. Oratorio — chant-blade, resonates on multi-hit combos.
    public static Weapon CreateYashaOratorio() => Make("kat_yasha_oratorio", "Yasha Oratorio", 18500, "Legendary", 230, 78, 148, 1,
        B().Add(StatType.Attack, 72).Add(StatType.Agility, 22).Add(StatType.Dexterity, 18).Add(StatType.Speed, 12), "ComboBonus+40");

    // F90+ Gaou Series katana. Oratorio — demon-king refrain of the same resonant blade lineage.
    public static Weapon CreateGaouOratorio() => Make("kat_gaou_oratorio", "Gaou Oratorio", 28000, "Legendary", 260, 88, 172, 1,
        B().Add(StatType.Attack, 86).Add(StatType.Agility, 26).Add(StatType.Dexterity, 20).Add(StatType.Speed, 16), "Bleed+25");

    // ── Hollow Fragment Avatar Weapons (Katana) ───────────────────

    public static Weapon CreateBurningHazeAvatar() => Make("kat_burning_haze_avatar", "Burning Haze Avatar", 21000, "Legendary", 230, 83, 162, 1,
        B().Add(StatType.Attack, 80).Add(StatType.Agility, 24).Add(StatType.Strength, 18).Add(StatType.Dexterity, 18), "Burn+20");

    // ── Lisbeth Rarity 6 Crafted (Katana) ─────────────────────────

    public static Weapon CreateGodslayerTatteredHope() => Make("kat_godslayer_tattered_hope", "Godslayer: Tattered Hope", 31000, "Legendary", 260, 90, 175, 1,
        B().Add(StatType.Attack, 86).Add(StatType.Agility, 26).Add(StatType.Dexterity, 20).Add(StatType.Speed, 16), "SkillCooldown-2");

    public static Weapon CreateAvidyaSamsaraBlade() => Make("kat_avidya_samsara_blade", "Avidya Samsara Blade", 33000, "Legendary", 270, 92, 183, 1,
        B().Add(StatType.Attack, 90).Add(StatType.Agility, 28).Add(StatType.Dexterity, 22).Add(StatType.Speed, 18), "CritImmune+100");

    // ── Infinity Moment shop katana ─────────────────────────────────

    // IM Legendary-band shop weapon. Canon demon-smith folded katana; hemorrhage
    // bleed theme. DefId uses kat_muramasa to avoid colliding with the existing
    // HR evolution chain's Masamune (Divine T4, memory #409).
    public static Weapon CreateMuramasa() => Make("kat_muramasa", "Muramasa", 21000, "Legendary", 230, 80, 168, 1,
        B().Add(StatType.Attack, 80).Add(StatType.Agility, 24).Add(StatType.Dexterity, 20).Add(StatType.Strength, 14), "Bleed+30");

    // ── Infinity Moment LAB katana (non-enhanceable) ────────────────

    // IM F94 floor-boss LAB reward. Canon name is 朔 (new-moon) kanji; we
    // render as plain "Saku" for terminal-font compatibility (Tyler Q1=b).
    // Shaded night-cutter blade; damage climbs in darkness.
    public static Weapon CreateSaku()
    {
        var w = Make("kat_saku", "Saku", 25000, "Legendary", 245, 84, 175, 1,
            B().Add(StatType.Attack, 88).Add(StatType.Agility, 26).Add(StatType.Dexterity, 20).Add(StatType.Speed, 14),
            "NightDamage+20");
        w.IsEnhanceable = false;
        return w;
    }
}
