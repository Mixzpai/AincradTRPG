using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// FB-474 HUD quest tracker. 28-col × 2-line block anchored top-right below
// the minimap. Shows ONLY the pinned quest (QuestSystem.PinnedQuestId).
// Hides entirely when no pinned quest exists; shows "COMPLETE — return to X"
// in green once the quest state flips to Complete, until turn-in clears pin.
public class QuestTrackerWidget : View
{
    public const int TrackerWidth = 28;
    public const int TrackerHeight = 2;

    public QuestTrackerWidget()
    {
        Width = TrackerWidth;
        Height = TrackerHeight;
        CanFocus = false;
        ColorScheme = ColorSchemes.Body;
    }

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        if (vp.Width <= 0 || vp.Height <= 0) return true;

        var quest = QuestSystem.PinnedQuest();
        // Empty-state: blank both rows (widget effectively hides).
        if (quest == null)
        {
            BlankRow(0, vp.Width);
            if (vp.Height > 1) BlankRow(1, vp.Width);
            return true;
        }

        bool complete = quest.Status == QuestStatus.Complete;
        Color nameColor = complete ? Color.BrightGreen : Color.BrightYellow;
        string line1 = TextHelpers.Truncate(quest.Title, TrackerWidth);
        string line2 = complete
            ? TextHelpers.Truncate($"COMPLETE — return to {quest.GiverName}", TrackerWidth)
            : BuildProgressLine(quest);

        DrawRow(0, line1, nameColor, vp.Width);
        if (vp.Height > 1)
        {
            Color line2Color = complete ? Color.BrightGreen : Color.Gray;
            DrawRow(1, line2, line2Color, vp.Width);
        }
        return true;
    }

    // "3/5 Kobolds · F2" style. Explore shows "40% · F2"; Deliver shows status.
    private static string BuildProgressLine(Quest q)
    {
        string body = q.Type switch
        {
            QuestType.Kill => $"{q.CurrentCount}/{q.TargetCount} {Pluralize(q.TargetMob ?? "mob", q.TargetCount)}",
            QuestType.Collect => $"{q.CurrentCount}/{q.TargetCount} {q.TargetItem}",
            QuestType.Explore => $"{q.CurrentCount}/{q.TargetCount}% map",
            QuestType.Deliver => q.CurrentCount >= q.TargetCount ? "Ready to deliver" : "Find recipient",
            _ => $"{q.CurrentCount}/{q.TargetCount}",
        };
        return TextHelpers.Truncate($"{body} · F{q.Floor}", TrackerWidth);
    }

    private static string Pluralize(string noun, int count)
        => count == 1 || string.IsNullOrEmpty(noun) ? noun : noun + "s";

    private void DrawRow(int row, string text, Color fg, int width)
    {
        Driver!.SetAttribute(Gfx.Attr(fg, Color.Black));
        Move(0, row);
        int i = 0;
        for (; i < text.Length && i < width; i++)
            Driver!.AddRune(new System.Text.Rune(text[i]));
        for (; i < width; i++) Driver!.AddRune(new System.Text.Rune(' '));
    }

    private void BlankRow(int row, int width)
    {
        Driver!.SetAttribute(Gfx.Attr(Color.Black, Color.Black));
        Move(0, row);
        for (int i = 0; i < width; i++) Driver!.AddRune(new System.Text.Rune(' '));
    }
}
