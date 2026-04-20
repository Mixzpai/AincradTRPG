using System.Diagnostics;
using Terminal.Gui;

namespace SAOTRPG.UI;

// File-based debug logger. Timestamped lines into debug.log with category tags:
// SESSION/INPUT/SCREEN/COMBAT/SYSTEM/LOG/STATE/PERF/ERROR. Init() once at startup;
// AttachKeyLogger() after Application.Init(); Shutdown() on exit. No-op until Init() runs.
public static class DebugLogger
{
    private static StreamWriter? _writer;
    private static bool _enabled;
    private static string? _logFilePath;

    // Absolute log-file path, or null if logging disabled.
    public static string? LogFilePath => _logFilePath;

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

    // ── Input ── Hooks Terminal.Gui's global KeyDown; must run after Application.Init().
    public static void AttachKeyLogger()
    {
        if (!_enabled) return;
        Application.KeyDown += (s, e) => Write("INPUT", $"Key: {e}");
    }

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
        Write("ERROR", $"[{context}] {ex.Message}");
        _writer?.WriteLine(ex.StackTrace);
        if (ex.InnerException != null)
            _writer?.WriteLine($"  Inner: {ex.InnerException.Message}");
    }

    // ── Core ── Line format: [HH:mm:ss.fff] [TAG    ] message
    private static void Write(string tag, string message)
    {
        _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{tag,-7}] {message}");
    }
}
