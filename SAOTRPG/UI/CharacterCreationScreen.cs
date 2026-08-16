using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Character creation — identity, stat allocation, live combat preview, Continue/Back.
// Up/Down walks the whole form; a stat row is adjusted in place with Left/Right.
public static class CharacterCreationScreen
{
    private const int MaxNameLength = 15;

    private const int StartingSkillPoints = 10;

    // A stat row is ONE focusable control, not a name plus a value plus a pair of stepper
    // buttons: it carries the stat, an allocation meter, the running total and what a point
    // buys, and it is adjusted in place with Left/Right. Six focus stops instead of twelve, and
    // the thing that highlights is the row the player is actually changing.
    //
    // The fields are padded to one fixed width because Terminal.Gui's Button paints the Focus
    // attribute only behind its text — see DialogHelper.CreateMenuRow, which owns that idiom.
    private const int StatNameWidth   = 13;
    private const int StatValueWidth  = 2;
    private const int StatEffectWidth = 12;
    private const int StatBarPips     = StartingSkillPoints;

    // Two columns of row decoration, then the fields and the gaps between them.
    private const int StatRowWidth =
        2 + StatNameWidth + 1 + StatBarPips + 2 + StatValueWidth + 2 + StatEffectWidth;

    // The stat rows are the widest content on the screen, so the section hairlines span exactly
    // one of them — derived rather than restated, so the two cannot drift apart.
    private const int SectionWidth = StatRowWidth;

    // Vertical rhythm. The form is one top-anchored stack, so on a short terminal it does not
    // collide with anything — it simply runs off the bottom, which is how the hint row ended up
    // 2 rows past the last row of a 120x30 window.
    //
    // The only give it has is three blank rows in its tail: after the preview text, before the
    // footer and before the hint. They are surrendered one at a time from the BOTTOM UP, so the
    // separation that matters most is the last to go, and nothing moves at all once the terminal
    // is tall enough.
    private const int StatsTop = 14;
    private const int TailGaps = 3;

    private static int Squeeze(int interiorHeight) =>
        interiorHeight <= 0 ? 0 : Math.Clamp(FormBottom - (interiorHeight - 1), 0, TailGaps);

    // A tail row, given how many surrenderable gaps sit below it. Resolved against the host at
    // layout time rather than at build time, so the screen survives a terminal resize.
    // Blank means "roll one". A number is taken as-is so a seed printed on the death card can be
    // typed straight back in; anything else is hashed, which lets a player seed a run with a word.
    private static int? ParseSeed(string? text)
    {
        text = text?.Trim() ?? "";
        if (text.Length == 0) return null;
        // StableHash, not GetHashCode: string hashing is randomised per process, so seeding a
        // run with a word gave a different world every launch.
        return int.TryParse(text, out int n) ? n : Systems.RunRng.StableHash(text);
    }

    private static Pos TailRow(int designRow, int gapsBelow, View host) =>
        Pos.Func(
            v => designRow - Math.Clamp(Squeeze(v?.Viewport.Height ?? 0) - gapsBelow, 0, TailGaps - gapsBelow),
            host);

    private static EventHandler<Key>? _escHandler;

    private static readonly (string Name, string Effect)[] Stats =
    {
        ("Vitality",     "+10 Max HP"),
        ("Strength",     "+2 Attack"),
        ("Endurance",    "+2 Defense"),
        ("Dexterity",    "+Crit Rate"),
        ("Agility",      "+2 Speed"),
        ("Intelligence", "+2 Skill Dmg"),
    };

    private static readonly string[] GenderOptions = { "Male", "Female" };

    // Declared after Stats: static field initializers run in declaration order, so anything
    // reading Stats.Length has to come below it or it reads a null array.
    private static readonly int PreviewTop = StatsTop + Stats.Length + 1;
    private static readonly int FooterTop  = PreviewTop + 6;
    private static readonly int FormBottom = FooterTop + 2;

    // Unhook before transitioning AWAY so Esc→DifficultyScreen doesn't leak into GameScreen.
    public static void UnhookEscHandler(Window mainWindow)
    {
        if (_escHandler != null) { mainWindow.KeyDown -= _escHandler; _escHandler = null; }
    }

