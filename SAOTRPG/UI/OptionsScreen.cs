using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Dialogs;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Settings screen — preferences grouped into tabs, each page scrolling independently.
// Changes auto-save to disk via UserSettings. Also reachable mid-run from the pause menu.
public static class OptionsScreen
{
    // The form is left-anchored inside the tab block: label column, a two-column gutter, then the
    // control column. It used to hang off ScreenHeader.Axis, i.e. the page's own centre once the
    // settings moved into tabs — which left ~19 dead columns down the left and pushed the control
    // column so far right that the widest selector (Footstep Style, 52 columns) ran off the page
    // edge at every terminal width, 316x91 included. Absolute columns cannot drift with the page.
    private const int LabelX     = 1;
    private const int LabelWidth = 22;
    private const int ControlX   = LabelX + LabelWidth + 2;

    // Tab block geometry. Width covers the label column, the gutter and the widest selector.
    // Height is chosen so the footer beneath it still fits the documented 120x30 minimum;
    // the one group taller than that scrolls, which is why every page is a scroller.
    private const int TabsTop    = 4;
    private const int TabsWidth  = 84;
    private const int TabsHeight = 20;
    private const int FooterTop  = TabsTop + TabsHeight + 1;

    private static EventHandler<Key>? _escHandler;

    // Unhook before transitioning AWAY. Terminal.Gui binds Esc to Command.Quit at the application
    // level, so a screen that does not consume Esc exits the game — this one has to consume it,
    // and then has to give it back when it leaves.
    public static void UnhookEscHandler(Window mainWindow)
    {
        if (_escHandler != null) { mainWindow.KeyDown -= _escHandler; _escHandler = null; }
    }

    // onBack: Back-button callback. Null = default TitleScreen return.
    // Pause menu passes a callback that reloads the save so the in-progress run is restored.
    public static void Show(Window mainWindow, Action? onBack = null)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        NavigationHelper.UnhookScreenEscHandlers(mainWindow);
        var settings = UserSettings.Current;

        // The settings used to be one ~71-row column, which did not fit the documented 120x30
        // minimum. They are grouped into tabs instead: each page is short enough to read at a
        // glance, and Left/Right moves between groups.
        //
        // Each page is still a scrolling container, because the longest group can outgrow a short
        // terminal on its own. Terminal.Gui scrolls a container on neither key presses nor focus
        // movement, so every focusable control reports its focus gain and is scrolled into view.
        var tabs = new Tabs
        {
            X = ScreenHeader.Axis - TabsWidth / 2, Y = TabsTop,
            Width = TabsWidth, Height = TabsHeight,
            SchemeName = ColorSchemes.CardName,
        };

        // Set as each group is built, so the control factories know which page to scroll.
        View currentPage = null!;

        View Page(string title)
        {
            var page = new View
            {
                Title = title, Id = title,
                CanFocus = true,
                Width = Dim.Fill(), Height = Dim.Fill(),
            };
            page.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
            tabs.Add(page);
            return page;
        }

        static void Reveal(View page, View v)
        {
            int top = v.Frame.Y;
            int bottom = top + Math.Max(v.Frame.Height, 1);
            int viewTop = page.Viewport.Y;
            int viewBottom = viewTop + page.Viewport.Height;
            if (top - 1 < viewTop) page.ScrollVertical(top - 1 - viewTop);
            else if (bottom + 1 > viewBottom) page.ScrollVertical(bottom + 1 - viewBottom);
        }

        void TrackFocus(View v)
        {
            View page = currentPage;
            v.HasFocusChanged += (s, e) => { if (e.NewValue) Reveal(page, v); };
        }

        var (header, headerRule) = ScreenHeader.Create("Options", 1, 20);

        // Below the tab block, not at Y=4 — that row is the tab bar now, and the confirmation
        // would print across the group captions the moment a setting changed.
        var saveLabel = new Label
        {
            Text = "", X = Pos.Center(), Y = TabsTop + TabsHeight,
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.SuccessName,
        };

        // Local helpers — capture saveLabel so individual call sites don't
        // have to duplicate UserSettings.Save() + feedback text.
        CheckBox Toggle(string name, int y, bool initial, Action<bool> apply)
        {
            var check = new CheckBox
            {
                Text = "",
                X = ControlX, Y = y,
                Value = initial ? CheckState.Checked : CheckState.UnChecked,
            };
            check.ValueChanging += (s, e) =>
            {
                bool v = e.NewValue == CheckState.Checked;
                apply(v);
                UserSettings.Save();
                saveLabel.Text = $"{name}: {(v ? "ON" : "OFF")}  [OK] Saved";
            };
            TrackFocus(check);
            return check;
        }

