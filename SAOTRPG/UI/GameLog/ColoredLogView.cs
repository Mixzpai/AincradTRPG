using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.GameLog;

// Where a message came from: the classifier's fallback when no phrase decides its type.
public enum LogCategory { General, Combat, System, Item, Dialog }

// One message: its text, its CDDA type, how many times it repeated, and the turn it was last said.
public sealed class LogEntry
{
    public required string Text { get; init; }
    public LogTone Tone { get; init; }
    public int Count { get; internal set; } = 1;
    public int Turn { get; internal set; }

    // CDDA's "%s x %d".
    public string Display => Count > 1 ? $"{Text} x {Count}" : Text;
}

// The sidebar message log, drawn as CDDA draws its own: the newest line on the bottom row,
// wrapped lines flush, nothing else in the pane. A line is in its type's colour while new, in the
// type's darker colour for five turns after that, then grey. MessageLogView is the full window.
public class ColoredLogView : View
{
    // CDDA's MESSAGE_LIMIT default. It trims before it adds, so the log holds one more.
    public const int MessageLimit = 255;

    private readonly List<LogEntry> _entries = new();

    // Wrapped rows, oldest first, appended as entries arrive; only a width change rewraps.
    private readonly List<(LogEntry Owner, string Text)> _rows = new();
    private int _rowsWidth = -1;

    // CDDA's curmes: the turn up to which the player has seen the log.
    private int _curmes, _drawnTurn;

    public ColoredLogView() => CanFocus = false;

    // The current turn (CDDA's calendar::turn), set by GameLogView. A new turn ages the lines, so
    // it repaints while the newest line is still short of grey; nothing else marks the log then.
    public int Now
    {
        get => _now;
        set
        {
            if (value == _now) return;
            _now = value;
            if (_entries.Count > 0 && _entries[^1].Turn + 6 >= _drawnTurn) SetNeedsDraw();
        }
    }
    private int _now;

    public IReadOnlyList<LogEntry> Entries => _entries;

    // Last N entries, oldest first (death recap).
    public List<string> GetRecentEntries(int count) =>
        _entries.Skip(Math.Max(0, _entries.Count - count)).Select(e => e.Display).ToList();

    // Drop every entry. Used by F9 hot-reload.
    public void Clear()
    {
        _entries.Clear();
        _rows.Clear();
        SetNeedsDraw();
    }

    public void AddEntry(string text, LogCategory category, int turn = 0)
    {
        using var prof = SAOTRPG.Systems.Diagnostics.Profiler.Begin("Log.AddEntry");
        if (string.IsNullOrEmpty(text)) return;
        var tone = LogTones.Classify(text, category);

        // CDDA coalesces with the last message only: same type and text, within three turns.
        if (_entries.Count > 0 && _entries[^1] is var last
            && last.Text == text && last.Tone == tone && last.Turn + 3 >= turn)
        {
            last.Count++;
            last.Turn = turn;
            if (_rowsWidth > 0)
            {
                int n = 0;
                while (n < _rows.Count && ReferenceEquals(_rows[^(n + 1)].Owner, last)) n++;
                _rows.RemoveRange(_rows.Count - n, n);
                Wrap(last, _rowsWidth, _rows);
            }
            SetNeedsDraw();
            return;
        }

        if (_entries.Count > MessageLimit)
        {
            _entries.RemoveRange(0, _entries.Count - MessageLimit);
            int n = 0;
            while (n < _rows.Count && !ReferenceEquals(_rows[n].Owner, _entries[0])) n++;
            _rows.RemoveRange(0, n);
        }
        var entry = new LogEntry { Text = text, Tone = tone, Turn = turn };
        _entries.Add(entry);
        if (_rowsWidth > 0) Wrap(entry, _rowsWidth, _rows);
        SetNeedsDraw();
    }

    // CDDA moves curmes to the current turn after each sidebar draw, and draws once per input.
    // This view draws every frame, so curmes moves once per turn change, which is when CDDA's would.
    public void AdvanceCurmes()
    {
        if (Now == _drawnTurn) return;
        _curmes = _drawnTurn;
        _drawnTurn = Now;
    }

    // Closing the message log window marks everything read, as CDDA's does.
    public void MarkSeen() => _curmes = _drawnTurn = Now;

    // CDDA's get_color: new from curmes on, slightly old for five turns before it, then grey.
    public Color ColorFor(LogEntry e) =>
        e.Turn >= _curmes ? LogTones.ColorOf(e.Tone)
        : e.Turn + 5 >= _curmes ? LogTones.ColorOf(e.Tone, old: true)
        : LogTones.Old();

    // ── Rows ──

    // Wraps for this width when the rows were built for another. Public so TileVerifyProbe can
    // drive the row cache without a terminal; the draw calls it with the pane's width.
    public void EnsureRows(int width)
    {
        if (width != _rowsWidth) RebuildRows(width);
    }

    // The reference: a full rewrap of the history, compared row by row with the incremental
    // cache, which is then put back so the cache under test keeps evolving incrementally.
    public int CountRowMismatches()
    {
        if (_rowsWidth <= 0) return 0;
        var incremental = new List<(LogEntry Owner, string Text)>(_rows);
        RebuildRows(_rowsWidth);
        int bad = Math.Abs(incremental.Count - _rows.Count);
        for (int i = 0; i < Math.Min(incremental.Count, _rows.Count); i++)
            if (!ReferenceEquals(incremental[i].Owner, _rows[i].Owner) || incremental[i].Text != _rows[i].Text)
                bad++;
        _rows.Clear();
        _rows.AddRange(incremental);
        return bad;
    }

    private void RebuildRows(int width)
    {
        _rows.Clear();
        foreach (var e in _entries) Wrap(e, width, _rows);
        _rowsWidth = width;
    }

    // Word-wrapped and flush, as CDDA folds a message; a word longer than the line is cut.
    internal static void Wrap(LogEntry e, int width, List<(LogEntry Owner, string Text)> rows)
    {
        string text = e.Display;
        int start = 0;
        do
        {
            int end;
            if (text.Length - start <= width) end = text.Length;
            else
            {
                int breakAt = text.LastIndexOf(' ', start + width, width + 1);
                end = breakAt <= start ? start + width : breakAt;
            }
            rows.Add((e, text[start..end]));
            start = end;
            while (start < text.Length && text[start] == ' ') start++;
        } while (start < text.Length);
    }

    // ── Rendering ──
    // Every cell of the pane is painted each pass, so the framework clear only duplicates output.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        using var prof = SAOTRPG.Systems.Diagnostics.Profiler.Begin("Draw.Log");
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;

        var vp = Viewport;
        if (vp.Width <= 0 || vp.Height <= 0) return true;
        EnsureRows(vp.Width);
        AdvanceCurmes();

        var bg = ColorSchemes.Theme.Base;
        var batch = Gfx.Begin(this);
        // The newest row on the bottom row; rows the history cannot fill stay blank above it.
        int first = _rows.Count - vp.Height;
        for (int y = 0; y < vp.Height; y++)
        {
            int idx = first + y;
            string text = idx >= 0 ? _rows[idx].Text : "";
            var attr = Gfx.Attr(idx >= 0 ? ColorFor(_rows[idx].Owner) : bg, bg);
            for (int c = 0; c < vp.Width; c++)
            {
                char ch = c < text.Length ? text[c] : ' ';
                if (char.IsSurrogate(ch)) ch = '?';
                batch.Put(c, y, ch, attr);
            }
        }
        return true;
    }
}
