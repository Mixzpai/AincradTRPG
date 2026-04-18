using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

// IM Dynamic Shop Tiering (System 4). Clearing a floor boss at F50+ unlocks
// a new slice of late-game weapon stock across ALL shops for the rest of the
// run. Additive only — shops never lose stock if the player regresses.
//
// Tyler's Q4 adjustment: start at F50 (not canon F76). Fifty unlock tiers
// across F51..F100. Each tier adds 1–2 weapons — a mix of existing mid-to-
// late registered weapons and the 12 new IM shop weapons (clustered at
// their canon floor bands: Epic at F76/78/80/82/84, Legendary at F86/88/90/92/94).
//
// State flow:
//   • TurnManager.HandleMonsterKill (Boss branch, not FieldBoss) calls
//     RegisterFloorBossClear(floor) — updates HighestClearedFloor if higher.
//   • ShopDialog queries GetUnlockedStockForFloor(vendor, floor).
//   • SaveData.HighestFloorBossCleared persists across sessions.
//
// Legacy saves default HighestFloorBossCleared = 0 → no tiered stock at all,
// so previously-saved runs see unchanged shop inventories unless the player
// clears their next F50+ floor post-update.
public static class ShopTierSystem
{
    // Highest floor whose BOSS has been cleared. 0 = nothing cleared.
    // Persisted on SaveData.HighestFloorBossCleared.
    public static int HighestFloorBossCleared { get; set; }

    // Per-floor stock unlocks. Each floor key unlocks the listed DefIds
    // when HighestFloorBossCleared >= floor. All floors ≤ 50 unlock nothing
    // (baseline shop stock covers early game). Floors 51..99 each add 1–2
    // entries — IM shop weapons live at their canon band floors; the rest
    // are existing mid-to-late registered weapons, rotated to flesh out the
    // tier progression.
    //
    // IM shop weapon placement (F76..F94):
    //   F76 rap_edelweiss, F78 ths_fasislawine, F80 sci_poisoned_syringe,
    //   F82 spr_foa_stoss,  F84 dag_flyheight_fang,
    //   F86 rap_noctis_strasse, F88 ths_wice_ritter, F90 axe_schwarzs_blitz,
    //   F92 kat_muramasa,  F94 sci_silver_wing + spr_wave_schneider,
    //   F96 dag_rue_feuille.
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

    // Number of unlock tiers that can still fire (F51..F99 inclusive).
    public const int TotalTiers = 50;

    // Register a floor-boss clear. Tier unlocks only start at F50+.
    // If floor is already <= HighestFloorBossCleared we no-op (additive only).
    // Returns the number of NEWLY unlocked stock tiers so the caller can log.
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

    // Number of unlock tiers already reached (up to TotalTiers). Shown in
    // the ShopDialog header as "Tier N/50 unlocked" for progression feedback.
    public static int CurrentTierCount()
    {
        int count = 0;
        foreach (var kv in TierUnlocks)
            if (kv.Key <= HighestFloorBossCleared) count++;
        return count;
    }

    // Return all DefIds unlocked by the current tier state. Called by
    // ShopDialog to augment a vendor's base stock each visit.
    public static IEnumerable<string> GetUnlockedDefIds()
    {
        foreach (var kv in TierUnlocks)
            if (kv.Key <= HighestFloorBossCleared)
                foreach (var id in kv.Value)
                    yield return id;
    }

    // FB-072 — Exposes every registered tier (floor → DefIds) in ascending
    // floor order. VendorInvestmentSystem uses this to synthesize per-vendor
    // bonus stock from tiers that haven't been globally unlocked yet.
    public static IEnumerable<(int Floor, string[] DefIds)> EnumerateAllTiers()
    {
        foreach (var kv in TierUnlocks.OrderBy(x => x.Key))
            yield return (kv.Key, kv.Value);
    }

    // Build the tiered stock additions as live BaseItem instances with the
    // standard 20% vendor markup already applied. Skips DefIds that fail to
    // resolve (defensive — ItemRegistry should know them all).
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

    // Reset the tier state to a specific value (used by SaveManager on load).
    // Additive only during gameplay — SetForLoad is the sole backward path.
    public static void SetForLoad(int highest)
    {
        HighestFloorBossCleared = Math.Max(0, highest);
        _seenDefIds.Clear();
    }

    // DefIds the player has seen flagged at shop-open. Used so the first
    // shop visit after unlocking a tier shows a "NEW" badge next to the
    // newly-unlocked items.
    private static readonly HashSet<string> _seenDefIds = new();

    // True if this DefId is still flagged NEW (never shown to the player).
    public static bool IsNew(string defId) =>
        !string.IsNullOrEmpty(defId) && !_seenDefIds.Contains(defId);

    // Mark a DefId as seen (so the NEW badge clears on the next visit).
    public static void MarkSeen(string defId)
    {
        if (!string.IsNullOrEmpty(defId)) _seenDefIds.Add(defId);
    }
}
