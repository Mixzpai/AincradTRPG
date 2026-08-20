using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Owns the single Terminal.Gui application instance for the process.
// Terminal.Gui's instance-based IApplication model replaced the static Application
// facade; the game's static screen/dialog architecture reaches the running instance
// through here rather than threading it through every Show() signature.
public static class AppHost
{
    // Main-loop iteration ceiling. The loop drains the input queue once per
    // iteration and sleeps out the remainder of the budget, so this doubles as the
    // input-polling rate: the framework default of 25 leaves ~40ms of keypress
    // latency and lets OS key-repeat outpace the loop, which batches several
    // queued moves into one iteration. 60 keeps latency under a frame.
    private const ushort IterationsPerSecond = 60;

    private static IApplication? _app;

    // The running application. Throws if reached before Start().
    // A timeout that fires ONCE and cannot stack, said in code rather than in a comment.
    //
    // The standing rule is that an AddTimeout registered from an event handler needs a
    // re-entrancy guard, because each call stacks another concurrent callback — auto-explore
    // once ran seven explorers off seven keypresses. A genuinely one-shot registration needs no
    // guard, but "this one is fine" was recorded in the checker as a (file, line) pair, and an
    // unrelated edit two lines above moved the line and failed the suite. Comments cannot carry
    // it either: `invariants.py` strips them first, deliberately, so that prose cannot satisfy a
    // guard check. A named call is the one form that is both self-documenting and machine-visible.
    //
    // Use it only where the registration genuinely cannot happen twice concurrently.
    public static void AddOneShotTimeout(TimeSpan delay, Func<bool> callback) =>
        App.AddTimeout(delay, callback);

    public static IApplication App =>
        _app ?? throw new InvalidOperationException("AppHost.Start() must run before any UI work.");

    // True when this view belongs to a session that something else is stacked on top of —
    // i.e. a dialog is open above it.
    //
    // Terminal.Gui enumerates its session stack top-first and draws it in that order without
    // reversing, so the window beneath a dialog paints AFTER the dialog does. The framework
    // relies on lower views being clean to make that harmless, but this game's toasts, tweens
    // and HUD refresh keep the map marked dirty, so it repaints every frame and overwrites the
    // dialog — leaving it invisible but still alive and holding focus. Views beneath a dialog
    // consult this and skip painting while it holds.
    //
    // Identity against the top session is the test, not IRunnable.IsModal: every runnable
    // pushed becomes IsModal, including the main window, and the one below a closing dialog
    // has IsModal restored to true as it is popped.
    public static bool IsBeneathTopSession(View view)
    {
        View? top = _app?.TopRunnableView;
        if (top is null) return false;

        View root = view;
        while (root.SuperView is { } parent) root = parent;

        return !ReferenceEquals(root, top);
    }

    // Creates and initializes the instance. Call once, at startup.
    // driverName selects the Terminal.Gui backend ("windows" / "ansi" / "dotnet");
    // null takes the platform default, which on Windows is the ANSI escape-sequence
    // driver rather than the native Console API one.
    public static IApplication Start(string? driverName = null)
    {
        Application.MaximumIterationsPerSecond = IterationsPerSecond;
        _app = Application.Create().Init(driverName);
        AttachDirtyLineReset(_app);
        LogStartupEnvironment(_app);
        if (SAOTRPG.UI.DebugMode.PerfSampling) AttachLoopSampler(_app);
        return _app;
    }

    // ── Main-loop diagnostics ───────────────────────────────────────
    // Samples achieved iteration rate and time spent inside each iteration.
    // The loop drains input once per iteration, so a rate well below the
    // configured ceiling means input latency scales with it.
    private static readonly System.Diagnostics.Stopwatch s_clock = System.Diagnostics.Stopwatch.StartNew();
    private static long s_windowStartMs;
    private static int s_iterations;
    private static long s_iterStartTicks;
    private static double s_drawPhaseMs, s_drawMaxMs;
    // Frames that actually drew. Averaging the draw phase over every iteration would
    // understate it, since idle iterations never raise the draw-complete event.
    private static int s_drawFrames;
    // Drawn frames in which the map painted — the only ones the pre/map/post split applies to.
    private static int s_mapFrames;
    // Frame split: work before the map painted (input, ANSI requests, size poll, layout)
    // and work after it (remaining views plus the driver's flush to the terminal).
    private static double s_preDrawMs, s_postDrawMs;
    // Time between the view tree finishing and the frame reaching the terminal.
    private static double s_flushMs;
    private static double s_periodMaxMs;
    private static long s_prevIterTicks;
    private static int s_gen0At, s_gen2At;
    // Processor time consumed by this process. Compared against wall time it
    // separates "our code is slow" from "we are parked waiting on the terminal":
    // rendering the glyphs happens in the terminal emulator's process, not ours,
    // and a full write pipe blocks us without burning any CPU here.
    private static TimeSpan s_cpuAt = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;

