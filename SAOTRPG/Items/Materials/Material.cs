namespace SAOTRPG.Items.Materials;

// Base class for items used in crafting and upgrading.
public abstract class Material : StackableItem
{
    public string? MaterialType { get; set; }
    public int CraftingTier { get; set; }
}