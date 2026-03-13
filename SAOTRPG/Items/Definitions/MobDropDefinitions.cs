using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all mob drop materials.
/// </summary>
public static class MobDropDefinitions
{
    public static MobDrop CreateSlimeGel() => new()
    {
        Name = "Slime Gel",
        Value = 5,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        MaterialType = "Monster Material",
        CraftingTier = 1,
        SourceMonster = "Slime",
        DropRate = 0.8f,
        IsBossDrop = false
    };

    public static MobDrop CreateWolfPelt() => new()
    {
        Name = "Wolf Pelt",
        Value = 15,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        MaterialType = "Monster Material",
        CraftingTier = 2,
        SourceMonster = "Wolf",
        DropRate = 0.6f,
        IsBossDrop = false
    };

    public static MobDrop CreateDragonScale() => new()
    {
        Name = "Dragon Scale",
        Value = 1000,
        Rarity = "Legendary",
        Quantity = 1,
        MaxStacks = 10,
        MaterialType = "Monster Material",
        CraftingTier = 5,
        SourceMonster = "Elder Dragon",
        DropRate = 0.1f,
        IsBossDrop = true
    };
}