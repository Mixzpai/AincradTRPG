using Inventory.Core;
using YourGame.Items;
using YourGame.Items.Consumables;
using EquipmentItem = YourGame.Items.Equipment.Equipment;

namespace Inventory.Events;

public class InventoryEvents
{
    public event EventHandler<ItemEventArgs>? ItemAdded;
    public event EventHandler<ItemEventArgs>? ItemRemoved;
    public event EventHandler<EquipmentEventArgs>? ItemEquipped;
    public event EventHandler<EquipmentEventArgs>? ItemUnequipped;
    public event EventHandler<ConsumableEventArgs>? ConsumableUsed;
    public event EventHandler? InventoryFull;

    internal void RaiseItemAdded(BaseItem item)
        => ItemAdded?.Invoke(this, new ItemEventArgs(item));

    internal void RaiseItemRemoved(BaseItem item)
        => ItemRemoved?.Invoke(this, new ItemEventArgs(item));

    internal void RaiseItemEquipped(EquipmentItem equipment, EquipmentSlot slot)
        => ItemEquipped?.Invoke(this, new EquipmentEventArgs(equipment, slot));

    internal void RaiseItemUnequipped(EquipmentItem equipment, EquipmentSlot slot)
        => ItemUnequipped?.Invoke(this, new EquipmentEventArgs(equipment, slot));

    internal void RaiseConsumableUsed(Consumable consumable)
        => ConsumableUsed?.Invoke(this, new ConsumableEventArgs(consumable));

    internal void RaiseInventoryFull()
        => InventoryFull?.Invoke(this, EventArgs.Empty);
}