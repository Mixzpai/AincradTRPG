using System.Text;

namespace SAOTRPG.UI;

/// <summary>
/// Lightweight log sink that writes to a <see cref="StringBuilder"/>.
/// Used during character creation where no Terminal.Gui log panel exists yet.
/// All categories write plain text — no coloring or prefixes needed.
/// </summary>
internal class StringGameLog : IGameLog
{
    private readonly StringBuilder _sb;

    public StringGameLog(StringBuilder sb) => _sb = sb;

    public void Log(string message)       => _sb.AppendLine(message);
    public void LogCombat(string message)  => _sb.AppendLine(message);
    public void LogSystem(string message)  => _sb.AppendLine(message);
}
