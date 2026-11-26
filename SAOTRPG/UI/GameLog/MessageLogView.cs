using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.GameLog;

// The message log window, laid out as CDDA lays out its own: a grey frame, an 11-column
// light-blue column saying how long ago each message came, the newest line at the bottom, every
// line in its type's bright colour, a scrollbar in the left border, and a filter that takes a
// "type:" prefix. Its keys are the Log context's; Esc closes it as it closes every dialog.
public class MessageLogView : View
{
    private const int TimeWidth = 11;              // "  1    turn"
    private const int TextX = 1 + TimeWidth + 1;   // border, time, one column of padding

    private readonly ColoredLogView _log;
    private readonly List<(LogEntry Owner, string Text)> _rows = new();
    private int _rowsWidth = -1, _offset;
    private string _filter = "", _beforeEdit = "";
    private bool _editing;

    public MessageLogView(ColoredLogView log)
    {
        _log = log;
        CanFocus = true;
    }

    // For the probe.
    public string Filter => _filter;
    public bool Editing => _editing;
    public int Offset => _offset;
    public int MaxOffset => Math.Max(0, _rows.Count - MaxLines);
    public int MaxLines => Math.Max(1, Viewport.Height - 2);
    public int RowCount => _rows.Count;
    public string? RowText(int visibleRow) => Row(visibleRow)?.Text;

    private int TextWidth => Math.Max(1, Viewport.Width - TextX - 1);

    private (LogEntry Owner, string Text)? Row(int visibleRow)
    {
        int i = _offset + visibleRow;
        return visibleRow >= 0 && visibleRow < MaxLines && i < _rows.Count ? _rows[i] : null;
    }

    // "type:text" keeps one type when the prefix names one exactly; any other prefix is dropped,
    // as CDDA drops it. The text matches anywhere, ignoring case, " x N" included.
    public static (LogTone? Type, string Text) ParseFilter(string filter)
    {
        int colon = filter.IndexOf(':');
        return colon < 0 ? (null, filter) : (LogTones.Parse(filter[..colon]), filter[(colon + 1)..]);
    }

    // CDDA's to_string_clipped, right-aligned in 11 columns, counted in turns, this game's clock.
    public static string Elapsed(int turns)
    {
        turns = Math.Max(0, turns);
        string s = $"{turns,3}{(turns == 1 ? "turn" : "turns"),8}";
        if (s.Length > TimeWidth) s = $"{turns} turns".PadLeft(TimeWidth);
        return s.Length > TimeWidth ? s[^TimeWidth..] : s;
    }

    private string TimeOf(int visibleRow) => Elapsed(_log.Now - Row(visibleRow)!.Value.Owner.Turn);

    // What the time column holds on a visible row: the time on the newest row of a run that
    // shares one, a bracket above it, opening at the run's top row.
    public string GutterOf(int visibleRow)
    {
        if (Row(visibleRow) is null) return "";
        string time = TimeOf(visibleRow);
        if (Row(visibleRow + 1) is null || TimeOf(visibleRow + 1) != time) return time;
        return visibleRow == 0 || TimeOf(visibleRow - 1) != time ? "┌" : "│";
    }

    // CDDA's do_filter: rewrap what the filter keeps and go to the newest line.
    private void Rebuild()
    {
        _rows.Clear();
        var (type, text) = ParseFilter(_filter);
        foreach (var e in _log.Entries)
            if ((type is null || e.Tone == type) && e.Display.Contains(text, StringComparison.OrdinalIgnoreCase))
                ColoredLogView.Wrap(e, TextWidth, _rows);
        _rowsWidth = TextWidth;
        _offset = MaxOffset;
    }

    private void SetFilter(string filter)
    {
        _filter = filter;
        Rebuild();
    }

