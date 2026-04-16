using SAOTRPG.Entities;

namespace SAOTRPG.Items.Consumables;

// Base class for consumable items that apply effects when used.
public abstract class Consumable : StackableItem
{
    // Category label (e.g. "Potion", "Antidote") shown in inventory UI.
    public string? ConsumableType { get; set; }
    // Human-readable description of the effect (e.g. "Restores 50 HP").
    public string? EffectDescription { get; set; }
    // Stat modifiers applied to the user when consumed.
    public StatModifierCollection Effects { get; set; } = new();

    // Consume one charge, applying stat effects to the target. No-op if quantity is 0.
    public virtual void Use(IStatModifiable target)
    {
        if (Quantity <= 0) return;

        Effects.ApplyTo(target);
        Quantity--;
    }
}