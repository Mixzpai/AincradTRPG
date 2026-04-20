using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all bow weapons.
// Bows: moderate damage, ranged (Range 2+). Dexterity-oriented.
public static class BowDefinitions
{
    private static Weapon Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDmg, int attackSpeed, int range, StatModifierCollection bonuses, string? specialEffect = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Weapon", WeaponType = "Bow",
            BaseDamage = baseDmg, AttackSpeed = attackSpeed, Range = range,
            SpecialEffect = specialEffect, Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Weapon CreateShortBow() => Make("short_bow", "Short Bow", 80, "Common", 40, 1, 8, 1, 2,
        B().Add(StatType.Attack, 6).Add(StatType.Dexterity, 4));

    public static Weapon CreateLongBow() => Make("long_bow", "Long Bow", 260, "Uncommon", 60, 10, 20, 2, 3,
        B().Add(StatType.Attack, 14).Add(StatType.Dexterity, 7));

    // --- Tier 3 ---

    public static Weapon CreateMythrilBow() => Make("mythril_bow", "Mythril Bow", 720, "Rare", 85, 25, 35, 1, 3,
        B().Add(StatType.Attack, 22).Add(StatType.Dexterity, 12));

    // --- Tier 4 ---

    public static Weapon CreateAdamantiteBow() => Make("adamantite_bow", "Adamantite Bow", 2200, "Epic", 140, 50, 60, 1, 3,
        B().Add(StatType.Attack, 38).Add(StatType.Dexterity, 18));

    // --- Tier 5 ---

    public static Weapon CreateCelestialBow() => Make("celestial_bow", "Celestial Bow", 5500, "Legendary", 180, 75, 100, 2, 4,
        B().Add(StatType.Attack, 58).Add(StatType.Dexterity, 24).Add(StatType.Agility, 10));

    // --- Named Weapons ---

    public static Weapon CreateTiasLongbow() => Make("tias_longbow", "Tia's Longbow", 900, "Rare", 90, 25, 32, 2, 4,
        B().Add(StatType.Attack, 22).Add(StatType.Dexterity, 12));

    // ── IM Last-Attack Bonus bows — guaranteed on floor-boss killing blow. Non-enhanceable; higher base vs Legendary.

    // IM F85 floor-boss LAB reward. Wind-sheared hunting bow.
    public static Weapon CreateZephyros()
    {
        var w = Make("bow_zephyros", "Zephyros", 24000, "Legendary", 240, 82, 170, 1, 4,
            B().Add(StatType.Attack, 88).Add(StatType.Dexterity, 22).Add(StatType.Agility, 14),
            "Burst+20");
        w.IsEnhanceable = false;
        return w;
    }

    // IM F99 LAB reward (paired with Night Sky Divine drop). Piercing moon-touched huntress bow.
    public static Weapon CreateArtemis()
    {
        var w = Make("bow_artemis", "Artemis", 32000, "Legendary", 260, 92, 188, 1, 5,
            B().Add(StatType.Attack, 98).Add(StatType.Dexterity, 28).Add(StatType.Agility, 18),
            "PiercingShot+25");
        w.IsEnhanceable = false;
        return w;
    }

    // ── Divine Objects — above Legendary, hand-placed, unbreakable, bypass block rolls.

    // Deusolbert Synthesis Seven — phoenix-forged; Full Control Art fires unlimited flame arrows.
    public static Weapon CreateConflagrantFlameBow() => Make("conflagrant_flame_bow", "Conflagrant Flame Bow", 42000, "Divine", 999, 82, 155, 2, 4,
        B().Add(StatType.Attack, 82).Add(StatType.Dexterity, 28).Add(StatType.Strength, 18), "Burn+30");

    // F11 Felos Ember Drake drop (AL Divine Beast tier). Cosmic-range crit bow.
    public static Weapon CreateStarfall() => Make("starfall", "Starfall", 8500, "Legendary", 170, 18, 85, 1, 4,
        B().Add(StatType.Attack, 42).Add(StatType.Dexterity, 20).Add(StatType.Agility, 10), "CritRate+20");

    // ── Integral Factor Series (Bow entries) ────────────────────────

    // F14 Integral Series bow. Arc Angel — pale-feathered holy-shot bow.
    public static Weapon CreateIntegralArcAngel() => Make("bow_integral_arc_angel", "Integral Arc Angel", 4600, "Epic", 155, 14, 78, 1, 3,
        B().Add(StatType.Attack, 40).Add(StatType.Dexterity, 16).Add(StatType.Agility, 8), "HolyDamage+10");

