using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Difficulty selection screen. A vertical RadioGroup handles the tier
// list with native Up/Down arrow navigation and a visible focus ring.
// The selected tier's description and stat modifiers update live.
// All elements centered via Pos.Center() for any terminal width.
public static class DifficultyScreen
{
    private const int DefaultTier = 3;
    private static EventHandler<Key>? _escHandler;

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        if (_escHandler != null) mainWindow.KeyDown -= _escHandler;
        var sw = DebugLogger.StartTimer("DifficultyScreen.Show");
        DebugLogger.LogScreen("DifficultyScreen");

        var tiers = DifficultyData.GetTiers();
        int count = tiers.Length;

        // ── Header (centered) ────────────────────────────────────────
        var (header, headerRule) = ScreenHeader.Create("Select Difficulty", 1, 20);

        // ── Tier RadioGroup (centered, native arrow key nav) ─────────
        // Each label: "TierName         Short description"
        var labels = new string[count];
        for (int i = 0; i < count; i++)
        {
            string desc = tiers[i].Description;
            // Take first sentence only for a compact inline preview.
            int dot = desc.IndexOf('.');
            if (dot > 0) desc = desc[..dot];
            labels[i] = $"{tiers[i].Name,-14}{desc}";
        }

        var tierRadio = new RadioGroup
        {
            X = Pos.Center() - 26, Y = 4,
            RadioLabels = labels,
            SelectedItem = DefaultTier,
            Width = 54, Height = count,
            ColorScheme = ColorSchemes.TierRadio,
        };

        // ── Thin rule between tier list and modifiers ─────────────────
        int previewY = 4 + count + 1;
        var tierModRule = new Label
        {
            Text = "--------------------------------------",
            X = Pos.Center(), Y = previewY,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim,
        };
        previewY += 1;

        // ── Live modifiers + description (centered) ──────────────────
        var modHeader = ScreenHeader.Section("Modifiers", previewY);

        var modLabel = new Label
        {
            Text = FormatModifiers(tiers[DefaultTier]),
            X = Pos.Center(), Y = previewY + 1,
            Width = Dim.Auto(), ColorScheme = ColorSchemes.Body,
        };

        var modDescRule = new Label
        {
            Text = "---",
            X = Pos.Center(), Y = previewY + 2,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim,
        };

        var descLabel = new Label
        {
            Text = tiers[DefaultTier].Description,
            X = Pos.Center(), Y = previewY + 3,
            Width = 60, Height = 2, ColorScheme = ColorSchemes.Dim,
        };

        // Update preview on every tier change.
        tierRadio.SelectedItemChanged += (s, e) =>
        {
            int idx = tierRadio.SelectedItem;
            if (idx < 0 || idx >= count) return;
            modLabel.Text = FormatModifiers(tiers[idx]);
            descLabel.Text = tiers[idx].Description;
        };

        // ── Hardcore toggle (centered) ───────────────────────────────
        int hcY = previewY + 6;
        var hardcoreCheck = new CheckBox
        {
            Text = " Hardcore — death is permanent, save deleted on death",
            X = Pos.Center() - 26, Y = hcY,
            ColorScheme = ColorSchemes.Body,
        };

        int unwinnableIndex = GetUnwinnableIndex(count);
        hardcoreCheck.CheckedStateChanging += (s, e) =>
        {
            if (e.NewValue != CheckState.Checked) return;
            int sel = tierRadio.SelectedItem;
            if (sel == 0)
                MessageBox.Query("Really?", "Hardcore on Story mode? Bold choice.", "Yes, really");
            else if (sel == unwinnableIndex)
                MessageBox.Query("Good Luck", "There's no way you can do this, but good luck.", "Bring it on");
        };

