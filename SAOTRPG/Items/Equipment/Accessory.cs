namespace SAOTRPG.Items.Equipment;

/// <summary>
/// Equipment that provides various bonuses.
/// </summary>
public class Accessory : EquipmentBase
{
    public string? AccessorySlot { get; set; }
    public int MaxEquipped { get; set; } = 1;
}