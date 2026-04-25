using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Items.Materials;
using SAOTRPG.Inventory.Core;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Split-pane inventory: equipment slots left, scrollable item list right.
// Enter on item → context-action popup (Use / Equip / Drop); arrows navigate, Tab switches panes.
public static class InventoryDialog
{
    private const int DialogWidth = 90, DialogHeight = 28;
    private const int LeftPaneWidth = 34;

    private enum SortMode { Default, ByType, ByRarity, ByName, ByValue }
    private static readonly string[] SortLabels = { "Default", "Type", "Rarity", "Name", "Value" };

    // Bundle 11 — category filter tabs. 1-5 cycles selection; All = default.
    private enum FilterMode { All, Weapons, Armor, Materials, Consumables }
    private static readonly string[] FilterLabels = { "All", "Weapons", "Armor", "Materials", "Consumables" };

    // Predicate per filter — keeps the type tests in one place so the tab
    // header text and the actual filter stay in sync.
    private static bool MatchesFilter(BaseItem item, FilterMode mode) => mode switch
    {
        FilterMode.All         => true,
        FilterMode.Weapons     => item is Weapon || item is Pickaxe,
        FilterMode.Armor       => item is Armor || item is Accessory,
        FilterMode.Materials   => item is Material,
        FilterMode.Consumables => item is Consumable,
        _ => true,
    };

