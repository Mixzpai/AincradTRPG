namespace SAOTRPG.UI;

// Lightweight log that writes to a StringBuilder — used for character creation preview
internal class StringGameLog : IGameLog
{
    private readonly System.Text.StringBuilder _sb;
    public StringGameLog(System.Text.StringBuilder sb) => _sb = sb;
    public void Log(string message) => _sb.AppendLine(message);
    public void LogCombat(string message) => _sb.AppendLine(message);
    public void LogSystem(string message) => _sb.AppendLine(message);
}
