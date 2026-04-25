using SAOTRPG.UI;

namespace SAOTRPG.Map.Generation;

// Runs passes in order, wrapping each in a DebugLogger timer for per-pass
// perf visibility. Short-circuits if ShouldRun returns false.
public static class GenerationPipeline
{
    public static (GameMap Map, List<Room> Rooms) Run(WorldContext ctx, IReadOnlyList<IGenerationPass> passes)
    {
        var outerSw = DebugLogger.StartTimer($"GenerateFloor({ctx.FloorNumber})");
        foreach (var pass in passes)
        {
            if (!pass.ShouldRun(ctx)) continue;
            var passSw = DebugLogger.StartTimer($"  Pass:{pass.Name}");
            pass.Execute(ctx);
            DebugLogger.EndTimer($"  Pass:{pass.Name}", passSw);
        }
        DebugLogger.EndTimer($"GenerateFloor({ctx.FloorNumber})", outerSw);
        return (ctx.Map, ctx.Rooms);
    }
}
