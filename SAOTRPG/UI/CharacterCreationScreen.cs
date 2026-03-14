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

        // Left panel — live stats preview (tall enough for full GetStatsDisplay output)
        var statsView = new TextView { X = 1, Y = 3, Width = 40, Height = 18, ReadOnly = true, Text = player.GetStatsDisplay() };

        // Right panel — per-stat +/- buttons with pending point counts
        var stats = new string[] { "Vitality", "Strength", "Endurance", "Dexterity", "Agility", "Intelligence" };
        var pending = new int[stats.Length]; // pending points queued per stat
        var pendingLabels = new Label[stats.Length];

        int startY = 3;
        int colX = 44;

        // Track total pending so we can't exceed available points
        int TotalPending() { int sum = 0; foreach (var p in pending) sum += p; return sum; }

        void RefreshPendingDisplay()
        {
            for (int i = 0; i < stats.Length; i++)
                pendingLabels[i].Text = pending[i] > 0 ? $"+{pending[i]}" : " 0";
        }

        for (int i = 0; i < stats.Length; i++)
        {
            int row = startY + i * 2;
            int idx = i; // capture for closures

            var nameLabel = new Label { Text = $"{stats[i]}:", X = colX, Y = row, Width = 14, Height = 1 };

            var minusBtn = new Button { Text = " - ", X = colX + 14, Y = row, ColorScheme = NavigationHelper.ButtonScheme };
            pendingLabels[i] = new Label { Text = " 0", X = Pos.Right(minusBtn) + 1, Y = row, Width = 4, Height = 1 };
            var plusBtn = new Button { Text = " + ", X = Pos.Right(pendingLabels[i]) + 1, Y = row, ColorScheme = NavigationHelper.ButtonScheme };

            minusBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (pending[idx] > 0)
                {
                    pending[idx]--;
                    RefreshPendingDisplay();
                }
            };

            plusBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (TotalPending() < player.SkillPoints)
                {
                    pending[idx]++;
                    RefreshPendingDisplay();
                }
            };

            mainWindow.Add(nameLabel, minusBtn, pendingLabels[i], plusBtn);
        }

        int controlsY = startY + stats.Length * 2 + 1;

        var allocateBtn = new Button { Text = "  Allocate  ", X = colX, Y = controlsY, ColorScheme = NavigationHelper.ButtonScheme };
        var resetBtn = new Button { Text = "  Reset  ", X = Pos.Right(allocateBtn) + 2, Y = controlsY, ColorScheme = NavigationHelper.ButtonScheme };
        var doneBtn = new Button { Text = "  Start Game  ", X = colX, Y = controlsY + 2, IsDefault = true, ColorScheme = NavigationHelper.ButtonScheme };

        // Feedback label for allocation results
        var messageLabel = new Label { Text = "", X = colX, Y = controlsY + 4, Width = 35, Height = 2 };

        // Commit all pending points at once
        allocateBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (TotalPending() == 0)
            { messageLabel.Text = "No points queued."; return; }

            var results = new System.Text.StringBuilder();
            for (int i = 0; i < stats.Length; i++)
            {
                if (pending[i] <= 0) continue;
                if (player.SpendSkillPoints(stats[i], pending[i]))
                    results.Append($"+{pending[i]} {stats[i]}  ");
                else
                    results.Append($"{stats[i]}: not enough pts  ");
                pending[i] = 0;
            }

            RefreshPendingDisplay();
            statsView.Text = player.GetStatsDisplay();
            messageLabel.Text = results.ToString();
        };

        // Reset — restore the same player's stats to base and refund all skill points
        resetBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            player.ResetStatAllocation();
            for (int i = 0; i < pending.Length; i++)
                pending[i] = 0;
            RefreshPendingDisplay();
            statsView.Text = player.GetStatsDisplay();
            messageLabel.Text = "Stats reset! Points refunded.";
        };

        // Transition to game screen with the configured player
        doneBtn.Accepting += (s, e) => { GameScreen.Show(mainWindow, player); e.Cancel = true; };

        mainWindow.Add(header, statsView, allocateBtn, resetBtn, doneBtn, messageLabel);
        DebugLogger.EndTimer("SkillAllocation.Show", sw);
    }
}
