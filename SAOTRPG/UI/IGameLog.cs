namespace SAOTRPG.UI;

// Contract for all log output — routes messages to whatever display is active
// Implementations: GameLogView (Terminal.Gui TextView), StringGameLog (StringBuilder for previews)
public interface IGameLog
{
    void Log(string message);        // General messages
    void LogCombat(string message);  // Combat-specific (damage, kills, XP)
    void LogSystem(string message);  // System events (level ups, equips)
}
