using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Definitions;

// Chain-specific weapon evolution materials — one unique per chain; T1→T4 step qtys in crafting recipes.
public static class EvolutionMaterialDefinitions
{
    // Tyrfing chain (One-Handed Sword).
    public static MobDrop CreateDemonicSigil() => new()
    {
        DefinitionId = "demonic_sigil",
        Name = "Demonic Sigil",
        Value = 800,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Hexagramme chain (Rapier).
    public static MobDrop CreateGeometricShard() => new()
    {
        DefinitionId = "geometric_shard",
        Name = "Geometric Shard",
        Value = 750,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Satanachia chain (Scimitar).
    public static MobDrop CreateInfernalGem() => new()
    {
        DefinitionId = "infernal_gem",
        Name = "Infernal Gem",
        Value = 850,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // The Iron Maiden chain (Dagger).
    public static MobDrop CreateValkyrieFeather() => new()
    {
        DefinitionId = "valkyrie_feather",
        Name = "Valkyrie Feather",
        Value = 750,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Mjolnir chain (Mace).
    public static MobDrop CreateLunarCore() => new()
    {
        DefinitionId = "lunar_core",
        Name = "Lunar Core",
        Value = 800,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Masamune chain (Katana).
    public static MobDrop CreateOniAsh() => new()
    {
        DefinitionId = "oni_ash",
        Name = "Oni Ash",
        Value = 800,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Ascalon chain (Two-Handed Sword).
    public static MobDrop CreateTitanFragment() => new()
    {
        DefinitionId = "titan_fragment",
        Name = "Titan Fragment",
        Value = 900,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Ouroboros chain (Axe).
    public static MobDrop CreateNidhoggScale() => new()
    {
        DefinitionId = "nidhogg_scale",
        Name = "Nidhogg Scale",
        Value = 850,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };

    // Caladbolg chain (Spear).
    public static MobDrop CreateTrishulaTip() => new()
    {
        DefinitionId = "trishula_tip",
        Name = "Trishula Tip",
        Value = 800,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        MaterialType = "Evolution Catalyst",
        CraftingTier = 4,
        SourceMonster = "various",
        DropRate = 0.1f,
        IsBossDrop = true,
    };
}
