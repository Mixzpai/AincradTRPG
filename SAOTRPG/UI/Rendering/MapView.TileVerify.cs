using SAOTRPG.UI.Helpers;
using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.UI.Rendering;

// --verify-tiles: compares the incrementally-patched tile layer against a full recompute, every
// frame, cell by cell.
//
// The incremental layer is correct only if every input the resolve reads is either sampled in
// TileKey, marked as point damage, or escalated to a full recompute. That is a claim about
// code that is spread across the map, the lighting system and a dozen MapView mutators, and it
// cannot be established by reading. This mode keeps the incremental result aside, recomputes
// the frame the slow way, diffs the two, and then hands the *full* result to the renderer so
// play stays correct while the comparison runs.
//
// Deliberately slower than having no cache at all — it does both passes plus a compare.
public partial class MapView
{
    private Gfx.Glyph[]? _verifyBuf;
    private TileKey[]? _verifyKey;

    // What the last tile-layer pass actually did, for the mismatch report.
    private int _verifyDx, _verifyDy;
    private bool _verifyScanRan;

    // Mismatch logging is capped per frame: a systematic break would otherwise write a line per
    // cell every frame and drown the log the failure has to be read from.
    private const int VerifyLogLimit = 12;
    private const int VerifySummaryEveryFrames = 120;

    private static int s_verifyFrames, s_verifyBadFrames, s_verifyScans, s_verifyDeferred;
    private bool[]? _verifyDeferMask;
    private static long s_verifyMismatches;

    // Read-only counters for the verification probes, which drive the real view and read what
    // the verifier recorded rather than reimplementing the comparison. VerifyScansRan says whether
    // the damage tracker was exercised at all: a frame that took the full-recompute path compares
    // nothing.
    public static int VerifyFramesChecked => s_verifyFrames;
    public static int VerifyBadFrames => s_verifyBadFrames;
    public static int VerifyScansRan => s_verifyScans;
    // Cells the patch held back for a later frame (a sky step, see SubtleLight): each must be
    // within a sky step of the truth, or it counts as a mismatch.
    public static int VerifyCellsDeferred => s_verifyDeferred;

    private void VerifyTileLayer(int w, int h)
    {
        int n = w * h;
        if (_verifyBuf == null || _verifyBuf.Length != n) _verifyBuf = new Gfx.Glyph[n];
        if (_verifyKey == null || _verifyKey.Length != n) _verifyKey = new TileKey[n];

        var incremental = _verifyBuf;
        var incrementalKeys = _verifyKey;
        Array.Copy(_tileBuf!, incremental, n);
        Array.Copy(_tileKey!, incrementalKeys, n);
        if (_verifyDeferMask == null || _verifyDeferMask.Length != n) _verifyDeferMask = new bool[n];
        var deferredMask = _verifyDeferMask;
        Array.Copy(_tileDeferMask!, deferredMask, n);
        int incTimeVarying = _timeVaryingCount;

        // Authoritative: overwrites _tileBuf and _tileKey, so a mismatch is corrected on screen
        // in the same frame it is reported.
        RunFullTileLoop(w, h);

        var truth = _tileBuf!;
        int mismatches = 0;
        int offX = EffOffsetX, offY = EffOffsetY;
        for (int i = 0; i < n; i++)
        {
            if (CellsEqual(in incremental[i], in truth[i])) continue;
            if (deferredMask[i] && CellsClose(in incremental[i], in truth[i])) { s_verifyDeferred++; continue; }
            mismatches++;
            if (mismatches > VerifyLogLimit) continue;

            int vx = i % w, vy = i / w;
            DebugLogger.LogGame("VERIFY",
                $"tile mismatch view({vx},{vy}) map({vx + offX},{vy + offY}) " +
                $"incremental={Describe(in incremental[i])} full={Describe(in truth[i])} " +
                $"keyBefore={Describe(in incrementalKeys[i])} keyAfter={Describe(in _tileKey![i])}");
        }

        s_verifyFrames++;
        if (_verifyScanRan) s_verifyScans++;
        if (mismatches > 0)
        {
            s_verifyBadFrames++;
            s_verifyMismatches += mismatches;
            DebugLogger.LogGame("VERIFY",
                $"FRAME FAILED — {mismatches}/{n} cells wrong (viewport {w}x{h}, " +
                $"timeVarying incremental {incTimeVarying} full {_timeVaryingCount}, " +
                $"camera {offX},{offY}, scroll {_verifyDx},{_verifyDy}, scanRan {_verifyScanRan}, " +
                $"turn {SAOTRPG.Map.Lighting.DayNightCycle.CurrentTurn})");
        }

        if (s_verifyFrames % VerifySummaryEveryFrames == 0)
        {
            DebugLogger.LogGame("VERIFY",
                $"{s_verifyFrames} frames checked | {s_verifyBadFrames} bad | " +
                $"{s_verifyMismatches} mismatching cells");
        }
    }

    private static bool CellsEqual(in Gfx.Glyph a, in Gfx.Glyph b)
        => a.Ch == b.Ch && a.Attr == b.Attr;

    // A held-back cell: the same glyph, and colours no further from the truth than a sky step
    // (SubtleLight, with room for the render's own scaling).
    private static bool CellsClose(in Gfx.Glyph a, in Gfx.Glyph b)
    {
        static bool Near(Color p, Color q) =>
            Math.Abs(p.R - q.R) <= 24 && Math.Abs(p.G - q.G) <= 24 && Math.Abs(p.B - q.B) <= 24;
        return a.Ch == b.Ch && Near(a.Attr.Foreground, b.Attr.Foreground)
               && Near(a.Attr.Background, b.Attr.Background);
    }

    // Every sampled input appears here, or a mismatch report points away from its own cause.
    private static string Describe(in TileKey k)
        => $"{{flags {k.Flags} state {k.State} type {k.Type} fade {k.Fade} vig {k.Vignette} " +
           $"edge {k.ViewEdge} light {k.Light.R:F0}/{k.Light.G:F0}/{k.Light.B:F0}}}";

    private static string Describe(in Gfx.Glyph g)
        => $"['{g.Ch}' fg {g.Attr.Foreground} bg {g.Attr.Background}]";
}
