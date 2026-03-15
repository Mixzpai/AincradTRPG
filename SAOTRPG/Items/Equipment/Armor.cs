namespace SAOTRPG.Items.Equipment;

/// <summary>
/// Equipment that provides defense.
/// </summary>
public class Armor : EquipmentBase
{
    public int BaseDefense { get; set; }
    public string? ArmorSlot { get; set; }
    public new int Weight { get; set; }

    /// <summary>
    /// Shield block chance (percent, 0-100). Only relevant for ArmorSlot="Shield".
    /// When a block triggers, the attack is fully negated.
    /// </summary>
    public int BlockChance { get; set; }
}