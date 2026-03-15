using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Two-phase character creation flow:
///
/// Phase 1 — Identity:
///   First Name / Last Name / Gender text fields with validation.
///
/// Phase 2 — Skill Allocation:
///   Left panel: live stats preview (read-only).
///   Right panel: stat selector radio, points input, allocate button.
///   "Start Game" transitions to GameScreen.
///
/// The player object is created between phases so stat changes reflect immediately.
/// </summary>
public static class CharacterCreationScreen
{
    // ── Layout constants — Phase 1 (Identity) ───────────────────────
    private const int HeaderY       = 1;
    private const int FieldStartY   = 4;
    private const int FieldSpacingY = 2;
    private const int LabelX        = 3;
    private const int FieldX        = 18;
    private const int FieldWidth    = 25;
    private const int LabelWidth    = 14;
    private const int ErrorY        = 10;
    private const int CreateBtnY    = 12;

    // ── Layout constants — Phase 2 (Allocation) ─────────────────────
    private const int StatsViewX    = 1;
    private const int StatsViewY    = 3;
    private const int StatsViewW    = 35;
    private const int StatsViewH    = 20;
    private const int RightPanelX   = 38;
    private const int StatRadioY    = 5;
    private const int PointsInputY  = 12;
    private const int AllocateBtnY  = 14;
    private const int DoneBtnY      = 16;
    private const int MessageY      = 18;

    // ══════════════════════════════════════════════════════════════════
    //  PHASE 1 — Identity Input
    // ══════════════════════════════════════════════════════════════════

