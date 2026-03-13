using Terminal.Gui;

namespace SAOTRPG.UI;

public static class OptionsScreen
{
    public static void Show(Window mainWindow)
    {
        mainWindow.RemoveAll();

        var header = new Label
        {
            Text = "=== Options ===",
            X = Pos.Center(),
            Y = 2,
            Width = Dim.Auto(),
            Height = 1
        };

        var placeholder = new Label
        {
            Text = "Nothing here yet... check back later!",
            X = Pos.Center(),
            Y = 5,
            Width = Dim.Auto(),
            Height = 1
        };

        var backBtn = new Button
        {
            Text = "  Back  ",
            X = Pos.Center(),
            Y = 8,
            IsDefault = true
        };

        backBtn.Accepting += (s, e) =>
        {
            TitleScreen.Show(mainWindow);
            e.Cancel = true;
        };

        mainWindow.Add(header, placeholder, backBtn);
        NavigationHelper.EnableGameNavigation(mainWindow);
        backBtn.SetFocus();
    }
}
