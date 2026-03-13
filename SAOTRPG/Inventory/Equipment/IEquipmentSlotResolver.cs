using Inventory.Core;
using EquipmentItem = YourGame.Items.Equipment.Equipment;

namespace Inventory.Equipment;

public interface IEquipmentSlotResolver
{
    EquipmentSlot? ResolveSlot(EquipmentItem equipment);
    void RegisterMapping(string equipmentType, EquipmentSlot slot);
}