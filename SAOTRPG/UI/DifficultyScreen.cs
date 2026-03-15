using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Difficulty selection screen — lets the player choose combat difficulty
/// and toggle hardcore (permadeath) mode before character creation.
///
/// Layout:
///   === Select Difficulty ===
///   ( ) Story / Very Easy / Easy / Normal / Hard / Very Hard / Masochist / Unwinnable
///   [description updates on selection]
///   ─────────────────────
///   [ ] Hardcore Mode
///   [Continue]  [Back]
///
/// Special combos (Story+Hardcore, Unwinnable+Hardcore) trigger flavor popups.
/// Debug difficulty appears only when launched with --debug.
/// </summary>
public static class DifficultyScreen
{
    // ── Layout constants ────────────────────────────────────────────
    private const int HeaderY         = 2;
    private const int RadioY          = 5;
    private const int DescriptionGap  = 1;      // Lines between radio bottom and description
    private const int DividerGap      = 3;      // Lines between description and divider
    private const int HardcoreGap     = 2;      // Lines between divider and hardcore toggle
    private const int ButtonGap       = 3;      // Lines between hardcore and buttons

    // ── Default selection ───────────────────────────────────────────
    private const int DefaultDifficulty = 3;    // "Normal"

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("DifficultyScreen.Show");
        DebugLogger.LogScreen("DifficultyScreen");

        // ── Header ──────────────────────────────────────────────────
        var header = new Label
        {
            Text = "=== Select Difficulty ===",
            X = Pos.Center(), Y = HeaderY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title
        };

        // ── Difficulty tiers (index-matched with descriptions) ──────
        var (difficulties, descriptions) = BuildDifficultyData();
        int count = difficulties.Length;

        // ── Radio selector (defaults to Normal) ─────────────────────
        var difficultyRadio = new RadioGroup
        {
            X = Pos.Center(), Y = RadioY,
            RadioLabels = difficulties,
            Width = 20, Height = count,
            SelectedItem = DefaultDifficulty
        };

        // ── Dynamic description label ───────────────────────────────
        int descY = RadioY + count + DescriptionGap;
        var descLabel = new Label
        {
            Text = descriptions[DefaultDifficulty],
            X = Pos.Center(), Y = descY,
            Width = 55, Height = 2,
            ColorScheme = ColorSchemes.Body
        };

        difficultyRadio.SelectedItemChanged += (s, e) =>
            descLabel.Text = descriptions[e.SelectedItem];

