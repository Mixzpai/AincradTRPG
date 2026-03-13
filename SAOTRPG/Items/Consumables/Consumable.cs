using SAOTRPG.Entities;

namespace SAOTRPG.Items.Consumables;

/// <summary>
/// Base class for consumable items that apply effects when used.
/// </summary>
public abstract class Consumable : StackableItem
{
    public string? ConsumableType { get; set; }
    public string? EffectDescription { get; set; }
    public StatModifierCollection Effects { get; set; } = new();

    public virtual void Use(IStatModifiable target)
    {
        if (Quantity <= 0) return;

        Effects.ApplyTo(target);
        Quantity--;
    }
}