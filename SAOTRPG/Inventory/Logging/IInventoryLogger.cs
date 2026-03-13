using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Logging;

// Contract for inventory log output — swap implementations for Console vs TUI
// Implementations: ConsoleInventoryLogger (stdout), TerminalGuiInventoryLogger (game log panel)
public interface IInventoryLogger
{
    void LogItemAdded(BaseItem item);
    void LogItemRemoved(BaseItem item);
    void LogItemEquipped(EquipmentBase equipment, EquipmentSlot slot);
    void LogItemUnequipped(EquipmentBase equipment, EquipmentSlot slot);
    void LogItemUsed(Consumable consumable, string effectDescription);
    void LogError(string message);   // Validation failures, full inventory, etc.
    void LogInfo(string message);    // General inventory info
}
