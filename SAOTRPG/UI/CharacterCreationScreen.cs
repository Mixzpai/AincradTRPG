using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Character creation — identity + per-stat +/- allocation + live combat preview + Continue/Back.
// Tab cycles identity → stat buttons → footer; Up/Down moves between stat rows for fast allocation.
public static class CharacterCreationScreen
{
    private const int MaxNameLength = 15;
    private const int StartingSkillPoints = 10;
    private static EventHandler<Key>? _escHandler;

    private static readonly (string Name, string Effect)[] Stats =
    {
        ("Vitality",     "+10 Max HP"),
        ("Strength",     "+2 Attack"),
        ("Endurance",    "+2 Defense"),
        ("Dexterity",    "+Crit Rate"),
        ("Agility",      "+2 Speed"),
        ("Intelligence", "+2 Skill Dmg"),
    };

    private static readonly string[] GenderOptions = { "Male", "Female" };

    // Unhook before transitioning AWAY so Esc→DifficultyScreen doesn't leak into GameScreen.
    public static void UnhookEscHandler(Window mainWindow)
    {
        if (_escHandler != null) { mainWindow.KeyDown -= _escHandler; _escHandler = null; }
    }

    public static void Show(Window mainWindow, int difficulty = 3)
    {
        mainWindow.RemoveAll();
        if (_escHandler != null) mainWindow.KeyDown -= _escHandler;
        DifficultyScreen.UnhookEscHandler(mainWindow);
        var sw = DebugLogger.StartTimer("CharacterCreationScreen.Show");
        DebugLogger.LogScreen("CharacterCreationScreen");

        int skillPoints = StartingSkillPoints;
        int[] allocated = new int[Stats.Length];

        // ── Header ───────────────────────────────────────────────────
        var (header, headerRule) = ScreenHeader.Create("Create Your Character", 1, 24);

        // ── Identity section (centered) ── Label(14) right-aligned, 3-gap, TextField.
        // inputCol anchors to label right + gap → no overlap at any width.
        int labelW = 14, gap = 3, inputW = 22;
        Pos leftCol = Pos.Center() - (labelW + gap + inputW) / 2;

        var firstLabel = ScreenHeader.FormLabel("First Name:", leftCol, 4, labelW);
        var firstField = new TextField { X = Pos.Right(firstLabel) + gap, Y = 4, Width = inputW };
        var firstHint  = new Label { Text = $"(max {MaxNameLength})", X = Pos.Right(firstField) + 1, Y = 4, ColorScheme = ColorSchemes.Dim };

        var lastLabel = ScreenHeader.FormLabel("Last Name:", leftCol, 6, labelW);
        var lastField = new TextField { X = Pos.Right(lastLabel) + gap, Y = 6, Width = inputW };
        var lastHint  = new Label { Text = $"(max {MaxNameLength})", X = Pos.Right(lastField) + 1, Y = 6, ColorScheme = ColorSchemes.Dim };

        var genderLabel = ScreenHeader.FormLabel("Gender:", leftCol, 8, labelW);
        var genderRadio = new RadioGroup
        {
            X = Pos.Right(genderLabel) + gap, Y = 8, RadioLabels = GenderOptions,
            Orientation = Orientation.Horizontal, SelectedItem = 0,
        };

        // ── Stats section ────────────────────────────────────────────
        var statsHeader = ScreenHeader.Section("Allocate Stats", 11);
        var pointsLabel = new Label
        {
            Text = FormatPointsRemaining(skillPoints),
            X = Pos.Center(), Y = 12, Width = Dim.Auto(), ColorScheme = ColorSchemes.Gold,
        };

        int statY0 = 14;
        var valueLabels = new Label[Stats.Length];
        var plusBtns  = new Button[Stats.Length];
        var minusBtns = new Button[Stats.Length];

        // Refresh all value labels + preview after any +/- press.
        Label previewLabel = null!; // forward-declared, assigned below
        Label feedbackLabel = null!;

        void Refresh()
        {
            pointsLabel.Text = FormatPointsRemaining(skillPoints);
            pointsLabel.ColorScheme = skillPoints == 0 ? ColorSchemes.Dim : ColorSchemes.Gold;
            for (int j = 0; j < Stats.Length; j++)
            {
                valueLabels[j].Text = $"{allocated[j],2}";
                valueLabels[j].ColorScheme = allocated[j] > 0 ? ColorSchemes.Gold : ColorSchemes.Body;
            }
            previewLabel.Text = FormatPreview(allocated);
        }

        // Build one row per stat.
        for (int i = 0; i < Stats.Length; i++)
        {
            int idx = i;
            int rowY = statY0 + i;

            mainWindow.Add(new Label
            {
                Text = $"{Stats[i].Name,-14}", X = leftCol, Y = rowY,
                ColorScheme = ColorSchemes.Body,
            });

            valueLabels[i] = new Label
            {
                Text = $" 0", X = leftCol + 15, Y = rowY, Width = 3,
                ColorScheme = ColorSchemes.Body,
            };

            plusBtns[i] = new Button
            {
                Text = " + ", X = leftCol + 19, Y = rowY,
                NoPadding = true, ColorScheme = ColorSchemes.Button,
            };
            minusBtns[i] = new Button
            {
                Text = " - ", X = leftCol + 24, Y = rowY,
                NoPadding = true, ColorScheme = ColorSchemes.Button,
            };

            mainWindow.Add(new Label
            {
                Text = Stats[i].Effect, X = leftCol + 29, Y = rowY,
                ColorScheme = ColorSchemes.Dim,
            });

            plusBtns[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (skillPoints <= 0) { ShowMsg(feedbackLabel, "No points left!", ColorSchemes.Danger); return; }
                allocated[idx]++;
                skillPoints--;
                ShowMsg(feedbackLabel, $"+1 {Stats[idx].Name}", ColorSchemes.Gold);
                Refresh();
            };

            minusBtns[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (allocated[idx] <= 0) { ShowMsg(feedbackLabel, "Already at zero.", ColorSchemes.Dim); return; }
                allocated[idx]--;
                skillPoints++;
                ShowMsg(feedbackLabel, $"-1 {Stats[idx].Name}", ColorSchemes.Dim);
                Refresh();
            };

            mainWindow.Add(valueLabels[i], plusBtns[i], minusBtns[i]);
        }

        // ── Preview section ──────────────────────────────────────────
        int previewY = statY0 + Stats.Length + 1;
        var previewHdr = ScreenHeader.Section("Preview", previewY);
        previewLabel = new Label
        {
            Text = FormatPreview(allocated),
            X = Pos.Center(), Y = previewY + 1,
            Width = Dim.Auto(), Height = 2, ColorScheme = ColorSchemes.Body,
        };

        feedbackLabel = new Label
        {
            Text = "", X = Pos.Center(), Y = previewY + 4,
            Width = Dim.Auto(), ColorScheme = ColorSchemes.Gold,
        };

        // ── Footer ───────────────────────────────────────────────────
        int footerY = previewY + 6;
        var continueBtn = new Button
        {
            Text = " Continue ", X = Pos.Center() - 12, Y = footerY,
            ColorScheme = ColorSchemes.MenuButton,
        };
        var backBtn = new Button
        {
            Text = " Back ", X = Pos.Center() + 4, Y = footerY,
            ColorScheme = ColorSchemes.MenuButton,
        };

        var navHint = new Label
        {
            Text = "Tab: next field   Up/Down: stat rows   Enter: press button   Esc: back",
            X = Pos.Center(), Y = footerY + 2, Width = Dim.Auto(), ColorScheme = ColorSchemes.Dim,
        };

        foreach (var btn in new[] { continueBtn, backBtn })
            btn.HasFocusChanged += (s, e) => { if (s is Button b) b.IsDefault = e.NewValue; };

        continueBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var firstName = firstField.Text?.Trim() ?? "";
            var lastName  = lastField.Text?.Trim()  ?? "";
            int gIdx = genderRadio.SelectedItem;
            string gender = gIdx >= 0 && gIdx < GenderOptions.Length ? GenderOptions[gIdx] : "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                ShowMsg(feedbackLabel, "Please enter both names.", ColorSchemes.Danger);
                return;
            }
            if (firstName.Length > MaxNameLength || lastName.Length > MaxNameLength)
            {
                ShowMsg(feedbackLabel, $"Names must be {MaxNameLength} chars or less.", ColorSchemes.Danger);
                return;
            }
            if (skillPoints > 0)
            {
                int confirm = MessageBox.Query("Unspent Points",
                    $"You have {skillPoints} skill point(s) remaining.\nContinue anyway?",
                    "Go Back", "Continue");
                if (confirm != 1) return;
            }

