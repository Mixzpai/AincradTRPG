namespace SAOTRPG.UI;

// Developer-only mode activated via the --debug launch flag.
// When enabled, the Debug difficulty tier is appended to Systems.DifficultyData.GetTiers,
// and DebugLogger starts writing to debug.log.
public static class DebugMode
{
    // True when the game was launched with --debug.
    public static bool IsEnabled { get; private set; }

    // Activate debug mode. Called once during startup when --debug is detected.
    public static void Enable() => IsEnabled = true;
}
