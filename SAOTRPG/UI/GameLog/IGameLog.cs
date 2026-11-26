namespace SAOTRPG.UI.GameLog;

// Log output contract. Backends: GameLogView (live colored panel), StringGameLog (char-creation preview).
// New category: add method here, implement in both; LogTones decides each line's colour.
public interface IGameLog
{
    // General-purpose message (white text).
    void Log(string message);

    // Combat event — damage, kills, XP gains (red-tinted).
    void LogCombat(string message);

    // System event — level ups, equips, floor changes (cyan-tinted).
    void LogSystem(string message);

    // Item / economy event — pickups, drops, purchases, sales (gold-tinted).
    void LogLoot(string message);

    // NPC dialogue line (white, reserved for the Dialog tab).
    void LogDialog(string message) => Log(message);

    // Current game turn — stamps entries so the live log can dim earlier turns.
    int CurrentTurn { get; set; }

    // Drop all buffered entries — used by F9 hot-reload to purge stale lines.
    void Clear() { }
}
