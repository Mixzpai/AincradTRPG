using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;

namespace SAOTRPG.UI.Helpers;

// Centralized rarity utilities — tag formatting, sort order, color, abbreviation, log tags.
// Used by InventoryDialog, EquipmentSlotView, ShopDialog, TurnManager, and loot displays.
// Tier order (lowest → highest):
//   Common (C, Gray) → Uncommon (U, Green) → Rare (R, Cyan) → Epic (E, Magenta) → Legendary (L, Yellow)
public static class RarityHelper
{
    // Formats a complete item line for list display: "[R] Iron Sword x1 (R)"
    // Consistent format used across inventory, shop, and loot screens.
    public static string FormatItemLine(BaseItem item, int maxWidth)
    {
        string qty = item is StackableItem s ? $" x{s.Quantity}" : "";
        string tag = FormatTag(item.Rarity);
        string abbrev = Abbreviation(item.Rarity);
        string line = $" {tag}{item.Name}{qty} ({abbrev})";
        return line.Length > maxWidth ? line[..maxWidth] : line.PadRight(maxWidth);
    }

    // Bracketed tag prefix for list display: [E], [R], [U], or spaces.
    public static string FormatTag(string? rarity) => rarity switch
    {
        "Epic"      => "[E] ",
        "Legendary" => "[L] ",
        "Rare"      => "[R] ",
        "Uncommon"  => "[U] ",
        _           => "    "
    };

    // Sort weight (higher = rarer). Used for ByRarity sort mode.
    public static int SortOrder(string? rarity) => rarity switch
    {
        "Legendary" => 5,
        "Epic"      => 4,
        "Rare"      => 3,
        "Uncommon"  => 2,
        "Common"    => 1,
        _           => 0
    };

    // Single-letter rarity codes for compact display.
    public static string Abbreviation(string? rarity) => rarity switch
    {
        "Common"    => "C",
        "Uncommon"  => "U",
        "Rare"      => "R",
        "Epic"      => "E",
        "Legendary" => "L",
        _           => "?"
    };

    // Item name color based on rarity tier.
    public static Color GetColor(string? rarity) => rarity switch
    {
        "Legendary" => Color.BrightYellow,
        "Epic"      => Color.BrightMagenta,
        "Rare"      => Color.BrightCyan,
        "Uncommon"  => Color.BrightGreen,
        "Common"    => Color.Gray,
        _           => Color.White
    };

    // Rarity tag for log messages: "[Rare] ", "[Epic] ", etc. Empty for Common.
    public static string LogTag(string? rarity) => rarity switch
    {
        "Legendary" => "[Legendary] ",
        "Epic"      => "[Epic] ",
        "Rare"      => "[Rare] ",
        "Uncommon"  => "[Uncommon] ",
        _           => ""
    };
}
