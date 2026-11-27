namespace SAOTRPG.UI.Helpers;

// Terminal.Gui rendering shim.
//
// Cell writes go straight into the driver's output buffer rather than through
// AddRune/AddStr. Those re-parse the grapheme cluster, call MakePrintable and
// GetColumns, and allocate per cell; at map scale that dominates frame time and
// drives constant gen-0 collection. Assigning a prebuilt Cell is allocation-free,
// while applying the same Clip test the driver performs internally.
//
// The only file that touches the driver's cell buffer, so a renderer or backend change
// is confined here.
public static class Gfx
{
    // Create a color attribute from foreground + background colors, with an optional
    // text style. Style carries hierarchy that is orthogonal to colour — Bold for
    // emphasis — but terminal support varies (conhost renders neither Faint nor
    // Italic), so style must reinforce a colour difference, never replace one.
    public static Attribute Attr(Color fg, Color bg, TextStyle style = TextStyle.None)
        => new Attribute(fg, bg) { Style = style };

    // Shorthand: fg on black background (most common case).
    public static Attribute Attr(Color fg)
        => new Attribute(fg, Color.Black);

    // Prebuilt Cells keyed by glyph. Caching them keeps the hot path off both the
    // per-call string allocation and Cell's grapheme validation, which runs in the
    // Grapheme setter. Bounded by the game's glyph set; UI-thread only.
    private static readonly Dictionary<char, Cell> s_cellByChar = new();

    // ── Write accounting (diagnostic — see DebugMode.PerfSampling) ──
    // Latched at type-init rather than read per call so the JIT can treat it as a
    // constant and drop the guarded blocks entirely from the per-cell path when off.
    // DebugMode must therefore be final before Gfx is first touched; Program.Main
    // parses every flag ahead of AppHost.Start for exactly this reason.
    internal static readonly bool Counting = SAOTRPG.Systems.Diagnostics.DebugMode.PerfSampling;

    // Counts cells actually committed vs skipped as unchanged, so the per-frame
    // volume reaching the terminal can be told apart from draw-path overhead.
    internal static long CellsWritten, CellsSkipped;
    // Rows with a dirty cell, and the dirty cells the driver will send — the transmitted
    // volume, which is what flush time tracks.
    internal static long RowsDirty, CellsTransmitted;
    // Attribute changes in transmission order: the driver emits a colour escape (~19 bytes for
    // a truecolor fg, more when bg or style also change) only when the attribute differs from
    // the previous cell it sent. Gaps are runs of dirty cells, each costing one cursor move.
    internal static long AttrRuns, Gaps;

    internal static (long written, long skipped, long rows, long sent, long runs, long gaps) DrainCounters()
    {
        var result = (CellsWritten, CellsSkipped, RowsDirty, CellsTransmitted, AttrRuns, Gaps);
        CellsWritten = 0; CellsSkipped = 0; RowsDirty = 0; CellsTransmitted = 0; AttrRuns = 0; Gaps = 0;
        return result;
    }

    private static Cell CellFor(char ch)
    {
        if (!s_cellByChar.TryGetValue(ch, out Cell cell))
        {
            cell = new Cell { Grapheme = ch.ToString(), IsDirty = true };
            s_cellByChar[ch] = cell;
        }

        return cell;
    }

    // Last glyph handed to MakeCell. Runs of one character are the norm across terrain,
    // so this skips most of the dictionary lookups. UI-thread only, like s_cellByChar.
    private static char s_lastMadeCh;
    private static Cell s_lastMadeCell;
    private static bool s_hasLastMade;

    // A resolved cell, before it becomes a driver Cell.
    //
    // Deliberately reference-free. Cell carries its grapheme as a string, so an array of Cells
    // pays a GC write barrier on every element store and cannot be memmoved — and the map's
    // tile layer is a viewport-sized array that is written and scrolled every frame. Holding
    // (char, Attribute) instead makes both operations plain memory traffic; the grapheme is
    // resolved once per cell that actually reaches the driver, in BlitGlyphs.
    public struct Glyph
    {
        public char Ch;
        public Attribute Attr;

        public Glyph(char ch, Attribute attr) { Ch = ch; Attr = attr; }
    }

