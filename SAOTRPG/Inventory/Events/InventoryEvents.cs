using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Events;

// Central event hub for inventory changes — subscribe to react to add/remove/equip/use
public class InventoryEvents
{
    // Item events — fired when items enter or leave the bag
    public event EventHandler<ItemEventArgs>? ItemAdded;
    public event EventHandler<ItemEventArgs>? ItemRemoved;

    // Equipment events — fired on equip/unequip actions
    public event EventHandler<EquipmentEventArgs>? ItemEquipped;
    public event EventHandler<EquipmentEventArgs>? ItemUnequipped;

    // Consumable events — fired when potions/food are used
    public event EventHandler<ConsumableEventArgs>? ConsumableUsed;

    // Capacity event — fired when bag is full
    public event EventHandler? InventoryFull;

    // Internal raise methods — only Inventory class should call these
    internal void RaiseItemAdded(BaseItem item) => ItemAdded?.Invoke(this, new ItemEventArgs(item));
    internal void RaiseItemRemoved(BaseItem item) => ItemRemoved?.Invoke(this, new ItemEventArgs(item));
    internal void RaiseItemEquipped(EquipmentBase equipment, EquipmentSlot slot) => ItemEquipped?.Invoke(this, new EquipmentEventArgs(equipment, slot));
    internal void RaiseItemUnequipped(EquipmentBase equipment, EquipmentSlot slot) => ItemUnequipped?.Invoke(this, new EquipmentEventArgs(equipment, slot));
    internal void RaiseConsumableUsed(Consumable consumable) => ConsumableUsed?.Invoke(this, new ConsumableEventArgs(consumable));
    internal void RaiseInventoryFull() => InventoryFull?.Invoke(this, EventArgs.Empty);
}
