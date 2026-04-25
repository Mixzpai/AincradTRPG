using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Bundle 9 — Divine Awakening modal. Opened via Selka F65 when player carries a Divine.
// Per-run cap means usually 1 Divine; picker used defensively if >1.
public static class DivineAwakeningDialog
{
    private const int DialogWidth = 64;
    private const int DialogHeight = 22;

    public static void Show(Player player)
    {
        // Gather all Divine-rarity weapons across inventory + equipped slots.
        var divines = CollectDivines(player);
        if (divines.Count == 0) { ShowEmpty(); return; }

        // Pick which Divine (auto-select if 1).
        Weapon? target;
        if (divines.Count == 1) target = divines[0];
        else
        {
            var labels = divines.Select(w => w.EnhancedName).Append("Cancel").ToArray();
            int pick = MessageBox.Query("Divine Awakening",
                "Selka: \"Which blade calls to be awakened?\"", labels);
            if (pick < 0 || pick >= divines.Count) return;
            target = divines[pick];
        }

        ShowAwakenDialog(player, target);
    }

    // Builds the Divine-weapon roster from equipped slots + backpack (de-duped by ref).
    private static List<Weapon> CollectDivines(Player player)
    {
        var result = new List<Weapon>();
        var seen = new HashSet<Weapon>();
        foreach (Inventory.Core.EquipmentSlot s in Enum.GetValues(typeof(Inventory.Core.EquipmentSlot)))
        {
            if (player.Inventory.GetEquipped(s) is Weapon wEq && wEq.Rarity == "Divine" && seen.Add(wEq))
                result.Add(wEq);
        }
        foreach (var item in player.Inventory.Items)
        {
            if (item is Weapon w && w.Rarity == "Divine" && seen.Add(w))
                result.Add(w);
        }
        return result;
    }

    // Fallback — HandleSelkaAwakening gates this path, so reaching here means a race/data gap.
    private static void ShowEmpty()
    {
        MessageBox.Query("Divine Awakening",
            "Selka: \"I sense no divine blade in your keeping.\"", "Close");
    }

    // Main preview modal: current/next level + stat delta + material costs + confirm/cancel.
    private static void ShowAwakenDialog(Player player, Weapon target)
    {
        var dialog = DialogHelper.Create(" * Divine Awakening -- Sister Selka * ", DialogWidth, DialogHeight);
        dialog.ColorScheme = ColorSchemes.Gold;

        var header = new Label
        {
            Text = $"Target: {target.EnhancedName}",
            X = 2, Y = 0, Width = Dim.Fill(2), ColorScheme = ColorSchemes.Title,
        };

        var body = new Label
        {
            Text = "", X = 2, Y = 2, Width = Dim.Fill(2), Height = 12,
            ColorScheme = ColorSchemes.Body,
        };

        var footerHint = new Label
        {
            Text = "Enter: Awaken  |  Esc: Cancel",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        // Bottom-row buttons. Confirm sits left-of-center so focus lands there first.
        var confirmBtn = DialogHelper.CreateButton("Awaken", isDefault: true);
        confirmBtn.X = Pos.Center() - 10;
        confirmBtn.Y = Pos.AnchorEnd(3);

        var cancelBtn = DialogHelper.CreateButton("Cancel");
        cancelBtn.X = Pos.Center() + 2;
        cancelBtn.Y = Pos.AnchorEnd(3);

        // Capture state so confirm knows whether to actually fire.
        bool canAfford = false;

        void Refresh()
        {
            header.Text = $"Target: {target.EnhancedName}";
            if (!target.CanAwaken)
            {
                body.Text =
                    $"Maximum awakening reached (◈{target.AwakeningLevel}).\n\n" +
                    "Selka smiles. \"The blade has unfolded all it can.\"";
                body.ColorScheme = ColorSchemes.Gold;
                confirmBtn.Enabled = false;
                canAfford = false;
                return;
            }

            int nextLevel = target.AwakeningLevel + 1;
            int currentBonus = DivineAwakening.ComputeBonusAttack(target);
            int nextBonus = target.BaseDamage * DivineAwakening.DamagePercentPerLevel * nextLevel / 100;
            int delta = nextBonus - currentBonus;
            int nextPct = DivineAwakening.DamagePercentPerLevel * nextLevel;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Current:  ◈{target.AwakeningLevel}    Next:  ◈{nextLevel}");
            sb.AppendLine();
            sb.AppendLine($"Stat gain: +{delta} Attack  (total {nextPct}% of BaseDamage {target.BaseDamage})");
            sb.AppendLine();
            sb.AppendLine("Materials required:");

            bool haveAll = true;
            if (DivineAwakening.CostPerLevel.TryGetValue(nextLevel, out var costs))
            {
                var missing = new List<string>();
                foreach (var (defId, need) in costs)
                {
                    int have = player.Inventory.CountByDefinitionId(defId);
                    string name = ItemRegistry.Create(defId)?.Name ?? defId;
                    string mark = have >= need ? "+" : "-";
                    sb.AppendLine($"   [{mark}] {need}x {name}   (have {have})");
                    if (have < need)
                    {
                        haveAll = false;
                        missing.Add($"{need - have}x {name}");
                    }
                }
                sb.AppendLine();
                if (haveAll)
                    sb.AppendLine("Selka nods. \"The petals are ready to unfold further.\"");
                else
                    sb.AppendLine($"Missing: {string.Join(", ", missing)}");
            }
            else
            {
                sb.AppendLine("   (no cost table for this level)");
                haveAll = false;
            }

            body.Text = sb.ToString();
            body.ColorScheme = haveAll ? ColorSchemes.Body : ColorSchemes.Dim;
            confirmBtn.Enabled = haveAll;
            canAfford = haveAll;
        }

        confirmBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (!target.CanAwaken || !canAfford) return;
            int newLevel = target.AwakeningLevel + 1;
            DivineAwakening.Awaken(target, player);
            // Post-state confirms level actually bumped (Awaken is silent-fail on race).
            if (target.AwakeningLevel == newLevel)
            {
                DivineObtainBanner.TriggerAwakening(target, newLevel);
                Application.RequestStop();
            }
            else
            {
                // Refresh redraws from live state; no separate failure text needed.
                Refresh();
            }
        };

        cancelBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        dialog.Add(header, body, confirmBtn, cancelBtn, footerHint);
        DialogHelper.CloseOnEscape(dialog);
        Refresh();
        // Focus Confirm when affordable, else Cancel so Enter is safe when materials missing.
        if (canAfford) confirmBtn.SetFocus(); else cancelBtn.SetFocus();
        DialogHelper.RunModal(dialog);
    }
}
