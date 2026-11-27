using System.Diagnostics;

namespace SAOTRPG.Systems.Diagnostics;

// Lightweight named-bucket timer. Compiled to no-ops when PROFILING is off.
// Runtime-gated by Profiler.Enabled (default false) so production builds carry
// zero cost — Begin returns default(Scope), no Stopwatch/dictionary touches.
// Shift+F10 toggles, Shift+F12 dumps to file, Shift+F11 resets.
public static class Profiler
{
#if PROFILING
    public static bool Enabled { get; set; } = false;

    // Bytes are the scope's own thread's allocation, inclusive of nested scopes, like the ticks;
    // SelfBytes leaves out what nested scopes allocated, which is what names the call site.
    private static readonly Dictionary<string, (long TotalTicks, int Count, long Max, long Bytes, long SelfBytes)> _buckets = new();

    // What each open scope's nested scopes allocated, innermost last, per thread.
    [ThreadStatic] private static long[]? _childBytes;
    [ThreadStatic] private static int _depth;

    // Floor generation opens a scope per pass and may run on a worker thread while the UI thread
    // is timing its own frame, so every bucket write is gated. Only reached when Enabled, which is
    // off by default, so the ordinary build pays nothing for it.
    private static readonly object BucketGate = new();

    private static void Accumulate(string name, long value, long bytes, long selfBytes)
    {
        lock (BucketGate)
        {
            if (!_buckets.TryGetValue(name, out var b)) b = default;
            _buckets[name] = (b.TotalTicks + value, b.Count + 1, Math.Max(b.Max, value), b.Bytes + bytes, b.SelfBytes + selfBytes);
        }
    }

    private static IEnumerable<string> FormatLines()
    {
        yield return $"=== Profiler dump {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===";
        // Snapshot under the gate: a dump taken while a floor is generating would otherwise
        // enumerate a dictionary another thread is writing.
        List<KeyValuePair<string, (long TotalTicks, int Count, long Max, long Bytes, long SelfBytes)>> snapshot;
        lock (BucketGate) snapshot = _buckets.ToList();
        foreach (var kv in snapshot.OrderByDescending(kv => kv.Value.TotalTicks))
        {
            var name = kv.Key;
            var b = kv.Value;
            double avgMs = b.TotalTicks * 1000.0 / Stopwatch.Frequency / Math.Max(1, b.Count);
            double maxMs = b.Max * 1000.0 / Stopwatch.Frequency;
            double totalMs = b.TotalTicks * 1000.0 / Stopwatch.Frequency;
            yield return $"{name,-32} {b.Count,6}x avg={avgMs:F2}ms max={maxMs:F2}ms total={totalMs:F1}ms"
                         + $" alloc={b.Bytes / 1024.0 / Math.Max(1, b.Count):F1}KB/call"
                         + $" self={b.SelfBytes / 1024.0 / Math.Max(1, b.Count):F2}KB/call";
        }
        yield return "=== end ===";
    }

