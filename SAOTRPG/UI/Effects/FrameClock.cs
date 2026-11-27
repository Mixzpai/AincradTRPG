using System.Diagnostics;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Effects;

// The real-time wall clock every animation reads: one global tick shared by all views.
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
    // shrine sparkle. Pinned to 0 by --freeze-anim and by the reduce-motion setting.
    //
    // Event-scoped timers (toasts, banners, effect lifetimes) must keep reading ElapsedMs:
    // freezing those would stop them ever expiring.
    public static long AmbientMs => Motion.Animate ? _elapsedMs : 0L;

    // True while an explicit Pause() is held or something is stacked above the root session.
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

    // Explicit pause hooks, called by DialogHelper.RunModal in addition to the ModalOnTop() test.
    public static void Pause() => _pauseDepth++;
    // Drops the baseline too: Tick is not called beneath a dialog, so its paused branch never
    // resets it, and the first frame after would otherwise see the whole dialog as one dt.
    public static void Resume()
    {
        if (_pauseDepth > 0 && --_pauseDepth == 0) _lastTimestamp = 0;
    }

    // True when a dialog or overlay sits above the root session.
    //
    // Depth, not IRunnable.IsModal: Terminal.Gui sets IsModal on every runnable it pushes,
    // including the main window, and restores it on the runnable beneath a closing dialog, so
    // IsModal reads true during ordinary gameplay and would hold this clock paused forever.
    private static bool ModalOnTop()
    {
        int depth = AppHost.App.SessionStack?.Count ?? 0;
        return depth > 1;
    }
}