    // Replays a composed glyph layer into the driver buffer, skipping any cell the terminal
    // already shows. Mirrors BlitRegion; the layer, not the driver's contents, is the source
    // of truth. Returns false when the driver is unavailable.
    public static bool BlitGlyphs(View view, Glyph[] src, int width, int height)
    {
        var buffer = AppHost.App.Driver?.GetOutputBuffer();
        var contents = buffer?.Contents;
        if (contents is null) return false;

        int rows = contents.GetLength(0), cols = contents.GetLength(1);
        var clip = ClipBounds(buffer, cols, rows);
        var origin = view.ViewportToScreen(new System.Drawing.Point(0, 0));
        // The visible span is the same on every row, so it is solved once instead of costing four
        // compares per cell across a full viewport. ClipBounds has already intersected the clip
        // with the buffer, so these bounds subsume the sx/sy range tests they replace.
        var (vx0, vy0, vx1, vy1) = VisibleSpan(clip, origin, width, height);

        for (int vy = vy0; vy < vy1; vy++)
        {
            int sy = origin.Y + vy;
            int rowBase = vy * width;
            bool rowTouched = false;
            for (int vx = vx0; vx < vx1; vx++)
            {
                int sx = origin.X + vx;
                Glyph g = src[rowBase + vx];
                ref Cell existing = ref contents[sy, sx];
                if (existing.Attribute == g.Attr
                    && existing.Grapheme is { Length: 1 } s && s[0] == g.Ch)
                {
                    if (Counting) CellsSkipped++;
                    continue;
                }

                Cell cell = MakeCell(g.Ch, g.Attr);
                cell.IsDirty = true;
                existing = cell;
                rowTouched = true;
                if (Counting) CellsWritten++;
            }

            if (rowTouched) buffer!.DirtyLines[sy] = true;
        }

        return true;
    }

    // Builds a driver Cell without touching the output buffer.
    public static Cell MakeCell(char ch, Attribute attr)
    {
        Cell cell;
        if (s_hasLastMade && ch == s_lastMadeCh)
        {
            cell = s_lastMadeCell;
        }
        else
        {
            cell = CellFor(ch);
            s_lastMadeCh = ch; s_lastMadeCell = cell; s_hasLastMade = true;
        }

        cell.Attribute = attr;
        return cell;
    }

    // Counts what the driver will send this frame, for --perf: dirty rows, dirty cells, colour
    // changes and gaps. Read-only. GameWindow.DrawComplete calls it after every view has painted
    // and before the driver flushes.
    //
    // The driver sends the frame in one write with one cursor move per gap, so only changed cells
    // go out. Dirty rows must not be filled end to end: the window border dirties every row, so a
    // fill would resend the whole screen.
    public static void CountTransmitted()
    {
        var buffer = AppHost.App.Driver?.GetOutputBuffer();
        var contents = buffer?.Contents;
        if (contents is null) return;

        int rows = contents.GetLength(0), cols = contents.GetLength(1);
        // Frame-wide, like the driver's own last-attribute: a colour carries across rows and gaps.
        Attribute? prev = null;
        for (int row = 0; row < rows; row++)
        {
            if (!buffer!.DirtyLines[row]) continue;

            bool rowSent = false, inGap = true;
            for (int col = 0; col < cols; col++)
            {
                if (!contents[row, col].IsDirty) { inGap = true; continue; }

                if (inGap) { Gaps++; inGap = false; }
                rowSent = true;
                CellsTransmitted++;
                Attribute? a = contents[row, col].Attribute;
                if (a != prev) AttrRuns++;
                prev = a;
            }

            if (rowSent) RowsDirty++;
        }
    }

    // Resolves the driver clip to integer bounds, intersected with the buffer.
    // Region.Contains takes a lock and walks a rectangle list on every call, which at
    // viewport scale is tens of thousands of lock acquisitions per frame; the driver's
    // own FillRect likewise clips against the bounding box.
    private static (int X0, int Y0, int X1, int Y1) ClipBounds(
        Terminal.Gui.Drivers.IOutputBuffer? buffer, int cols, int rows)
    {
        int x0 = 0, y0 = 0, x1 = cols, y1 = rows;
        if (buffer?.Clip is { } clip)
        {
            var b = clip.GetBounds();
            if (b.Left > x0) x0 = b.Left;
            if (b.Top > y0) y0 = b.Top;
            if (b.Right < x1) x1 = b.Right;
            if (b.Bottom < y1) y1 = b.Bottom;
        }

        return (x0, y0, x1, y1);
    }

