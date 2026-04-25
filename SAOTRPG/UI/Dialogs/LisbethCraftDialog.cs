using System.Collections.ObjectModel;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Items.Materials;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Lisbeth R6 crafting (F48 Lindarth). 18 canon HF recipes + B12 C2 iron-ingot enhance lane.
// F1/F2 swap section. R6 = 3M Col + rare mats; Iron Ingot Enhance = 200 Col + 3 iron_ingot per +1.
public static class LisbethCraftDialog
{
    private const int DialogWidth = 86, DialogHeight = 26;

    // Bundle 13 (Items 4a/4c/4d) — three new tabs sit alongside R6 + Iron.
    // F1 R6Craft / F2 Iron / F3 Mithril / F4 Reforge / F5 Crystallite.
    private enum Mode { R6Craft, IronIngotEnhance, MithrilEnhance, Reforge, CrystalliteEnhance }

    public static void Show(Player player, IGameLog log)
    {
        var dialog = DialogHelper.Create(" Lisbeth — Lindarth Forge ", DialogWidth, DialogHeight);

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

        var mode = Mode.R6Craft;

        var header = new Label
        {
            Text = "",
            X = 10, Y = 0, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Gold,
        };

        // Tab strip: highlights active section (F1 R6 | F2 Iron Ingot Enhance).
        var tabLabel = new Label
        {
            Text = "",
            X = 10, Y = 1, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Body,
        };

        var subheader = new Label
        {
            Text = "",
            X = 2, Y = 2, Width = Dim.Fill(2), ColorScheme = ColorSchemes.Dim,
        };

        var names = new ObservableCollection<string>();
        var wrapper = new ListWrapper<string>(names);
        var listView = new ListView
        {
            X = 1, Y = 4, Width = Dim.Fill(1), Height = Dim.Fill(7),
            Source = wrapper,
        };

        var detailLabel = new Label
        {
            Text = "Select an entry to see its details.",
            X = 1, Y = Pos.AnchorEnd(6), Width = Dim.Fill(1), Height = 3,
            ColorScheme = ColorSchemes.Body,
        };

        // Cached enhance candidate snapshot — refreshed per RefreshList in enhance/reforge modes.
        List<Weapon> enhanceCandidates = new();
        List<Weapon> reforgeCandidates = new();

        void RefreshHeader()
        {
            header.Text = $"Col: {player.ColOnHand}    Inventory: {player.Inventory.ItemCount}/{player.Inventory.MaxSlots}";
            string Mark(string label, Mode m) => mode == m ? $"[*{label}*]" : $" {label} ";
            tabLabel.Text =
                $"F1 {Mark("R6", Mode.R6Craft)}  " +
                $"F2 {Mark("Iron", Mode.IronIngotEnhance)}  " +
                $"F3 {Mark("Mithril", Mode.MithrilEnhance)}  " +
                $"F4 {Mark("Reforge", Mode.Reforge)}  " +
                $"F5 {Mark("Crystallite", Mode.CrystalliteEnhance)}";
        }

        void RefreshList()
        {
            names.Clear();
            switch (mode)
            {
                case Mode.R6Craft:
                {
                    subheader.Text = "Rarity 6 Lisbeth crafts — 3,000,000 Col + rare materials per weapon.";
                    foreach (var recipe in LisbethRecipes.All)
                    {
                        string status = GetStatus(player, recipe);
                        names.Add($"  {recipe.DisplayName,-34}  {status}");
                    }
                    break;
                }
                case Mode.IronIngotEnhance:
                {
                    var ir = LisbethRecipes.LowTierEnhanceRecipes[0];
                    subheader.Text = $"Iron Ingot Enhance — {ir.ColCost} Col + 3x Iron Ingot per +1 (cap +{ir.MaxEnhancementCap}, Common/Uncommon).";
                    enhanceCandidates = GatherEnhanceCandidatesForRarities(player, "Common", "Uncommon");
                    PrintEnhanceCandidates(names, enhanceCandidates, ir, "Common/Uncommon");
                    break;
                }
                case Mode.MithrilEnhance:
                {
                    var mr = LisbethRecipes.MidTierEnhanceRecipes[0];
                    subheader.Text = $"Mithril Ingot Enhance — {mr.ColCost} Col + 3x Mithril Ingot per +1 (cap +{mr.MaxEnhancementCap}, Rare/Epic).";
                    enhanceCandidates = GatherEnhanceCandidatesForRarities(player, "Rare", "Epic");
                    PrintEnhanceCandidates(names, enhanceCandidates, mr, "Rare/Epic");
                    break;
                }
                case Mode.CrystalliteEnhance:
                {
                    var cr = LisbethRecipes.HighTierEnhanceRecipes[0];
                    subheader.Text = $"Crystallite Ingot Enhance — {cr.ColCost} Col + 3x Crystallite Ingot per +1 (cap +{cr.MaxEnhancementCap}, Epic/Legendary).";
                    enhanceCandidates = GatherEnhanceCandidatesForRarities(player, "Epic", "Legendary");
                    PrintEnhanceCandidates(names, enhanceCandidates, cr, "Epic/Legendary");
                    break;
                }
                case Mode.Reforge:
                {
                    subheader.Text = "Reforge — re-roll random Bonuses within rarity tier. Divine and LAB-sealed weapons rejected.";
                    reforgeCandidates = GatherReforgeCandidates(player);
                    if (reforgeCandidates.Count == 0)
                    {
                        names.Add("  (no eligible weapons in inventory)");
                    }
                    else
                    {
                        foreach (var w in reforgeCandidates)
                        {
                            var cost = Reforge.GetCost(w);
                            string sealTag = !w.IsEnhanceable ? " [SEALED]" : "";
                            names.Add($"  {w.EnhancedName,-32}  [{w.Rarity}] {cost.ColCost:N0}c{sealTag}");
                        }
                    }
                    break;
                }
            }
            RefreshHeader();
            listView.SetNeedsDraw();
        }

        // Shared printer for the four enhance-mode lists. Caller passes the
        // recipe + a label like "Common/Uncommon" used in the empty-list message.
        static void PrintEnhanceCandidates(
            ObservableCollection<string> names,
            List<Weapon> candidates,
            LisbethRecipes.LowTierEnhanceRecipe recipe,
            string rarityLabel)
        {
            if (candidates.Count == 0)
            {
                names.Add($"  (no {rarityLabel} weapons in inventory)");
                return;
            }
            foreach (var w in candidates)
            {
                string capTag = w.EnhancementLevel >= recipe.MaxEnhancementCap ? " [CAP]" : "";
                string sealTag = !w.IsEnhanceable ? " [SEALED]" : "";
                names.Add($"  {w.EnhancedName,-32}  [{w.Rarity}]{capTag}{sealTag}");
            }
        }

        listView.SelectedItemChanged += (s, e) =>
        {
            int idx = listView.SelectedItem;
            switch (mode)
            {
                case Mode.R6Craft:
                    if (idx < 0 || idx >= LisbethRecipes.All.Length) { detailLabel.Text = ""; return; }
                    detailLabel.Text = BuildRecipeDetail(player, LisbethRecipes.All[idx]);
                    detailLabel.ColorScheme = ColorSchemes.Body;
                    break;
                case Mode.IronIngotEnhance:
                case Mode.MithrilEnhance:
                case Mode.CrystalliteEnhance:
                {
                    if (idx < 0 || idx >= enhanceCandidates.Count) { detailLabel.Text = ""; return; }
                    var rec = ActiveEnhanceRecipe(mode);
                    detailLabel.Text = BuildEnhanceDetailGeneric(player, enhanceCandidates[idx], rec);
                    detailLabel.ColorScheme = ColorSchemes.Body;
                    break;
                }
                case Mode.Reforge:
                {
                    if (idx < 0 || idx >= reforgeCandidates.Count) { detailLabel.Text = ""; return; }
                    detailLabel.Text = BuildReforgeDetail(player, reforgeCandidates[idx]);
                    detailLabel.ColorScheme = ColorSchemes.Body;
                    break;
                }
            }
        };

        var craftBtn = DialogHelper.CreateButton("Confirm", isDefault: true);
        craftBtn.X = Pos.Center() - 12;
        craftBtn.Y = Pos.AnchorEnd(2);
        craftBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int idx = listView.SelectedItem;
            switch (mode)
            {
                case Mode.R6Craft:
                    if (idx < 0 || idx >= LisbethRecipes.All.Length) return;
                    TryCraft(player, LisbethRecipes.All[idx], log, detailLabel, RefreshList);
                    break;
                case Mode.IronIngotEnhance:
                    if (idx < 0 || idx >= enhanceCandidates.Count) return;
                    TryIronIngotEnhance(player, enhanceCandidates[idx], log, detailLabel, RefreshList);
                    break;
                case Mode.MithrilEnhance:
                    if (idx < 0 || idx >= enhanceCandidates.Count) return;
                    TryGenericEnhance(player, enhanceCandidates[idx],
                        LisbethRecipes.MidTierEnhanceRecipes[0], "mithril_ingot", "Mithril Ingot",
                        log, detailLabel, RefreshList);
                    break;
                case Mode.CrystalliteEnhance:
                    if (idx < 0 || idx >= enhanceCandidates.Count) return;
                    TryGenericEnhance(player, enhanceCandidates[idx],
                        LisbethRecipes.HighTierEnhanceRecipes[0], "crystallite_ingot", "Crystallite Ingot",
                        log, detailLabel, RefreshList);
                    break;
                case Mode.Reforge:
                    if (idx < 0 || idx >= reforgeCandidates.Count) return;
                    TryReforge(player, reforgeCandidates[idx], log, detailLabel, RefreshList);
                    break;
            }
        };

