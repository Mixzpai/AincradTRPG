using YourGame.Items;

namespace YourGame.Items.Consumables;

/// <summary>
/// Base class for consumable items that apply effects when used.
/// </summary>
public abstract class Consumable : StackableItem
{
    public string? ConsumableType { get; set; }
    public string? EffectDescription { get; set; }
    public StatModifierCollection Effects { get; set; } = new();

    public virtual void Use(Player player)
    {
        if (Quantity <= 0) return;

        Effects.ApplyTo(player);
        Quantity--;
    }
}