    // The view-relative rectangle whose screen cells are inside both the buffer and the clip.
    // Solving it once per pass turns four per-cell compares into loop bounds; ClipBounds has
    // already intersected the clip with the buffer, so no separate 0..cols test is needed.
    private static (int X0, int Y0, int X1, int Y1) VisibleSpan(
        (int X0, int Y0, int X1, int Y1) clip, System.Drawing.Point origin, int width, int height)
    {
        int x0 = clip.X0 - origin.X; if (x0 < 0) x0 = 0;
        int y0 = clip.Y0 - origin.Y; if (y0 < 0) y0 = 0;
        int x1 = clip.X1 - origin.X; if (x1 > width) x1 = width;
        int y1 = clip.Y1 - origin.Y; if (y1 > height) y1 = height;
        return (x0, y0, x1, y1);
    }

    // ── Region snapshot / replay ───────────────────────────────────────
    // Backs a caller-owned frame cache: capture a view's painted cells, then replay
    // them on frames where nothing changed. Cells are copied as structs, so graphemes
    // (including non-BMP) survive without re-parsing.

    // Copies the view's on-screen cells into dst, row-major, width*height.
    // Returns false when the driver is unavailable, leaving dst untouched.
    public static bool CaptureRegion(View view, Cell[] dst, int width, int height)
    {
        var contents = AppHost.App.Driver?.GetOutputBuffer()?.Contents;
        if (contents is null) return false;

        int rows = contents.GetLength(0), cols = contents.GetLength(1);
        var origin = view.ViewportToScreen(new System.Drawing.Point(0, 0));
        // Unclipped — a capture reads whatever is on screen — so the span is the buffer itself.
        var (vx0, vy0, vx1, vy1) = VisibleSpan((0, 0, cols, rows), origin, width, height);

        for (int vy = vy0; vy < vy1; vy++)
        {
            int sy = origin.Y + vy;
            int rowBase = vy * width;
            for (int vx = vx0; vx < vx1; vx++)
            {
                int sx = origin.X + vx;
                Cell cell = contents[sy, sx];
                // Never-written cells carry a null attribute; pin the default so a
                // later replay doesn't hand the driver an unset colour.
                cell.Attribute ??= Attr(Color.White, Color.Black);
                dst[rowBase + vx] = cell;
            }
        }

        return true;
    }

    // Replays captured cells back into the buffer, skipping any the terminal already
    // shows. Returns false when the driver is unavailable.
    public static bool BlitRegion(View view, Cell[] src, int width, int height)
    {
        var buffer = AppHost.App.Driver?.GetOutputBuffer();
        var contents = buffer?.Contents;
        if (contents is null) return false;

        int rows = contents.GetLength(0), cols = contents.GetLength(1);
        var clip = ClipBounds(buffer, cols, rows);
        var origin = view.ViewportToScreen(new System.Drawing.Point(0, 0));
        var (vx0, vy0, vx1, vy1) = VisibleSpan(clip, origin, width, height);

        for (int vy = vy0; vy < vy1; vy++)
        {
            int sy = origin.Y + vy;
            int rowBase = vy * width;
            bool rowTouched = false;
            for (int vx = vx0; vx < vx1; vx++)
            {
                int sx = origin.X + vx;
                Cell cell = src[rowBase + vx];

                // Replaying an unchanged snapshot as dirty would rewrite the whole
                // viewport every idle frame.
                ref Cell existing = ref contents[sy, sx];
                if (existing.Attribute == cell.Attribute
                    && string.Equals(existing.Grapheme, cell.Grapheme, StringComparison.Ordinal))
                {
                    if (Counting) CellsSkipped++;
                    continue;
                }

                cell.IsDirty = true;
                existing = cell;
                rowTouched = true;
                if (Counting) CellsWritten++;
            }

            if (rowTouched) buffer!.DirtyLines[sy] = true;
        }

        return true;
    }

