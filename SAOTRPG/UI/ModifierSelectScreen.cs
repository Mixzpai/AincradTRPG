using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Run Modifier picker — a tier-grouped checkbox card beside a detail card that follows focus,
// with the score multiplier beside the buttons it applies to. Tab moves between modifiers.
// Writes RunModifiers.Active on Apply. Gated behind an F100 clear in DifficultyScreen.
public static class ModifierSelectScreen
{
    // Card block geometry, matching DifficultyScreen so the two screens sit on the same grid.
    private const int ListCardWidth   = 50;
    private const int DetailCardWidth = 40;
    private const int CardGap         = 2;
    private const int TotalCardsWidth = ListCardWidth + CardGap + DetailCardWidth;

    // Columns the Apply / Clear All / Cancel row occupies, so the score readout can share that
    // row without their frames overlapping.
    private const int ButtonsWidth = 40;

    private static EventHandler<Key>? _escHandler;

    // Unhook before transitioning AWAY. The handler re-enters DifficultyScreen, so leaving it
    // attached means a later Esc — in character creation or mid-run — tears down the screen and
    // rebuilds the difficulty picker.
    public static void UnhookEscHandler(Window mainWindow)
    {
        if (_escHandler != null) { mainWindow.KeyDown -= _escHandler; _escHandler = null; }
    }

    public static void Show(Window mainWindow, Action onApplied)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        // Reached from DifficultyScreen, whose handler is still live and would send Esc to the
        // title screen alongside this screen's own return-to-difficulty.
        NavigationHelper.UnhookScreenEscHandlers(mainWindow);
        DebugLogger.LogScreen("ModifierSelectScreen");

        var (header, headerRule) = ScreenHeader.Create("Run Modifiers", 1, 24);

        var tierOrder = new[]
        {
            (ModifierTier.Easy,      "Easy"),
            (ModifierTier.Moderate,  "Moderate"),
            (ModifierTier.Hard,      "Hard"),
            (ModifierTier.Nightmare, "Nightmare"),
        };

        // ── Two cards, centred on the axis ───────────────────────────
        // This screen used to be the only one anchored to a left margin, so entering it shunted
        // the whole interface leftward. It now shares the axis with its siblings.
        var checkboxes = new Dictionary<RunModifier, CheckBox>();
        var defsByCheckbox = new Dictionary<CheckBox, ModifierDef>();

        Pos cardsLeft = ScreenHeader.Axis - TotalCardsWidth / 2;
        int listInner = ListCardWidth - Card.ChromeWidth;
        int detailInner = DetailCardWidth - Card.ChromeWidth;

        // Height the list needs: a header plus its rows per tier, and a blank row between tiers.
        int rows = tierOrder.Length * 2 - 1;   // a caption per tier, a blank row between tiers
        foreach (var (tier, _) in tierOrder)
            rows += RunModifiers.Definitions.Count(kv => kv.Value.Tier == tier);

        const int CardsTop = 4;
        Pos detailLeft = cardsLeft + ListCardWidth + CardGap;
        var listCard = new Card("MODIFIERS", cardsLeft, CardsTop, ListCardWidth, rows + Card.ChromeHeight);
        var detailCard = new Card("DETAILS", detailLeft, CardsTop,
            DetailCardWidth, rows + Card.ChromeHeight);
        // Frames first so they paint under their content, which is added as siblings — nesting
        // controls inside a card traps Tab traversal, and Tab is how this screen is driven.
        var views = new List<View> { header, headerRule, listCard, detailCard };

        int y = Card.InsideY(CardsTop);
        foreach (var (tier, tierLabel) in tierOrder)
        {
            var (caption, rule) = ScreenHeader.Section(tierLabel, Card.InsideX(cardsLeft), y, listInner);
            views.Add(caption);
            views.Add(rule);
            y++;

            foreach (var kv in RunModifiers.Definitions)
            {
                var def = kv.Value;
                if (def.Tier != tier) continue;

                // Short label: " Name (×1.15)" — always fits.
                // Tier is carried by the section header and the detail pane, not by recolouring
                // every row: two saturated hues in one list makes the accent mean nothing.
                var check = new CheckBox
                {
                    Text = $" {def.Name} (×{def.ScoreMultiplier:F2})",
                    X = Card.InsideX(cardsLeft), Y = y,
                    Width = listInner,
                    Value = RunModifiers.IsActive(def.Id)
                        ? CheckState.Checked : CheckState.UnChecked,
                    SchemeName = ColorSchemes.BodyName,
                };
                checkboxes[def.Id] = check;
                defsByCheckbox[check] = def;
                views.Add(check);
                y++;
            }
            // Blank row between tier groups, but not after the last one.
            if (tier != tierOrder[^1].Item1) y++;
        }

