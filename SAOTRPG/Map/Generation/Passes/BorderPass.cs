namespace SAOTRPG.Map.Generation.Passes;

// Jagged mountain border around the map. Thin wrapper over the existing
// MapGenerator.BuildJaggedMountainBorder helper.
public sealed class BorderPass : IGenerationPass
{
    public string Name => "Border";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        MapGenerator.BuildJaggedMountainBorder(ctx.Map, ctx.Rng);
    }
}
