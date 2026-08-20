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
            // Every pipeline floor is an open-air disk and takes sun shadows. F100 is the Ruby
            // Palace throne room, which is interior — as is the labyrinth, which never reaches
            // this method at all and so keeps GameMap's conservative default.
            ctx.Map.HasSky = ctx.FloorNumber != 100;
            BuildCircleMask(ctx);
            ComputeTownKeepOut(ctx);
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

    // F100 skips mask build (mask stays null → IsInsideCircle returns true everywhere).
    // GameMap mirrors the mask so post-pipeline Population can gate placements.
    private static void BuildCircleMask(WorldContext ctx)
    {
        if (ctx.FloorNumber == 100) return;
        // RunRng.StableHash rather than seed ^ floor: XOR collides in general, though not here
        // (Build also takes Width/Height, which shrink with floor independently of the seed).
        // It must NOT be HashCode.Combine — that randomises per process, which made the floor's
        // own shape differ on every launch of the game for the same seed.
        ctx.CircleMask = FloorMask.Build(ctx.Width, ctx.Height,
            RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber));
        ctx.Map.CircleMask = ctx.CircleMask;
    }

    // Mark the town footprint + 8-tile gate-approach margin so terrain passes
    // skip stamping inside. F1=Town of Beginnings 51x29, F48=Lindarth 61x35,
    // others use a small spawn-clearing buffer. F100 castle = no keep-out.
    private static void ComputeTownKeepOut(WorldContext ctx)
    {
        if (ctx.FloorNumber == 100) return;
        int sx = ctx.Width / 2, sy = ctx.Height / 2;
        const int Margin = 8;
        int halfW, halfH;
        if (ctx.FloorNumber == 1)       { halfW = 25 + Margin; halfH = 14 + Margin; }
        else if (ctx.FloorNumber == 48) { halfW = 30 + Margin; halfH = 17 + Margin; }
        else                            { halfW = 8;           halfH = 8;           }

        ctx.TownKeepOutX = Math.Max(0, sx - halfW);
        ctx.TownKeepOutY = Math.Max(0, sy - halfH);
        ctx.TownKeepOutW = halfW * 2;
        ctx.TownKeepOutH = halfH * 2;
    }
}
