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

    // onBack: Back-button callback. Null = default TitleScreen return.
    // Pause menu passes a callback that reloads the save so the in-progress run is restored.
    public static void Show(Window mainWindow, Action? onBack = null)
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

        // ── Accessibility ────────────────────────────────────────────
        var accessHeader = ScreenHeader.Section("Accessibility", y);
        y += 2;

        var shakeLabel = FormLabel("Screen Shake", y);
        var shakeCheck = Toggle("Screen Shake", y, settings.ScreenShakeEnabled,
            v => settings.ScreenShakeEnabled = v);
        y += 1;
        var shakeDesc = new Label
        {
            Text = "Brief viewport jitter on crits and heavy hits.",
            X = Pos.Center() + ControlOffset, Y = y,
            Width = 50, Height = 1, ColorScheme = ColorSchemes.Dim,
        };
        y += 2;

        // Damage breakdown — 4-mode cycle + 4-line preview box (FB-463).
        var breakdownLabel = FormLabel("Damage Breakdown", y);
        var breakdownRadio = Radio("Damage Breakdown", y,
            new[] { "Off", "Concise", "Medium", "Verbose" },
            (int)settings.DamageBreakdownMode,
            v => settings.DamageBreakdownMode = (DamageBreakdownMode)v);
        y += 2;

        // Static preview lines — numbers are frozen samples, not live data.
        // Selected mode highlights Gold; others dim so the format comparison
        // is immediate and doesn't require farming a monster to check.
        string[] previewLines =
        {
            "Off      You hit Kobold for 14 damage",
            "Concise  You hit Kobold for 14 dmg (18 - 4 armor)",
            "Medium   You hit Kobold: 18 raw - 4 armor = 14",
            "Verbose  You hit Kobold for 14 dmg (18 atk - 4 def + 0 resist)",
        };
        var previewLabels = new Label[4];
        for (int i = 0; i < 4; i++)
        {
            previewLabels[i] = new Label
            {
                Text = previewLines[i], X = Pos.Center() + LabelOffset,
                Y = y + i, Width = 60, Height = 1,
                ColorScheme = i == (int)settings.DamageBreakdownMode
                    ? ColorSchemes.Gold : ColorSchemes.Dim,
            };
        }
        breakdownRadio.SelectedItemChanged += (s, e) =>
        {
            for (int i = 0; i < 4; i++)
                previewLabels[i].ColorScheme = i == breakdownRadio.SelectedItem
                    ? ColorSchemes.Gold : ColorSchemes.Dim;
        };
        y += 5;

        // ── Particle Density (FB-450) ────────────────────────────────
        var particleLabel = FormLabel("Particle Density", y);
        var particleRadio = Radio("Particle Density", y,
            new[] { "Off", "Subtle", "Moderate", "Pronounced" },
            (int)settings.ParticleDensity,
            v => settings.ParticleDensity = (ParticleDensity)v);
        y += 2;
        string[] particlePreview =
        {
            "Off         no particle effects",
            "Subtle      1-3 particles, 150ms fade",
            "Moderate    3-6 particles, 500ms envelope",
            "Pronounced  5-10 particles, 800ms envelope",
        };
        var particleLabels = new Label[4];
        for (int i = 0; i < 4; i++)
        {
            particleLabels[i] = new Label
            {
                Text = particlePreview[i], X = Pos.Center() + LabelOffset,
                Y = y + i, Width = 60, Height = 1,
                ColorScheme = i == (int)settings.ParticleDensity
                    ? ColorSchemes.Gold : ColorSchemes.Dim,
            };
        }
        particleRadio.SelectedItemChanged += (s, e) =>
        {
            for (int i = 0; i < 4; i++)
                particleLabels[i].ColorScheme = i == particleRadio.SelectedItem
                    ? ColorSchemes.Gold : ColorSchemes.Dim;
        };
        y += 5;

        // ── Damage Tag Position (FB-452) ─────────────────────────────
        var tagPosLabel = FormLabel("Damage Tag Position", y);
        var tagPosRadio = Radio("Damage Tag Position", y,
            new[] { "Prefix", "Suffix", "Inline" },
            (int)settings.DamageTagPosition,
            v => settings.DamageTagPosition = (DamageTagPosition)v);
        y += 2;
        string[] tagPosPreview =
        {
            "Prefix   [SLASH] You hit Kobold for 8",
            "Suffix   You hit Kobold for 8 [SLASH]",
            "Inline   You hit Kobold for 8 [SLASH] dmg",
        };
        var tagPosLabels = new Label[3];
        for (int i = 0; i < 3; i++)
        {
            tagPosLabels[i] = new Label
            {
                Text = tagPosPreview[i], X = Pos.Center() + LabelOffset,
                Y = y + i, Width = 60, Height = 1,
                ColorScheme = i == (int)settings.DamageTagPosition
                    ? ColorSchemes.Gold : ColorSchemes.Dim,
            };
        }
        tagPosRadio.SelectedItemChanged += (s, e) =>
        {
            for (int i = 0; i < 3; i++)
                tagPosLabels[i].ColorScheme = i == tagPosRadio.SelectedItem
                    ? ColorSchemes.Gold : ColorSchemes.Dim;
        };
        y += 4;

        // ── Damage Tag Style (FB-452) ────────────────────────────────
        var tagStyleLabel = FormLabel("Damage Tag Style", y);
        var tagStyleRadio = Radio("Damage Tag Style", y,
            new[] { "Brackets", "Bare", "Chip" },
            (int)settings.DamageTagStyle,
            v => settings.DamageTagStyle = (DamageTagStyle)v);
        y += 2;
        string[] tagStylePreview =
        {
            "Brackets  [FIRE]",
            "Bare      FIRE",
            "Chip      ◆FIRE◆",
        };
        var tagStyleLabels = new Label[3];
        for (int i = 0; i < 3; i++)
        {
            tagStyleLabels[i] = new Label
            {
                Text = tagStylePreview[i], X = Pos.Center() + LabelOffset,
                Y = y + i, Width = 40, Height = 1,
                ColorScheme = i == (int)settings.DamageTagStyle
                    ? ColorSchemes.Gold : ColorSchemes.Dim,
            };
        }
        tagStyleRadio.SelectedItemChanged += (s, e) =>
        {
            for (int i = 0; i < 3; i++)
                tagStyleLabels[i].ColorScheme = i == tagStyleRadio.SelectedItem
                    ? ColorSchemes.Gold : ColorSchemes.Dim;
        };
        y += 4;

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
        backBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (onBack != null) onBack();
            else TitleScreen.Show(mainWindow);
        };

        mainWindow.Add(
            header, headerRule, saveLabel,
            gameplayHeader, autoPickupLabel, autoPickupCheck,
            textSpeedLabel, textSpeedRadio,
            displayHeader, footstepLabel, footstepCheck, flashLabel, flashCheck,
            accessHeader, shakeLabel, shakeCheck, shakeDesc,
            breakdownLabel, breakdownRadio,
            previewLabels[0], previewLabels[1], previewLabels[2], previewLabels[3],
            particleLabel, particleRadio,
            particleLabels[0], particleLabels[1], particleLabels[2], particleLabels[3],
            tagPosLabel, tagPosRadio,
            tagPosLabels[0], tagPosLabels[1], tagPosLabels[2],
            tagStyleLabel, tagStyleRadio,
            tagStyleLabels[0], tagStyleLabels[1], tagStyleLabels[2],
            controlsHeader, keybindLabel, keybindBtn,
            aboutHeader, versionLabel, tributeLabel,
            resetBtn, backBtn);

        NavigationHelper.EnableGameNavigation(mainWindow);
        backBtn.SetFocus();
    }

    private static Label FormLabel(string text, int y) =>
        ScreenHeader.FormLabel(text, Pos.Center() + LabelOffset, y, LabelWidth);
}
