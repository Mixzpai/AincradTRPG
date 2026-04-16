using System.Text;

namespace SAOTRPG.UI;

// Lightweight log sink that writes to a StringBuilder.
// Used during character creation where no Terminal.Gui log panel exists yet.
// All categories write plain text — no coloring or prefixes needed.
internal class StringGameLog : IGameLog
{
    private readonly StringBuilder _sb;

    public StringGameLog(StringBuilder sb) => _sb = sb;

    public int CurrentTurn { get; set; }

    public void Log(string message)       => _sb.AppendLine(message);
    public void LogCombat(string message)  => _sb.AppendLine(message);
    public void LogSystem(string message)  => _sb.AppendLine(message);
    public void LogLoot(string message)    => _sb.AppendLine(message);
}
