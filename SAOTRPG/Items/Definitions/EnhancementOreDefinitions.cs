using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Definitions;

// IM Enhancement Ores: 1 ore per enhance level biases BonusPerLevel into a stat. Crimson Flame→ATK, Adamant→DEF(+Dur), Crust→VIT, Sharp Blade→DEX, Flowing Water→SPD, Wind Flower→AGI, Ash White→INT.
// All Uncommon Materials, stack 20, 80-120 Col. Drop from themed mobs (3-5%) and bosses (15-30%).
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

// Enhancement Ore material. Stackable; BiasStat read by CraftingDialog.Enhance. Separate from Ingot (Refinement) to avoid filter cross-contamination.
public class EnhancementOre : Material
{
    public StatType BiasStat { get; set; }

    // Adamant-only: +N ItemDurability/level (additive; refunded on demote).
    public int DurabilityBonus { get; set; }
}
