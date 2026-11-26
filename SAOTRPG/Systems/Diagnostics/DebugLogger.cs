using System.Diagnostics;

namespace SAOTRPG.Systems.Diagnostics;

// File-based debug log. Timestamped lines → debug.log with category tags (SESSION/INPUT/SCREEN/COMBAT/SYSTEM/LOG/STATE/PERF/ERROR).
// Lifecycle: Init() at startup, Shutdown() on exit; no-op until Init(). Terminal.Gui-free: the key
// trace lives in AppHost.AttachKeyLogger.
public static class DebugLogger
{
    private static StreamWriter? _writer;
    private static bool _enabled;
    private static string? _logFilePath;

    // StreamWriter is not thread-safe, and floor generation logs a line per pass from a worker
    // thread while the UI thread is logging keystrokes. Unsynchronized that interleaves two
    // partial lines at best and faults at worst. The lock is uncontended on the ordinary path.
    private static readonly object Gate = new();

    // ── Session ──
    // Creates/overwrites debug.log in cwd; falls back to timestamped filename on file-lock.
    public static void Init(string path = "debug.log")
    {
        try
        {
            _logFilePath = Path.GetFullPath(path);
            _writer = new StreamWriter(path, append: false) { AutoFlush = true };
        }
        catch (IOException)
        {
            // File locked by another instance — use a timestamped fallback.
            path = $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            _logFilePath = Path.GetFullPath(path);
            _writer = new StreamWriter(path, append: false) { AutoFlush = true };
        }
        _enabled = true;
        Write("SESSION", "Debug logger initialized");
        Write("SESSION", $"Log file: {_logFilePath}");
    }

    // Flush + close log file; call on app exit.
    public static void Shutdown()
    {
        if (!_enabled) return;
        Write("SESSION", "Debug logger shutdown");
        _writer?.Dispose();
        _writer = null;
        _enabled = false;
    }

    public static bool IsEnabled => _enabled;

    // ── Game Output ──
    public static void LogGame(string category, string message)
    {
        if (_enabled) Write(category, message);
    }

    public static void LogScreen(string screenName)
    {
        if (_enabled) Write("SCREEN", $"Navigated to {screenName}");
    }

    // ── State Snapshots ── e.g. [STATE] Player "Name" | LVL:5 HP:150/200 ATK:42
    public static void LogState(string label, string snapshot)
    {
        if (_enabled) Write("STATE", $"{label} | {snapshot}");
    }

    // ── Performance Timing ── Usage: var sw = StartTimer("Foo"); …; EndTimer("Foo", sw);
    public static Stopwatch StartTimer(string label)
    {
        var sw = Stopwatch.StartNew();
        if (_enabled) Write("PERF", $"[START] {label}");
        return sw;
    }

    // Logs elapsed ms: [PERF] [END] {label} — 12.34ms
    public static void EndTimer(string label, Stopwatch sw)
    {
        sw.Stop();
        if (_enabled) Write("PERF", $"[END] {label} — {sw.Elapsed.TotalMilliseconds:F2}ms");
    }

    // ── Errors ── Logs context + message + stack; inner exception on its own line.
    public static void LogError(string context, Exception ex)
    {
        if (!_enabled) return;
        // One lock for the whole record, so a stack trace cannot be split by another thread's line.
        lock (Gate)
        {
            WriteLocked("ERROR", $"[{context}] {ex.Message}");
            _writer?.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
                _writer?.WriteLine($"  Inner: {ex.InnerException.Message}");
        }
    }

    // ── Core ── Line format: [HH:mm:ss.fff] [TAG    ] message
    private static void Write(string tag, string message)
    {
        lock (Gate) WriteLocked(tag, message);
    }

    private static void WriteLocked(string tag, string message)
    {
        _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{tag,-7}] {message}");
    }
}