    // Resets the driver's per-row dirty flags after every flush.
    //
    // Terminal.Gui clears each cell's IsDirty as it writes, but never resets the per-row
    // DirtyLines flags. Every row that has ever been drawn therefore stays dirty for the
    // life of the process, so each Refresh re-scans the full width of every such row and
    // emits a cursor move per clean cell. LayoutAndDrawComplete fires immediately after
    // Refresh, so clearing here keeps the next flush proportional to what actually changed.
    //
    // This is a render fix, not instrumentation — it subscribes unconditionally and must
    // never be folded back into the sampler below.
    private static void AttachDirtyLineReset(IApplication app)
    {
        app.LayoutAndDrawComplete += (_, _) =>
        {
            bool[]? dirtyLines = app.Driver?.GetOutputBuffer()?.DirtyLines;
            if (dirtyLines is { Length: > 0 }) Array.Clear(dirtyLines);
        };
    }

    // One-line startup breadcrumb: which backend, how many cells, which terminal emulator.
    // Unconditional — it costs a single log line and every render bug report starts here.
    // Which emulator is hosting us decides how the frame is rasterised; Windows Terminal
    // advertises itself via WT_SESSION, and without it we are almost certainly on conhost,
    // whose VT escape handling is emulated and CPU-bound.
    private static void LogStartupEnvironment(IApplication app)
    {
        string? wt = Environment.GetEnvironmentVariable("WT_SESSION");
        string host = wt is { Length: > 0 } ? "Windows Terminal" : "conhost (legacy)";

        SAOTRPG.UI.DebugLogger.LogGame("SYSTEM",
            $"Driver={app.Driver?.GetName() ?? "<null>"} screen={app.Screen.Width}x{app.Screen.Height} " +
            $"cells={app.Screen.Width * app.Screen.Height} maxIterations/s={IterationsPerSecond} " +
            $"host={host} legacyConsole={app.Driver?.IsLegacyConsole} " +
            $"perfSampling={SAOTRPG.UI.DebugMode.PerfSampling}");
    }

