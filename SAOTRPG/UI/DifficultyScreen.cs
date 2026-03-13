using Terminal.Gui;

namespace SAOTRPG.UI;

public static class DifficultyScreen
{
    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("DifficultyScreen.Show");
        DebugLogger.LogScreen("DifficultyScreen");

        var header = new Label { Text = "=== Select Difficulty ===", X = Pos.Center(), Y = 2, Width = Dim.Auto(), Height = 1 };

        // Difficulty tiers — index-matched with descriptions below
        var difficultyList = new List<string> { "Story", "Very Easy", "Easy", "Normal", "Hard", "Very Hard", "Masochist", "Unwinnable" };
        var descriptionList = new List<string>
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

        // Debug difficulty — only visible when launched with --debug
        if (DebugMode.IsEnabled)
        {
            difficultyList.Add("Debug");
            descriptionList.Add("Developer mode. All restrictions lifted.");
        }

        var difficulties = difficultyList.ToArray();
        var descriptions = descriptionList.ToArray();
        int count = difficulties.Length;

        // Radio selector — defaults to Normal
        var difficultyRadio = new RadioGroup { X = Pos.Center(), Y = 5, RadioLabels = difficulties, Width = 20, Height = count, SelectedItem = 3 };

        // Dynamic description — updates when radio selection changes
        int descY = 5 + count + 1;
        var descLabel = new Label { Text = descriptions[3], X = Pos.Center(), Y = descY, Width = 55, Height = 2 };
        difficultyRadio.SelectedItemChanged += (s, e) => { descLabel.Text = descriptions[e.SelectedItem]; };

        int dividerY = descY + 3;
        var divider = new Label { Text = "─────────────────────────────────", X = Pos.Center(), Y = dividerY, Width = Dim.Auto(), Height = 1 };

        // Hardcore toggle — permadeath modifier, applies on top of any difficulty
        int hardcoreY = dividerY + 2;
        var hardcoreCheck = new CheckBox { Text = " Hardcore Mode  (one life only)", X = Pos.Center(), Y = hardcoreY, Width = Dim.Auto(), Height = 1 };
        var hardcoreDesc = new Label { Text = "  Death is permanent. There are no second chances.", X = Pos.Center(), Y = hardcoreY + 1, Width = Dim.Auto(), Height = 1 };

        // Popup when Unwinnable + Hardcore are both active
        // Find the index of Unwinnable (always second-to-last when debug is on, last otherwise)
        int unwinnableIndex = difficultyList.IndexOf("Unwinnable");
        hardcoreCheck.CheckedStateChanging += (s, e) =>
        {
            bool togglingOn = e.NewValue == CheckState.Checked;
            if (togglingOn && difficultyRadio.SelectedItem == 0)
            {
                MessageBox.Query("Really?", "Really? Hardcore and Story mode?", "Yes, really");
            }
            else if (togglingOn && difficultyRadio.SelectedItem == unwinnableIndex)
            {
                MessageBox.Query("Good Luck", "There's no way you can do this, but good luck.", "Bring it on");
            }
        };

        difficultyRadio.SelectedItemChanged += (s, e) =>
        {
            if (hardcoreCheck.CheckedState != CheckState.Checked) return;

            if (e.SelectedItem == 0)
            {
                MessageBox.Query("Really?", "Really? Hardcore and Story mode?", "Yes, really");
            }
            else if (e.SelectedItem == unwinnableIndex)
            {
                MessageBox.Query("Good Luck", "There's no way you can do this, but good luck.", "Bring it on");
            }
        };

        // Navigation buttons — yellow highlight + diamond glyphs follow focus
        var menuButtonScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
            Focus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            HotNormal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
            HotFocus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
        };

        int btnY = hardcoreY + 3;
        var continueBtn = new Button { Text = "  Continue  ", X = Pos.Center(), Y = btnY, IsDefault = false, ColorScheme = menuButtonScheme };
        var backBtn = new Button { Text = "    Back    ", X = Pos.Center(), Y = Pos.Bottom(continueBtn) + 1, ColorScheme = menuButtonScheme };

        // Diamond glyphs follow focus on the two buttons
        foreach (var btn in new[] { continueBtn, backBtn })
        {
            btn.HasFocusChanged += (s, e) =>
            {
                if (s is Button b)
                    b.IsDefault = e.NewValue;
            };
        }

        // Continue — passes difficulty/hardcore to character creation (values wired when gameplay uses them)
        continueBtn.Accepting += (s, e) =>
        {
            // TODO: pass selectedDifficulty and isHardcore through to GameScreen
            // var selectedDifficulty = difficulties[difficultyRadio.SelectedItem];
            // var isHardcore = hardcoreCheck.CheckedState == CheckState.Checked;
            CharacterCreationScreen.Show(mainWindow);
            e.Cancel = true;
        };

        backBtn.Accepting += (s, e) => { TitleScreen.Show(mainWindow); e.Cancel = true; };

        mainWindow.Add(header, difficultyRadio, descLabel, divider,
            hardcoreCheck, hardcoreDesc, continueBtn, backBtn);

        // Per-control key handlers — fires BEFORE each control's built-in processing,
        // giving us full control over navigation order and wrapping.
        // Flow: Radio items → Hardcore checkbox → Continue → Back (wraps both ways)

        difficultyRadio.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    if (difficultyRadio.SelectedItem > 0)
                        difficultyRadio.SelectedItem--;
                    else
                        backBtn.SetFocus();
                    e.Handled = true;
                    break;
                case KeyCode.S: case KeyCode.CursorDown:
                    if (difficultyRadio.SelectedItem < difficulties.Length - 1)
                        difficultyRadio.SelectedItem++;
                    else
                        hardcoreCheck.SetFocus();
                    e.Handled = true;
                    break;
                case KeyCode.Enter:
                    hardcoreCheck.SetFocus();
                    e.Handled = true;
                    break;
            }
        };

        hardcoreCheck.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    difficultyRadio.SetFocus();
                    difficultyRadio.SelectedItem = difficulties.Length - 1;
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
                    difficultyRadio.SetFocus();
                    difficultyRadio.SelectedItem = 0;
                    e.Handled = true;
                    break;
            }
        };

        difficultyRadio.SetFocus();
        DebugLogger.EndTimer("DifficultyScreen.Show", sw);
    }
}
