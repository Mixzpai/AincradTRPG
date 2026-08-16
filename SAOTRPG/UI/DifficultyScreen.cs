using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Difficulty select: a tier list card with a risk meter beside a detail card that follows the
// selection. Arrow keys move the list; boundary handlers step out of it to the footer row.
public static class DifficultyScreen
{
    private const int DefaultTier = 3;

    // Card block geometry. The Run Modifiers row below aligns to the same left edge.
    // List holds "Name(13) + meter(6)" plus the selector's own marker; detail holds the wrapped
    // description and the stat rows.
    private const int ListCardWidth   = 26;
    private const int DetailCardWidth = 44;
    private const int CardGap         = 2;
    private const int TotalCardsWidth = ListCardWidth + CardGap + DetailCardWidth;

    // Pips in the risk meter.
    private const int RiskPips = 6;

    private static EventHandler<Key>? _escHandler;

    // Unhook Esc handler before transitioning AWAY so Esc→TitleScreen doesn't leak into GameScreen.
    public static void UnhookEscHandler(Window mainWindow)
    {
        if (_escHandler != null) { mainWindow.KeyDown -= _escHandler; _escHandler = null; }
    }

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        // Character creation and the modifier picker both return here, and either one's
        // handler would otherwise stay attached and fire alongside this screen's.
        NavigationHelper.UnhookScreenEscHandlers(mainWindow);
        var sw = DebugLogger.StartTimer("DifficultyScreen.Show");
        DebugLogger.LogScreen("DifficultyScreen");

        var tiers = DifficultyData.GetTiers();
        int count = tiers.Length;

        // ── Header (centered) ────────────────────────────────────────
        var (header, headerRule) = ScreenHeader.Create("Select Difficulty", 1, 20);

        // ── Two cards: the tier list on the left, the selected tier's detail on the right ──
        // The list carries only name + risk meter; the description lives in the detail card, so
        // the same sentence is not printed twice on one screen.
        var labels = new string[count];
        for (int i = 0; i < count; i++)
            labels[i] = $"{tiers[i].Name,-13}{RiskMeter(tiers[i].MobStatPercent)}";

        Pos cardsLeft = ScreenHeader.Axis - TotalCardsWidth / 2;
        int cardHeight = count + Card.ChromeHeight;

        const int CardsTop = 4;
        var listCard = new Card("SELECT DIFFICULTY", cardsLeft, CardsTop, ListCardWidth, cardHeight);
        var tierRadio = new OptionSelector
        {
            X = Card.InsideX(cardsLeft), Y = Card.InsideY(CardsTop),
            Labels = labels,
            Value = DefaultTier,
            Width = ListCardWidth - Card.ChromeWidth, Height = count,
            SchemeName = ColorSchemes.TierRadioName,
        };

        // Tab skips the tier list entirely. Arrows are this screen's navigation and reach every
        // tier; Tab is here to get to the Run Modifiers row and the footer, so stopping on the
        // list only made it land on the selected row and walk forward from there — five of eight
        // tiers, in an order that depends on the current selection.
        //
        // Both settings are needed and they are different things: TabStop keeps Tab off the
        // selector itself, TabBehavior keeps it out of the rows underneath. Set before the arrow
        // wiring below, because assigning TabBehavior rebuilds the rows.
        //
        // SetFocus still works on a NoStop view, so the arrow boundary handlers and the initial
        // focus below are unaffected — NoStop governs Tab traversal, not focusability.
        tierRadio.TabStop = TabBehavior.NoStop;
        tierRadio.TabBehavior = TabBehavior.NoStop;

        Pos detailLeft = cardsLeft + ListCardWidth + CardGap;
        var detailCard = new Card(tiers[DefaultTier].Name.ToUpperInvariant(),
            detailLeft, CardsTop, DetailCardWidth, cardHeight);

        int detailInner = DetailCardWidth - Card.ChromeWidth;
        var descLabel = new Label
        {
            Text = tiers[DefaultTier].Description,
            X = Card.InsideX(detailLeft), Y = Card.InsideY(CardsTop),
            Width = detailInner, Height = 3, SchemeName = ColorSchemes.DimName,
        };
        // Fixed width, and every row padded to it: these strings change as the selection moves,
        // and a shorter one must overwrite the tail of the longer one it replaces.
        var statsLabel = new Label
        {
            Text = FormatModifiers(tiers[DefaultTier], detailInner),
            X = Card.InsideX(detailLeft), Y = Card.InsideY(CardsTop) + 4,
            Width = detailInner, Height = 4, SchemeName = ColorSchemes.BodyName,
        };

