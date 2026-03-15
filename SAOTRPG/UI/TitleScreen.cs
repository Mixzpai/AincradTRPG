using Terminal.Gui;

namespace SAOTRPG.UI;

public static class TitleScreen
{
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
            ColorScheme = Theme.Dim
        };
        row += 6;

        // ── Gold divider above title ──────────────────────────────────
        var divTop = new Label
        {
            Text = DividerTop,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };
        row += 1;

        // ── AINCRAD block title ───────────────────────────────────────
        var titleAincrad = new Label
        {
            Text = AincradTitle,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = Theme.Title
        };
        row += 6;

        // ── TRPG block title ──────────────────────────────────────────
        var titleTrpg = new Label
        {
            Text = TrpgTitle,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = Theme.Gold
        };
        row += 6;

        // ── Gold divider below title ──────────────────────────────────
        var divBot = new Label
        {
            Text = DividerBottom,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };
        row += 2;

        // ── Tagline ──────────────────────────────────────────────────
        var tagline = new Label
        {
            Text = $"{Theme.SparkleOpen} Link Start — Your Story Awaits {Theme.SparkleOpen}",
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Subtitle
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
            ColorScheme = Theme.FrameSubtle,
            BorderStyle = LineStyle.Double
        };

        string pad = "      ";
        var newGameBtn = new Button
        {
            Text = $"{pad}New Game{pad} ",
            X = Pos.Center(), Y = 0,
            IsDefault = true,
            ColorScheme = Theme.MenuButton
        };
        var loadGameBtn = new Button
        {
            Text = $"{pad}Load Game{pad}",
            X = Pos.Center(), Y = 2,
            ColorScheme = Theme.MenuButton
        };
        var optionsBtn = new Button
        {
            Text = $"{pad} Options {pad}",
            X = Pos.Center(), Y = 4,
            ColorScheme = Theme.MenuButton
        };
        var exitBtn = new Button
        {
            Text = $"{pad}  Exit  {pad} ",
            X = Pos.Center(), Y = 6,
            ColorScheme = Theme.MenuButton
        };

        // Separators inside the menu frame
        string sepText = Theme.LightRule(38);
        var sep1 = new Label { Text = sepText, X = Pos.Center(), Y = 1, Width = Dim.Auto(), Height = 1, ColorScheme = Theme.FrameSubtle };
        var sep2 = new Label { Text = sepText, X = Pos.Center(), Y = 3, Width = Dim.Auto(), Height = 1, ColorScheme = Theme.FrameSubtle };
        var sep3 = new Label { Text = sepText, X = Pos.Center(), Y = 5, Width = Dim.Auto(), Height = 1, ColorScheme = Theme.FrameSubtle };

        menuFrame.Add(newGameBtn, sep1, loadGameBtn, sep2, optionsBtn, sep3, exitBtn);

        // ── Diamond glyphs follow focus ──────────────────────────────
        Theme.AttachDiamondFocus(newGameBtn, loadGameBtn, optionsBtn, exitBtn);

        // ── Button event handlers ─────────────────────────────────────
        newGameBtn.Accepting += (s, e) => { DifficultyScreen.Show(mainWindow); e.Cancel = true; };
        loadGameBtn.Accepting += (s, e) => { MessageBox.Query("Load Game", "No save data found.", "OK"); e.Cancel = true; };
        optionsBtn.Accepting += (s, e) => { OptionsScreen.Show(mainWindow); e.Cancel = true; };
        exitBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // ── Footer (anchored to bottom) ───────────────────────────────
        var footerRule = new Label
        {
            Text = Theme.HeavyRule(60),
            X = Pos.Center(), Y = Pos.AnchorEnd(5),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };
        var credits = new Label
        {
            Text = $"{Theme.Sparkle} Crafted by NoDice99 & Mixzpai {Theme.Sparkle}",
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Subtitle
        };
        var tribute = new Label
        {
            Text = "A Fan-Made Tribute to Sword Art Online",
            X = Pos.Center(), Y = Pos.AnchorEnd(3),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };
        var controls = new Label
        {
            Text = $"[{Theme.ArrowUp}/{Theme.ArrowDown} or W/S] Navigate    [Enter] Select",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
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
