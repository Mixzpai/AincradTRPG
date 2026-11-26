using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI.Helpers;

// Durability display helpers for HUD + Look. Modes: Warning tags "[!WPN:5]" (below threshold, default 10),
// Snapshot "Wpn:45  Hd:30" (compact all-equipped overview).
public static class DurabilityHelper
{
    // Per-slot abbreviation pairs: (HUD warning tag, Look snapshot tag).
    // HUD tags are uppercase 3-letter codes; Look tags are short 2-letter codes.
    private static readonly Dictionary<EquipmentSlot, (string Hud, string Look)> SlotTags = new()
    {
        [EquipmentSlot.Weapon]   = ("WPN", "Wpn"),
        [EquipmentSlot.Head]     = ("HLM", "Hd"),
        [EquipmentSlot.Chest]    = ("ARM", "Ch"),
        [EquipmentSlot.Legs]     = ("LEG", "Lg"),
        [EquipmentSlot.Feet]     = ("BTS", "Ft"),
        [EquipmentSlot.OffHand]  = ("SLD", "OH"),
        [EquipmentSlot.RightRing]= ("RRG", "RR"),
        [EquipmentSlot.LeftRing] = ("LRG", "LR"),
        [EquipmentSlot.Bracelet] = ("BRC", "Br"),
        [EquipmentSlot.Necklace] = ("NKL", "Nk"),
        // The Tool slot was missing from this table entirely, so a worn-out pickaxe never warned —
        // and once the slot started holding the player's only light source, neither did a torch
        // about to leave them blind. An absolute threshold suits fuel well: a light spends one
        // point every LightBurnTurns turns whatever its tier, so "below 10" is the same ~330 turns
        // of warning for a Wooden Torch and a Crystal Lantern alike.
        [EquipmentSlot.Tool]     = ("TOL", "Tl"),
    };

    // Every slot this table covers. Public so a check can assert the table is complete against the
    // EquipmentSlot enum rather than against a second hand-written list.
    public static IReadOnlyCollection<EquipmentSlot> TaggedSlots => SlotTags.Keys;

    // What "Repair All" restores an item to. An item that declares its OWN capacity is repaired
    // to that; anything else falls back to a floor-derived ceiling. Public and static so the
    // invariant can be checked against the real implementation rather than a copy of it — while
    // this was inline in the Anvil dialog, a check could only recompute the formula and would
    // therefore pass against any change to it.
    public static int RepairCeiling(EquipmentBase eq, int floor) =>
        eq.MaxDurability > 0
            ? eq.MaxDurability
            : 50 + floor * 10 + eq.EnhancementLevel * 5;

    // The durability below which the HUD shows a warning tag. Named so the Guide's account of it
    // is checkable, and so the two warning ladders — this one and the combat log's — can be
    // compared rather than drifting apart.
    public const int HudWarnBelow = 10;

    // Builds HUD warning tags for equipped items below the durability threshold.
    // Example: "  [!WPN:5]  [!ARM:3]"
    public static string BuildWarningTags(SAOTRPG.Inventory.Core.Inventory inv,
                                          int threshold = HudWarnBelow)
    {
        string result = "";
        foreach (var (slot, tags) in SlotTags)
        {
            var eq = inv.GetEquipped(slot);
            if (eq != null && eq.ItemDurability < threshold)
                result += $"  [!{tags.Hud}:{eq.ItemDurability}]";
        }
        return result;
    }
}
