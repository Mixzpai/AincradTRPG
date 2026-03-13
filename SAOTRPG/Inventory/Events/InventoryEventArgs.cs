using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Events;

// Event args for generic item add/remove
public class ItemEventArgs : EventArgs
{
    public BaseItem Item { get; }
    public ItemEventArgs(BaseItem item) => Item = item;
}

// Event args for equip/unequip — carries both the gear and the slot
public class EquipmentEventArgs : EventArgs
{
    public EquipmentBase Equipment { get; }
    public EquipmentSlot Slot { get; }
    public EquipmentEventArgs(EquipmentBase equipment, EquipmentSlot slot)
    {
        Equipment = equipment;
        Slot = slot;
    }
}

// Event args for consumable use
public class ConsumableEventArgs : EventArgs
{
    public Consumable Consumable { get; }
    public ConsumableEventArgs(Consumable consumable) => Consumable = consumable;
}
