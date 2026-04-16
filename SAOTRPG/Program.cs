using System.Runtime.InteropServices;
using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG
{
    // Application entry point — initializes the terminal window, sets up
    // Terminal.Gui, attaches crash logging, and launches the title screen.
    internal class Program
    {
        // ── Win32 imports for window resize — targets the foreground terminal window ──
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        // GetSystemMetrics index for primary screen width in pixels.
        private const int SM_CXSCREEN = 0;
        // GetSystemMetrics index for primary screen height in pixels.
        private const int SM_CYSCREEN = 1;

        // Target window width in pixels on launch.
        private const int WindowWidth  = 1920;
        // Target window height in pixels on launch.
        private const int WindowHeight = 1080;

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
                    int x = Math.Max(0, (screenW - WindowWidth) / 2);
                    int y = Math.Max(0, (screenH - WindowHeight) / 2);
                    MoveWindow(hwnd, x, y, WindowWidth, WindowHeight, true);
                }
            }
            catch { /* Not supported on all terminals — silently continue */ }

            // Load user settings (persists across sessions)
            UserSettings.Load();

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

            var blackScheme = new ColorScheme
            {
                Normal = Gfx.Attr(Color.DarkGray, Color.Black),
                Focus = Gfx.Attr(Color.Gray, Color.Black),
                HotNormal = Gfx.Attr(Color.Gray, Color.Black),
                HotFocus = Gfx.Attr(Color.White, Color.Black),
                Disabled = Gfx.Attr(Color.DarkGray, Color.Black)
            };

            // Main window fills entire terminal — all screens render inside this
            var mainWindow = new Window
            {
                Title = "Aincrad TRPG",
                X = 0, Y = 0,
                Width = Dim.Fill(), Height = Dim.Fill(),
                ColorScheme = blackScheme
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
