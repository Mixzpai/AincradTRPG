using Terminal.Gui;

namespace SAOTRPG.UI;

// Live game log — pipes all messages into a Terminal.Gui TextView and auto-scrolls
public class GameLogView : IGameLog
{
    private readonly TextView _textView;

    public GameLogView(TextView textView) => _textView = textView;

    public void Log(string message) { DebugLogger.LogGame("LOG", message); AppendLine(message); }
    public void LogCombat(string message) { DebugLogger.LogGame("COMBAT", message); AppendLine(message); }
    public void LogSystem(string message) { DebugLogger.LogGame("SYSTEM", message); AppendLine(message); }

    // Append text and scroll to bottom so latest messages are always visible
    private void AppendLine(string text)
    {
        _textView.Text += text + "\n";
        _textView.MoveEnd();
    }
}
