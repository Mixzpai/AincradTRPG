using System.Diagnostics;
using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.Systems;

// Canonical real-time wall-clock for animations. Single global tick; replaces per-MapView TickFrameClock.
// Pause-on-modal: animations freeze while a Dialog/overlay is on top of the main Toplevel.
public static class FrameClock
{
    // Stopwatch ticks (high-res, monotonic) of the previous Tick() call. 0 = never ticked.
    private static long _lastTimestamp;
    // Total wall-clock ms accumulated. Excludes paused intervals.
    private static long _elapsedMs;
    // Outstanding Pause() calls. A counter rather than a flag so a dialog opened from another
    // dialog does not unpause the clock when only the inner one closes.
    private static int _pauseDepth;

    // Total wall-clock ms accumulated since program start, excluding paused intervals.
    public static long ElapsedMs => _elapsedMs;

    // Clock for ambient, continuously-looping visuals — flickering tiles, water flow, rain,
    // shrine sparkle. Pinned by --freeze-anim so a perf run can separate timed animation from
    // other sources of per-frame cell churn, and by the reduce-motion setting.
    //
    // Event-scoped timers (toasts, banners, effect lifetimes) must keep reading ElapsedMs:
    // freezing those would stop them ever expiring, which is the failure the paused-clock bug
    // already produced once.
    public static long AmbientMs => Motion.Animate ? _elapsedMs : 0L;

    // True while an explicit Pause() is held OR something is stacked above the root session.
    private static bool IsPaused => _pauseDepth > 0 || ModalOnTop();

    // Call once per render frame. Returns dtMs since last call, clamped to 200ms.
    // Returns 0 while paused; resets baseline so the next unpaused call doesn't see the pause as one huge dt.
    public static int Tick()
    {
        long now = Stopwatch.GetTimestamp();
        if (_lastTimestamp == 0) { _lastTimestamp = now; return 16; }
        if (IsPaused) { _lastTimestamp = now; return 0; }
        long deltaTicks = now - _lastTimestamp;
        _lastTimestamp = now;
        long dtMs = deltaTicks * 1000 / Stopwatch.Frequency;
        if (dtMs < 0) dtMs = 0;
        if (dtMs > 200) dtMs = 200;
        _elapsedMs += dtMs;
        return (int)dtMs;
    }

    // Explicit pause hooks (DialogHelper.RunModal). Belt-and-suspenders alongside ModalOnTop().
    public static void Pause() => _pauseDepth++;
    public static void Resume() { if (_pauseDepth > 0) _pauseDepth--; }

    // True when a dialog or overlay sits above the root session.
    //
    // Depth, not IRunnable.IsModal: Terminal.Gui sets IsModal on EVERY runnable it pushes —
    // including the main window at startup — and restores it on the runnable beneath a closing
    // dialog. Testing it therefore reports "modal" during ordinary gameplay, which pinned this
    // clock paused permanently: Tick returned 0 forever, so ElapsedMs never advanced, every
    // wall-clock animation froze, and every timer that decrements by dt stopped expiring.
    private static bool ModalOnTop()
    {
        int depth = AppHost.App.SessionStack?.Count ?? 0;
        return depth > 1;
    }
}
