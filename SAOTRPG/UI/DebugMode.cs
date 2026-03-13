namespace SAOTRPG.UI;

/// <summary>
/// Developer-only mode activated via --debug launch flag.
/// </summary>
public static class DebugMode
{
    public static bool IsEnabled { get; private set; }
    public static void Enable() => IsEnabled = true;
}
