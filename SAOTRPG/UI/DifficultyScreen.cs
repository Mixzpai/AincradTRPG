using Terminal.Gui;

namespace SAOTRPG.UI;

public static class DifficultyScreen
{
    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();

        var header = new Label
        {
            Text = "=== Select Difficulty ===",
            X = Pos.Center(),
            Y = 2,
            Width = Dim.Auto(),
            Height = 1
        };

        var difficulties = new string[] { "Easy", "Normal", "Hard", "Very Hard", "Masochist" };
        var descriptions = new string[]
        {
            "A relaxed adventure. Reduced enemy stats, forgiving combat.",
            "The intended experience. Balanced risk and reward.",
            "Tougher enemies, less room for error.",
            "Punishing combat. Every mistake costs you.",
            "You asked for this. No mercy. No regrets."
        };

        var difficultyRadio = new RadioGroup
        {
            X = Pos.Center(),
            Y = 5,
            RadioLabels = difficulties,
            Width = 20,
            Height = 5,
            SelectedItem = 1
        };

        var descLabel = new Label
        {
            Text = descriptions[1],
            X = Pos.Center(),
            Y = 11,
            Width = 55,
            Height = 2
        };

        difficultyRadio.SelectedItemChanged += (s, e) =>
        {
            descLabel.Text = descriptions[e.SelectedItem];
        };

        var divider = new Label
        {
            Text = "─────────────────────────────────",
            X = Pos.Center(),
            Y = 14,
            Width = Dim.Auto(),
            Height = 1
        };

        var hardcoreCheck = new CheckBox
        {
            Text = " Hardcore Mode  (one life only)",
            X = Pos.Center(),
            Y = 16,
            Width = Dim.Auto(),
            Height = 1
        };

        var hardcoreDesc = new Label
        {
            Text = "  Death is permanent. There are no second chances.",
            X = Pos.Center(),
            Y = 17,
            Width = Dim.Auto(),
            Height = 1
        };

        var continueBtn = new Button
        {
            Text = "  Continue  ",
            X = Pos.Center(),
            Y = 20,
            IsDefault = false
        };

        var backBtn = new Button
        {
            Text = "   Back   ",
            X = Pos.Center(),
            Y = Pos.Bottom(continueBtn) + 1
        };

        continueBtn.Accepting += (s, e) =>
        {
            // Difficulty and hardcore values available for future use:
            // var selectedDifficulty = difficulties[difficultyRadio.SelectedItem];
            // var isHardcore = hardcoreCheck.CheckedState == CheckState.Checked;
            CharacterCreationScreen.Show(mainWindow);
            e.Cancel = true;
        };

        backBtn.Accepting += (s, e) =>
        {
            TitleScreen.Show(mainWindow);
            e.Cancel = true;
        };

        mainWindow.Add(header, difficultyRadio, descLabel, divider,
            hardcoreCheck, hardcoreDesc, continueBtn, backBtn);

        // Custom navigation: arrows/WASD change radio selection when RadioGroup
        // is focused, otherwise navigate between controls like NavigationHelper
        var focusableControls = new View[] { difficultyRadio, hardcoreCheck, continueBtn, backBtn };

        mainWindow.KeyDown += (s, e) =>
        {
            var focused = mainWindow.MostFocused;
            bool onRadio = focused == difficultyRadio || focused?.SuperView == difficultyRadio;

            if (onRadio)
            {
                // W/Up = previous radio item, S/Down = next radio item
                switch (e.KeyCode)
                {
                    case KeyCode.W:
                    case KeyCode.CursorUp:
                        if (difficultyRadio.SelectedItem > 0)
                            difficultyRadio.SelectedItem--;
                        e.Handled = true;
                        return;
                    case KeyCode.S:
                    case KeyCode.CursorDown:
                        if (difficultyRadio.SelectedItem < difficulties.Length - 1)
                            difficultyRadio.SelectedItem++;
                        e.Handled = true;
                        return;
                    case KeyCode.Enter:
                        // Move focus to next control (hardcore checkbox)
                        hardcoreCheck.SetFocus();
                        e.Handled = true;
                        return;
                }
            }
            else
            {
                // Standard navigation between controls
                switch (e.KeyCode)
                {
                    case KeyCode.W:
                    case KeyCode.CursorUp:
                        mainWindow.AdvanceFocus(NavigationDirection.Backward, TabBehavior.TabStop);
                        e.Handled = true;
                        break;
                    case KeyCode.S:
                    case KeyCode.CursorDown:
                        mainWindow.AdvanceFocus(NavigationDirection.Forward, TabBehavior.TabStop);
                        e.Handled = true;
                        break;
                }
            }
        };

        difficultyRadio.SetFocus();
    }
}
