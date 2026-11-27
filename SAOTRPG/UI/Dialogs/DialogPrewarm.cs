using System.Reflection;
using System.Runtime.CompilerServices;
using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.UI.Dialogs;

// Without this, a dialog's first open in a session pays its JIT and static set-up on the keypress.
// Started from Program.Main, this compiles the game's UI code and the Terminal.Gui views the
// dialogs are built from, and builds the Guide's shared content, on a worker while the player is
// still on the title screen. A method that cannot be compiled ahead is counted and logged.
public static class DialogPrewarm
{
    // The last run's outcome, for the log; also read by the verification probes.
    public static string LastSummary { get; private set; } = "not run";

    public static Task Start() => Task.Run(() =>
    {
        try { Run(); }
        catch (Exception ex) { LastSummary = $"failed: {ex.GetType().Name}"; DebugLogger.LogError("DialogPrewarm", ex); }
    });

    private static void Run()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        PlayerGuideDialog.Prewarm();
        int compiled = 0, failed = 0;
        string? firstFailure = null;
        // Nested closure types share their namespace, so the dialogs' lambdas are compiled too.
        Compile(typeof(DialogPrewarm).Assembly, ns => ns.StartsWith("SAOTRPG.UI", StringComparison.Ordinal),
                ref compiled, ref failed, ref firstFailure);
        Compile(typeof(Terminal.Gui.Views.Dialog).Assembly,
                ns => ns is "Terminal.Gui.Views" or "Terminal.Gui.ViewBase" or "Terminal.Gui.Text",
                ref compiled, ref failed, ref firstFailure);
        LastSummary = $"{compiled} methods compiled ahead in {sw.ElapsedMilliseconds} ms"
                      + (failed > 0 ? $"; {failed} could not be, first {firstFailure}" : "");
        DebugLogger.LogGame("PREWARM", LastSummary);
    }

    private static void Compile(Assembly assembly, Func<string, bool> wanted, ref int compiled, ref int failed,
                                ref string? firstFailure)
    {
        const BindingFlags All = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static
                                 | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace is not { } ns || !wanted(ns) || type.ContainsGenericParameters) continue;
            foreach (MethodBase m in type.GetMethods(All).Cast<MethodBase>().Concat(type.GetConstructors(All)))
            {
                if (m.IsAbstract || m.ContainsGenericParameters || m.GetMethodBody() == null) continue;
                try { RuntimeHelpers.PrepareMethod(m.MethodHandle); compiled++; }
                catch (Exception ex) { failed++; firstFailure ??= $"{type.Name}.{m.Name}: {ex.GetType().Name}"; }
            }
        }
    }
}
