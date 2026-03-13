using Inventory.Core;
using YourGame.Items;
using YourGame.Items.Consumables;
using EquipmentItem = YourGame.Items.Equipment.Equipment;

namespace Inventory.Logging;

public class ConsoleInventoryLogger : IInventoryLogger
{
    public void LogItemAdded(BaseItem item)
        => Console.WriteLine($"Added {item.Name} to inventory.");

    public void LogItemRemoved(BaseItem item)
        => Console.WriteLine($"Removed {item.Name} from inventory.");

    public void LogItemEquipped(EquipmentItem equipment, EquipmentSlot slot)
        => Console.WriteLine($"Equipped {equipment.Name} to {slot} slot.");

    public void LogItemUnequipped(EquipmentItem equipment, EquipmentSlot slot)
        => Console.WriteLine($"Unequipped {equipment.Name} from {slot} slot.");

    public void LogItemUsed(Consumable consumable, string effectDescription)
        => Console.WriteLine($"Used {consumable.Name}. {effectDescription}");

    public void LogError(string message)
        => Console.WriteLine($"[Error] {message}");

    public void LogInfo(string message)
        => Console.WriteLine(message);
}