        var closeBtn = DialogHelper.CreateButton("Leave");
        closeBtn.X = Pos.Center() + 3;
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        var escHint = new Label
        {
            Text = "[F1-F5] Switch  [Esc] Leave",
            X = Pos.AnchorEnd(28), Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Dim,
        };

        // F1-F5 toggle modes — capture at dialog level so list focus doesn't swallow.
        // Selection reset (D4) — heterogeneous lists across modes; out-of-range guards
        // exist in handlers but resetting to 0 prevents an obviously-wrong row stickying.
        dialog.KeyDown += (s, k) =>
        {
            Mode? target = k.KeyCode switch
            {
                KeyCode.F1 => Mode.R6Craft,
                KeyCode.F2 => Mode.IronIngotEnhance,
                KeyCode.F3 => Mode.MithrilEnhance,
                KeyCode.F4 => Mode.Reforge,
                KeyCode.F5 => Mode.CrystalliteEnhance,
                _          => null,
            };
            if (target == null || target == mode) return;
            mode = target.Value;
            RefreshList();
            listView.SelectedItem = 0;
            detailLabel.Text = mode switch
            {
                Mode.R6Craft           => "Select a recipe to see its full details.",
                Mode.Reforge           => "Select a weapon to preview a reforge roll.",
                _                       => "Select a weapon to see enhance status.",
            };
            k.Handled = true;
        };

