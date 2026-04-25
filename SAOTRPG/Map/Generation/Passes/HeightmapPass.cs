namespace SAOTRPG.Map.Generation.Passes;

// Materializes FBm+domain-warp heightmap into ctx.Heights. Also publishes to
// ctx.Map.DebugHeights so Shift+G overlay can render without an extra seam.
public sealed class HeightmapPass : IGenerationPass
{
    public string Name => "Heightmap";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        ctx.Heights = HeightmapField.Build(ctx);
        ctx.Map.DebugHeights = ctx.Heights;
    }
}
