using System.Diagnostics;
using Terminal.Gui;

namespace SAOTRPG.UI;

// File-based debug logger for Aincrad TRPG.
// Writes timestamped entries to debug.log with category tags:
//   [SESSION]  — app start/stop
//   [INPUT]    — every keystroke (via AttachKeyLogger)
//   [SCREEN]   — screen transitions + load times
//   [COMBAT]   — damage, kills, XP
//   [SYSTEM]   — level ups, equips
//   [LOG]      — general game messages
//   [STATE]    — player/entity stat snapshots
//   [PERF]     — timed operations (screen loads, combat rounds)
//   [ERROR]    — exceptions with full stack traces
// Usage:
//   DebugLogger.Init()            — call once at startup
//   DebugLogger.AttachKeyLogger() — call after Application.Init()
//   DebugLogger.Shutdown()        — call on exit
// Zero overhead when Init() is never called — all methods bail immediately.
public static class DebugLogger
{
    private static StreamWriter? _writer;
    private static bool _enabled;
    private static string? _logFilePath;

    // Absolute path to the active log file, or null if logging is disabled.
    public static string? LogFilePath => _logFilePath;

    // ── Session ──────────────────────────────────────────────────

    // Initialize the logger — creates/overwrites debug.log in the working directory.
    // Call once at startup. All other methods are no-ops until Init is called.
    // Resilient to file locks — falls back to a timestamped filename if locked.
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

    // Flush and close the log file. Call on application exit.
    public static void Shutdown()
    {
        if (!_enabled) return;
        Write("SESSION", "Debug logger shutdown");
        _writer?.Dispose();
        _writer = null;
        _enabled = false;
    }

    // ── Input ────────────────────────────────────────────────────

    // Hook into Terminal.Gui's global key event to capture all keystrokes.
    // Must be called after Application.Init().
    public static void AttachKeyLogger()
    {
        if (!_enabled) return;
        Application.KeyDown += (s, e) => Write("INPUT", $"Key: {e}");
    }

    // ── Game Output ──────────────────────────────────────────────

    // Log a game event with a category tag (COMBAT, SYSTEM, LOG, etc.).
    public static void LogGame(string category, string message)
    {
        if (_enabled) Write(category, message);
    }

    // Log a screen transition (e.g., "Navigated to GameScreen").
    public static void LogScreen(string screenName)
    {
        if (_enabled) Write("SCREEN", $"Navigated to {screenName}");
    }

    // ── State Snapshots ──────────────────────────────────────────

    // Dump a named entity's key stats — call at combat start, level up, equip, etc.
    // Example output: [STATE] Player "Kirito" | LVL:5 HP:150/200 ATK:42 DEF:30 SPD:18
    public static void LogState(string label, string snapshot)
    {
        if (_enabled) Write("STATE", $"{label} | {snapshot}");
    }

    // ── Performance Timing ───────────────────────────────────────

    // Start a named timer. Returns a Stopwatch to pass to EndTimer.
    // var sw = DebugLogger.StartTimer("GameScreen.Show");
    // // ... do work ...
    // DebugLogger.EndTimer("GameScreen.Show", sw);
    public static Stopwatch StartTimer(string label)
    {
        var sw = Stopwatch.StartNew();
        if (_enabled) Write("PERF", $"[START] {label}");
        return sw;
    }

    // Stop a named timer and log elapsed milliseconds.
    // Example output: [PERF] [END] GameScreen.Show — 12.34ms
    public static void EndTimer(string label, Stopwatch sw)
    {
        sw.Stop();
        if (_enabled) Write("PERF", $"[END] {label} — {sw.Elapsed.TotalMilliseconds:F2}ms");
    }

    // ── Errors ───────────────────────────────────────────────────

    // Log an exception with context label and full stack trace.
    // Inner exceptions are logged on a subsequent line.
    public static void LogError(string context, Exception ex)
    {
        if (!_enabled) return;
        Write("ERROR", $"[{context}] {ex.Message}");
        _writer?.WriteLine(ex.StackTrace);
        if (ex.InnerException != null)
            _writer?.WriteLine($"  Inner: {ex.InnerException.Message}");
    }

    // ── Core ─────────────────────────────────────────────────────

    // Write a single timestamped log line: [HH:mm:ss.fff] [TAG    ] message
    private static void Write(string tag, string message)
    {
        _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{tag,-7}] {message}");
    }
}
