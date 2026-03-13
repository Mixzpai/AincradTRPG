using Terminal.Gui;

namespace SAOTRPG.UI;

public static class CharacterCreationScreen
{
    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();

        var header = new Label
        {
            Text = "=== Character Creation ===",
            X = Pos.Center(),
            Y = 1,
            Width = Dim.Auto(),
            Height = 1
        };

        // First Name
        var firstNameLabel = new Label
        {
            Text = "First Name:",
            X = 3,
            Y = 4,
            Width = 14,
            Height = 1
        };
        var firstNameField = new TextField
        {
            X = 18,
            Y = 4,
            Width = 25,
            Height = 1
        };

        // Last Name
        var lastNameLabel = new Label
        {
            Text = "Last Name:",
            X = 3,
            Y = 6,
            Width = 14,
            Height = 1
        };
        var lastNameField = new TextField
        {
            X = 18,
            Y = 6,
            Width = 25,
            Height = 1
        };

        // Gender
        var genderLabel = new Label
        {
            Text = "Gender:",
            X = 3,
            Y = 8,
            Width = 14,
            Height = 1
        };
        var genderField = new TextField
        {
            X = 18,
            Y = 8,
            Width = 25,
            Height = 1
        };

        var errorLabel = new Label
        {
            Text = "",
            X = 3,
            Y = 10,
            Width = 40,
            Height = 1
        };

        var createBtn = new Button
        {
            Text = "  Create Character  ",
            X = Pos.Center(),
            Y = 12,
            IsDefault = true
        };

        createBtn.Accepting += (s, e) =>
        {
            var firstName = firstNameField.Text?.Trim() ?? "";
            var lastName = lastNameField.Text?.Trim() ?? "";
            var gender = genderField.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(gender))
            {
                errorLabel.Text = "Please fill in all fields.";
                e.Cancel = true;
                return;
            }

            ShowSkillAllocation(mainWindow, firstName, lastName, gender);
            e.Cancel = true;
        };

        mainWindow.Add(header, firstNameLabel, firstNameField, lastNameLabel, lastNameField,
            genderLabel, genderField, errorLabel, createBtn);
        firstNameField.SetFocus();
    }

    private static void ShowSkillAllocation(Window mainWindow, string firstName, string lastName, string gender)
    {
        mainWindow.RemoveAll();

        // Create a temporary log that collects messages for the status label
        var statusMessages = new System.Text.StringBuilder();
        var tempLog = new StringGameLog(statusMessages);

        var player = Player.CreateNewPlayer(firstName, lastName, gender, tempLog,
            new TerminalGuiInventoryLogger(tempLog));

        var header = new Label
        {
            Text = "=== Allocate Skill Points ===",
            X = Pos.Center(),
            Y = 1,
            Width = Dim.Auto(),
            Height = 1
        };

        var statsView = new TextView
        {
            X = 1,
            Y = 3,
            Width = 35,
            Height = 20,
            ReadOnly = true,
            Text = player.GetStatsDisplay()
        };

        var statLabel = new Label
        {
            Text = "Stat:",
            X = 38,
            Y = 4,
            Width = 8,
            Height = 1
        };

        var stats = new string[] { "Vitality", "Strength", "Endurance", "Dexterity", "Agility", "Intelligence" };
        var statRadio = new RadioGroup
        {
            X = 38,
            Y = 5,
            RadioLabels = stats,
            Width = 20,
            Height = 6
        };

        var pointsLabel = new Label
        {
            Text = "Points:",
            X = 38,
            Y = 12,
            Width = 10,
            Height = 1
        };

        var pointsField = new TextField
        {
            Text = "1",
            X = 49,
            Y = 12,
            Width = 8,
            Height = 1
        };

        var allocateBtn = new Button
        {
            Text = "  Allocate  ",
            X = 38,
            Y = 14
        };

        var doneBtn = new Button
        {
            Text = "  Start Game  ",
            X = 38,
            Y = 16,
            IsDefault = true
        };

        var messageLabel = new Label
        {
            Text = "",
            X = 38,
            Y = 18,
            Width = 35,
            Height = 2
        };

        allocateBtn.Accepting += (s, e) =>
        {
            var selectedStat = stats[statRadio.SelectedItem];
            var pointsText = pointsField.Text?.Trim() ?? "";

            if (!int.TryParse(pointsText, out int points) || points <= 0)
            {
                messageLabel.Text = "Enter a valid number.";
                e.Cancel = true;
                return;
            }

            if (player.SpendSkillPoints(selectedStat, points))
            {
                statsView.Text = player.GetStatsDisplay();
                messageLabel.Text = $"+{points} {selectedStat}!";
            }
            else
            {
                messageLabel.Text = "Not enough points!";
            }
            e.Cancel = true;
        };

        doneBtn.Accepting += (s, e) =>
        {
            GameScreen.Show(mainWindow, player);
            e.Cancel = true;
        };

        mainWindow.Add(header, statsView, statLabel, statRadio, pointsLabel,
            pointsField, allocateBtn, doneBtn, messageLabel);
        statRadio.SetFocus();
    }
}

/// <summary>
/// Simple IGameLog that writes to a StringBuilder (used during character creation).
/// </summary>
internal class StringGameLog : IGameLog
{
    private readonly System.Text.StringBuilder _sb;
    public StringGameLog(System.Text.StringBuilder sb) => _sb = sb;
    public void Log(string message) => _sb.AppendLine(message);
    public void LogCombat(string message) => _sb.AppendLine(message);
    public void LogSystem(string message) => _sb.AppendLine(message);
}
