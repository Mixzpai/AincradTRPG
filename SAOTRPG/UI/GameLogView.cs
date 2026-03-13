using Terminal.Gui;

namespace SAOTRPG.UI;

public class GameLogView : IGameLog
{
    private readonly TextView _textView;

    public GameLogView(TextView textView)
    {
        _textView = textView;
    }

    public void Log(string message) => AppendLine(message);
    public void LogCombat(string message) => AppendLine(message);
    public void LogSystem(string message) => AppendLine(message);

    private void AppendLine(string text)
    {
        _textView.Text += text + "\n";
        _textView.MoveEnd();
    }
}
