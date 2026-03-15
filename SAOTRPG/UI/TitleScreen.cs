using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Main title screen — the first thing players see.
///
/// Layout (top to bottom):
///   - Ambient stars (atmosphere)
///   - Gold divider
///   - AINCRAD block letters (white)
///   - TRPG block letters (gold)
///   - Gold divider
///   - Tagline
///   - Menu frame (New Game / Load / Options / Exit)
///   - Random gameplay tip
///   - Footer (credits, controls)
/// </summary>
public static class TitleScreen
{
    // ══════════════════════════════════════════════════════════════════
    //  ASCII ART — Title graphics
    // ══════════════════════════════════════════════════════════════════

    // Ambient stars — scattered across the top for atmosphere (80 chars wide)
    private const string StarsArt =
        "        .            *            .                   *          .          *     \n" +
        "  *              .          .              *       .           .                  \n" +
        "       .    *        .            .    *              .           *      .        \n" +
        "  .              .          *                 .              .                    ";

    // AINCRAD block letters (56 chars wide)
    private const string AincradTitle =
        " █████╗ ██╗███╗   ██╗ ██████╗██████╗  █████╗ ██████╗ \n" +
        "██╔══██╗██║████╗  ██║██╔════╝██╔══██╗██╔══██╗██╔══██╗\n" +
        "███████║██║██╔██╗ ██║██║     ██████╔╝███████║██║  ██║\n" +
        "██╔══██║██║██║╚██╗██║██║     ██╔══██╗██╔══██║██║  ██║\n" +
        "██║  ██║██║██║ ╚████║╚██████╗██║  ██║██║  ██║██████╔╝\n" +
        "╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝";

    // TRPG block letters (padded to ~56 chars wide)
    private const string TrpgTitle =
        "  ████████╗██████╗ ██████╗  ██████╗   \n" +
        "  ╚══██╔══╝██╔══██╗██╔══██╗██╔════╝   \n" +
        "     ██║   ██████╔╝██████╔╝██║  ███╗  \n" +
        "     ██║   ██╔══██╗██╔═══╝ ██║   ██║  \n" +
        "     ██║   ██║  ██║██║     ╚██████╔╝  \n" +
        "     ╚═╝   ╚═╝  ╚═╝╚═╝      ╚═════╝  ";

    // ══════════════════════════════════════════════════════════════════
    //  DECORATIVE ELEMENTS
    // ══════════════════════════════════════════════════════════════════

    private const string DividerTop     = "══════════════════════════╦═══════╦══════════════════════════";
    private const string DividerBottom  = "══════════════════════════╩═══════╩══════════════════════════";
    private const string MenuSeparator  = "──────────────────────────────────────";
    private const string FooterRule     = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

    // ══════════════════════════════════════════════════════════════════
    //  LAYOUT CONSTANTS
    // ══════════════════════════════════════════════════════════════════

    private const int MenuWidth  = 40;
    private const int MenuHeight = 11;

    // Vertical row positions (tracked incrementally in Show)
    private const int StarsRow       = 1;
    private const int StarsHeight    = 6;    // 4 lines of art + 2 padding
    private const int TitleHeight    = 6;    // each block letter set is 6 lines

    // ══════════════════════════════════════════════════════════════════
    //  SHOW
    // ══════════════════════════════════════════════════════════════════

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("TitleScreen.Show");
        DebugLogger.LogScreen("TitleScreen");

        int row = StarsRow;

        // ── Ambient stars ────────────────────────────────────────────
        var stars = new Label
        {
            Text = StarsArt,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Dim
        };
        row += StarsHeight;

        // ── Gold divider above title ─────────────────────────────────
        var divTop = new Label
        {
            Text = DividerTop,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Gold
        };
        row += 1;

        // ── AINCRAD block letters ────────────────────────────────────
        var titleAincrad = new Label
        {
            Text = AincradTitle,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Title
        };
        row += TitleHeight;

        // ── TRPG block letters ───────────────────────────────────────
        var titleTrpg = new Label
        {
            Text = TrpgTitle,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = Dim.Auto(),
            ColorScheme = ColorSchemes.Gold
        };
        row += TitleHeight;

        // ── Gold divider below title ─────────────────────────────────
        var divBot = new Label
        {
            Text = DividerBottom,
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Gold
        };
        row += 2;

        // ── Tagline ──────────────────────────────────────────────────
        var tagline = new Label
        {
            Text = "\"Link Start — Your Story Awaits\"",
            X = Pos.Center(), Y = row,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Body
        };
        row += 2;

        // ══════════════════════════════════════════════════════════════
        //  MENU
        // ══════════════════════════════════════════════════════════════

        var menuFrame = new FrameView
        {
            Title = "",
            X = Pos.Center(), Y = row,
            Width = MenuWidth, Height = MenuHeight,
            ColorScheme = ColorSchemes.MenuFrame,
            BorderStyle = LineStyle.Double
        };

