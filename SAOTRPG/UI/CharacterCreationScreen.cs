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

        // ── Header ──────────────────────────────────────────────────────
        var header = new Label
        {
            Text = $"{Theme.DoubleRule(6)} Character Creation {Theme.DoubleRule(6)}",
            X = Pos.Center(), Y = 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };

        var subHeader = new Label
        {
            Text = $"  {Theme.SparkleOpen} Create your avatar for the world of Aincrad",
            X = Pos.Center(), Y = 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        // ── Form frame ──────────────────────────────────────────────────
        var formFrame = new FrameView
        {
            Title = $" {Theme.Diamond} Identity ",
            X = Pos.Center(), Y = 4,
            Width = 50, Height = 12,
            ColorScheme = Theme.FrameSubtle,
            BorderStyle = LineStyle.Rounded
        };

        var firstNameLabel = new Label
        {
            Text = $" {Theme.TriRight} First Name:",
            X = 1, Y = 1, Width = 16, Height = 1,
            ColorScheme = Theme.StatLabel
        };
        var firstNameField = new TextField
        {
            X = 17, Y = 1, Width = 26, Height = 1,
            ColorScheme = Theme.Input
        };

        var lastNameLabel = new Label
        {
            Text = $" {Theme.TriRight} Last Name:",
            X = 1, Y = 3, Width = 16, Height = 1,
            ColorScheme = Theme.StatLabel
        };
        var lastNameField = new TextField
        {
            X = 17, Y = 3, Width = 26, Height = 1,
            ColorScheme = Theme.Input
        };

        var genderLabel = new Label
        {
            Text = $" {Theme.TriRight} Gender:",
            X = 1, Y = 5, Width = 16, Height = 1,
            ColorScheme = Theme.StatLabel
        };
        var genderField = new TextField
        {
            X = 17, Y = 5, Width = 26, Height = 1,
            ColorScheme = Theme.Input
        };

        // Validation feedback
        var errorLabel = new Label
        {
            Text = "",
            X = 1, Y = 7, Width = 44, Height = 1,
            ColorScheme = Theme.Error
        };

        var createBtn = new Button
        {
            Text = $" {Theme.Sparkle} Create Character ",
            X = Pos.Center(), Y = 9,
            IsDefault = true,
            ColorScheme = Theme.MenuButton
        };
        Theme.AttachDiamondFocus(createBtn);

        formFrame.Add(firstNameLabel, firstNameField, lastNameLabel, lastNameField,
            genderLabel, genderField, errorLabel, createBtn);

        // Validate all fields filled, then proceed to skill allocation
        createBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var firstName = firstNameField.Text?.Trim() ?? "";
            var lastName = lastNameField.Text?.Trim() ?? "";
            var gender = genderField.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(gender))
            {
                errorLabel.Text = $" {Theme.Cross} Please fill in all fields.";
                return;
            }

            ShowSkillAllocation(mainWindow, firstName, lastName, gender);
        };

        // ── Controls hint ───────────────────────────────────────────────
        var controlsHint = new Label
        {
            Text = $"[Tab] Next Field    [{Theme.ArrowUp}/{Theme.ArrowDown}] Navigate    [Enter] Confirm",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        mainWindow.Add(header, subHeader, formFrame, controlsHint);
        firstNameField.SetFocus();
        DebugLogger.EndTimer("CharacterCreationScreen.Show", sw);
    }

    // ── Phase 2 — Skill allocation using shared component ────────────────
    private static void ShowSkillAllocation(Window mainWindow, string firstName, string lastName, string gender)
    {
        var sw = DebugLogger.StartTimer("SkillAllocation.Show");
        mainWindow.RemoveAll();

        // Temp log collects messages during creation (discarded when GameScreen wires real log)
        var tempLog = new StringGameLog(new System.Text.StringBuilder());
        var player = Player.CreateNewPlayer(firstName, lastName, gender, tempLog, new TerminalGuiInventoryLogger(tempLog));

        // ── Header ──────────────────────────────────────────────────────
        var header = new Label
        {
            Text = $"{Theme.DoubleRule(6)} Allocate Skill Points {Theme.DoubleRule(6)}",
            X = Pos.Center(), Y = 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };

        var subHeader = new Label
        {
            Text = $"  {Theme.SparkleOpen} Distribute points to shape {firstName}'s abilities",
            X = Pos.Center(), Y = 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        // ── Stats preview frame (left) ──────────────────────────────────
        var statsFrame = new FrameView
        {
            Title = $" {Theme.Heart} {firstName} {lastName} ",
            X = 1, Y = 3,
            Width = 40, Height = 22,
            ColorScheme = Theme.FrameSubtle,
            BorderStyle = LineStyle.Rounded
        };

        var statsView = new TextView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(),
            ReadOnly = true,
            Text = player.GetStatsDisplay(),
            ColorScheme = Theme.Body
        };
        statsFrame.Add(statsView);

        // Use shared SkillAllocationPanel — pass mainWindow as container for controls
        var result = SkillAllocationPanel.Build(mainWindow, player, statsX: -999, statsY: -999, controlsX: 44, controlsStartY: 4);

        // Override the panel's statsView — we use our own in the frame
        result.RefreshStats = () =>
        {
            statsView.Text = player.GetStatsDisplay();
            result.StatsView.Text = player.GetStatsDisplay();
        };

        // Hide the panel's default statsView (it's positioned off-screen at -999)

        // ── Start Game button ───────────────────────────────────────────
        var doneBtn = new Button
        {
            Text = $" {Theme.Star} Start Game ",
            X = 44, Y = 20,
            IsDefault = true,
            ColorScheme = Theme.MenuButton
        };
        Theme.AttachDiamondFocus(doneBtn);
        doneBtn.Accepting += (s, e) => { GameScreen.Show(mainWindow, player); e.Cancel = true; };

        // ── Controls hint ───────────────────────────────────────────────
        var controlsHint = new Label
        {
            Text = $"[{Theme.ArrowUp}/{Theme.ArrowDown}] Navigate    [Enter] Select    [Tab] Next",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        mainWindow.Add(header, subHeader, statsFrame, doneBtn, controlsHint);
        DebugLogger.EndTimer("SkillAllocation.Show", sw);
    }
}
