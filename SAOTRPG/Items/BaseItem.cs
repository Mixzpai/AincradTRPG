namespace SAOTRPG.Items;

// Base class for all items in the game.
public abstract class BaseItem
{
    /// <summary>Registry key for definition-created items. Null for procedural/loot items.</summary>
    public string? DefinitionId { get; set; }
    public string? Name { get; set; }
    public int Value { get; set; }
    public string? Rarity { get; set; }
    public int ItemDurability { get; set; }
    /// <summary>Item weight in units. Used for encumbrance display. Default 1.</summary>
    public int Weight { get; set; } = 1;
}