    // F25 Nox Series bow. Nox Arc Angel — dark-feathered counterpart from B5F.
    public static Weapon CreateNoxArcAngel() => Make("bow_nox_arc_angel", "Nox Arc Angel", 7000, "Epic", 170, 25, 90, 1, 3,
        B().Add(StatType.Attack, 46).Add(StatType.Dexterity, 18).Add(StatType.Agility, 10), "Bleed+12");

    // F61 Rosso Series bow. Albatross — red-plumed greatbow, long-travel shots.
    public static Weapon CreateRossoAlbatross() => Make("bow_rosso_albatross", "Rosso Albatross", 13500, "Legendary", 215, 55, 140, 1, 4,
        B().Add(StatType.Attack, 68).Add(StatType.Dexterity, 26).Add(StatType.Agility, 12), "CritRate+15");

    // ── Hollow Area Uniques (Bow) — Rare/Epic drops ──────────────

    // HF F55 Hollow Area. Shroudbow — Star Stitcher; stitches constellations into the air as it fires.
    public static Weapon CreateShroudbowStarStitcher() => Make("bow_shroudbow_star_stitcher", "Shroudbow: Star Stitcher", 7400, "Epic", 170, 50, 92, 1, 4,
        B().Add(StatType.Attack, 46).Add(StatType.Dexterity, 22).Add(StatType.Agility, 12), "CritRate+15");

    // ── Memory Defrag Originals (Bow entries) ──────────────────────────

    // MD Epic. Tide-spread longbow, diffused freezing barrage.
    public static Weapon CreateAquaSpread() => Make("bow_aqua_spread", "Aqua Spread", 5600, "Epic", 160, 50, 118, 1, 4,
        B().Add(StatType.Attack, 52).Add(StatType.Dexterity, 22).Add(StatType.Agility, 10), "FrostDamage+20");

    // MD Rare. Supportive bow of the heart-crowd; crit-boost on encourage shots.
    public static Weapon CreateCheerOfLoveBow() => Make("bow_cheer_of_love", "Cheer of Love Bow", 2600, "Rare", 115, 30, 65, 1, 3,
        B().Add(StatType.Attack, 30).Add(StatType.Dexterity, 14).Add(StatType.Agility, 6), "CritRate+10");

    // ── Alicization Lycoris Relic Boss Drops (Bow) — Legendary F80-95 ─

    // Cinder Bow — ember-fletched longbow, burning arrow motif.
    public static Weapon CreateCinderBow() => Make("bow_cinder_bow", "Cinder Bow", 18500, "Legendary", 218, 82, 150, 1, 4,
        B().Add(StatType.Attack, 76).Add(StatType.Dexterity, 26).Add(StatType.Agility, 14), "Burn+25");

    // ── Alicization Lycoris DLC (Bow) — Legendary F85-99 ───────────

    // Loveblight Bow — DLC cursed-compassion bow, deep bleed.
    public static Weapon CreateLoveblightBow() => Make("bow_loveblight_bow", "Loveblight Bow", 20000, "Legendary", 225, 86, 158, 1, 4,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 28).Add(StatType.Agility, 16), "Bleed+30");

    // Glitzwood Bow — DLC lacquered-wood crit-bow.
    public static Weapon CreateGlitzwoodBow() => Make("bow_glitzwood_bow", "Glitzwood Bow", 20500, "Legendary", 225, 87, 160, 1, 4,
        B().Add(StatType.Attack, 80).Add(StatType.Dexterity, 28).Add(StatType.Agility, 16), "CritRate+25");

    // ── SAO Lost Song Top-Tier (Bow) — Legendary F80-95 ──────────
    // Silvan Bow — canonical LS apex bow, forest-hunter crit.
    public static Weapon CreateSilvanBow() => Make("bow_silvan_bow", "Silvan Bow", 19200, "Legendary", 220, 83, 155, 1, 4,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 26).Add(StatType.Agility, 14), "CritRate+20");

    // ── SAO Lost Song Mythological (Bow) — Legendary F75-99 ──────────

    // Holy L'arc Qui ne Faut — holy piercing bow of infallible aim.
    public static Weapon CreateHolyLarcQuiNeFaut() => Make("bow_holy_larc_qui_ne_faut", "Holy L'arc Qui ne Faut", 20000, "Legendary", 225, 85, 160, 1, 4,
        B().Add(StatType.Attack, 80).Add(StatType.Dexterity, 28).Add(StatType.Agility, 14), "PiercingShot+25");

    // Artemis' Fult — hunter-goddess shortbow, crit-focused.
    public static Weapon CreateArtemisFult() => Make("bow_artemis_fult", "Artemis' Fult", 19500, "Legendary", 220, 84, 158, 1, 4,
        B().Add(StatType.Attack, 78).Add(StatType.Dexterity, 28).Add(StatType.Agility, 16), "CritRate+25");
}
