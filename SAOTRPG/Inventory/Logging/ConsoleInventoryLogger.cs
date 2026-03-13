using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Logging;

// Fallback logger that writes to Console.WriteLine — used outside Terminal.Gui context
public class ConsoleInventoryLogger : IInventoryLogger
{
    public void LogItemAdded(BaseItem item) => Console.WriteLine($"Added {item.Name} to inventory.");
    public void LogItemRemoved(BaseItem item) => Console.WriteLine($"Removed {item.Name} from inventory.");
    public void LogItemEquipped(EquipmentBase equipment, EquipmentSlot slot) => Console.WriteLine($"Equipped {equipment.Name} to {slot} slot.");
    public void LogItemUnequipped(EquipmentBase equipment, EquipmentSlot slot) => Console.WriteLine($"Unequipped {equipment.Name} from {slot} slot.");
    public void LogItemUsed(Consumable consumable, string effectDescription) => Console.WriteLine($"Used {consumable.Name}. {effectDescription}");
    public void LogError(string message) => Console.WriteLine($"[Error] {message}");
    public void LogInfo(string message) => Console.WriteLine(message);
}
