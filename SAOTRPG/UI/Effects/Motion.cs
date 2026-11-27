using SAOTRPG.Systems.Persistence;

namespace SAOTRPG.UI.Effects;

// One answer to "should this move?", so the reduce-motion setting cannot be half-applied.
//
// Two separate reasons the answer can be no:
//   - The player asked for stillness (UserSettings.ReduceMotion) — a comfort and low-end-terminal
//     setting, saved with the rest.
//   - --freeze-anim, a session-only debug flag that pins wall-clock visuals.
//
// This must not touch event lifetimes. Toast TTLs, banner fades and effect durations read
// FrameClock.ElapsedMs, and stopping that would stop them ever expiring. Reduce-motion makes those
// things appear and disappear without an animated transition; it never leaves them on screen.
public static class Motion
{
    // False when nothing should animate on its own.
    public static bool Animate =>
        !UserSettings.Current.ReduceMotion && !SAOTRPG.Systems.Diagnostics.DebugMode.FreezeAnimations;

    // A tween's duration, or 0 to jump straight to the target value.
    public static int TweenMs(int normal) => Animate ? normal : 0;
}
