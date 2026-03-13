using Terminal.Gui;

namespace SAOTRPG.UI;

public static class TitleScreen
{
    // ── Color Palettes ────────────────────────────────────────────────

    private static readonly ColorScheme TitleScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.White, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.White, Color.Black),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    private static readonly ColorScheme GoldScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    private static readonly ColorScheme DimScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    private static readonly ColorScheme SubtitleScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    private static readonly ColorScheme MenuFrameScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    private static readonly ColorScheme MenuButtonScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    // ── ASCII Art (no leading newlines — every line counts) ────────────

    //                          80 chars wide (widest line)
    // Stars: scattered across the top for atmosphere
    private const string StarsArt =
        "        .            *            .                   *          .          *     \n" +
        "  *              .          .              *       .           .                  \n" +
        "       .    *        .            .    *              .           *      .        \n" +
        "  .              .          *                 .              .                    ";

    //               AINCRAD block letters — 56 chars wide
    private const string AincradTitle =
        " █████╗ ██╗███╗   ██╗ ██████╗██████╗  █████╗ ██████╗ \n" +
        "██╔══██╗██║████╗  ██║██╔════╝██╔══██╗██╔══██╗██╔══██╗\n" +
        "███████║██║██╔██╗ ██║██║     ██████╔╝███████║██║  ██║\n" +
        "██╔══██║██║██║╚██╗██║██║     ██╔══██╗██╔══██║██║  ██║\n" +
        "██║  ██║██║██║ ╚████║╚██████╗██║  ██║██║  ██║██████╔╝\n" +
        "╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝";

    //               TRPG block letters — 52 chars wide (padded to 56)
    private const string TrpgTitle =
        "  ████████╗██████╗ ██████╗  ██████╗   \n" +
        "  ╚══██╔══╝██╔══██╗██╔══██╗██╔════╝   \n" +
        "     ██║   ██████╔╝██████╔╝██║  ███╗  \n" +
        "     ██║   ██╔══██╗██╔═══╝ ██║   ██║  \n" +
        "     ██║   ██║  ██║██║     ╚██████╔╝  \n" +
        "     ╚═╝   ╚═╝  ╚═╝╚═╝      ╚═════╝  ";

    // ── Decorative dividers (61 chars wide) ───────────────────────────
    private const string DividerTop    = "══════════════════════════╦═══════╦══════════════════════════";
    private const string DividerBottom = "══════════════════════════╩═══════╩══════════════════════════";

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("TitleScreen.Show");
        DebugLogger.LogScreen("TitleScreen");

        int row = 1;

        // ── Ambient stars ─────────────────────────────────────────────
        var stars = new Label
        {
            Text = StarsArt,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = DimScheme
        };
        row += 6;

        // ── Gold divider above title ──────────────────────────────────
        var divTop = new Label
        {
            Text = DividerTop,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = GoldScheme
        };
        row += 1;

        // ── AINCRAD block title ───────────────────────────────────────
        var titleAincrad = new Label
        {
            Text = AincradTitle,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = TitleScheme
        };
        row += 6;

        // ── TRPG block title ──────────────────────────────────────────
        var titleTrpg = new Label
        {
            Text = TrpgTitle,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = GoldScheme
        };
        row += 6;

        // ── Gold divider below title ──────────────────────────────────
        var divBot = new Label
        {
            Text = DividerBottom,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = GoldScheme
        };
        row += 2;

        // ── Tagline ──────────────────────────────────────────────────
        var tagline = new Label
        {
            Text = "\"Link Start — Your Story Awaits\"",
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = SubtitleScheme
        };
        row += 2;

        // ── Menu (proper FrameView so borders align with contents) ────
        const int menuWidth = 40;
        const int menuHeight = 11;

        var menuFrame = new FrameView
        {
            Title = "",
            X = Pos.Center(), Y = row,
            Width = menuWidth, Height = menuHeight,
            ColorScheme = MenuFrameScheme,
            BorderStyle = LineStyle.Double
        };

        var newGameBtn = new Button
        {
            Text = "    New Game     ",
            X = Pos.Center(), Y = 0,
            IsDefault = true,
            ColorScheme = MenuButtonScheme
        };
        var loadGameBtn = new Button
        {
            Text = "    Load Game    ",
            X = Pos.Center(), Y = 2,
            ColorScheme = MenuButtonScheme
        };
        var optionsBtn = new Button
        {
            Text = "     Options     ",
            X = Pos.Center(), Y = 4,
            ColorScheme = MenuButtonScheme
        };
        var exitBtn = new Button
        {
            Text = "      Exit       ",
            X = Pos.Center(), Y = 6,
            ColorScheme = MenuButtonScheme
        };

        // Separators inside the menu frame
        var sep1 = new Label
        {
            Text = "──────────────────────────────────────",
            X = Pos.Center(), Y = 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = MenuFrameScheme
        };
        var sep2 = new Label
        {
            Text = "──────────────────────────────────────",
            X = Pos.Center(), Y = 3,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = MenuFrameScheme
        };
        var sep3 = new Label
        {
            Text = "──────────────────────────────────────",
            X = Pos.Center(), Y = 5,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = MenuFrameScheme
        };

        menuFrame.Add(newGameBtn, sep1, loadGameBtn, sep2, optionsBtn, sep3, exitBtn);

        // ── Diamond glyphs follow focus ──────────────────────────────
        var menuButtons = new[] { newGameBtn, loadGameBtn, optionsBtn, exitBtn };
        foreach (var btn in menuButtons)
        {
            btn.HasFocusChanged += (s, e) =>
            {
                if (s is Button b)
                    b.IsDefault = e.NewValue;
            };
        }

        // ── Button event handlers ─────────────────────────────────────
        newGameBtn.Accepting += (s, e) => { DifficultyScreen.Show(mainWindow); e.Cancel = true; };
        loadGameBtn.Accepting += (s, e) => { MessageBox.Query("Load Game", "No save data found.", "OK"); e.Cancel = true; };
        optionsBtn.Accepting += (s, e) => { OptionsScreen.Show(mainWindow); e.Cancel = true; };
        exitBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // ── Footer (anchored to bottom) ───────────────────────────────
        var footerRule = new Label
        {
            Text = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            X = Pos.Center(), Y = Pos.AnchorEnd(5),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = DimScheme
        };
        var credits = new Label
        {
            Text = "Crafted by NoDice99 & Mixzpai",
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = SubtitleScheme
        };
        var tribute = new Label
        {
            Text = "A Fan-Made Tribute to Sword Art Online",
            X = Pos.Center(), Y = Pos.AnchorEnd(3),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = DimScheme
        };
        var controls = new Label
        {
            Text = "[W/S or Arrow Keys] Navigate    [Enter] Select",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = DimScheme
        };

        // ── Assemble ─────────────────────────────────────────────────
        mainWindow.Add(
            stars,
            divTop, titleAincrad, titleTrpg, divBot,
            tagline, menuFrame,
            footerRule, credits, tribute, controls
        );

        NavigationHelper.EnableGameNavigation(mainWindow);
        newGameBtn.SetFocus();
        DebugLogger.EndTimer("TitleScreen.Show", sw);
    }
}
