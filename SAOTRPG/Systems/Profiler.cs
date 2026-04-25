using System.Diagnostics;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Lightweight named-bucket timer. Compiled to no-ops when PROFILING is off.
// Use Profiler.Begin("Name") in a using-statement; Shift+F12 dumps, Shift+F11 resets.
public static class Profiler
{
#if PROFILING
    private static readonly Dictionary<string, (long TotalTicks, int Count, long Max)> _buckets = new();

    public readonly struct Scope : IDisposable
    {
        private readonly string _name;
        private readonly long _start;
        public Scope(string name) { _name = name; _start = Stopwatch.GetTimestamp(); }
        public void Dispose()
        {
            long t = Stopwatch.GetTimestamp() - _start;
            if (!_buckets.TryGetValue(_name, out var b)) b = default;
            _buckets[_name] = (b.TotalTicks + t, b.Count + 1, Math.Max(b.Max, t));
        }
    }

    public static Scope Begin(string name) => new(name);

    public static void Record(string name, Stopwatch sw)
    {
        sw.Stop();
        long t = sw.ElapsedTicks;
        if (!_buckets.TryGetValue(name, out var b)) b = default;
        _buckets[name] = (b.TotalTicks + t, b.Count + 1, Math.Max(b.Max, t));
    }

    // Synthetic counter — ticks field carries the count value.
    public static void RecordCount(string name, int count)
    {
        if (!_buckets.TryGetValue(name, out var b)) b = default;
        _buckets[name] = (b.TotalTicks + count, b.Count + 1, Math.Max(b.Max, count));
    }

    public static void Dump(IGameLog log)
    {
        log.Log("=== Profiler dump ===");
        foreach (var kv in _buckets.OrderByDescending(kv => kv.Value.TotalTicks))
        {
            var name = kv.Key;
            var b = kv.Value;
            double avgMs = b.TotalTicks * 1000.0 / Stopwatch.Frequency / Math.Max(1, b.Count);
            double maxMs = b.Max * 1000.0 / Stopwatch.Frequency;
            double totalMs = b.TotalTicks * 1000.0 / Stopwatch.Frequency;
            log.Log($"{name,-32} {b.Count,6}x avg={avgMs:F2}ms max={maxMs:F2}ms total={totalMs:F1}ms");
        }
        log.Log("=== end ===");
    }

    public static void Reset() => _buckets.Clear();
#else
    public readonly struct Scope : IDisposable
    {
        public void Dispose() { }
    }
    public static Scope Begin(string name) => default;
    public static void Record(string name, Stopwatch sw) { }
    public static void RecordCount(string name, int count) { }
    public static void Dump(IGameLog log) { }
    public static void Reset() { }
#endif
}
