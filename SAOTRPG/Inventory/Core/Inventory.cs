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

    // Equipped gear, indexed by (int)EquipmentSlot. An array rather than a
    // Dictionary<EquipmentSlot, EquipmentBase?>: enums do not implement IEquatable, so a
    // dictionary keyed by one pays two virtual comparer calls per lookup, and slot lookups sit
    // on the combat path (TurnManager.SumAllSlots walks every slot several times per defence event).
    //
    // EquipmentSlot is contiguous from 0, and SlotCount is taken from the enum below, so
    // the index is the slot's own value. An out-of-range slot throws rather than landing in a
    // phantom entry that grants stats but can never be seen or unequipped.
    private static readonly int SlotCount = Enum.GetValues<EquipmentSlot>().Length;
    private readonly EquipmentBase?[] _equippedItems = new EquipmentBase?[SlotCount];

    private IInventoryLogger _logger;
    private readonly IEquipmentSlotResolver _slotResolver;

    public InventoryEvents Events { get; } = new();

    public void SetLogger(IInventoryLogger logger) => _logger = logger;

    // The backpack, read-only. The wrapper is built once and reused: _items is never replaced,
    // so one ReadOnlyCollection stays correct for the inventory's whole life. AsReadOnly()
    // allocates a fresh wrapper per call, and this property is read on every HUD refresh.
    private IReadOnlyList<BaseItem>? _itemsView;
    public IReadOnlyList<BaseItem> Items => _itemsView ??= _items.AsReadOnly();
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
    }

    private static int SlotIndex(EquipmentSlot slot)
    {
        if ((uint)slot >= (uint)SlotCount)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "Not a declared EquipmentSlot.");
        return (int)slot;
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
            return false;
        }

        _items.Add(item);
        _logger.LogItemAdded(item);
        Events.RaiseItemAdded(item);
        return true;
    }

    // First same-named stack in backpack order. A plain loop, so a pickup allocates no closure
    // or iterator.
    private bool TryStackItem(StackableItem stackable)
    {
        StackableItem? existingStack = null;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] is StackableItem s && s.Name == stackable.Name) { existingStack = s; break; }
        }

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
        if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.NakedIngress)
            && equipment is Armor
            && slot.Value != EquipmentSlot.Weapon && slot.Value != EquipmentSlot.OffHand)
        {
            _logger.LogError($"Naked Ingress modifier: cannot equip armor '{equipment.Name}'.");
            return false;
        }
        // Sword Art Only: weapons restricted to One-Handed Sword.
        if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.SwordArtOnly)
            && equipment is SAOTRPG.Items.Equipment.Weapon w
            && w.WeaponType != "One-Handed Sword")
        {
            _logger.LogError($"Sword Art Only modifier: only One-Handed Swords may be equipped (got {w.WeaponType}).");
            return false;
        }

        // Dual Blades / paired-weapon auto-route: occupied Weapon + empty OffHand routes second weapon to OffHand when
        // (a) Dual Blades unlocked AND incoming is 1H sword, OR (b) IsDualWieldPaired (no unlock needed).
        if (slot == EquipmentSlot.Weapon
            && _equippedItems[(int)EquipmentSlot.Weapon] != null
            && _equippedItems[(int)EquipmentSlot.OffHand] == null
            && equipment is Weapon incomingWeapon)
        {
            bool pairedBypass = incomingWeapon.IsDualWieldPaired;
            bool dualBladesClassic = incomingWeapon.WeaponType == "One-Handed Sword"
                && SAOTRPG.Systems.Skills.UniqueSkillSystem.HasDualBlades();
            if (pairedBypass || dualBladesClassic)
                slot = EquipmentSlot.OffHand;
        }

        // Ring overflow, the same shape as the dual-wield route above. Every ring resolves to
        // RightRing, so a second ring whose `Accessory.MaxEquipped` is 2 or more goes to an empty
        // LeftRing instead of evicting the first. This is the only route into LeftRing.
        if (slot == EquipmentSlot.RightRing
            && _equippedItems[(int)EquipmentSlot.RightRing] != null
            && _equippedItems[(int)EquipmentSlot.LeftRing] == null
            && equipment is Items.Equipment.Accessory ring
            && ring.MaxEquipped >= 2)
        {
            slot = EquipmentSlot.LeftRing;
        }

        // Out of the bag first, so a swap into a full bag has room for the old piece; if the old
        // piece still cannot come off, put the new one back and refuse rather than lose either.
        int bagIndex = _items.IndexOf(equipment);
        if (bagIndex >= 0) _items.RemoveAt(bagIndex);
        if (_equippedItems[SlotIndex(slot.Value)] != null && !Unequip(slot.Value, target))
        {
            if (bagIndex >= 0) _items.Insert(bagIndex, equipment);
            return false;
        }

        // Re-equipping the piece already in this slot: Unequip just put it back in the bag.
        _items.Remove(equipment);
        _equippedItems[SlotIndex(slot.Value)] = equipment;
        _statBonusCache = null;

        _logger.LogItemEquipped(equipment, slot.Value);
        Events.RaiseItemEquipped(equipment, slot.Value);
        return true;
    }

    public bool Unequip(EquipmentSlot slot, IStatModifiable target)
    {
        var equipment = _equippedItems[SlotIndex(slot)];
        if (equipment == null)
        {
            _logger.LogError($"No item equipped in {slot} slot.");
            return false;
        }

        if (IsFull)
        {
            _logger.LogError("Cannot unequip - inventory is full!");
            return false;
        }

        _equippedItems[SlotIndex(slot)] = null;
        _items.Add(equipment);
        _statBonusCache = null;

        _logger.LogItemUnequipped(equipment, slot);
        return true;
    }

    public EquipmentBase? GetEquipped(EquipmentSlot slot) => _equippedItems[SlotIndex(slot)];

    // Save/load path: places the item without Equip's checks or bag removal.
    public void ForceEquipForLoad(EquipmentSlot slot, EquipmentBase equipment)
    {
        _equippedItems[SlotIndex(slot)] = equipment;
        _statBonusCache = null;
    }

    // Force-remove an equipped item without returning it to inventory (e.g. item broke).
    public void DestroyEquipped(EquipmentSlot slot)
    {
        _equippedItems[SlotIndex(slot)] = null;
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

    // Cached aggregate stat bonuses (by StatType). Invalidated on Equip, Unequip,
    // ForceEquipForLoad and DestroyEquipped, plus externally via InvalidateStatCache.
    private int[]? _statBonusCache;
    private static readonly int StatTypeCount = Enum.GetValues<StatType>().Length;

    // Reused across rebuilds so the live-piece list is allocated once per inventory rather than
    // once per equip. Only ever touched inside GetTotalEquipmentBonus, and left empty afterwards.
    private readonly List<Items.Equipment.EquipmentBase> _liveScratch = new(SlotCount);

    // Invalidate after durability/enhancement/repair changes that bypass Equip/Unequip.
    public void InvalidateStatCache() => _statBonusCache = null;

    // The equipped piece actually providing the light — the widest non-broken radius, plus the
    // slot it sits in so a caller can spend or destroy it. Null when nothing is lit, which is the
    // normal state for a player carrying none rather than an error. Broken gear is skipped on the
    // same rule the stat loop uses: a torch burned down to zero durability has gone out.
    //
    // Returns a nullable value tuple, so the per-turn burn tick reads it without allocating.
    // Not cached: it is read once per turn from at most eleven slots, and a cache here would need
    // its own invalidation on every durability tick.
    public (EquipmentSlot Slot, EquipmentBase Item)? GetActiveLightSource()
    {
        EquipmentSlot bestSlot = default;
        EquipmentBase? best = null;
        var slots = _equippedItems;
        for (int i = 0; i < slots.Length; i++)
        {
            var eq = slots[i];
            if (eq == null || eq.ItemDurability <= 0 || eq.LightRadius <= 0) continue;
            if (best != null && eq.LightRadius <= best.LightRadius) continue;
            best = eq;
            bestSlot = (EquipmentSlot)i;
        }
        return best == null ? null : (bestSlot, best);
    }

    // Widest light any equipped piece casts, or 0 for a player carrying none. Derived from the
    // scan above rather than repeating it, so "how bright is the player" and "which item is
    // paying for that" can never disagree — the burn tick spends exactly the item the renderer
    // is lighting the map with.
    public int GetEquippedLightRadius() => GetActiveLightSource()?.Item.LightRadius ?? 0;

    // Backpack lights that could take over from a spent one, best first: widest radius, then
    // lowest remaining durability, so partial torches are burned down before fresh ones — which
    // is what a player swapping by hand would do.
    //
    // RequiredLevel is filtered here rather than left to Equip. Equip refuses a level it cannot
    // meet and returns false, so an unfiltered "widest radius" pick would hand a level-1 player
    // the Iron Lantern (RequiredLevel 5), fail, and leave them dark while appearing to work.
    // This narrows the field; it does not promise the equip succeeds, and the caller still tests
    // the return value.
    //
    // A fresh list rather than a lazy sequence: the caller equips out of it, and Equip removes
    // the item from _items, so iterating the live backpack would be mutating it mid-walk.
    public List<EquipmentBase> LightSourceCandidates(int level)
    {
        var list = new List<EquipmentBase>();
        foreach (var item in _items)
        {
            if (item is not EquipmentBase eq) continue;
            if (eq.LightRadius <= 0 || eq.ItemDurability <= 0 || eq.RequiredLevel > level) continue;
            list.Add(eq);
        }
        list.Sort((a, b) => b.LightRadius != a.LightRadius
            ? b.LightRadius.CompareTo(a.LightRadius)
            : a.ItemDurability.CompareTo(b.ItemDurability));
        return list;
    }

    // Every combat calculation reads one of these, so the hit path is a field read plus an array
    // index and nothing else — the rebuild lives in its own method so the JIT can inline this one.
    public int GetTotalEquipmentBonus(StatType statType)
        => (_statBonusCache ?? RebuildStatBonusCache())[(int)statType];

    private int[] RebuildStatBonusCache()
    {
        var cache = new int[StatTypeCount];
        var live = _liveScratch;
        live.Clear();

        var slots = _equippedItems;
        for (int i = 0; i < slots.Length; i++)
        {
            var eq = slots[i];
            if (eq == null || eq.ItemDurability <= 0) continue;
            live.Add(eq);
            var effects = eq.Bonuses?.Effects;
            if (effects == null) continue;
            for (int e = 0; e < effects.Count; e++)
            {
                var effect = effects[e];
                cache[(int)effect.Type] += effect.Potency;
            }
        }

        // Armour set bonuses fold in here rather than at a call site, so every consumer of
        // GetTotalEquipmentBonus sees them and the existing invalidation (Equip / Unequip /
        // ForceEquipForLoad / DestroyEquipped / InvalidateStatCache) covers them for free.
        // `live` excludes broken pieces, so a set at zero durability stops counting — the same
        // rule the per-item loop above already applies.
        Span<int> worn = stackalloc int[SAOTRPG.Systems.Combat.EquipmentSets.All.Length];
        SAOTRPG.Systems.Combat.EquipmentSets.CountWorn(live, worn);
        for (int i = 0; i < worn.Length; i++)
        {
            var bonuses = SAOTRPG.Systems.Combat.EquipmentSets.BonusesFor(SAOTRPG.Systems.Combat.EquipmentSets.All[i], worn[i]);
            if (bonuses == null) continue;
            foreach (var b in bonuses) cache[(int)b.Stat] += b.Value;
        }

        live.Clear();
        _statBonusCache = cache;
        return cache;
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