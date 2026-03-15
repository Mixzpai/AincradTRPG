using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;

namespace SAOTRPG.UI.Helpers;

/// <summary>
/// Custom view that renders equipment slots with individually colored icons.
/// Handles up/down navigation and selection highlighting.
/// Shared between EquipmentDialog (T key) and InventoryDialog (I key).
/// </summary>
public class EquipmentSlotView : View
{
    // ── Slot display definitions ────────────────────────────────────────
    // Unicode icons with per-slot colors for quick visual scanning.
    public static readonly (EquipmentSlot Slot, char Icon, Color IconColor, string Label)[] DefaultSlotLayout =
    {
        (EquipmentSlot.Weapon,    '\u2694', Color.BrightYellow, "Weapon"),      // ⚔
        (EquipmentSlot.OffHand,   '\u25C8', Color.BrightCyan,   "Off-Hand"),   // ◈
        (EquipmentSlot.Head,      '\u25B2', Color.White,         "Head"),       // ▲
        (EquipmentSlot.Chest,     '\u25A0', Color.White,         "Chest"),      // ■
        (EquipmentSlot.Legs,      '\u25BC', Color.White,         "Legs"),       // ▼
        (EquipmentSlot.Feet,      '\u25AA', Color.White,         "Feet"),       // ▪
        (EquipmentSlot.RightRing, '\u25CB', Color.BrightMagenta, "Right Ring"), // ○
        (EquipmentSlot.LeftRing,  '\u25CB', Color.BrightMagenta, "Left Ring"),  // ○
        (EquipmentSlot.Bracelet,  '\u25CB', Color.BrightGreen,   "Bracelet"),   // ○
        (EquipmentSlot.Necklace,  '\u25CB', Color.BrightGreen,   "Necklace"),   // ○
    };

    public static int SlotCount => DefaultSlotLayout.Length;

    private readonly Player _player;
    private readonly (EquipmentSlot Slot, char Icon, Color IconColor, string Label)[] _slots;
    private readonly int _contentWidth;
    private int _selectedIndex;

    public int SelectedIndex => _selectedIndex;
    public event Action<int>? SelectedSlotChanged;

    public EquipmentSlotView(Player player, int contentWidth)
        : this(player, DefaultSlotLayout, contentWidth) { }

    public EquipmentSlotView(
        Player player,
        (EquipmentSlot Slot, char Icon, Color IconColor, string Label)[] slots,
        int contentWidth)
    {
        _player = player;
        _slots = slots;
        _contentWidth = contentWidth;
        _selectedIndex = -1; // no highlight until user presses arrow/WASD
        CanFocus = true;
    }

    protected override bool OnKeyDown(Key keyEvent)
    {
        if (keyEvent.KeyCode == KeyCode.CursorUp || keyEvent.KeyCode == KeyCode.W)
        {
            if (_selectedIndex < 0)
                _selectedIndex = 0; // first press activates highlight
            else if (_selectedIndex > 0)
                _selectedIndex--;
            SelectedSlotChanged?.Invoke(_selectedIndex);
            SetNeedsDraw();
            keyEvent.Handled = true;
            return true;
        }
        if (keyEvent.KeyCode == KeyCode.CursorDown || keyEvent.KeyCode == KeyCode.S)
        {
            if (_selectedIndex < 0)
                _selectedIndex = 0; // first press activates highlight
            else if (_selectedIndex < _slots.Length - 1)
                _selectedIndex++;
            SelectedSlotChanged?.Invoke(_selectedIndex);
            SetNeedsDraw();
            keyEvent.Handled = true;
            return true;
        }
        return base.OnKeyDown(keyEvent);
    }

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        for (int i = 0; i < _slots.Length && i < vp.Height; i++)
        {
            var (slot, icon, iconColor, label) = _slots[i];
            var eq = _player.Inventory.GetEquipped(slot);
            bool selected = (i == _selectedIndex && HasFocus);

            // Background for selection highlight
            var bgColor = selected ? Color.DarkGray : Color.Black;

            // Clear the line
            Driver.SetAttribute(new Terminal.Gui.Attribute(Color.Gray, bgColor));
            Move(0, i);
            for (int c = 0; c < _contentWidth; c++)
                Driver.AddRune(new System.Text.Rune(' '));

            // " " prefix
            Move(0, i);
            Driver.SetAttribute(new Terminal.Gui.Attribute(Color.Gray, bgColor));
            Driver.AddStr(" ");

            // Colored icon
            Driver.SetAttribute(new Terminal.Gui.Attribute(iconColor, bgColor));
            Driver.AddRune(new System.Text.Rune(icon));

            // Space + label (fixed width)
            Driver.SetAttribute(new Terminal.Gui.Attribute(Color.Gray, bgColor));
            Driver.AddStr(" ");
            string paddedLabel = label.PadRight(12);
            Driver.AddStr(paddedLabel);

            // Item name or (empty)
            if (eq != null)
            {
                var nameColor = GetRarityColor(eq.Rarity);
                Driver.SetAttribute(new Terminal.Gui.Attribute(nameColor, bgColor));
                Driver.AddStr(eq.Name);

                Driver.SetAttribute(new Terminal.Gui.Attribute(Color.DarkGray, bgColor));
                Driver.AddStr($" ({eq.Rarity})");
            }
            else
            {
                Driver.SetAttribute(new Terminal.Gui.Attribute(Color.DarkGray, bgColor));
                Driver.AddStr("(empty)");
            }
        }
        return true;
    }

    /// <summary>Item name color based on rarity tier.</summary>
    public static Color GetRarityColor(string? rarity)
    {
        if (rarity == "Epic") return Color.BrightMagenta;
        if (rarity == "Rare") return Color.BrightCyan;
        if (rarity == "Uncommon") return Color.BrightGreen;
        return Color.White;
    }
}