        // Menu buttons — evenly spaced with separators between
        var newGameBtn  = CreateMenuButton("    New Game     ", 0, isDefault: true);
        var loadGameBtn = CreateMenuButton(SaveManager.SaveExists() ? "   Continue Game  " : "    Load Game    ", 2);
        var optionsBtn  = CreateMenuButton("     Options     ", 4);
        var exitBtn     = CreateMenuButton("      Exit       ", 6);

        // Separators between buttons
        var sep1 = CreateMenuSeparator(1);
        var sep2 = CreateMenuSeparator(3);
        var sep3 = CreateMenuSeparator(5);

        menuFrame.Add(newGameBtn, sep1, loadGameBtn, sep2, optionsBtn, sep3, exitBtn);

        // Focus glow — highlight the active button with IsDefault styling
        foreach (var btn in new[] { newGameBtn, loadGameBtn, optionsBtn, exitBtn })
        {
            btn.HasFocusChanged += (s, e) =>
            {
                if (s is Button b) b.IsDefault = e.NewValue;
            };
        }

        // ── Button actions ───────────────────────────────────────────
        newGameBtn.Accepting  += (s, e) => { DifficultyScreen.Show(mainWindow); e.Cancel = true; };
        loadGameBtn.Accepting += (s, e) =>
        {
            if (SaveManager.SaveExists())
            {
                var save = SaveManager.LoadGame();
                if (save != null)
                    GameScreen.ShowFromSave(mainWindow, save);
                else
                    MessageBox.Query("Load Game", "Save file is corrupted.", "OK");
            }
            else
            {
                MessageBox.Query("Load Game", "No save data found.", "OK");
            }
            e.Cancel = true;
        };
        optionsBtn.Accepting  += (s, e) => { OptionsScreen.Show(mainWindow); e.Cancel = true; };
        exitBtn.Accepting     += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // ══════════════════════════════════════════════════════════════
        //  FOOTER (anchored to bottom edge)
        // ══════════════════════════════════════════════════════════════

        // ── Random gameplay tip ─────────────────────────────────────
        var tip = new Label
        {
            Text = $"Tip: {Tips[_tipRng.Next(Tips.Length)]}",
            X = Pos.Center(), Y = Pos.AnchorEnd(6),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Body
        };

        var footerRuleLabel = new Label
        {
            Text = FooterRule,
            X = Pos.Center(), Y = Pos.AnchorEnd(5),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };
        var credits = new Label
        {
            Text = "Crafted by NoDice99 & Mixzpai",
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Body
        };
        var tribute = new Label
        {
            Text = "A Fan-Made Tribute to Sword Art Online",
            X = Pos.Center(), Y = Pos.AnchorEnd(3),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };
        var controls = new Label
        {
            Text = "[W/S or Arrow Keys] Navigate    [Enter] Select",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        // ══════════════════════════════════════════════════════════════
        //  ASSEMBLE
        // ══════════════════════════════════════════════════════════════

        mainWindow.Add(
            stars,
            divTop, titleAincrad, titleTrpg, divBot,
            tagline, menuFrame,
            tip, footerRuleLabel, credits, tribute, controls
        );

        NavigationHelper.EnableGameNavigation(mainWindow);
        newGameBtn.SetFocus();
        DebugLogger.EndTimer("TitleScreen.Show", sw);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GAMEPLAY TIPS — shown randomly on the title screen
    // ══════════════════════════════════════════════════════════════════

    // Add new tips by adding a string to this array
    private static readonly string[] Tips =
    {
        "Press H during gameplay to view all keybindings.",
        "Campfires restore health — look for the '&' symbol.",
        "Watch out for traps! Spike traps deal damage, teleport traps move you.",
        "Press I to open your inventory and manage gear.",
        "Mobs have different aggro ranges — some spot you from far away.",
        "Press C to view your stats and allocate skill points.",
        "Items glow on the ground — walk over them to pick up loot.",
        "Your minimap shows explored areas, enemies, and items.",
        "XP gains diminish against enemies much weaker than you.",
        "Stronger enemies appear on higher floors. Prepare before ascending.",
        "The game log highlights important events with color-coded text.",
        "Use arrow keys or WASD to move around the dungeon.",
        "Dexterity increases your critical hit chance.",
        "Agility increases your dodge chance against enemy attacks.",
        "Vendors sell better gear on higher floors.",
        "You can sell unwanted items to vendors for Col.",
    };

    private static readonly Random _tipRng = new();

    // ══════════════════════════════════════════════════════════════════
    //  HELPERS — Menu element factories
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Creates a centered menu button at the given row inside the menu frame.</summary>
    private static Button CreateMenuButton(string text, int row, bool isDefault = false) => new()
    {
        Text = text,
        X = Pos.Center(), Y = row,
        IsDefault = isDefault,
        ColorScheme = ColorSchemes.MenuButton
    };

    /// <summary>Creates a horizontal separator line inside the menu frame.</summary>
    private static Label CreateMenuSeparator(int row) => new()
    {
        Text = MenuSeparator,
        X = Pos.Center(), Y = row,
        Width = Dim.Auto(), Height = 1,
        ColorScheme = ColorSchemes.MenuFrame
    };
}