        // Update the detail card on every tier change.
        tierRadio.ValueChanged += (s, e) =>
        {
            int idx = tierRadio.Value ?? 0;
            if (idx < 0 || idx >= count) return;
            detailCard.Title = tiers[idx].Name.ToUpperInvariant();
            descLabel.Text = tiers[idx].Description;
            statsLabel.Text = FormatModifiers(tiers[idx], detailInner);
            detailCard.SetNeedsDraw();
        };

        // Everything below hangs off the bottom of the card block. Permadeath is universal, so
        // this is a statement rather than a choice — it stays centred and quiet.
        int hcY = CardsTop + cardHeight + 1;
        var permadeathNotice = new Label
        {
            Text = "All runs are permadeath — death deletes your save.",
            X = Pos.Center(), Y = hcY,
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DangerName,
        };

        // ── Run Modifiers row — unlocked after first F100 victory.
        int modY = hcY + 2;
        bool modifiersUnlocked = ProfileData.HasCompletedGame;
        var modifierBtn = DialogHelper.CreateMenuButton(
            modifiersUnlocked ? "Run Modifiers" : "Run Modifiers (locked)");
        modifierBtn.X = cardsLeft; modifierBtn.Y = modY;
        modifierBtn.Enabled = modifiersUnlocked;
        if (!modifiersUnlocked) modifierBtn.SchemeName = ColorSchemes.DimName;
        // Chained off the button's right edge: the label is 9 columns longer when the feature
        // is locked, and a fixed offset put the chip text on top of it in that state.
        var modifierChipLabel = new Label
        {
            Text = FormatModifierChips(),
            X = Pos.Right(modifierBtn) + 2, Y = modY,
            Width = Dim.Auto(), SchemeName = ColorSchemes.DimName,
        };
        modifierBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            ModifierSelectScreen.Show(mainWindow, () =>
            {
                // Re-render DifficultyScreen after modifier picker closes.
                Show(mainWindow);
            });
        };

        // ── Footer buttons (centered row) ────────────────────────────
        int btnY = hcY + 4;
        // Pos.Align centres the row as a group, so no width has to be totted up by hand and the
        // row stays centred if a label ever changes. Menu buttons already pad two columns each
        // side, so the single column Align inserts reads as a five-column gap between words.
        var continueBtn = DialogHelper.CreateMenuButton("Continue");
        continueBtn.X = Pos.Align(Alignment.Center, AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems); continueBtn.Y = btnY;
        var backBtn = DialogHelper.CreateMenuButton("Back");
        backBtn.X = Pos.Align(Alignment.Center, AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems); backBtn.Y = btnY;

        var hintPairs = new[] { ("↑↓", "select"), ("enter", "continue"), ("esc", "back") };
        var hint = ScreenHeader.KeyHints(
            ScreenHeader.Axis - ScreenHeader.KeyHintsWidth(hintPairs) / 2, btnY + 2, hintPairs);

        continueBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            CharacterCreationScreen.Show(mainWindow, tierRadio.Value ?? 0);
        };

        backBtn.Accepting += (s, e) => { e.Handled = true; TitleScreen.Show(mainWindow); };

        _escHandler = (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc) { TitleScreen.Show(mainWindow); e.Handled = true; }
        };
        mainWindow.KeyDown += _escHandler;

        // The Run Modifiers row drops out of the nav chain while it is locked: SetFocus on a
        // disabled button is a no-op, so pointing a boundary at it strands focus and eats the key.
        View belowList   = modifiersUnlocked ? modifierBtn : continueBtn;
        View aboveFooter = modifiersUnlocked ? modifierBtn : (View)tierRadio;

        // ── Arrow nav ── the ends of the tier list are exits: Down past the last tier drops to the
        // row beneath it, Up past the first reaches the footer.
        tierRadio.KeyDown += (s, e) =>
        {
            if (e.KeyCode is KeyCode.CursorDown or KeyCode.S
                && tierRadio.Value == count - 1)
            {
                belowList.SetFocus(); e.Handled = true;
            }
            else if (e.KeyCode is KeyCode.CursorUp or KeyCode.W
                && tierRadio.Value == 0)
            {
                backBtn.SetFocus(); e.Handled = true;
            }
        };

        // Enter on the tier list confirms the run, which is what the hint footer has always
        // promised. It cannot be done from KeyDown: SelectorBase turns Enter into Activate+Accept
        // INSIDE the selector, so the key is consumed before the screen's own handler ever sees it
        // — measured, and the KeyDown case that used to live above was dead from the 2.4.5
        // migration onward. Accepting is where the selector surfaces it.
        tierRadio.Accepting += (s, e) =>
        {
            e.Handled = true;
            continueBtn.InvokeCommand(Command.HotKey);
        };

        // Attached after the boundary handler above so that one runs first and keeps the ends of
        // the list as exits rather than wrapping.
        NavigationHelper.EnableSelectorArrowNav(tierRadio);

        // Run Modifiers button ↔ tier list / footer row (replaces the old
        // hardcore checkbox in the nav chain).
        NavigationHelper.WireUpDown(modifierBtn, tierRadio, continueBtn);

        // Footer buttons: Up→the row above, Down→tier list, Left/Right between them
        var footerBtns = new[] { continueBtn, backBtn };
        for (int i = 0; i < footerBtns.Length; i++)
        {
            int li = (i - 1 + footerBtns.Length) % footerBtns.Length;
            int ri = (i + 1) % footerBtns.Length;
            NavigationHelper.WireUpDown(footerBtns[i], aboveFooter, tierRadio);
            NavigationHelper.WireLeftRight(footerBtns[i], footerBtns[li], footerBtns[ri]);
        }

        // ── Assemble (all centered) ──────────────────────────────────
        mainWindow.Add(header, headerRule,
            listCard, detailCard,
            tierRadio, descLabel, statsLabel,
            permadeathNotice,
            modifierBtn, modifierChipLabel,
            continueBtn, backBtn);
        mainWindow.Add(hint.ToArray());

        tierRadio.SetFocus();
        DebugLogger.EndTimer("DifficultyScreen.Show", sw);
    }

    // A filled-pip meter for enemy strength, so relative danger is readable at a glance instead of
    // having to be inferred from three percentages. Derived from MobStatPercent (40 to 300 across
    // the tiers) rather than from list position, so the pips mean something. Thresholds are chosen
    // to be non-decreasing down the ordered list while still using the full range.
    private static string RiskMeter(int mobStatPercent)
    {
        int filled =
            mobStatPercent <  70 ? 1 :
            mobStatPercent <  90 ? 2 :
            mobStatPercent < 110 ? 3 :
            mobStatPercent < 150 ? 4 :
            mobStatPercent < 200 ? 5 : 6;
        return ScreenHeader.Meter(filled, RiskPips);
    }

    // Stat rows for the detail card: label left, value right, each row padded to the card's inner
    // width so a shorter value cannot leave the tail of the previous one behind.
    private static string FormatModifiers(DifficultyData.DifficultyTier tier, int width)
    {
        string regen = tier.RegenInterval > 0 ? $"1 HP / {tier.RegenInterval} turns" : "none";
        return string.Join('\n',
            StatRow("Enemy Stats", $"{tier.MobStatPercent}%", width),
            StatRow("XP Reward",   $"{tier.XpPercent}%",      width),
            StatRow("Col Streak",  $"+{tier.ColStreakBonus}%", width),
            StatRow("Regen",       regen,                     width));
    }

    private static string StatRow(string label, string value, int width)
    {
        int pad = Math.Max(1, width - label.Length - value.Length);
        return label + new string(' ', pad) + value;
    }

    // Compact chip summary of active run modifiers shown next to the button.
    private static string FormatModifierChips()
    {
        // No leading pad — the label is positioned off the button's right edge.
        if (RunModifiers.Active.Count == 0) return "(none active)";
        double mul = RunModifiers.TotalScoreMultiplier();
        var names = string.Join(", ", RunModifiers.Active
            .Select(m => RunModifiers.Definitions[m].Name));
        if (names.Length > 28) names = names[..27] + "…";
        return $"×{mul:F2}  {names}";
    }

}
