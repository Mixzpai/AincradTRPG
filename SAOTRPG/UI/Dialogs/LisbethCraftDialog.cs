using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Materials;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Lisbeth R6 crafting (F48 Lindarth). 18 canon HF recipes, availability status, consumes mats + 3M Col.
// Not once-per-run — the canon cost self-gates.
public static class LisbethCraftDialog
{
    private const int DialogWidth = 86, DialogHeight = 26;

    public static void Show(Player player, IGameLog log)
    {
        var dialog = DialogHelper.Create(" Lisbeth — Lindarth Forge (Rarity 6) ", DialogWidth, DialogHeight);

        // Lisbeth portrait anchored top-left — pink/magenta hair marker.
        var lisbethPortrait = AsciiPortraits.Get("lisbeth");
        if (lisbethPortrait.Length > 0)
        {
            var portraitLabel = new Label
            {
                Text = string.Join("\n", lisbethPortrait),
                X = 0, Y = 0, Width = 9, Height = lisbethPortrait.Length,
                ColorScheme = ColorSchemes.FromColor(Terminal.Gui.Color.BrightMagenta),
            };
            dialog.Add(portraitLabel);
        }

        var header = new Label
        {
            Text = $"Col: {player.ColOnHand}    Inventory: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}",
            X = 10, Y = 0, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Gold,
        };

        var subheader = new Label
        {
            Text = "Rarity 6 Lisbeth crafts — 3,000,000 Col + rare materials per weapon.",
            X = 2, Y = 1, Width = Dim.Fill(2), ColorScheme = ColorSchemes.Dim,
        };

        var names = new ObservableCollection<string>();
        var wrapper = new ListWrapper<string>(names);
        var listView = new ListView
        {
            X = 1, Y = 3, Width = Dim.Fill(1), Height = Dim.Fill(7),
            Source = wrapper,
        };

        var detailLabel = new Label
        {
            Text = "Select a weapon to see its full recipe.",
            X = 1, Y = Pos.AnchorEnd(6), Width = Dim.Fill(1), Height = 3,
            ColorScheme = ColorSchemes.Body,
        };

        void RefreshList()
        {
            names.Clear();
            foreach (var recipe in LisbethRecipes.All)
            {
                string status = GetStatus(player, recipe);
                names.Add($"  {recipe.DisplayName,-34}  {status}");
            }
            header.Text = $"Col: {player.ColOnHand}    Inventory: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
            listView.SetNeedsDraw();
        }

        listView.SelectedItemChanged += (s, e) =>
        {
            int idx = listView.SelectedItem;
            if (idx < 0 || idx >= LisbethRecipes.All.Length)
            {
                detailLabel.Text = "";
                return;
            }
            var recipe = LisbethRecipes.All[idx];
            detailLabel.Text = BuildRecipeDetail(player, recipe);
        };

        var craftBtn = DialogHelper.CreateButton("Craft", isDefault: true);
        craftBtn.X = Pos.Center() - 12;
        craftBtn.Y = Pos.AnchorEnd(2);
        craftBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int idx = listView.SelectedItem;
            if (idx < 0 || idx >= LisbethRecipes.All.Length) return;
            var recipe = LisbethRecipes.All[idx];
            TryCraft(player, recipe, log, detailLabel, RefreshList);
        };

