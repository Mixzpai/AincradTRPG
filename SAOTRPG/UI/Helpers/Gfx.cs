using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Terminal.Gui rendering shim.
//
// Cell writes go straight into the driver's output buffer rather than through
// AddRune/AddStr. Those re-parse the grapheme cluster, call MakePrintable and
// GetColumns, and allocate ~900 bytes per cell; at map scale (~12k cells/frame)
// that dominates frame time and drives constant gen-0 collection. Assigning a
// prebuilt Cell is allocation-free and measured ~40x faster, while applying the
// same Clip test the driver performs internally.
//
// Upgrade-isolation point — renderer/backend migrations only touch this file.
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
    internal static readonly bool Counting = SAOTRPG.UI.DebugMode.PerfSampling;

    // Counts cells actually committed vs skipped as unchanged, so the per-frame
    // volume reaching the terminal can be told apart from draw-path overhead.
    internal static long CellsWritten, CellsSkipped;
    // Rows touched and cells actually handed to the driver after span filling —
    // the transmitted volume, which is what frame time tracks.
    internal static long RowsDirty, CellsTransmitted;
    // Attribute changes along the transmitted spans. The driver emits a colour escape
    // only when the attribute differs from the previous cell, so this is the count of
    // escape sequences per frame — ~40 bytes each for truecolor, versus ~1 byte for a
    // cell that continues the current colour.
    internal static long AttrRuns;

    internal static (long written, long skipped, long rows, long sent, long runs) DrainCounters()
    {
        var result = (CellsWritten, CellsSkipped, RowsDirty, CellsTransmitted, AttrRuns);
        CellsWritten = 0; CellsSkipped = 0; RowsDirty = 0; CellsTransmitted = 0; AttrRuns = 0;
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

        for (int vy = 0; vy < height; vy++)
        {
            int sy = origin.Y + vy;
            if (sy < 0 || sy >= rows || sy < clip.Y0 || sy >= clip.Y1) continue;

            int rowBase = vy * width;
            bool rowTouched = false;
            for (int vx = 0; vx < width; vx++)
            {
                int sx = origin.X + vx;
                if (sx < 0 || sx >= cols || sx < clip.X0 || sx >= clip.X1) continue;

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

    // Marks every cell of every dirty row across the whole buffer, from the row's leftmost dirty
    // cell to its rightmost.
    //
    // ── DO NOT "OPTIMISE" THIS BY SENDING FEWER CELLS. Tried twice; 5x and 60x slower. ──
    //
    // A CLEAN CELL INSIDE A DIRTY ROW COSTS A SYSCALL. A DIRTY ONE IS ALMOST FREE.
    //
    // Terminal.Gui's OutputBase.Write(IOutputBuffer) walks every column of every dirty row, and
    // its clean-cell branch calls SetCursorPositionImpl per clean cell — which in AnsiOutput emits
    // a cursor escape through its own WriteFile on the console handle. Dirty cells, by contrast,
    // accumulate into one buffer and leave in a single write. So the cheapest possible frame is one
    // where every dirty row is 100% dirty, and cost scales with the clean cells you leave *inside*
    // the span, not with the cells you send.
    //
    // Filling lo..hi is therefore optimal, not wasteful. Two measured attempts to send less:
    //   - bridge only gaps <=12 cells: payload -97% (28,756 -> ~914 cells), flush 19ms -> ~1,100ms
    //   - drop the window border:      rows 91 -> 1, flush 19ms -> ~105ms even at the title screen
    // Both lost precisely because they left clean cells inside dirty rows.
    //
    // This is also why the host window's border is LOAD-BEARING: it dirties column 0 and column
    // W-1 on every row, which pins lo/hi to the screen edges and guarantees zero clean-cell walks.
    // Do not remove it for performance reasons.
    //
    // There is no further win here without replacing the driver's write loop.
    //
    // Must run after every view has painted but before the driver flushes.
    public static void FillDirtyRows()
    {
        var buffer = AppHost.App.Driver?.GetOutputBuffer();
        var contents = buffer?.Contents;
        if (contents is null) return;

        int rows = contents.GetLength(0), cols = contents.GetLength(1);
        for (int row = 0; row < rows; row++)
        {
            if (!buffer!.DirtyLines[row]) continue;

            int lo = -1, hi = -1;
            for (int col = 0; col < cols; col++)
            {
                if (!contents[row, col].IsDirty) continue;
                if (lo < 0) lo = col;
                hi = col;
            }

            if (lo < 0) continue;

            for (int col = lo; col <= hi; col++) contents[row, col].IsDirty = true;

            if (!Counting) continue;

            // Accounting only. Kept as a second pass so the fill above stays a bare
            // store per cell — the attribute read and compare below exist purely to
            // count colour escapes and have no effect on what the driver transmits.
            RowsDirty++;
            CellsTransmitted += hi - lo + 1;
            Attribute? prev = null;
            for (int col = lo; col <= hi; col++)
            {
                Attribute? a = contents[row, col].Attribute;
                if (col == lo || a != prev) AttrRuns++;
                prev = a;
            }
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

        for (int vy = 0; vy < height; vy++)
        {
            int sy = origin.Y + vy;
            if (sy < 0 || sy >= rows) continue;

            int rowBase = vy * width;
            for (int vx = 0; vx < width; vx++)
            {
                int sx = origin.X + vx;
                if (sx < 0 || sx >= cols) continue;

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

        for (int vy = 0; vy < height; vy++)
        {
            int sy = origin.Y + vy;
            if (sy < 0 || sy >= rows || sy < clip.Y0 || sy >= clip.Y1) continue;

            int rowBase = vy * width;
            bool rowTouched = false;
            for (int vx = 0; vx < width; vx++)
            {
                int sx = origin.X + vx;
                if (sx < 0 || sx >= cols || sx < clip.X0 || sx >= clip.X1) continue;

                Cell cell = src[rowBase + vx];

                // A dirty row is re-emitted in full on Refresh, so replaying an
                // unchanged snapshot would rewrite the whole viewport every idle frame.
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
    // across a full-viewport tile loop.
    public class Batch
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

        // Only touch the cell if it actually changes. Marking a row dirty makes the
        // driver re-emit that whole row to the terminal on the next Refresh, which
        // costs far more than this comparison — leaving unchanged rows clean is what
        // keeps idle frames from rewriting the entire screen.
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