    private static string GetDefaultDumpPath()
    {
        string baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AincradTRPG",
            "profiler");
        // Milliseconds included so two dumps in the same second (dump, reset, dump) resolve to
        // different paths rather than the second overwriting the first.
        return Path.Combine(baseDir, $"profiler_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");
    }

    // Wall-clock ticks spent inside a nested modal run loop, accumulated process-wide.
    //
    // A scope that stays open across a dialog otherwise measures the player's reading time as
    // compute time — and Terminal.Gui keeps firing Iteration inside a nested Run, so the
    // main-loop sampler shows nothing unusual and the inflated figure looks credible.
    // Scopes subtract whatever accumulated here during their lifetime.
    private static long _excludedTicks;
    private static long _exclusionStart;
    private static int _exclusionDepth;

    // The same for the garbage a modal allocates (its construction and every frame it draws),
    // on the thread that runs it.
    private static long _excludedBytes;
    private static long _exclusionBytesStart;

    // Bytes allocated inside modals since startup. Read by the verification probes.
    public static long ExcludedBytes => _excludedBytes;

    // Bracketed by DialogHelper.RunModal around AppHost.App.Run. Depth-counted so a dialog
    // opened from another dialog does not end the exclusion when only the inner one closes.
    public static void BeginExclusion()
    {
        if (_exclusionDepth++ != 0) return;
        _exclusionStart = Stopwatch.GetTimestamp();
        _exclusionBytesStart = GC.GetAllocatedBytesForCurrentThread();
    }

    public static void EndExclusion()
    {
        if (_exclusionDepth == 0) return;
        if (--_exclusionDepth != 0) return;
        _excludedTicks += Stopwatch.GetTimestamp() - _exclusionStart;
        _excludedBytes += GC.GetAllocatedBytesForCurrentThread() - _exclusionBytesStart;
    }

    public readonly struct Scope : IDisposable
    {
        private readonly string? _name;
        private readonly long _start;
        private readonly long _excludedAtStart;
        private readonly long _bytesAtStart;
        private readonly long _excludedBytesAtStart;
        private readonly int _level;

        public Scope(string name)
        {
            _name = name;
            _start = Stopwatch.GetTimestamp();
            _excludedAtStart = _excludedTicks;
            _excludedBytesAtStart = _excludedBytes;
            long[] child = _childBytes ??= new long[64];
            _level = _depth;
            if (_depth < child.Length) child[_depth] = 0;
            _depth++;
            _bytesAtStart = GC.GetAllocatedBytesForCurrentThread();
        }

        public void Dispose()
        {
            // Default Scope (returned when Enabled=false) has _name=null and is a no-op.
            if (_name == null) return;
            // A scope that opened *inside* a modal over-subtracts and clamps to zero, which is
            // the conservative direction: better to under-report than to invent compute time.
            long t = Stopwatch.GetTimestamp() - _start - (_excludedTicks - _excludedAtStart);
            if (t < 0) t = 0;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - _bytesAtStart - (_excludedBytes - _excludedBytesAtStart);
            // A scope disposed out of order (one that outlives a nested one) resets the stack to
            // its own level, so the counts stay sane rather than drift.
            long[] child = _childBytes!;
            _depth = _level;
            long nested = _level < child.Length ? child[_level] : 0;
            if (_level > 0 && _level - 1 < child.Length) child[_level - 1] += bytes;
            Accumulate(_name, t, bytes, bytes - nested);
        }
    }

    public static Scope Begin(string name) => Enabled && !_offThisThread ? new Scope(name) : default;

    // A worker's scopes would read as the main thread's time (the light cache's worker switches
    // them off for itself).
    [ThreadStatic] private static bool _offThisThread;
    public static bool OffThisThread { get => _offThisThread; set => _offThisThread = value; }

    // Synthetic counter — ticks field carries the count value.
    public static void RecordCount(string name, int count)
    {
        if (!Enabled) return;
        Accumulate(name, count, 0, 0);
    }

    // Pre-accumulated tick total — for sub-bucket timing inside hot loops where
    // a per-iteration Scope would dominate. Caller adds Stopwatch.GetTimestamp() deltas.
    public static void RecordRaw(string name, long ticks)
    {
        if (!Enabled) return;
        Accumulate(name, ticks, 0, 0);
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

    public static void Reset() { lock (BucketGate) _buckets.Clear(); }
#else
    public static bool Enabled { get; set; } = false;
    public readonly struct Scope : IDisposable
    {
        public void Dispose() { }
    }
    public static Scope Begin(string name) => default;
    public static bool OffThisThread { get; set; }
    public static void RecordCount(string name, int count) { }
    public static void RecordRaw(string name, long ticks) { }
    public static string DumpToFile(string? customPath = null) => string.Empty;
    public static void Reset() { }
    public static void BeginExclusion() { }
    public static void EndExclusion() { }
    public static long ExcludedBytes => 0;
#endif
}
