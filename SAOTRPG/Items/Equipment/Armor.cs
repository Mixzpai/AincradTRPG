namespace SAOTRPG.Items.Equipment;

/// <summary>
/// Equipment that provides defense.
/// </summary>
public class Armor : EquipmentBase
{
    public int BaseDefense { get; set; }
    public string? ArmorSlot { get; set; }
    public int Weight { get; set; }
}