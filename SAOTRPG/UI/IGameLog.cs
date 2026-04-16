namespace SAOTRPG.UI;

// Contract for all log output — routes messages to whatever display backend is active.
// Implementations:
//   - GameLogView    — live Terminal.Gui colored log panel (gameplay)
//   - StringGameLog  — StringBuilder sink (character creation preview)
// Adding a new log category:
//   1. Add a method here (e.g. LogLoot)
//   2. Implement in both classes above
//   3. Optionally add a matching LogCategory entry for coloring
public interface IGameLog
{
    // General-purpose message (white text).
    void Log(string message);

    // Combat event — damage, kills, XP gains (red-tinted).
    void LogCombat(string message);

    // System event — level ups, equips, floor changes (cyan-tinted).
    void LogSystem(string message);

    // Loot/economy event — pickups, drops, purchases, sales (yellow-tinted).
    void LogLoot(string message);

    // Current game turn — used by LogCombat for [T{N}] prefix.
    int CurrentTurn { get; set; }
}