        // ── Detail card — updates on focus change ────────────────────
        var detailName = new Label
        {
            Text = "", X = Card.InsideX(detailLeft), Y = Card.InsideY(CardsTop),
            Width = detailInner, Height = 1, SchemeName = ColorSchemes.TitleName,
        };
        var detailTier = new Label
        {
            Text = "", X = Card.InsideX(detailLeft), Y = Card.InsideY(CardsTop) + 1,
            Width = detailInner, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        var detailDesc = new Label
        {
            Text = "Select a modifier to see its effect.",
            X = Card.InsideX(detailLeft), Y = Card.InsideY(CardsTop) + 3,
            Width = detailInner, Height = 8, SchemeName = ColorSchemes.BodyName,
        };
        views.Add(detailName);
        views.Add(detailTier);
        views.Add(detailDesc);

        foreach (var (check, def) in defsByCheckbox)
        {
            var captured = def;
            check.HasFocusChanged += (s, e) =>
            {
                if (!e.NewValue) return;
                detailName.Text = captured.Name;
                detailTier.Text = $"{captured.Tier} tier · score ×{captured.ScoreMultiplier:F2}";
                detailDesc.Text = WrapText(captured.Description, detailInner);
            };
        }

        // ── Score readout — the point of the screen, so it gets its own line ──
        int footerY = CardsTop + rows + Card.ChromeHeight + 1;
        var scoreLabel = new Label
        {
            Text = "", X = cardsLeft + ButtonsWidth, Y = footerY,
            Width = TotalCardsWidth - ButtonsWidth, Height = 1,
            TextAlignment = Alignment.End,
            SchemeName = ColorSchemes.GoldName,
        };
        void RefreshScore()
        {
            // Temporarily apply selections so TotalScoreMultiplier reflects them.
            var before = new HashSet<RunModifier>(RunModifiers.Active);
            RunModifiers.Active.Clear();
            foreach (var (mod, cb) in checkboxes)
                if (cb.Value == CheckState.Checked) RunModifiers.Active.Add(mod);
            double mul = RunModifiers.TotalScoreMultiplier();
            int active = RunModifiers.Active.Count;
            scoreLabel.Text = $"{active} active      score ×{mul:F2}";
            RunModifiers.Active = before;  // restore until Apply
        }
        foreach (var cb in checkboxes.Values)
            cb.ValueChanged += (s, e) => RefreshScore();
        RefreshScore();
        views.Add(scoreLabel);

        // ── Buttons (bottom row, left-anchored with detail-pane clear) ──
        var applyBtn = DialogHelper.CreateMenuButton("Apply");
        applyBtn.X = cardsLeft; applyBtn.Y = footerY;
        var clearBtn = DialogHelper.CreateMenuButton("Clear All");
        clearBtn.X = Pos.Right(applyBtn) + 3; clearBtn.Y = footerY;
        var cancelBtn = DialogHelper.CreateMenuButton("Cancel");
        cancelBtn.X = Pos.Right(clearBtn) + 3; cancelBtn.Y = footerY;

        void Apply()
        {
            RunModifiers.Active.Clear();
            foreach (var (mod, cb) in checkboxes)
                if (cb.Value == CheckState.Checked) RunModifiers.Active.Add(mod);
            onApplied();
        }

        applyBtn.Accepting += (s, e) => { e.Handled = true; Apply(); };

        clearBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            foreach (var cb in checkboxes.Values) cb.Value = CheckState.UnChecked;
            RefreshScore();
        };

        cancelBtn.Accepting += (s, e) => { e.Handled = true; onApplied(); };

        views.Add(applyBtn);
        views.Add(clearBtn);
        views.Add(cancelBtn);

        // Four pairs rather than three: Tab is how this screen is navigated, so omitting it
        // leaves the player with no stated way to move between modifiers.
        var hintPairs = new[] { ("tab", "next"), ("space", "toggle"), ("enter", "apply"), ("esc", "cancel") };
        views.AddRange(ScreenHeader.KeyHints(cardsLeft, footerY + 1, hintPairs));

        mainWindow.Add(views.ToArray());

        // Stored rather than inline so it can be detached — an anonymous lambda has no
        // reference to unsubscribe with, which is how this outlived the screen.
        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { onApplied(); e.Handled = true; }
            else if (e.KeyCode == KeyCode.Enter) { Apply(); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;
    }

    // Simple word-wrap for the detail panel description.
    private static string WrapText(string text, int maxWidth)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var cur = new System.Text.StringBuilder();
        foreach (var w in words)
        {
            if (cur.Length + w.Length + 1 > maxWidth)
            {
                lines.Add(cur.ToString());
                cur.Clear();
            }
            if (cur.Length > 0) cur.Append(' ');
            cur.Append(w);
        }
        if (cur.Length > 0) lines.Add(cur.ToString());
        return string.Join("\n", lines);
    }
}
