using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Log category → base color when no keyword rule matches. Healing collapses visually into Combat.
public enum LogCategory { General, Combat, System, Item, Healing, Dialog }

// Terminal.Gui view rendering per-line colored log. Priority: keyword rule > category color.
// Tab-key cycles the 5-view filter: All → Combat → System → Item → Dialog → All.
public class ColoredLogView : View
{
    // ── Log entry storage ──
    private readonly List<(string Text, LogCategory Category, int Count)> _entries = new();
    private int _scrollOffset;
    private const int MaxEntries = 500;

    // ── Tab filtering ── null = show all.
    private LogCategory? _filter;
    // Hook so GameScreen can visually sync its tab buttons with Tab-key cycling.
    public event Action<LogCategory?>? FilterChanged;

    // Wrapped-row cache invalidated on AddEntry/SetFilter/width change.
    private List<(string Text, Color Color, bool IsContinuation)>? _wrappedCache;
    private int _wrappedCacheWidth;

    // ── Category → fallback color (keyword rules in LogColorRules.cs) ──
    private static readonly Dictionary<LogCategory, Color> CategoryColors = new()
    {
        { LogCategory.Combat,  Color.BrightRed },
        { LogCategory.System,  Color.BrightCyan },
        { LogCategory.Item,    Color.BrightYellow },
        { LogCategory.Healing, Color.BrightGreen },
        { LogCategory.Dialog,  Color.White },
        { LogCategory.General, Color.Gray },
    };

    // Tab-cycle order: null (All) → Combat → System → Item → Dialog → back to null.
    private static readonly LogCategory?[] TabCycle =
    {
        null, LogCategory.Combat, LogCategory.System, LogCategory.Item, LogCategory.Dialog,
    };

    public ColoredLogView() { CanFocus = true; }

    // ── Public API ──
    // Last N entries (most recent last, unfiltered). Used by death recap.
    public List<string> GetRecentEntries(int count)
    {
        int start = Math.Max(0, _entries.Count - count);
        var result = new List<string>();
        for (int i = start; i < _entries.Count; i++)
        {
            var e = _entries[i];
            result.Add(e.Count > 1 ? $"{e.Text} (x{e.Count})" : e.Text);
        }
        return result;
    }

    // Add entry; consecutive dupes increment Count instead of adding a new line.
    public void AddEntry(string text, LogCategory category)
    {
        if (_entries.Count > 0)
        {
            var last = _entries[^1];
            if (last.Text == text && last.Category == category)
            {
                _entries[^1] = (text, category, last.Count + 1);
                _wrappedCache = null;
                SetNeedsDraw();
                return;
            }
        }
        _entries.Add((text, category, 1));
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(0, _entries.Count - MaxEntries);
        _wrappedCache = null;
        ScrollToEnd();
        SetNeedsDraw();
    }

    public LogCategory? Filter => _filter;

    public void SetFilter(LogCategory? category)
    {
        _filter = category;
        _wrappedCache = null;
        ScrollToEnd();
        SetNeedsDraw();
        FilterChanged?.Invoke(category);
    }

    // Healing entries map to the Combat tab so the player sees damage+heal in one stream.
    private bool Matches(LogCategory entry)
    {
        if (_filter == null) return true;
        if (_filter == LogCategory.Combat && entry == LogCategory.Healing) return true;
        return entry == _filter;
    }

    // ── Filtered entries with count suffix ──
    private List<(string Text, LogCategory Category)> GetVisibleEntries()
    {
        var result = new List<(string, LogCategory)>(_entries.Count);
        foreach (var e in _entries)
        {
            if (!Matches(e.Category)) continue;
            result.Add((e.Count > 1 ? $"{e.Text} (x{e.Count})" : e.Text, e.Category));
        }
        return result;
    }

