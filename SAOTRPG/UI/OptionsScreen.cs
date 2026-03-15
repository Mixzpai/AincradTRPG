using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Placeholder options screen — will eventually hold audio, display,
/// and keybind settings. Currently shows a "coming soon" message.
/// </summary>
public static class OptionsScreen
{
    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();

        // ── Header ──────────────────────────────────────────────────
        var header = new Label
        {
            Text = "=== Options ===",
            X = Pos.Center(), Y = 2,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Title
        };

        // ── Placeholder message ─────────────────────────────────────
        var placeholder = new Label
        {
            Text = "Nothing here yet... check back later!",
            X = Pos.Center(), Y = 5,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        // ── Back button ─────────────────────────────────────────────
        var backBtn = new Button
        {
            Text = "  Back  ",
            X = Pos.Center(), Y = 8,
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };

        backBtn.Accepting += (s, e) =>
        {
            TitleScreen.Show(mainWindow);
            e.Cancel = true;
        };

        // ── Assemble ────────────────────────────────────────────────
        mainWindow.Add(header, placeholder, backBtn);
        NavigationHelper.EnableGameNavigation(mainWindow);
        backBtn.SetFocus();
    }
}
