namespace SAOTRPG.Items.Equipment;

// Equipment that provides defense.
public class Armor : EquipmentBase
{
    // Raw defense value before stat scaling and bonuses.
    public int BaseDefense { get; set; }
    // Body slot this armor occupies (e.g. "Chest", "Legs", "Shield").
    public string? ArmorSlot { get; set; }
    // Armor weight — shadows BaseItem.Weight for armor-specific encumbrance.
    public new int Weight { get; set; }

    // Shield block chance (percent, 0-100). Only relevant for ArmorSlot="Shield".
    // When a block triggers, the attack is fully negated.
    public int BlockChance { get; set; }
}