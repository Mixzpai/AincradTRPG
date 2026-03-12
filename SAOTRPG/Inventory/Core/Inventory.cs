using Inventory.Equipment;
using Inventory.Events;
using Inventory.Logging;

namespace Inventory.Core;

/// <summary>
/// Manages the player's inventory and equipped items.
/// </summary>
public class Inventory
{
    private readonly int _maxSlots;
    private readonly List<BaseItem> _items = [];
    private readonly Dictionary<EquipmentSlot, global::Equipment?> _equippedItems = [];

    private readonly IInventoryLogger _logger;
    private readonly IEquipmentSlotResolver _slotResolver;

    public InventoryEvents Events { get; } = new();

    public IReadOnlyList<BaseItem> Items => _items.AsReadOnly();
    public int ItemCount => _items.Count;
    public int MaxSlots => _maxSlots;
    public bool IsFull => _items.Count >= _maxSlots;

    public Inventory(
        int maxSlots = 20,
        IInventoryLogger? logger = null,
        IEquipmentSlotResolver? slotResolver = null)
    {
        _maxSlots = maxSlots;
        _logger = logger ?? new ConsoleInventoryLogger();
        _slotResolver = slotResolver ?? new EquipmentSlotResolver();

        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            _equippedItems[slot] = null;
        }
    }

    public bool AddItem(BaseItem item)
    {
        if (item == null)
        {
            _logger.LogError("Cannot add null item to inventory.");
            return false;
        }

        if (item is StackableItem stackable && TryStackItem(stackable))
        {
            return true;
        }

        if (IsFull)
        {
            _logger.LogError($"Inventory is full. Cannot add item: {item.Name}");
            Events.RaiseInventoryFull();
            return false;
        }

        _items.Add(item);
        _logger.LogItemAdded(item);
        Events.RaiseItemAdded(item);
        return true;
    }

    private bool TryStackItem(StackableItem stackable)
    {
        var existingStack = _items.OfType<StackableItem>()
            .FirstOrDefault(i => i.Name == stackable.Name);

        if (existingStack != null)
        {
            int remaining = existingStack.Stack(stackable);
            return remaining <= 0;
        }
        return false;
    }

    public bool RemoveItem(BaseItem item)
    {
        if (_items.Remove(item))
        {
            _logger.LogItemRemoved(item);
            Events.RaiseItemRemoved(item);
            return true;
        }
        return false;
    }

    public bool Equip(global::Equipment equipment, Player player)
    {
        if (player.Level < equipment.RequiredLevel)
        {
            _logger.LogError($"Cannot equip {equipment.Name}. Required level: {equipment.RequiredLevel}");
            return false;
        }

        var slot = _slotResolver.ResolveSlot(equipment);
        if (slot == null)
        {
            _logger.LogError($"Unknown equipment type: {equipment.EquipmentType}");
            return false;
        }

        if (_equippedItems[slot.Value] != null)
        {
            Unequip(slot.Value, player);
        }

        _items.Remove(equipment);
        _equippedItems[slot.Value] = equipment;
        equipment.Equip(player);

        _logger.LogItemEquipped(equipment, slot.Value);
        Events.RaiseItemEquipped(equipment, slot.Value);
        return true;
    }

    public bool Unequip(EquipmentSlot slot, Player player)
    {
        var equipment = _equippedItems[slot];
        if (equipment == null)
        {
            _logger.LogError($"No item equipped in {slot} slot.");
            return false;
        }

        if (IsFull)
        {
            _logger.LogError("Cannot unequip - inventory is full!");
            Events.RaiseInventoryFull();
            return false;
        }

        equipment.Unequip(player);
        _equippedItems[slot] = null;
        _items.Add(equipment);

        _logger.LogItemUnequipped(equipment, slot);
        Events.RaiseItemUnequipped(equipment, slot);
        return true;
    }

    public global::Equipment? GetEquipped(EquipmentSlot slot) => _equippedItems[slot];

    public bool UseConsumable(Consumable consumable, Player player)
    {
        if (!_items.Contains(consumable))
        {
            _logger.LogError($"{consumable.Name} is not in inventory.");
            return false;
        }

        if (consumable.Quantity <= 0)
        {
            _logger.LogError($"No {consumable.Name} remaining.");
            return false;
        }

        consumable.Use(player);
        _logger.LogItemUsed(consumable, consumable.EffectDescription ?? "");
        Events.RaiseConsumableUsed(consumable);

        if (consumable.Quantity <= 0)
        {
            _items.Remove(consumable);
        }

        return true;
    }

    public void DisplayInventory()
    {
        _logger.LogInfo($"=== Inventory ({_items.Count}/{_maxSlots}) ===");

        if (_items.Count == 0)
        {
            _logger.LogInfo("  (Empty)");
        }
        else
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                string quantityText = item is StackableItem s ? $" x{s.Quantity}" : "";
                _logger.LogInfo($"  [{i + 1}] {item.Name}{quantityText} ({item.Rarity})");
            }
        }

        _logger.LogInfo("");
        _logger.LogInfo("=== Equipped Items ===");
        foreach (var slot in _equippedItems)
        {
            _logger.LogInfo($"  {slot.Key}: {slot.Value?.Name ?? "(Empty)"}");
        }
    }

    public int GetTotalEquipmentBonus(StatType statType)
    {
        return _equippedItems.Values
            .Where(e => e?.Bonuses?.Effects != null)
            .SelectMany(e => e!.Bonuses.Effects)
            .Where(effect => effect.Type == statType)
            .Sum(effect => effect.Potency);
    }
}