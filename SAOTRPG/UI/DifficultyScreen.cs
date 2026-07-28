using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Difficulty select: vertical RadioGroup (native Up/Down), live description + stat modifiers update.
public static class DifficultyScreen
{
    private const int DefaultTier = 3;
    private static EventHandler<Key>? _escHandler;

    // Unhook Esc handler before transitioning AWAY so Esc→TitleScreen doesn't leak into GameScreen.
    public static void UnhookEscHandler(Window mainWindow)
    {
        if (_escHandler != null) { mainWindow.KeyDown -= _escHandler; _escHandler = null; }
    }

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        if (_escHandler != null) mainWindow.KeyDown -= _escHandler;
        // The modifier picker returns here via its own Esc handler, which would otherwise
        // stay attached and fire alongside this screen's.
        ModifierSelectScreen.UnhookEscHandler(mainWindow);
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

        var tierRadio = new OptionSelector
        {
            X = Pos.Center() - 26, Y = 4,
            Labels = labels,
            Value = DefaultTier,
            Width = 54, Height = count,
            SchemeName = ColorSchemes.TierRadioName,
        };

        // ── Thin rule between tier list and modifiers ─────────────────
        int previewY = 4 + count + 1;
        var tierModRule = new Label
        {
            Text = "--------------------------------------",
            X = Pos.Center(), Y = previewY,
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DimName,
        };
        previewY += 1;

        // ── Live modifiers + description (centered) ──────────────────
        var modHeader = ScreenHeader.Section("Modifiers", previewY);

        var modLabel = new Label
        {
            Text = FormatModifiers(tiers[DefaultTier]),
            X = Pos.Center(), Y = previewY + 1,
            Width = Dim.Auto(), SchemeName = ColorSchemes.BodyName,
        };

        var modDescRule = new Label
        {
            Text = "---",
            X = Pos.Center(), Y = previewY + 2,
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DimName,
        };

        var descLabel = new Label
        {
            Text = tiers[DefaultTier].Description,
            X = Pos.Center(), Y = previewY + 3,
            Width = 60, Height = 2, SchemeName = ColorSchemes.DimName,
        };

        // Update preview on every tier change.
        tierRadio.ValueChanged += (s, e) =>
        {
            int idx = tierRadio.Value ?? 0;
            if (idx < 0 || idx >= count) return;
            modLabel.Text = FormatModifiers(tiers[idx]);
            descLabel.Text = tiers[idx].Description;
        };

        // Permadeath is universal now; old Hardcore checkbox removed but Y anchor kept as spacer.
        int hcY = previewY + 5;
        var permadeathNotice = new Label
        {
            Text = "All runs are permadeath — death deletes your save.",
            X = Pos.Center(), Y = hcY,
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DangerName,
        };

        // ── Run Modifiers row — unlocked after first F100 victory.
        int modY = hcY + 2;
        bool modifiersUnlocked = ProfileData.HasCompletedGame;
        var modifierBtn = new Button
        {
            Text = modifiersUnlocked ? " Run Modifiers " : " Run Modifiers (locked) ",
            X = Pos.Center() - 26, Y = modY,
            SchemeName = modifiersUnlocked ? ColorSchemes.MenuButtonName : ColorSchemes.DimName,
            Enabled = modifiersUnlocked,
        };
        var modifierChipLabel = new Label
        {
            Text = FormatModifierChips(),
            X = Pos.Center() + 0, Y = modY,
            Width = 40, SchemeName = ColorSchemes.DimName,
        };
        modifierBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
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
            SchemeName = ColorSchemes.MenuButtonName,
        };
        var detailsBtn = new Button
        {
            Text = " Details ", X = Pos.Center() - 3, Y = btnY,
            SchemeName = ColorSchemes.MenuButtonName,
        };
        var backBtn = new Button
        {
            Text = " Back ", X = Pos.Center() + 11, Y = btnY,
            SchemeName = ColorSchemes.MenuButtonName,
        };

        var hint = new Label
        {
            Text = "Up/Down: select   Enter: continue   Esc: back",
            X = Pos.Center(), Y = btnY + 2,
            Width = Dim.Auto(), SchemeName = ColorSchemes.DimName,
        };

        foreach (var btn in new[] { continueBtn, detailsBtn, backBtn })
            btn.HasFocusChanged += (s, e) => { if (s is Button b) b.IsDefault = e.NewValue; };

        detailsBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            int sel = tierRadio.Value ?? 0;
            DialogHelper.Query($"{tiers[sel].Name} — Details",
                DifficultyData.GetStatsTooltip(sel), "OK");
        };

        continueBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            CharacterCreationScreen.Show(mainWindow, tierRadio.Value ?? 0);
        };

        backBtn.Accepting += (s, e) => { e.Handled = true; TitleScreen.Show(mainWindow); };

        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { TitleScreen.Show(mainWindow); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;

        // ── Arrow nav ── RadioGroup eats Up/Down; intercept at boundary (Down on last → checkbox,
        // Up on first → buttons). Enter on tier list fires Continue.
        tierRadio.KeyDown += (s, e) =>
        {
            if (e.KeyCode is KeyCode.CursorDown or KeyCode.S
                && tierRadio.Value == count - 1)
            {
                modifierBtn.SetFocus(); e.Handled = true;
            }
            else if (e.KeyCode is KeyCode.CursorUp or KeyCode.W
                && tierRadio.Value == 0)
            {
                backBtn.SetFocus(); e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.Enter)
            {
                continueBtn.InvokeCommand(Command.HotKey);
                e.Handled = true;
            }
        };

        // Run Modifiers button ↔ tier list / footer row (replaces the old
        // hardcore checkbox in the nav chain).
        NavigationHelper.WireUpDown(modifierBtn, tierRadio, continueBtn);

        // Footer buttons: Up→modifier row, Down→tier list, Left/Right between them
        var footerBtns = new[] { continueBtn, detailsBtn, backBtn };
        for (int i = 0; i < footerBtns.Length; i++)
        {
            int li = (i - 1 + footerBtns.Length) % footerBtns.Length;
            int ri = (i + 1) % footerBtns.Length;
            NavigationHelper.WireUpDown(footerBtns[i], modifierBtn, tierRadio);
            NavigationHelper.WireLeftRight(footerBtns[i], footerBtns[li], footerBtns[ri]);
        }

        // ── Assemble (all centered) ──────────────────────────────────
        mainWindow.Add(header, headerRule,
            tierRadio, tierModRule,
            modHeader, modLabel, modDescRule, descLabel,
            permadeathNotice,
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

}