    private static void AttachLoopSampler(IApplication app)
    {
        // LayoutAndDraw runs late in the iteration and raises this on completion, so
        // Iteration -> LayoutAndDrawComplete covers input + size poll + the whole
        // view-tree draw. Whatever is left of the period sits outside that window.
        app.LayoutAndDrawComplete += (_, _) =>
        {
            if (s_iterStartTicks == 0) return;

            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            double total = (now - s_iterStartTicks) * toMs;
            s_drawPhaseMs += total;
            s_drawFrames++;
            if (total > s_drawMaxMs) s_drawMaxMs = total;

            // Bracket around the map's own paint. Only frames where the map actually painted
            // can be split into pre/map/post, and they are averaged over their own count:
            // folding map-less frames into post (and dividing by all drawn frames) made the
            // split incomparable between runs, which matters now that the frame cache and the
            // beneath-dialog guard make skipping common.
            long mapStart = SAOTRPG.UI.MapView.LastDrawStartTicks;
            long mapEnd = SAOTRPG.UI.MapView.LastDrawEndTicks;
            if (mapStart >= s_iterStartTicks && mapEnd >= mapStart)
            {
                s_preDrawMs += (mapStart - s_iterStartTicks) * toMs;
                s_postDrawMs += (now - mapEnd) * toMs;
                s_mapFrames++;
            }

            // Driver.Refresh runs between the view tree completing and this event.
            long vdc = GameWindow.ViewDrawCompleteTicks;
            if (vdc >= s_iterStartTicks && now >= vdc) s_flushMs += (now - vdc) * toMs;
        };

        app.Iteration += (_, _) =>
        {
            long tick = System.Diagnostics.Stopwatch.GetTimestamp();
            if (s_prevIterTicks != 0)
            {
                double gap = (tick - s_prevIterTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                if (gap > s_periodMaxMs) s_periodMaxMs = gap;
            }
            s_prevIterTicks = tick;
            s_iterStartTicks = tick;

            s_iterations++;
            long ms = s_clock.ElapsedMilliseconds;
            long span = ms - s_windowStartMs;
            if (span < 1000) return;

            // The loop sleeps out any unused budget, so a rate far below the ceiling
            // means each iteration's work is overrunning it — that period is the
            // input latency the player feels.
            int n = Math.Max(1, s_iterations);
            double period = (double)span / n;
            int gen0 = GC.CollectionCount(0), gen2 = GC.CollectionCount(2);
            var cells = Gfx.DrainCounters();

            // The per-iteration size poll queries the console before any drawing.
            // Timing one call here shows whether that query is a fixed blocking cost
            // sitting inside the measured draw window.
            long szTicks = s_clock.ElapsedTicks;
            _ = app.Driver?.GetOutput()?.GetSize();
            double sizeMs = (s_clock.ElapsedTicks - szTicks) * 1000.0
                            / System.Diagnostics.Stopwatch.Frequency;

            TimeSpan cpuNow = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            double cpuPct = (cpuNow - s_cpuAt).TotalMilliseconds / Math.Max(1, span) * 100.0;
            s_cpuAt = cpuNow;
            int d = Math.Max(1, s_drawFrames);
            // pre/map/post are per map-painting frame; flush is per drawn frame.
            int m = Math.Max(1, s_mapFrames);
            var tiles = SAOTRPG.UI.MapView.DrainTileLayerCounts();
            SAOTRPG.UI.DebugLogger.LogGame("PERF",
                $"loop {s_iterations} it/s (ceiling {IterationsPerSecond})" +
                $" | period avg {period:F1}ms worst {s_periodMaxMs:F0}ms" +
                $" | drewFrames {s_drawFrames}/{s_iterations}" +
                $" | draw avg {s_drawPhaseMs / d:F1}ms worst {s_drawMaxMs:F0}ms" +
                $" | mapFrames {s_mapFrames}/{s_drawFrames}" +
                $" | tileLoops {tiles.loops}/{tiles.paints}" +
                // Cells the tile layer had to resolve against cells it scanned, per frame that
                // painted tiles. This is the damage tracker's hit rate: during sustained
                // movement it should sit far below 1.
                $" | tileCells {tiles.resolved / Math.Max(1, tiles.paints)}" +
                $"/{tiles.scanned / Math.Max(1, tiles.paints)}" +
                $" | pre {s_preDrawMs / m:F1}ms map {SAOTRPG.UI.MapView.DrainDrawAvgMs():F1}ms post {s_postDrawMs / m:F1}ms" +
                $" (of which flush {s_flushMs / d:F1}ms)" +
                // Per DRAWN frame, matching flush. Dividing these by iterations instead made a
                // frame that transmits the whole screen look like it transmitted a few hundred
                // cells, and comparing that against a per-drawn-frame flush is what produced the
                // false conclusion that flush does not respond to payload.
                $" | cells w{cells.written / d} s{cells.skipped / d} sent {cells.sent / d} rows {cells.rows / d}" +
                $" | escapes {cells.runs / d} (~{(cells.runs * 40 + cells.sent) / d / 1024.0:F0}KB/frame)" +
                $" | clock {SAOTRPG.Systems.FrameClock.ElapsedMs}ms" +
                $" | sizeQuery {sizeMs:F1}ms | cpu {cpuPct:F0}% of wall" +
                $" | gc0 {gen0 - s_gen0At} gc2 {gen2 - s_gen2At}" +
                $" | screen {app.Screen.Width}x{app.Screen.Height}");

            s_windowStartMs = ms;
            s_iterations = 0;
            s_drawPhaseMs = 0; s_drawMaxMs = 0; s_drawFrames = 0; s_mapFrames = 0;
            s_preDrawMs = 0; s_postDrawMs = 0; s_flushMs = 0;
            s_periodMaxMs = 0;
            s_gen0At = gen0;
            s_gen2At = gen2;
        };
    }

    // Disposes the instance and clears the reference. Call once, at exit.
    public static void Stop()
    {
        _app?.Dispose();
        _app = null;
    }
}
