namespace SAOTRPG.UI.Helpers;

// Full-screen host window for every screen.
//
// The framework clears a view's viewport before drawing it, and that clear runs
// FillRect over every cell: a string allocation and grapheme measurement per cell,
// then SetAttributeAndDirty marking the cell AND its row for retransmit. On a window
// spanning the terminal that means any redraw at all — one menu keypress, one label
// change — blanks and dirties the whole screen, so the driver re-sends every cell.
//
// Screens repaint their own content, and the areas between panels are static once
// drawn, so the routine clear is pure cost. It is cancelled here; screen swaps that
// genuinely need a wipe call RequestFullClear.
public class GameWindow : Window
{
    // Timestamp of the moment the view tree finished painting. The driver's flush to
    // the terminal runs between this and the application's draw-complete event, so the
    // gap isolates "time spent handing the frame to the terminal" from "time spent
    // drawing views" — the last unsplit segment of a frame. Diagnostic only.
    public static long ViewDrawCompleteTicks;

    private static readonly bool s_sampling = SAOTRPG.Systems.Diagnostics.DebugMode.PerfSampling;

    public GameWindow()
    {
        DrawComplete += (_, _) =>
        {
            // Every view has painted; the driver flush follows. Instrumentation only.
            if (!s_sampling) return;
            Gfx.CountTransmitted();
            ViewDrawCompleteTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        };
    }

    protected override bool OnClearingViewport() => true;

    // Terminal.Gui redraws the border whenever ANY subview redraws: BorderView blanks its cells and
    // the line canvas re-resolves them, ~480 KB of garbage a frame at 316x91. The border changes
    // only when the window itself is invalidated (a resize, a screen swap, a modal closing:
    // Runnable and DialogHelper.RunModal both SetNeedsDraw it), so a subview-only pass skips it.
    // DrawingSelf is the patched Terminal.Gui's (vendor/README.md).
    protected override bool OnDrawingAdornments() => !DrawingSelf;

    // Forces one real full-screen clear on the next draw. Call when replacing a
    // screen's contents, where leftover cells would otherwise survive in the gaps
    // between the new screen's views.
    public static void RequestFullClear() => AppHost.App.ClearScreenNextIteration = true;
}