        dialog.Add(header, tabLabel, subheader, listView, detailLabel, craftBtn, closeBtn, escHint);
        DialogHelper.CloseOnEscape(dialog);
        RefreshList();
        listView.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    // Common/Uncommon weapons in inventory (equipped or backpack), excluding sealed LAB weapons.
    private static List<Weapon> GatherEnhanceCandidates(Player player)
        => GatherEnhanceCandidatesForRarities(player, "Common", "Uncommon");

    // Bundle 13 — generic gather: any weapon whose rarity is in the allow set,
    // including equipped slots. Used by Iron/Mithril/Crystallite enhance tabs.
    private static List<Weapon> GatherEnhanceCandidatesForRarities(Player player, params string[] allowed)
    {
        var allow = new HashSet<string>(allowed);
        var seen = new HashSet<Weapon>();
        var result = new List<Weapon>();
        foreach (var item in player.Inventory.Items)
        {
            if (item is Weapon w && w.Rarity != null && allow.Contains(w.Rarity) && seen.Add(w))
                result.Add(w);
        }
        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.OffHand })
        {
            var eq = player.Inventory.GetEquipped(slot);
            if (eq is Weapon w && w.Rarity != null && allow.Contains(w.Rarity) && seen.Add(w))
                result.Add(w);
        }
        return result;
    }

    private static bool IsLowTierRarity(string? rarity) =>
        rarity == "Common" || rarity == "Uncommon";

    // Bundle 13 — Reforge candidates: every weapon (equipped + bag) where Reforge.IsEligible.
    private static List<Weapon> GatherReforgeCandidates(Player player)
    {
        var seen = new HashSet<Weapon>();
        var result = new List<Weapon>();
        foreach (var item in player.Inventory.Items)
        {
            if (item is Weapon w && Reforge.IsEligible(w, out _) && seen.Add(w))
                result.Add(w);
        }
        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.OffHand })
        {
            var eq = player.Inventory.GetEquipped(slot);
            if (eq is Weapon w && Reforge.IsEligible(w, out _) && seen.Add(w))
                result.Add(w);
        }
        return result;
    }

    private static LisbethRecipes.LowTierEnhanceRecipe ActiveEnhanceRecipe(Mode mode) => mode switch
    {
        Mode.IronIngotEnhance   => LisbethRecipes.LowTierEnhanceRecipes[0],
        Mode.MithrilEnhance     => LisbethRecipes.MidTierEnhanceRecipes[0],
        Mode.CrystalliteEnhance => LisbethRecipes.HighTierEnhanceRecipes[0],
        _                        => LisbethRecipes.LowTierEnhanceRecipes[0],
    };

    private static string BuildEnhanceDetailGeneric(Player player, Weapon weapon,
        LisbethRecipes.LowTierEnhanceRecipe recipe)
    {
        var matReq = recipe.Materials[0];
        int have = CountMaterial(player, matReq.DefId);
        string matName = ItemRegistry.Create(matReq.DefId)?.Name ?? matReq.DefId;
        var lines = new List<string>
        {
            $"{weapon.EnhancedName} ({weapon.Rarity}) — current +{weapon.EnhancementLevel}",
            $"Cost: {matReq.Qty}x {matName} ({have} owned), {recipe.ColCost:N0} Col → +{weapon.EnhancementLevel + 1}",
        };
        if (weapon.EnhancementLevel >= recipe.MaxEnhancementCap)
            lines.Add($"At Lisbeth cap (+{recipe.MaxEnhancementCap}) for this tier.");
        else if (!weapon.IsEnhanceable)
            lines.Add("[SEALED] Last-Attack-Bonus weapon — cannot be enhanced.");
        return string.Join("\n", lines);
    }

    private static string BuildReforgeDetail(Player player, Weapon weapon)
    {
        if (!Reforge.IsEligible(weapon, out var why))
            return $"{weapon.EnhancedName}\n{why}";
        var cost = Reforge.GetCost(weapon);
        var matReq = cost.Mats.Count > 0 ? cost.Mats[0] : null;
        int have = matReq != null ? CountMaterial(player, matReq.DefId) : 0;
        string matName = matReq != null ? (ItemRegistry.Create(matReq.DefId)?.Name ?? matReq.DefId) : "(none)";
        // Read-only preview; deterministic seed = HashCode of (weapon, EnhancementLevel) for cursor stability.
        int seed = HashCode.Combine(weapon.GetHashCode(), weapon.EnhancementLevel);
        var preview = Reforge.PeekRoll(weapon, seed);
        var lines = new List<string>
        {
            $"{weapon.EnhancedName} ({weapon.Rarity})",
            $"Cost: {cost.ColCost:N0} Col + {(matReq?.Qty ?? 0)}x {matName} ({have} owned)",
            $"Now : {preview.CurrentBonusesDescription}",
            $"Roll: {preview.PreviewBonusesDescription} [{(preview.IsUpgrade ? "preview+" : "preview-")}]",
        };
        return string.Join("\n", lines);
    }

    // Bundle 13 (Item 4d) — Reforge with explicit before/after confirmation modal (R8 mitigation).
    // Player MUST accept the new roll — never auto-applied because random rolls can wipe good ones.
    private static void TryReforge(Player player, Weapon weapon, IGameLog log,
        Label detailLabel, Action refresh)
    {
        if (!Reforge.IsEligible(weapon, out var reason))
        {
            detailLabel.Text = reason ?? "Not eligible.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        var cost = Reforge.GetCost(weapon);
        if (player.ColOnHand < cost.ColCost)
        {
            detailLabel.Text = $"Not enough Col! Need {cost.ColCost:N0}, have {player.ColOnHand:N0}.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        foreach (var mat in cost.Mats)
        {
            int have = CountMaterial(player, mat.DefId);
            if (have < mat.Qty)
            {
                string matName = ItemRegistry.Create(mat.DefId)?.Name ?? mat.DefId;
                detailLabel.Text = $"Missing {mat.Qty - have}x {matName}.";
                detailLabel.ColorScheme = ColorSchemes.Danger;
                return;
            }
        }

        // Two-step confirm — show before/after one more time so player explicitly accepts the random roll.
        int previewSeed = unchecked(System.Environment.TickCount + weapon.GetHashCode());
        var preview = Reforge.PeekRoll(weapon, previewSeed);
        string body =
            $"Reforge {weapon.EnhancedName} ({weapon.Rarity})?\n\n" +
            $"NOW : {preview.CurrentBonusesDescription}\n" +
            $"ROLL: {preview.PreviewBonusesDescription}\n\n" +
            $"Cost: {cost.ColCost:N0} Col + materials.\n" +
            $"NOTE: actual roll on apply is independent of this preview — accepts random outcome.";
        int confirm = MessageBox.Query("Confirm Reforge", body, "Reforge", "Cancel");
        if (confirm != 0) return;

        if (Reforge.Apply(weapon, player, log))
        {
            detailLabel.Text = $"◈ Reforged {weapon.EnhancedName}!";
            detailLabel.ColorScheme = ColorSchemes.Gold;
        }
        else
        {
            detailLabel.Text = "Reforge failed — see log.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
        }
        refresh();
    }

    // Generic enhance handler for Mithril (Rare/Epic) and Crystallite (Epic/Legendary) tiers.
    // Iron stays on TryIronIngotEnhance because it routes through CraftingDialog.ApplyLisbethIronIngotEnhance.
    private static void TryGenericEnhance(Player player, Weapon weapon,
        LisbethRecipes.LowTierEnhanceRecipe recipe, string matDefId, string matName,
        IGameLog log, Label detailLabel, Action refresh)
    {
        if (Array.IndexOf(recipe.AllowedRarities, weapon.Rarity) < 0)
        {
            detailLabel.Text = $"{weapon.Name}: only {string.Join("/", recipe.AllowedRarities)} weapons accept this tier.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (!weapon.IsEnhanceable)
        {
            detailLabel.Text = $"{weapon.EnhancedName} is sealed (LAB drop) — cannot be enhanced.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (weapon.EnhancementLevel >= recipe.MaxEnhancementCap)
        {
            detailLabel.Text = $"{weapon.EnhancedName} is at +{recipe.MaxEnhancementCap} cap for this tier.";
            detailLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }
        var matReq = recipe.Materials[0];
        int have = CountMaterial(player, matDefId);
        if (have < matReq.Qty)
        {
            detailLabel.Text = $"Missing {matReq.Qty - have}x {matName}.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (player.ColOnHand < recipe.ColCost)
        {
            detailLabel.Text = $"Not enough Col! Need {recipe.ColCost:N0}, have {player.ColOnHand:N0}.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        int confirm = MessageBox.Query("Confirm Enhance",
            $"Lisbeth will burn {matReq.Qty}x {matName} + {recipe.ColCost:N0} Col to push\n" +
            $"{weapon.EnhancedName} → +{weapon.EnhancementLevel + 1}.\n\nConfirm?",
            "Enhance", "Cancel");
        if (confirm != 0) return;

        ConsumeMaterial(player, matDefId, matReq.Qty);
        player.ColOnHand -= recipe.ColCost;
        // Same Anvil-style enhance flow as the Iron path — routes through CraftingDialog so
        // Bonuses+EnhancementLevel+Equip/Unequip+InvalidateStatCache stays in one code path.
        CraftingDialog.ApplyLisbethIronIngotEnhance(player, weapon);

        log.LogLoot($"  ◈ Lisbeth bumps {weapon.Name} to +{weapon.EnhancementLevel}! (-{matReq.Qty} {matName}, -{recipe.ColCost:N0} Col)");
        detailLabel.Text = $"◈ {weapon.EnhancedName} forged!";
        detailLabel.ColorScheme = ColorSchemes.Gold;
        refresh();
    }

    private static string BuildEnhanceDetail(Player player, Weapon weapon)
    {
        var recipe = LisbethRecipes.LowTierEnhanceRecipes[0];
        int haveIron = CountMaterial(player, "iron_ingot");
        var lines = new List<string>
        {
            $"{weapon.EnhancedName} ({weapon.Rarity}) — current +{weapon.EnhancementLevel}",
            $"Cost: 3x Iron Ingot ({haveIron} owned), {recipe.ColCost} Col → +{weapon.EnhancementLevel + 1}",
        };
        if (weapon.EnhancementLevel >= recipe.MaxEnhancementCap)
            lines.Add($"At Lisbeth cap (+{recipe.MaxEnhancementCap}). Push +6..+10 at Anvil with Enhancement Ores.");
        else if (!weapon.IsEnhanceable)
            lines.Add("[SEALED] Last-Attack-Bonus weapon — cannot be enhanced.");
        return string.Join("\n", lines);
    }

    // B12 C2 — order: rarity → seal → cap → mats → Col. Routes through CraftingDialog helper for R9 mitigation.
    private static void TryIronIngotEnhance(Player player, Weapon weapon, IGameLog log,
        Label detailLabel, Action refresh)
    {
        var recipe = LisbethRecipes.LowTierEnhanceRecipes[0];

        if (!IsLowTierRarity(weapon.Rarity))
        {
            detailLabel.Text = $"{weapon.Name}: only Common/Uncommon weapons accept iron-ingot enhance.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (!weapon.IsEnhanceable)
        {
            detailLabel.Text = $"{weapon.EnhancedName} is sealed (LAB drop) — cannot be enhanced.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (weapon.EnhancementLevel >= recipe.MaxEnhancementCap)
        {
            detailLabel.Text = $"{weapon.EnhancedName} is at Lisbeth's cap (+{recipe.MaxEnhancementCap}). " +
                "Use the Anvil to push +6..+10.";
            detailLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

        int haveIron = CountMaterial(player, "iron_ingot");
        var ironReq = recipe.Materials[0];
        if (haveIron < ironReq.Qty)
        {
            detailLabel.Text = $"Missing {ironReq.Qty - haveIron}x Iron Ingot.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (player.ColOnHand < recipe.ColCost)
        {
            detailLabel.Text = $"Not enough Col! Need {recipe.ColCost}, have {player.ColOnHand:N0}.";
            detailLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        int confirm = MessageBox.Query(
            "Confirm Enhance",
            $"Lisbeth will burn 3x Iron Ingot + {recipe.ColCost} Col to push\n" +
            $"{weapon.EnhancedName} → +{weapon.EnhancementLevel + 1}.\n\nConfirm?",
            "Enhance", "Cancel");
        if (confirm != 0) return;

        ConsumeMaterial(player, "iron_ingot", ironReq.Qty);
        player.ColOnHand -= recipe.ColCost;
        // R9 mitigation: routes through CraftingDialog.ApplyLisbethIronIngotEnhance — same Bonuses.Add +
        // EnhancementLevel + Equip/Unequip + InvalidateStatCache flow as the Anvil enhance path.
        CraftingDialog.ApplyLisbethIronIngotEnhance(player, weapon);

        log.LogLoot($"  ◈ Lisbeth bumps {weapon.Name} to +{weapon.EnhancementLevel}! (-3 Iron Ingot, -{recipe.ColCost} Col)");
        detailLabel.Text = $"◈ {weapon.EnhancedName} forged! Lisbeth wipes her brow.";
        detailLabel.ColorScheme = ColorSchemes.Gold;
        refresh();
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
