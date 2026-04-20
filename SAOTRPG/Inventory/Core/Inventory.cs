using SAOTRPG.Entities;
using SAOTRPG.Inventory.Equipment;
using SAOTRPG.Inventory.Events;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Inventory.Core;

// Manages the player's inventory and equipped items.
public class Inventory
{
    private readonly int _maxSlots;
    private readonly List<BaseItem> _items = [];
    private readonly Dictionary<EquipmentSlot, EquipmentBase?> _equippedItems = [];

    private IInventoryLogger _logger;
    private readonly IEquipmentSlotResolver _slotResolver;

    public InventoryEvents Events { get; } = new();

    public void SetLogger(IInventoryLogger logger) => _logger = logger;

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

    // Add an item to the backpack. Attempts to stack if stackable. Returns false if full.
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

    public bool Equip(EquipmentBase equipment, IStatModifiable target)
    {
        if (target.Level < equipment.RequiredLevel)
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

        // Naked Ingress: no armor (any slot except weapon/offhand).
        if (SAOTRPG.Systems.RunModifiers.IsActive(SAOTRPG.Systems.RunModifier.NakedIngress)
            && equipment is SAOTRPG.Items.Equipment.Armor
            && slot.Value != EquipmentSlot.Weapon && slot.Value != EquipmentSlot.OffHand)
        {
            _logger.LogError($"Naked Ingress modifier: cannot equip armor '{equipment.Name}'.");
            return false;
        }
        // Sword Art Only: weapons restricted to One-Handed Sword.
        if (SAOTRPG.Systems.RunModifiers.IsActive(SAOTRPG.Systems.RunModifier.SwordArtOnly)
            && equipment is SAOTRPG.Items.Equipment.Weapon w
            && w.WeaponType != "One-Handed Sword")
        {
            _logger.LogError($"Sword Art Only modifier: only One-Handed Swords may be equipped (got {w.WeaponType}).");
            return false;
        }

        // Dual Blades / FD Paired auto-route: occupied Weapon + empty OffHand routes second weapon to OffHand when
        // (a) Dual Blades unlocked AND incoming is 1H sword, OR (b) IsDualWieldPaired (no unlock needed).
        if (slot == EquipmentSlot.Weapon
            && _equippedItems[EquipmentSlot.Weapon] != null
            && _equippedItems[EquipmentSlot.OffHand] == null
            && equipment is Weapon incomingWeapon)
        {
            bool pairedBypass = incomingWeapon.IsDualWieldPaired;
            bool dualBladesClassic = incomingWeapon.WeaponType == "One-Handed Sword"
                && SAOTRPG.Systems.Skills.UniqueSkillSystem.HasDualBlades();
            if (pairedBypass || dualBladesClassic)
                slot = EquipmentSlot.OffHand;
        }

        if (_equippedItems[slot.Value] != null)
        {
            Unequip(slot.Value, target);
        }

        _items.Remove(equipment);
        _equippedItems[slot.Value] = equipment;
        equipment.Equip(target);
        _statBonusCache = null;

        _logger.LogItemEquipped(equipment, slot.Value);
        Events.RaiseItemEquipped(equipment, slot.Value);
        return true;
    }

    public bool Unequip(EquipmentSlot slot, IStatModifiable target)
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

        equipment.Unequip(target);
        _equippedItems[slot] = null;
        _items.Add(equipment);
        _statBonusCache = null;

        _logger.LogItemUnequipped(equipment, slot);
        Events.RaiseItemUnequipped(equipment, slot);
        return true;
    }

    public EquipmentBase? GetEquipped(EquipmentSlot slot) => _equippedItems[slot];

    // Save/load path: saved base stats already include equipment bonuses, so skip re-applying.
    public void ForceEquipForLoad(EquipmentSlot slot, EquipmentBase equipment)
    {
        _equippedItems[slot] = equipment;
        _statBonusCache = null;
    }

    // Force-remove an equipped item without returning it to inventory (e.g. item broke).
    public void DestroyEquipped(EquipmentSlot slot)
    {
        _equippedItems[slot] = null;
        _statBonusCache = null;
    }

    public bool UseConsumable(Consumable consumable, IStatModifiable target)
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

        consumable.Use(target);
        _logger.LogItemUsed(consumable, consumable.EffectDescription ?? "");
        Events.RaiseConsumableUsed(consumable);

        if (consumable.Quantity <= 0)
        {
            _items.Remove(consumable);
        }

        return true;
    }

    // Cached aggregate stat bonuses (by StatType). Invalidated on Equip/Unequip/ForceEquip/Destroy + external via InvalidateStatCache.
    private int[]? _statBonusCache;
    private static readonly int StatTypeCount = Enum.GetValues<StatType>().Length;

    // Invalidate after durability/enhancement/repair changes that bypass Equip/Unequip.
    public void InvalidateStatCache() => _statBonusCache = null;

    public int GetTotalEquipmentBonus(StatType statType)
    {
        var cache = _statBonusCache;
        if (cache == null)
        {
            cache = new int[StatTypeCount];
            foreach (var eq in _equippedItems.Values)
            {
                if (eq?.Bonuses?.Effects == null || eq.ItemDurability <= 0) continue;
                foreach (var effect in eq.Bonuses.Effects)
                {
                    cache[(int)effect.Type] += effect.Potency;
                }
            }
            _statBonusCache = cache;
        }
        return cache[(int)statType];
    }

    // Count by DefinitionId across stacks — used by cooking/crafting.
    public int CountByDefinitionId(string definitionId)
    {
        int total = 0;
        foreach (var item in _items)
        {
            if (item.DefinitionId != definitionId) continue;
            total += item is StackableItem s ? s.Quantity : 1;
        }
        return total;
    }

    // Consume N by DefinitionId. Returns true on full consume; false if short (no partial consume).
    public bool ConsumeByDefinitionId(string definitionId, int count)
    {
        if (CountByDefinitionId(definitionId) < count) return false;
        int remaining = count;
        for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = _items[i];
            if (item.DefinitionId != definitionId) continue;
            if (item is StackableItem s)
            {
                int take = Math.Min(remaining, s.Quantity);
                s.Quantity -= take;
                remaining -= take;
                if (s.Quantity <= 0) _items.RemoveAt(i);
            }
            else
            {
                _items.RemoveAt(i);
                remaining--;
            }
        }
        return true;
    }
}