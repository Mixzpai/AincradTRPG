using SAOTRPG.Inventory.Core;

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
    };

    // Builds HUD warning tags for equipped items below the durability threshold.
    // Example: "  [!WPN:5]  [!ARM:3]"
    public static string BuildWarningTags(SAOTRPG.Inventory.Core.Inventory inv, int threshold = 10)
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
