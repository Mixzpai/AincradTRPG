using Terminal.Gui;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;

namespace SAOTRPG.UI.Helpers;

// Rarity utilities — tags, sort, color, abbrev, log tags. Used by InventoryDialog/EquipmentSlotView/ShopDialog/TurnManager/loot.
// Tiers (low→high): Common(C,Gray), Uncommon(U,Green), Rare(R,Cyan), Epic(E,Magenta), Legendary(L,Yellow), Divine(◈,BrightRed — SAO Priority/Divine Object).
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

    // Bracketed tag prefix for list display: [◈], [L], [E], [R], [U], or spaces.
    public static string FormatTag(string? rarity) => rarity switch
    {
        "Divine"    => "[◈] ",
        "Legendary" => "[L] ",
        "Epic"      => "[E] ",
        "Rare"      => "[R] ",
        "Uncommon"  => "[U] ",
        _           => "    "
    };

    // Sort weight (higher = rarer). Used for ByRarity sort mode.
    public static int SortOrder(string? rarity) => rarity switch
    {
        "Divine"    => 6,
        "Legendary" => 5,
        "Epic"      => 4,
        "Rare"      => 3,
        "Uncommon"  => 2,
        "Common"    => 1,
        _           => 0
    };

    // Single-letter rarity codes for compact display. Divine uses ◈ diamond glyph.
    public static string Abbreviation(string? rarity) => rarity switch
    {
        "Common"    => "C",
        "Uncommon"  => "U",
        "Rare"      => "R",
        "Epic"      => "E",
        "Legendary" => "L",
        "Divine"    => "◈",
        _           => "?"
    };

    // Item name color based on rarity tier. Divine uses BrightRed — the "sacred
    // flame" / Mirror-tier / Exotic-tier convention above Legendary yellow.
    public static Color GetColor(string? rarity) => rarity switch
    {
        "Divine"    => Color.BrightRed,
        "Legendary" => Color.BrightYellow,
        "Epic"      => Color.BrightMagenta,
        "Rare"      => Color.BrightCyan,
        "Uncommon"  => Color.BrightGreen,
        "Common"    => Color.Gray,
        _           => Color.White
    };

    // Rarity tag for log messages: "[Divine ◈] ", "[Legendary] ", etc. Empty for Common.
    public static string LogTag(string? rarity) => rarity switch
    {
        "Divine"    => "[Divine ◈] ",
        "Legendary" => "[Legendary] ",
        "Epic"      => "[Epic] ",
        "Rare"      => "[Rare] ",
        "Uncommon"  => "[Uncommon] ",
        _           => ""
    };

    // True if the item is a Divine Object — unbreakable, bypasses block rolls.
    public static bool IsDivine(string? rarity) => rarity == "Divine";

    // Bundle 13 — Slicing Stone glyphs (Wave 2 evolve-dialog visual cue).
    // Lesser ◊ BrightCyan / Greater ◈ BrightMagenta / Perfect ✦ BrightYellow ("gold" of palette).
    public static string SlicingStoneGlyph(string? defId) => defId switch
    {
        "slicing_stone_lesser"  => "◊",
        "slicing_stone_greater" => "◈",
        "slicing_stone_perfect" => "✦",
        _                       => ""
    };

    public static Color SlicingStoneColor(string? defId) => defId switch
    {
        "slicing_stone_lesser"  => Color.BrightCyan,
        "slicing_stone_greater" => Color.BrightMagenta,
        "slicing_stone_perfect" => Color.BrightYellow,
        _                       => Color.White
    };
}