        OptionSelector Radio(string name, int y, string[] labels, int initial, Action<int> apply)
        {
            var radio = new OptionSelector
            {
                X = ControlX, Y = y, Labels = labels,
                Orientation = Orientation.Horizontal,
                Value = Math.Clamp(initial, 0, labels.Length - 1),
            };
            radio.ValueChanged += (s, e) =>
            {
                apply(radio.Value ?? 0);
                UserSettings.Save();
                saveLabel.Text = $"{name}: {labels[radio.Value ?? 0]}  [OK] Saved";
            };
            NavigationHelper.EnableSelectorArrowNav(radio);
            TrackFocus(radio);
            return radio;
        }

        // ── Gameplay ─────────────────────────────────────────────────
        var gameplayPage = Page("Gameplay");
        currentPage = gameplayPage;
        int y = 1;

        var autoPickupLabel = FormLabel("Auto-Pickup Items", y);
        var autoPickupCheck = Toggle("Auto-Pickup", y, settings.AutoPickup, v => settings.AutoPickup = v);
        y += 2;

        var textSpeedLabel = FormLabel("Text Speed", y);
        var textSpeedRadio = Radio("Text Speed", y,
            new[] { "Fast", "Normal", "Slow" },
            settings.TextSpeed, v => settings.TextSpeed = v);
        y += 3;

        int gameplayRows = y;

        // ── Display ──────────────────────────────────────────────────
        var displayPage = Page("Display");
        currentPage = displayPage;
        y = 1;

        // Style + Length + Opacity replace the single ShowFootsteps toggle.
        var footstepStyleLabel = FormLabel("Footstep Style", y);
        var footstepStyleRadio = Radio("Footstep Style", y,
            new[] { "Off", "Dots", "Dashes", "Paws", "Boots", "Chevrons" },
            (int)settings.FootstepStyle,
            v => settings.FootstepStyle = (FootstepStyle)v);
        y += 2;

        // Length picker maps option index → turn count. 1000 = "unlimited" hard ceiling.
        int[] footstepLengthOptions = { 0, 5, 10, 20, 50, 1000 };
        string[] footstepLengthLabels = { "Off", "5", "10", "20", "50", "Unlim" };
        int currentLengthIdx = Array.IndexOf(footstepLengthOptions, settings.FootstepLength);
        if (currentLengthIdx < 0) currentLengthIdx = 2;
        var footstepLengthLabel = FormLabel("Footstep Length", y);
        var footstepLengthRadio = Radio("Footstep Length", y,
            footstepLengthLabels, currentLengthIdx,
            v => settings.FootstepLength = footstepLengthOptions[v]);
        y += 2;

        var footstepOpacityLabel = FormLabel("Footstep Opacity", y);
        var footstepOpacityRadio = Radio("Footstep Opacity", y,
            new[] { "Subtle", "Medium", "Bold" },
            (int)settings.FootstepOpacity,
            v => settings.FootstepOpacity = (FootstepOpacity)v);
        y += 2;

        var flashLabel = FormLabel("Damage Flash", y);
        var flashCheck = Toggle("Damage Flash", y, settings.ShowDamageFlash, v => settings.ShowDamageFlash = v);
        y += 2;

