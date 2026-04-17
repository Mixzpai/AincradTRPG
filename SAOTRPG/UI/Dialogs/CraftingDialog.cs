using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
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
    private const int DialogWidth = 72, DialogHeight = 26;
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

        var refineBtn = DialogHelper.CreateButton("Refine Equipment");
        refineBtn.X = 2; refineBtn.Y = 8;

        var resultLabel = new Label
        {
            Text = "", X = 2, Y = 10,
            Width = Dim.Fill(2), Height = 12, ColorScheme = ColorSchemes.Body,
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

        refineBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            ShowRefineMenu(player, resultLabel, header);
        };

        var hintLabel = new Label
        {
            Text = "Enter: select | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(header, repairBtn, enhanceBtn, evolveBtn, refineBtn, resultLabel, hintLabel);
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
            string lock1 = item is Weapon w && !w.IsEnhanceable ? " [SEALED]" : "";
            labels[i] = $"{slot}: {item.EnhancedName}{lock1} -> +{item.EnhancementLevel + 1}  " +
                         $"({cost} Col, {mats} mats, {rate}% chance)";
        }

        int choice = MessageBox.Query("Enhance Which?",
            string.Join("\n", labels),
            labels.Select((_, i) => candidates[i].Slot.ToString()).Append("Cancel").ToArray());

        if (choice < 0 || choice >= candidates.Count) return;

        var (chosenSlot, chosenItem) = candidates[choice];

        // IM Non-enhanceable check (System 2): LAB floor-boss weapons are
        // sealed — canon IM tradeoff for their higher flat stats.
        if (chosenItem is Weapon weaponCheck && !weaponCheck.IsEnhanceable)
        {
            resultLabel.Text = $"{weaponCheck.EnhancedName} cannot be enhanced.\n" +
                               "This Last-Attack Bonus weapon is sealed by its forging.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

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

        // IM System 3: for weapons, require the player to pick an
        // enhancement ore up front. Ore identity decides which stat gets
        // this level's bonus. Armor/accessories skip the ore picker and
        // use the legacy flat-Defense/-Attack path.
        string? chosenOreDefId = null;
        if (chosenItem is Weapon)
        {
            chosenOreDefId = PickEnhancementOre(player, resultLabel);
            if (chosenOreDefId == null) return;  // player cancelled or no ores
        }

        // Consume resources
        player.ColOnHand -= enhCost;
        ConsumeMaterials(player, matsNeeded);
        if (chosenOreDefId != null)
            player.Inventory.ConsumeByDefinitionId(chosenOreDefId, 1);

        // Roll for success
        bool success = Random.Shared.Next(100) < successRate;

        if (success)
        {
            // Unequip to remove old bonuses, enhance, re-equip with new bonuses.
            // Weapon path threads the ore DefId through so the per-level bonus
            // is biased to the ore's stat instead of flat Attack.
            ApplyEnhancementDelta(player, chosenItem, +1, chosenOreDefId);

            string biasMsg = "";
            if (chosenOreDefId != null &&
                EnhancementOreDefinitions.OreDefIdToStat.TryGetValue(chosenOreDefId, out var biasStat))
            {
                biasMsg = $"\n+{BonusPerLevel(chosenItem)} {biasStat} from this ore.";
            }
            resultLabel.Text = $"SUCCESS! {chosenItem.EnhancedName}{biasMsg}\n" +
                               $"(-{enhCost} Col, -{matsNeeded} materials" +
                               (chosenOreDefId != null ? $", -1 ore" : "") + ")";
            resultLabel.ColorScheme = ColorSchemes.Gold;
        }
        else
        {
            // Failure: materials consumed, Col spent, no improvement.
            // On +7 or higher, risk of losing a level (SAO canon).
            if (chosenItem.EnhancementLevel >= 7 && Random.Shared.Next(100) < 30)
            {
                // On level loss we pop the last ore from history (if any) and
                // remove the stat it contributed, matching the +1 inverse.
                ApplyEnhancementDelta(player, chosenItem, -1, null);

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

    // IM System 3: list all enhancement ores the player holds (grouped by
    // DefId, counts shown) and return the chosen DefId — or null if the
    // player has no ores or cancels. Writes a hint into resultLabel on the
    // no-ore path so the Anvil UI reflects why the flow aborted.
    private static string? PickEnhancementOre(Player player, Label resultLabel)
    {
        var oreStacks = player.Inventory.Items
            .OfType<EnhancementOre>()
            .Where(o => o.Quantity > 0)
            .GroupBy(o => o.DefinitionId ?? "")
            .Select(g => (DefId: g.Key, Sample: g.First(), Qty: g.Sum(x => x.Quantity)))
            .Where(t => !string.IsNullOrEmpty(t.DefId))
            .OrderBy(t => t.Sample.Name)
            .ToList();

        if (oreStacks.Count == 0)
        {
            resultLabel.Text = "Enhancement requires an Enhancement Ore.\n" +
                               "Defeat themed mobs or bosses to collect one.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return null;
        }

        // MessageBox.Query supports ~10 buttons comfortably — 7 ores fits.
        var labels = oreStacks
            .Select(t => $"{t.Sample.Name} x{t.Qty} (+{t.Sample.BiasStat})")
            .Append("Cancel")
            .ToArray();
        int which = MessageBox.Query(
            "Choose Enhancement Ore",
            "Pick an ore — its theme biases this level's bonus:",
            labels);
        if (which < 0 || which >= oreStacks.Count) return null;
        return oreStacks[which].DefId;
    }

    // Shared enhance/de-enhance: unequip, adjust level and stat bonus by ±1,
    // re-equip. Weapon bonuses route through the ore-biased stat (IM System 3)
    // — oreDefId names the ore consumed for +1, and is pulled from history on
    // -1. Armor/accessories keep legacy flat-Defense/-Attack behavior.
    private static void ApplyEnhancementDelta(Player player, EquipmentBase item, int delta, string? oreDefId)
    {
        item.Unequip(player);
        int bonus = BonusPerLevel(item);

        if (item is Weapon weapon)
        {
            if (delta > 0)
            {
                // oreDefId must be non-null on the +1 path.
                string ore = oreDefId ?? "ore_crimson_flame";
                weapon.EnhancementOreHistory.Add(ore);
                var stat = EnhancementOreDefinitions.OreDefIdToStat
                    .TryGetValue(ore, out var s) ? s : StatType.Attack;
                weapon.Bonuses.Add(stat, bonus);
                // Adamant flavour: ores may carry a one-off ItemDurability
                // reinforcement on socket. Read via ItemRegistry prototype
                // to avoid hard-coded ore defId lookups here. Tyler Q3=a.
                int durBump = GetOreDurabilityBonus(ore);
                if (durBump != 0) weapon.ItemDurability += durBump;
            }
            else
            {
                // -1 path: pop the last ore and refund its biased stat.
                if (weapon.EnhancementOreHistory.Count > 0)
                {
                    string lastOre = weapon.EnhancementOreHistory[^1];
                    weapon.EnhancementOreHistory.RemoveAt(weapon.EnhancementOreHistory.Count - 1);
                    var stat = EnhancementOreDefinitions.OreDefIdToStat
                        .TryGetValue(lastOre, out var s) ? s : StatType.Attack;
                    weapon.Bonuses.Add(stat, -bonus);
                    int durBump = GetOreDurabilityBonus(lastOre);
                    if (durBump != 0) weapon.ItemDurability = Math.Max(1, weapon.ItemDurability - durBump);
                }
                else
                {
                    weapon.Bonuses.Add(StatType.Attack, -bonus);
                }
            }
        }
        else
        {
            int signedBonus = bonus * delta;
            StatType stat = item is Armor ? StatType.Defense : StatType.Attack;
            item.Bonuses.Add(stat, signedBonus);
        }

        item.EnhancementLevel += delta;
        item.Equip(player);
        // Bonuses list was mutated on a still-equipped item — drop cache.
        player.Inventory.InvalidateStatCache();
    }

    // Look up an ore's DurabilityBonus by instantiating its prototype from
    // ItemRegistry. Returns 0 for any ore that doesn't define one (default).
    // Adamant Ore is the only ore with a non-zero value today (+10/level).
    private static int GetOreDurabilityBonus(string oreDefId)
    {
        if (ItemRegistry.Create(oreDefId) is EnhancementOre ore)
            return ore.DurabilityBonus;
        return 0;
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
        // the displayed +N is real. IM System 3: preserve per-level ore
        // biases from the old weapon's history so the stat distribution
        // follows the player's prior investment (not a flat +Attack rebake).
        if (oldEnhLevel > 0)
        {
            int bonusPerLevel = BonusPerLevel(nextWeapon);
            nextWeapon.EnhancementLevel = oldEnhLevel;
            // Copy ore history 1:1 (length == oldEnhLevel on any save-migrated
            // or natively-forged weapon). Fallback to Crimson Flame if the
            // count mismatch sneaks through.
            nextWeapon.EnhancementOreHistory = new List<string>(weapon.EnhancementOreHistory);
            while (nextWeapon.EnhancementOreHistory.Count < oldEnhLevel)
                nextWeapon.EnhancementOreHistory.Add("ore_crimson_flame");
            for (int i = 0; i < oldEnhLevel; i++)
            {
                string oreId = nextWeapon.EnhancementOreHistory[i];
                var stat = EnhancementOreDefinitions.OreDefIdToStat
                    .TryGetValue(oreId, out var s) ? s : StatType.Attack;
                nextWeapon.Bonuses.Add(stat, bonusPerLevel);
            }
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

    // ── Refine (IF Phase B Agent 4) ──────────────────────────────────────
    // Socket an Ingot into one of the 3 Refinement slots on the currently
    // equipped weapon or shield (OffHand). Override-only: replacing an
    // occupied slot destroys the old ingot. Divine-rarity gear is sealed
    // and cannot be refined (handled here + in Refinement.Socket).
    private static void ShowRefineMenu(Player player, Label resultLabel, Label header)
    {
        // Collect refinable equipment: Weapon slot + OffHand if it's a shield
        // (Armor but NOT a Weapon — dual-blade offhands are not refinable via
        // this flow either since they compete with the weapon branch).
        var candidates = new List<(EquipmentSlot Slot, EquipmentBase Item, string Label)>();
        var weapon = player.Inventory.GetEquipped(EquipmentSlot.Weapon);
        if (weapon is EquipmentBase wEq) candidates.Add((EquipmentSlot.Weapon, wEq, "Weapon"));
        var off = player.Inventory.GetEquipped(EquipmentSlot.OffHand);
        if (off is Armor offArmor) candidates.Add((EquipmentSlot.OffHand, offArmor, "Shield"));

        if (candidates.Count == 0)
        {
            resultLabel.Text = "Refinement requires an equipped weapon or shield.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

        // Pick which piece of gear to refine (if there are multiple candidates).
        EquipmentBase? target;
        string targetLabel;
        if (candidates.Count == 1)
        {
            target = candidates[0].Item;
            targetLabel = candidates[0].Label;
        }
        else
        {
            var btnLabels = candidates
                .Select(c => $"{c.Label}: {c.Item.EnhancedName}")
                .Append("Cancel")
                .ToArray();
            int which = MessageBox.Query("Refine Which?",
                "Choose equipment to refine:", btnLabels);
            if (which < 0 || which >= candidates.Count) return;
            target = candidates[which].Item;
            targetLabel = candidates[which].Label;
        }

        // Divine = sealed endgame gear.
        if (target.Rarity == "Divine")
        {
            resultLabel.Text = $"{target.EnhancedName}: Divine weapons cannot be refined.";
            resultLabel.ColorScheme = ColorSchemes.Gold;
            return;
        }

        // Pick slot (1/2/3) — show current occupant per slot.
        var slotChoices = new string[EquipmentBase.RefinementSlotCount + 1];
        for (int i = 0; i < EquipmentBase.RefinementSlotCount; i++)
        {
            string occ = target.RefinementSlots[i] ?? "";
            string occName = string.IsNullOrEmpty(occ)
                ? "<empty>" : (ItemRegistry.Create(occ)?.Name ?? occ);
            slotChoices[i] = $"Slot {i + 1}: {occName}";
        }
        slotChoices[EquipmentBase.RefinementSlotCount] = "Cancel";
        var summary = Refinement.GetBonusSummary(target);
        int slotIdx = MessageBox.Query(
            $"Refine — {targetLabel}",
            $"{target.EnhancedName}\nCurrent refinement: {summary.DisplayText}\n\nChoose a slot:",
            slotChoices);
        if (slotIdx < 0 || slotIdx >= EquipmentBase.RefinementSlotCount) return;

        // Pick an Ingot from inventory. Group by rarity label in the list.
        var ingotStacks = player.Inventory.Items
            .OfType<Ingot>()
            .Where(ig => ig.Quantity > 0)
            .GroupBy(ig => ig.DefinitionId ?? "")
            .Select(g => (DefId: g.Key, Sample: g.First(), Qty: g.Sum(x => x.Quantity)))
            .Where(t => !string.IsNullOrEmpty(t.DefId))
            .OrderBy(t => RarityOrder(t.Sample.Rarity))
            .ThenBy(t => t.Sample.Name)
            .ToList();

        if (ingotStacks.Count == 0)
        {
            resultLabel.Text = "You have no Ingots to socket.";
            resultLabel.ColorScheme = ColorSchemes.Dim;
            return;
        }

        // Build a choice list. MessageBox.Query gets cramped with 12+
        // buttons — truncate to the first 10 owned ingots, cheapest first.
        var shown = ingotStacks.Take(10).ToList();
        var ingotLabels = shown
            .Select(t =>
            {
                int need = Refinement.CostForRarity(t.Sample.Rarity);
                return $"[{t.Sample.Rarity}] {t.Sample.Name} x{t.Qty} (cost {need})";
            })
            .Append("Cancel")
            .ToArray();
        int which2 = MessageBox.Query(
            $"Slot {slotIdx + 1} — Pick an Ingot",
            $"{target.EnhancedName} slot {slotIdx + 1}:",
            ingotLabels);
        if (which2 < 0 || which2 >= shown.Count) return;

        var pick = shown[which2];
        string ingotDefId = pick.DefId!;
        int cost = Refinement.CostForRarity(pick.Sample.Rarity);

        // Confirmation with DESTROY warning for override.
        string? occId = target.RefinementSlots[slotIdx];
        string oldLine = string.IsNullOrEmpty(occId)
            ? ""
            : $"Will DESTROY: {ItemRegistry.Create(occId)?.Name ?? occId}\n";
        if (player.Inventory.CountByDefinitionId(ingotDefId) < cost)
        {
            resultLabel.Text =
                $"Need {cost}x {pick.Sample.Name}, have {pick.Qty}.";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        int confirm = MessageBox.Query("Confirm Refinement",
            $"Socket {pick.Sample.Name} into slot {slotIdx + 1} of\n" +
            $"{target.EnhancedName}?\n\n" +
            $"{oldLine}" +
            $"Effect: {pick.Sample.EffectDescription}\n" +
            $"Cost: {cost}x {pick.Sample.Name}",
            "Confirm", "Cancel");
        if (confirm != 0) return;

        // Refinement.Socket consumes exactly 1 ingot from the inventory.
        // Any "rarity tax" beyond that (CostForRarity - 1) is consumed here
        // as additional matching ingots. This mirrors the scout's intent
        // (higher-tier ingots demand more stock to attempt the socket).
        int extra = Math.Max(0, cost - 1);
        if (extra > 0)
        {
            if (!player.Inventory.ConsumeByDefinitionId(ingotDefId, extra))
            {
                resultLabel.Text = "Failed to consume extra ingots — aborted.";
                resultLabel.ColorScheme = ColorSchemes.Danger;
                return;
            }
        }

        bool ok = Refinement.Socket(target, slotIdx, ingotDefId, player.Inventory, player);
        if (!ok)
        {
            resultLabel.Text = "Refinement failed (sealed gear or inventory mismatch).";
            resultLabel.ColorScheme = ColorSchemes.Danger;
            return;
        }

        var newSummary = Refinement.GetBonusSummary(target);
        resultLabel.Text =
            $"◈ {pick.Sample.Name} socketed into {target.EnhancedName} slot {slotIdx + 1}.\n" +
            $"New refinement: {newSummary.DisplayText}";
        resultLabel.ColorScheme = ColorSchemes.Gold;
        header.Text = $"Col: {player.ColOnHand}    Materials: {CountMaterials(player)}";
    }

    // Sort order for ingot listing — Common → Legendary.
    private static int RarityOrder(string? rarity) => rarity switch
    {
        "Common"    => 0,
        "Uncommon"  => 1,
        "Rare"      => 2,
        "Epic"      => 3,
        "Legendary" => 4,
        "Divine"    => 5,
        _           => 9,
    };

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
