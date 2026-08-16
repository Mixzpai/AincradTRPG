using System.Diagnostics;
using System.IO;

namespace SAOTRPG.Systems;

// Lightweight named-bucket timer. Compiled to no-ops when PROFILING is off.
// Runtime-gated by Profiler.Enabled (default false) so production builds carry
// zero cost — Begin returns default(Scope), no Stopwatch/dictionary touches.
// Shift+F10 toggles, Shift+F12 dumps to file, Shift+F11 resets.
public static class Profiler
{
#if PROFILING
    public static bool Enabled { get; set; } = false;

    private static readonly Dictionary<string, (long TotalTicks, int Count, long Max)> _buckets = new();

    private static IEnumerable<string> FormatLines()
    {
        yield return $"=== Profiler dump {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===";
        foreach (var kv in _buckets.OrderByDescending(kv => kv.Value.TotalTicks))
        {
            var name = kv.Key;
            var b = kv.Value;
            double avgMs = b.TotalTicks * 1000.0 / Stopwatch.Frequency / Math.Max(1, b.Count);
            double maxMs = b.Max * 1000.0 / Stopwatch.Frequency;
            double totalMs = b.TotalTicks * 1000.0 / Stopwatch.Frequency;
            yield return $"{name,-32} {b.Count,6}x avg={avgMs:F2}ms max={maxMs:F2}ms total={totalMs:F1}ms";
        }
        yield return "=== end ===";
    }

    private static string GetDefaultDumpPath()
    {
        string baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AincradTRPG",
            "profiler");
        // Milliseconds included: two dumps in the same second used to resolve to one path, and
        // the second silently overwrote the first — easy to hit via dump, reset, dump.
        return Path.Combine(baseDir, $"profiler_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");
    }

    // Wall-clock ticks spent inside a nested modal run loop, accumulated process-wide.
    //
    // A scope that stays open across a dialog otherwise measures the player's reading time as
    // compute time — and Terminal.Gui keeps firing Iteration inside a nested Run, so the
    // main-loop sampler shows nothing unusual and the inflated figure looks entirely credible.
    // That combination produced a wrong diagnosis once already: a 663ms "player move" that no
    // frame ever stalled for. Scopes subtract whatever accumulated here during their lifetime.
    private static long _excludedTicks;
    private static long _exclusionStart;
    private static int _exclusionDepth;

    // Bracketed by DialogHelper.RunModal around AppHost.App.Run. Depth-counted so a dialog
    // opened from another dialog does not end the exclusion when only the inner one closes.
    public static void BeginExclusion()
    {
        if (_exclusionDepth++ == 0) _exclusionStart = Stopwatch.GetTimestamp();
    }

    public static void EndExclusion()
    {
        if (_exclusionDepth == 0) return;
        if (--_exclusionDepth == 0) _excludedTicks += Stopwatch.GetTimestamp() - _exclusionStart;
    }

    public readonly struct Scope : IDisposable
    {
        private readonly string? _name;
        private readonly long _start;
        private readonly long _excludedAtStart;

        public Scope(string name)
        {
            _name = name;
            _start = Stopwatch.GetTimestamp();
            _excludedAtStart = _excludedTicks;
        }

        public void Dispose()
        {
            // Default Scope (returned when Enabled=false) has _name=null and is a no-op.
            if (_name == null) return;
            // A scope that opened *inside* a modal over-subtracts and clamps to zero, which is
            // the conservative direction: better to under-report than to invent compute time.
            long t = Stopwatch.GetTimestamp() - _start - (_excludedTicks - _excludedAtStart);
            if (t < 0) t = 0;
            if (!_buckets.TryGetValue(_name, out var b)) b = default;
            _buckets[_name] = (b.TotalTicks + t, b.Count + 1, Math.Max(b.Max, t));
        }
    }

    public static Scope Begin(string name) => Enabled ? new Scope(name) : default;

    // Synthetic counter — ticks field carries the count value.
    public static void RecordCount(string name, int count)
    {
        if (!Enabled) return;
        if (!_buckets.TryGetValue(name, out var b)) b = default;
        _buckets[name] = (b.TotalTicks + count, b.Count + 1, Math.Max(b.Max, count));
    }

    // Pre-accumulated tick total — for sub-bucket timing inside hot loops where
    // a per-iteration Scope would dominate. Caller adds Stopwatch.GetTimestamp() deltas.
    public static void RecordRaw(string name, long ticks)
    {
        if (!Enabled) return;
        if (!_buckets.TryGetValue(name, out var b)) b = default;
        _buckets[name] = (b.TotalTicks + ticks, b.Count + 1, Math.Max(b.Max, ticks));
    }

    // Writes a timestamped file under %LocalAppData%/AincradTRPG/profiler/.
    // Returns the full path written so callers can echo it to the user.
    public static string DumpToFile(string? customPath = null)
    {
        string path = customPath ?? GetDefaultDumpPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, FormatLines());
        return path;
    }

    public static void Reset() => _buckets.Clear();
#else
    public static bool Enabled { get; set; } = false;
    public readonly struct Scope : IDisposable
    {
        public void Dispose() { }
    }
    public static Scope Begin(string name) => default;
    public static void RecordCount(string name, int count) { }
    public static void RecordRaw(string name, long ticks) { }
    public static string DumpToFile(string? customPath = null) => string.Empty;
    public static void Reset() { }
    public static void BeginExclusion() { }
    public static void EndExclusion() { }
#endif
}
