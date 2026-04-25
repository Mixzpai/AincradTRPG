namespace SAOTRPG.Map.Generation.Passes;

// Populates ctx.Rng from (globalSeed, floorNumber). Stable per-floor for F9 hot-reload,
// distinct per floor so layouts don't repeat.
public sealed class SeedDerivationPass : IGenerationPass
{
    public string Name => "SeedDerivation";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        int seed = HashCode.Combine(ctx.GlobalSeed, ctx.FloorNumber);
        ctx.Rng = new Random(seed);
    }
}
