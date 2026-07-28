using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// HUD quest tracker: 28x2 block anchored top-right below minimap. Shows ONLY the pinned quest.
// Hides when unpinned; shows green "COMPLETE — return to X" once state flips until turn-in clears pin.
public class QuestTrackerWidget : View
{
    public const int TrackerWidth = 28;
    public const int TrackerHeight = 2;

    public QuestTrackerWidget()
    {
        Width = TrackerWidth;
        Height = TrackerHeight;
        CanFocus = false;
        SchemeName = ColorSchemes.BodyName;
    }

    // Children repaint every cell they own, and the framework clear would otherwise blank
    // this widget during the window where it skips painting beneath a dialog.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        var vp = Viewport;
        if (vp.Width <= 0 || vp.Height <= 0) return true;

        var batch = Gfx.Begin(this);
        var quest = QuestSystem.PinnedQuest();
        // Empty-state: blank both rows (widget effectively hides).
        if (quest == null)
        {
            BlankRow(batch, 0, vp.Width);
            if (vp.Height > 1) BlankRow(batch, 1, vp.Width);
            return true;
        }

        bool complete = quest.Status == QuestStatus.Complete;
        Color nameColor = complete ? Color.BrightGreen : Color.BrightYellow;
        string line1 = TextHelpers.Truncate(quest.Title, TrackerWidth);
        string line2 = complete
            ? TextHelpers.Truncate($"COMPLETE — return to {quest.GiverName}", TrackerWidth)
            : BuildProgressLine(quest);

        DrawRow(batch, 0, line1, nameColor, vp.Width);
        if (vp.Height > 1)
        {
            Color line2Color = complete ? Color.BrightGreen : Color.Gray;
            DrawRow(batch, 1, line2, line2Color, vp.Width);
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

    // Batched rather than the framework's SetAttribute/Move/AddRune trio, which re-parses the
    // grapheme and allocates per cell. Identical output: same glyphs, attributes and columns.
    private static void DrawRow(Gfx.Batch batch, int row, string text, Color fg, int width)
    {
        var attr = Gfx.Attr(fg, Color.Black);
        int i = 0;
        for (; i < text.Length && i < width; i++) batch.Put(i, row, text[i], attr);
        for (; i < width; i++) batch.Put(i, row, ' ', attr);
    }

    private static void BlankRow(Gfx.Batch batch, int row, int width)
    {
        var attr = Gfx.Attr(Color.Black, Color.Black);
        for (int i = 0; i < width; i++) batch.Put(i, row, ' ', attr);
    }
}
