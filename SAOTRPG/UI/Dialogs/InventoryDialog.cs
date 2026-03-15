using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Core;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Split-pane inventory dialog — equipment slots on the left, scrollable
/// item list on the right. Accessible via I key or the Inventory button.
/// </summary>
public static class InventoryDialog
{
    // ── Layout constants ─────────────────────────────────────────────
    private const int DialogWidth  = 80;
    private const int DialogHeight = 24;
    private const int LeftPaneWidth = 32; // equipment pane width

    // ── Sort modes ────────────────────────────────────────────────────
    private enum SortMode { Default, ByType, ByRarity, ByName, ByValue }
    private static readonly string[] SortLabels = { "Default", "Type", "Rarity", "Name", "Value" };

    public static void Show(Player player)
    {
        var currentSort = SortMode.Default;

        var dialog = new Dialog
        {
            Title = "Inventory",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        // ════════════════════════════════════════════════════════════════
        //  LEFT PANE — Equipment slots
        // ════════════════════════════════════════════════════════════════

        var equipFrame = new FrameView
        {
            Title = "Equipment",
            X = 0, Y = 0,
            Width = LeftPaneWidth,
            Height = EquipmentSlotView.SlotCount + 2, // +2 for frame border
            ColorScheme = ColorSchemes.Dialog
        };

        var slotView = new EquipmentSlotView(player, LeftPaneWidth - 2)
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = EquipmentSlotView.SlotCount
        };
        equipFrame.Add(slotView);

        // Gear stat totals below equipment frame
        var gearLabel = new Label
        {
            Text = EquipmentDialog.BuildStatTotals(player),
            X = 0, Y = EquipmentSlotView.SlotCount + 2,
            Width = LeftPaneWidth,
            ColorScheme = ColorSchemes.Gold
        };

        // ════════════════════════════════════════════════════════════════
        //  RIGHT PANE — Inventory items (scrollable)
        // ════════════════════════════════════════════════════════════════

        var itemNames = new ObservableCollection<string>();
        var itemRefs = new List<BaseItem>();

        var itemHeader = new Label
        {
            Text = $"Items ({player.Inventory.ItemCount}/{player.Inventory.MaxSlots})  [Sort: Default]",
            X = LeftPaneWidth + 1, Y = 0,
            Width = Dim.Fill()
        };

        void RefreshItemList()
        {
            itemNames.Clear();
            itemRefs.Clear();

            var sorted = currentSort switch
            {
                SortMode.ByType   => player.Inventory.Items.OrderBy(i => i.GetType().Name).ThenBy(i => i.Name),
                SortMode.ByRarity => player.Inventory.Items.OrderByDescending(i => RarityOrder(i.Rarity)).ThenBy(i => i.Name),
                SortMode.ByName   => player.Inventory.Items.OrderBy(i => i.Name),
                SortMode.ByValue  => player.Inventory.Items.OrderByDescending(i => i.Value).ThenBy(i => i.Name),
                _                 => player.Inventory.Items.AsEnumerable()
            };

            // Right pane content width for padding
            int rightWidth = DialogWidth - LeftPaneWidth - 5;

            foreach (var item in sorted)
            {
                string qty = item is StackableItem s ? $" x{s.Quantity}" : "";
                string rarityTag = FormatRarityTag(item.Rarity);
                string line = $" {rarityTag}{item.Name}{qty} ({item.Rarity})";
                // Pad to uniform length (prevents Terminal.Gui render crash)
                itemNames.Add(line.Length > rightWidth ? line[..rightWidth] : line.PadRight(rightWidth));
                itemRefs.Add(item);
            }
        }

        RefreshItemList();

        var listView = new ListView
        {
            X = LeftPaneWidth + 1, Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(5),
            Source = new ListWrapper<string>(itemNames)
        };

        // ════════════════════════════════════════════════════════════════
        //  BOTTOM AREA — shared detail, comparison, buttons, footer
        // ════════════════════════════════════════════════════════════════

        var detailLabel = new Label
        {
            Text = "",
            X = 0, Y = Pos.AnchorEnd(5),
            Width = Dim.Fill(),
            Height = 1
        };

        var compareLabel = new Label
        {
            Text = "",
            X = 0, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(),
            Height = 1
        };

        var useBtn   = new Button { Text = " Use ",   X = 0,                       Y = Pos.AnchorEnd(3), ColorScheme = ColorSchemes.Button };
        var equipBtn = new Button { Text = " Equip ", X = Pos.Right(useBtn) + 1,   Y = Pos.AnchorEnd(3), ColorScheme = ColorSchemes.Button };
        var dropBtn  = new Button { Text = " Drop ",  X = Pos.Right(equipBtn) + 1, Y = Pos.AnchorEnd(3), ColorScheme = ColorSchemes.Button };
        var sortBtn  = new Button { Text = " Sort ",  X = Pos.Right(dropBtn) + 1,  Y = Pos.AnchorEnd(3), ColorScheme = ColorSchemes.Button };
        var closeBtn = new Button { Text = " Close ", X = Pos.Right(sortBtn) + 1,  Y = Pos.AnchorEnd(3), IsDefault = true, ColorScheme = ColorSchemes.Button };

        // Sell value + weight footer
        var sellLabel = new Label
        {
            Text = "",
            X = 0, Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            ColorScheme = ColorSchemes.Dim
        };

        // Pane indicator
        var paneHint = new Label
        {
            Text = "Tab: switch pane",
            X = 0, Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            ColorScheme = ColorSchemes.Dim
        };

        // ── Helpers ─────────────────────────────────────────────────────

        void UpdateLabels()
        {
            itemHeader.Text = $"Items ({player.Inventory.ItemCount}/{player.Inventory.MaxSlots})  [Sort: {SortLabels[(int)currentSort]}]";
            int total = player.Inventory.Items.Sum(i => i.Value);
            int totalWeight = player.Inventory.Items.Sum(i => i.Weight);
            sellLabel.Text = $"Total sell value: {total} Col  |  Weight: {totalWeight}";
            gearLabel.Text = EquipmentDialog.BuildStatTotals(player);
        }

        UpdateLabels();

        BaseItem? GetSelectedItem()
        {
            int idx = listView.SelectedItem;
            return idx >= 0 && idx < itemRefs.Count ? itemRefs[idx] : null;
        }

        void RefreshAfterChange()
        {
            RefreshItemList();
            UpdateLabels();
            listView.Source = new ListWrapper<string>(itemNames);
            listView.SetNeedsDraw();
            slotView.SetNeedsDraw();
        }

        // ── Selection events ────────────────────────────────────────────

        // Inventory list selection — show item detail + comparison
        listView.SelectedItemChanged += (s, e) =>
        {
            var item = GetSelectedItem();
            if (item == null) { detailLabel.Text = ""; compareLabel.Text = ""; return; }

            detailLabel.Text = item switch
            {
                Consumable c   => $"{c.EffectDescription ?? "Consumable"} | Value: {c.Value} Col",
                EquipmentBase eq => $"Lv.{eq.RequiredLevel} {eq.EquipmentType} | Value: {eq.Value} Col",
                _              => $"Value: {item.Value} Col"
            };

            if (item is EquipmentBase eqItem)
                compareLabel.Text = EquipmentComparer.BuildComparison(player, eqItem);
            else
                compareLabel.Text = "";
        };

        // Equipment slot selection — show equipped item detail
        slotView.SelectedSlotChanged += (idx) =>
        {
            var slots = EquipmentSlotView.DefaultSlotLayout;
            if (idx < 0 || idx >= slots.Length) { detailLabel.Text = ""; compareLabel.Text = ""; return; }
            var eq = player.Inventory.GetEquipped(slots[idx].Slot);
            if (eq != null)
            {
                detailLabel.Text = EquipmentDialog.BuildItemDetail(eq);
                compareLabel.Text = "";
            }
            else
            {
                detailLabel.Text = "No item equipped in this slot.";
                compareLabel.Text = "";
            }
        };

        // ── Button actions ──────────────────────────────────────────────

        useBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (GetSelectedItem() is Consumable consumable)
            {
                player.UseItem(consumable);
                RefreshAfterChange();
            }
        };

        equipBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (GetSelectedItem() is EquipmentBase equipment)
            {
                player.EquipItem(equipment);
                RefreshAfterChange();
            }
        };

        dropBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var item = GetSelectedItem();
            if (item != null)
            {
                player.Inventory.RemoveItem(item);
                RefreshAfterChange();
            }
        };

        sortBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            currentSort = (SortMode)(((int)currentSort + 1) % SortLabels.Length);
            RefreshAfterChange();
        };

        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // ── Assemble ────────────────────────────────────────────────────
        dialog.Add(equipFrame, gearLabel, itemHeader, listView,
            detailLabel, compareLabel,
            useBtn, equipBtn, dropBtn, sortBtn, closeBtn,
            sellLabel, paneHint);
        Application.Run(dialog);
        dialog.Dispose();
    }

    // ── Rarity tag prefixes ──────────────────────────────────────────
    private static string FormatRarityTag(string rarity) => rarity switch
    {
        "Epic"     => "[E] ",
        "Rare"     => "[R] ",
        "Uncommon" => "[U] ",
        _          => "    "
    };

    // ── Rarity sort order (higher = rarer) ────────────────────────
    private static int RarityOrder(string? rarity) => rarity switch
    {
        "Epic"     => 4,
        "Rare"     => 3,
        "Uncommon" => 2,
        "Common"   => 1,
        _          => 0
    };
}
