using SAOTRPG.UI.Helpers;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.UI.Screens;

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
            // Debug logger first, so a settings or keybind load error is logged
            DebugLogger.Init();

            // Load user settings and key bindings (both persist across sessions)
            UserSettings.Load();
            SAOTRPG.Systems.Input.Keybinds.Load();

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
            if (args.Contains("--verify-light")) DebugMode.EnableVerifyLight();

            // --perf implies --quickstart: a perf run wants the map, and driving four screens by
            // hand is the slow part of taking a measurement. Order matters — the explicit opt-out
            // is applied last so `--perf --no-quickstart` reaches the title screen, which is the
            // only way to measure the title screen's own unexplained ~105ms frame.
            if (args.Contains("--perf") || args.Contains("--quickstart")) DebugMode.EnableQuickStart();
            if (args.Contains("--no-quickstart")) DebugMode.DisableQuickStart();

            int seedArg = Array.IndexOf(args, "--seed");
            if (seedArg >= 0 && seedArg + 1 < args.Length
                && int.TryParse(args[seedArg + 1], out int quickSeed))
                DebugMode.SetQuickStartSeed(quickSeed);

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
            AppHost.AttachKeyLogger();

            // Main window fills entire terminal — all screens render inside this
            var mainWindow = new GameWindow
            {
                Title = "Aincrad TRPG",
                X = 0, Y = 0,
                Width = Dim.Fill(), Height = Dim.Fill(),
                SchemeName = ColorSchemes.WindowName,
            };

            if (DebugMode.QuickStart) QuickStart.Launch(mainWindow, DebugMode.QuickStartSeed);
            else TitleScreen.Show(mainWindow);
            app.Run(mainWindow);
            mainWindow.Dispose();
            AppHost.Stop();
            DebugLogger.Shutdown();
        }
    }
}
