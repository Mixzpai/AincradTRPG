using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// Per-tile RGB light. Shadowcaster-driven FOV per emissive tile (+ player torch);
// linear falloff, summed per channel, clamped to [0,255]. Owned by GameMap; recomputed on UpdateVisibility.
public sealed class LightingSystem
{
    // Accumulated light at a tile, stored as float RGB to allow clean summation.
    public struct LightRgb
    {
        public float R, G, B;
        public static LightRgb Of(float r, float g, float b) => new() { R = r, G = g, B = b };
    }

    public int Width { get; }
    public int Height { get; }

    // Ambient via DayNightCycle.Ambient. Player torch: warm white, flickers (radius 11-13, color warm/cool per turn).
    private static LightRgb TorchColor()
    {
        int phase = DayNightCycle.CurrentTurn % 4;
        return phase switch
        {
            0 => LightRgb.Of(220, 200, 160),
            1 => LightRgb.Of(230, 190, 150),
            2 => LightRgb.Of(215, 205, 165),
            _ => LightRgb.Of(225, 195, 155),
        };
    }
    private static int TorchRadius() => 11 + (DayNightCycle.CurrentTurn % 3);

    private readonly LightRgb[,] _light;

    // Emissive coords owned by GameMap.EmissiveTiles; we only cache per-type (color, radius).

    public LightingSystem(int width, int height)
    {
        Width = width;
        Height = height;
        _light = new LightRgb[width, height];
        // Fill entire map with default ambient once at construction.
        var (ar, ag, ab) = DayNightCycle.Ambient;
        var ambient = LightRgb.Of(ar, ag, ab);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _light[x, y] = ambient;
    }

    public LightRgb GetLight(int x, int y) =>
        (uint)x < (uint)Width && (uint)y < (uint)Height ? _light[x, y] : default;

    // Fast path for render loops that have already done a bounds check.
    public LightRgb GetLightUnchecked(int x, int y) => _light[x, y];

    // ROI radius matches active FOV so all visible tiles are lit (FOV 4× the viewport).
    private static int LightingRoiRadius => DayNightCycle.FovRadius + 10;

    public void Update(GameMap map, int playerX, int playerY)
    {
        using var _ = Profiler.Begin("Lighting.Update");
        FillAmbientRegion(playerX, playerY);
        AddSource(map, playerX, playerY, TorchRadius(), TorchColor());

        int roiSq = LightingRoiRadius * LightingRoiRadius;
        // Shrine/Fountain radius breathes ±1 on a slow sine (colored pool expands/contracts).
        double pulsePhase = (DayNightCycle.CurrentTurn % 12) / 12.0 * Math.PI * 2.0;
        int pulseDelta = (int)Math.Round(Math.Sin(pulsePhase));
        int emissiveCount = 0;
        // Bucket radius 2 (5x5 64-cell buckets) covers ROI≈92 with margin.
        int bucketRadius = (LightingRoiRadius >> GameMap.EmissiveBucketShift) + 1;
        foreach (var (sx, sy, stype) in map.EmissiveTilesNear(playerX, playerY, bucketRadius))
        {
            emissiveCount++;
            var emission = GetEmission(stype);
            if (emission == null) continue;
            int dx = sx - playerX, dy = sy - playerY;
            if (dx * dx + dy * dy > roiSq) continue; // skip distant lights
            int radius = emission.Value.Radius;
            if (stype is TileType.Shrine or TileType.Fountain or TileType.EnchantShrine)
                radius = Math.Max(2, radius + pulseDelta);
            AddSource(map, sx, sy, radius, emission.Value.Color);
        }
        Profiler.RecordCount("Lighting.EmissiveCount", emissiveCount);
    }

    // Only fill ambient in a region around the player — not the entire map.
    private void FillAmbientRegion(int px, int py)
    {
        using var _ = Profiler.Begin("Lighting.FillAmbient");
        var (ar, ag, ab) = DayNightCycle.Ambient;
        var ambient = LightRgb.Of(ar, ag, ab);
        int x0 = Math.Max(0, px - LightingRoiRadius);
        int x1 = Math.Min(Width, px + LightingRoiRadius);
        int y0 = Math.Max(0, py - LightingRoiRadius);
        int y1 = Math.Min(Height, py + LightingRoiRadius);
        for (int x = x0; x < x1; x++)
        for (int y = y0; y < y1; y++)
            _light[x, y] = ambient;
    }

    // Scratch per-source — diagonals visited by two quadrants don't double-light.
    // Sized to (2*MaxSourceRadius+1)^2 with local-coord mapping (worldX - originX).
    private float[]? _scratchR, _scratchG, _scratchB;
    private const int MaxSourceRadius = 16;
    private const int ScratchStride = MaxSourceRadius * 2 + 1;
    private const int ScratchLen = ScratchStride * ScratchStride;

    // Cached delegates + per-source state reused across AddSource calls to avoid 20-40 closures/turn.
    private GameMap? _srcMap;
    private int _srcSx, _srcSy, _srcRadius;
    private int _srcOriginX, _srcOriginY;
    private float _srcColorR, _srcColorG, _srcColorB;
    private float _srcInvRadius;
    private Shadowcaster.BlockingPredicate? _blockingPred;
    private Shadowcaster.VisibilityCallback? _revealCb;

