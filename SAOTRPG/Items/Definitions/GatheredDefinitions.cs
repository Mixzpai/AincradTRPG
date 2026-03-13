using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all gathered materials.
/// </summary>
public static class GatheredDefinitions
{
    public static Gathered CreateIronOre() => new()
    {
        Name = "Iron Ore",
        Value = 15,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        MaterialType = "Ore",
        CraftingTier = 1,
        GatheringType = "Mining",
        RequiredGatheringLevel = 1,
        GatheringLocation = "Mines"
    };

    public static Gathered CreateGoldOre() => new()
    {
        Name = "Gold Ore",
        Value = 50,
        Rarity = "Uncommon",
        Quantity = 1,
        MaxStacks = 99,
        MaterialType = "Ore",
        CraftingTier = 3,
        GatheringType = "Mining",
        RequiredGatheringLevel = 15,
        GatheringLocation = "Deep Mines"
    };

    public static Gathered CreateHealingHerb() => new()
    {
        Name = "Healing Herb",
        Value = 8,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        MaterialType = "Herb",
        CraftingTier = 1,
        GatheringType = "Herbalism",
        RequiredGatheringLevel = 1,
        GatheringLocation = "Forest"
    };

    public static Gathered CreateRiverFish() => new()
    {
        Name = "River Fish",
        Value = 12,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        MaterialType = "Fish",
        CraftingTier = 1,
        GatheringType = "Fishing",
        RequiredGatheringLevel = 1,
        GatheringLocation = "River"
    };
}