    public static void Show(Window mainWindow, int difficulty = 3)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        NavigationHelper.UnhookScreenEscHandlers(mainWindow);
        var sw = DebugLogger.StartTimer("CharacterCreationScreen.Show");
        DebugLogger.LogScreen("CharacterCreationScreen");

        int skillPoints = StartingSkillPoints;
        int[] allocated = new int[Stats.Length];

        // ── Header ───────────────────────────────────────────────────
        var (header, headerRule) = ScreenHeader.Create("Create Your Character", 1, 24);

        // ── Identity section (centered) ── Label(14) right-aligned, 3-gap, TextField.
        // inputCol anchors to label right + gap → no overlap at any width.
        //
        // leftCol is measured from the screen axis, not Pos.Center(): a Pos.Center() offset
        // resolves against the width of whichever view it is applied to, so the stat rows —
        // whose label, value, buttons and effect text are all different widths — each landed
        // on a different origin and the row drifted apart.
        int labelW = 14, gap = 3, inputW = 22;
        Pos leftCol = ScreenHeader.Axis - (labelW + gap + inputW) / 2;

        var firstLabel = ScreenHeader.FormLabel("First Name:", leftCol, 4, labelW);
        var firstField = new TextField { X = Pos.Right(firstLabel) + gap, Y = 4, Width = inputW };
        var firstHint  = new Label { Text = $"(max {MaxNameLength})", X = Pos.Right(firstField) + 1, Y = 4, SchemeName = ColorSchemes.DimName };

        var lastLabel = ScreenHeader.FormLabel("Last Name:", leftCol, 6, labelW);
        var lastField = new TextField { X = Pos.Right(lastLabel) + gap, Y = 6, Width = inputW };
        var lastHint  = new Label { Text = $"(max {MaxNameLength})", X = Pos.Right(lastField) + 1, Y = 6, SchemeName = ColorSchemes.DimName };

        var seedLabel = ScreenHeader.FormLabel("Seed:", leftCol, 10, labelW);
        var seedField = new TextField { X = Pos.Right(seedLabel) + gap, Y = 10, Width = inputW };
        var seedHint  = new Label { Text = "(blank = random)", X = Pos.Right(seedField) + 1, Y = 10,
                                    SchemeName = ColorSchemes.DimName };

        var genderLabel = ScreenHeader.FormLabel("Gender:", leftCol, 8, labelW);
        var genderRadio = new OptionSelector
        {
            X = Pos.Right(genderLabel) + gap, Y = 8, Labels = GenderOptions,
            Orientation = Orientation.Horizontal, Value = 0,
        };

        // ── Stats section ────────────────────────────────────────────
        var statsHeader = ScreenHeader.Section("Allocate Stats", leftCol, 11, SectionWidth);
        var pointsLabel = new Label
        {
            Text = FormatPointsRemaining(skillPoints),
            X = Pos.Center(), Y = 12, Width = Dim.Auto(), SchemeName = ColorSchemes.GoldName,
        };

        var statRows = new Button[Stats.Length];

        // Forward-declared: Refresh and Adjust close over them, and both run only after the
        // whole form is built.
        Label previewLabel = null!;
        Label feedbackLabel = null!;

        void Refresh()
        {
            pointsLabel.Text = FormatPointsRemaining(skillPoints);
            pointsLabel.SchemeName = skillPoints == 0 ? ColorSchemes.DimName : ColorSchemes.GoldName;
            for (int j = 0; j < Stats.Length; j++)
                DialogHelper.SetMenuRowLabel(statRows[j], StatRowLabel(j, allocated[j]), StatRowWidth);
            previewLabel.Text = FormatPreview(allocated);
        }

        void Adjust(int idx, int delta)
        {
            if (delta > 0)
            {
                if (skillPoints <= 0) { ShowMsg(feedbackLabel, "No points left.", ColorSchemes.Danger); return; }
                allocated[idx]++;
                skillPoints--;
                ShowMsg(feedbackLabel, $"+1 {Stats[idx].Name}", ColorSchemes.Gold);
            }
            else
            {
                if (allocated[idx] <= 0) { ShowMsg(feedbackLabel, "Already at zero.", ColorSchemes.Dim); return; }
                allocated[idx]--;
                skillPoints++;
                ShowMsg(feedbackLabel, $"-1 {Stats[idx].Name}", ColorSchemes.Dim);
            }
            Refresh();
        }

