using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

// Vein depletion drop tables.
// Drop chance = base + LifeSkill MiningOreDropBonusPercent + Pickaxe.OreQualityBonus.
// L25+ rolls one BONUS ore (20% chance). L50 grants guaranteed +1 mithril_trace on
// Mithril veins. L99 adds +20% to Divine vein primary drop.
public static class MiningOreTables
{
    // Per-vein primary-ingot drop chances (before bonuses). Iron generous; Divine rare.
    private const int IronPrimaryBaseChance    = 75;  // iron_ingot
    private const int MithrilPrimaryBaseChance = 60;  // mithril_ingot
    private const int DivinePrimaryBaseChance  = 30;  // divine_fragment

    // Secondary (flavor) drop chances — canon mats, low base.
    private const int IronOreBonusChance       = 35;  // iron_ore on top of ingot
    private const int MithrilTraceBonusChance  = 50;  // mithril_trace on top of ingot
    private const int PrimordialShardChance    = 4;   // ultra-rare on Divine deplete

    // The vein TYPE decides the metal; the BIOME decides which Enhancement Ore the surrounding
    // rock carries with it. Keyed by BiomeGenConfig.OreTableId, which every biome JSON declared
    // and nothing read. Each of the seven canon ores is claimed exactly once, matched to the
    // biome its bias suits: fire→Attack, water→Speed, frozen stone→Defense, baked earth→Vitality.
    // "default" is deliberately absent — the four biomes using it add no roll at all, so their
    // drops AND their rng draw count are exactly what they were.
    private static readonly Dictionary<string, string> OreTableToEnhancementOre =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["volcanic"] = "ore_crimson_flame",   // Attack
            ["aquatic"]  = "ore_flowing_water",   // Speed
            ["ice"]      = "ore_adamant",         // Defense + durability
            ["desert"]   = "ore_crust",           // Vitality
            ["swamp"]    = "ore_wind_flower",     // Agility
            ["dark"]     = "ore_ash_white",       // Intelligence
            ["void"]     = "ore_sharp_blade",     // Dexterity
        };

    // Richer veins carry more of it. Enhancement ores otherwise drop at 3-5% from themed mobs, so
    // mining is a deliberate, competitive route to them rather than a replacement.
    private const int BiomeOreIronChance    = 8;
    private const int BiomeOreMithrilChance = 14;
    private const int BiomeOreDivineChance  = 22;

    // An unknown ore table id is a config typo, not a silent "no ore" — logged ONCE per distinct
    // id, because this runs on every vein depletion and a per-strike log would drown the file.
    private static readonly HashSet<string> _unknownTablesLogged = new(StringComparer.OrdinalIgnoreCase);

    // Returns the drop list for a vein depletion. Caller (TurnManager.Mining)
    // dumps these onto the depleted tile via _map.AddItem.
    public static List<BaseItem> RollDepletionDrops(
        TileType veinType, Pickaxe? pick, Player player)
    {
        var drops = new List<BaseItem>();
        int dropBonus = (player.LifeSkills?.MiningOreDropBonusPercent() ?? 0)
                      + (pick?.OreQualityBonus ?? 0);

        switch (veinType)
        {
            case TileType.OreVeinIron:
                AddRoll(drops, "iron_ingot",   IronPrimaryBaseChance + dropBonus);
                AddRoll(drops, "iron_ore",     IronOreBonusChance + dropBonus);
                break;
            case TileType.OreVeinMithril:
                AddRoll(drops, "mithril_ingot", MithrilPrimaryBaseChance + dropBonus);
                AddRoll(drops, "mithril_trace", MithrilTraceBonusChance + dropBonus);
                // L50 milestone: guaranteed extra mithril_trace.
                if (player.LifeSkills?.MiningMithrilAdamantBonus == true)
                    AddItemById(drops, "mithril_trace");
                break;
            case TileType.OreVeinDivine:
                int divineChance = DivinePrimaryBaseChance + dropBonus
                    + (player.LifeSkills?.GetLevel(LifeSkillType.Mining) >= 99 ? 20 : 0);
                AddRoll(drops, "divine_fragment", divineChance);
                AddRoll(drops, "primordial_shard", PrimordialShardChance + dropBonus / 4);
                break;
        }

        // L25 bonus-ore roll (20%) — re-rolls primary on success.
        int bonusRoll = player.LifeSkills?.MiningBonusOreRollPercent ?? 0;
        if (bonusRoll > 0 && RunRng.Next(100) < bonusRoll)
        {
            string? extra = veinType switch
            {
                TileType.OreVeinIron    => "iron_ingot",
                TileType.OreVeinMithril => "mithril_ingot",
                TileType.OreVeinDivine  => "divine_fragment",
                _ => null,
            };
            if (extra != null) AddItemById(drops, extra);
        }

        // Appended LAST on purpose: every roll above keeps the exact draw order it had, so a
        // "default" biome is bit-identical to before this existed and the others differ only by a
        // single trailing draw.
        string? biomeOre = BiomeEnhancementOre();
        if (biomeOre != null)
        {
            int biomeChance = veinType switch
            {
                TileType.OreVeinIron    => BiomeOreIronChance,
                TileType.OreVeinMithril => BiomeOreMithrilChance,
                TileType.OreVeinDivine  => BiomeOreDivineChance,
                _                       => 0,
            };
            // Half weight on the mining/pickaxe bonus: it should reward investment without making
            // the biome ore more common than the ingot the vein is actually for.
            AddRoll(drops, biomeOre, biomeChance + dropBonus / 2);
        }
        return drops;
    }

    // The current floor's Enhancement Ore, or null when its biome declares the default table.
    public static string? BiomeEnhancementOre()
    {
        string? tableId = BiomeSystem.CurrentGenConfig?.OreTableId;
        if (string.IsNullOrWhiteSpace(tableId)) return null;
        if (OreTableToEnhancementOre.TryGetValue(tableId, out string? ore)) return ore;
        if (!tableId.Equals("default", StringComparison.OrdinalIgnoreCase)
            && _unknownTablesLogged.Add(tableId))
        {
            UI.DebugLogger.LogGame("MINING",
                $"biome={BiomeSystem.Current} declares oreTableId='{tableId}', which matches no ore table");
        }
        return null;
    }

    private static void AddRoll(List<BaseItem> drops, string defId, int chancePercent)
    {
        if (chancePercent <= 0) return;
        if (RunRng.Next(100) < Math.Min(100, chancePercent))
            AddItemById(drops, defId);
    }

    private static void AddItemById(List<BaseItem> drops, string defId)
    {
        var item = ItemRegistry.Create(defId);
        if (item != null) drops.Add(item);
    }

    // Mining XP per strike.
    public static int XpForStrike(TileType veinType) => veinType switch
    {
        TileType.OreVeinIron    => 4,
        TileType.OreVeinMithril => 9,
        TileType.OreVeinDivine  => 18,
        _ => 0,
    };
}
