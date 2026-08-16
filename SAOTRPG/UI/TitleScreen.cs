using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Main title screen -- Aincrad floating castle with menu.
public static class TitleScreen
{
    // Solid letterforms. The font this came from draws a 3D edge with box-drawing glyphs; those
    // are gone, which is what dated it, and it also freed a row — the old last line was pure
    // shadow. Each row is its own Label so it can carry one step of a vertical gradient.
    private static readonly string[] BannerRows =
    {
        " █████  ██ ███    ██  ██████ ██████   █████  ██████  ",
        "██   ██ ██ ████   ██ ██      ██   ██ ██   ██ ██   ██ ",
        "███████ ██ ██ ██  ██ ██      ██████  ███████ ██   ██ ",
        "██   ██ ██ ██  ██ ██ ██      ██   ██ ██   ██ ██   ██ ",
        "██   ██ ██ ██   ████  ██████ ██   ██ ██   ██ ██████  ",
    };

    private const int BannerWidth = 53;

    // Vertical rhythm. The banner sits high, the cards under it, and the footer keeps its
    // bottom anchor — the star field is what makes the space between them read as sky.
    private const int BannerY      = 6;
    private const int SubTitleRow  = 13;
    private const int QuoteRow     = 15;
    private const int CardsTop     = 17;

    // Card block: the menu column plus a last-run summary beside it.
    private const int MenuCardWidth  = DialogHelper.MenuRowWidth + Card.ChromeWidth;
    private const int RunCardWidth   = 34;
    private const int CardGap        = 2;
    private const int TotalCardsWidth = MenuCardWidth + CardGap + RunCardWidth;

    // Five menu rows plus the frame.
    private const int CardHeight = 5 + Card.ChromeHeight;

    // Rows the anchored footer block occupies at the bottom of the screen: rule, tip, credit,
    // hints and the version corner mark, plus a row of clearance.
    private const int FooterRows = 7;

    // How far the whole upper block has to move up to clear the footer.
    //
    // The rows above are a design for a terminal with room for the banner, the cards and the
    // bottom-anchored footer. The footer keeps its anchor whatever the height, so on a short
    // terminal the fixed rows walk into it — at the documented 120x30 minimum the card block
    // landed exactly on the footer rule. Everything above the footer shifts up by however many
    // rows are missing, and nothing moves at all once there is room.
    private static int Squeeze(int interiorHeight) =>
        interiorHeight <= 0
            ? 0
            : Math.Max(0, CardsTop + CardHeight - 1 - (interiorHeight - FooterRows));

    // A design row, resolved against the host at layout time rather than at build time, so the
    // screen survives a terminal resize instead of only being right when it was created.
    private static Pos Row(int designRow, View host) =>
        Pos.Func(v => designRow - Squeeze(v?.Viewport.Height ?? 0), host);

    // Gradient endpoints for the banner, top to bottom: the SAO system-window cyan falling into
    // the interface accent.
    // PATH-D-PORT: a per-row colour ramp. A renderer swap reimplements the lerp, nothing else.
    private static readonly Color BannerTop    = new(0x4F, 0xC3, 0xF7);
    private static readonly Color BannerBottom = new(0xFF, 0xB4, 0x54);

    private const string SubTitle = "T U R N - B A S E D   R O G U E L I K E";

    private static readonly string Version = AppVersion.Display;

    private static readonly string[] Quotes =
    {
        "\"There is one thing I've learned here — to keep fighting.\"",
        "\"In this world, a single blade can take you anywhere.\"",
        "\"Levels are just numbers. Strength is just numbers.\"",
        "\"Real strength is not about how much you can lift.\"",
        "\"Even if I die, you keep living, okay?\"",
        "\"It's impossible to work hard for something you don't enjoy.\"",
        "\"Life isn't just doing things for yourself.\"",
        "\"Sometimes the thing that binds us is more powerful than the thing that divides us.\"",
    };

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        // Title is the hub every menu path returns to, so clear the menu Esc handlers here
        // as well — each re-enters the screen that installed it.
        NavigationHelper.UnhookScreenEscHandlers(mainWindow);
        var sw = DebugLogger.StartTimer("TitleScreen.Show");
        DebugLogger.LogScreen("TitleScreen");

        // Aincrad floats, so there is sky above and below it. The field spans the whole screen
        // and thins towards the middle, which is what turns the empty rows into composed space
        // rather than the void a four-row scatter at the top left behind. Static and sparse: it
        // never animates and never rises above one glyph in forty cells.
        var stars = new Label
        {
            Text = "", Id = "starfield", X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(), SchemeName = ColorSchemes.DimName,
        };
        stars.SubViewsLaidOut += (s, e) =>
        {
            if (stars.Viewport.Width > 0 && stars.Viewport.Height > 0 && stars.Text.Length == 0)
                stars.Text = StarField(stars.Viewport.Width, stars.Viewport.Height);
        };