        for (int i = 0; i < Stats.Length; i++)
        {
            int idx = i;
            var row = DialogHelper.CreateMenuRow(StatRowLabel(i, 0), StatRowWidth);
            row.X = leftCol;
            row.Y = StatsTop + i;

            // Enter spends a point too: a focused Button consumes Enter itself, and adding one
            // is the only thing Enter could sensibly mean on a stat row.
            row.Accepting += (s, e) => { e.Handled = true; Adjust(idx, +1); };
            row.KeyDown += (s, e) =>
            {
                int delta = StatDelta(e);
                if (delta == 0) return;

                Adjust(idx, delta);
                e.Handled = true;
            };

            statRows[i] = row;
        }

        // ── Preview section ──────────────────────────────────────────
        int previewY = PreviewTop;
        var previewHdr = ScreenHeader.Section("Preview", leftCol, previewY, SectionWidth);
        previewLabel = new Label
        {
            Text = FormatPreview(allocated),
            X = Pos.Center(), Y = previewY + 1,
            Width = Dim.Auto(), Height = 2, SchemeName = ColorSchemes.BodyName,
        };

        feedbackLabel = new Label
        {
            Text = "", X = Pos.Center(), Y = TailRow(PreviewTop + 4, 2, mainWindow),
            Width = Dim.Auto(), SchemeName = ColorSchemes.GoldName,
        };

        // ── Footer ───────────────────────────────────────────────────
        Pos footerY = TailRow(FooterTop, 1, mainWindow);
        // Pos.Align centres the pair as a group; no width arithmetic to keep in step.
        var continueBtn = DialogHelper.CreateMenuButton("Continue");
        continueBtn.X = Pos.Align(Alignment.Center, AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems); continueBtn.Y = footerY;
        var backBtn = DialogHelper.CreateMenuButton("Back");
        backBtn.X = Pos.Align(Alignment.Center, AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems); backBtn.Y = footerY;

        var navPairs = new[] { ("↑↓", "move"), ("←→ +-", "allocate"), ("esc", "back") };
        var navHint = ScreenHeader.KeyHints(
            ScreenHeader.Axis - ScreenHeader.KeyHintsWidth(navPairs) / 2,
            TailRow(FooterTop + 2, 0, mainWindow), navPairs);

        continueBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            var firstName = firstField.Text?.Trim() ?? "";
            var lastName  = lastField.Text?.Trim()  ?? "";
            int gIdx = genderRadio.Value ?? 0;
            string gender = gIdx >= 0 && gIdx < GenderOptions.Length ? GenderOptions[gIdx] : "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                ShowMsg(feedbackLabel, "Please enter both names.", ColorSchemes.Danger);
                return;
            }
            if (firstName.Length > MaxNameLength || lastName.Length > MaxNameLength)
            {
                ShowMsg(feedbackLabel, $"Names must be {MaxNameLength} chars or less.", ColorSchemes.Danger);
                return;
            }
            if (skillPoints > 0)
            {
                int confirm = DialogHelper.Query("Unspent Points",
                    $"You have {skillPoints} skill point(s) remaining.\nContinue anyway?",
                    "Go Back", "Continue");
                if (confirm != 1) return;
            }

            int slot = Dialogs.SaveSlotDialog.ShowNewGameSlotPicker();
            if (slot < 1) return;

            var tempLog = new StringGameLog(new System.Text.StringBuilder());
            var player = Player.CreateNewPlayer(firstName, lastName, gender,
                tempLog, new TerminalGuiInventoryLogger(tempLog));

            for (int i = 0; i < Stats.Length; i++)
                if (allocated[i] > 0) player.SpendSkillPoints(Stats[i].Name, allocated[i]);

            int? runSeed = ParseSeed(seedField.Text?.ToString());
            GameScreen.Show(mainWindow, player, difficulty, saveSlot: slot, runSeed: runSeed);
        };

        backBtn.Accepting += (s, e) => { e.Handled = true; DifficultyScreen.Show(mainWindow); };

        // ── Arrow key navigation ─────────────────────────────────────
        // Full vertical chain: names → gender → stats → footer → wrap.
        // Arrows only on the name fields — W and S have to reach them as text.
        NavigationHelper.WireUpDownArrowsOnly(firstField, continueBtn, lastField);
        NavigationHelper.WireUpDownArrowsOnly(lastField, firstField, genderRadio);
        NavigationHelper.WireUpDown(genderRadio, lastField, seedField);
        NavigationHelper.WireUpDownArrowsOnly(seedField, genderRadio, statRows[0]);
        // Horizontal, so the selector takes Left/Right and the wiring above keeps Up/Down.
        NavigationHelper.EnableSelectorArrowNav(genderRadio);

        // Left/Right belong to the row itself, so the stat block only needs vertical wiring.
        for (int i = 0; i < Stats.Length; i++)
            NavigationHelper.WireUpDown(statRows[i],
                i == 0 ? seedField : statRows[i - 1],
                i == Stats.Length - 1 ? continueBtn : statRows[i + 1]);

        NavigationHelper.WireUpDown(continueBtn, statRows[^1], firstField);
        NavigationHelper.WireLeftRight(continueBtn, backBtn, backBtn);
        NavigationHelper.WireUpDown(backBtn, statRows[^1], firstField);
        NavigationHelper.WireLeftRight(backBtn, continueBtn, continueBtn);

        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { DifficultyScreen.Show(mainWindow); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;

        // ── Assemble ─────────────────────────────────────────────────
        // Add order IS tab order, so the stat rows go in between the gender picker and the footer
        // rather than in their build loop — added there, Tab reached them after the Back button.
        mainWindow.Add(header, headerRule,
            firstLabel, firstField, firstHint,
            lastLabel, lastField, lastHint,
            genderLabel, genderRadio,
            seedLabel, seedField, seedHint,
            statsHeader.Caption, statsHeader.Rule, pointsLabel);
        mainWindow.Add(statRows);
        mainWindow.Add(previewHdr.Caption, previewHdr.Rule, previewLabel, feedbackLabel,
            continueBtn, backBtn);
        mainWindow.Add(navHint.ToArray());

        firstField.SetFocus();
        DebugLogger.EndTimer("CharacterCreationScreen.Show", sw);
    }

    // How far one keypress moves a stat. Left/Right is the primary gesture; +/- is what most
    // people reach for on an allocation screen, so both are bound.
    //
    // The +/- test is on the RUNE, not on KeyCode. A printable key carries its character in the
    // low bits of KeyCode, but '+' is Shift+'=' on a US layout and a driver may report either the
    // produced character or the physical key with the Shift bit set — measured against the shipped
    // assembly, Key('+').WithShift is 0x1000002B, so a bare KeyCode compare misses it while AsRune
    // still reads '+'. Accepting '=' and '_' covers the physical-key reading, and both are
    // reasonable keys to press here regardless. AsRune is empty while Alt or Ctrl is held, so
    // chords never land on a stat.
    private static int StatDelta(Key key)
    {
        if (key.KeyCode == KeyCode.CursorRight) return +1;
        if (key.KeyCode == KeyCode.CursorLeft) return -1;

        return key.AsRune.Value switch
        {
            '+' or '=' => +1,
            '-' or '_' => -1,
            _ => 0,
        };
    }

    // One stat row's text: stat, allocation meter, running total, and what a point buys.
    private static string StatRowLabel(int index, int points) =>
        Stats[index].Name.PadRight(StatNameWidth) + " " +
        ScreenHeader.Meter(points, StatBarPips) + "  " +
        points.ToString().PadLeft(StatValueWidth) + "  " +
        Stats[index].Effect.PadRight(StatEffectWidth);

    private static void ShowMsg(Label label, string text, Scheme scheme)
    {
        label.Text = text;
        label.SetScheme(scheme);
    }

    private static string FormatPointsRemaining(int pts) =>
        pts > 0 ? $"{pts} points remaining" : "All points allocated!";

    private static string FormatPreview(int[] a)
    {
        int hp = 100 + a[0] * 10;
        int atk = a[1] * 2, def = a[2] * 2, spd = a[4] * 2;
        int crit = 5 + a[3] / 2, cdmg = 10 + a[3];
        return $"HP: {hp}   ATK: {atk}   DEF: {def}   SPD: {spd}\n" +
               $"CRIT: {crit}%   CDMG: +{cdmg}   SDMG: {a[5] * 2}";
    }
}
