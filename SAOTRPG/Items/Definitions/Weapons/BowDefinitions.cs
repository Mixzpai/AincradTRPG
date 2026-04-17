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

    // ── Divine Objects ──────────────────────────────────────────────
    // Above Legendary. Hand-placed only. Unbreakable. Bypass block rolls.

    // Deusolbert Synthesis Seven's Divine Object. Created from a phoenix that
    // lived in a volcano. Canon: fires unlimited flaming arrows via Full Control Art.
    public static Weapon CreateConflagrantFlameBow() => Make("conflagrant_flame_bow", "Conflagrant Flame Bow", 42000, "Divine", 999, 82, 155, 2, 4,
        B().Add(StatType.Attack, 82).Add(StatType.Dexterity, 28).Add(StatType.Strength, 18), "Burn+30");

    // F11 Felos the Ember Drake drops (Alicization Lycoris Divine Beast tier).
    // Celestial bow — hits from cosmic distance. Crit-oriented.
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
}
