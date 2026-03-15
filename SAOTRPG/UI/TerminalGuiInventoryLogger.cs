using SAOTRPG.Inventory.Core;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI;

/// <summary>
/// Bridges the inventory logging system into the Terminal.Gui game log panel.
///
/// Translates <see cref="IInventoryLogger"/> callbacks (item added, equipped, used, etc.)
/// into human-readable messages routed through <see cref="IGameLog"/>.
///
/// Adding new inventory events:
///   1. Add the method to <see cref="IInventoryLogger"/>
///   2. Implement it here with an appropriate log message
/// </summary>
public class TerminalGuiInventoryLogger : IInventoryLogger
{
    private readonly IGameLog _log;

    public TerminalGuiInventoryLogger(IGameLog log) => _log = log;

    // ── Item lifecycle ──────────────────────────────────────────────
    public void LogItemAdded(BaseItem item)       => _log.Log($"Added {item.Name} to inventory.");
    public void LogItemRemoved(BaseItem item)     => _log.Log($"Removed {item.Name} from inventory.");

    // ── Equipment events ────────────────────────────────────────────
    // Add new equip lines by adding a string (use {0}=item, {1}=slot)
    private static readonly string[] EquipFlavors =
    {
        "Equipped {0} to {1} slot.",
        "You ready {0} in the {1} slot. Feels solid.",
        "{0} equipped. You test its weight — perfect.",
        "You don {0}. The {1} slot hums with new power.",
    };

    public void LogItemEquipped(EquipmentBase equipment, EquipmentSlot slot)
    {
        string msg = string.Format(
            EquipFlavors[Random.Shared.Next(EquipFlavors.Length)],
            equipment.Name, slot);
        _log.Log(msg);
    }

    public void LogItemUnequipped(EquipmentBase equipment, EquipmentSlot slot)
        => _log.Log($"Unequipped {equipment.Name} from {slot} slot.");

    // ── Consumable usage ────────────────────────────────────────────
    // Add new potion flavor by adding a string to this array
    private static readonly string[] PotionFlavors =
    {
        "The potion's warmth spreads through your body.",
        "A bitter taste, but your wounds begin to close.",
        "You feel the healing magic take hold.",
        "The liquid glows faintly as you drink it down.",
        "Relief washes over you as the potion takes effect.",
    };

    // Add new food flavor by adding a string to this array
    private static readonly string[] FoodFlavors =
    {
        "A satisfying meal. Your stamina returns.",
        "You eat quickly between breaths. It helps.",
        "Simple food, but nourishing. You feel steadier.",
        "The taste of home. Even dungeon rations have their charm.",
        "You chew thoughtfully. Every bite counts in Aincrad.",
    };

    public void LogItemUsed(Consumable consumable, string effectDescription)
    {
        _log.Log($"Used {consumable.Name}. {effectDescription}");
        if (consumable is Potion)
            _log.Log(PotionFlavors[Random.Shared.Next(PotionFlavors.Length)]);
        else if (consumable is Food)
            _log.Log(FoodFlavors[Random.Shared.Next(FoodFlavors.Length)]);
    }

    // ── General messages ────────────────────────────────────────────
    public void LogError(string message) => _log.Log($"[Error] {message}");
    public void LogInfo(string message)  => _log.Log(message);
}
