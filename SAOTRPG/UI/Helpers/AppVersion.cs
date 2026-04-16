namespace SAOTRPG.UI.Helpers;

// Centralized app version string — avoids duplicating the reflection call
// across TitleScreen, OptionsScreen, and any future version displays.
public static class AppVersion
{
    // Display-ready version string, e.g. "v0.1.0".
    // Falls back to "v0.1.0" if the assembly version is unavailable.
    public static readonly string Display =
        $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0"}";
}
