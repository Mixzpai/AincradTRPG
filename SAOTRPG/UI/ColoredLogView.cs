using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Log category → base color when no keyword rule matches.
public enum LogCategory { General, Combat, System, Loot, Healing }

// Terminal.Gui view rendering per-line colored log. Priority: keyword rule > category color.
// Tab filter narrows to one category; Up/Down/PageUp/PageDown scroll; auto-scrolls on AddEntry.
public class ColoredLogView : View
{
    // ── Log entry storage ──
    private readonly List<(string Text, LogCategory Category, int Count)> _entries = new();
    private int _scrollOffset;
    private const int MaxEntries = 500;

    // ── Tab filtering ── null = show all.
    private LogCategory? _filter;

    // Wrapped-row cache invalidated on AddEntry/SetFilter/width change.
    private List<(string Text, Color Color)>? _wrappedCache;
    private int _wrappedCacheWidth;

    // ── Category → fallback color (keyword rules in LogColorRules.cs) ──
    private static readonly Dictionary<LogCategory, Color> CategoryColors = new()
    {
        { LogCategory.Combat,  Color.BrightRed },
        { LogCategory.System,  Color.BrightCyan },
        { LogCategory.Loot,    Color.BrightYellow },
        { LogCategory.Healing, Color.BrightGreen },
        { LogCategory.General, Color.White },
    };

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

    public void SetFilter(LogCategory? category)
    {
        _filter = category;
        _wrappedCache = null;
        ScrollToEnd();
        SetNeedsDraw();
    }

    // ── Filtered entries with count suffix ──
    private List<(string Text, LogCategory Category)> GetVisibleEntries()
    {
        var source = _filter == null
            ? _entries
            : _entries.Where(e => e.Category == _filter.Value).ToList();
        return source.Select(e =>
            (e.Count > 1 ? $"{e.Text} (x{e.Count})" : e.Text, e.Category)
        ).ToList();
    }

    // ── Scrolling ──
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
    private List<(string Text, Color Color)> GetWrappedRows(int width)
    {
        if (_wrappedCache != null && _wrappedCacheWidth == width) return _wrappedCache;
        _wrappedCache = BuildWrappedRows(width);
        _wrappedCacheWidth = width;
        return _wrappedCache;
    }

    private List<(string Text, Color Color)> BuildWrappedRows(int width)
    {
        var rows = new List<(string, Color)>();
        foreach (var (text, category) in GetVisibleEntries())
        {
            Color color = ResolveColor(text, category);
            if (text.Length <= width)
            {
                rows.Add((text, color));
                continue;
            }
            foreach (var segment in WrapText(text, width))
                rows.Add((segment, color));
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
            if (breakAt <= start) breakAt = hardEnd; // no space → hard break
            yield return text.Substring(start, breakAt - start);
            start = breakAt;
            while (start < text.Length && text[start] == ' ') start++;
        }
    }

    // ── Rendering ── Draws each visible wrapped row with its resolved color.
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

            var (text, fg) = rows[idx];
            Driver!.SetAttribute(Gfx.Attr(fg, Color.Black));
            Move(0, row);

            for (int c = 0; c < vp.Width; c++)
            {
                char ch = c < text.Length ? text[c] : ' ';
                if (char.IsSurrogate(ch)) ch = '?';  // surrogate pair → '?'
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

    // ── Color resolution ──
    // 1) LogColorRules keyword (first match wins, case-insensitive)
    // 2) reward pattern ("+X EXP" / "+X Col") → BrightYellow, 3) CategoryColors fallback.
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

    // ── Scroll input ── Up/Down/PgUp/PgDn when focused.
    protected override bool OnKeyDown(Key keyEvent)
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        int maxScroll = Math.Max(0, GetWrappedRows(Math.Max(1, Viewport.Width)).Count - visibleLines);

        switch (keyEvent.KeyCode)
        {
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
}
