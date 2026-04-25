using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation.Passes;

// Seeds per-floor ambient particles into ParticleQueue from biome config; no-op when
// ParticleDensity=Off or no particleId. Density tier scales count (Subtle 0.5x / Mod 1x / Pro 2x).
public sealed class AmbientOverlayPass : IGenerationPass
{
    public string Name => "AmbientOverlay";

    public bool ShouldRun(WorldContext ctx) =>
        UserSettings.Current.ParticleDensity != ParticleDensity.Off
        && !string.IsNullOrEmpty(ctx.Config.AmbientOverlay?.ParticleId);

    public void Execute(WorldContext ctx)
    {
        float densityMul = UserSettings.Current.ParticleDensity switch
        {
            ParticleDensity.Subtle     => 0.5f,
            ParticleDensity.Moderate   => 1.0f,
            ParticleDensity.Pronounced => 2.0f,
            _ => 0f,
        };
        if (densityMul <= 0f) return;

        int permille = ctx.Config.AmbientOverlay!.DensityPermille;
        if (permille <= 0) return;

        int targetCount = (int)Math.Round(
            ctx.Width * ctx.Height * permille / 1000f * densityMul);
        targetCount = Math.Min(targetCount, ParticleQueue.MaxConcurrent - 5);
        if (targetCount <= 0) return;

        var walkable = GatherWalkable(ctx.Map, ctx.Width, ctx.Height);
        if (walkable.Count == 0) return;

        ParticleQueue.SeedAmbient(ctx.Config.AmbientOverlay.ParticleId!, walkable, ctx.Rng, targetCount);
    }

    private static List<(int X, int Y)> GatherWalkable(GameMap map, int w, int h)
    {
        var list = new List<(int X, int Y)>(w * h / 2);
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (!map.InBounds(x, y)) continue;
            if (map.Tiles[x, y].IsWalkable) list.Add((x, y));
        }
        return list;
    }
}
