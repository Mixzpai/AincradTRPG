using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Main title screen -- Aincrad floating castle with menu.
public static class TitleScreen
{
    private const string StarsArt =
        "        .            *            .                   *          .          *     \n" +
        "  *              .          .              *       .           .                  \n" +
        "       .    *        .            .    *              .           *      .        \n" +
        "  .              .          *                 .              .                    ";

    private const string TitleText =
        " █████╗ ██╗███╗   ██╗ ██████╗██████╗  █████╗ ██████╗ \n" +
        "██╔══██╗██║████╗  ██║██╔════╝██╔══██╗██╔══██╗██╔══██╗\n" +
        "███████║██║██╔██╗ ██║██║     ██████╔╝███████║██║  ██║\n" +
        "██╔══██║██║██║╚██╗██║██║     ██╔══██╗██╔══██║██║  ██║\n" +
        "██║  ██║██║██║ ╚████║╚██████╗██║  ██║██║  ██║██████╔╝\n" +
        "╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝";

    private const string SubTitle = "T U R N - B A S E D   R O G U E L I K E";

    private static readonly string Version = AppVersion.Display;

    private static readonly string[] Quotes =
    {
        "\"There is one thing I've learned here -- to keep fighting.\"",
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
        var sw = DebugLogger.StartTimer("TitleScreen.Show");
        DebugLogger.LogScreen("TitleScreen");

        // Stars background
        var stars = new Label
        {
            Text = StarsArt, X = Pos.Center(), Y = 1,
            Width = Dim.Auto(), Height = Dim.Auto(), ColorScheme = ColorSchemes.Dim,
        };

        // Title
        var title = new Label
        {
            Text = TitleText, X = Pos.Center(), Y = 6,
            Width = Dim.Auto(), Height = Dim.Auto(), ColorScheme = ColorSchemes.Title,
        };

        // Subtitle
        var subtitle = new Label
        {
            Text = SubTitle, X = Pos.Center(), Y = 13,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Gold,
        };

        // Quote
        var quote = new Label
        {
            Text = Quotes[_tipRng.Next(Quotes.Length)],
            X = Pos.Center(), Y = 15,
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim,
        };

        // Menu buttons — bracket-free. The diamond markers `► … ►` toggle on
        // focus via HasFocusChanged below so the highlighted option is
        // spatially obvious without framing chrome around idle rows.
        int menuY = 18;
        var newGameBtn  = MakeBtn("New Game",  menuY,     true);
        var loadGameBtn = MakeBtn("Load Game", menuY + 2, false);
        var recordsBtn  = MakeBtn("Records",   menuY + 4, false);
        var optionsBtn  = MakeBtn("Options",   menuY + 6, false);
        var exitBtn     = MakeBtn("Exit",      menuY + 8, false);

        // Save preview
        string savePreview = BuildSavePreview();
        Label? saveLabel = null;
        if (savePreview.Length > 0)
        {
            saveLabel = new Label
            {
                Text = savePreview, X = Pos.Center(), Y = menuY + 10,
                Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim,
            };
        }

        // Focus highlight wiring — wrap focused row in `► Label ◄` (mirror
        // pair, triangles point INTO the word like framing brackets). Strip
        // markers on blur. Text is pre-padded with 2 spaces either side when
        // idle so horizontal alignment stays stable when focus moves. Also
        // keeps IsDefault synced with focus so Enter always activates the
        // currently-highlighted row.
        foreach (var btn in new[] { newGameBtn, loadGameBtn, recordsBtn, optionsBtn, exitBtn })
        {
            btn.HasFocusChanged += (s, e) =>
            {
                if (s is not Button b) return;
                b.IsDefault = e.NewValue;
                string core = StripMarkers(b.Text?.ToString() ?? "");
                b.Text = e.NewValue ? $"► {core} ◄" : $"  {core}  ";
            };
        }
        // Initialize the triangle markers on the default-focused row.
        newGameBtn.Text = $"► {StripMarkers(newGameBtn.Text?.ToString() ?? "")} ◄";
        foreach (var btn in new[] { loadGameBtn, recordsBtn, optionsBtn, exitBtn })
            btn.Text = $"  {StripMarkers(btn.Text?.ToString() ?? "")}  ";

        // Button actions
        newGameBtn.Accepting += (s, e) => { DifficultyScreen.Show(mainWindow); e.Cancel = true; };
        loadGameBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (!SaveManager.AnySaveExists()) { MessageBox.Query("Load Game", "No save data found.", "OK"); return; }
            var result = Dialogs.SaveSlotDialog.ShowLoad();
            if (result.HasValue) GameScreen.ShowFromSave(mainWindow, result.Value.Data, result.Value.Slot);
        };
        recordsBtn.Accepting += (s, e) => { e.Cancel = true; ShowRecords(); };
        optionsBtn.Accepting += (s, e) => { OptionsScreen.Show(mainWindow); e.Cancel = true; };
        exitBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        // Footer
        var footerRule = new Label
        {
            Text = "------------------------------------------------------------",
            X = Pos.Center(), Y = Pos.AnchorEnd(6),
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
        };
        var tip = new Label
        {
            Text = $"Tip: {Tips[_tipRng.Next(Tips.Length)]}",
            X = Pos.Center(), Y = Pos.AnchorEnd(5),
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
        };
        var tribute = new Label
        {
            Text = "Crafted by NoDice99 & Mixzpai -- A Fan-Made Tribute to Sword Art Online",
            X = Pos.Center(), Y = Pos.AnchorEnd(4),
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Body
        };
        var controls = new Label
        {
            Text = "[W/S] Navigate   [Enter] Select   [Esc] Quit",
            X = Pos.Center(), Y = Pos.AnchorEnd(3),
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
        };
        var versionLabel = new Label
        {
            Text = Version,
            X = Pos.AnchorEnd(Version.Length + 1), Y = Pos.AnchorEnd(1),
            Width = Dim.Auto(), Height = 1, ColorScheme = ColorSchemes.Dim
        };

