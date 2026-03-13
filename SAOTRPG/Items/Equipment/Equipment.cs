using SAOTRPG.Entities;

namespace SAOTRPG.Items.Equipment;

/// <summary>
/// Base class for equipment items that provide stat bonuses when equipped.
/// </summary>
public abstract class Equipment : BaseItem
{
    public int RequiredLevel { get; set; }
    public string? EquipmentType { get; set; }
    public StatModifierCollection Bonuses { get; set; } = new();

    public virtual void Equip(Player player)
    {
        Bonuses.ApplyTo(player);
    }

    public virtual void Unequip(Player player)
    {
        Bonuses.RemoveFrom(player);
    }
}