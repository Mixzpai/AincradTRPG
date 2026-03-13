using Inventory.Core;
using YourGame.Items;
using YourGame.Items.Consumables;
using EquipmentItem = YourGame.Items.Equipment.Equipment;

namespace Inventory.Logging;

public interface IInventoryLogger
{
    void LogItemAdded(BaseItem item);
    void LogItemRemoved(BaseItem item);
    void LogItemEquipped(EquipmentItem equipment, EquipmentSlot slot);
    void LogItemUnequipped(EquipmentItem equipment, EquipmentSlot slot);
    void LogItemUsed(Consumable consumable, string effectDescription);
    void LogError(string message);
    void LogInfo(string message);
}