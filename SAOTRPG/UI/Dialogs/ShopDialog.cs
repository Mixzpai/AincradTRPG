using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Vendor shop dialog — buy/sell items.
/// Toggling the Buy/Sell buttons switches between vendor stock and player inventory.
/// </summary>
public static class ShopDialog
{
    // ── Layout constants ─────────────────────────────────────────────
    private const int DialogWidth  = 64;
    private const int DialogHeight = 22;

    public static void Show(Player player, Vendor vendor, int currentFloor = 1)
    {
        var dialog = new Dialog
        {
            Title = vendor.ShopName ?? "Shop",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        // ── Player currency display ──────────────────────────────────
        var colLabel = new Label
        {
            Text = $"Your Col: {player.ColOnHand}",
            X = 0, Y = 0
        };

        // ── Mode header ──────────────────────────────────────────────
        var buyHeader = new Label { Text = "── For Sale ──", X = 0, Y = 1 };

        // ── Buy-mode data ────────────────────────────────────────────
        var buyNames = new ObservableCollection<string>();
        var buyRefs  = new List<BaseItem>();

        void RefreshBuyList()
        {
            buyNames.Clear();
            buyRefs.Clear();
            foreach (var item in vendor.ShopStock)
            {
                string info = FormatItemInfo(item);
                buyNames.Add($"  {item.Name} — {item.Value} Col  ({info})");
                buyRefs.Add(item);
            }
            colLabel.Text = $"Your Col: {player.ColOnHand}";
        }

        RefreshBuyList();

        // ── Shared list view ─────────────────────────────────────────
        var listView = new ListView
        {
            X = 0, Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(5),
            Source = new ListWrapper<string>(buyNames)
        };

        // ── Detail/status line ───────────────────────────────────────
        var detailLabel = new Label
        {
            Text = "Browse wares or press Sell to sell your items.",
            X = 0, Y = Pos.AnchorEnd(5),
            Width = Dim.Fill(), Height = 1
        };

        // ── Action buttons ───────────────────────────────────────────
        var buyBtn    = new Button { Text = " Buy ",    X = 0,                       Y = Pos.AnchorEnd(4), ColorScheme = ColorSchemes.Button };
        var sellBtn   = new Button { Text = " Sell ",   X = Pos.Right(buyBtn) + 1,   Y = Pos.AnchorEnd(4), ColorScheme = ColorSchemes.Button };
        var repairBtn = new Button { Text = " Repair ", X = Pos.Right(sellBtn) + 1,  Y = Pos.AnchorEnd(4), ColorScheme = ColorSchemes.Button };
        var closeBtn  = new Button { Text = " Leave ",  X = Pos.Right(repairBtn) + 1, Y = Pos.AnchorEnd(4), IsDefault = true, ColorScheme = ColorSchemes.Button };

        // ── Sell-mode state ──────────────────────────────────────────
        var sellHeader = new Label { Text = "", X = 0, Y = Pos.AnchorEnd(3) };
        var sellInfo   = new Label { Text = "", X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill() };

        bool sellMode = false;
        var sellNames = new ObservableCollection<string>();
        var sellRefs  = new List<BaseItem>();

        void RefreshSellList()
        {
            sellNames.Clear();
            sellRefs.Clear();
            foreach (var item in player.Inventory.Items)
            {
                int sellPrice = CalcSellPrice(item, currentFloor);
                string qty = item is StackableItem s ? $" x{s.Quantity}" : "";
                sellNames.Add($"  {item.Name}{qty} — sells for {sellPrice} Col");
                sellRefs.Add(item);
            }
        }

        // ── Mode switches ────────────────────────────────────────────

        void SwitchToBuy()
        {
            sellMode = false;
            buyHeader.Text = "── For Sale ──";
            sellHeader.Text = "";
            RefreshBuyList();
            listView.Source = new ListWrapper<string>(buyNames);
            listView.SetNeedsDraw();
        }

        void SwitchToSell()
        {
            sellMode = true;
            buyHeader.Text = "── Your Items (Sell) ──";
            sellHeader.Text = $"Items: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
            RefreshSellList();
            listView.Source = new ListWrapper<string>(sellNames);
            listView.SetNeedsDraw();
        }

        // ── Selection change — update detail line ────────────────────
        listView.SelectedItemChanged += (s, e) =>
        {
            int idx = listView.SelectedItem;

            if (!sellMode && idx >= 0 && idx < buyRefs.Count)
            {
                bool canAfford = player.ColOnHand >= buyRefs[idx].Value;
                detailLabel.Text = canAfford
                    ? $"Press Buy to purchase for {buyRefs[idx].Value} Col."
                    : "Not enough Col!";
            }
            else if (sellMode && idx >= 0 && idx < sellRefs.Count)
            {
                detailLabel.Text = $"Press Sell to sell for {CalcSellPrice(sellRefs[idx], currentFloor)} Col.";
            }
            else
            {
                detailLabel.Text = "";
            }
        };

        // ── Buy handler ──────────────────────────────────────────────
        buyBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (sellMode) { SwitchToBuy(); return; }

            int idx = listView.SelectedItem;
            if (idx < 0 || idx >= buyRefs.Count) return;

            var item = buyRefs[idx];
            if (player.ColOnHand < item.Value)
            {
                detailLabel.Text = "Not enough Col!";
                return;
            }

            // Clone item so vendor stock stays intact
            BaseItem bought = CloneShopItem(item);
            if (!player.Inventory.AddItem(bought))
            {
                detailLabel.Text = "Inventory full!";
                return;
            }

            player.ColOnHand -= item.Value;
            detailLabel.Text = $"Purchased {item.Name} for {item.Value} Col.";
            colLabel.Text = $"Your Col: {player.ColOnHand}";
        };

        // ── Sell handler ─────────────────────────────────────────────
        sellBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (!sellMode) { SwitchToSell(); return; }

            int idx = listView.SelectedItem;
            if (idx < 0 || idx >= sellRefs.Count) return;

            var item = sellRefs[idx];
            int sellPrice = CalcSellPrice(item, currentFloor);
            player.Inventory.RemoveItem(item);
            player.ColOnHand += sellPrice;

            detailLabel.Text = $"Sold {item.Name} for {sellPrice} Col.";
            colLabel.Text = $"Your Col: {player.ColOnHand}";
            sellHeader.Text = $"Items: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
            RefreshSellList();
            listView.Source = new ListWrapper<string>(sellNames);
            listView.SetNeedsDraw();
        };

