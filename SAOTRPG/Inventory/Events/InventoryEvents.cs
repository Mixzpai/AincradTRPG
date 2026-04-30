using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Events;

// Central event hub for inventory changes — subscribe to react to add/equip/use
public class InventoryEvents
{
    // Item events — fired when items enter the bag
    public event EventHandler<ItemEventArgs>? ItemAdded;

    // Equipment events — fired on equip actions
    public event EventHandler<EquipmentEventArgs>? ItemEquipped;

    // Consumable events — fired when potions/food are used
    public event EventHandler<ConsumableEventArgs>? ConsumableUsed;

    // Internal raise methods — only Inventory class should call these
    internal void RaiseItemAdded(BaseItem item) => ItemAdded?.Invoke(this, new ItemEventArgs(item));
    internal void RaiseItemEquipped(EquipmentBase equipment, EquipmentSlot slot) => ItemEquipped?.Invoke(this, new EquipmentEventArgs(equipment, slot));
    internal void RaiseConsumableUsed(Consumable consumable) => ConsumableUsed?.Invoke(this, new ConsumableEventArgs(consumable));
}
