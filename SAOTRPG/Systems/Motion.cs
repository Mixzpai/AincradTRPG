namespace SAOTRPG.Systems;

// One answer to "should this move?", so the reduce-motion setting cannot be half-applied.
//
// Two separate reasons the answer can be no, and they are deliberately different things:
//   - The player asked for stillness (UserSettings.ReduceMotion) — a comfort and low-end-terminal
//     setting, saved with the rest.
//   - --freeze-anim, which pins wall-clock visuals so a perf run can separate timed animation from
//     other per-frame cell churn. Session-only, and it was here first.
//
// WHAT THIS MUST NOT TOUCH: event lifetimes. Toast TTLs, banner fades and effect durations read
// FrameClock.ElapsedMs, and stopping that would stop them ever expiring — the exact failure the
// paused-clock bug produced once already. Reduce-motion makes those things appear and disappear
// without an animated transition; it never leaves them on screen forever.
public static class Motion
{
    // False when nothing should animate on its own.
    public static bool Animate =>
        !UserSettings.Current.ReduceMotion && !UI.DebugMode.FreezeAnimations;

    // A tween's duration, or 0 to jump straight to the target value.
    public static int TweenMs(int normal) => Animate ? normal : 0;
}
