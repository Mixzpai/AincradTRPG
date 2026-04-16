namespace SAOTRPG.Items;

// Stackable items like potions or crafting materials.
public abstract class StackableItem : BaseItem
{
    // Current number of items in this stack.
    public int Quantity { get; set; }
    // Maximum stack size. Stack() refuses to add beyond this cap.
    public int MaxStacks { get; set; }

    // Stacks another item of the same type onto this one.
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