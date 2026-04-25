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

        var samples = ReservoirSample(ctx.Map, ctx.Width, ctx.Height, targetCount, ctx.Rng);
        if (samples.Count == 0) return;

        ParticleQueue.SeedAmbient(ctx.Config.AmbientOverlay.ParticleId!, samples, ctx.Rng, targetCount);
    }

    // Reservoir sampling: keep `target` random samples in a single pass — no full-map list buffer.
    private static List<(int X, int Y)> ReservoirSample(GameMap map, int w, int h, int target, Random rng)
    {
        var reservoir = new List<(int X, int Y)>(target);
        int seen = 0;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (!map.InBounds(x, y)) continue;
            if (!map.Tiles[x, y].IsWalkable) continue;
            seen++;
            if (reservoir.Count < target) reservoir.Add((x, y));
            else
            {
                int j = rng.Next(seen);
                if (j < target) reservoir[j] = (x, y);
            }
        }
        return reservoir;
    }
}
