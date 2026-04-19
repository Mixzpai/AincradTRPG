using SAOTRPG.Entities;

namespace SAOTRPG.Items.Equipment;

// Base class for equipment items that provide stat bonuses when equipped.
public abstract class EquipmentBase : BaseItem
{
    // Minimum player level required to equip this item.
    public int RequiredLevel { get; set; }
    // Slot category: "Weapon", "Armor", or "Accessory".
    public string? EquipmentType { get; set; }
    // Stat modifiers applied while this equipment is worn.
    public StatModifierCollection Bonuses { get; set; } = new();

    // Enhancement level (+0 to +10). Each level adds flat bonus stats.
    // Enhancement is done at the Anvil using materials + Col.
    public int EnhancementLevel { get; set; }

    // Display name with enhancement suffix: "Iron Sword +3"
    public string EnhancedName => EnhancementLevel > 0 ? $"{Name} +{EnhancementLevel}" : Name ?? "";

    // ── IF Refinement ────────────────────────────────────────────────────
    // Three refinement slots. Each entry is either null (empty) or the
    // DefinitionId of a socketed Ingot. Use Systems.Refinement.Socket() to
    // mutate — do NOT write directly, because the helper also folds the
    // ingot's stat bonuses into Bonuses and destroys the prior ingot on
    // override. Divine-rarity equipment cannot be refined.
    public const int RefinementSlotCount = 3;
    public string?[] RefinementSlots { get; set; } = new string?[RefinementSlotCount];

    // Convenience: any non-null slot → refined.
    public bool HasAnyRefinement =>
        RefinementSlots != null && RefinementSlots.Any(s => !string.IsNullOrEmpty(s));

    // Apply stat bonuses to the target entity when equipped.
    public virtual void Equip(IStatModifiable target)
    {
        Bonuses.ApplyTo(target);
    }

    // Remove stat bonuses from the target entity when unequipped.
    public virtual void Unequip(IStatModifiable target)
    {
        Bonuses.RemoveFrom(target);
    }
}