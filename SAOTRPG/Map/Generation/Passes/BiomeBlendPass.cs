namespace SAOTRPG.Map.Generation.Passes;

// Softens tile edges (forest→tall-grass, rocks/shores→sparse). Fringe chances come from
// biome config so dense-forest biomes bleed more while sparse deserts barely blend.
public sealed class BiomeBlendPass : IGenerationPass
{
    public string Name => "BiomeBlend";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        // Forest fringe tracks TreeDensity (0..~2.5 nominally); rock fringe tracks
        // RockDensity; shore fringe is high when lakes are enabled.
        float forestFringeChance = Math.Clamp(ctx.Config.TreeDensity / 2.5f, 0.1f, 0.9f);
        float rockFringeChance   = Math.Clamp(ctx.Config.RockDensity / 2.5f, 0.1f, 0.9f);
        float shoreFringeChance  = ctx.Config.WaterLakesEnabled ? 0.5f : 0.2f;
        MapGenerator.BlendBiomes(ctx.Map, ctx.Rng,
            forestFringeChance, rockFringeChance, shoreFringeChance);
    }
}
