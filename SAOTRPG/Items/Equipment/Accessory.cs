namespace YourGame.Items.Equipment;

/// <summary>
/// Equipment that provides various bonuses.
/// </summary>
public class Accessory : Equipment
{
    public string? AccessorySlot { get; set; }
    public int MaxEquipped { get; set; } = 1;
}