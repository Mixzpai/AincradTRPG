using Inventory.Core;
using Inventory.Logging;
using YourGame.Items;
using YourGame.Items.Consumables;
using EquipmentItem = YourGame.Items.Equipment.Equipment;

namespace SAOTRPG.UI;

public class TerminalGuiInventoryLogger : IInventoryLogger
{
    private readonly IGameLog _log;

    public TerminalGuiInventoryLogger(IGameLog log) => _log = log;

    public void LogItemAdded(BaseItem item)
        => _log.Log($"Added {item.Name} to inventory.");

    public void LogItemRemoved(BaseItem item)
        => _log.Log($"Removed {item.Name} from inventory.");

    public void LogItemEquipped(EquipmentItem equipment, EquipmentSlot slot)
        => _log.Log($"Equipped {equipment.Name} to {slot} slot.");

    public void LogItemUnequipped(EquipmentItem equipment, EquipmentSlot slot)
        => _log.Log($"Unequipped {equipment.Name} from {slot} slot.");

    public void LogItemUsed(Consumable consumable, string effectDescription)
        => _log.Log($"Used {consumable.Name}. {effectDescription}");

    public void LogError(string message)
        => _log.Log($"[Error] {message}");

    public void LogInfo(string message)
        => _log.Log(message);
}