        var closeBtn = DialogHelper.CreateButton("Leave");
        closeBtn.X = Pos.Center() + 3;
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        var escHint = new Label
        {
            Text = "[Esc] Leave",
            X = Pos.AnchorEnd(14), Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(header, subheader, listView, detailLabel, craftBtn, closeBtn, escHint);
        DialogHelper.CloseOnEscape(dialog);
        RefreshList();
        listView.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    private static string GetStatus(Player player, LisbethRecipes.Recipe recipe)
    {
        if (player.ColOnHand < recipe.ColCost) return $"NEED {recipe.ColCost:N0} Col";
        foreach (var mat in recipe.Materials)
        {
            int have = CountMaterial(player, mat.DefId);
            if (have < mat.Qty)
            {
                string name = ItemRegistry.Create(mat.DefId)?.Name ?? mat.DefId;
                return $"NEED {mat.Qty}x {name} ({have} owned)";
            }
        }
        return "AVAILABLE";
    }

    private static string BuildRecipeDetail(Player player, LisbethRecipes.Recipe recipe)
    {
        var parts = new List<string>
        {
            $"{recipe.DisplayName} — {recipe.ColCost:N0} Col",
        };
        var matLines = new List<string>();
        foreach (var mat in recipe.Materials)
        {
            int have = CountMaterial(player, mat.DefId);
            string name = ItemRegistry.Create(mat.DefId)?.Name ?? mat.DefId;
            matLines.Add($"{mat.Qty}x {name} ({have})");
        }
        parts.Add("Materials: " + string.Join(", ", matLines));
        return string.Join("\n", parts);
    }

    private static void TryCraft(Player player, LisbethRecipes.Recipe recipe, IGameLog log,
        Label detailLabel, Action refresh)
    {
        // Col check
        if (player.ColOnHand < recipe.ColCost)
        {
            detailLabel.Text = $"Not enough Col! Need {recipe.ColCost:N0}, have {player.ColOnHand:N0}.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        // Material check
        foreach (var mat in recipe.Materials)
        {
            int have = CountMaterial(player, mat.DefId);
            if (have < mat.Qty)
            {
                string name = ItemRegistry.Create(mat.DefId)?.Name ?? mat.DefId;
                detailLabel.Text = $"Missing {mat.Qty - have}x {name}.";
                detailLabel.ColorScheme = ColorSchemes.Danger;
                return;
            }
        }

        // Inventory space check — craft fails loud if full.
        var crafted = ItemRegistry.Create(recipe.WeaponDefId);
        if (crafted == null)
        {
            detailLabel.Text = $"Internal error: unknown DefId '{recipe.WeaponDefId}'.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        int confirm = MessageBox.Query(
            "Confirm Craft",
            $"Lisbeth will craft {recipe.DisplayName} for {recipe.ColCost:N0} Col and your listed materials.\n\n" +
            "Confirm?",
            "Craft", "Cancel");
        if (confirm != 0) return;

        // Consume materials + Col
        foreach (var mat in recipe.Materials)
            ConsumeMaterial(player, mat.DefId, mat.Qty);
        player.ColOnHand -= recipe.ColCost;

        // Add weapon — drop at player's feet if inventory full
        bool added = player.Inventory.AddItem(crafted);
        if (added)
        {
            log.LogLoot($"  ◈ Lisbeth crafts {crafted.Name} for you! (-{recipe.ColCost:N0} Col)");
            detailLabel.Text = $"◈ {crafted.Name} crafted! The forge rings with its birth.";
            detailLabel.ColorScheme = ColorSchemes.Gold;
        }
        else
        {
            log.LogLoot($"  ◈ Lisbeth crafts {crafted.Name} — inventory full, she sets it aside for you.");
            detailLabel.Text = $"◈ {crafted.Name} crafted, but your pack is full. " +
                "Make room and talk to Lisbeth to take it.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            // No pickup-later persistence, so refund materials if AddItem fails (fail-loud path).
            foreach (var mat in recipe.Materials)
            {
                var matItem = ItemRegistry.Create(mat.DefId);
                if (matItem is Material m)
                {
                    m.Quantity = mat.Qty;
                    player.Inventory.AddItem(m);
                }
            }
            player.ColOnHand += recipe.ColCost;
            detailLabel.Text = "Inventory full! Craft aborted — materials and Col refunded.";
        }
        refresh();
    }

    private static int CountMaterial(Player player, string defId)
    {
        int total = 0;
        foreach (var item in player.Inventory.Items)
        {
            if (item.DefinitionId != defId) continue;
            if (item is StackableItem s) total += s.Quantity;
            else total += 1;
        }
        return total;
    }

    private static void ConsumeMaterial(Player player, string defId, int count)
    {
        int remaining = count;
        var toRemove = new List<BaseItem>();
        foreach (var item in player.Inventory.Items.ToList())
        {
            if (remaining <= 0) break;
            if (item.DefinitionId != defId) continue;
            if (item is StackableItem s)
            {
                if (s.Quantity <= remaining) { remaining -= s.Quantity; toRemove.Add(item); }
                else { s.Quantity -= remaining; remaining = 0; }
            }
            else
            {
                toRemove.Add(item);
                remaining -= 1;
            }
        }
        foreach (var item in toRemove)
            player.Inventory.RemoveItem(item);
    }
}
