using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Run Modifier picker — tier-grouped checkbox grid, live score multiplier preview,
// writes RunModifiers.Active on Apply. Gated behind F100 clear in DifficultyScreen.
public static class ModifierSelectScreen
{
    public static void Show(Window mainWindow, Action onApplied)
    {
        mainWindow.RemoveAll();
        DebugLogger.LogScreen("ModifierSelectScreen");

        var (header, headerRule) = ScreenHeader.Create("Run Modifiers", 1, 24);

        var tierOrder = new[]
        {
            (ModifierTier.Easy,      "Easy"),
            (ModifierTier.Moderate,  "Moderate"),
            (ModifierTier.Hard,      "Hard"),
            (ModifierTier.Nightmare, "Nightmare"),
        };

        // ── Build checkbox grid, grouped by tier ─────────────────────
        // Compact label on checkbox; full desc in focus-driven detail panel (avoids overflow on narrow terminals).
        var checkboxes = new Dictionary<RunModifier, CheckBox>();
        var defsByCheckbox = new Dictionary<CheckBox, ModifierDef>();
        int y = 4;
        const int LabelX = 4;       // left margin rather than center — predictable width
        const int LabelWidth = 44;  // fits on any 60+ column terminal
        var views = new List<View> { header, headerRule };

        foreach (var (tier, tierLabel) in tierOrder)
        {
            var tierHeader = new Label
            {
                Text = $"[ {tierLabel} tier ]",
                X = LabelX, Y = y,
                Width = LabelWidth, ColorScheme = ColorSchemes.Gold,
            };
            views.Add(tierHeader);
            y++;

            foreach (var kv in RunModifiers.Definitions)
            {
                var def = kv.Value;
                if (def.Tier != tier) continue;

                // Short label: " Name (×1.15)" — always fits.
                var check = new CheckBox
                {
                    Text = $" {def.Name} (×{def.ScoreMultiplier:F2})",
                    X = LabelX, Y = y,
                    Width = LabelWidth,
                    CheckedState = RunModifiers.IsActive(def.Id)
                        ? CheckState.Checked : CheckState.UnChecked,
                    ColorScheme = tier switch
                    {
                        ModifierTier.Nightmare => ColorSchemes.Danger,
                        ModifierTier.Hard      => ColorSchemes.Gold,
                        _                       => ColorSchemes.Body,
                    },
                };
                checkboxes[def.Id] = check;
                defsByCheckbox[check] = def;
                views.Add(check);
                y++;
            }
            y++;
        }

        // ── Detail panel (right side) — updates on focus change ──────
        var detailHeader = new Label
        {
            Text = "Details",
            X = LabelX + LabelWidth + 2, Y = 4,
            Width = Dim.Fill(2), ColorScheme = ColorSchemes.Gold,
        };
        var detailName = new Label
        {
            Text = "",
            X = LabelX + LabelWidth + 2, Y = 6,
            Width = Dim.Fill(2), ColorScheme = ColorSchemes.Body,
        };
        var detailTier = new Label
        {
            Text = "",
            X = LabelX + LabelWidth + 2, Y = 7,
            Width = Dim.Fill(2), ColorScheme = ColorSchemes.Dim,
        };
        var detailDesc = new Label
        {
            Text = "Select a modifier to see its effect.",
            X = LabelX + LabelWidth + 2, Y = 9,
            Width = Dim.Fill(2), Height = 8,
            ColorScheme = ColorSchemes.Body,
        };
        views.Add(detailHeader);
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
                detailDesc.Text = WrapText(captured.Description, 34);
            };
        }

        // ── Live score multiplier preview ────────────────────────────
        var scoreLabel = new Label
        {
            Text = "",
            X = LabelX, Y = y,
            Width = LabelWidth, ColorScheme = ColorSchemes.Gold,
        };
        void RefreshScore()
        {
            // Temporarily apply selections so TotalScoreMultiplier reflects them.
            var before = new HashSet<RunModifier>(RunModifiers.Active);
            RunModifiers.Active.Clear();
            foreach (var (mod, cb) in checkboxes)
                if (cb.CheckedState == CheckState.Checked) RunModifiers.Active.Add(mod);
            double mul = RunModifiers.TotalScoreMultiplier();
            int count = RunModifiers.Active.Count;
            scoreLabel.Text = $"Active: {count}    Score multiplier: ×{mul:F2}";
            RunModifiers.Active = before;  // restore until Apply
        }
        foreach (var cb in checkboxes.Values)
            cb.CheckedStateChanged += (s, e) => RefreshScore();
        RefreshScore();
        views.Add(scoreLabel);
        y += 2;

        // ── Buttons (bottom row, left-anchored with detail-pane clear) ──
        var applyBtn = new Button
        {
            Text = " Apply ", X = LabelX, Y = Pos.AnchorEnd(2),
            IsDefault = true, ColorScheme = ColorSchemes.MenuButton,
        };
        var clearBtn = new Button
        {
            Text = " Clear All ", X = LabelX + 12, Y = Pos.AnchorEnd(2),
            ColorScheme = ColorSchemes.MenuButton,
        };
        var cancelBtn = new Button
        {
            Text = " Cancel ", X = LabelX + 26, Y = Pos.AnchorEnd(2),
            ColorScheme = ColorSchemes.MenuButton,
        };

        applyBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            RunModifiers.Active.Clear();
            foreach (var (mod, cb) in checkboxes)
                if (cb.CheckedState == CheckState.Checked) RunModifiers.Active.Add(mod);
            onApplied();
        };

        clearBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            foreach (var cb in checkboxes.Values) cb.CheckedState = CheckState.UnChecked;
            RefreshScore();
        };

        cancelBtn.Accepting += (s, e) => { e.Cancel = true; onApplied(); };

        views.Add(applyBtn);
        views.Add(clearBtn);
        views.Add(cancelBtn);

        var hint = new Label
        {
            Text = "Space: toggle   Tab: next   Enter: Apply   Esc: Cancel",
            X = LabelX, Y = Pos.AnchorEnd(1), Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Dim,
        };
        views.Add(hint);

        mainWindow.Add(views.ToArray());

        mainWindow.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { onApplied(); e.Handled = true; }
        };
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
