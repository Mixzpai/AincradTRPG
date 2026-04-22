using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Systems;

// Per-player consumable quickbar: 10 slots keyed 1-0, stores DefinitionId (null = empty).
// Manual bind via Shift+N in InventoryDialog; auto-fills on first pickup if slot free.
public class QuickbarState
{
    public const int SlotCount = 10;

    // Index 0..9 = keys D1..D0. Null = empty.
    public string?[] SlotItemDefIds { get; set; } = new string?[SlotCount];

    public bool IsEmpty(int slot)
        => slot < 0 || slot >= SlotCount || string.IsNullOrEmpty(SlotItemDefIds[slot]);

    public void Bind(int slot, string? defId)
    {
        if (slot < 0 || slot >= SlotCount) return;
        SlotItemDefIds[slot] = string.IsNullOrEmpty(defId) ? null : defId;
    }

    public void Clear(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        SlotItemDefIds[slot] = null;
    }

    // Returns the slot index (0-9) already bound to defId, or -1.
    public int FindBoundSlot(string defId)
    {
        if (string.IsNullOrEmpty(defId)) return -1;
        for (int i = 0; i < SlotCount; i++)
            if (SlotItemDefIds[i] == defId) return i;
        return -1;
    }

    // Returns the lowest empty slot index, or -1 if full.
    public int FindEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
            if (string.IsNullOrEmpty(SlotItemDefIds[i])) return i;
        return -1;
    }

    // Auto-bind a newly-picked-up consumable. No-op if already bound or no
    // empty slot available. Only binds items with a DefinitionId.
    public void TryAutoBind(BaseItem item)
    {
        if (item is not Consumable) return;
        if (string.IsNullOrEmpty(item.DefinitionId)) return;
        if (FindBoundSlot(item.DefinitionId) >= 0) return;
        int slot = FindEmptySlot();
        if (slot < 0) return;
        SlotItemDefIds[slot] = item.DefinitionId;
    }

    // Resolve the first inventory consumable matching the slot's bound defId.
    // Returns null if slot empty or no matching item (or 0 quantity).
    public Consumable? ResolveItem(int slot, Player player)
    {
        if (slot < 0 || slot >= SlotCount) return null;
        string? defId = SlotItemDefIds[slot];
        if (string.IsNullOrEmpty(defId)) return null;
        foreach (var item in player.Inventory.Items)
        {
            if (item is Consumable c && c.DefinitionId == defId && c.Quantity > 0)
                return c;
        }
        return null;
    }

    // Single-character display glyph: first letter of item name, or '·' empty.
    // Count suffix (stackable qty) omitted here — HUD shows glyph only per research.
    public (char Glyph, bool Filled, int Count) SlotDisplay(int slot, Player player)
    {
        if (IsEmpty(slot)) return ('·', false, 0);
        string? defId = SlotItemDefIds[slot];
        int count = 0;
        string? firstName = null;
        foreach (var item in player.Inventory.Items)
        {
            if (item is Consumable c && c.DefinitionId == defId)
            {
                count += c.Quantity;
                firstName ??= c.Name;
            }
        }
        if (firstName == null)
        {
            // Slot bound but item not currently in inventory — try the registry
            // for a fallback glyph so the bind persists visually while out.
            var fallback = ItemRegistry.Create(defId!);
            firstName = fallback?.Name;
        }
        char glyph = !string.IsNullOrEmpty(firstName)
            ? char.ToUpperInvariant(firstName![0]) : '?';
        return (glyph, count > 0, count);
    }
}
