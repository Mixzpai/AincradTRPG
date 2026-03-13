using Inventory.Core;
using YourGame.Items;
using YourGame.Items.Consumables;
using EquipmentItem = YourGame.Items.Equipment.Equipment;

namespace Inventory.Events;

public class ItemEventArgs : EventArgs
{
    public BaseItem Item { get; }
    public ItemEventArgs(BaseItem item) => Item = item;
}

public class EquipmentEventArgs : EventArgs
{
    public EquipmentItem Equipment { get; }
    public EquipmentSlot Slot { get; }
    public EquipmentEventArgs(EquipmentItem equipment, EquipmentSlot slot)
    {
        Equipment = equipment;
        Slot = slot;
    }
}

public class ConsumableEventArgs : EventArgs
{
    public Consumable Consumable { get; }
    public ConsumableEventArgs(Consumable consumable) => Consumable = consumable;
}   