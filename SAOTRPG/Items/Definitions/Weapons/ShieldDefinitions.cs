using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

// Static registry of all shield items.
// Shields are Armor (OffHand slot) with BlockChance.
// Defense/Vitality-oriented.
public static class ShieldDefinitions
{
    // Mirror the 1H-sword Make() pattern for consistency with the rest of the
    // weapon definition files. Shields are Armor subtype; slot = "Shield".
    //
    // NOTE: Armor has no SpecialEffect field (only Weapon does). IF shields' canon-flavor
    // effects (DamageReflect, HPRegen, Barrier, CritImmune) are annotated in comments
    // only — they are flavor-only in the current parser.
    private static Armor Make(string id, string name, int value, string rarity, int durability,
        int level, int baseDefense, int weight, int blockChance,
        StatModifierCollection bonuses)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = durability, RequiredLevel = level,
            EquipmentType = "Armor", ArmorSlot = "Shield",
            BaseDefense = baseDefense, Weight = weight, BlockChance = blockChance,
            Bonuses = bonuses,
        };

    private static StatModifierCollection B() => new();

    public static Armor CreateWoodenShield() => new()
    {
        DefinitionId = "wooden_shield",
        Name = "Wooden Shield",
        Value = 65,
        Rarity = "Common",
        ItemDurability = 45,
        RequiredLevel = 1,
        EquipmentType = "Armor",
        ArmorSlot = "Shield",
        BaseDefense = 4,
        Weight = 6,
        BlockChance = 10,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 4)
    };

    public static Armor CreateIronShield() => new()
    {
        DefinitionId = "iron_shield",
        Name = "Iron Shield",
        Value = 210,
        Rarity = "Uncommon",
        ItemDurability = 70,
        RequiredLevel = 8,
        EquipmentType = "Armor",
        ArmorSlot = "Shield",
        BaseDefense = 10,
        Weight = 10,
        BlockChance = 18,
        Bonuses = new StatModifierCollection()
            .Add(StatType.Defense, 10)
            .Add(StatType.Vitality, 4)
    };

    // ── Integral Factor Series Shields ──────────────────────────────

    // F14 Integral Series shield. Fermat — endgame-shield of the Integral series.
    // Canon (wiki-confirmed). Epic rarity, defense+block all-rounder.
    public static Armor CreateFermat() => Make("shd_fermat", "Fermat", 4800, "Epic",
        150, 14, 18, 14, 28,
        B().Add(StatType.Defense, 12).Add(StatType.Vitality, 8));

    // F25 Nox Series shield. Nox Fermat — Underground Labyrinth B5F shadow-forged counterpart.
    // Canon (wiki + IF Facebook announce). Epic rarity; flavor: DamageReflect+5.
    public static Armor CreateNoxFermat() => Make("shd_nox_fermat", "Nox Fermat", 7500, "Epic",
        170, 25, 22, 18, 32,
        B().Add(StatType.Defense, 18).Add(StatType.Vitality, 10));

    // F61 Rosso Series shield. [INVENTED] — Italian "red" + Greek aegis. Legendary.
    // Flavor: CritImmune+5.
    public static Armor CreateRossoAegis() => Make("shd_rosso_aegis", "Rosso Aegis", 14000, "Legendary",
        210, 55, 32, 22, 38,
        B().Add(StatType.Defense, 30).Add(StatType.Vitality, 14));

    // F87 Yasha Series shield. [INVENTED] — Sanskrit armor-word "kavacha". Legendary.
    // Flavor: HPRegen+3.
    public static Armor CreateYashaKavacha() => Make("shd_yasha_kavacha", "Yasha Kavacha", 19500, "Legendary",
        230, 78, 38, 26, 42,
        B().Add(StatType.Defense, 38).Add(StatType.Vitality, 18));

    // F90+ Gaou Series shield. [INVENTED] — Japanese "curse" + demon-king. Legendary.
    // Flavor: Barrier+10.
    public static Armor CreateGaouTatari() => Make("shd_gaou_tatari", "Gaou Tatari", 28000, "Legendary",
        255, 88, 46, 30, 46,
        B().Add(StatType.Defense, 45).Add(StatType.Vitality, 22));

    // ── SAO Lost Song Mythological (Shield) — Legendary F75-99 ────────
    // Ancile — Roman sacred shield, block-biased.
    public static Armor CreateAncile() => Make("shd_ancile", "Ancile", 22000, "Legendary",
        240, 82, 40, 28, 50,
        B().Add(StatType.Defense, 40).Add(StatType.Vitality, 20));
}