        // Sun shadows and contact shading across the map. It has a control because it trades
        // contrast for depth: a glyph in deep shadow is dimmer than one in the open, which is
        // the whole point of it and also a reason someone might want it off.
        var shadingLabel = FormLabel("Terrain Shading", y);
        var shadingCheck = Toggle("Terrain Shading", y, settings.TerrainShading,
            v => settings.TerrainShading = v);
        y += 1;
        var shadingDesc = new Label
        {
            Text = "Trees and walls cast graded shadows that swing with the sun.",
            X = ControlX, Y = y,
            Width = 62, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        y += 2;

        // Eighth-block bars vs. ASCII fallback for HP/XP/SAT.
        var asciiBarsLabel = FormLabel("ASCII Stat Bars", y);
        var asciiBarsCheck = Toggle("ASCII Stat Bars", y, settings.UseAsciiStatBars,
            v => settings.UseAsciiStatBars = v);
        y += 1;
        var asciiBarsDesc = new Label
        {
            Text = "Use ASCII bars if eighth-block (▏▎▍▌▋▊▉) misrenders.",
            X = ControlX, Y = y,
            Width = 56, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        y += 2;

        // Status tray labels. Shift+S flips this in play; here it is set before a run starts.
        var trayVerboseLabel = FormLabel("Status Tray Labels", y);
        var trayVerboseCheck = Toggle("Status Tray Labels", y, settings.StatusTrayVerbose,
            v => settings.StatusTrayVerbose = v);
        y += 1;
        var trayVerboseDesc = new Label
        {
            Text = "Spell status effects out (POISON·3) instead of letters (P×3).",
            X = ControlX, Y = y,
            Width = 62, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        y += 2;

        // Same family as the bars above: a glyph fallback for a font that lacks the code point.
        var asciiGlyphsLabel = FormLabel("ASCII Guide Arrows", y);
        var asciiGlyphsCheck = Toggle("ASCII Guide Arrows", y, settings.UseAsciiDisclosureGlyphs,
            v => settings.UseAsciiDisclosureGlyphs = v);
        y += 1;
        var asciiGlyphsDesc = new Label
        {
            Text = "Player Guide folds use > / v if ▸ / ▾ misrender.",
            X = ControlX, Y = y,
            Width = 56, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        y += 2;

        int displayRows = y;

        // ── Accessibility ────────────────────────────────────────────
        var accessPage = Page("Accessibility");
        currentPage = accessPage;
        y = 1;

        // Reduce motion — the master switch. It overrides Screen Shake and Particle Density
        // below rather than reading alongside them, so the two cannot disagree.
        var motionLabel = FormLabel("Reduce Motion", y);
        var motionCheck = Toggle("Reduce Motion", y, settings.ReduceMotion,
            v => settings.ReduceMotion = v);
        y += 1;
        var motionDesc = new Label
        {
            Text = "Stops looping visuals, shake, particles and fades.",
            X = ControlX, Y = y,
            Width = 50, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        y += 2;

        // Colour theme. Vertical rather than horizontal, because the theme names are long and a
        // horizontal selector would run off the tab page.
        var themeLabel = FormLabel("Colour Theme", y);
        var themeRadio = new OptionSelector
        {
            X = ControlX, Y = y,
            Labels = ColorTheme.All.Select(t => t.Name).ToArray(),
            Value = Array.FindIndex(ColorTheme.All, t => t.Id == settings.ColorTheme),
        };
        var themeDesc = new Label
        {
            Text = ColorTheme.ById(settings.ColorTheme).Description,
            X = ControlX, Y = y + ColorTheme.All.Length,
            Width = 50, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        themeRadio.ValueChanged += (s, e) =>
        {
            int idx = Math.Clamp(themeRadio.Value ?? 0, 0, ColorTheme.All.Length - 1);
            ColorTheme picked = ColorTheme.All[idx];
            settings.ColorTheme = picked.Id;
            UserSettings.Save();
            themeDesc.Text = picked.Description;

            // Republishing the schemes repaints every view bound by SchemeName immediately. The
            // handful bound with SetScheme hold their Scheme object, so the screen is rebuilt too
            // — deferred out of this handler, because rebuilding disposes the control raising it.
            ColorSchemes.ApplyTheme(picked.Id);
            AppHost.App.Invoke(() => Show(mainWindow, onBack));
        };
        NavigationHelper.EnableSelectorArrowNav(themeRadio);
        TrackFocus(themeRadio);
        y += ColorTheme.All.Length + 2;

        var shakeLabel = FormLabel("Screen Shake", y);
        var shakeCheck = Toggle("Screen Shake", y, settings.ScreenShakeEnabled,
            v => settings.ScreenShakeEnabled = v);
        y += 1;
        var shakeDesc = new Label
        {
            Text = "Brief viewport jitter on crits and heavy hits.",
            X = ControlX, Y = y,
            Width = 50, Height = 1, SchemeName = ColorSchemes.DimName,
        };
        y += 2;

        // Damage breakdown — 4-mode cycle + 4-line preview box.
        var breakdownLabel = FormLabel("Damage Breakdown", y);
        var breakdownRadio = Radio("Damage Breakdown", y,
            new[] { "Off", "Concise", "Medium", "Verbose" },
            (int)settings.DamageBreakdownMode,
            v => settings.DamageBreakdownMode = (DamageBreakdownMode)v);
        y += 2;

        // Static preview lines — frozen samples, not live. Selected mode highlights Gold,
        // others dim, so format comparison is immediate without farming a monster.
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
                Text = previewLines[i], X = LabelX,
                Y = y + i, Width = Dim.Auto(), Height = 1,
                SchemeName = i == (int)settings.DamageBreakdownMode
                    ? ColorSchemes.GoldName : ColorSchemes.DimName,
            };
        }
        breakdownRadio.ValueChanged += (s, e) =>
        {
            for (int i = 0; i < 4; i++)
                previewLabels[i].SchemeName = i == breakdownRadio.Value
                    ? ColorSchemes.GoldName : ColorSchemes.DimName;
        };
        y += 5;

        int accessRows = y;

        // ── Combat Text ──────────────────────────────────────────────
        var combatPage = Page("Combat Text");
        currentPage = combatPage;
        y = 1;

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
                Text = particlePreview[i], X = LabelX,
                Y = y + i, Width = Dim.Auto(), Height = 1,
                SchemeName = i == (int)settings.ParticleDensity
                    ? ColorSchemes.GoldName : ColorSchemes.DimName,
            };
        }
        particleRadio.ValueChanged += (s, e) =>
        {
            for (int i = 0; i < 4; i++)
                particleLabels[i].SchemeName = i == particleRadio.Value
                    ? ColorSchemes.GoldName : ColorSchemes.DimName;
        };
        y += 5;

        // ── Damage Tag Position ──────────────────────────────────────
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
                Text = tagPosPreview[i], X = LabelX,
                Y = y + i, Width = Dim.Auto(), Height = 1,
                SchemeName = i == (int)settings.DamageTagPosition
                    ? ColorSchemes.GoldName : ColorSchemes.DimName,
            };
        }
        tagPosRadio.ValueChanged += (s, e) =>
        {
            for (int i = 0; i < 3; i++)
                tagPosLabels[i].SchemeName = i == tagPosRadio.Value
                    ? ColorSchemes.GoldName : ColorSchemes.DimName;
        };
        y += 4;

