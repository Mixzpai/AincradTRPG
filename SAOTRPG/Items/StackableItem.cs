namespace YourGame.Items;

/// <summary>
/// Stackable items like potions or crafting materials.
/// </summary>
public abstract class StackableItem : BaseItem
{
    public int Quantity { get; set; }
    public int MaxStacks { get; set; }

    /// <summary>
    /// Stacks another item of the same type onto this one.
    /// </summary>
    /// <returns>Remaining quantity that couldn't be stacked.</returns>
    public int Stack(StackableItem other)
    {
        if (other == null) return 0;

        int space = MaxStacks - Quantity;
        if (space <= 0) return other.Quantity;

        int toMove = Math.Min(space, other.Quantity);
        Quantity += toMove;
        other.Quantity -= toMove;
        return other.Quantity;
    }
}