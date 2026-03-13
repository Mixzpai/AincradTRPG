using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Logging;

namespace SAOTRPG.UI;

public static class CharacterCreationScreen
{
    public static void Show(Window mainWindow)
    {
        var sw = DebugLogger.StartTimer("CharacterCreationScreen.Show");
        DebugLogger.LogScreen("CharacterCreationScreen");
        mainWindow.RemoveAll();

        var header = new Label { Text = "=== Character Creation ===", X = Pos.Center(), Y = 1, Width = Dim.Auto(), Height = 1 };

        // Input fields — first name, last name, gender
        var firstNameLabel = new Label { Text = "First Name:", X = 3, Y = 4, Width = 14, Height = 1 };
        var firstNameField = new TextField { X = 18, Y = 4, Width = 25, Height = 1 };
        var lastNameLabel = new Label { Text = "Last Name:", X = 3, Y = 6, Width = 14, Height = 1 };
        var lastNameField = new TextField { X = 18, Y = 6, Width = 25, Height = 1 };
        var genderLabel = new Label { Text = "Gender:", X = 3, Y = 8, Width = 14, Height = 1 };
        var genderField = new TextField { X = 18, Y = 8, Width = 25, Height = 1 };

        // Validation feedback
        var errorLabel = new Label { Text = "", X = 3, Y = 10, Width = 40, Height = 1 };

        var createBtn = new Button { Text = "  Create Character  ", X = Pos.Center(), Y = 12, IsDefault = true, ColorScheme = NavigationHelper.ButtonScheme };

        // Validate all fields filled, then proceed to skill allocation
        createBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var firstName = firstNameField.Text?.Trim() ?? "";
            var lastName = lastNameField.Text?.Trim() ?? "";
            var gender = genderField.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(gender))
            { errorLabel.Text = "Please fill in all fields."; return; }

            ShowSkillAllocation(mainWindow, firstName, lastName, gender);
        };

        mainWindow.Add(header, firstNameLabel, firstNameField, lastNameLabel, lastNameField,
            genderLabel, genderField, errorLabel, createBtn);
        firstNameField.SetFocus();
        DebugLogger.EndTimer("CharacterCreationScreen.Show", sw);
    }

    // Second phase — allocate starting skill points before entering the game
    private static void ShowSkillAllocation(Window mainWindow, string firstName, string lastName, string gender)
    {
        var sw = DebugLogger.StartTimer("SkillAllocation.Show");
        mainWindow.RemoveAll();

        // Temp log collects messages during creation (discarded when GameScreen wires real log)
        var tempLog = new StringGameLog(new System.Text.StringBuilder());
        var player = Player.CreateNewPlayer(firstName, lastName, gender, tempLog, new TerminalGuiInventoryLogger(tempLog));

        var header = new Label { Text = "=== Allocate Skill Points ===", X = Pos.Center(), Y = 1, Width = Dim.Auto(), Height = 1 };

        // Left panel — live stats preview
        var statsView = new TextView { X = 1, Y = 3, Width = 35, Height = 20, ReadOnly = true, Text = player.GetStatsDisplay() };

        // Right panel — stat selector, points input, action buttons
        var statLabel = new Label { Text = "Stat:", X = 38, Y = 4, Width = 8, Height = 1 };
        var stats = new string[] { "Vitality", "Strength", "Endurance", "Dexterity", "Agility", "Intelligence" };
        var statRadio = new RadioGroup { X = 38, Y = 5, RadioLabels = stats, Width = 20, Height = 6 };

        var pointsLabel = new Label { Text = "Points:", X = 38, Y = 12, Width = 10, Height = 1 };
        var pointsField = new TextField { Text = "1", X = 49, Y = 12, Width = 8, Height = 1 };

        var allocateBtn = new Button { Text = "  Allocate  ", X = 38, Y = 14, ColorScheme = NavigationHelper.ButtonScheme };
        var doneBtn = new Button { Text = "  Start Game  ", X = 38, Y = 16, IsDefault = true, ColorScheme = NavigationHelper.ButtonScheme };

        // Feedback label for allocation results
        var messageLabel = new Label { Text = "", X = 38, Y = 18, Width = 35, Height = 2 };

        // Spend points on selected stat, refresh stats preview
        allocateBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var pointsText = pointsField.Text?.Trim() ?? "";

            if (!int.TryParse(pointsText, out int points) || points <= 0)
            { messageLabel.Text = "Enter a valid number."; return; }

            var selectedStat = stats[statRadio.SelectedItem];
            if (player.SpendSkillPoints(selectedStat, points))
            {
                statsView.Text = player.GetStatsDisplay();
                messageLabel.Text = $"+{points} {selectedStat}!";
            }
            else
                messageLabel.Text = "Not enough points!";
        };

        // Transition to game screen with the configured player
        doneBtn.Accepting += (s, e) => { GameScreen.Show(mainWindow, player); e.Cancel = true; };

        mainWindow.Add(header, statsView, statLabel, statRadio, pointsLabel,
            pointsField, allocateBtn, doneBtn, messageLabel);
        statRadio.SetFocus();
        DebugLogger.EndTimer("SkillAllocation.Show", sw);
    }
}
