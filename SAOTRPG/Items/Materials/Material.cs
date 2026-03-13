namespace SAOTRPG.Items.Materials;

/// <summary>
/// Base class for items used in crafting and upgrading.
/// </summary>
public abstract class Material : StackableItem
{
    public string? MaterialType { get; set; }
    public int CraftingTier { get; set; }
}