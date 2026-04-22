namespace SAOTRPG.UI;

// Live game log → ColoredLogView with per-line category/keyword coloring. Prefixes: General (none), Combat [!], System [*], Item [$], Dialog (none).
// Also forwards to DebugLogger for file-based debug output.
public class GameLogView : IGameLog
{
    // ── Prefix tags ─────────────────────────────────────────────────
    private const string CombatPrefix = "[!] ";
    private const string SystemPrefix = "[*] ";
    private const string ItemPrefix   = "[$] ";

    private readonly ColoredLogView _logView;

    // Current game turn — set by TurnManager each tick for combat log prefixing.
    public int CurrentTurn { get; set; }

    // Create a GameLogView backed by the given colored log widget.
    public GameLogView(ColoredLogView logView) => _logView = logView;

    // Log a general (untagged) message to the game log.
    public void Log(string message)
    {
        DebugLogger.LogGame("LOG", message);
        _logView.AddEntry(message, LogCategory.General);
    }

    // Log a combat message prefixed with [!] and turn marker [T{N}].
    public void LogCombat(string message)
    {
        string turnTag = CurrentTurn > 0 ? $"[T{CurrentTurn}] " : "";
        DebugLogger.LogGame("COMBAT", message);
        _logView.AddEntry($"{CombatPrefix}{turnTag}{message}", LogCategory.Combat);
    }

    // Log a system message prefixed with [*].
    public void LogSystem(string message)
    {
        DebugLogger.LogGame("SYSTEM", message);
        _logView.AddEntry($"{SystemPrefix}{message}", LogCategory.System);
    }

    // Log an item / economy message prefixed with [$].
    public void LogLoot(string message)
    {
        DebugLogger.LogGame("ITEM", message);
        _logView.AddEntry($"{ItemPrefix}{message}", LogCategory.Item);
    }

    // Log NPC dialogue — category=Dialog so it filters into the Dialog tab.
    public void LogDialog(string message)
    {
        DebugLogger.LogGame("DIALOG", message);
        _logView.AddEntry(message, LogCategory.Dialog);
    }
}
