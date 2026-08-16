using SAOTRPG.ThirdParty;

namespace SAOTRPG.Map.Generation;

// OpenSimplex2 + FBm + domain warp heightmap. 500×250 × 4 bytes = ~500 KB peak per floor.
// Eagerly materialized at pass time, stored on GameMap for UI debug overlay consumption.
public sealed class HeightmapField
{
    private readonly FastNoiseLite _base;
    private readonly FastNoiseLite _warp;
    private readonly FastNoiseLite? _ridge;
    private readonly float _ridgeStrength;
    private readonly float _ridgeSpreadFix = 1f;
    private readonly int _w, _h;

    // The values every biome used while these were hardcoded. A biome that sets neither is
    // bit-identical to before this knob existed, which is what keeps the blast radius to the
    // biomes that actually ask for a different shape.
    public const int DefaultOctaves = 4;
    public const float DefaultWarpAmp = 30f;

    // FastNoiseLite runs the octave loop per sample, so cost is linear in octave count and an
    // unbounded config value is a per-cell cost multiplier on a 1000x1000 floor. Gain is 0.5, so
    // octave 8 already contributes 1/256 of the amplitude and anything beyond is invisible.
    private const int MinOctaves = 1, MaxOctaves = 8;
    private const float MinWarpAmp = 0f, MaxWarpAmp = 120f;

    // `ridgeStrength` 0 means pure FBm and takes a branch that skips the ridged field entirely,
    // so a biome with ridging off is bit-identical to before ridging existed — which is what keeps
    // the blast radius of this knob to the four biomes that actually ask for it.
    public HeightmapField(int seed, int w, int h, float ridgeStrength = 0f,
                          int octaves = DefaultOctaves, float warpAmp = DefaultWarpAmp)
    {
        _w = w; _h = h;
        _base = new FastNoiseLite(seed);
        _base.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _base.SetFractalType(FastNoiseLite.FractalType.FBm);
        _base.SetFractalOctaves(octaves);
        _base.SetFractalLacunarity(2.0f);
        _base.SetFractalGain(0.5f);
        _base.SetFrequency(0.02f);

        _warp = new FastNoiseLite(seed + 1);
        _warp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        _warp.SetDomainWarpAmp(warpAmp);

        _ridgeStrength = ridgeStrength;
        if (_ridgeStrength > 0f)
        {
            // Ridged multifractal over the SAME warped coordinates and the same octave/frequency
            // settings, so ridges follow the landscape the base field describes. The seed is shared
            // with the base field, which is presentational rather than load-bearing: the ridge
            // transform (1 - |n|) maps +n and -n to the same value and so destroys the sign
            // correlation anyway. Measured both ways — the difference is under a percent.
            _ridge = new FastNoiseLite(seed);
            _ridge.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            _ridge.SetFractalType(FastNoiseLite.FractalType.Ridged);
            _ridge.SetFractalOctaves(octaves);
            _ridge.SetFractalLacunarity(2.0f);
            _ridge.SetFractalGain(0.5f);
            _ridge.SetFrequency(0.02f);

            // Blending two fields of equal variance that are effectively uncorrelated leaves
            // variance sigma^2*((1-s)^2 + s^2) — a trough at s=0.5 that costs ~29% of the spread.
            // MEASURED before this existed: both endpoints are healthy (base stdev 0.168, pure
            // ridged 0.166) while the mixes collapsed to 0.117-0.136, and the top and bottom
            // terrain bands all but disappeared. So mid-strength ridging FLATTENED the map, which
            // is the opposite of the feature. Undoing the known factor about the midpoint restores
            // the spread at every strength and is exact at both ends (s=0 and s=1 give 1.0).
            _ridgeSpreadFix = 1f / MathF.Sqrt((1f - _ridgeStrength) * (1f - _ridgeStrength)
                                              + _ridgeStrength * _ridgeStrength);
        }
    }