    // Per-frame writer for bulk painting. Resolves the buffer, clip and screen origin
    // once instead of per cell — PutCell's ViewportToScreen call alone is significant
    // across a full-viewport tile loop. A struct, so a paint allocates nothing: hold it in a
    // local or a field and pass it by ref, since a copy starts its glyph cache over.
    public struct Batch
    {
        private readonly Terminal.Gui.Drivers.IOutputBuffer? _buffer;
        private readonly Cell[,]? _contents;
        private readonly int _ox, _oy;
        // Clip resolved once to integer bounds. Region.Contains locks and walks a
        // rectangle list on every call, which at viewport scale is tens of thousands
        // of lock acquisitions per frame; the driver's own FillRect likewise clips
        // against the bounding box.
        private readonly int _clipX0, _clipY0, _clipX1, _clipY1;
        // Last glyph resolved. Runs of one character are the norm across terrain, so
        // this skips most of the dictionary lookups.
        private char _lastCh;
        private Cell _lastCell;
        private bool _hasLast;

        internal Batch(View view)
        {
            _buffer = AppHost.App.Driver?.GetOutputBuffer();
            _contents = _buffer?.Contents;
            var origin = view.ViewportToScreen(new System.Drawing.Point(0, 0));
            _ox = origin.X; _oy = origin.Y;

            int rows = _contents?.GetLength(0) ?? 0;
            int cols = _contents?.GetLength(1) ?? 0;
            (_clipX0, _clipY0, _clipX1, _clipY1) = ClipBounds(_buffer, cols, rows);
        }

        public void Put(int vx, int vy, char ch, Attribute attr)
        {
            if (_contents is null) return;
            int sy = _oy + vy, sx = _ox + vx;
            if (sy < _clipY0 || sy >= _clipY1 || sx < _clipX0 || sx >= _clipX1) return;

            Cell cell;
            if (_hasLast && ch == _lastCh)
            {
                cell = _lastCell;
            }
            else
            {
                cell = CellFor(ch);
                _lastCh = ch; _lastCell = cell; _hasLast = true;
            }
            cell.Attribute = attr;

            ref Cell existing = ref _contents[sy, sx];
            if (existing.Attribute == cell.Attribute
                && string.Equals(existing.Grapheme, cell.Grapheme, StringComparison.Ordinal))
            {
                if (Counting) CellsSkipped++;
                return;
            }

            existing = cell;
            _buffer!.DirtyLines[sy] = true;
            if (Counting) CellsWritten++;
        }
    }

    public static Batch Begin(View view) => new Batch(view);

    // Write a single character to the terminal at view-relative (x, y).
    public static void PutCell(View view, int x, int y, char ch, Color fg, Color bg)
        => PutCell(view, x, y, ch, Attr(fg, bg));

    // Write a single character using a pre-built attribute.
    public static void PutCell(View view, int x, int y, char ch, Attribute attr)
    {
        var buffer = AppHost.App.Driver?.GetOutputBuffer();
        var contents = buffer?.Contents;
        if (contents is null) return;

        var screen = view.ViewportToScreen(new System.Drawing.Point(x, y));
        if (screen.Y < 0 || screen.Y >= contents.GetLength(0)) return;
        if (screen.X < 0 || screen.X >= contents.GetLength(1)) return;
        // A null Clip means unclipped — the driver defaults it to the full screen.
        var clip = buffer!.Clip;
        if (clip is not null && !clip.Contains(screen.X, screen.Y)) return;

        Cell cell = CellFor(ch);
        cell.Attribute = attr;

        // Only touch the cell if it actually changes. A dirty cell is sent to the
        // terminal on the next Refresh, which costs far more than this comparison —
        // leaving unchanged cells clean is what keeps idle frames from rewriting the
        // entire screen.
        ref Cell existing = ref contents[screen.Y, screen.X];
        if (existing.Attribute == cell.Attribute
            && string.Equals(existing.Grapheme, cell.Grapheme, StringComparison.Ordinal))
        {
            if (Counting) CellsSkipped++;
            return;
        }

        existing = cell;
        buffer.DirtyLines[screen.Y] = true;
        if (Counting) CellsWritten++;
    }
}