        // Banner — one Label per row, all sharing one X so the letterforms line up, each tinted
        // a step along the vertical gradient.
        var bannerRows = new Label[BannerRows.Length];
        for (int i = 0; i < BannerRows.Length; i++)
        {
            float t = BannerRows.Length == 1 ? 0f : (float)i / (BannerRows.Length - 1);
            bannerRows[i] = new Label
            {
                Text = BannerRows[i],
                X = ScreenHeader.Axis - BannerWidth / 2, Y = Row(BannerY + i, mainWindow),
                Width = BannerWidth, Height = 1,
            }.WithScheme(ColorSchemes.FromColor(Lerp(BannerTop, BannerBottom, t)));
        }

        // Subtitle
        var subtitle = new Label
        {
            Text = SubTitle, X = Pos.Center(), Y = Row(SubTitleRow, mainWindow),
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.GoldName,
        };

        // Quote
        var quote = new Label
        {
            Text = Quotes[_tipRng.Next(Quotes.Length)],
            X = Pos.Center(), Y = Row(QuoteRow, mainWindow),
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DimName,
        };

        // ── Menu and last-run cards, side by side and centred ────────
        // The cards are frames; their content is added to the screen as siblings, because a
        // focusable container traps Tab traversal (see Card).
        Pos cardsLeft = ScreenHeader.Axis - TotalCardsWidth / 2;
        Pos runLeft = cardsLeft + MenuCardWidth + CardGap;

        Pos cardsTop = Row(CardsTop, mainWindow);
        var menuCard = new Card("MENU", cardsLeft, cardsTop, MenuCardWidth, CardHeight);
        var runCard = new Card("LAST RUN", runLeft, cardsTop, RunCardWidth, CardHeight);

        Pos menuY = Card.InsideY(cardsTop);
        Pos menuX = Card.InsideX(cardsLeft);
        var newGameBtn  = MenuBtn("New Game",  menuX, menuY,     true);
        var loadGameBtn = MenuBtn("Load Game", menuX, menuY + 1, false);
        var recordsBtn  = MenuBtn("Records",   menuX, menuY + 2, false);
        var optionsBtn  = MenuBtn("Options",   menuX, menuY + 3, false);
        var exitBtn     = MenuBtn("Exit",      menuX, menuY + 4, false);

        // Last-run card: turns a dead status line into the reason to press Load.
        var runLines = BuildRunCard(RunCardWidth - Card.ChromeWidth);
        var runLabels = new Label[runLines.Length];
        for (int i = 0; i < runLines.Length; i++)
        {
            runLabels[i] = new Label
            {
                Text = runLines[i],
                X = Card.InsideX(runLeft), Y = Card.InsideY(cardsTop) + i,
                Width = RunCardWidth - Card.ChromeWidth, Height = 1,
                SchemeName = i == 0 ? ColorSchemes.TitleName : ColorSchemes.DimName,
            };
        }

