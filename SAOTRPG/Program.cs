using Terminal.Gui;
using SAOTRPG.UI;

namespace SAOTRPG
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Application.Init();

            var blackScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black),
                Focus = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
                HotNormal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
                HotFocus = new Terminal.Gui.Attribute(Color.White, Color.Black),
                Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
            };

            var mainWindow = new Window
            {
                Title = "Aincrad TRPG",
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = blackScheme
            };

            TitleScreen.Show(mainWindow);

            Application.Run(mainWindow);
            mainWindow.Dispose();
            Application.Shutdown();
        }
    }
}