    public static void Show(Window mainWindow, int difficulty = 3, bool hardcore = false)
    {
        var sw = DebugLogger.StartTimer("CharacterCreationScreen.Show");
        DebugLogger.LogScreen("CharacterCreationScreen");
        mainWindow.RemoveAll();

        // ── Header ──────────────────────────────────────────────────
        var header = new Label
        {
            Text = "=== Character Creation ===",
            X = Pos.Center(), Y = HeaderY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title
        };

        // ── Input fields ────────────────────────────────────────────
        var (firstLabel, firstField) = CreateField("First Name:", FieldStartY);
        var (lastLabel, lastField)   = CreateField("Last Name:",  FieldStartY + FieldSpacingY);
        var (genderLabel, genderField) = CreateField("Gender:",   FieldStartY + FieldSpacingY * 2);

        // ── Validation feedback ─────────────────────────────────────
        var errorLabel = new Label
        {
            Text = "", X = LabelX, Y = ErrorY,
            Width = 40, Height = 1,
            ColorScheme = ColorSchemes.Danger
        };

        // ── Create button ───────────────────────────────────────────
        var createBtn = new Button
        {
            Text = "  Create Character  ",
            X = Pos.Center(), Y = CreateBtnY,
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };

        createBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var firstName = firstField.Text?.Trim() ?? "";
            var lastName  = lastField.Text?.Trim() ?? "";
            var gender    = genderField.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) ||
                string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(gender))
            {
                errorLabel.Text = "Please fill in all fields.";
                return;
            }

            ShowSkillAllocation(mainWindow, firstName, lastName, gender, difficulty, hardcore);
        };

        // ── Assemble ────────────────────────────────────────────────
        mainWindow.Add(header,
            firstLabel, firstField, lastLabel, lastField,
            genderLabel, genderField, errorLabel, createBtn);
        firstField.SetFocus();
        DebugLogger.EndTimer("CharacterCreationScreen.Show", sw);
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE 2 — Skill Allocation
    // ══════════════════════════════════════════════════════════════════

    private static void ShowSkillAllocation(Window mainWindow, string firstName, string lastName, string gender,
        int difficulty = 3, bool hardcore = false)
    {
        var sw = DebugLogger.StartTimer("SkillAllocation.Show");
        mainWindow.RemoveAll();

        // Temp log collects messages during creation (discarded when GameScreen wires the real log)
        var tempLog = new StringGameLog(new System.Text.StringBuilder());
        var player = Player.CreateNewPlayer(firstName, lastName, gender, tempLog, new TerminalGuiInventoryLogger(tempLog));

        // ── Header ──────────────────────────────────────────────────
        var header = new Label
        {
            Text = "=== Allocate Skill Points ===",
            X = Pos.Center(), Y = HeaderY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title
        };

        // ── Left panel — live stats preview ─────────────────────────
        var statsView = new TextView
        {
            X = StatsViewX, Y = StatsViewY,
            Width = StatsViewW, Height = StatsViewH,
            ReadOnly = true,
            Text = player.GetStatsDisplay()
        };

        // ── Right panel — stat selector ─────────────────────────────
        var statLabel = new Label
        {
            Text = "Stat:", X = RightPanelX, Y = StatRadioY - 1,
            Width = 8, Height = 1,
            ColorScheme = ColorSchemes.Body
        };

        string[] stats = { "Vitality", "Strength", "Endurance", "Dexterity", "Agility", "Intelligence" };
        var statRadio = new RadioGroup
        {
            X = RightPanelX, Y = StatRadioY,
            RadioLabels = stats,
            Width = 20, Height = stats.Length
        };

        // ── Points input ────────────────────────────────────────────
        var pointsLabel = new Label
        {
            Text = "Points:", X = RightPanelX, Y = PointsInputY,
            Width = 10, Height = 1,
            ColorScheme = ColorSchemes.Body
        };
        var pointsField = new TextField
        {
            Text = "1",
            X = RightPanelX + 11, Y = PointsInputY,
            Width = 8, Height = 1
        };

        // ── Action buttons ──────────────────────────────────────────
        var allocateBtn = new Button
        {
            Text = "  Allocate  ",
            X = RightPanelX, Y = AllocateBtnY,
            ColorScheme = ColorSchemes.Button
        };
        var doneBtn = new Button
        {
            Text = "  Start Game  ",
            X = RightPanelX, Y = DoneBtnY,
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };

        // ── Feedback label ──────────────────────────────────────────
        var messageLabel = new Label
        {
            Text = "", X = RightPanelX, Y = MessageY,
            Width = 35, Height = 2,
            ColorScheme = ColorSchemes.Gold
        };

        // ── Allocate handler ────────────────────────────────────────
        allocateBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            var pointsText = pointsField.Text?.Trim() ?? "";

            if (!int.TryParse(pointsText, out int points) || points <= 0)
            {
                messageLabel.Text = "Enter a valid number.";
                return;
            }

            var selectedStat = stats[statRadio.SelectedItem];
            if (player.SpendSkillPoints(selectedStat, points))
            {
                statsView.Text = player.GetStatsDisplay();
                messageLabel.Text = $"+{points} {selectedStat}!";
            }
            else
            {
                messageLabel.Text = "Not enough points!";
            }
        };

        // ── Start game ──────────────────────────────────────────────
        doneBtn.Accepting += (s, e) =>
        {
            GameScreen.Show(mainWindow, player, difficulty, hardcore);
            e.Cancel = true;
        };

        // ── Assemble ────────────────────────────────────────────────
        mainWindow.Add(header, statsView, statLabel, statRadio,
            pointsLabel, pointsField, allocateBtn, doneBtn, messageLabel);
        statRadio.SetFocus();
        DebugLogger.EndTimer("SkillAllocation.Show", sw);
    }

    // ══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Creates a label + text field pair at a given Y position.</summary>
    private static (Label label, TextField field) CreateField(string labelText, int y)
    {
        var label = new Label
        {
            Text = labelText,
            X = LabelX, Y = y,
            Width = LabelWidth, Height = 1,
            ColorScheme = ColorSchemes.Body
        };
        var field = new TextField
        {
            X = FieldX, Y = y,
            Width = FieldWidth, Height = 1
        };
        return (label, field);
    }
}
