using Terminal.Gui;

namespace SAOTRPG.UI;

public static class DifficultyScreen
{
    // ── Modifier data — display-only until backend difficulty system exists ──
    private static readonly string[] DifficultyNames =
        { "Story", "Very Easy", "Easy", "Normal", "Hard", "Very Hard", "Masochist", "Unwinnable" };

    private static readonly string[] DifficultyDescriptions =
    {
        "Sit back and enjoy the tale of Aincrad. Combat is an afterthought.",
        "A carefree stroll through Aincrad. Enemies are weak, danger is minimal.",
        "A relaxed adventure. Reduced enemy stats, forgiving combat.",
        "The intended experience. Balanced risk and reward.",
        "Tougher enemies, less room for error.",
        "Punishing combat. Every mistake costs you.",
        "You asked for this. No mercy. No regrets.",
        "You will not make it past Floor 1. Don't kid yourself."
    };

    private static readonly string[] DifficultyModifiers =
    {
        // Story
        "  Enemy HP:  -50%\n  Enemy ATK: -50%\n  Enemy DEF: -50%\n\n  EXP Gain:  +100%\n  Col Gain:  +100%",
        // Very Easy
        "  Enemy HP:  -30%\n  Enemy ATK: -30%\n  Enemy DEF: -30%\n\n  EXP Gain:  +50%\n  Col Gain:  +50%",
        // Easy
        "  Enemy HP:  -15%\n  Enemy ATK: -15%\n  Enemy DEF: -15%\n\n  EXP Gain:  +25%\n  Col Gain:  +25%",
        // Normal
        "  No modifiers.\n\n  The intended experience.\n  All stats at base values.",
        // Hard
        "  Enemy HP:  +25%\n  Enemy ATK: +25%\n  Enemy DEF: +25%\n\n  EXP Gain:  +25%\n  Col Gain:  +25%",
        // Very Hard
        "  Enemy HP:  +50%\n  Enemy ATK: +50%\n  Enemy DEF: +50%\n\n  EXP Gain:  +50%\n  Col Gain:  +50%",
        // Masochist
        "  Enemy HP:  +100%\n  Enemy ATK: +100%\n  Enemy DEF: +100%\n  Enemy SPD: +50%\n\n  EXP Gain:  +100%\n  Col Gain:  +100%",
        // Unwinnable
        "  Enemy HP:  +200%\n  Enemy ATK: +200%\n  Enemy DEF: +200%\n  Enemy SPD: +100%\n  Enemy CRIT: +50%\n\n  EXP Gain:  +200%\n  Col Gain:  +200%"
    };

    private const string DebugModifiers =
        "  Player HP: 99999\n  Player ATK: 999\n  Player DEF: 999\n\n  Enemies: 1 HP\n  EXP Gain:  +999%\n  Col Gain:  +999%";

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("DifficultyScreen.Show");
        DebugLogger.LogScreen("DifficultyScreen");

        // Build working copies (may include Debug)
        var names = new List<string>(DifficultyNames);
        var descriptions = new List<string>(DifficultyDescriptions);
        var modifiers = new List<string>(DifficultyModifiers);

        if (DebugMode.IsEnabled)
        {
            names.Add("Debug");
            descriptions.Add("Developer mode. All restrictions lifted.");
            modifiers.Add(DebugModifiers);
        }

        int count = names.Count;
        int selectedIndex = 3; // Default: Normal

        // ── Header ──────────────────────────────────────────────────────
        var header = new Label
        {
            Text = $"{Theme.DoubleRule(8)} Select Difficulty {Theme.DoubleRule(8)}",
            X = Pos.Center(), Y = 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };

