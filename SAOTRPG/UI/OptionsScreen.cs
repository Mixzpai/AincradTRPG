using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Dialogs;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Settings screen — gameplay and display preferences. Changes auto-save
// to disk via UserSettings.
public static class OptionsScreen
{
    // Label column sits left of center, control column sits right of center.
    // Keeps every row aligned on the same visual axis.
    private const int LabelOffset   = -22;
    private const int ControlOffset =   2;
    private const int LabelWidth    = 22;

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        var settings = UserSettings.Current;

        var (header, headerRule) = ScreenHeader.Create("Options", 2, 20);

        var saveLabel = new Label
        {
            Text = "", X = Pos.Center(), Y = 4,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Gold,
        };

        // Local helpers — capture saveLabel so individual call sites don't
        // have to duplicate UserSettings.Save() + feedback text.
        CheckBox Toggle(string name, int y, bool initial, Action<bool> apply)
        {
            var check = new CheckBox
            {
                Text = "",
                X = Pos.Center() + ControlOffset, Y = y,
                CheckedState = initial ? CheckState.Checked : CheckState.UnChecked,
            };
            check.CheckedStateChanging += (s, e) =>
            {
                bool v = e.NewValue == CheckState.Checked;
                apply(v);
                UserSettings.Save();
                saveLabel.Text = $"{name}: {(v ? "ON" : "OFF")}  [OK] Saved";
            };
            return check;
        }

        RadioGroup Radio(string name, int y, string[] labels, int initial, Action<int> apply)
        {
            var radio = new RadioGroup
            {
                X = Pos.Center() + ControlOffset, Y = y, RadioLabels = labels,
                Orientation = Orientation.Horizontal,
                SelectedItem = Math.Clamp(initial, 0, labels.Length - 1),
            };
            radio.SelectedItemChanged += (s, e) =>
            {
                apply(radio.SelectedItem);
                UserSettings.Save();
                saveLabel.Text = $"{name}: {labels[radio.SelectedItem]}  [OK] Saved";
            };
            return radio;
        }

        int y = 6;

        // ── Gameplay ─────────────────────────────────────────────────
        var gameplayHeader = ScreenHeader.Section("Gameplay", y);
        y += 2;

        var autoPickupLabel = FormLabel("Auto-Pickup Items", y);
        var autoPickupCheck = Toggle("Auto-Pickup", y, settings.AutoPickup, v => settings.AutoPickup = v);
        y += 2;

        var textSpeedLabel = FormLabel("Text Speed", y);
        var textSpeedRadio = Radio("Text Speed", y,
            new[] { "Fast", "Normal", "Slow" },
            settings.TextSpeed, v => settings.TextSpeed = v);
        y += 3;

        // ── Display ──────────────────────────────────────────────────
        var displayHeader = ScreenHeader.Section("Display", y);
        y += 2;

        var footstepLabel = FormLabel("Footstep Trails", y);
        var footstepCheck = Toggle("Footstep Trails", y, settings.ShowFootsteps, v => settings.ShowFootsteps = v);
        y += 2;

        var flashLabel = FormLabel("Damage Flash", y);
        var flashCheck = Toggle("Damage Flash", y, settings.ShowDamageFlash, v => settings.ShowDamageFlash = v);
        y += 3;

        // ── Controls ─────────────────────────────────────────────────
        var controlsHeader = ScreenHeader.Section("Controls", y);
        y += 2;

        var keybindLabel = FormLabel("View Keybindings", y);
        var keybindBtn = new Button
        {
            Text = " Open (H) ", X = Pos.Center() + ControlOffset, Y = y,
            ColorScheme = ColorSchemes.Button,
        };
        keybindBtn.Accepting += (s, e) => { e.Cancel = true; HelpDialog.Show(); };
        y += 3;

        // ── About ────────────────────────────────────────────────────
        var aboutHeader = ScreenHeader.Section("About", y);
        y += 2;

        var versionLabel = new Label
        {
            Text = $"AincradTRPG {AppVersion.Display}",
            X = Pos.Center(), Y = y, Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Body,
        };
        var tributeLabel = new Label
        {
            Text = "An SAO-themed ASCII roguelike",
            X = Pos.Center(), Y = y + 1, Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim,
        };
        y += 4;

        // ── Footer buttons ───────────────────────────────────────────
        var resetBtn = new Button
        {
            Text = " Reset Defaults ", X = Pos.Center() - 15, Y = y,
            ColorScheme = ColorSchemes.Button,
        };
        resetBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (!DialogHelper.ConfirmAction("Reset", "all settings to defaults")) return;
            UserSettings.ResetToDefaults();
            Show(mainWindow);
        };

        var backBtn = new Button
        {
            Text = " Back ", X = Pos.Center() + 5, Y = y,
            IsDefault = true, ColorScheme = ColorSchemes.Button,
        };
        backBtn.Accepting += (s, e) => { TitleScreen.Show(mainWindow); e.Cancel = true; };

        mainWindow.Add(
            header, headerRule, saveLabel,
            gameplayHeader, autoPickupLabel, autoPickupCheck,
            textSpeedLabel, textSpeedRadio,
            displayHeader, footstepLabel, footstepCheck, flashLabel, flashCheck,
            controlsHeader, keybindLabel, keybindBtn,
            aboutHeader, versionLabel, tributeLabel,
            resetBtn, backBtn);

        NavigationHelper.EnableGameNavigation(mainWindow);
        backBtn.SetFocus();
    }

    private static Label FormLabel(string text, int y) =>
        ScreenHeader.FormLabel(text, Pos.Center() + LabelOffset, y, LabelWidth);
}
