using System.Runtime.InteropServices;
using Terminal.Gui;
using SAOTRPG.UI;

namespace SAOTRPG
{
    internal class Program
    {
        // Win32 imports for window resize — targets the foreground terminal window
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;  // Primary screen width
        private const int SM_CYSCREEN = 1;  // Primary screen height

        static void Main(string[] args)
        {
            // Resize terminal to 1920x1080, centered on screen
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    int screenW = GetSystemMetrics(SM_CXSCREEN);
                    int screenH = GetSystemMetrics(SM_CYSCREEN);
                    int x = Math.Max(0, (screenW - 1920) / 2);
                    int y = Math.Max(0, (screenH - 1080) / 2);
                    MoveWindow(hwnd, x, y, 1920, 1080, true);
                }
            }
            catch { /* Not supported on all terminals — silently continue */ }

            // Initialize debug logger — writes keystrokes + game output to debug.log
            DebugLogger.Init();

            // Catch any unhandled crash and dump it to debug.log before dying
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    DebugLogger.LogError("CRASH", ex);
                DebugLogger.Shutdown();
            };

            // Initialize Terminal.Gui with black color scheme
            Application.Init();

            // Attach keystroke logger to capture all input
            DebugLogger.AttachKeyLogger();

            // Main window fills entire terminal — all screens render inside this
            var mainWindow = new Window
            {
                Title = $" {Theme.Sparkle} Aincrad TRPG {Theme.Sparkle} ",
                X = 0, Y = 0,
                Width = Dim.Fill(), Height = Dim.Fill(),
                ColorScheme = Theme.WindowBase
            };

            if (args.Contains("--debug")) DebugMode.Enable();

            TitleScreen.Show(mainWindow);
            Application.Run(mainWindow);
            mainWindow.Dispose();
            Application.Shutdown();
            DebugLogger.Shutdown();
        }
    }
}