        var subHeader = new Label
        {
            Text = $"  {Theme.SparkleOpen} Choose wisely — this cannot be changed later",
            X = Pos.Center(), Y = 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        // ── Selection frame ─────────────────────────────────────────────
        int frameHeight = count + 6;
        var selectionFrame = new FrameView
        {
            Title = $" {Theme.Sword} Difficulty ",
            X = Pos.Center(), Y = 4,
            Width = 58, Height = frameHeight,
            ColorScheme = Theme.FrameSubtle,
            BorderStyle = LineStyle.Rounded
        };

        // ── Description area (below rows, inside frame) ─────────────────
        var descDivider = new Label
        {
            Text = Theme.DottedRule(52),
            X = 1, Y = count + 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        var descLabel = new Label
        {
            Text = $"  {Theme.BulletOpen} {descriptions[selectedIndex]}",
            X = 1, Y = count + 2,
            Width = 54, Height = 2,
            ColorScheme = Theme.Subtitle
        };

        // ── Custom radio rows — indicator + name + [?] button ───────────
        var radioIndicators = new Label[count];
        var nameLabels = new Label[count];
        var helpButtons = new Button[count];

        void RefreshSelection()
        {
            for (int i = 0; i < count; i++)
            {
                bool sel = i == selectedIndex;
                radioIndicators[i].Text = sel ? $" {Theme.Diamond}" : $" {Theme.DiamondOpen}";
                radioIndicators[i].ColorScheme = sel ? Theme.Gold : Theme.Dim;
                nameLabels[i].ColorScheme = sel ? Theme.Title : Theme.Subtitle;
            }
            descLabel.Text = $"  {Theme.BulletOpen} {descriptions[selectedIndex]}";
        }

        for (int i = 0; i < count; i++)
        {
            int row = 1 + i;
            int idx = i;

            radioIndicators[i] = new Label
            {
                Text = i == selectedIndex ? $" {Theme.Diamond}" : $" {Theme.DiamondOpen}",
                X = 1, Y = row,
                Width = 3, Height = 1,
                ColorScheme = i == selectedIndex ? Theme.Gold : Theme.Dim
            };

            nameLabels[i] = new Label
            {
                Text = $" {names[i]}",
                X = 4, Y = row,
                Width = 16, Height = 1,
                ColorScheme = i == selectedIndex ? Theme.Title : Theme.Subtitle
            };

            helpButtons[i] = new Button
            {
                Text = " ? ",
                X = 38, Y = row,
                ColorScheme = Theme.SmallButton
            };

            // [?] popup — triggered by Space key, NOT Enter (Enter advances to next section)
            string modTitle = names[idx];
            string modBody = modifiers[idx];
            helpButtons[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                // Only show popup if triggered by Space (not Enter)
                // Enter is intercepted in KeyDown below
            };

            selectionFrame.Add(radioIndicators[i], nameLabels[i], helpButtons[i]);
        }

        selectionFrame.Add(descDivider, descLabel);

        // ── Hardcore modifier ───────────────────────────────────────────
        int hardcoreY = frameHeight + 5;
        var hardcoreDivider = new Label
        {
            Text = Theme.LightRule(40),
            X = Pos.Center(), Y = hardcoreY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        var hardcoreCheck = new CheckBox
        {
            Text = $" {Theme.Cross} Hardcore Mode  (one life only)",
            X = Pos.Center(), Y = hardcoreY + 1,
            Width = Dim.Auto(), Height = 1
        };

        var hardcoreDesc = new Label
        {
            Text = $"  {Theme.BulletOpen} Death is permanent. There are no second chances.",
            X = Pos.Center(), Y = hardcoreY + 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Error
        };

        // Popup when certain combos are active
        int unwinnableIndex = names.IndexOf("Unwinnable");
        hardcoreCheck.CheckedStateChanging += (s, e) =>
        {
            bool togglingOn = e.NewValue == CheckState.Checked;
            if (togglingOn && selectedIndex == 0)
                MessageBox.Query("Really?", "Really? Hardcore and Story mode?", "Yes, really");
            else if (togglingOn && selectedIndex == unwinnableIndex)
                MessageBox.Query("Good Luck", "There's no way you can do this, but good luck.", "Bring it on");
        };

        // ── Navigation buttons ──────────────────────────────────────────
        int btnY = hardcoreY + 4;
        var continueBtn = new Button
        {
            Text = $" {Theme.TriRight} Continue  ",
            X = Pos.Center(), Y = btnY,
            IsDefault = false,
            ColorScheme = Theme.MenuButton
        };
        var backBtn = new Button
        {
            Text = $" {Theme.ArrowLeft} Back      ",
            X = Pos.Center(), Y = Pos.Bottom(continueBtn) + 1,
            ColorScheme = Theme.MenuButton
        };

        Theme.AttachDiamondFocus(continueBtn, backBtn);

        continueBtn.Accepting += (s, e) =>
        {
            // TODO: pass selectedDifficulty and isHardcore through to GameScreen
            CharacterCreationScreen.Show(mainWindow);
            e.Cancel = true;
        };

        backBtn.Accepting += (s, e) => { TitleScreen.Show(mainWindow); e.Cancel = true; };

        // ── Controls hint ───────────────────────────────────────────────
        var controlsHint = new Label
        {
            Text = $"[{Theme.ArrowUp}/{Theme.ArrowDown} or W/S] Navigate    [Space] View Modifiers    [Enter] Select",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        mainWindow.Add(header, subHeader, selectionFrame, hardcoreDivider,
            hardcoreCheck, hardcoreDesc, continueBtn, backBtn, controlsHint);

        // ── Keyboard navigation — per [?] button ────────────────────────
        for (int i = 0; i < count; i++)
        {
            int idx = i;
            string mTitle = names[i];
            string mBody = modifiers[i];

            helpButtons[i].KeyDown += (s, e) =>
            {
                switch (e.KeyCode)
                {
                    case KeyCode.W: case KeyCode.CursorUp:
                        if (idx > 0)
                        {
                            selectedIndex = idx - 1;
                            RefreshSelection();
                            helpButtons[idx - 1].SetFocus();
                        }
                        else
                        {
                            backBtn.SetFocus();
                        }
                        e.Handled = true;
                        break;

                    case KeyCode.S: case KeyCode.CursorDown:
                        if (idx < count - 1)
                        {
                            selectedIndex = idx + 1;
                            RefreshSelection();
                            helpButtons[idx + 1].SetFocus();
                        }
                        else
                        {
                            hardcoreCheck.SetFocus();
                        }
                        e.Handled = true;
                        break;

                    case KeyCode.Enter:
                        // Enter advances to hardcore checkbox, does NOT open [?] popup
                        hardcoreCheck.SetFocus();
                        e.Handled = true;
                        break;

                    case KeyCode.Space:
                        // Space opens the modifier popup for this row
                        ShowModifierPopup(mTitle, mBody);
                        e.Handled = true;
                        break;
                }
            };
        }

        hardcoreCheck.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    helpButtons[count - 1].SetFocus();
                    e.Handled = true;
                    break;
                case KeyCode.S: case KeyCode.CursorDown:
                    continueBtn.SetFocus();
                    e.Handled = true;
                    break;
                case KeyCode.Enter:
                    hardcoreCheck.CheckedState = hardcoreCheck.CheckedState == CheckState.Checked
                        ? CheckState.UnChecked : CheckState.Checked;
                    e.Handled = true;
                    break;
            }
        };

        continueBtn.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    hardcoreCheck.SetFocus();
                    e.Handled = true;
                    break;
                case KeyCode.S: case KeyCode.CursorDown:
                    backBtn.SetFocus();
                    e.Handled = true;
                    break;
            }
        };

