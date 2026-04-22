using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

// Dynamic Shop Tiering: F50+ boss clear unlocks stock (additive, never loses).
// Flow: HandleMonsterKill(Boss) → RegisterFloorBossClear → ShopDialog query.
public static class ShopTierSystem
{
    // Highest floor whose boss cleared. 0 = none. Persists via SaveData.
    public static int HighestFloorBossCleared { get; set; }

    // Floor → DefIds unlocked when HighestFloorBossCleared >= floor. F≤50 = baseline.
    // IM shop weapons inline-tagged below at their canon band floors.
    private static readonly Dictionary<int, string[]> TierUnlocks = new()
    {
        [51] = new[] { "mythril_katana" },
        [52] = new[] { "mythril_rapier" },
        [53] = new[] { "mythril_spear" },
        [54] = new[] { "mythril_bow" },
        [55] = new[] { "mythril_scimitar" },
        [56] = new[] { "mythril_dagger" },
        [57] = new[] { "mythril_axe" },
        [58] = new[] { "mythril_mace" },
        [59] = new[] { "mythril_greatsword" },
        [60] = new[] { "mythril_claws" },
        [61] = new[] { "mythril_scythe" },
        [62] = new[] { "adamantite_sword" },
        [63] = new[] { "adamantite_katana" },
        [64] = new[] { "adamantite_rapier" },
        [65] = new[] { "adamantite_spear" },
        [66] = new[] { "adamantite_bow" },
        [67] = new[] { "adamantite_scimitar" },
        [68] = new[] { "adamantite_dagger" },
        [69] = new[] { "adamantite_axe" },
        [70] = new[] { "adamantite_mace" },
        [71] = new[] { "adamantite_greatsword" },
        [72] = new[] { "adamantite_claws" },
        [73] = new[] { "adamantite_scythe" },
        [74] = new[] { "queens_knightsword" },
        [75] = new[] { "azure_sky_blade" },
        [76] = new[] { "rap_edelweiss" },              // IM Epic
        [77] = new[] { "sword_breaker" },
        [78] = new[] { "ths_fasislawine" },            // IM Epic
        [79] = new[] { "crimson_longsword" },
        [80] = new[] { "sci_poisoned_syringe" },       // IM Epic
        [81] = new[] { "asmodeus" },
        [82] = new[] { "spr_foa_stoss" },              // IM Epic
        [83] = new[] { "pale_edge" },
        [84] = new[] { "dag_flyheight_fang" },         // IM Epic
        [85] = new[] { "guilty_thorn" },
        [86] = new[] { "rap_noctis_strasse" },         // IM Legendary
        [87] = new[] { "karakurenai" },
        [88] = new[] { "ths_wice_ritter" },            // IM Legendary
        [89] = new[] { "celestial_blade" },
        [90] = new[] { "axe_schwarzs_blitz" },         // IM Legendary
        [91] = new[] { "celestial_katana" },
        [92] = new[] { "kat_muramasa" },               // IM Legendary
        [93] = new[] { "celestial_rapier" },
        [94] = new[] { "sci_silver_wing", "spr_wave_schneider" }, // IM Legendary
        [95] = new[] { "celestial_spear" },
        [96] = new[] { "dag_rue_feuille" },            // IM Legendary
        [97] = new[] { "celestial_greatsword" },
        [98] = new[] { "celestial_bow" },
        [99] = new[] { "anubis_spear" },
    };

    public const int TotalTiers = 50;

    // Register floor-boss clear (F50+ only). Returns count of newly unlocked tiers.
    public static int RegisterFloorBossClear(int floor, SAOTRPG.UI.IGameLog? log = null)
    {
        if (floor < 50) return 0;
        int before = HighestFloorBossCleared;
        if (floor <= before) return 0;
        HighestFloorBossCleared = floor;
        int newTiers = 0;
        for (int f = before + 1; f <= floor; f++)
            if (TierUnlocks.ContainsKey(f)) newTiers++;
        if (newTiers > 0 && log != null)
        {
            log.LogSystem($"  ◆ Shop tier unlocked! ({CurrentTierCount()}/{TotalTiers} tiers cleared — new stock available)");
        }
        return newTiers;
    }

    // Reached-tier count (up to TotalTiers). ShopDialog shows "Tier N/50".
    public static int CurrentTierCount()
    {
        int count = 0;
        foreach (var kv in TierUnlocks)
            if (kv.Key <= HighestFloorBossCleared) count++;
        return count;
    }

    // Unlocked DefIds at current tier. ShopDialog augments vendor base stock per visit.
    public static IEnumerable<string> GetUnlockedDefIds()
    {
        foreach (var kv in TierUnlocks)
            if (kv.Key <= HighestFloorBossCleared)
                foreach (var id in kv.Value)
                    yield return id;
    }

    // FB-072 — all tiers ascending. VendorInvestmentSystem synthesizes per-vendor
    // bonus stock from tiers not yet globally unlocked.
    public static IEnumerable<(int Floor, string[] DefIds)> EnumerateAllTiers()
    {
        foreach (var kv in TierUnlocks.OrderBy(x => x.Key))
            yield return (kv.Key, kv.Value);
    }

    // Tier additions as live BaseItems with 20% vendor markup. Skips unknown DefIds.
    public static List<BaseItem> BuildTierStock()
    {
        var list = new List<BaseItem>();
        foreach (var defId in GetUnlockedDefIds())
        {
            var item = ItemRegistry.Create(defId);
            if (item == null) continue;
            item.Value = (int)(item.Value * 1.2);
            list.Add(item);
        }
        return list;
    }

    // Reset tier state for SaveManager load — only backward path.
    public static void SetForLoad(int highest)
    {
        HighestFloorBossCleared = Math.Max(0, highest);
        _seenDefIds.Clear();
    }

    // Tracks seen DefIds for the "NEW" badge on first shop visit post-unlock.
    private static readonly HashSet<string> _seenDefIds = new();

    public static bool IsNew(string defId) =>
        !string.IsNullOrEmpty(defId) && !_seenDefIds.Contains(defId);

    public static void MarkSeen(string defId)
    {
        if (!string.IsNullOrEmpty(defId)) _seenDefIds.Add(defId);
    }
}
