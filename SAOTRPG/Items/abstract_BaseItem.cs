namespace YourGame.Items;

// Base class for all items in the game.
public abstract class BaseItem
{
    public string? Name { get; set; }
    public int Value { get; set; }
    public string? Rarity { get; set; }
    public int ItemDurability { get; set; }
}