        backBtn.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    continueBtn.SetFocus();
                    e.Handled = true;
                    break;
                case KeyCode.S: case KeyCode.CursorDown:
                    selectedIndex = 0;
                    RefreshSelection();
                    helpButtons[0].SetFocus();
                    e.Handled = true;
                    break;
            }
        };

        // Start focused on the default selection's [?] row
        helpButtons[selectedIndex].SetFocus();
        DebugLogger.EndTimer("DifficultyScreen.Show", sw);
    }

    // ── Modifier popup — themed dialog showing stat changes ─────────────
    private static void ShowModifierPopup(string difficultyName, string modifierBody)
    {
        var dialog = new Dialog
        {
            Title = $" {Theme.DiamondOpen} {difficultyName} — Modifiers ",
            Width = 42, Height = 16,
            ColorScheme = Theme.FrameSubtle
        };

        var headerRule = new Label
        {
            Text = Theme.HeavyRule(36),
            X = 1, Y = 0,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };

        var body = new Label
        {
            Text = modifierBody,
            X = 1, Y = 1,
            Width = 38, Height = 10,
            ColorScheme = Theme.Body
        };

        var footerRule = new Label
        {
            Text = Theme.HeavyRule(36),
            X = 1, Y = Pos.AnchorEnd(3),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };

        dialog.Add(headerRule, body, footerRule);

        var closeBtn = new Button { Text = $" {Theme.ArrowLeft} Close ", IsDefault = true, ColorScheme = Theme.MenuButton };
        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };
        dialog.AddButton(closeBtn);

        Application.Run(dialog);
        dialog.Dispose();
    }
}