    // Returns [0..1].
    public float GetHeight(int x, int y)
    {
        float fx = x, fy = y;
        _warp.DomainWarp(ref fx, ref fy);
        float n = _base.GetNoise(fx, fy);
        float v = (n + 1f) * 0.5f;
        if (_ridge != null)
        {
            // Lerp rather than add: at strength 1 the result is pure ridged terrain and the range
            // stays [0..1], so a biome's heightmapThresholds keep meaning the same proportions of
            // the map whatever the strength. Adding would push the whole field upward and quietly
            // turn every threshold into a different fraction of the floor.
            float r = (_ridge.GetNoise(fx, fy) + 1f) * 0.5f;
            v += (r - v) * _ridgeStrength;
            // Restore the spread the blend costs, about the field's midpoint. Clamped below.
            v = 0.5f + (v - 0.5f) * _ridgeSpreadFix;
        }
        if (v < 0f) v = 0f; else if (v > 1f) v = 1f;
        return v;
    }

    // Parallel across columns. GetHeight is a PURE function of (x, y) — FastNoiseLite writes its
    // fields only in Set* configuration methods, so all three noise objects are read-only here — and
    // each cell writes its own slot. Output is therefore bit-identical to the serial version
    // regardless of scheduling, which is the property that makes this safe to parallelise at all:
    // there is no shared RNG whose draw order could shift. Verified by floor signature.
    // Small maps stay serial; the high floors are 100x100 and the fork would cost more than the work.
    private const int ParallelCellThreshold = 65536;

    public float[,] Materialize()
    {
        var heights = new float[_w, _h];
        if ((long)_w * _h < ParallelCellThreshold)
        {
            for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                heights[x, y] = GetHeight(x, y);
            return heights;
        }
        System.Threading.Tasks.Parallel.For(0, _w, x =>
        {
            for (int y = 0; y < _h; y++)
                heights[x, y] = GetHeight(x, y);
        });
        return heights;
    }

    public static float[,] Build(WorldContext ctx)
    {
        // StableHash: HashCode.Combine randomises per process, which made the terrain itself
        // different on every launch for the same seed.
        int noiseSeed = Systems.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "heightmap");
        var field = new HeightmapField(noiseSeed, ctx.Width, ctx.Height, RidgeStrength(ctx),
                                       Octaves(ctx), WarpAmp(ctx));
        return field.Materialize();
    }

    // Ridging is off unless the biome asks for it. A strength outside [0..1] is a broken config,
    // not a stronger effect: it is clamped AND logged rather than silently accepted, because the
    // lerp would otherwise extrapolate past pure-ridged terrain and skew every height threshold.
    private static float RidgeStrength(WorldContext ctx)
    {
        if (!ctx.Config.RidgeNoiseEnabled) return 0f;
        float s = ctx.Config.RidgeNoiseStrength;
        if (s >= 0f && s <= 1f) return s;
        float clamped = s < 0f ? 0f : 1f;
        UI.DebugLogger.LogGame("HEIGHTMAP",
            $"biome={ctx.Biome} ridgeNoiseStrength={s} outside [0,1] — clamped to {clamped}");
        return clamped;
    }

    // Same contract as RidgeStrength: an out-of-range config value is a broken file, not a
    // stronger effect, so it is clamped AND logged rather than silently accepted.
    private static int Octaves(WorldContext ctx)
    {
        int o = ctx.Config.HeightmapOctaves;
        if (o >= MinOctaves && o <= MaxOctaves) return o;
        int clamped = Math.Clamp(o, MinOctaves, MaxOctaves);
        UI.DebugLogger.LogGame("HEIGHTMAP",
            $"biome={ctx.Biome} heightmapOctaves={o} outside [{MinOctaves},{MaxOctaves}] — "
            + $"clamped to {clamped}");
        return clamped;
    }

    private static float WarpAmp(WorldContext ctx)
    {
        float a = ctx.Config.HeightmapWarpAmp;
        if (a >= MinWarpAmp && a <= MaxWarpAmp) return a;
        float clamped = Math.Clamp(a, MinWarpAmp, MaxWarpAmp);
        UI.DebugLogger.LogGame("HEIGHTMAP",
            $"biome={ctx.Biome} heightmapWarpAmp={a} outside [{MinWarpAmp},{MaxWarpAmp}] — "
            + $"clamped to {clamped}");
        return clamped;
    }
}
