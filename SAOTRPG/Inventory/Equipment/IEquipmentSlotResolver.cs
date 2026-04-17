using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Equipment;

// Resolves which EquipmentSlot a piece of gear belongs to based on its type/subtype
// Implementation: EquipmentSlotResolver — uses dictionary mappings + fallback logic
public interface IEquipmentSlotResolver
{
    EquipmentSlot? ResolveSlot(EquipmentBase equipment);          // Returns null if type is unknown
    void RegisterMapping(string equipmentType, EquipmentSlot slot); // Add custom type → slot mappings at runtime

    // True if the given equipment is legal in the OffHand slot. Shields and
    // bucklers always qualify. One-handed swords qualify when the player
    // has unlocked Dual Blades.
    bool CanGoInOffHand(EquipmentBase equipment);
}
