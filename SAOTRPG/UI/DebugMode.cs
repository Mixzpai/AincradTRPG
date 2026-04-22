namespace SAOTRPG.UI;

// Dev-only mode via --debug flag. When on: Debug difficulty tier is appended to DifficultyData.GetTiers,
// and DebugLogger starts writing debug.log.
public static class DebugMode
{
    // True when the game was launched with --debug.
    public static bool IsEnabled { get; private set; }

    // Activate debug mode. Called once during startup when --debug is detected.
    public static void Enable() => IsEnabled = true;
}