    private bool IsSourceBlocking(int x, int y)
        => !_srcMap!.InBounds(x, y) || _srcMap.IsOpaque(x, y);

    private void RevealSourceTile(int x, int y)
    {
        int dx = x - _srcSx, dy = y - _srcSy;
        int d2 = dx * dx + dy * dy;
        float dist = FastSqrt(d2);
        float falloff = 1f - dist * _srcInvRadius;
        if (falloff <= 0) return;
        int lx = x - _srcOriginX, ly = y - _srcOriginY;
        if ((uint)lx >= (uint)ScratchStride || (uint)ly >= (uint)ScratchStride) return;
        int idx = ly * ScratchStride + lx;
        float contribR = _srcColorR * falloff;
        float contribG = _srcColorG * falloff;
        float contribB = _srcColorB * falloff;
        if (contribR > _scratchR![idx]) _scratchR[idx] = contribR;
        if (contribG > _scratchG![idx]) _scratchG[idx] = contribG;
        if (contribB > _scratchB![idx]) _scratchB[idx] = contribB;
    }

    private void AddSource(GameMap map, int sx, int sy, int radius, LightRgb color)
    {
        using var _ = Profiler.Begin("Lighting.AddSource");
        // Lazy-allocate ROI-sized scratch (cap at MaxSourceRadius). Reused across calls.
        if (_scratchR == null)
        {
            _scratchR = new float[ScratchLen];
            _scratchG = new float[ScratchLen];
            _scratchB = new float[ScratchLen];
        }

        if (radius > MaxSourceRadius) radius = MaxSourceRadius;

        // Method-group delegates allocate once and reuse.
        _blockingPred ??= IsSourceBlocking;
        _revealCb ??= RevealSourceTile;

        // Stash per-source context so cached delegates read without captures.
        _srcMap = map;
        _srcSx = sx; _srcSy = sy; _srcRadius = radius;
        _srcOriginX = sx - MaxSourceRadius;
        _srcOriginY = sy - MaxSourceRadius;
        _srcColorR = color.R; _srcColorG = color.G; _srcColorB = color.B;
        _srcInvRadius = radius > 0 ? 1f / radius : 0f;

        // Scratch takes MAX per tile — overlapping quadrant boundaries don't double-add.
        Shadowcaster.Compute(sx, sy, radius, _blockingPred, _revealCb);

        // Additively merge scratch into grid, then clear scratch (local→world coord remap).
        int x0 = Math.Max(0, sx - radius), x1 = Math.Min(Width, sx + radius + 1);
        int y0 = Math.Max(0, sy - radius), y1 = Math.Min(Height, sy + radius + 1);
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            int idx = (y - _srcOriginY) * ScratchStride + (x - _srcOriginX);
            float sr = _scratchR[idx], sg = _scratchG![idx], sb = _scratchB![idx];
            if (sr > 0 || sg > 0 || sb > 0)
            {
                _light[x, y].R = Math.Min(255f, _light[x, y].R + sr);
                _light[x, y].G = Math.Min(255f, _light[x, y].G + sg);
                _light[x, y].B = Math.Min(255f, _light[x, y].B + sb);
                _scratchR[idx] = 0; _scratchG[idx] = 0; _scratchB[idx] = 0;
            }
        }
    }

    // sqrt LUT for integer d²; covers torch radius 13 → (13+3)²=256. Fallback to Math.Sqrt beyond.
    private const int SqrtLutSize = 400;
    private static readonly float[] SqrtLut = BuildSqrtLut();
    private static float[] BuildSqrtLut()
    {
        var t = new float[SqrtLutSize];
        for (int i = 0; i < SqrtLutSize; i++) t[i] = (float)Math.Sqrt(i);
        return t;
    }
    private static float FastSqrt(int d2)
    {
        if ((uint)d2 < (uint)SqrtLutSize) return SqrtLut[d2];
        return (float)Math.Sqrt(d2);
    }

    // Emissive tiles: RGB tint + Euclidean shadowcasted radius. Generous overlap with torch for warm/cool blends.
    private static (LightRgb Color, int Radius)? GetEmission(TileType type) => type switch
    {
        TileType.Campfire      => (LightRgb.Of(255, 180,  80), 8),   // warm orange
        TileType.Lava          => (LightRgb.Of(255, 100,  30), 5),   // hot red-orange
        TileType.Shrine        => (LightRgb.Of(200, 120, 255), 6),   // mystical violet
        TileType.EnchantShrine => (LightRgb.Of(255, 220, 100), 5),   // golden
        TileType.Fountain      => (LightRgb.Of(100, 200, 255), 5),   // cool cyan
        TileType.StairsUp      => (LightRgb.Of(180, 230, 255), 3),   // pale blue
        TileType.LoreStone     => (LightRgb.Of(220, 100, 255), 3),   // purple
        TileType.GasVent       => (LightRgb.Of(100, 255, 100), 3),   // sickly green
        TileType.Anvil         => (LightRgb.Of(255, 160,  60), 4),   // forge glow
        TileType.Door          => (LightRgb.Of(180, 160, 120), 2),   // faint warm light
        TileType.LabyrinthEntrance => (LightRgb.Of(160, 200, 255), 4), // pale blue archway
        _ => null,
    };
}
