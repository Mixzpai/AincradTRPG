using SAOTRPG.Inventory.Core;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI;

// Bridges inventory logging into the Terminal.Gui game log panel
public class TerminalGuiInventoryLogger : IInventoryLogger
{
    private readonly IGameLog _log;
    public TerminalGuiInventoryLogger(IGameLog log) => _log = log;

    public void LogItemAdded(BaseItem item) => _log.LogSystem($"Added {item.Name} to inventory.");
    public void LogItemRemoved(BaseItem item) => _log.LogSystem($"Removed {item.Name} from inventory.");
    public void LogItemEquipped(EquipmentBase equipment, EquipmentSlot slot) => _log.LogSystem($"Equipped {equipment.Name} {Theme.ArrowRight} {slot} slot.");
    public void LogItemUnequipped(EquipmentBase equipment, EquipmentSlot slot) => _log.LogSystem($"Unequipped {equipment.Name} {Theme.ArrowLeft} {slot} slot.");
    public void LogItemUsed(Consumable consumable, string effectDescription) => _log.LogSystem($"Used {consumable.Name}. {effectDescription}");
    public void LogError(string message) => _log.Log($" {Theme.Cross} {message}");
    public void LogInfo(string message) => _log.Log(message);
}
