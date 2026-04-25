using SAOTRPG.Systems;
using SAOTRPG.UI;

namespace SAOTRPG.Map.Generation;

// Runs passes in order, wrapping each in a DebugLogger timer for per-pass
// perf visibility. Short-circuits if ShouldRun returns false.
public static class GenerationPipeline
{
    public static (GameMap Map, List<Room> Rooms) Run(WorldContext ctx, IReadOnlyList<IGenerationPass> passes)
    {
        var outerSw = DebugLogger.StartTimer($"GenerateFloor({ctx.FloorNumber})");
        using (Profiler.Begin("MapGen.Total"))
        {
            BuildCircleMask(ctx);
            foreach (var pass in passes)
            {
                if (!pass.ShouldRun(ctx)) continue;
                var passSw = DebugLogger.StartTimer($"  Pass:{pass.Name}");
                using (Profiler.Begin($"Pass.{pass.Name}"))
                    pass.Execute(ctx);
                DebugLogger.EndTimer($"  Pass:{pass.Name}", passSw);
            }
        }
        DebugLogger.EndTimer($"GenerateFloor({ctx.FloorNumber})", outerSw);
        return (ctx.Map, ctx.Rooms);
    }

    // Towns + F100 skip mask build (mask stays null → IsInsideCircle returns true everywhere).
    // GameMap mirrors the mask so post-pipeline Population can gate placements.
    private static void BuildCircleMask(WorldContext ctx)
    {
        if (FloorScale.IsHandBuiltTownFloor(ctx.FloorNumber)) return;
        if (ctx.FloorNumber == 100) return;
        ctx.CircleMask = FloorMask.Build(ctx.Width, ctx.Height, ctx.GlobalSeed ^ ctx.FloorNumber);
        ctx.Map.CircleMask = ctx.CircleMask;
    }
}