        // ── Repair handler — restore equipped weapon/armor durability for Col ──
        repairBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            // Find equipped items that need repair
            var repairSlots = new[] {
                SAOTRPG.Inventory.Core.EquipmentSlot.Weapon,
                SAOTRPG.Inventory.Core.EquipmentSlot.Chest,
                SAOTRPG.Inventory.Core.EquipmentSlot.Head,
                SAOTRPG.Inventory.Core.EquipmentSlot.Feet,
                SAOTRPG.Inventory.Core.EquipmentSlot.OffHand
            };
            bool repaired = false;
            foreach (var slot in repairSlots)
            {
                var eq = player.Inventory.GetEquipped(slot);
                if (eq == null) continue;
                // Max durability estimate: base 50 per item
                int maxDur = 50 + eq.RequiredLevel * 10;
                if (eq.ItemDurability >= maxDur) continue;
                int missing = maxDur - eq.ItemDurability;
                int cost = missing * 2;  // 2 Col per durability point
                if (player.ColOnHand < cost)
                {
                    detailLabel.Text = $"Not enough Col to repair {eq.Name}! ({cost} Col needed)";
                    continue;
                }
                player.ColOnHand -= cost;
                eq.ItemDurability = maxDur;
                detailLabel.Text = $"Repaired {eq.Name} to full durability for {cost} Col!";
                colLabel.Text = $"Your Col: {player.ColOnHand}";
                repaired = true;
            }
            if (!repaired)
                detailLabel.Text = "Nothing to repair — all gear is in good shape!";
        };

        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // ── Assemble ─────────────────────────────────────────────────
        dialog.Add(colLabel, buyHeader, listView, detailLabel, buyBtn, sellBtn, repairBtn, closeBtn, sellHeader, sellInfo);
        Application.Run(dialog);
        dialog.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Sell price = half buy price + 5% per floor above 1. Higher floor vendors pay more.
    /// </summary>
    private static int CalcSellPrice(BaseItem item, int floor = 1)
    {
        int basePrice = Math.Max(1, item.Value / 2);
        int floorBonus = basePrice * Math.Max(0, floor - 1) * 5 / 100;
        return basePrice + floorBonus;
    }

    /// <summary>Format the info tag shown next to item names in shop list.</summary>
    private static string FormatItemInfo(BaseItem item) => item switch
    {
        Armor { ArmorSlot: "Shield" } sh => $"Lv.{sh.RequiredLevel} Shield {sh.BlockChance}% block",
        EquipmentBase eq => $"Lv.{eq.RequiredLevel} {eq.EquipmentType}",
        Consumable c     => c.EffectDescription ?? "Consumable",
        _                => ""
    };

    /// <summary>
    /// Creates a fresh copy of a shop item so multiple purchases don't share refs.
    /// Each item type needs its own clone path to copy type-specific properties.
    /// </summary>
    private static BaseItem CloneShopItem(BaseItem item)
    {
        if (item is Weapon w)
            return new Weapon
            {
                Name = w.Name, Value = w.Value, Rarity = w.Rarity,
                ItemDurability = w.ItemDurability, RequiredLevel = w.RequiredLevel,
                EquipmentType = w.EquipmentType, WeaponType = w.WeaponType,
                BaseDamage = w.BaseDamage, AttackSpeed = w.AttackSpeed,
                Range = w.Range, Bonuses = w.Bonuses
            };

        if (item is Armor a)
            return new Armor
            {
                Name = a.Name, Value = a.Value, Rarity = a.Rarity,
                ItemDurability = a.ItemDurability, RequiredLevel = a.RequiredLevel,
                EquipmentType = a.EquipmentType, ArmorSlot = a.ArmorSlot,
                BaseDefense = a.BaseDefense, Weight = a.Weight,
                BlockChance = a.BlockChance, Bonuses = a.Bonuses
            };

        if (item is Potion p)
            return new Potion
            {
                Name = p.Name, Value = p.Value, Rarity = p.Rarity,
                Quantity = 1, MaxStacks = p.MaxStacks,
                ConsumableType = p.ConsumableType, PotionType = p.PotionType,
                Cooldown = p.Cooldown, EffectDescription = p.EffectDescription,
                Effects = p.Effects
            };

        if (item is Food f)
            return new Food
            {
                Name = f.Name, Value = f.Value, Rarity = f.Rarity,
                Quantity = 1, MaxStacks = f.MaxStacks,
                ConsumableType = f.ConsumableType, FoodType = f.FoodType,
                RegenerationRate = f.RegenerationRate,
                RegenerationDuration = f.RegenerationDuration,
                EffectDescription = f.EffectDescription
            };

        // Fallback — shouldn't hit this, but safe
        return item;
    }
}
