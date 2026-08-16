using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// --verify-tiles: proves the incrementally-patched tile layer equals a full recompute, every
// frame, cell by cell.
//
// The incremental layer is correct only if every input the resolve reads is either sampled in
// TileKey, marked as point damage, or escalated to a full recompute. That is a claim about
// code that is spread across the map, the lighting system and a dozen MapView mutators, and it
// cannot be established by reading. This mode establishes it by measurement instead: it keeps
// the incremental result aside, recomputes the frame the slow way, diffs the two, and then
// hands the *full* result to the renderer so play stays correct while the check runs.
//
// Deliberately slower than having no cache at all — it does both passes plus a compare.
public partial class MapView
{
    private Gfx.Glyph[]? _verifyBuf;
    private TileKey[]? _verifyKey;

    // What the last tile-layer pass actually did, for the mismatch report.
    private int _verifyDx, _verifyDy;
    private bool _verifyScanRan;

    // Mismatch spam is capped per frame: a systematic break would otherwise write a line per
    // cell, which is 28,756 lines a frame and drowns the log the failure has to be read from.
    private const int VerifyLogLimit = 12;
    private const int VerifySummaryEveryFrames = 120;

    private static int s_verifyFrames, s_verifyBadFrames, s_verifyScans;
    private static long s_verifyMismatches;

    // Read-only seam for Tools/TileVerifyProbe: counters only, so the probe drives the real view
    // and reads what the verifier recorded rather than reimplementing any of the comparison.
    // VerifyScansRan is the one that matters as a precondition — a run in which every frame took
    // the full-recompute path would pass without ever exercising the damage tracker.
    public static int VerifyFramesChecked => s_verifyFrames;
    public static int VerifyBadFrames => s_verifyBadFrames;
    public static int VerifyScansRan => s_verifyScans;

    private void VerifyTileLayer(int w, int h)
    {
        int n = w * h;
        if (_verifyBuf == null || _verifyBuf.Length != n) _verifyBuf = new Gfx.Glyph[n];
        if (_verifyKey == null || _verifyKey.Length != n) _verifyKey = new TileKey[n];

        var incremental = _verifyBuf;
        var incrementalKeys = _verifyKey;
        Array.Copy(_tileBuf!, incremental, n);
        Array.Copy(_tileKey!, incrementalKeys, n);
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
                $"turn {Map.DayNightCycle.CurrentTurn})");
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

    // Every sampled input appears here, or a mismatch report points away from its own cause.
    private static string Describe(in TileKey k)
        => $"{{flags {k.Flags} state {k.State} type {k.Type} fade {k.Fade} vig {k.Vignette} " +
           $"light {k.Light.R:F0}/{k.Light.G:F0}/{k.Light.B:F0}}}";

    private static string Describe(in Gfx.Glyph g)
        => $"['{g.Ch}' fg {g.Attr.Foreground} bg {g.Attr.Background}]";
}