            int slot = Dialogs.SaveSlotDialog.ShowNewGameSlotPicker();
            if (slot < 1) return;

            var tempLog = new StringGameLog(new System.Text.StringBuilder());
            var player = Player.CreateNewPlayer(firstName, lastName, gender,
                tempLog, new TerminalGuiInventoryLogger(tempLog));

            for (int i = 0; i < Stats.Length; i++)
                if (allocated[i] > 0) player.SpendSkillPoints(Stats[i].Name, allocated[i]);

            GameScreen.Show(mainWindow, player, difficulty, saveSlot: slot);
        };

        backBtn.Accepting += (s, e) => { e.Cancel = true; DifficultyScreen.Show(mainWindow); };

        // ── Arrow key navigation ─────────────────────────────────────
        // Full vertical chain: names → gender → stats → footer → wrap.
        NavigationHelper.WireUpDown(firstField, continueBtn, lastField);
        NavigationHelper.WireUpDown(lastField, firstField, genderRadio);
        NavigationHelper.WireUpDown(genderRadio, lastField, plusBtns[0]);

        for (int i = 0; i < Stats.Length; i++)
        {
            View upPlus  = i == 0 ? genderRadio : plusBtns[i - 1];
            View upMinus = i == 0 ? genderRadio : minusBtns[i - 1];
            View dnPlus  = i == Stats.Length - 1 ? continueBtn : plusBtns[i + 1];
            View dnMinus = i == Stats.Length - 1 ? continueBtn : minusBtns[i + 1];
            WireStatNav(plusBtns[i],  upPlus,  dnPlus,  minusBtns[i]);
            WireStatNav(minusBtns[i], upMinus, dnMinus, plusBtns[i]);
        }

        NavigationHelper.WireUpDown(continueBtn, plusBtns[^1], firstField);
        NavigationHelper.WireLeftRight(continueBtn, backBtn, backBtn);
        NavigationHelper.WireUpDown(backBtn, minusBtns[^1], firstField);
        NavigationHelper.WireLeftRight(backBtn, continueBtn, continueBtn);

        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { DifficultyScreen.Show(mainWindow); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;

        // ── Assemble ─────────────────────────────────────────────────
        mainWindow.Add(header, headerRule,
            firstLabel, firstField, firstHint,
            lastLabel, lastField, lastHint,
            genderLabel, genderRadio,
            statsHeader, pointsLabel,
            previewHdr, previewLabel, feedbackLabel,
            continueBtn, backBtn, navHint);

        firstField.SetFocus();
        DebugLogger.EndTimer("CharacterCreationScreen.Show", sw);
    }

    private static void WireStatNav(Button btn, View up, View down, View sibling)
    {
        btn.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.CursorUp:   case KeyCode.W: up.SetFocus();      e.Handled = true; break;
                case KeyCode.CursorDown: case KeyCode.S: down.SetFocus();    e.Handled = true; break;
                case KeyCode.CursorLeft: case KeyCode.CursorRight:
                    sibling.SetFocus(); e.Handled = true; break;
            }
        };
    }

    private static void ShowMsg(Label label, string text, ColorScheme scheme)
    {
        label.Text = text;
        label.ColorScheme = scheme;
    }

    private static string FormatPointsRemaining(int pts) =>
        pts > 0 ? $"{pts} points remaining" : "All points allocated!";

    private static string FormatPreview(int[] a)
    {
        int hp = 100 + a[0] * 10;
        int atk = a[1] * 2, def = a[2] * 2, spd = a[4] * 2;
        int crit = 5 + a[3] / 2, cdmg = 10 + a[3];
        return $"HP: {hp}   ATK: {atk}   DEF: {def}   SPD: {spd}\n" +
               $"CRIT: {crit}%   CDMG: +{cdmg}   SDMG: {a[5] * 2}";
    }
}