        // ── Section divider ─────────────────────────────────────────
        int dividerY = descY + DividerGap;
        var divider = new Label
        {
            Text = "─────────────────────────────────",
            X = Pos.Center(), Y = dividerY,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        // ── Hardcore toggle ─────────────────────────────────────────
        int hardcoreY = dividerY + HardcoreGap;
        var hardcoreCheck = new CheckBox
        {
            Text = " Hardcore Mode  (one life only)",
            X = Pos.Center(), Y = hardcoreY,
            Width = Dim.Auto(), Height = 1
        };
        var hardcoreDesc = new Label
        {
            Text = "  Death is permanent. There are no second chances.",
            X = Pos.Center(), Y = hardcoreY + 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        // ── Flavor popups for wild combos ───────────────────────────
        int unwinnableIndex = Array.IndexOf(difficulties, "Unwinnable");
        WireHardcorePopups(hardcoreCheck, difficultyRadio, unwinnableIndex);

        // ── Navigation buttons ──────────────────────────────────────
        int btnY = hardcoreY + ButtonGap;
        var continueBtn = new Button
        {
            Text = "  Continue  ",
            X = Pos.Center(), Y = btnY,
            IsDefault = false,
            ColorScheme = ColorSchemes.MenuButton
        };
        var backBtn = new Button
        {
            Text = "    Back    ",
            X = Pos.Center(), Y = Pos.Bottom(continueBtn) + 1,
            ColorScheme = ColorSchemes.MenuButton
        };

        // Diamond glyph follows focus
        foreach (var btn in new[] { continueBtn, backBtn })
        {
            btn.HasFocusChanged += (s, e) =>
            {
                if (s is Button b)
                    b.IsDefault = e.NewValue;
            };
        }

        // ── Button actions ──────────────────────────────────────────
        continueBtn.Accepting += (s, e) =>
        {
            int selectedDifficulty = difficultyRadio.SelectedItem;
            bool isHardcore = hardcoreCheck.CheckedState == CheckState.Checked;
            CharacterCreationScreen.Show(mainWindow, selectedDifficulty, isHardcore);
            e.Cancel = true;
        };

        backBtn.Accepting += (s, e) =>
        {
            TitleScreen.Show(mainWindow);
            e.Cancel = true;
        };

        // ── Assemble ────────────────────────────────────────────────
        mainWindow.Add(header, difficultyRadio, descLabel, divider,
            hardcoreCheck, hardcoreDesc, continueBtn, backBtn);

        // ── Per-control navigation ──────────────────────────────────
        // Flow: Radio ↔ Hardcore ↔ Continue ↔ Back (wraps both ways)
        WireNavigation(difficultyRadio, hardcoreCheck, continueBtn, backBtn, difficulties.Length);

        difficultyRadio.SetFocus();
        DebugLogger.EndTimer("DifficultyScreen.Show", sw);
    }

    // ══════════════════════════════════════════════════════════════════
    //  DATA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the difficulty tier names and matching descriptions.
    /// Debug tier is appended when --debug flag is active.
    /// </summary>
    private static (string[] names, string[] descriptions) BuildDifficultyData()
    {
        var names = new List<string>
        {
            "Story", "Very Easy", "Easy", "Normal",
            "Hard", "Very Hard", "Masochist", "Unwinnable"
        };
        var descs = new List<string>
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

        // Debug tier — only visible with --debug
        if (DebugMode.IsEnabled)
        {
            names.Add("Debug");
            descs.Add("Developer mode. All restrictions lifted.");
        }

        return (names.ToArray(), descs.ToArray());
    }

    // ══════════════════════════════════════════════════════════════════
    //  HARDCORE POPUPS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shows flavor popups when the player enables especially wild combos:
    /// Story + Hardcore or Unwinnable + Hardcore.
    /// </summary>
    private static void WireHardcorePopups(CheckBox hardcoreCheck, RadioGroup radio, int unwinnableIndex)
    {
        // Popup when toggling hardcore on while on Story/Unwinnable
        hardcoreCheck.CheckedStateChanging += (s, e) =>
        {
            if (e.NewValue != CheckState.Checked) return;

            if (radio.SelectedItem == 0)
                MessageBox.Query("Really?", "Really? Hardcore and Story mode?", "Yes, really");
            else if (radio.SelectedItem == unwinnableIndex)
                MessageBox.Query("Good Luck", "There's no way you can do this, but good luck.", "Bring it on");
        };

        // Popup when switching to Story/Unwinnable while hardcore is on
        radio.SelectedItemChanged += (s, e) =>
        {
            if (hardcoreCheck.CheckedState != CheckState.Checked) return;

            if (e.SelectedItem == 0)
                MessageBox.Query("Really?", "Really? Hardcore and Story mode?", "Yes, really");
            else if (e.SelectedItem == unwinnableIndex)
                MessageBox.Query("Good Luck", "There's no way you can do this, but good luck.", "Bring it on");
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  NAVIGATION WIRING
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Wires W/S and arrow key navigation across all controls.
    /// Flow: Radio items → Hardcore checkbox → Continue → Back (wraps).
    /// </summary>
    private static void WireNavigation(RadioGroup radio, CheckBox hardcore,
        Button continueBtn, Button backBtn, int radioCount)
    {
        radio.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    if (radio.SelectedItem > 0)
                        radio.SelectedItem--;
                    else
                        backBtn.SetFocus();     // Wrap to bottom
                    e.Handled = true;
                    break;

                case KeyCode.S: case KeyCode.CursorDown:
                    if (radio.SelectedItem < radioCount - 1)
                        radio.SelectedItem++;
                    else
                        hardcore.SetFocus();    // Flow to hardcore
                    e.Handled = true;
                    break;

                case KeyCode.Enter:
                    hardcore.SetFocus();
                    e.Handled = true;
                    break;
            }
        };

        hardcore.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.W: case KeyCode.CursorUp:
                    radio.SetFocus();
                    radio.SelectedItem = radioCount - 1;
                    e.Handled = true;
                    break;

                case KeyCode.S: case KeyCode.CursorDown:
                    continueBtn.SetFocus();
                    e.Handled = true;
                    break;

                case KeyCode.Enter:
                    hardcore.CheckedState = hardcore.CheckedState == CheckState.Checked
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
                    hardcore.SetFocus();
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
                    radio.SetFocus();
                    radio.SelectedItem = 0;     // Wrap to top
                    e.Handled = true;
                    break;
            }
        };
    }
}
