using Inventory.Core;

namespace Inventory.Equipment;

public interface IEquipmentSlotResolver
{
    EquipmentSlot? ResolveSlot(global::Equipment equipment);
    void RegisterMapping(string equipmentType, EquipmentSlot slot);
}