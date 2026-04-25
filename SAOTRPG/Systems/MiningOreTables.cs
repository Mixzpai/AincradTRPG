using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

// Bundle 10 — vein depletion drop tables. Per Q15 trim: iron_ingot is the only
// NEW material; mithril_ingot / divine_fragment / primordial_shard are reused
// from Bundle 9, iron_ore / mithril_trace are reused from existing canon mats.
//
// Drop chance = base + LifeSkill MiningOreDropBonusPercent + Pickaxe.OreQualityBonus.
// L25+ rolls one BONUS ore (20% chance) on top of base drops. L50 grants a
// guaranteed +1 mithril_trace bonus on Mithril veins. L99 gives a flat +20%
// to Divine vein primary drop.
public static class MiningOreTables
{
    // Per-vein primary-ingot drop chances (before bonuses). Iron is generous —
    // the vein WILL drop ore most strikes. Divine is gated and rare.
    private const int IronPrimaryBaseChance    = 75;  // iron_ingot
    private const int MithrilPrimaryBaseChance = 60;  // mithril_ingot
    private const int DivinePrimaryBaseChance  = 30;  // divine_fragment

    // Secondary (flavor) drop chances — existing canon mats, low base.
    private const int IronOreBonusChance       = 35;  // iron_ore on top of ingot
    private const int MithrilTraceBonusChance  = 50;  // mithril_trace on top of ingot
    private const int PrimordialShardChance    = 4;   // ultra-rare on Divine deplete

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
                // L50 milestone: guaranteed extra mithril_trace per scout 2.2.
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

        // L25 bonus-ore roll (20% per scout) — re-rolls primary on success.
        int bonusRoll = player.LifeSkills?.MiningBonusOreRollPercent ?? 0;
        if (bonusRoll > 0 && Random.Shared.Next(100) < bonusRoll)
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
        return drops;
    }

    private static void AddRoll(List<BaseItem> drops, string defId, int chancePercent)
    {
        if (chancePercent <= 0) return;
        if (Random.Shared.Next(100) < Math.Min(100, chancePercent))
            AddItemById(drops, defId);
    }

    private static void AddItemById(List<BaseItem> drops, string defId)
    {
        var item = ItemRegistry.Create(defId);
        if (item != null) drops.Add(item);
    }

    // Mining XP per strike (locked Q10 per scout 2.2).
    public static int XpForStrike(TileType veinType) => veinType switch
    {
        TileType.OreVeinIron    => 4,
        TileType.OreVeinMithril => 9,
        TileType.OreVeinDivine  => 18,
        _ => 0,
    };
}
