using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Per-vendor stock-tier investment (layers on ShopTierSystem). Cumulative
// thresholds 1k=+1, 5k=+2, 20k=+3 (cap). Keyed by ShopName (distinct per floor).
public static class VendorInvestmentSystem
{
    public const int MaxInvestmentPerVendor = 20000;

    // (MinDeposit, BonusTiers) — walked highest-first; first hit wins.
    private static readonly (int MinDeposit, int BonusTiers)[] Thresholds =
    {
        (20000, 3),
        ( 5000, 2),
        ( 1000, 1),
    };

    // Cumulative deposits by vendor key. Roundtripped via SaveData.
    public static Dictionary<string, int> Investments { get; } = new();

    // Key = ShopName (fallback "{Name}#{Id}"). Vendor has no stable Id yet.
    public static string KeyFor(Vendor vendor) =>
        !string.IsNullOrEmpty(vendor.ShopName) ? vendor.ShopName
            : $"{vendor.Name ?? "vendor"}#{vendor.Id}";

    public static int GetInvested(string vendorKey) =>
        Investments.TryGetValue(vendorKey, out var total) ? total : 0;

    public static int GetInvested(Vendor vendor) => GetInvested(KeyFor(vendor));

    // Bonus tiers (0..3) unlocked via investment. Stacks with ShopTier global.
    public static int GetInvestedTiers(string vendorKey)
    {
        int total = GetInvested(vendorKey);
        foreach (var (min, tiers) in Thresholds)
            if (total >= min) return tiers;
        return 0;
    }

    public static int GetInvestedTiers(Vendor vendor) => GetInvestedTiers(KeyFor(vendor));

    // Col needed to reach next tier; 0 at MaxInvestmentPerVendor.
    public static int NextTierCost(string vendorKey)
    {
        int total = GetInvested(vendorKey);
        if (total >= 20000) return 0;
        if (total >= 5000) return 20000 - total;
        if (total >= 1000) return 5000 - total;
        return 1000 - total;
    }

    public static int NextTierCost(Vendor vendor) => NextTierCost(KeyFor(vendor));

    // Invest `amount` Col; clamps to MaxInvestmentPerVendor + player balance.
    // Returns actual transferred (0 if cap or broke). Logs one line if log != null.
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

    // Extra stock from investment: next N tiers beyond global unlock.
    // If all tiers already globally unlocked, no visible extra stock (UI documents).
    public static List<BaseItem> BuildVendorExtraStock(Vendor vendor)
    {
        int bonusTiers = GetInvestedTiers(vendor);
        if (bonusTiers <= 0) return new List<BaseItem>();

        var list = new List<BaseItem>();
        int granted = 0;
        // Walk tiers ascending, skip globally-unlocked, take next `bonusTiers`.
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

    // Replace state from saved dict (null = clear). Called by SaveManager.
    public static void SetForLoad(Dictionary<string, int>? saved)
    {
        Investments.Clear();
        if (saved == null) return;
        foreach (var kvp in saved)
            if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value > 0)
                Investments[kvp.Key] = Math.Clamp(kvp.Value, 0, MaxInvestmentPerVendor);
    }

    // Shallow copy for save-writer ownership.
    public static Dictionary<string, int> Snapshot() => new(Investments);

    public static void Clear() => Investments.Clear();
}
