namespace SAOTRPG.Map;

// Per-tile RGB light accumulation. Each emissive tile (and the player's
// personal torch) casts a Shadowcaster-driven FOV; contributions are
// summed per channel with a linear falloff and clamped to [0, 255].
// Usage: one instance owned by GameMap, recomputed whenever
// GameMap.UpdateVisibility runs.
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

    // Baseline light in every tile — now driven by DayNightCycle so the
    // world brightens at noon and darkens toward midnight. See
    // DayNightCycle.Ambient for the per-frame RGB.

    // Player-carried torch — warm white, moderately bright so nearby
    // feature lights (campfires, shrines) can still show their own
    // color over the torch's glow instead of being drowned out.
    // Torch flickers: radius oscillates 11-13, color shifts warm/cool per turn.
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

    // Emissive tile coordinates are owned by GameMap (GameMap.EmissiveTiles).
    // We only cache the per-type (color, radius) lookup to skip the switch
    // on every frame.

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

    // Region-of-interest radius — matches the viewport FOV so all visible
    // tiles are lit. Updated dynamically from DayNightCycle.ViewportRadius.
    private static int LightingRoiRadius => DayNightCycle.ViewportRadius + 10;

    public void Update(GameMap map, int playerX, int playerY)
    {
        FillAmbientRegion(playerX, playerY);
        AddSource(map, playerX, playerY, TorchRadius(), TorchColor());

        int roiSq = LightingRoiRadius * LightingRoiRadius;
        // Shrine/Fountain radius breathes ±1 tile on a slow sine so the
        // colored pool on the floor expands and contracts.
        double pulsePhase = (DayNightCycle.CurrentTurn % 12) / 12.0 * Math.PI * 2.0;
        int pulseDelta = (int)Math.Round(Math.Sin(pulsePhase));
        foreach (var (sx, sy, stype) in map.EmissiveTiles)
        {
            var emission = GetEmission(stype);
            if (emission == null) continue;
            int dx = sx - playerX, dy = sy - playerY;
            if (dx * dx + dy * dy > roiSq) continue; // skip distant lights
            int radius = emission.Value.Radius;
            if (stype is TileType.Shrine or TileType.Fountain or TileType.EnchantShrine)
                radius = Math.Max(2, radius + pulseDelta);
            AddSource(map, sx, sy, radius, emission.Value.Color);
        }
    }

    // Only fill ambient in a region around the player — not the entire map.
    private void FillAmbientRegion(int px, int py)
    {
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

    // Scratch buffer to track per-source contributions so diagonal tiles
    // (visited by two shadowcaster quadrants) don't get double-lit.
    private float[]? _scratchR, _scratchG, _scratchB;

    // Cached delegates and per-source state for shadowcasting — reused across
    // every AddSource call so we don't allocate 20-40 closures per turn.
    private GameMap? _srcMap;
    private int _srcSx, _srcSy, _srcRadius;
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
        int idx = y * Width + x;
        float contribR = _srcColorR * falloff;
        float contribG = _srcColorG * falloff;
        float contribB = _srcColorB * falloff;
        if (contribR > _scratchR![idx]) _scratchR[idx] = contribR;
        if (contribG > _scratchG![idx]) _scratchG[idx] = contribG;
        if (contribB > _scratchB![idx]) _scratchB[idx] = contribB;
    }

    private void AddSource(GameMap map, int sx, int sy, int radius, LightRgb color)
    {
        // Lazy-allocate scratch buffers sized to the map.
        if (_scratchR == null || _scratchR.Length != Width * Height)
        {
            int len = Width * Height;
            _scratchR = new float[len];
            _scratchG = new float[len];
            _scratchB = new float[len];
        }

        // Lazy-cache the delegates once. Method-group conversions to instance
        // methods allocate one delegate up front and are reused forever after.
        _blockingPred ??= IsSourceBlocking;
        _revealCb ??= RevealSourceTile;

        // Stash per-source context into fields so the cached delegates can
        // read them without captures.
        _srcMap = map;
        _srcSx = sx; _srcSy = sy; _srcRadius = radius;
        _srcColorR = color.R; _srcColorG = color.G; _srcColorB = color.B;
        _srcInvRadius = radius > 0 ? 1f / radius : 0f;

        // Collect per-source contribution into scratch, taking MAX per tile
        // so overlapping quadrant boundaries don't double-add.
        Shadowcaster.Compute(sx, sy, radius, _blockingPred, _revealCb);

        // Merge scratch into the light grid (additive across different sources)
        // then clear scratch for next source.
        int x0 = Math.Max(0, sx - radius), x1 = Math.Min(Width, sx + radius + 1);
        int y0 = Math.Max(0, sy - radius), y1 = Math.Min(Height, sy + radius + 1);
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            int idx = y * Width + x;
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

    // Precomputed sqrt LUT for small integer distance-squared values used by
    // the light falloff. Covers d^2 up to (maxRadius)^2 — max light radius in
    // GetEmission is ~13 (torch) so (13+3)^2 = 256 comfortably covers it.
    // Values beyond the table fall back to Math.Sqrt (rare at runtime).
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

    // Tiles that emit light and the RGB tint + reach of each. All radii are
    // Euclidean and shadowcasted, so walls block propagation naturally.
    // Radii are generous enough that the colored pools overlap with the
    // player torch, producing visible warm/cool blends on the ground.
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
