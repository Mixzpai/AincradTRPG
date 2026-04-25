using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

// Bundle 10 — Tool-slot Pickaxe tiers. Three tiers cover early/mid/end:
// Wooden (TOB starter), Iron (F10+), Mithril (F50+ endgame, +10% ore quality).
public static class PickaxeDefinitions
{
    private static Pickaxe Make(
        string id, string name, int value, string rarity,
        int maxDurability, int miningPower, int oreQualityBonus,
        int requiredLevel)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = maxDurability, MaxDurability = maxDurability,
            MiningPower = miningPower, OreQualityBonus = oreQualityBonus,
            RequiredLevel = requiredLevel,
        };

    // Early-game pickaxe — sold by TOB merchants + low-floor vendors.
    // Brittle (30 dur), no power bonus, no quality bonus.
    public static Pickaxe CreateWoodenPickaxe() => Make(
        "wooden_pickaxe", "Wooden Pickaxe", 80, "Common",
        maxDurability: 30, miningPower: 0, oreQualityBonus: 0,
        requiredLevel: 1);

    // Mid-tier pickaxe — crafted/found F10+. Solid 80 dur and +1 strike-equivalent
    // (one-shots Iron veins, halves Mithril strike count).
    public static Pickaxe CreateIronPickaxe() => Make(
        "iron_pickaxe", "Iron Pickaxe", 320, "Uncommon",
        maxDurability: 80, miningPower: 1, oreQualityBonus: 0,
        requiredLevel: 5);

    // Endgame pickaxe — crafted/found F50+. 200 dur, +2 power (one-shots Mithril,
    // softens Divine), +10% to ore drop chance via OreQualityBonus.
    public static Pickaxe CreateMithrilPickaxe() => Make(
        "mithril_pickaxe", "Mithril Pickaxe", 1800, "Rare",
        maxDurability: 200, miningPower: 2, oreQualityBonus: 10,
        requiredLevel: 25);
}
