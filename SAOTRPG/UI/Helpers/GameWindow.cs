using Terminal.Gui;

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
    internal static long ViewDrawCompleteTicks;

    private static readonly bool s_sampling = SAOTRPG.UI.DebugMode.PerfSampling;

    public GameWindow()
    {
        DrawComplete += (_, _) =>
        {
            // Every view has painted; the driver flush follows. Close the gaps in touched
            // rows first so it writes one run per row instead of thousands of fragments.
            // This is the render fix — it runs unconditionally, and only the timestamp
            // below is instrumentation.
            Gfx.FillDirtyRows();
            if (s_sampling) ViewDrawCompleteTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        };
    }

    protected override bool OnClearingViewport() => true;

    // Forces one real full-screen clear on the next draw. Call when replacing a
    // screen's contents, where leftover cells would otherwise survive in the gaps
    // between the new screen's views.
    public static void RequestFullClear() => AppHost.App.ClearScreenNextIteration = true;
}
