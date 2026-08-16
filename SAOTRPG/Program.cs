using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG
{
    // Application entry point — sets up Terminal.Gui, attaches crash logging,
    // and launches the title screen.
    //
    // The game takes the terminal at whatever size it is already. It does not move or resize
    // the host window.
    internal class Program
    {
        static void Main(string[] args)
        {
            // Load user settings and key bindings (both persist across sessions)
            UserSettings.Load();
            SAOTRPG.Systems.Input.Keybinds.Load();

            // Initialize debug logger — writes keystrokes + game output to debug.log
            DebugLogger.Init();

            // Catch any unhandled crash and dump it to debug.log before dying
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    DebugLogger.LogError("CRASH", ex);
                DebugLogger.Shutdown();
            };

            // Flags are parsed before any UI type is touched: the render instrumentation
            // latches DebugMode.PerfSampling at type-init, so it has to be final by then.
            if (args.Contains("--debug")) DebugMode.Enable();
            if (args.Contains("--freeze-anim")) DebugMode.EnableFreezeAnimations();
            if (args.Contains("--perf")) DebugMode.EnablePerfSampling();
            if (args.Contains("--verify-tiles")) DebugMode.EnableVerifyTiles();

            // Create and initialize the Terminal.Gui application instance.
            // --driver <windows|ansi|dotnet> overrides the backend; the platform
            // default on Windows is "ansi", which routes all console I/O through
            // escape sequences instead of the native Console API.
            int driverArg = Array.IndexOf(args, "--driver");
            string? driverName = driverArg >= 0 && driverArg + 1 < args.Length ? args[driverArg + 1] : null;
            var app = AppHost.Start(driverName);

            // Register the game's named palettes with SchemeManager so views can
            // bind via SchemeName, and replace Terminal.Gui's default marker glyphs.
            // Both must run before any UI construction.
            ColorSchemes.ApplyTheme(UserSettings.Current.ColorTheme);
            ColorSchemes.ApplyGlyphs();

            // Attach keystroke logger to capture all input
            DebugLogger.AttachKeyLogger();

            // Main window fills entire terminal — all screens render inside this
            var mainWindow = new GameWindow
            {
                Title = "Aincrad TRPG",
                X = 0, Y = 0,
                Width = Dim.Fill(), Height = Dim.Fill(),
                SchemeName = ColorSchemes.WindowName,
            };

            // The border is LOAD-BEARING for render performance — see Gfx.FillDirtyRows. It puts a
            // dirty cell at both ends of every row, so FillDirtyRows expands each row end to end and
            // the driver never walks a clean cell. Removing it measured 5-19x SLOWER.

            TitleScreen.Show(mainWindow);
            app.Run(mainWindow);
            mainWindow.Dispose();
            AppHost.Stop();
            DebugLogger.Shutdown();
        }
    }
}