    // ── Scrolling ── Viewport.Height is the authoritative visible row count; matches
    // the 1080p-tall panel exactly regardless of terminal-cell grid dimensions.
    public void ScrollToEnd()
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        int totalRows = GetWrappedRows(Math.Max(1, Viewport.Width)).Count;
        _scrollOffset = Math.Max(0, totalRows - visibleLines);
    }

    public void ScrollPageUp()
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        _scrollOffset = Math.Max(0, _scrollOffset - visibleLines);
        SetNeedsDraw();
    }

    public void ScrollPageDown()
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        int totalRows = GetWrappedRows(Math.Max(1, Viewport.Width)).Count;
        int maxScroll = Math.Max(0, totalRows - visibleLines);
        _scrollOffset = Math.Min(maxScroll, _scrollOffset + visibleLines);
        SetNeedsDraw();
    }

    // ── Word-wrapping ──
    // Cached wrapped rows; rebuilds only on entry/filter/width change.
    private List<(string Text, Color Color, bool IsContinuation)> GetWrappedRows(int width)
    {
        if (_wrappedCache != null && _wrappedCacheWidth == width) return _wrappedCache;
        _wrappedCache = BuildWrappedRows(width);
        _wrappedCacheWidth = width;
        return _wrappedCache;
    }

    // Continuation indent = 2 spaces. Preserves category color (research §5 — dim
    // continuation reads as "something's broken").
    private const string ContinuationIndent = "  ";

    private List<(string Text, Color Color, bool IsContinuation)> BuildWrappedRows(int width)
    {
        var rows = new List<(string, Color, bool)>();
        foreach (var (text, category) in GetVisibleEntries())
        {
            Color color = ResolveColor(text, category);
            if (text.Length <= width) { rows.Add((text, color, false)); continue; }
            bool first = true;
            int wrapWidth = Math.Max(1, width - ContinuationIndent.Length);
            foreach (var segment in WrapText(text, first ? width : wrapWidth))
            {
                if (first) { rows.Add((segment, color, false)); first = false; }
                else       { rows.Add((ContinuationIndent + segment, color, true)); }
            }
        }
        return rows;
    }

    private static IEnumerable<string> WrapText(string text, int width)
    {
        int start = 0;
        while (start < text.Length)
        {
            if (text.Length - start <= width)
            {
                yield return text.Substring(start);
                yield break;
            }
            int hardEnd = start + width;
            int breakAt = text.LastIndexOf(' ', hardEnd - 1, width);
            if (breakAt <= start) breakAt = hardEnd;
            yield return text.Substring(start, breakAt - start);
            start = breakAt;
            while (start < text.Length && text[start] == ' ') start++;
        }
    }

    // ── Rendering ── Draws each visible wrapped row with its resolved color.
    // Viewport.Height drives both scroll math and the render loop — matches the
    // actual pane height at any resolution (1080p rendering bug fix).
    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        var rows = GetWrappedRows(vp.Width);

        for (int row = 0; row < vp.Height; row++)
        {
            int idx = _scrollOffset + row;

            if (idx < 0 || idx >= rows.Count)
            {
                RenderBlankRow(row, vp.Width);
                continue;
            }

            var (text, fg, _) = rows[idx];
            Driver!.SetAttribute(Gfx.Attr(fg, Color.Black));
            Move(0, row);

            for (int c = 0; c < vp.Width; c++)
            {
                char ch = c < text.Length ? text[c] : ' ';
                if (char.IsSurrogate(ch)) ch = '?';
                Driver!.AddRune(new System.Text.Rune(ch));
            }
        }

        return true;
    }

    private void RenderBlankRow(int row, int width)
    {
        Driver!.SetAttribute(Gfx.Attr(Color.DarkGray, Color.Black));
        Move(0, row);
        for (int c = 0; c < width; c++)
            Driver!.AddRune(new System.Text.Rune(' '));
    }

    // ── Color resolution ── LogColorRules keyword (first match, case-insensitive),
    // then reward pattern ("+X EXP"/"+X Col" → BrightYellow), then CategoryColors fallback.
    private static Color ResolveColor(string text, LogCategory category)
    {
        foreach (var (keyword, color) in LogColorRules.Rules)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return color;
        }
        if (text.Contains('+') && (text.Contains("EXP") || text.Contains("Col")))
            return Color.BrightYellow;
        return CategoryColors.GetValueOrDefault(category, Color.White);
    }

    // ── Scroll input ── Tab cycles filter; Up/Down/PgUp/PgDn scroll.
    protected override bool OnKeyDown(Key keyEvent)
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        int maxScroll = Math.Max(0, GetWrappedRows(Math.Max(1, Viewport.Width)).Count - visibleLines);

        switch (keyEvent.KeyCode)
        {
            case KeyCode.Tab:
                CycleFilter(forward: true);
                keyEvent.Handled = true;
                return true;

            case KeyCode.CursorUp:
                _scrollOffset = Math.Max(0, _scrollOffset - 1);
                break;

            case KeyCode.CursorDown:
                _scrollOffset = Math.Min(maxScroll, _scrollOffset + 1);
                break;

            case KeyCode.PageUp:
                _scrollOffset = Math.Max(0, _scrollOffset - visibleLines);
                break;

            case KeyCode.PageDown:
                _scrollOffset = Math.Min(maxScroll, _scrollOffset + visibleLines);
                break;

            default:
                return base.OnKeyDown(keyEvent);
        }

        SetNeedsDraw();
        keyEvent.Handled = true;
        return true;
    }

    private void CycleFilter(bool forward)
    {
        int idx = Array.IndexOf(TabCycle, _filter);
        if (idx < 0) idx = 0;
        idx = forward ? (idx + 1) % TabCycle.Length
                      : (idx - 1 + TabCycle.Length) % TabCycle.Length;
        SetFilter(TabCycle[idx]);
    }
}
