using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Equipment;

// Resolves which EquipmentSlot a piece of gear belongs to based on its type/subtype
// Implementation: EquipmentSlotResolver — uses dictionary mappings + fallback logic
public interface IEquipmentSlotResolver
{
    EquipmentSlot? ResolveSlot(EquipmentBase equipment);          // Returns null if type is unknown
    void RegisterMapping(string equipmentType, EquipmentSlot slot); // Add custom type → slot mappings at runtime
}