        // ── Damage Tag Style ─────────────────────────────────────────
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
                Text = tagStylePreview[i], X = LabelX,
                Y = y + i, Width = Dim.Auto(), Height = 1,
                SchemeName = i == (int)settings.DamageTagStyle
                    ? ColorSchemes.GoldName : ColorSchemes.DimName,
            };
        }
        tagStyleRadio.ValueChanged += (s, e) =>
        {
            for (int i = 0; i < 3; i++)
                tagStyleLabels[i].SchemeName = i == tagStyleRadio.Value
                    ? ColorSchemes.GoldName : ColorSchemes.DimName;
        };
        y += 4;

        int combatRows = y;

        // ── Controls and About ───────────────────────────────────────
        var aboutPage = Page("About");
        currentPage = aboutPage;
        y = 1;

        var keybindLabel = FormLabel("Key Bindings", y);
        var keybindBtn = new Button
        {
            Text = " Rebind ", X = ControlX, Y = y,
            SchemeName = ColorSchemes.ButtonName,
            ShadowStyle = null,
        };
        keybindBtn.Accepting += (s, e) => { e.Handled = true; KeybindDialog.Show(); };
        TrackFocus(keybindBtn);
        y += 2;

        var keyRefLabel = FormLabel("Key Reference", y);
        var keyRefBtn = new Button
        {
            Text = " Open (H) ", X = ControlX, Y = y,
            SchemeName = ColorSchemes.ButtonName,
            ShadowStyle = null,
        };
        keyRefBtn.Accepting += (s, e) => { e.Handled = true; HelpDialog.Show(); };
        TrackFocus(keyRefBtn);
        y += 3;


        var versionLabel = new Label
        {
            Text = $"AincradTRPG {AppVersion.Display}",
            X = LabelX, Y = y, Width = Dim.Auto(), Height = 1,
            SchemeName = ColorSchemes.BodyName,
        };
        var tributeLabel = new Label
        {
            Text = "An SAO-themed ASCII roguelike",
            X = LabelX, Y = y + 1, Width = Dim.Auto(), Height = 1,
            SchemeName = ColorSchemes.DimName,
        };
        y += 4;

        // ── Footer buttons ───────────────────────────────────────────
        // Pos.Align centres the pair as a group; no width arithmetic to keep in step.
        var resetBtn = DialogHelper.CreateMenuButton("Reset Defaults");
        resetBtn.X = Pos.Align(Alignment.Center, AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems); resetBtn.Y = FooterTop;
        resetBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            if (!DialogHelper.ConfirmAction("Reset", "all settings to defaults")) return;
            UserSettings.ResetToDefaults();
            Show(mainWindow);
        };

        var backBtn = DialogHelper.CreateMenuButton("Back", isDefault: true);
        backBtn.X = Pos.Align(Alignment.Center, AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems); backBtn.Y = FooterTop;
        // One definition of "leave this screen", used by the button and by Esc.
        void GoBack()
        {
            if (onBack != null) onBack();
            else TitleScreen.Show(mainWindow);
        }

        backBtn.Accepting += (s, e) => { e.Handled = true; GoBack(); };

        // Without this Esc falls through to Terminal.Gui's application-level Quit binding and
        // exits the game — after Difficulty, character creation and the modifier picker have all
        // taught the player that Esc means "back".
        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { GoBack(); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;

        // The footer sits under the tab block, outside it: Reset and Back apply to the whole
        // screen, not to whichever group happens to be showing.
        var hintPairs = new[] { ("↑↓", "move"), ("←→", "group"), ("esc", "back") };
        var hint = ScreenHeader.KeyHints(
            ScreenHeader.Axis - ScreenHeader.KeyHintsWidth(hintPairs) / 2, FooterTop + 2, hintPairs);

        gameplayPage.Add(autoPickupLabel, autoPickupCheck, textSpeedLabel, textSpeedRadio);
        displayPage.Add(
            footstepStyleLabel, footstepStyleRadio,
            footstepLengthLabel, footstepLengthRadio,
            footstepOpacityLabel, footstepOpacityRadio,
            flashLabel, flashCheck,
            shadingLabel, shadingCheck, shadingDesc,
            asciiBarsLabel, asciiBarsCheck, asciiBarsDesc, trayVerboseLabel, trayVerboseCheck, trayVerboseDesc,
            asciiGlyphsLabel, asciiGlyphsCheck, asciiGlyphsDesc);
        accessPage.Add(
            motionLabel, motionCheck, motionDesc,
            themeLabel, themeRadio, themeDesc,
            shakeLabel, shakeCheck, shakeDesc,
            breakdownLabel, breakdownRadio,
            previewLabels[0], previewLabels[1], previewLabels[2], previewLabels[3]);
        combatPage.Add(
            particleLabel, particleRadio,
            particleLabels[0], particleLabels[1], particleLabels[2], particleLabels[3],
            tagPosLabel, tagPosRadio,
            tagPosLabels[0], tagPosLabels[1], tagPosLabels[2],
            tagStyleLabel, tagStyleRadio,
            tagStyleLabels[0], tagStyleLabels[1], tagStyleLabels[2]);
        int aboutRows = y;

        aboutPage.Add(keybindLabel, keybindBtn, versionLabel, tributeLabel);

        // Each page scrolls independently, so each needs its own content height — and a page
        // whose declared height falls short of its layout puts the last control past the end of
        // the scroll range, where no key reaches it. These were hand-maintained constants and had
        // drifted on three of the five pages, About by one row in the direction that clips. Take
        // the row the builder actually reached instead, so the two cannot disagree again.
        gameplayPage.SetContentHeight(gameplayRows);
        displayPage.SetContentHeight(displayRows);
        accessPage.SetContentHeight(accessRows);
        combatPage.SetContentHeight(combatRows);
        aboutPage.SetContentHeight(aboutRows);

        mainWindow.Add(header, headerRule, saveLabel, tabs, resetBtn, backBtn);
        mainWindow.Add(hint.ToArray());

        NavigationHelper.EnableGameNavigation(mainWindow);
        // First control of the first group, not Back: focus drives each page's scroll position,
        // and focusing a control near the bottom would open that page already scrolled.
        // Esc still leaves from anywhere.
        autoPickupCheck.SetFocus();
    }

    private static Label FormLabel(string text, int y) =>
        ScreenHeader.FormLabel(text, LabelX, y, LabelWidth);

}
