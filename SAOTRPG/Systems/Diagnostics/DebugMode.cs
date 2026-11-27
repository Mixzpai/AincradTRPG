namespace SAOTRPG.Systems.Diagnostics;

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
    // This gates only the instrumentation. The DirtyLines reset is part of rendering, not a
    // diagnostic, and runs unconditionally.
    public static bool PerfSampling { get; private set; }

    public static void EnablePerfSampling() => PerfSampling = true;

    // Correctness harness for the damage-tracked tile layer. Every frame that paints tiles
    // renders the layer incrementally, then recomputes it in full, compares the two cell by
    // cell, logs any mismatch to debug.log, and keeps the full result so play stays correct
    // while verifying. Deliberately slower than having no cache at all. Launch with
    // --verify-tiles.
    public static bool VerifyTiles { get; private set; }

    public static void EnableVerifyTiles() => VerifyTiles = true;
    public static void DisableVerifyTiles() => VerifyTiles = false;   // verification probe only

    // Correctness harness for the cached ambient+shadow field. The sweep is skipped whenever
    // nothing it depends on has moved, which is most turns — and a missed dependency leaves
    // shadows silently stale, showing as a lighting field that belongs to an earlier sun or an
    // earlier map. This recomputes the field from scratch every turn, compares it against what
    // the cache served, logs any mismatch, and keeps the fresh result so play stays correct
    // while verifying. Slower than having no cache at all; correctness only. Launch with
    // --verify-light.
    public static bool VerifyLight { get; private set; }

    public static void EnableVerifyLight() => VerifyLight = true;

    // Boot straight onto Floor 1 with a fixed character, skipping title, difficulty and
    // character creation. Launch with --quickstart; --perf implies it, and --no-quickstart
    // opts back out.
    //
    // --perf implies it because a perf run almost always wants the map; the opt-out keeps
    // the pre-map screens reachable under --perf.
    public static bool QuickStart { get; private set; }

    public static void EnableQuickStart() => QuickStart = true;

    public static void DisableQuickStart() => QuickStart = false;

    // World seed for a quick-start run, from --seed <n>. Null mints a random one; pinning it
    // makes two perf runs comparable.
    public static int? QuickStartSeed { get; private set; }

    public static void SetQuickStartSeed(int seed) => QuickStartSeed = seed;
}
