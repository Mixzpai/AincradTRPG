/****************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

/****************************************************************************************/
// Equipment Slots

public enum EquipmentSlot
{
    Weapon,
    Head,
    Chest,
    Legs,
    Feet,
    RightRing,
    LeftRing,
    Bracelet,
    Necklace
}

/****************************************************************************************/
// Inventory Class - Manages the player's inventory and equipped items
public class Inventory
{
    // Fields
    // The maximum number of slots in the inventory
    private readonly int _MaxSlots;
    // The list of items currently in the inventory
    private readonly List<BaseItem> _items = [];
    // The currently equipped items in each equipment slot
    private readonly Dictionary<EquipmentSlot, Equipment?> _equippedItems = [];

    // Constructor - Initializes the inventory with a specified maximum number of slots
    public Inventory(int MaxSlots)
    {
        _MaxSlots = MaxSlots;

        // Initialize the equipped items dictionary with null values for each equipment slot
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            _equippedItems[slot] = null;
        }
    }

    /****************************************************************************************/
    // Item Management
    public IReadOnlyList<BaseItem> Items => _items.AsReadOnly();
    public int ItemCount => _items.Count;
    public int MaxSlots => _MaxSlots;
    public bool IsFull => _items.Count >= _MaxSlots;

    /****************************************************************************************/
    // Adds an item to the inventory if there is space
    public bool AddItem(BaseItem item)
    {
        // Check if the inventory is full before adding the item
        if (IsFull)
        {
            Console.WriteLine("Inventory is full. Cannot add item: " + item.Name);
            return false;
        }

        // Check if the item is null before adding it to the inventory
        if (item == null)
        {
            Console.WriteLine("Cannot add null item to inventory.");
            return false;
        }

        // If the item is stackable, try to stack it with existing items in the inventory
        if (item is StackableItem stackable)
        {
            // Look for an existing stack of the same item in the inventory
            var existingStack = _items.OfType<StackableItem>().FirstOrDefault(i => i.Name == stackable.Name);

            // If an existing stack is found, try to stack the new item with it
            if (existingStack != null)
            {
                int remaining = existingStack.Stack(stackable);
                // If there are remaining items that couldn't be stacked, add them to the inventory as a new stack
                if (remaining <= 0)
                {
                    return true;
                }
            }
        }

        // Remove the call to CanEquip and directly add the equipment to inventory if not auto-equipping
        if (item is Equipment equipment)
        {
            // Optionally, you could check for auto-equip logic here, but since CanEquip does not exist,
            // just add the equipment to the inventory as with other items.
            _items.Add(item);
            Console.WriteLine($"Added {item.Name} to inventory.");
            return true;
        }

        _items.Add(item);
        Console.WriteLine($"Added {item.Name} to inventory.");
        return true;
    }

    /****************************************************************************************/
    // Remove and item from the inventory
    public bool RemoveItem(BaseItem item)
    {
        if (_items.Remove(item))
        {
            Console.WriteLine($"Removed {item.Name} from inventory.");
            return true;
        }
        return false;
    }

    /****************************************************************************************/
    // Get the equipment slot for an item base on its type
    private static EquipmentSlot? GetEquipmentSlot(Equipment equipment)
    {
        return equipment.EquipmentType?.ToLower() switch
        {
            "broadsword" or "longsword" or "rapier" or "dagger" or "mace" or "hammer" or "polearm" or "haukax" => EquipmentSlot.Weapon,
            "cap" or "bandana" or "hood" or "circlet" or "mask" or "coif" or "helm" or "helmet" or "visor" => EquipmentSlot.Head,
            "coat" or "robe" or "mail" or "jacket" or "plate" => EquipmentSlot.Chest,
            "pants" or "leggings" or "trousers" or "greaves" => EquipmentSlot.Legs,
            "shoes" or "boots" or "sabatons" => EquipmentSlot.Feet,
            "ring-right" => EquipmentSlot.RightRing,
            "left-left" => EquipmentSlot.LeftRing,
            "bracelet" or "bangle " => EquipmentSlot.Bracelet,
            "necklace" => EquipmentSlot.Necklace,
            _ => null
        };
    }

    /****************************************************************************************/
    // Equip an item to the appropriate equipment slot
    public bool Equip(Equipment equipment, Player player)
    {
        if (player.Level < equipment.RequiredLevel)
        {
            Console.WriteLine($"Cannot equip {equipment.Name}. Required level: {equipment.RequiredLevel}");
            return false;
        }

        var slot = GetEquipmentSlot(equipment);
        if (slot == null)
        {
            Console.WriteLine($"Unknown equipment type: {equipment.EquipmentType}");
            return false;
        }

        // Unequip current item in the slot if any
        if (_equippedItems[slot.Value] != null)
        {
            Unequip(slot.Value, player);
        }

        // Remove from inventory and equip
        _items.Remove(equipment);
        _equippedItems[slot.Value] = equipment;
        equipment.Bonuses.ApplyTo(player);

        Console.WriteLine($"Equipped {equipment.Name} to {slot.Value} slot.");
        return true;
    }

    /****************************************************************************************/
    // Unequip an item from a slot
    public bool Unequip(EquipmentSlot slot, Player player)
    {
        var equipment = _equippedItems[slot];
        if (equipment == null)
        {
            Console.WriteLine($"No item equipped in {slot} slot.");
            return false;
        }

        if (IsFull)
        {
            Console.WriteLine("Cannot unequip - inventory is full!");
            return false;
        }

        // Remove bonuses and return to inventory
        equipment.Bonuses.RemoveFrom(player);
        _equippedItems[slot] = null;
        _items.Add(equipment);

        Console.WriteLine($"Unequipped {equipment.Name} from {slot} slot.");
        return true;
    }

    /****************************************************************************************/
    // Get equipped item in a specific slot
    public Equipment? GetEquipped(EquipmentSlot slot) => _equippedItems[slot];

    /****************************************************************************************/
    // Use a consumable item
    public bool UseConsumable(Consumable consumable, Player player)
    {
        if (!_items.Contains(consumable))
        {
            Console.WriteLine($"{consumable.Name} is not in inventory.");
            return false;
        }

        if (consumable.Quantity <= 0)
        {
            Console.WriteLine($"No {consumable.Name} remaining.");
            return false;
        }

        consumable.Use(player);
        Console.WriteLine($"Used {consumable.Name}. {consumable.EffectDescription}");

        // Remove item if quantity reaches 0
        if (consumable.Quantity <= 0)
        {
            _items.Remove(consumable);
        }

        return true;
    }

    /****************************************************************************************/
    // Display inventory contents
    public void DisplayInventory()
    {
        Console.WriteLine($"=== Inventory ({_items.Count}/{_MaxSlots}) ===");

        if (_items.Count == 0)
        {
            Console.WriteLine("  (Empty)");
        }
        else
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                string quantityText = item is StackableItem s ? $" x{s.Quantity}" : "";
                Console.WriteLine($"  [{i + 1}] {item.Name}{quantityText} ({item.Rarity})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== Equipped Items ===");
        foreach (var slot in _equippedItems)
        {
            string itemName = slot.Value?.Name ?? "(Empty)";
            Console.WriteLine($"  {slot.Key}: {itemName}");
        }
    }

    /****************************************************************************************/
    // Calculate total stat bonuses from all equipped items
    public int GetTotalEquipmentBonus(StatType statType)
    {
        int total = 0;
        foreach (var equipment in _equippedItems.Values)
        {
            if (equipment?.Bonuses?.Effects == null) continue;

            foreach (var effect in equipment.Bonuses.Effects)
            {
                if (effect.Type == statType)
                {
                    total += effect.Potency;
                }
            }
        }
        return total;
    }
}
/****************************************************************************************/

