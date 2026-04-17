using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Items.Materials;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Anvil crafting dialog -- Repair equipment, Enhance it (+1 to +10),
// or Evolve a chain weapon to its next tier (Priority 5 Phase B).
// Enhancement costs Col + materials and has a success rate that drops
// at higher levels, following SAO canon mechanics. Evolution consumes
// chain-specific catalysts and swaps the equipped weapon for its apex tier.
public static class CraftingDialog
{
    private const int DialogWidth = 70, DialogHeight = 22;
    private const int MaxEnhancement = 10;

    // Col cost per enhancement level: scales steeply.
    private static int EnhanceCost(int currentLevel, int floor) =>
        (currentLevel + 1) * 100 + floor * 50;

    // Materials needed per enhancement level.
    private static int MaterialsNeeded(int currentLevel) =>
        1 + currentLevel / 3;

    // Success rate: high at low levels, drops after +4 (SAO canon).
    private static int SuccessRate(int currentLevel) => currentLevel switch
    {
        0 => 95,  // +0 -> +1
        1 => 90,  // +1 -> +2
        2 => 85,  // +2 -> +3
        3 => 75,  // +3 -> +4
        4 => 60,  // +4 -> +5
        5 => 50,  // +5 -> +6
        6 => 40,  // +6 -> +7
        7 => 30,  // +7 -> +8
        8 => 20,  // +8 -> +9
        9 => 10,  // +9 -> +10
        _ => 0,
    };

    // Stat bonus per enhancement level (flat addition).
    private static int BonusPerLevel(EquipmentBase eq) => eq switch
    {
        Weapon => 3,   // +3 ATK per level
        Armor  => 2,   // +2 DEF per level
        _      => 1,   // +1 primary stat per level
    };

    public static void Show(Player player, int floor)
    {
        var dialog = DialogHelper.Create("Anvil -- Blacksmith", DialogWidth, DialogHeight);

        var header = new Label
        {
            Text = $"Col: {player.ColOnHand}    Materials: {CountMaterials(player)}",
            X = 2, Y = 0, Width = Dim.Fill(2), ColorScheme = ColorSchemes.Gold,
        };

        var repairBtn = DialogHelper.CreateButton("Repair All");
        repairBtn.X = 2; repairBtn.Y = 2;

        var enhanceBtn = DialogHelper.CreateButton("Enhance Equipment");
        enhanceBtn.X = 2; enhanceBtn.Y = 4;

        var evolveBtn = DialogHelper.CreateButton("Evolve Weapon");
        evolveBtn.X = 2; evolveBtn.Y = 6;

        var resultLabel = new Label
        {
            Text = "", X = 2, Y = 8,
            Width = Dim.Fill(2), Height = 10, ColorScheme = ColorSchemes.Body,
        };

        int repairCost = 50 + floor * 25;

        repairBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (player.ColOnHand < repairCost)
            {
                resultLabel.Text = $"Repairs cost {repairCost} Col. You only have {player.ColOnHand}.";
                resultLabel.ColorScheme = ColorSchemes.Danger;
                return;
            }
            int repaired = 0;
            foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
            {
                var eq = player.Inventory.GetEquipped(slot);
                if (eq == null) continue;
                int maxDur = 50 + floor * 10 + eq.EnhancementLevel * 5;
                if (eq.ItemDurability < maxDur) { eq.ItemDurability = maxDur; repaired++; }
            }
            // Broken gear contributes 0 bonuses; repairing restores them, so
            // the cached aggregate must be rebuilt on next read.
            if (repaired > 0) player.Inventory.InvalidateStatCache();
            if (repaired == 0)
            {
                resultLabel.Text = "All equipment is in good condition.";
                resultLabel.ColorScheme = ColorSchemes.Dim;
            }
            else
            {
                player.ColOnHand -= repairCost;
                resultLabel.Text = $"Repaired {repaired} item(s)! (-{repairCost} Col)";
                resultLabel.ColorScheme = ColorSchemes.Gold;
                header.Text = $"Col: {player.ColOnHand}    Materials: {CountMaterials(player)}";
            }
        };

        enhanceBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            ShowEnhanceMenu(player, floor, resultLabel, header);
        };

        evolveBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            TryEvolveWeapon(player, resultLabel, header);
        };

        var hintLabel = new Label
        {
            Text = "Enter: select | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(header, repairBtn, enhanceBtn, evolveBtn, resultLabel, hintLabel);
        DialogHelper.AddCloseFooter(dialog);
        repairBtn.SetFocus();
        DialogHelper.RunModal(dialog);
    }

    private static void ShowEnhanceMenu(Player player, int floor, Label resultLabel, Label header)
    {
        // Gather all equipped items that can be enhanced
        var candidates = new List<(EquipmentSlot Slot, EquipmentBase Item)>();
        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            var eq = player.Inventory.GetEquipped(slot);
            if (eq != null && eq.EnhancementLevel < MaxEnhancement)
                candidates.Add((slot, eq));
        }

        if (candidates.Count == 0)
        {
            resultLabel.Text = "No equipment to enhance. Equip gear first, or all items are at +10.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

        // Build choice list
        var labels = new string[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            var (slot, item) = candidates[i];
            int cost = EnhanceCost(item.EnhancementLevel, floor);
            int mats = MaterialsNeeded(item.EnhancementLevel);
            int rate = SuccessRate(item.EnhancementLevel);
            labels[i] = $"{slot}: {item.EnhancedName} -> +{item.EnhancementLevel + 1}  " +
                         $"({cost} Col, {mats} mats, {rate}% chance)";
        }

        int choice = MessageBox.Query("Enhance Which?",
            string.Join("\n", labels),
            labels.Select((_, i) => candidates[i].Slot.ToString()).Append("Cancel").ToArray());

        if (choice < 0 || choice >= candidates.Count) return;

        var (chosenSlot, chosenItem) = candidates[choice];
        int enhCost = EnhanceCost(chosenItem.EnhancementLevel, floor);
        int matsNeeded = MaterialsNeeded(chosenItem.EnhancementLevel);
        int successRate = SuccessRate(chosenItem.EnhancementLevel);
        int matCount = CountMaterials(player);

        if (player.ColOnHand < enhCost)
        {
            resultLabel.Text = $"Not enough Col! Need {enhCost}, have {player.ColOnHand}.";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (matCount < matsNeeded)
        {
            resultLabel.Text = $"Not enough materials! Need {matsNeeded}, have {matCount}.\n" +
                               "Defeat monsters to collect crafting materials.";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        // Consume resources
        player.ColOnHand -= enhCost;
        ConsumeMaterials(player, matsNeeded);

        // Roll for success
        bool success = Random.Shared.Next(100) < successRate;

        if (success)
        {
            // Unequip to remove old bonuses, enhance, re-equip with new bonuses
            ApplyEnhancementDelta(player, chosenItem, +1);

            resultLabel.Text = $"SUCCESS! {chosenItem.EnhancedName}\n" +
                               $"+{BonusPerLevel(chosenItem)} {(chosenItem is Weapon ? "ATK" : "DEF")} per level\n" +
                               $"(-{enhCost} Col, -{matsNeeded} materials)";
            resultLabel.ColorScheme = ColorSchemes.Gold;
        }
        else
        {
            // Failure: materials consumed, Col spent, no improvement.
            // On +7 or higher, risk of losing a level (SAO canon).
            if (chosenItem.EnhancementLevel >= 7 && Random.Shared.Next(100) < 30)
            {
                ApplyEnhancementDelta(player, chosenItem, -1);

                resultLabel.Text = $"FAILURE! Enhancement dropped to +{chosenItem.EnhancementLevel}!\n" +
                                   $"Materials and Col consumed. High-level enhancement is risky!";
                resultLabel.ColorScheme = ColorSchemes.Danger;
            }
            else
            {
                resultLabel.Text = $"FAILURE! The enhancement didn't take.\n" +
                                   $"Materials and Col consumed. Try again?";
                resultLabel.ColorScheme = ColorSchemes.Danger;
            }
        }

        header.Text = $"Col: {player.ColOnHand}    Materials: {CountMaterials(player)}";
    }

    // Shared enhance/de-enhance: unequip, adjust level and stat bonus by ±1,
    // re-equip. Weapon bonuses go to Attack, Armor+other to Defense/Attack.
    private static void ApplyEnhancementDelta(Player player, EquipmentBase item, int delta)
    {
        item.Unequip(player);
        int signedBonus = BonusPerLevel(item) * delta;
        StatType stat = item is Armor ? StatType.Defense : StatType.Attack;
        item.Bonuses.Add(stat, signedBonus);
        item.EnhancementLevel += delta;
        item.Equip(player);
        // Bonuses list was mutated on a still-equipped item — drop cache.
        player.Inventory.InvalidateStatCache();
    }

    // ── Evolve (Priority 5 Phase B) ───────────────────────────────────
    // Consumes chain-specific catalysts (+ peak extra at T3) and swaps the
    // equipped weapon for its next-tier incarnation. Preserves enhancement
    // level. T4 apex weapons cannot evolve further.
    private static void TryEvolveWeapon(Player player, Label resultLabel, Label header)
    {
        var equipped = player.Inventory.GetEquipped(EquipmentSlot.Weapon);
        if (equipped is not Weapon weapon)
        {
            resultLabel.Text = "Equip a weapon first to check for evolution paths.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

        var step = WeaponEvolutionChains.Get(weapon.DefinitionId);
        if (step == null)
        {
            resultLabel.Text = $"{weapon.Name}\nThis weapon is not part of an evolution chain.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

        if (step.Tier == ChainTier.T4 || step.NextDefId == null)
        {
            resultLabel.Text = $"{weapon.Name}\nThis weapon is already at its peak evolution.";
            resultLabel.ColorScheme = ColorSchemes.Gold;
            return;
        }

        // Compose costs + resolve display names up front for the prompt.
        string matName = MaterialDisplayName(step.MaterialDefId);
        int haveMat = CountMaterialByDefId(player, step.MaterialDefId);

        string? peakName = step.PeakExtraMatId != null ? MaterialDisplayName(step.PeakExtraMatId) : null;
        int havePeak = step.PeakExtraMatId != null ? CountMaterialByDefId(player, step.PeakExtraMatId) : 0;

        // Preview the resulting weapon (without committing it) so we can show its name.
        var previewNext = ItemRegistry.Create(step.NextDefId);
        string nextName = previewNext?.Name ?? step.NextDefId;

        // Build the cost summary shown in the confirmation prompt.
        string costLine = $"{step.MaterialQty}x {matName} ({haveMat} owned)";
        if (step.PeakExtraMatId != null && peakName != null)
            costLine += $"\n + 1x {peakName} ({havePeak} owned)";

        string promptBody =
            $"Evolve {weapon.EnhancedName} into {nextName}?\n\n" +
            $"Cost: {costLine}";

        int choice = MessageBox.Query("Evolve Weapon", promptBody, "Confirm", "Cancel");
        if (choice != 0) return;

        // Validate materials after confirm so the player sees the full prompt first.
        if (haveMat < step.MaterialQty)
        {
            resultLabel.Text = $"Not enough {matName}! Need {step.MaterialQty}, have {haveMat}.";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }
        if (step.PeakExtraMatId != null && havePeak < 1)
        {
            resultLabel.Text = $"Apex evolution requires 1x {peakName}. You have {havePeak}.";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        // Build the next-tier weapon, preserving enhancement level if supported.
        var created = ItemRegistry.Create(step.NextDefId);
        if (created is not Weapon nextWeapon)
        {
            resultLabel.Text = $"Internal error: next weapon '{step.NextDefId}' could not be created.";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        // Consume materials.
        ConsumeMaterialByDefId(player, step.MaterialDefId, step.MaterialQty);
        if (step.PeakExtraMatId != null)
            ConsumeMaterialByDefId(player, step.PeakExtraMatId, 1);

        // Preserve enhancement. The enhance bonuses live in Bonuses; we bake
        // the equivalent of the existing level into the new weapon, then swap.
        int oldEnhLevel = weapon.EnhancementLevel;
        string oldName = weapon.EnhancedName;

        // Unequip old weapon (removes its stat bonuses from the player).
        weapon.Unequip(player);

        // If the old weapon was +N, fold those bonuses into the new weapon so
        // the displayed +N is real. ApplyEnhancementDelta's pattern of adding
        // flat Attack per level is reused here for parity with Enhance flow.
        if (oldEnhLevel > 0)
        {
            int bonusPerLevel = BonusPerLevel(nextWeapon);
            nextWeapon.Bonuses.Add(StatType.Attack, bonusPerLevel * oldEnhLevel);
            nextWeapon.EnhancementLevel = oldEnhLevel;
        }

        // Put the old weapon back in the backpack (inventory list) so the
        // player can verify the swap / sell the previous tier. The Equip call
        // below will auto-remove the old one from the equipped slot anyway.
        player.Inventory.AddItem(weapon);

        // Equip the new weapon. Inventory.Equip will also pull it off the
        // backpack list if present; since we just created it, we add first
        // then equip to route through the normal flow.
        player.Inventory.AddItem(nextWeapon);
        player.Inventory.Equip(nextWeapon, player);
        player.Inventory.InvalidateStatCache();

        // Log with gold + magenta emphasis. The ◈ prefix triggers
        // LogColorRules.Rules to render the line in BrightRed as a heavy
        // accent for the transformation, matching the Divine Object log style.
        resultLabel.Text =
            $"◈ Your {oldName} evolves into {nextWeapon.EnhancedName}!\n" +
            $"Consumed {step.MaterialQty}x {matName}" +
            (step.PeakExtraMatId != null ? $" + 1x {peakName}." : ".");
        resultLabel.ColorScheme = ColorSchemes.Gold;

        header.Text = $"Col: {player.ColOnHand}    Materials: {CountMaterials(player)}";
    }

    // Resolves the display name of a material from its DefId via a throwaway
    // ItemRegistry.Create lookup. Falls back to the DefId if unknown.
    private static string MaterialDisplayName(string defId)
    {
        var item = ItemRegistry.Create(defId);
        return item?.Name ?? defId;
    }

    // Count of a specific chain material in the player's backpack, keyed by DefId.
    private static int CountMaterialByDefId(Player player, string defId) =>
        player.Inventory.Items
            .OfType<Material>()
            .Where(m => m.DefinitionId == defId)
            .Sum(m => m.Quantity);

    // Decrement `count` units of the material matching `defId` across stacks.
    // Removes empty stacks from the inventory. No-op if insufficient.
    private static void ConsumeMaterialByDefId(Player player, string defId, int count)
    {
        int remaining = count;
        var toRemove = new List<BaseItem>();
        foreach (var mat in player.Inventory.Items.OfType<Material>().ToList())
        {
            if (remaining <= 0) break;
            if (mat.DefinitionId != defId) continue;
            if (mat.Quantity <= remaining)
            {
                remaining -= mat.Quantity;
                toRemove.Add(mat);
            }
            else
            {
                mat.Quantity -= remaining;
                remaining = 0;
            }
        }
        foreach (var item in toRemove)
            player.Inventory.RemoveItem(item);
    }

    private static int CountMaterials(Player player) =>
        player.Inventory.Items.OfType<Material>().Sum(m => m.Quantity);

    private static void ConsumeMaterials(Player player, int count)
    {
        int remaining = count;
        var toRemove = new List<BaseItem>();
        foreach (var mat in player.Inventory.Items.OfType<Material>().ToList())
        {
            if (remaining <= 0) break;
            if (mat.Quantity <= remaining)
            {
                remaining -= mat.Quantity;
                toRemove.Add(mat);
            }
            else
            {
                mat.Quantity -= remaining;
                remaining = 0;
            }
        }
        foreach (var item in toRemove)
            player.Inventory.RemoveItem(item);
    }
}
