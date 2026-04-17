using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Definitions;

// IM canon Enhancement Ores (System 3). Seven themed ores — each enhance
// level consumes exactly one ore, and the ore chosen biases that level's
// BonusPerLevel into a specific stat instead of the legacy flat +Attack.
//
// Ore → stat map (Tyler-approved):
//   Crimson Flame  → Attack
//   Adamant        → Defense (+ small Durability bump on socket)
//   Crust          → Vitality (feeds MaxHP indirectly)
//   Sharp Blade    → Dexterity (crit feel)
//   Flowing Water  → Speed
//   Wind Flower    → Agility
//   Ash White      → Intelligence (feeds SkillDamage indirectly)
//
// All ores are Uncommon Materials, stack to 20, price ~80–120 Col. They
// drop from themed mobs via LootGenerator.OreByLootTag (3–5%) and from
// field/floor bosses at 15–30% via the boss roll in TurnManager.Combat.
public static class EnhancementOreDefinitions
{
    private static EnhancementOre Make(string id, string name, int value, StatType stat)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = "Uncommon",
            Quantity = 1, MaxStacks = 20,
            MaterialType = "Enhancement Ore", CraftingTier = 2,
            BiasStat = stat,
        };

    public static EnhancementOre CreateCrimsonFlame() =>
        Make("ore_crimson_flame", "Crimson Flame Ore", 120, StatType.Attack);

    public static EnhancementOre CreateAdamant()
    {
        var o = Make("ore_adamant", "Adamant Ore", 110, StatType.Defense);
        o.DurabilityBonus = 10;  // canon: Adamant reinforces the weapon
        return o;
    }

    public static EnhancementOre CreateCrust() =>
        Make("ore_crust", "Crust Ore", 95, StatType.Vitality);

    public static EnhancementOre CreateSharpBlade() =>
        Make("ore_sharp_blade", "Sharp Blade Ore", 100, StatType.Dexterity);

    public static EnhancementOre CreateFlowingWater() =>
        Make("ore_flowing_water", "Flowing Water Ore", 90, StatType.Speed);

    public static EnhancementOre CreateWindFlower() =>
        Make("ore_wind_flower", "Wind Flower Ore", 85, StatType.Agility);

    public static EnhancementOre CreateAshWhite() =>
        Make("ore_ash_white", "Ash White Ore", 115, StatType.Intelligence);

    // Convenience map: stat → ore DefId. Used by CraftingDialog + migration.
    public static readonly Dictionary<StatType, string> StatToOreDefId = new()
    {
        [StatType.Attack]       = "ore_crimson_flame",
        [StatType.Defense]      = "ore_adamant",
        [StatType.Vitality]     = "ore_crust",
        [StatType.Dexterity]    = "ore_sharp_blade",
        [StatType.Speed]        = "ore_flowing_water",
        [StatType.Agility]      = "ore_wind_flower",
        [StatType.Intelligence] = "ore_ash_white",
    };

    // Inverse map: ore DefId → bias stat. Used to apply the per-level bonus.
    public static readonly Dictionary<string, StatType> OreDefIdToStat = new()
    {
        ["ore_crimson_flame"] = StatType.Attack,
        ["ore_adamant"]       = StatType.Defense,
        ["ore_crust"]         = StatType.Vitality,
        ["ore_sharp_blade"]   = StatType.Dexterity,
        ["ore_flowing_water"] = StatType.Speed,
        ["ore_wind_flower"]   = StatType.Agility,
        ["ore_ash_white"]     = StatType.Intelligence,
    };

    // Every registered ore DefId (for iteration / boss drop rolls).
    public static readonly string[] AllOreDefIds =
    {
        "ore_crimson_flame", "ore_adamant",       "ore_crust",
        "ore_sharp_blade",   "ore_flowing_water", "ore_wind_flower",
        "ore_ash_white",
    };
}

// Enhancement Ore material subclass. Stackable, routed via ItemRegistry; its
// BiasStat is read by CraftingDialog.Enhance to decide which stat gets the
// level's BonusPerLevel. Kept separate from Ingot (Refinement system) so
// the two systems don't cross-contaminate inventory filters.
public class EnhancementOre : Material
{
    // Which stat this ore biases the enhancement into on use.
    public StatType BiasStat { get; set; }

    // Adamant flavour: +N ItemDurability on socket. Default 0 for every
    // other ore. Applied additively each enhance level by CraftingDialog
    // and refunded on demote. Tyler Q3=a (2026-04-17).
    public int DurabilityBonus { get; set; }
}