        // ── Run Modifiers row (FB-564) — TESTING_ALWAYS_ON (memory #375).
        // Canonical unlock: ProfileData.HasCompletedGame after F100 clear.
        int modY = hcY + 2;
        bool modifiersUnlocked = true; // TESTING_ALWAYS_ON — restore: ProfileData.HasCompletedGame
        var modifierBtn = new Button
        {
            Text = modifiersUnlocked ? " Run Modifiers " : " Run Modifiers (locked) ",
            X = Pos.Center() - 26, Y = modY,
            ColorScheme = modifiersUnlocked ? ColorSchemes.MenuButton : ColorSchemes.Dim,
            Enabled = modifiersUnlocked,
        };
        var modifierChipLabel = new Label
        {
            Text = FormatModifierChips(),
            X = Pos.Center() + 0, Y = modY,
            Width = 40, ColorScheme = ColorSchemes.Dim,
        };
        modifierBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            ModifierSelectScreen.Show(mainWindow, () =>
            {
                // Re-render DifficultyScreen after modifier picker closes.
                Show(mainWindow);
            });
        };

        // ── Footer buttons (centered row) ────────────────────────────
        int btnY = hcY + 5;
        // Total button width: Continue(12) + gap(3) + Details(11) + gap(3) + Back(8) = 37
        // Center the group: offset = -37/2 ≈ -18
        var continueBtn = new Button
        {
            Text = " Continue ", X = Pos.Center() - 18, Y = btnY,
            ColorScheme = ColorSchemes.MenuButton,
        };
        var detailsBtn = new Button
        {
            Text = " Details ", X = Pos.Center() - 3, Y = btnY,
            ColorScheme = ColorSchemes.MenuButton,
        };
        var backBtn = new Button
        {
            Text = " Back ", X = Pos.Center() + 11, Y = btnY,
            ColorScheme = ColorSchemes.MenuButton,
        };

        var hint = new Label
        {
            Text = "Up/Down: select   Space: hardcore   Enter: continue   Esc: back",
            X = Pos.Center(), Y = btnY + 2,
            Width = Dim.Auto(), ColorScheme = ColorSchemes.Dim,
        };

        foreach (var btn in new[] { continueBtn, detailsBtn, backBtn })
            btn.HasFocusChanged += (s, e) => { if (s is Button b) b.IsDefault = e.NewValue; };

        detailsBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int sel = tierRadio.SelectedItem;
            MessageBox.Query($"{tiers[sel].Name} — Details",
                DifficultyData.GetStatsTooltip(sel), "OK");
        };

        continueBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            bool hc = hardcoreCheck.CheckedState == CheckState.Checked;
            if (hc)
            {
                int confirm = MessageBox.Query("Hardcore Mode",
                    "Death is PERMANENT.\nYour save will be deleted on death.\n\nAre you sure?",
                    "I'm Ready", "Cancel");
                if (confirm != 0) return;
            }
            CharacterCreationScreen.Show(mainWindow, tierRadio.SelectedItem, hc);
        };

        backBtn.Accepting += (s, e) => { e.Cancel = true; TitleScreen.Show(mainWindow); };

        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { TitleScreen.Show(mainWindow); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;

        // ── Arrow key navigation ─────────────────────────────────────
        // RadioGroup consumes Up/Down internally, so intercept at the
        // boundary: Down on last tier → checkbox, Up on first → buttons.
        // Enter on the tier list fires Continue for fast selection.
        tierRadio.KeyDown += (s, e) =>
        {
            if (e.KeyCode is KeyCode.CursorDown or KeyCode.S
                && tierRadio.SelectedItem == count - 1)
            {
                hardcoreCheck.SetFocus(); e.Handled = true;
            }
            else if (e.KeyCode is KeyCode.CursorUp or KeyCode.W
                && tierRadio.SelectedItem == 0)
            {
                backBtn.SetFocus(); e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.Enter)
            {
                continueBtn.InvokeCommand(Command.HotKey);
                e.Handled = true;
            }
        };

        // Checkbox ↔ tier list / footer row
        NavigationHelper.WireUpDown(hardcoreCheck, tierRadio, continueBtn);

        // Footer buttons: Up→checkbox, Down→tier list, Left/Right between them
        var footerBtns = new[] { continueBtn, detailsBtn, backBtn };
        for (int i = 0; i < footerBtns.Length; i++)
        {
            int li = (i - 1 + footerBtns.Length) % footerBtns.Length;
            int ri = (i + 1) % footerBtns.Length;
            NavigationHelper.WireUpDown(footerBtns[i], hardcoreCheck, tierRadio);
            NavigationHelper.WireLeftRight(footerBtns[i], footerBtns[li], footerBtns[ri]);
        }

        // ── Assemble (all centered) ──────────────────────────────────
        mainWindow.Add(header, headerRule,
            tierRadio, tierModRule,
            modHeader, modLabel, modDescRule, descLabel,
            hardcoreCheck,
            modifierBtn, modifierChipLabel,
            continueBtn, detailsBtn, backBtn,
            hint);

        tierRadio.SetFocus();
        DebugLogger.EndTimer("DifficultyScreen.Show", sw);
    }

    private static string FormatModifiers(DifficultyData.DifficultyTier tier) =>
        $"Enemy Stats: {tier.MobStatPercent}%     " +
        $"XP Reward: {tier.XpPercent}%     " +
        $"Col Streak: +{tier.ColStreakBonus}%";

    // Compact chip summary of active run modifiers shown next to the button.
    private static string FormatModifierChips()
    {
        if (RunModifiers.Active.Count == 0) return "   (none active)";
        double mul = RunModifiers.TotalScoreMultiplier();
        var names = string.Join(", ", RunModifiers.Active
            .Select(m => RunModifiers.Definitions[m].Name));
        if (names.Length > 28) names = names[..27] + "…";
        return $"   ×{mul:F2}  {names}";
    }

    private static int GetUnwinnableIndex(int tierCount) =>
        tierCount - (DebugMode.IsEnabled ? 2 : 1);

}
