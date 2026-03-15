using Terminal.Gui;

namespace SAOTRPG.UI;

// Placeholder options screen — will hold audio, display, keybind settings
public static class OptionsScreen
{
    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();

        // ── Header ──────────────────────────────────────────────────────
        var header = new Label
        {
            Text = $"{Theme.DoubleRule(10)} Options {Theme.DoubleRule(10)}",
            X = Pos.Center(), Y = 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Gold
        };

        var subHeader = new Label
        {
            Text = $"  {Theme.SparkleOpen} Configure your experience",
            X = Pos.Center(), Y = 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        // ── Options frame ───────────────────────────────────────────────
        var optionsFrame = new FrameView
        {
            Title = $" {Theme.DiamondOpen} Settings ",
            X = Pos.Center(), Y = 4,
            Width = 44, Height = 12,
            ColorScheme = Theme.FrameSubtle,
            BorderStyle = LineStyle.Rounded
        };

        var placeholder = new Label
        {
            Text = $"  {Theme.SparkleOpen} Nothing here yet...",
            X = 1, Y = 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Subtitle
        };

        var comingSoon1 = new Label
        {
            Text = $"  {Theme.BulletOpen} Audio settings",
            X = 1, Y = 4,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };
        var comingSoon2 = new Label
        {
            Text = $"  {Theme.BulletOpen} Display settings",
            X = 1, Y = 5,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };
        var comingSoon3 = new Label
        {
            Text = $"  {Theme.BulletOpen} Keybind settings",
            X = 1, Y = 6,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        var comingTag = new Label
        {
            Text = $"  {Theme.DottedRule(16)} coming soon {Theme.DottedRule(16)}",
            X = 1, Y = 8,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        optionsFrame.Add(placeholder, comingSoon1, comingSoon2, comingSoon3, comingTag);

        // ── Back button ─────────────────────────────────────────────────
        var backBtn = new Button
        {
            Text = $" {Theme.ArrowLeft} Back ",
            X = Pos.Center(), Y = 17,
            IsDefault = true,
            ColorScheme = Theme.MenuButton
        };
        Theme.AttachDiamondFocus(backBtn);
        backBtn.Accepting += (s, e) => { TitleScreen.Show(mainWindow); e.Cancel = true; };

        // ── Controls hint ───────────────────────────────────────────────
        var controlsHint = new Label
        {
            Text = $"[Enter] Select    [{Theme.ArrowUp}/{Theme.ArrowDown} or W/S] Navigate",
            X = Pos.Center(), Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        mainWindow.Add(header, subHeader, optionsFrame, backBtn, controlsHint);
        NavigationHelper.EnableGameNavigation(mainWindow);
        backBtn.SetFocus();
    }
}