    // ── Keys ──
    protected override bool OnKeyDown(Key key)
    {
        if (_editing) EditKey(key);
        else
        {
            // Esc is the dialog's.
            if (key.KeyCode == KeyCode.Esc) return base.OnKeyDown(key);
            switch (Keybinds.Resolve(InputContext.Log, key))
            {
                case GameAction.MessageLogUp:          ScrollBy(-1); break;
                case GameAction.MessageLogDown:        ScrollBy(1); break;
                case GameAction.MessageLogPageUp:      ScrollBy(-MaxLines); break;
                case GameAction.MessageLogPageDown:    ScrollBy(MaxLines); break;
                case GameAction.MessageLogFilter:      _editing = true; _beforeEdit = _filter; break;
                case GameAction.MessageLogResetFilter: SetFilter(""); break;
                case GameAction.MessageLogClose:       AppHost.App.RequestStop(); break;
            }
        }
        // Every other key is swallowed: unhandled, Enter would close the dialog (rule 56) and Tab
        // would move focus off the only view in it (rule 289).
        key.Handled = true;
        SetNeedsDraw();
        return true;
    }

    // Live, as CDDA's filter popup is: Enter keeps the filter, Esc puts the old one back.
    private void EditKey(Key key)
    {
        if (key.KeyCode == KeyCode.Enter) _editing = false;
        else if (key.KeyCode == KeyCode.Esc) { _editing = false; SetFilter(_beforeEdit); }
        else if (key.KeyCode == KeyCode.Backspace) { if (_filter.Length > 0) SetFilter(_filter[..^1]); }
        else if (key.AsRune.Value is >= 32 and not 127) SetFilter(_filter + key.AsRune);
    }

    private void ScrollBy(int lines) => _offset = Math.Clamp(_offset + lines, 0, MaxOffset);

    // ── Rendering ──
    // Every cell is painted each pass, so the framework clear only duplicates output.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;
        int w = vp.Width, h = vp.Height;
        if (w < TextX + 2 || h < 3) return true;
        if (TextWidth != _rowsWidth) Rebuild();
        _offset = Math.Clamp(_offset, 0, MaxOffset);

        var bg = ColorSchemes.Theme.Base;
        var batch = Gfx.Begin(this);
        var border = Gfx.Attr(LogTones.Border(), bg);
        Box(batch, 0, 0, w, h, border);

        var time = Gfx.Attr(LogTones.Time(), bg);
        var bracket = Gfx.Attr(LogTones.Bracket(), bg);
        for (int r = 0; r < MaxLines; r++)
        {
            int y = 1 + r;
            var row = Row(r);
            string gutter = GutterOf(r);
            for (int c = 1; c <= TimeWidth; c++)
            {
                bool isBracket = gutter.Length == 1;
                char ch = isBracket ? (c == TimeWidth ? gutter[0] : ' ') : c - 1 < gutter.Length ? gutter[c - 1] : ' ';
                batch.Put(c, y, ch, isBracket ? bracket : time);
            }
            batch.Put(TimeWidth + 1, y, ' ', time);
            var attr = Gfx.Attr(row is { } rr ? LogTones.ColorOf(rr.Owner.Tone) : bg, bg);
            Put(batch, TextX, y, row?.Text ?? "", w - 1 - TextX, attr);
        }

