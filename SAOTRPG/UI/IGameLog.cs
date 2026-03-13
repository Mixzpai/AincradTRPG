namespace SAOTRPG.UI;

public interface IGameLog
{
    void Log(string message);
    void LogCombat(string message);
    void LogSystem(string message);
}
