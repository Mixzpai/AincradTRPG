using System.Text;

namespace SAOTRPG.UI;

// StringBuilder log sink — used during character creation (no live Terminal.Gui panel yet).
// All categories write plain text; no coloring or prefixes.
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