        Scrollbar(batch, bg);
        if (_editing) FilterBox(batch, w, h, bg, border);
        else BottomHint(batch, w, h, bg, border);
        return true;
    }

    // CDDA's scrollbar, on the left border, only when the lines overflow.
    private void Scrollbar(Gfx.Batch batch, Color bg)
    {
        int lines = MaxLines;
        if (_rows.Count <= lines) return;
        var arrow = Gfx.Attr(LogTones.Arrow(), bg);
        batch.Put(0, 1, '^', arrow);
        batch.Put(0, lines, 'v', arrow);
        int track = lines - 2;
        if (track < 1) return;
        int thumb = Math.Clamp(track * lines / _rows.Count, 1, track);
        int at = (track - thumb) * _offset / Math.Max(1, MaxOffset);
        var slot = Gfx.Attr(LogTones.Bright(), bg);
        var bar = Gfx.Attr(LogTones.Help(), LogTones.Help());
        for (int i = 0; i < track; i++)
        {
            bool onThumb = i >= at && i < at + thumb;
            batch.Put(0, 2 + i, onThumb ? ' ' : '│', onThumb ? bar : slot);
        }
    }

    // The bottom border names the filter keys, or shows the filter while one is set.
    private void BottomHint(Gfx.Batch batch, int w, int h, Color bg, Attribute border)
    {
        string hint = _filter.Length == 0
            ? $"< Press {KeyName(GameAction.MessageLogFilter)} to filter, {KeyName(GameAction.MessageLogResetFilter)} to reset >"
            : $"< {_filter} >";
        Put(batch, 1, h - 1, hint, Math.Min(hint.Length, w - 2), border);
        if (_filter.Length > 0)
            Put(batch, 3, h - 1, _filter, Math.Clamp(_filter.Length, 0, w - 4), Gfx.Attr(LogTones.Bright(), bg));
    }

    // CDDA's filter popup: a box over the bottom of the log, its help in cyan, the input on its
    // bottom border.
    private void FilterBox(Gfx.Batch batch, int w, int h, Color bg, Attribute border)
    {
        var help = new List<List<(string Text, Color Fg)>>();
        foreach (string line in WrapWords(
                     "Type to search the whole log; case is ignored. Start with a message type "
                     + "and a colon to search that type only.", w - 2))
            help.Add(new() { (line, LogTones.Help()) });
        help.Add(new() { ("Examples:", LogTones.Help()) });
        foreach (string example in new[] { "good:Kobold", ":level up", "bad:" })
            help.Add(new() { ("  " + example, LogTones.Bright()) });
        var types = new List<(string, Color)> { ("Types: ", LogTones.Help()) };
        for (int i = 0; i < LogTones.Key.Length; i++)
        {
            if (i > 0) types.Add((", ", LogTones.Help()));
            types.Add((LogTones.Key[i].Name, LogTones.ColorOf(LogTones.Key[i].Tone)));
        }
        help.Add(types);

        int boxH = Math.Min(h, help.Count + 2), top = h - boxH;
        Box(batch, 0, top, w, boxH, border);
        for (int i = 0; i < boxH - 2; i++)
        {
            int x = 1, y = top + 1 + i;
            foreach (var (text, fg) in help[i])
            {
                Put(batch, x, y, text, w - 1 - x, Gfx.Attr(fg, bg));
                x = Math.Min(w - 1, x + text.Length);
            }
            Put(batch, x, y, "", w - 1 - x, Gfx.Attr(bg, bg));
        }

        Put(batch, 1, h - 1, "< ", 2, border);
        Put(batch, w - 3, h - 1, " >", 2, border);
        int room = Math.Max(1, w - 6);
        string shown = _filter.Length >= room ? _filter[^(room - 1)..] : _filter;
        var input = Gfx.Attr(LogTones.Bright(), bg);
        Put(batch, 3, h - 1, shown, room, input);
        batch.Put(3 + shown.Length, h - 1, ' ', Gfx.Attr(bg, LogTones.Bright()));   // the cursor
    }

    private static string KeyName(GameAction a)
    {
        string k = KeyHints.Key(a);
        return k.Length == 1 ? k.ToLowerInvariant() : k;
    }

    private static List<string> WrapWords(string text, int width)
    {
        var lines = new List<string>();
        var entry = new LogEntry { Text = text };
        var rows = new List<(LogEntry, string Text)>();
        ColoredLogView.Wrap(entry, Math.Max(1, width), rows);
        foreach (var (_, t) in rows) lines.Add(t);
        return lines;
    }

    private static void Box(Gfx.Batch batch, int x0, int y0, int w, int h, Attribute attr)
    {
        for (int x = x0; x < x0 + w; x++)
        {
            bool l = x == x0, r = x == x0 + w - 1;
            batch.Put(x, y0, l ? '┌' : r ? '┐' : '─', attr);
            batch.Put(x, y0 + h - 1, l ? '└' : r ? '┘' : '─', attr);
        }
        for (int y = y0 + 1; y < y0 + h - 1; y++)
        {
            batch.Put(x0, y, '│', attr);
            batch.Put(x0 + w - 1, y, '│', attr);
        }
    }

    // Writes text from x, padded with blanks to exactly `width` cells.
    private static void Put(Gfx.Batch batch, int x, int y, string text, int width, Attribute attr)
    {
        for (int c = 0; c < width; c++)
        {
            char ch = c < text.Length ? text[c] : ' ';
            if (char.IsSurrogate(ch)) ch = '?';
            batch.Put(x + c, y, ch, attr);
        }
    }
}
