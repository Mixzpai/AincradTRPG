namespace SAOTRPG.UI;

// Log output contract. Backends: GameLogView (live colored panel), StringGameLog (char-creation preview).
// New category: add method here, implement in both, optionally add a LogCategory for coloring.
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

    // Current game turn — used by LogCombat for [T{N}] prefix.
    int CurrentTurn { get; set; }
}
