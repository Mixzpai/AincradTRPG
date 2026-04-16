namespace SAOTRPG.Items;

// Base class for all items in the game — equipment, consumables, food, and throwables.
// Holds common properties shared by every item: name, value, rarity, and durability.
public abstract class BaseItem
{
    // Registry key for definition-created items. Null for procedural/loot items.
    public string? DefinitionId { get; set; }
    // Display name shown in inventory and shop UI.
    public string? Name { get; set; }
    // Base Col value — used as sell/buy price in vendor shops.
    public int Value { get; set; }
    // Rarity tier string: "Common", "Uncommon", "Rare", "Epic", or "Legendary".
    public string? Rarity { get; set; }
    // Remaining durability points. Equipment breaks when this reaches 0.
    public int ItemDurability { get; set; }
    // Item weight in units. Used for encumbrance display. Default 1.
    public int Weight { get; set; } = 1;
}