        // Assemble
        mainWindow.Add(stars, title, subtitle, quote,
            newGameBtn, loadGameBtn, recordsBtn, optionsBtn, exitBtn,
            footerRule, tip, tribute, controls, versionLabel);
        if (saveLabel != null) mainWindow.Add(saveLabel);

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
        "Death costs 25% of your Col and 10% of your XP. Prepare well.",
        "Vendors sell better gear on higher floors. Check their stock.",
        "Floor 100 awaits the final challenge -- your own reflection.",
    };

    private static readonly Random _tipRng = new();

    private static Button MakeBtn(string text, int y, bool isDefault) => new()
    {
        Text = text, X = Pos.Center(), Y = y,
        IsDefault = isDefault, ColorScheme = ColorSchemes.MenuButton,
        // Strip Terminal.Gui's own [ ] button chrome so the only decoration
        // is the `► … ►` focus marker we manage via HasFocusChanged.
        NoDecorations = true, NoPadding = true,
    };

    // Remove existing markers (triangles + surrounding spaces) before
    // re-applying a new state. Idempotent — safe to call on any button
    // text regardless of current focus state. Handles both the mirror
    // pair (► … ◄) and any legacy single-direction pair from prior
    // iterations so swapping marker styles doesn't leave stale glyphs.
    private static string StripMarkers(string text)
    {
        var s = text.Trim();
        if (s.StartsWith("► ")) s = s[2..];
        if (s.EndsWith(" ◄")) s = s[..^2];
        if (s.EndsWith(" ►")) s = s[..^2];  // legacy guard
        return s.Trim();
    }

    private static string BuildSavePreview()
    {
        try
        {
            var summaries = SaveManager.GetSlotSummaries();
            SaveSlotSummary? best = null;
            foreach (var s in summaries)
                if (s != null && (best == null || s.Timestamp > best.Timestamp)) best = s;
            if (best == null) return "";

            string timeStr = best.PlayTime.TotalHours >= 1
                ? $"{(int)best.PlayTime.TotalHours}h {best.PlayTime.Minutes:D2}m"
                : $"{best.PlayTime.Minutes}m";
            return $"Last save: {best.Name} Lv.{best.Level}  Floor {best.Floor}  {timeStr}";
        }
        catch { return ""; }
    }

    // Routes to RecordsDialog: structured 80x30 dialog showing summary,
    // achievement panels, progress bars, recent runs (10), and a
    // tab-switchable victory leaderboard sortable by Col / Turns / Level /
    // Kills / PlayTime / Date / Grade.
    private static void ShowRecords() => Dialogs.RecordsDialog.Show();
}
