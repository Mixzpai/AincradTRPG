using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;

namespace SAOTRPG.UI.Helpers;

// Custom view that renders equipment slots with individually colored icons.
// Handles up/down navigation and selection highlighting.
// Shared between EquipmentDialog (T key) and InventoryDialog (I key).
// Each row: [icon] SlotLabel    ItemName (R)
//   - Icon is color-coded per slot type
//   - Item name is color-coded by rarity
//   - Rarity shown as single-letter abbreviation: (C)ommon, (U)ncommon, etc.
//   - Selected row gets a dark gray highlight background
public class EquipmentSlotView : View
{
    // ── Constants ─────────────────────────────────────────────────────
    // LabelPadWidth = 12 fits the longest slot label ("Right Ring" = 10 chars) + 2 padding.
    private const int LabelPadWidth = 12;

    // Placeholder text shown when no item is equipped in a slot.
    private const string EmptySlotText = "(empty)";

    // ── Slot display definitions ─────────────────────────────────────
    // Unicode icons with per-slot colors for quick visual scanning.
    public static readonly (EquipmentSlot Slot, char Icon, Color IconColor, string Label)[] DefaultSlotLayout =
    {
        (EquipmentSlot.Weapon,    '\u2694', Color.BrightYellow,  "Weapon"),      // ⚔
        (EquipmentSlot.OffHand,   '\u25C8', Color.BrightCyan,    "Off-Hand"),   // ◈
        (EquipmentSlot.Head,      '\u25B2', Color.White,          "Head"),       // ▲
        (EquipmentSlot.Chest,     '\u25A0', Color.White,          "Chest"),      // ■
        (EquipmentSlot.Legs,      '\u25BC', Color.White,          "Legs"),       // ▼
        (EquipmentSlot.Feet,      '\u25AA', Color.White,          "Feet"),       // ▪
        (EquipmentSlot.RightRing, '\u25CB', Color.BrightMagenta,  "Right Ring"), // ○
        (EquipmentSlot.LeftRing,  '\u25CB', Color.BrightMagenta,  "Left Ring"),  // ○
        (EquipmentSlot.Bracelet,  '\u25CB', Color.BrightGreen,    "Bracelet"),   // ○
        (EquipmentSlot.Necklace,  '\u25CB', Color.BrightGreen,    "Necklace"),   // ○
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

    // ── Navigation ───────────────────────────────────────────────────

    protected override bool OnKeyDown(Key keyEvent)
    {
        int delta = keyEvent.KeyCode switch
        {
            KeyCode.CursorUp or KeyCode.W => -1,
            KeyCode.CursorDown or KeyCode.S => 1,
            _ => 0
        };

        if (delta == 0) return base.OnKeyDown(keyEvent);

        if (_selectedIndex < 0)
            _selectedIndex = 0; // first press activates highlight
        else
            _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _slots.Length - 1);

        SelectedSlotChanged?.Invoke(_selectedIndex);
        SetNeedsDraw();
        keyEvent.Handled = true;
        return true;
    }

    // ── Rendering ────────────────────────────────────────────────────

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        for (int i = 0; i < _slots.Length && i < vp.Height; i++)
        {
            var (slot, icon, iconColor, label) = _slots[i];
            var eq = _player.Inventory.GetEquipped(slot);
            bool selected = i == _selectedIndex && HasFocus;
            var bg = selected ? Color.DarkGray : Color.Black;

            // Clear the line
            SetAttr(Color.Gray, bg);
            Move(0, i);
            Driver!.AddStr(new string(' ', _contentWidth));

            // " " prefix + colored icon + " " + padded label
            Move(0, i);
            Driver!.AddStr(" ");

            SetAttr(iconColor, bg);
            Driver!.AddRune((System.Text.Rune)icon);

            SetAttr(Color.Gray, bg);
            Driver!.AddStr($" {label.PadRight(LabelPadWidth)}");

            // Item name + rarity abbreviation, or (empty)
            if (eq != null)
            {
                SetAttr(GetRarityColor(eq.Rarity), bg);
                Driver!.AddStr(eq.Name ?? "");

                SetAttr(Color.DarkGray, bg);
                Driver!.AddStr($" ({AbbrevRarity(eq.Rarity)})");

                if (eq.ItemDurability <= 0)
                {
                    SetAttr(Color.BrightRed, bg);
                    Driver!.AddStr(" BROKEN");
                }
            }
            else
            {
                SetAttr(Color.DarkGray, bg);
                Driver!.AddStr(EmptySlotText);
            }
        }
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    // Shorthand for setting driver attribute.
    private void SetAttr(Color fg, Color bg)
        => Driver!.SetAttribute(Gfx.Attr(fg, bg));

    // Item name color based on rarity tier. Delegates to RarityHelper.
    public static Color GetRarityColor(string? rarity) => RarityHelper.GetColor(rarity);

    // Single-letter rarity codes for compact display. Delegates to RarityHelper.
    public static string AbbrevRarity(string? rarity) => RarityHelper.Abbreviation(rarity);
}
