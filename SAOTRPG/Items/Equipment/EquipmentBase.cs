using SAOTRPG.Entities;

namespace SAOTRPG.Items.Equipment;

/// <summary>
/// Base class for equipment items that provide stat bonuses when equipped.
/// </summary>
public abstract class EquipmentBase : BaseItem
{
    public int RequiredLevel { get; set; }
    public string? EquipmentType { get; set; }
    public StatModifierCollection Bonuses { get; set; } = new();

    public virtual void Equip(IStatModifiable target)
    {
        Bonuses.ApplyTo(target);
    }

    public virtual void Unequip(IStatModifiable target)
    {
        Bonuses.RemoveFrom(target);
    }
}