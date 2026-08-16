using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Vendor shop dialog — buy, sell, and repair items.
public static class ShopDialog
{
    private const int DialogWidth = 64, DialogHeight = 22;
    private const int CostPerDurability = 2, BaseDurability = 50, DurabilityPerLevel = 10;

    // Opens the shop dialog — buy, sell, and repair items from a vendor.
    public static void Show(Player player, Vendor vendor, int currentFloor = 1,
        SAOTRPG.UI.IGameLog? log = null)
    {
        // Karma: Outlaw-tier players are refused service outright.
        // Honorable / Shady tiers apply a ±10% markup via BuyPrice() below.
        float karmaMul = KarmaSystem.ShopPriceMultiplier(player.Karma);
        if (karmaMul < 0)
        {
            DialogHelper.Query(vendor.ShopName ?? "Shop",
                $"{vendor.Name ?? "The shopkeep"} will not serve an outlaw.\n\n" +
                $"Your karma is {player.Karma} ({KarmaSystem.TierLabel(player.Karma)}).\n" +
                "Come back when the world sees you differently.",
                "Leave");
            return;
        }
        // Bargaining stacks multiplicatively on karma multiplier (L99 × Honorable = 0.90×0.85 = 0.765).
        float bargainMul = LifeSkillSystem.BargainingDiscount(player);
        // Beater's second half. The modifier applied its faction-rep hit at run start and logged
        // "Shop prices hiked", but nothing here consulted it and rep does not feed pricing — so
        // the +25% the selection screen advertises was never charged. Stacks like bargainMul.
        float beaterMul = RunModifiers.IsActive(RunModifier.Beater) ? 1.25f : 1f;
        int BuyPrice(BaseItem it) =>
            Math.Max(1, (int)Math.Round(it.Value * karmaMul * bargainMul * beaterMul));

        // Dynamic tiering: fold newly-unlocked items (dedup by DefId). NEW-to-this-visit set
        // lets the render loop flag them; MarkSeen clears the flag next visit.
        var newFlags = new HashSet<string>();
        var existingDefIds = new HashSet<string>(
            vendor.ShopStock
                .Where(i => !string.IsNullOrEmpty(i.DefinitionId))
                .Select(i => i.DefinitionId!));
        if (ShopTierSystem.HighestFloorBossCleared >= 50)
        {
            foreach (var item in ShopTierSystem.BuildTierStock())
            {
                if (!string.IsNullOrEmpty(item.DefinitionId)
                    && !existingDefIds.Contains(item.DefinitionId))
                {
                    vendor.ShopStock.Add(item);
                    existingDefIds.Add(item.DefinitionId);
                    if (ShopTierSystem.IsNew(item.DefinitionId))
                        newFlags.Add(item.DefinitionId);
                }
            }
            // Clear the NEW flags so the badge only shows once.
            foreach (var defId in newFlags) ShopTierSystem.MarkSeen(defId);
        }

        // Per-vendor invested stock — tiers beyond the global band, scoped to THIS vendor.
        foreach (var item in VendorInvestmentSystem.BuildVendorExtraStock(vendor))
        {
            if (!string.IsNullOrEmpty(item.DefinitionId)
                && !existingDefIds.Contains(item.DefinitionId))
            {
                vendor.ShopStock.Add(item);
                existingDefIds.Add(item.DefinitionId);
            }
        }

        var dialog = DialogHelper.Create(vendor.ShopName ?? "Shop", DialogWidth, DialogHeight);

        // Vendor portrait (Agil / Lisbeth / etc.) — anchored top-left, non-interactive.
        string? vendorPortraitKey = AsciiPortraits.KeyForName(vendor.Name);
        string[] vendorPortrait = vendorPortraitKey != null ? AsciiPortraits.Get(vendorPortraitKey) : Array.Empty<string>();
        if (vendorPortrait.Length > 0)
        {
            var portraitLabel = new Label
            {
                Text = string.Join("\n", vendorPortrait),
                X = 0, Y = 0, Width = 9, Height = vendorPortrait.Length,
            }.WithScheme(ColorSchemes.FromColor(vendor.SymbolColor));
            dialog.Add(portraitLabel);
        }

        var colLabel = new Label { Text = $"Your Col: {player.ColOnHand}", X = Pos.Center(), Y = 0 };
        string tierInfo = ShopTierSystem.HighestFloorBossCleared >= 50
            ? $"   Tier {ShopTierSystem.CurrentTierCount()}/{ShopTierSystem.TotalTiers} unlocked"
            : "";
        int investTiers = VendorInvestmentSystem.GetInvestedTiers(vendor);
        string investInfo = investTiers > 0 ? $"   Invested +{investTiers}" : "";
        var modeHeader = new Label { Text = $"[ For Sale ]{tierInfo}{investInfo}", X = Pos.Center(), Y = 1 };
        var emptyLabel = new Label
        {
            Text = "", X = Pos.Center(), Y = 6,
            Width = Dim.Auto(), Height = 1, Visible = false,
            SchemeName = ColorSchemes.DimName
        };

        var buyNames = new ObservableCollection<string>();
        var buyRefs = new List<BaseItem>();

        void RefreshBuyList()
        {
            buyNames.Clear();
            buyRefs.Clear();
            foreach (var item in vendor.ShopStock)
            {
                string tag = RarityHelper.FormatTag(item.Rarity);
                string newTag = (!string.IsNullOrEmpty(item.DefinitionId)
                                 && newFlags.Contains(item.DefinitionId!))
                    ? "[NEW] " : "";
                int price = BuyPrice(item);
                string karmaMark = karmaMul > 1.01f ? " (+karma markup)"
                                 : karmaMul < 0.99f ? " (-karma discount)" : "";
                buyNames.Add($"  {newTag}{tag}{item.Name} — {price} Col{karmaMark}  ({FormatItemInfo(item)})");
                buyRefs.Add(item);
            }
            colLabel.Text = $"Your Col: {player.ColOnHand}";
        }

        RefreshBuyList();
        emptyLabel.Text = "No items for sale.";
        emptyLabel.Visible = buyNames.Count == 0;

        var buyWrapper = new ListWrapper<string>(buyNames);
        var listView = new ListView
        {
            X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(5),
            Source = buyWrapper
        };

        var detailLabel = new Label
        {
            Text = "Browse wares or press Sell to sell your items.",
            X = 0, Y = Pos.AnchorEnd(5), Width = Dim.Fill(), Height = 1
        };
        var compareLabel = new Label
        {
            Text = "", X = 0, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(), Height = 1, SchemeName = ColorSchemes.DimName
        };

        var buyBtn = DialogHelper.CreateButton("Buy");
        var sellBtn = DialogHelper.CreateButton("Sell");
        var junkBtn = DialogHelper.CreateButton("Sell Junk");
        var repairBtn = DialogHelper.CreateButton("Repair");
        var investBtn = DialogHelper.CreateButton("Invest");
        var closeBtn = DialogHelper.CreateButton("Leave", isDefault: true);

        // Flat, not nested. These six sat inside a CanFocus=false container, which Terminal.Gui
        // refuses to traverse — twelve Tab presses never left the list, so every shop action was
        // unreachable by keyboard. Pos.Align centres them as a group with no width to maintain.
        foreach (var b in new[] { buyBtn, sellBtn, junkBtn, repairBtn, investBtn, closeBtn })
        {
            b.X = Pos.Align(Alignment.Center,
                AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems);
            b.Y = Pos.AnchorEnd(3);
        }

        var sellHeader = new Label { Text = "", X = Pos.Center(), Y = Pos.AnchorEnd(2) };
        bool sellMode = false;
        var sellNames = new ObservableCollection<string>();
        var sellRefs = new List<BaseItem>();
        var sellWrapper = new ListWrapper<string>(sellNames);

        void RefreshSellList()
        {
            sellNames.Clear();
            sellRefs.Clear();
            foreach (var item in player.Inventory.Items)
            {
                int sellPrice = CalcSellPrice(item, currentFloor, player);
                string tag = RarityHelper.FormatTag(item.Rarity);
                string qty = item is StackableItem s ? $" x{s.Quantity}" : "";
                sellNames.Add($"  {tag}{item.Name}{qty} — sells for {sellPrice} Col");
                sellRefs.Add(item);
            }
        }

        void SwitchToBuy()
        {
            sellMode = false;
            modeHeader.Text = "[ For Sale ]";
            sellHeader.Text = "";
            RefreshBuyList();
            listView.Source = buyWrapper;
            listView.SelectedItem = 0;
            listView.SetNeedsDraw();
            emptyLabel.Text = "No items for sale.";
            emptyLabel.Visible = buyNames.Count == 0;
        }

        void SwitchToSell()
        {
            sellMode = true;
            modeHeader.Text = "[ Your Items -- Sell ]";
            sellHeader.Text = $"Items: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
            compareLabel.Text = "";
            RefreshSellList();
            listView.Source = sellWrapper;
            listView.SelectedItem = 0;
            listView.SetNeedsDraw();
            emptyLabel.Text = "Your inventory is empty.";
            emptyLabel.Visible = sellNames.Count == 0;
        }

        listView.ValueChanged += (s, e) =>
        {
            int idx = listView.SelectedItem ?? -1;
            detailLabel.SchemeName = ColorSchemes.BodyName;

            if (!sellMode && idx >= 0 && idx < buyRefs.Count)
            {
                var item = buyRefs[idx];
                int price = BuyPrice(item);
                bool canAfford = player.ColOnHand >= price;
                detailLabel.Text = canAfford
                    ? $"Press Buy to purchase for {price} Col."
                    : "Not enough Col!";
                compareLabel.Text = BuildComparison(item, player);
                if (item is EquipmentBase eqItem)
                {
                    var verdict = EquipmentComparer.GetVerdict(player, eqItem);
                    compareLabel.SchemeName = verdict switch
                    {
                        EquipmentComparer.CompareResult.Upgrade => ColorSchemes.GoldName,
                        EquipmentComparer.CompareResult.Downgrade => ColorSchemes.DangerName,
                        _ => ColorSchemes.DimName
                    };
                }
            }
            else if (sellMode && idx >= 0 && idx < sellRefs.Count)
            {
                detailLabel.Text = $"Press Sell to sell for {CalcSellPrice(sellRefs[idx], currentFloor, player)} Col.";
                compareLabel.Text = "";
                compareLabel.SchemeName = ColorSchemes.DimName;
            }
            else
            {
                detailLabel.Text = "";
                compareLabel.Text = "";
                compareLabel.SchemeName = ColorSchemes.DimName;
            }
        };

        buyBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            if (sellMode) { SwitchToBuy(); return; }
            int idx = listView.SelectedItem ?? -1;
            if (idx < 0 || idx >= buyRefs.Count) return;

            var item = buyRefs[idx];
            int price = BuyPrice(item);
            if (player.ColOnHand < price)
            {
                detailLabel.Text = "Not enough Col!";
                detailLabel.SchemeName = ColorSchemes.DangerName;
                return;
            }

            BaseItem bought = CloneShopItem(item);
            if (!player.Inventory.AddItem(bought))
            {
                detailLabel.Text = "Inventory full!";
                detailLabel.SchemeName = ColorSchemes.DangerName;
                return;
            }

            player.ColOnHand -= price;
            // Bargaining XP: +1 per shop transaction (buy).
            player.LifeSkills.GrantXp(LifeSkillType.Bargaining, 1);
            detailLabel.Text = $"Purchased {item.Name} for {price} Col.";
            detailLabel.SchemeName = ColorSchemes.SuccessName;
            colLabel.Text = $"Your Col: {player.ColOnHand}";
        };

        sellBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            if (!sellMode) { SwitchToSell(); return; }
            int idx = listView.SelectedItem ?? -1;
            if (idx < 0 || idx >= sellRefs.Count) return;

            var item = sellRefs[idx];
            int sellPrice = CalcSellPrice(item, currentFloor, player);

            string qtyTag = item is StackableItem st ? $" x{st.Quantity}" : "";
            bool isRarePlus = item.Rarity != null && item.Rarity != "Common";
            if (item.Value >= 100 || item is StackableItem { Quantity: > 1 } || isRarePlus)
            {
                string rarityWarn = isRarePlus ? $" [{item.Rarity}]" : "";
                int confirm = DialogHelper.Query("Sell", $"Sell{rarityWarn} {item.Name}{qtyTag} for {sellPrice} Col?", "Yes", "No");
                if (confirm != 0) return;
            }

            player.Inventory.RemoveItem(item);
            player.ColOnHand += sellPrice;
            // Bargaining XP: +1 per shop transaction (sell).
            player.LifeSkills.GrantXp(LifeSkillType.Bargaining, 1);
            detailLabel.Text = $"Sold {item.Name} for {sellPrice} Col.";
            detailLabel.SchemeName = ColorSchemes.SuccessName;
            colLabel.Text = $"Your Col: {player.ColOnHand}";
            sellHeader.Text = $"Items: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
            RefreshSellList();
            listView.Source = sellWrapper;
            listView.SetNeedsDraw();
            emptyLabel.Visible = sellNames.Count == 0;
        };

        repairBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            var repairSlots = new[] {
                EquipmentSlot.Weapon, EquipmentSlot.Chest, EquipmentSlot.Head,
                EquipmentSlot.Feet, EquipmentSlot.OffHand
            };

            int totalCost = 0, itemsToRepair = 0;
            foreach (var slot in repairSlots)
            {
                var eq = player.Inventory.GetEquipped(slot);
                if (eq == null) continue;
                int maxDur = BaseDurability + eq.RequiredLevel * DurabilityPerLevel;
                if (eq.ItemDurability >= maxDur) continue;
                totalCost += (maxDur - eq.ItemDurability) * CostPerDurability;
                itemsToRepair++;
            }

            if (itemsToRepair == 0) { detailLabel.Text = "Nothing to repair — all gear is in good shape!"; return; }

            var breakdown = new System.Text.StringBuilder();
            foreach (var slot in repairSlots)
            {
                var eq = player.Inventory.GetEquipped(slot);
                if (eq == null) continue;
                int maxDur = BaseDurability + eq.RequiredLevel * DurabilityPerLevel;
                if (eq.ItemDurability >= maxDur) continue;
                breakdown.AppendLine($"  {eq.Name}: {eq.ItemDurability}/{maxDur} (+{maxDur - eq.ItemDurability})");
            }

            int confirm = DialogHelper.Query("Repair All",
                $"{breakdown}Total: {totalCost} Col for {itemsToRepair} item(s)\nYour Col: {player.ColOnHand}",
                "Repair", "Cancel");
            if (confirm != 0) return;

            bool repaired = false;
            foreach (var slot in repairSlots)
            {
                var eq = player.Inventory.GetEquipped(slot);
                if (eq == null) continue;
                int maxDur = BaseDurability + eq.RequiredLevel * DurabilityPerLevel;
                if (eq.ItemDurability >= maxDur) continue;
                int cost = (maxDur - eq.ItemDurability) * CostPerDurability;
                if (player.ColOnHand < cost)
                {
                    detailLabel.Text = $"Not enough Col to repair {eq.Name}! ({cost} Col needed)";
                    continue;
                }
                player.ColOnHand -= cost;
                eq.ItemDurability = maxDur;
                repaired = true;
            }
            detailLabel.Text = repaired ? $"Repaired all gear for {totalCost} Col!" : "Not enough Col to repair anything!";
            detailLabel.SchemeName = repaired ? ColorSchemes.SuccessName : ColorSchemes.DangerName;
            colLabel.Text = $"Your Col: {player.ColOnHand}";
        };

        junkBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            var junkItems = player.Inventory.Items.Where(i => i.Rarity == "Common").ToList();
            if (junkItems.Count == 0)
            {
                detailLabel.Text = "No Common items to sell!";
                detailLabel.SchemeName = ColorSchemes.DimName;
                return;
            }

            int totalCol = junkItems.Sum(i => CalcSellPrice(i, currentFloor, player));
            int confirm = DialogHelper.Query("Sell Junk",
                $"Sell {junkItems.Count} Common item(s) for {totalCol} Col?", "Sell All", "Cancel");
            if (confirm != 0) return;

            foreach (var item in junkItems) player.Inventory.RemoveItem(item);
            player.ColOnHand += totalCol;
            // Bargaining XP: +1 per bulk-junk transaction.
            player.LifeSkills.GrantXp(LifeSkillType.Bargaining, 1);
            detailLabel.Text = $"Sold {junkItems.Count} junk item(s) for {totalCol} Col!";
            detailLabel.SchemeName = ColorSchemes.SuccessName;
            colLabel.Text = $"Your Col: {player.ColOnHand}";

            if (sellMode)
            {
                sellHeader.Text = $"Items: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
                RefreshSellList();
                listView.Source = sellWrapper;
                listView.SetNeedsDraw();
                emptyLabel.Visible = sellNames.Count == 0;
            }
        };

        // Invest prompt: preset Col tiers, clamped by ColOnHand and 20,000 per-vendor cap.
        // Tier-up + running total piped into the game log.
        investBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            string shop = vendor.ShopName ?? "Shop";
            int current = VendorInvestmentSystem.GetInvested(vendor);
            int tiers = VendorInvestmentSystem.GetInvestedTiers(vendor);
            int nextCost = VendorInvestmentSystem.NextTierCost(vendor);
            string nextLine = nextCost > 0
                ? $"Next tier at {current + nextCost} Col invested (need {nextCost} more)."
                : "Maxed — 20,000 Col invested, +3 stock tiers unlocked.";
            int choice = DialogHelper.Query("Invest in Shop",
                $"Deposit Col at {shop}.\n\n" +
                $"Invested: {current}/{VendorInvestmentSystem.MaxInvestmentPerVendor} Col\n" +
                $"Bonus stock tiers: +{tiers}\n" +
                $"{nextLine}\n\n" +
                $"Your Col on hand: {player.ColOnHand}\n" +
                "Choose deposit amount:",
                "500", "1000", "5000", "20000", "Cancel");

            int[] amounts = { 500, 1000, 5000, 20000 };
            if (choice < 0 || choice >= amounts.Length) return;
            int deposit = amounts[choice];
            int actual = VendorInvestmentSystem.Invest(vendor, player, deposit, log);
            if (actual <= 0)
            {
                detailLabel.Text = "Investment failed — not enough Col or vendor at cap.";
                detailLabel.SchemeName = ColorSchemes.DangerName;
                return;
            }

            int newTiers = VendorInvestmentSystem.GetInvestedTiers(vendor);
            string tierMsg = newTiers > tiers ? $" (+{newTiers - tiers} new stock tier!)" : "";
            detailLabel.Text = $"Invested {actual} Col at {shop}.{tierMsg}";
            detailLabel.SchemeName = ColorSchemes.SuccessName;
            colLabel.Text = $"Your Col: {player.ColOnHand}";
            // Update header readout so the +Invested badge refreshes live.
            investTiers = VendorInvestmentSystem.GetInvestedTiers(vendor);
            string newInvestInfo = investTiers > 0 ? $"   Invested +{investTiers}" : "";
            modeHeader.Text = sellMode
                ? "[ Your Items -- Sell ]"
                : $"[ For Sale ]{tierInfo}{newInvestInfo}";
        };

        closeBtn.Accepting += (s, e) => { AppHost.App.RequestStop(); e.Handled = true; };

        var hintLabel = new Label
        {
            Text = "Enter: buy/sell | Tab: switch | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), SchemeName = ColorSchemes.DimName,
        };

        // Enter is Command.Accept. Unclaimed it bubbles to the dialog's default button — the
        // "Leave" button — so the advertised "Enter: buy/sell" shut the shop instead. Routing it
        // to whichever button owns the current mode keeps one copy of the buy/sell logic.
        listView.Accepting += (s, e) =>
        {
            e.Handled = true;
            (sellMode ? sellBtn : buyBtn).InvokeCommand(Command.Accept);
        };

        dialog.Add(colLabel, modeHeader, listView, emptyLabel, detailLabel, compareLabel,
                   buyBtn, sellBtn, junkBtn, repairBtn, investBtn, closeBtn,
                   sellHeader, hintLabel);
        DialogHelper.CloseOnEscape(dialog);
        DialogHelper.SelectFirstRow(listView);
        DialogHelper.RunModal(dialog);
    }

    private static string BuildComparison(BaseItem shopItem, Player player) =>
        shopItem is not EquipmentBase eq ? "" : GearCompare.BuildDiffForPlayer(player, eq);

    // Sell price w/ floor bonus. With `player`: symmetric karma mult (Honorable +10%, Shady -10%).
    // Outlaw (karma ≤ -50) already blocked at entry.
    public static int CalcSellPrice(BaseItem item, int floor = 1, Player? player = null)
    {
        int basePrice = Math.Max(1, item.Value / 2);
        int floorBonus = basePrice * Math.Max(0, floor - 1) * 5 / 100;
        int raw = basePrice + floorBonus;
        if (player == null) return raw;
        float buyMul = KarmaSystem.ShopPriceMultiplier(player.Karma);
        if (buyMul < 0) return raw;  // Outlaw — caller already blocked entry
        float sellMul = 2f - buyMul;
        // Bargaining sell = 2 − buyMul (e.g. 0.85 → 1.15, +15% at L99 cap); stacks × karma sell mult.
        float bargainBuy = LifeSkillSystem.BargainingDiscount(player);
        float sellBargain = 2f - bargainBuy;
        return Math.Max(1, (int)Math.Round(raw * sellMul * sellBargain));
    }

    private static string FormatItemInfo(BaseItem item) => item switch
    {
        Armor { ArmorSlot: "Shield" } sh => $"Lv.{sh.RequiredLevel} Shield {sh.BlockChance}% block",
        EquipmentBase eq => $"Lv.{eq.RequiredLevel} {eq.EquipmentType}",
        Consumable c => c.EffectDescription ?? "Consumable",
        _ => ""
    };

    // Shallow clone via MemberwiseClone reflection; Bonuses/Effects stay aliased to template.
    // Quantity resets to 1 for stackable consumables so the buyer gets a single unit.
    private static readonly System.Reflection.MethodInfo _memberwiseClone =
        typeof(object).GetMethod("MemberwiseClone",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    private static BaseItem CloneShopItem(BaseItem item)
    {
        var clone = (BaseItem)_memberwiseClone.Invoke(item, null)!;
        if (clone is StackableItem s) s.Quantity = 1;
        return clone;
    }
}
