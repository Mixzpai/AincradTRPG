namespace SAOTRPG.UI;

/// <summary>
/// Contract for all log output — routes messages to whatever display backend is active.
///
/// Implementations:
///   - <see cref="GameLogView"/>    — live Terminal.Gui colored log panel (gameplay)
///   - <see cref="StringGameLog"/>  — StringBuilder sink (character creation preview)
///
/// Adding a new log category:
///   1. Add a method here (e.g. LogLoot)
///   2. Implement in both classes above
///   3. Optionally add a matching <see cref="LogCategory"/> entry for coloring
/// </summary>
public interface IGameLog
{
    /// <summary>General-purpose message (white text).</summary>
    void Log(string message);

    /// <summary>Combat event — damage, kills, XP gains (red-tinted).</summary>
    void LogCombat(string message);

    /// <summary>System event — level ups, equips, floor changes (cyan-tinted).</summary>
    void LogSystem(string message);
}
