using Terminal.Gui;

namespace SAOTRPG.UI;

/// <summary>
/// Log message categories — determines base color when no keyword match is found.
/// </summary>
public enum LogCategory { General, Combat, System, Loot, Healing }

/// <summary>
/// Custom Terminal.Gui view that renders game log messages with per-line coloring.
///
/// Color priority:
///   1. Keyword rules (checked first — content-specific overrides)
///   2. Category color (fallback based on LogCategory)
///
/// Supports scrolling with Up/Down/PageUp/PageDown when focused.
/// Auto-scrolls to bottom on new entries.
/// </summary>
public class ColoredLogView : View
{
    // ── Log entry storage ────────────────────────────────────────────
    private readonly List<(string Text, LogCategory Category)> _entries = new();
    private int _scrollOffset;

    // ── Keyword → color rules ────────────────────────────────────────
    // Checked in order — first match wins. Add new rules here.
    // Each rule: (keyword substring, resulting color)
    private static readonly (string Keyword, Color Color)[] KeywordRules =
    {
        // Critical / special moments
        ("CRITICAL HIT",    Color.BrightMagenta),
        ("LEVEL UP",        Color.BrightMagenta),
        ("UNSTOPPABLE",     Color.BrightMagenta),
        ("Rampage",         Color.BrightMagenta),
        ("PERFECT!",        Color.BrightYellow),

        // Kill / loot rewards
        ("defeated!",       Color.BrightYellow),
        ("dropped a",       Color.BrightYellow),
        ("Picked up",       Color.BrightYellow),
        ("slain!",          Color.BrightYellow),
        ("FLOOR BOSS",      Color.BrightMagenta),

        // Combo / streak
        ("combo!",          Color.BrightCyan),
        ("streak",          Color.BrightCyan),
        ("Double Kill",     Color.BrightCyan),
        ("Triple Kill",     Color.BrightCyan),

        // Economy
        ("Purchased",       Color.BrightGreen),
        ("Sold",            Color.BrightGreen),

        // Dodge
        ("you dodge",       Color.BrightCyan),

        // Healing / campfire
        ("restores",        Color.BrightGreen),
        ("healed",          Color.BrightGreen),
        ("warmth restores", Color.BrightGreen),

        // Status effects
        ("poisoned",        Color.Green),
        ("bleeding",        Color.Red),
        ("starving",        Color.Yellow),

        // Traps
        ("spike trap",      Color.BrightRed),
        ("teleport trap",   Color.BrightRed),
        ("lava",            Color.BrightRed),

        // Floor transitions
        ("Ascending to",    Color.BrightCyan),
        ("Welcome to Floor",Color.BrightCyan),

        // Proficiency
        ("proficiency",     Color.BrightMagenta),

        // Near death
        ("near death",      Color.BrightRed),
        ("barely standing", Color.BrightRed),
        ("One more hit",    Color.BrightRed),
    };

    // ── Category → fallback color ────────────────────────────────────
    private static readonly Dictionary<LogCategory, Color> CategoryColors = new()
    {
        { LogCategory.Combat,  Color.BrightRed },
        { LogCategory.System,  Color.BrightCyan },
        { LogCategory.Loot,    Color.BrightYellow },
        { LogCategory.Healing, Color.BrightGreen },
        { LogCategory.General, Color.White },
    };

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>Returns the last N log entries (most recent last). Used by death recap.</summary>
    public List<string> GetRecentEntries(int count)
    {
        int start = Math.Max(0, _entries.Count - count);
        var result = new List<string>();
        for (int i = start; i < _entries.Count; i++)
            result.Add(_entries[i].Text);
        return result;
    }

    /// <summary>Add a new log entry and auto-scroll to bottom.</summary>
    public void AddEntry(string text, LogCategory category)
    {
        _entries.Add((text, category));
        ScrollToEnd();
        SetNeedsDraw();
    }

    /// <summary>Scroll the view to show the most recent entries.</summary>
    public void ScrollToEnd()
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        _scrollOffset = Math.Max(0, _entries.Count - visibleLines);
    }

    /// <summary>Scroll up by one page (called from MapView via GameScreen).</summary>
    public void ScrollPageUp()
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        _scrollOffset = Math.Max(0, _scrollOffset - visibleLines);
        SetNeedsDraw();
    }

    /// <summary>Scroll down by one page (called from MapView via GameScreen).</summary>
    public void ScrollPageDown()
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        int maxScroll = Math.Max(0, _entries.Count - visibleLines);
        _scrollOffset = Math.Min(maxScroll, _scrollOffset + visibleLines);
        SetNeedsDraw();
    }

    // ── Rendering ────────────────────────────────────────────────────

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;

        for (int row = 0; row < vp.Height; row++)
        {
            int idx = _scrollOffset + row;

            // Blank rows past the end of entries
            if (idx < 0 || idx >= _entries.Count)
            {
                RenderBlankRow(row, vp.Width);
                continue;
            }

            // Render colored text
            var (text, category) = _entries[idx];
            Color fg = ResolveColor(text, category);
            Driver.SetAttribute(new Terminal.Gui.Attribute(fg, Color.Black));
            Move(0, row);

            for (int c = 0; c < vp.Width; c++)
            {
                char ch = c < text.Length ? text[c] : ' ';
                Driver.AddRune(new System.Text.Rune(ch));
            }
        }

        return true;
    }

    /// <summary>Fill a row with blank space (used for empty rows below content).</summary>
    private void RenderBlankRow(int row, int width)
    {
        Driver.SetAttribute(new Terminal.Gui.Attribute(Color.DarkGray, Color.Black));
        Move(0, row);
        for (int c = 0; c < width; c++)
            Driver.AddRune(new System.Text.Rune(' '));
    }

    // ── Color resolution ─────────────────────────────────────────────

    /// <summary>
    /// Determine the foreground color for a log line.
    /// Keyword rules are checked first (case-insensitive), then category fallback.
    /// </summary>
    private static Color ResolveColor(string text, LogCategory category)
    {
        // Check keyword rules first (content-specific overrides)
        foreach (var (keyword, color) in KeywordRules)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return color;
        }

        // XP/Col reward lines (need multi-keyword check)
        if (text.Contains('+') && (text.Contains("EXP") || text.Contains("Col")))
            return Color.BrightYellow;

        // Fall back to category color
        return CategoryColors.GetValueOrDefault(category, Color.White);
    }

    // ── Scroll input handling ────────────────────────────────────────

    protected override bool OnKeyDown(Key keyEvent)
    {
        int visibleLines = Math.Max(1, Viewport.Height);
        int maxScroll = Math.Max(0, _entries.Count - visibleLines);

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
