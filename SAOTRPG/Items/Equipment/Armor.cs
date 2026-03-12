namespace YourGame.Items.Equipment;

/// <summary>
/// Equipment that provides defense.
/// </summary>
public class Armor : Equipment
{
    public int BaseDefense { get; set; }
    public string? ArmorSlot { get; set; }
    public int Weight { get; set; }
}