        // Button actions
        newGameBtn.Accepting += (s, e) => { DifficultyScreen.Show(mainWindow); e.Handled = true; };
        loadGameBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            if (!SaveManager.AnySaveExists()) { DialogHelper.Query("Load Game", "No save data found.", "OK"); return; }
            var result = Dialogs.SaveSlotDialog.ShowLoad();
            if (result.HasValue) GameScreen.ShowFromSave(mainWindow, result.Value.Data, result.Value.Slot);
        };
        recordsBtn.Accepting += (s, e) => { e.Handled = true; ShowRecords(); };
        optionsBtn.Accepting += (s, e) => { OptionsScreen.Show(mainWindow); e.Handled = true; };
        exitBtn.Accepting += (s, e) => { AppHost.App.RequestStop(); e.Handled = true; };

        // Footer
        var footerRule = new Label
        {
            Text = new string(ScreenHeader.Hairline, 60),
            X = Pos.Center(), Y = Pos.AnchorEnd(6),
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DimName
        };
        var tip = new Label
        {
            Text = $"Tip: {Tips[_tipRng.Next(Tips.Length)]}",
            X = Pos.Center(), Y = Pos.AnchorEnd(5),
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DimName
        };
        var tribute = new Label
        {
            Text = "Crafted by NoDice99 & Mixzpai — A Fan-Made Tribute to Sword Art Online",
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.BodyName
        };
        var hintPairs = new[] { ("↑↓", "navigate"), ("enter", "select"), ("esc", "quit") };
        var controls = ScreenHeader.KeyHints(
            ScreenHeader.Axis - ScreenHeader.KeyHintsWidth(hintPairs) / 2,
            Pos.AnchorEnd(3), hintPairs);
        var versionLabel = new Label
        {
            Text = Version,
            X = Pos.AnchorEnd(Version.Length + 1), Y = Pos.AnchorEnd(1),
            Width = Dim.Auto(), Height = 1, SchemeName = ColorSchemes.DimName
        };

        // Assemble
        mainWindow.Add(stars);
        mainWindow.Add(bannerRows);
        mainWindow.Add(subtitle, quote,
            menuCard, runCard,
            newGameBtn, loadGameBtn, recordsBtn, optionsBtn, exitBtn,
            footerRule, tip, tribute, versionLabel);
        mainWindow.Add(runLabels);
        mainWindow.Add(controls.ToArray());

        NavigationHelper.EnableGameNavigation(mainWindow);
        newGameBtn.SetFocus();
        DebugLogger.EndTimer("TitleScreen.Show", sw);
    }

    private static readonly string[] Tips =
    {
        "Press H during gameplay to view all keybindings.",
        "Campfires restore health. Look for the '&' symbol.",
        "Press F to manage your Sword Skills. F1-F4 to use them in combat.",
        "Press J to open your Quest Journal. Talk to NPCs for quests.",
        "Weapon proficiency grows with kills. Higher proficiency = new skills.",
        "Step on an Anvil to repair or enhance equipment (+1 to +10).",
        "Recruit party members by talking to Klein, Asuna, or Agil.",
        "Each floor has a unique biome with gameplay effects.",
        "Every floor has a unique named boss waiting in the Labyrinth.",
        "Press I for inventory, P for stats, T for equipment.",
        "Dexterity increases crit rate. Agility increases dodge chance.",
        "Death is permanent — your save is deleted. Retreat while you still can.",
        "Vendors sell better gear on higher floors. Check their stock.",
        "Floor 100 awaits the final challenge — your own reflection.",
    };

    private static readonly Random _tipRng = new();

    // Positions a menu row inside the menu card. The bar, tint, chrome and focus behaviour all
    // live in DialogHelper.CreateMenuRow; every row is the same width, so the labels share a
    // left edge instead of each being centred on its own length.
    private static Button MenuBtn(string text, Pos x, Pos y, bool isDefault)
    {
        var btn = DialogHelper.CreateMenuRow(text, MenuCardWidth - Card.ChromeWidth,
            isDefault: isDefault);
        btn.X = x;
        btn.Y = y;
        return btn;
    }

    // Linear interpolation between two colours, for the banner's vertical ramp.
    private static Color Lerp(Color a, Color b, float t) => new(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));

    // A deterministic star field. A hash of the coordinates picks the cells, so it is identical
    // every time the screen is built and never animates.
    //
    // Rows carrying text are left empty rather than relied on being painted over. Most content
    // does cover the sky behind it, but a row built from several small labels does not: the key
    // hints have a one-column gap between each key and its action, and a star landing in that gap
    // renders as "↑↓·navigate", which reads as a typo.
    private static string StarField(int width, int height)
    {
        var sb = new System.Text.StringBuilder(width * height + height);
        for (int y = 0; y < height; y++)
        {
            if (y > 0) sb.Append('\n');

            int squeeze = Squeeze(height);
            bool contentBand = y >= BannerY - squeeze - 1 && y <= CardsTop - squeeze + CardHeight;
            bool footerBand = y >= height - FooterRows;
            if (contentBand || footerBand)
            {
                sb.Append(' ', width);
                continue;
            }

            // 0 at the vertical centre of the content band, rising towards both edges.
            float edge = Math.Abs(y - height * 0.35f) / Math.Max(1f, height * 0.65f);
            int oneIn = (int)(90 - 60 * Math.Clamp(edge, 0f, 1f));
            for (int x = 0; x < width; x++)
            {
                int h = (x * 73856093) ^ (y * 19349663);
                h = (h ^ (h >> 13)) & 0x7FFFFFFF;
                sb.Append(h % oneIn == 0 ? (h % 3 == 0 ? '\u00b7' : '.') : ' ');
            }
        }
        return sb.ToString();
    }

    // Rows for the last-run card. Returns a "no save" line when nothing is stored, rather than
    // vanishing — an absent card reads as a missing feature.
    private static string[] BuildRunCard(int width)
    {
        SaveSlotSummary? best = LatestSave();
        if (best is null) return new[] { "No save data", "", "Start a new game to", "begin your climb." };

        string time = best.PlayTime.TotalHours >= 1
            ? $"{(int)best.PlayTime.TotalHours}h {best.PlayTime.Minutes:D2}m"
            : $"{best.PlayTime.Minutes}m";
        int bar = Math.Max(4, width - 10);
        int filled = Math.Clamp(best.Floor * bar / 100, 0, bar);
        return new[]
        {
            best.Name,
            $"Lv.{best.Level}  ·  {best.Difficulty}",
            $"{time} played",
            new string('\u2588', filled) + new string('\u2591', bar - filled) + $" {best.Floor}/100",
        };
    }

    // Newest save across all slots, or null when there is none.
    //
    // A bad slot must not take the title screen down, but swallowing the error silently would
    // hide a corrupt or unreadable save behind a missing card, which reads as "no save exists".
    private static SaveSlotSummary? LatestSave()
    {
        try
        {
            var summaries = SaveManager.GetSlotSummaries();
            SaveSlotSummary? best = null;
            foreach (var s in summaries)
                if (s != null && (best == null || s.Timestamp > best.Timestamp)) best = s;
            return best;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("TitleScreen.LatestSave", ex);
            return null;
        }
    }

    // Routes to RecordsDialog — 80x30 summary + achievements + recent runs + leaderboard.
    private static void ShowRecords() => Dialogs.RecordsDialog.Show();
}
