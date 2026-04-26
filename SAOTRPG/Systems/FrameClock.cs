using System.Diagnostics;
using Terminal.Gui;

namespace SAOTRPG.Systems;

// Canonical real-time wall-clock for animations. Single global tick; replaces per-MapView TickFrameClock.
// Pause-on-modal: animations freeze while a Dialog/overlay is on top of the main Toplevel.
public static class FrameClock
{
    // Stopwatch ticks (high-res, monotonic) of the previous Tick() call. 0 = never ticked.
    private static long _lastTimestamp;
    // Total wall-clock ms accumulated. Excludes paused intervals.
    private static long _elapsedMs;
    // True while explicit Pause() was called and Resume() not yet seen.
    private static bool _explicitPause;

    // Total wall-clock ms accumulated since program start, excluding paused intervals.
    public static long ElapsedMs => _elapsedMs;

    // True while either explicit Pause() is held OR a modal Toplevel is on top.
    public static bool IsPaused => _explicitPause || ModalOnTop();

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
    public static void Pause() => _explicitPause = true;
    public static void Resume() => _explicitPause = false;

    // Detects "modal currently visible" via Terminal.Gui v2 Application state.
    // When Application.Run(modal) is active, Application.Top points at the modal Toplevel,
    // distinct from the main game window pushed first.
    private static bool ModalOnTop()
    {
        var top = Application.Top;
        if (top == null) return false;
        if (top.Modal) return true;
        return false;
    }
}
