namespace SAOTRPG.Items.Equipment;

// Equipment that provides various bonuses.
public class Accessory : EquipmentBase
{
    // Slot this accessory occupies (e.g. "Ring", "Necklace", "Earring").
    public string? AccessorySlot { get; set; }
    // Max number of this accessory type that can be equipped simultaneously. Default 1.
    public int MaxEquipped { get; set; } = 1;
}