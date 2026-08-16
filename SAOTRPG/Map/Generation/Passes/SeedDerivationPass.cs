namespace SAOTRPG.Map.Generation.Passes;

// Populates ctx.Rng from (globalSeed, floorNumber). Stable per-floor for F9 hot-reload,
// distinct per floor so layouts don't repeat.
public sealed class SeedDerivationPass : IGenerationPass
{
    public string Name => "SeedDerivation";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        // StableHash, not HashCode.Combine: the latter randomises per process, so every pass's
        // RNG — and therefore the whole floor — changed on each launch of the game.
        int seed = Systems.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber);
        ctx.Rng = new Random(seed);
    }
}