    public static void Show(Player player, int floor = 1)
    {
        var currentSort = SortMode.Default;
        var currentFilter = FilterMode.All;
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

        // Bundle 11 — filter tab strip on row 1. Label rebuilt on every tab
        // switch so the active tab reads bright while the rest dim.
        var tabLabel = new Label
        {
            Text = BuildTabStrip(currentFilter),
            X = rightX, Y = 1, Width = Dim.Fill(1), ColorScheme = ColorSchemes.Body,
        };

        var itemNames = new ObservableCollection<string>();
        var itemRefs = new List<BaseItem>();

        var emptyLabel = new Label
        {
            Text = "  Your inventory is empty.",
            X = rightX, Y = 4, Width = Dim.Fill(), Height = 1,
            Visible = false, ColorScheme = ColorSchemes.Dim,
        };

        int rightWidth = DialogWidth - LeftPaneWidth - 4;
        void RefreshItemList()
        {
            itemNames.Clear();
            itemRefs.Clear();
            var filtered = player.Inventory.Items.Where(i => MatchesFilter(i, currentFilter));
            var sorted = currentSort switch
            {
                SortMode.ByType => filtered.OrderBy(i => i.GetType().Name).ThenBy(i => i.Name),
                SortMode.ByRarity => filtered.OrderByDescending(i => RarityHelper.SortOrder(i.Rarity)).ThenBy(i => i.Name),
                SortMode.ByName => filtered.OrderBy(i => i.Name),
                SortMode.ByValue => filtered.OrderByDescending(i => i.Value).ThenBy(i => i.Name),
                _ => filtered,
            };
            foreach (var item in sorted)
            {
                itemNames.Add(RarityHelper.FormatItemLine(item, rightWidth));
                itemRefs.Add(item);
            }
            string emptyMsg = currentFilter == FilterMode.All
                ? "  Your inventory is empty."
                : $"  No {FilterLabels[(int)currentFilter].ToLower()} in inventory.";
            emptyLabel.Text = emptyMsg;
            emptyLabel.Visible = itemNames.Count == 0;
        }
        RefreshItemList();

        var listView = new ListView
        {
            X = rightX, Y = 2, Width = Dim.Fill(1), Height = Dim.Fill(6),
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
            Text = "Enter: act  |  L: lore  |  1-5: filter  |  Shift+N: bind slot  |  Esc: close",
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

        // Preserve selected item ref across refilter/sort so the cursor doesn't
        // jump to row 0 every time. If the prior selection no longer matches
        // the filter, fall back to row 0.
        void RefreshAfterChange()
        {
            var preserved = GetSelectedItem();
            RefreshItemList();
            UpdateLabels();
            tabLabel.Text = BuildTabStrip(currentFilter);
            listView.Source = new ListWrapper<string>(itemNames);
            if (preserved != null)
            {
                int newIdx = itemRefs.IndexOf(preserved);
                if (newIdx >= 0) listView.SelectedItem = newIdx;
            }
            listView.SetNeedsDraw();
            slotView.SetNeedsDraw();
        }

        // ── Context action popup ── Enter on item → small popup with only applicable actions.
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

        // ── Event wiring ── Enter → context popup.
        // OpenSelectedItem is the Terminal.Gui 2.0.0 hook; migrate to Accepting past v2.2.
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
                // GearCompare returns a compact diff ("DMG +6  ATK +2  DEX -1") or mismatch banner.
                // Color inherits from the net verdict so upgrade/downgrade reads at a glance.
                compareLabel.Text = GearCompare.BuildDiffForPlayer(player, eqItem);
                var verdict = EquipmentComparer.GetVerdict(player, eqItem);
                compareLabel.ColorScheme = verdict switch
                {
                    EquipmentComparer.CompareResult.Upgrade => ColorSchemes.Success,
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

        // Bundle 11 — bare 1-5 cycle filter category. Shift+1-5 stays as
        // quickbar-bind (next handler), so we must reject any modifier here.
        listView.KeyDown += (s, e) =>
        {
            if (e.IsShift || (e.KeyCode & KeyCode.CtrlMask) != 0
                || (e.KeyCode & KeyCode.AltMask) != 0) return;
            FilterMode? newFilter = e.KeyCode switch
            {
                KeyCode.D1 => FilterMode.Weapons,
                KeyCode.D2 => FilterMode.Armor,
                KeyCode.D3 => FilterMode.Materials,
                KeyCode.D4 => FilterMode.Consumables,
                KeyCode.D5 => FilterMode.All,
                _ => null,
            };
            if (newFilter == null) return;
            currentFilter = newFilter.Value;
            RefreshAfterChange();
            e.Handled = true;
        };

        // 'L' = Lore: open canon citation popup for selected item. Bundle 11.
        // Listed before the Shift+N handler so unmodified L is captured first.
        listView.KeyDown += (s, e) =>
        {
            var bare = e.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;
            if (bare != KeyCode.L || e.IsShift || (e.KeyCode & KeyCode.CtrlMask) != 0) return;
            var sel = GetSelectedItem();
            if (sel == null) return;
            CanonInspectPopup.Show(sel);
            e.Handled = true;
        };

        // Shift+N on the item list binds the selected consumable to quickbar
        // slot N (Shift+0 = slot 10). Non-consumable selections are ignored.
        listView.KeyDown += (s, e) =>
        {
            if (!e.IsShift) return;
            int slot = e.KeyCode switch
            {
                KeyCode.D1 => 1, KeyCode.D2 => 2, KeyCode.D3 => 3,
                KeyCode.D4 => 4, KeyCode.D5 => 5, KeyCode.D6 => 6,
                KeyCode.D7 => 7, KeyCode.D8 => 8, KeyCode.D9 => 9,
                KeyCode.D0 => 10,
                _ => 0,
            };
            if (slot == 0) return;
            var selected = GetSelectedItem();
            if (selected is not Consumable cons || string.IsNullOrEmpty(cons.DefinitionId))
            {
                detailLabel.Text = "Only consumables with a definition can be bound.";
                detailLabel.ColorScheme = ColorSchemes.Danger;
                e.Handled = true;
                return;
            }
            player.Quickbar.Bind(slot - 1, cons.DefinitionId);
            detailLabel.Text = $"Bound {cons.Name} to quickbar slot {(slot == 10 ? "0" : slot.ToString())}.";
            detailLabel.ColorScheme = ColorSchemes.Success;
            e.Handled = true;
        };

        // ── Assemble ─────────────────────────────────────────────────
        dialog.Add(equipHeader, slotView, gearLabel,
            itemHeader, sortLabel, tabLabel, listView, emptyLabel,
            detailLabel, compareLabel, sortBtn, sellLabel, hintLabel);

        var closeBtn = DialogHelper.AddCloseFooter(dialog);
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.X = Pos.Right(sortBtn) + 2;

        // Focus the item list on open so arrow keys work immediately.
        listView.SetFocus();

        DialogHelper.RunModal(dialog);
    }

    // Bundle 11 — render the 5-tab filter strip. Active tab uppercase + bracketed,
    // others dimmed by surrounding spaces. Single Label so we don't fight focus.
    private static string BuildTabStrip(FilterMode active)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < FilterLabels.Length; i++)
        {
            bool isActive = i == (int)active;
            string label = FilterLabels[i];
            sb.Append(isActive ? $"[{i + 1} {label}]" : $" {i + 1} {label} ");
            if (i < FilterLabels.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }
}
