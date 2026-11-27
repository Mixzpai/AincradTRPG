using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.UI.GameLog;

// Live game log → ColoredLogView, which types each line by meaning and ages it by turn.
// Also forwards to DebugLogger for file-based debug output.
public class GameLogView : IGameLog
{
    private readonly ColoredLogView _logView;

    // Current game turn, set by TurnManager: each entry's stamp, and the log's "now" for ageing.
    public int CurrentTurn { get => _logView.Now; set => _logView.Now = value; }

    public GameLogView(ColoredLogView logView) => _logView = logView;

    public void Log(string message)
    {
        DebugLogger.LogGame("LOG", message);
        _logView.AddEntry(message, LogCategory.General, CurrentTurn);
    }

    public void LogCombat(string message)
    {
        DebugLogger.LogGame("COMBAT", message);
        _logView.AddEntry(message, LogCategory.Combat, CurrentTurn);
    }

    public void LogSystem(string message)
    {
        DebugLogger.LogGame("SYSTEM", message);
        _logView.AddEntry(message, LogCategory.System, CurrentTurn);
    }

    public void LogLoot(string message)
    {
        DebugLogger.LogGame("ITEM", message);
        _logView.AddEntry(message, LogCategory.Item, CurrentTurn);
    }

    // NPC dialogue: typed info.
    public void LogDialog(string message)
    {
        DebugLogger.LogGame("DIALOG", message);
        _logView.AddEntry(message, LogCategory.Dialog, CurrentTurn);
    }

    // F9 hot-reload purge.
    public void Clear() => _logView.Clear();
}
