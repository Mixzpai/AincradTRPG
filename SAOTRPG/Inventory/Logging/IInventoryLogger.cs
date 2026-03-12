namespace Inventory.Logging;

public interface IInventoryLogger
{
    void LogItemAdded(BaseItem item);
    void LogItemRemoved(BaseItem item);
    void LogItemEquipped(Equipment equipment, EquipmentSlot slot);
    void LogItemUnequipped(Equipment equipment, EquipmentSlot slot);
    void LogItemUsed(Consumable consumable, string effectDescription);
    void LogError(string message);
    void LogInfo(string message);
}