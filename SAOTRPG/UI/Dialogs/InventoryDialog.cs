using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Core;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Split-pane inventory dialog — equipment slots on left, scrollable item
// list on right. Press Enter on a selected item to open a context-action
// popup (Use / Equip / Drop). Arrow keys navigate; Tab switches panes.
public static class InventoryDialog
{
    private const int DialogWidth = 90, DialogHeight = 28;
    private const int LeftPaneWidth = 34;

    private enum SortMode { Default, ByType, ByRarity, ByName, ByValue }
    private static readonly string[] SortLabels = { "Default", "Type", "Rarity", "Name", "Value" };

    public static void Show(Player player, int floor = 1)
    {
        var currentSort = SortMode.Default;
        var dialog = DialogHelper.Create("Inventory", DialogWidth, DialogHeight);

        // ── Left pane: equipped gear ─────────────────────────────────
        var equipHeader = new Label
        {
            Text = "[ Equipped ]", X = 1, Y = 0,
            Width = LeftPaneWidth, ColorScheme = ColorSchemes.Gold,
        };
        var slotView = new EquipmentSlotView(player, LeftPaneWidth - 2)
        {
            X = 1, Y = 1, Width = LeftPaneWidth - 2, Height = EquipmentSlotView.SlotCount,
        };
        var gearLabel = new Label
        {
            Text = EquipmentDialog.BuildStatTotals(player),
            X = 1, Y = EquipmentSlotView.SlotCount + 2,
            Width = LeftPaneWidth, ColorScheme = ColorSchemes.Gold,
        };

        // ── Right pane: item list ────────────────────────────────────
        int rightX = LeftPaneWidth + 1;
        var itemHeader = new Label
        {
            Text = $"[ Items ({player.Inventory.ItemCount}/{player.Inventory.MaxSlots}) ]",
            X = rightX, Y = 0, Width = Dim.Fill(1), ColorScheme = ColorSchemes.Gold,
        };
        var sortLabel = new Label
        {
            Text = $"[Sort: {SortLabels[0]}]",
            X = Pos.AnchorEnd(18), Y = 0, Width = 17, ColorScheme = ColorSchemes.Dim,
        };

        var itemNames = new ObservableCollection<string>();
        var itemRefs = new List<BaseItem>();

        var emptyLabel = new Label
        {
            Text = "  Your inventory is empty.",
            X = rightX, Y = 3, Width = Dim.Fill(), Height = 1,
            Visible = false, ColorScheme = ColorSchemes.Dim,
        };

        int rightWidth = DialogWidth - LeftPaneWidth - 4;
        void RefreshItemList()
        {
            itemNames.Clear();
            itemRefs.Clear();
            var sorted = currentSort switch
            {
                SortMode.ByType => player.Inventory.Items.OrderBy(i => i.GetType().Name).ThenBy(i => i.Name),
                SortMode.ByRarity => player.Inventory.Items.OrderByDescending(i => RarityHelper.SortOrder(i.Rarity)).ThenBy(i => i.Name),
                SortMode.ByName => player.Inventory.Items.OrderBy(i => i.Name),
                SortMode.ByValue => player.Inventory.Items.OrderByDescending(i => i.Value).ThenBy(i => i.Name),
                _ => player.Inventory.Items.AsEnumerable(),
            };
            foreach (var item in sorted)
            {
                itemNames.Add(RarityHelper.FormatItemLine(item, rightWidth));
                itemRefs.Add(item);
            }
            emptyLabel.Visible = itemNames.Count == 0;
        }
        RefreshItemList();

        var listView = new ListView
        {
            X = rightX, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(5),
            Source = new ListWrapper<string>(itemNames),
            CanFocus = true,
        };

        // ── Detail + compare row ─────────────────────────────────────
        var detailLabel = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(1), Height = 1,
        };
        var compareLabel = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(1), Height = 1,
        };

        // ── Footer ───────────────────────────────────────────────────
        // Action buttons removed — replaced by Enter-to-act popup.
        var sortBtn = DialogHelper.CreateButton("Sort");
        sortBtn.X = 1; sortBtn.Y = Pos.AnchorEnd(2);

        var hintLabel = new Label
        {
            Text = "Enter: act on item  |  S: sort  |  Tab: switch pane  |  Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        var sellLabel = new Label
        {
            Text = "", X = Pos.Right(sortBtn) + 2, Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        // ── Refresh helpers ──────────────────────────────────────────
        void UpdateLabels()
        {
            int count = player.Inventory.ItemCount;
            int max = player.Inventory.MaxSlots;
            bool nearFull = count >= (int)(max * 0.75);
            string fullTag = nearFull ? " [NEARLY FULL]" : "";
            itemHeader.Text = $"[ Items ({count}/{max}){fullTag} ]";
            itemHeader.ColorScheme = nearFull ? ColorSchemes.Danger : ColorSchemes.Gold;
            sortLabel.Text = $"[Sort: {SortLabels[(int)currentSort]}]";
            sortLabel.ColorScheme = currentSort != SortMode.Default ? ColorSchemes.Gold : ColorSchemes.Dim;
            int total = player.Inventory.Items.Sum(i => i.Value);
            sellLabel.Text = $"Total value: {total} Col";
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

        // ── Context action popup — the "select then act" pattern ─────
        // When the user presses Enter on a selected item, a small popup
        // shows only the actions that apply to that item type.
        void ActOnSelectedItem()
        {
            var item = GetSelectedItem();
            if (item == null) return;

            string name = TextHelpers.Truncate(item.Name ?? "", 30);

            if (item is Consumable consumable)
            {
                int choice = MessageBox.Query(name, $"What do you want to do?", "Use", "Drop", "Cancel");
                if (choice == 0) { player.UseItem(consumable); detailLabel.Text = $"Used {name}."; }
                else if (choice == 1 && DialogHelper.ConfirmAction("Drop", name)) player.Inventory.RemoveItem(item);
                else return;
            }
            else if (item is EquipmentBase equipment)
            {
                int choice = MessageBox.Query(name, $"What do you want to do?", "Equip", "Drop", "Cancel");
                if (choice == 0) { player.EquipItem(equipment); detailLabel.Text = $"Equipped {name}."; }
                else if (choice == 1 && DialogHelper.ConfirmAction("Drop", name)) player.Inventory.RemoveItem(item);
                else return;
            }
            else
            {
                int choice = MessageBox.Query(name, $"What do you want to do?", "Drop", "Cancel");
                if (choice == 0 && DialogHelper.ConfirmAction("Drop", name)) player.Inventory.RemoveItem(item);
                else return;
            }
            RefreshAfterChange();
        }

        // ── Event wiring ─────────────────────────────────────────────
        // Enter on a list item → open the context action popup.
        // Note: OpenSelectedItem is correct for Terminal.Gui 2.0.0.
        // If upgrading past v2.2, migrate to the Accepting event.
        listView.OpenSelectedItem += (s, e) =>
        {
            // Defer so the Enter keypress that triggered OpenSelectedItem
            // doesn't propagate into the MessageBox and auto-select.
            Application.Invoke(() => ActOnSelectedItem());
        };

        // Detail label updates as the user browses.
        listView.SelectedItemChanged += (s, e) =>
        {
            var item = GetSelectedItem();
            if (item == null) { detailLabel.Text = ""; compareLabel.Text = ""; return; }

            int sellVal = ShopDialog.CalcSellPrice(item, floor);
            string rarityTag = item.Rarity != null && item.Rarity != "Common" ? $"[{item.Rarity}] " : "";
            detailLabel.Text = item switch
            {
                Consumable c => $"{rarityTag}{c.EffectDescription ?? "Consumable"} | Sell: {sellVal}c",
                EquipmentBase eq => $"{rarityTag}Lv.{eq.RequiredLevel} {eq.EquipmentType} | Sell: {sellVal}c",
                _ => $"{rarityTag}Value: {item.Value} Col | Sell: {sellVal}c",
            };
            detailLabel.ColorScheme = ColorSchemes.FromColor(RarityHelper.GetColor(item.Rarity));

            if (item is EquipmentBase eqItem)
            {
                compareLabel.Text = EquipmentComparer.BuildComparison(player, eqItem);
                var verdict = EquipmentComparer.GetVerdict(player, eqItem);
                compareLabel.ColorScheme = verdict switch
                {
                    EquipmentComparer.CompareResult.Upgrade => ColorSchemes.Gold,
                    EquipmentComparer.CompareResult.Downgrade => ColorSchemes.Danger,
                    _ => ColorSchemes.Dim,
                };
            }
            else { compareLabel.Text = ""; compareLabel.ColorScheme = ColorSchemes.Dim; }
        };

        // Equipment slot selection shows detail for the equipped item.
        slotView.SelectedSlotChanged += (idx) =>
        {
            var slots = EquipmentSlotView.DefaultSlotLayout;
            if (idx < 0 || idx >= slots.Length) { detailLabel.Text = ""; compareLabel.Text = ""; return; }
            var eq = player.Inventory.GetEquipped(slots[idx].Slot);
            detailLabel.Text = eq != null ? EquipmentDialog.BuildItemDetail(eq) : "No item equipped in this slot.";
            compareLabel.Text = "";
        };

        sortBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            currentSort = (SortMode)(((int)currentSort + 1) % SortLabels.Length);
            RefreshAfterChange();
        };

        // ── Assemble ─────────────────────────────────────────────────
        dialog.Add(equipHeader, slotView, gearLabel,
            itemHeader, sortLabel, listView, emptyLabel,
            detailLabel, compareLabel, sortBtn, sellLabel, hintLabel);

        var closeBtn = DialogHelper.AddCloseFooter(dialog);
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.X = Pos.Right(sortBtn) + 2;

        // Focus the item list on open so arrow keys work immediately.
        listView.SetFocus();

        DialogHelper.RunModal(dialog);
    }
}
