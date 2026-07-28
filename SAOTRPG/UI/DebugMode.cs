namespace SAOTRPG.UI;

// Dev-only mode via --debug flag. When on: Debug difficulty tier is appended to DifficultyData.GetTiers,
// and DebugLogger starts writing debug.log.
public static class DebugMode
{
    // True when the game was launched with --debug.
    public static bool IsEnabled { get; private set; }

    // Activate debug mode. Called once during startup when --debug is detected.
    public static void Enable() => IsEnabled = true;

    // Diagnostic: pins every wall-clock-driven visual (torch flicker, emissive pulse,
    // animated tile phase) to a fixed frame. Nothing else changes, so a run with this
    // on isolates how much of the per-frame cell churn comes from timed animation
    // versus something that varies for another reason. Launch with --freeze-anim.
    public static bool FreezeAnimations { get; private set; }

    public static void EnableFreezeAnimations() => FreezeAnimations = true;

    // Render instrumentation: the once-a-second PERF line (loop rate, draw phase split,
    // cell/row/escape volume, GC, CPU share) plus the per-cell and per-draw counters that
    // feed it. Launch with --perf.
    //
    // Off by default and must stay free when off: consumers latch this into a static
    // readonly field at type-init so the JIT can fold the check out of per-cell paths.
    // It must therefore be set before any of them is first touched — see Program.Main,
    // which parses every flag ahead of AppHost.Start.
    //
    // This gates only the measurement. Gfx.FillDirtyRows, the GameWindow.DrawComplete hook
    // that calls it, and the DirtyLines reset are render fixes, not diagnostics, and run
    // unconditionally.
    public static bool PerfSampling { get; private set; }

    public static void EnablePerfSampling() => PerfSampling = true;
}
