using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// FB-072 Part B — per-vendor stock-tier investment.
//
// Players can deposit Col into a specific vendor to boost ONLY that vendor's
// available stock tier. This layers on top of the global ShopTierSystem (which
// unlocks tiers run-wide when floor bosses are cleared at F50+). Each vendor
// tracks its own cumulative deposit total; tier bonuses are derived from
// thresholds and capped to prevent runaway scaling.
//
// Thresholds (cumulative):
//   •    1,000 Col → +1 tier
//   •    5,000 Col → +2 tiers
//   •   20,000 Col → +3 tiers (CAP)
//
// Keyed by vendor identity — defaults to ShopName when the vendor has no
// persistent Id. Fine for this pass since each floor's vendors have distinct
// ShopName strings. Re-keying to a stable GUID would be the migration path
// if two vendors ever share a ShopName.
//
// Legacy saves: VendorInvestments defaults to an empty dict → zero invested
// tiers everywhere → no behavior change until the player opens the shop
// dialog and uses the Invest button.
public static class VendorInvestmentSystem
{
    // Maximum cumulative deposit per vendor. Beyond this, Invest() clamps.
    public const int MaxInvestmentPerVendor = 20000;

    // Tier thresholds in ascending order: (MinDeposit, BonusTiers).
    // The lookup walks this from highest to lowest and returns the first hit.
    private static readonly (int MinDeposit, int BonusTiers)[] Thresholds =
    {
        (20000, 3),
        ( 5000, 2),
        ( 1000, 1),
    };

    // Cumulative deposits keyed by vendor id (or ShopName fallback).
    // SaveData roundtrips this dict so mid-run investment survives a reload.
    public static Dictionary<string, int> Investments { get; } = new();

    // Resolve the key used to look up / write a vendor's investment total.
    // Vendor currently has no dedicated Id — ShopName is guaranteed non-null
    // and distinct across the run's vendor roster in practice.
    public static string KeyFor(Vendor vendor) =>
        !string.IsNullOrEmpty(vendor.ShopName) ? vendor.ShopName
            : $"{vendor.Name ?? "vendor"}#{vendor.Id}";

    // Total Col currently deposited at this vendor (0 if never invested).
    public static int GetInvested(string vendorKey) =>
        Investments.TryGetValue(vendorKey, out var total) ? total : 0;

    public static int GetInvested(Vendor vendor) => GetInvested(KeyFor(vendor));

    // Number of BONUS stock tiers this vendor has unlocked via investment.
    // 0..3. Stacks with ShopTierSystem.CurrentTierCount() — those are global,
    // this one is scoped to the vendor being queried.
    public static int GetInvestedTiers(string vendorKey)
    {
        int total = GetInvested(vendorKey);
        foreach (var (min, tiers) in Thresholds)
            if (total >= min) return tiers;
        return 0;
    }

    public static int GetInvestedTiers(Vendor vendor) => GetInvestedTiers(KeyFor(vendor));

    // Col required to reach the NEXT tier from the current deposit total.
    // Returns 0 once the player is at MaxInvestmentPerVendor (no next tier).
    public static int NextTierCost(string vendorKey)
    {
        int total = GetInvested(vendorKey);
        if (total >= 20000) return 0;
        if (total >= 5000) return 20000 - total;
        if (total >= 1000) return 5000 - total;
        return 1000 - total;
    }

    public static int NextTierCost(Vendor vendor) => NextTierCost(KeyFor(vendor));

    // Attempt to invest `amount` Col into a vendor. The deposit is clamped
    // to not exceed MaxInvestmentPerVendor cumulatively. Returns the actual
    // Col transferred (0 if the player couldn't afford anything or the
    // vendor is already at cap). Logs a one-line result when log != null.
    public static int Invest(Vendor vendor, Player player, int amount, IGameLog? log = null)
    {
        if (amount <= 0) return 0;
        string key = KeyFor(vendor);
        int current = GetInvested(key);
        int capRemaining = MaxInvestmentPerVendor - current;
        if (capRemaining <= 0)
        {
            log?.Log($"{vendor.ShopName ?? vendor.Name ?? "Shop"} is already fully invested.");
            return 0;
        }

        int actual = Math.Min(amount, Math.Min(capRemaining, player.ColOnHand));
        if (actual <= 0)
        {
            log?.Log("Not enough Col to invest.");
            return 0;
        }

        int tiersBefore = GetInvestedTiers(key);
        player.ColOnHand -= actual;
        Investments[key] = current + actual;
        int tiersAfter = GetInvestedTiers(key);

        if (log != null)
        {
            string shop = vendor.ShopName ?? vendor.Name ?? "Shop";
            log.LogSystem($"  ◆ Invested {actual} Col at {shop} (total: {Investments[key]}/{MaxInvestmentPerVendor})");
            if (tiersAfter > tiersBefore)
                log.LogSystem($"  ◆ {shop} stock tier +{tiersAfter - tiersBefore}! (now +{tiersAfter} bonus tier{(tiersAfter == 1 ? "" : "s")})");
        }
        return actual;
    }

    // Build extra stock entries unlocked by this vendor's investment tier.
    // Walks ShopTierSystem's tier list beyond the run's current global tier
    // and pulls the NEXT N tiers (where N = investment bonus) into this
    // vendor's stock. If no extra global tiers remain (all unlocked), the
    // investment provides no visible stock — documented in the UI label.
    public static List<BaseItem> BuildVendorExtraStock(Vendor vendor)
    {
        int bonusTiers = GetInvestedTiers(vendor);
        if (bonusTiers <= 0) return new List<BaseItem>();

        var list = new List<BaseItem>();
        int granted = 0;
        // Walk the registered tier floors in ascending order, skipping ones
        // already globally unlocked. Take the next `bonusTiers` unlock lists.
        foreach (var (floor, defIds) in ShopTierSystem.EnumerateAllTiers())
        {
            if (floor <= ShopTierSystem.HighestFloorBossCleared) continue;
            foreach (var defId in defIds)
            {
                var item = ItemRegistry.Create(defId);
                if (item == null) continue;
                item.Value = (int)(item.Value * 1.2);
                list.Add(item);
            }
            granted++;
            if (granted >= bonusTiers) break;
        }
        return list;
    }

    // Reset from a loaded save. Replaces in-memory state with the persisted
    // dict (or empties if null). Called from SaveManager on load.
    public static void SetForLoad(Dictionary<string, int>? saved)
    {
        Investments.Clear();
        if (saved == null) return;
        foreach (var kvp in saved)
            if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value > 0)
                Investments[kvp.Key] = Math.Clamp(kvp.Value, 0, MaxInvestmentPerVendor);
    }

    // Snapshot for saving. Returns a shallow copy so the save-writer owns
    // the dict it hands to the JSON serializer.
    public static Dictionary<string, int> Snapshot() => new(Investments);

    // Reset between runs (new game). Mirrors ShopTierSystem.SetForLoad(0).
    public static void Clear() => Investments.Clear();
}
