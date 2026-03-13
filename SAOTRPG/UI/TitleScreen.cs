using Terminal.Gui;

namespace SAOTRPG.UI;

public static class TitleScreen
{
    private const string AsciiTitle = @"
   ___    _                          __
  /   |  (_)___  ______________ ____/ /
 / /| | / / __ \/ ___/ ___/ __ `/ __  /
/ ___ |/ / / / / /__/ /  / /_/ / /_/ /
/_/  |_/_/_/ /_/\___/_/   \__,_/\__,_/

  ___________  ____  ______
 /_  __/ __ \/ __ \/ ____/
  / / / /_/ / /_/ / / __
 / / / _, _/ ____/ /_/ /
/_/ /_/ |_/_/    \____/
";

    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();

        var titleLabel = new Label
        {
            Text = AsciiTitle,
            X = Pos.Center(),
            Y = 1,
            Width = Dim.Auto(),
            Height = Dim.Auto()
        };

        var divider = new Label
        {
            Text = "─────────────────────────────────────────",
            X = Pos.Center(),
            Y = Pos.Bottom(titleLabel),
            Width = Dim.Auto(),
            Height = 1
        };

        var newGameBtn = new Button
        {
            Text = "  New Game  ",
            X = Pos.Center(),
            Y = Pos.Bottom(divider) + 2,
            IsDefault = true
        };

        var loadGameBtn = new Button
        {
            Text = " Load Game  ",
            X = Pos.Center(),
            Y = Pos.Bottom(newGameBtn) + 1
        };

        var optionsBtn = new Button
        {
            Text = "  Options   ",
            X = Pos.Center(),
            Y = Pos.Bottom(loadGameBtn) + 1
        };

        var exitBtn = new Button
        {
            Text = "    Exit    ",
            X = Pos.Center(),
            Y = Pos.Bottom(optionsBtn) + 1
        };

        newGameBtn.Accepting += (s, e) =>
        {
            DifficultyScreen.Show(mainWindow);
            e.Cancel = true;
        };

        loadGameBtn.Accepting += (s, e) =>
        {
            MessageBox.Query("Load Game", "No save data found.", "OK");
            e.Cancel = true;
        };

        optionsBtn.Accepting += (s, e) =>
        {
            OptionsScreen.Show(mainWindow);
            e.Cancel = true;
        };

        exitBtn.Accepting += (s, e) =>
        {
            Application.RequestStop();
            e.Cancel = true;
        };

        var creditsDivider = new Label
        {
            Text = "· · · · · · · · · · · · · · · · · · · ·",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(4),
            Width = Dim.Auto(),
            Height = 1
        };

        var creditsLabel = new Label
        {
            Text = "~ crafted by NoDice99 & Mixzpai ~",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(3),
            Width = Dim.Auto(),
            Height = 1
        };

        var creditsTagline = new Label
        {
            Text = "« a fan-made tribute to Sword Art Online »",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(2),
            Width = Dim.Auto(),
            Height = 1
        };

        mainWindow.Add(titleLabel, divider, newGameBtn, loadGameBtn, optionsBtn, exitBtn,
            creditsDivider, creditsLabel, creditsTagline);
        NavigationHelper.EnableGameNavigation(mainWindow);
        newGameBtn.SetFocus();
    }
}
