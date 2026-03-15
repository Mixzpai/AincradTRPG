namespace SAOTRPG.UI;

/// <summary>
/// Live game log — pipes all messages into a <see cref="ColoredLogView"/>
/// with per-line coloring based on category and keyword rules.
///
/// Each category gets a prefix tag for easy scanning:
///   General → (no prefix)
///   Combat  → [!]
///   System  → [*]
///
/// Also forwards to <see cref="DebugLogger"/> for file-based debug output.
/// </summary>
public class GameLogView : IGameLog
{
    // ── Prefix tags ─────────────────────────────────────────────────
    private const string CombatPrefix = "[!] ";
    private const string SystemPrefix = "[*] ";

    private readonly ColoredLogView _logView;

    public GameLogView(ColoredLogView logView) => _logView = logView;

    public void Log(string message)
    {
        DebugLogger.LogGame("LOG", message);
        _logView.AddEntry(message, LogCategory.General);
    }

    public void LogCombat(string message)
    {
        DebugLogger.LogGame("COMBAT", message);
        _logView.AddEntry($"{CombatPrefix}{message}", LogCategory.Combat);
    }

    public void LogSystem(string message)
    {
        DebugLogger.LogGame("SYSTEM", message);
        _logView.AddEntry($"{SystemPrefix}{message}", LogCategory.System);
    }
}
