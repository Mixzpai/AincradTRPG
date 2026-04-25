using SAOTRPG.ThirdParty;

namespace SAOTRPG.Map.Generation;

// OpenSimplex2 + FBm + domain warp heightmap. 500×250 × 4 bytes = ~500 KB peak per floor.
// Eagerly materialized at pass time, stored on GameMap for UI debug overlay consumption.
public sealed class HeightmapField
{
    private readonly FastNoiseLite _base;
    private readonly FastNoiseLite _warp;
    private readonly int _w, _h;

    public HeightmapField(int seed, int w, int h)
    {
        _w = w; _h = h;
        _base = new FastNoiseLite(seed);
        _base.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _base.SetFractalType(FastNoiseLite.FractalType.FBm);
        _base.SetFractalOctaves(4);
        _base.SetFractalLacunarity(2.0f);
        _base.SetFractalGain(0.5f);
        _base.SetFrequency(0.02f);

        _warp = new FastNoiseLite(seed + 1);
        _warp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        _warp.SetDomainWarpAmp(30f);
    }

    // Returns [0..1].
    public float GetHeight(int x, int y)
    {
        float fx = x, fy = y;
        _warp.DomainWarp(ref fx, ref fy);
        float n = _base.GetNoise(fx, fy);
        float v = (n + 1f) * 0.5f;
        if (v < 0f) v = 0f; else if (v > 1f) v = 1f;
        return v;
    }

    public float[,] Materialize()
    {
        var heights = new float[_w, _h];
        for (int x = 0; x < _w; x++)
        for (int y = 0; y < _h; y++)
            heights[x, y] = GetHeight(x, y);
        return heights;
    }

    public static float[,] Build(WorldContext ctx)
    {
        int noiseSeed = HashCode.Combine(ctx.GlobalSeed, ctx.FloorNumber, "heightmap");
        var field = new HeightmapField(noiseSeed, ctx.Width, ctx.Height);
        return field.Materialize();
    }
}
