using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// Per-tile RGB light. Shadowcaster-driven FOV per emissive tile (+ player torch);
// linear falloff, summed per channel, clamped to [0,255]. Owned by GameMap; recomputed on UpdateVisibility.
public sealed partial class LightingSystem
{
    // Accumulated light at a tile, stored as float RGB to allow clean summation.
    public struct LightRgb
    {
        public float R, G, B;
        public static LightRgb Of(float r, float g, float b) => new() { R = r, G = g, B = b };
    }

    public int Width { get; }
    public int Height { get; }

    // Ambient via DayNightCycle.Ambient. Player torch: warm white, flickers (radius 11-13, color warm/cool, real-time @ 4Hz).
    private static LightRgb TorchColor()
    {
        int phase = (int)(FrameClock.AmbientMs / 250) % 4;
        var c = phase switch
        {
            0 => LightRgb.Of(220, 200, 160),
            1 => LightRgb.Of(230, 190, 150),
            2 => LightRgb.Of(215, 205, 165),
            _ => LightRgb.Of(225, 195, 155),
        };
        // Scaled by how dark it is. Flat, this added ~220 per channel at every hour, so at noon
        // the player stood at the centre of a thirteen-tile disc clipped at 255 — brighter than
        // the daylight around it, and washing out every sun shadow within reach of the one
        // viewer who could see them.
        float k = DayNightCycle.TorchInfluence;
        return LightRgb.Of(c.R * k, c.G * k, c.B * k);
    }

    // Shrinks with the flame. A dim torch that still reached thirteen tiles would read as a
    // hole in the daylight rather than as a flame; this also cuts the midday shadowcast.
    private static int TorchRadius()
    {
        int full = 11 + (int)(FrameClock.AmbientMs / 250) % 3;
        return Math.Max(4, (int)MathF.Round(4f + (full - 4f) * DayNightCycle.TorchInfluence));
    }

    // Row-major (y*Width + x). Was [x, y], which strided by Height in all three hot loops:
    // the ambient refill, AddSource's merge, and the map tile loop's per-cell read. Row-major
    // makes each of them a contiguous walk and lets the refill go through Span.Fill.
    //
    // PATH-D-PORT: an RGB light field at ONE VALUE PER LOGICAL TILE, multiplied onto cells in
    // ApplyLighting. A pixel renderer would naturally interpolate between tile centres, which is a
    // different look, not a free upgrade — the hard tile edges are what make light read as
    // roguelike illumination. Keep the layout row-major to match every other per-cell grid.
    private readonly LightRgb[] _light;

    // Emissive coords owned by GameMap.EmissiveTiles; we only cache per-type (color, radius).

    public LightingSystem(int width, int height)
    {
        Width = width;
        Height = height;
        _light = new LightRgb[width * height];
        // Fill entire map with default ambient once at construction.
        var (ar, ag, ab) = DayNightCycle.Ambient;
        _light.AsSpan().Fill(LightRgb.Of(ar, ag, ab));
    }

    // A PROBE SEAM FOR THE ONE FAILURE THIS LAYER CANNOT SHOW YOU: a cell the renderer draws but
    // the fill never reached keeps whatever was in the grid, which looks like light rather than
    // like a bug. Narrowing the lit region is exactly the change that could cause it, so the
    // check pokes a sentinel into every cell it cares about, runs a turn, and looks for
    // survivors.
    //
    // Deliberately NOT a general setter: it writes one fixed value and nothing else, so it cannot
    // be used to fake a light level — the same reasoning as SourcesLastUpdate being read-only.
    private const float UnwrittenSentinel = -12345f;

    public void MarkUnwrittenForVerification(int x, int y) =>
        _light[y * Width + x] = LightRgb.Of(UnwrittenSentinel, UnwrittenSentinel, UnwrittenSentinel);

    public bool IsUnwritten(int x, int y) => _light[y * Width + x].R <= UnwrittenSentinel + 1f;

    // Fast path for render loops that have already done a bounds check.
    public LightRgb GetLightUnchecked(int x, int y) => _light[y * Width + x];

    // How many point lights the last Update actually added — the torch plus every emissive tile
    // that survived the reach gates. A measurement seam, not state: a floor is lit PER TILE and
    // lava is a tile, so this is the number that decides what a turn costs, and a check that
    // recomputes the gate for itself instead of reading this cannot notice the gate widening.
    public int SourcesLastUpdate { get; private set; }

    // ROI radius matches active FOV so all visible tiles are lit (FOV 4× the viewport).
    private static int LightingRoiRadius => DayNightCycle.FovRadius + 10;

    // HOW MANY STEPS OF UPWIND HISTORY A SHADOW NEEDS BEFORE IT IS INDISTINGUISHABLE FROM ONE
    // SWEPT FROM INFINITY — MEASURED, not bounded.
    //
    // The earlier figure was 67, derived by assuming the worst ambient (180), the deepest shadow
    // (0.48) and the slowest decay (0.955) all at once. THEY NEVER CO-OCCUR: the twilight ramp
    // means a slow decay only ever arrives with a weak shadow under a dim sky. Sweeping all 25
    // bearings and asking where `ambient x strength x decay^m` falls under the renderer's 4/255
    // half-step gives a worst case of 31 steps, at bearing 3 (ambient 129, strength 0.373,
    // decay 0.921). The conservative bound was wrong by more than 2x, and it is what made
    // narrowing the region look unprofitable in PERF_HANDOFF §0j.
    private const int ShadowHistorySteps = 31;

    // HOW FAR ABOVE OR BELOW THE PLAYER THE GROUND HAS TO BE LIT AT ALL.
    //
    // Same argument as EmissiveRowReach below, one level up: light is only READ for cells the
    // tile loop draws, the camera is player-centred and unclamped, and the viewport is wide and
    // SHORT — so the rows worth filling are half a viewport height plus enough history for the
    // shadows arriving at them to be right. The horizontal extent stays the full ROI because
    // viewport WIDTH is not derivable from anything the lighting can see.
    //
    // Two things this must clear, both asserted by construction rather than by a number:
    // the drawn rows (FovRadius / FovMultiplier is h/2 + 2), and the rows a light SOURCE may
    // write into, which is EmissiveRowReach + MaxSourceRadius — filling less than that would
    // leave a source screening onto ambient nobody refreshed.
    private static int ShadingRowReach
    {
        get
        {
            int drawn = DayNightCycle.FovRadius / DayNightCycle.FovMultiplier;
            int needed = drawn + ShadowHistorySteps + 16;          // 16 = slack for an odd viewport
            int sourcesWrite = EmissiveRowReach + MaxSourceRadius;
            return Math.Min(LightingRoiRadius, Math.Max(needed, sourcesWrite));
        }
    }

    // HOW FAR ABOVE OR BELOW THE PLAYER A LIGHT SOURCE CAN STILL MATTER.
    //
    // The ROI above is a SQUARE of ~186, but a source only changes cells within its own radius,
    // and a cell is only read if the renderer draws it — and the drawn region is a wide, SHORT
    // viewport. `Camera.CenterOn` does not clamp, so a drawn cell is always within half a
    // viewport height of the player; MapView derives FovRadius from exactly that height as
    // (h/2 + 2) * FovMultiplier, so the row reach falls out here with nothing new having to be
    // published from the view.
    //
    // It matters because a floor is lit PER TILE and lava is a tile: a Volcanic floor carries 220
    // of them, and a square ROI made a single turn run ~193 shadowcasts. The horizontal reach
    // stays the full ROI because viewport WIDTH is not derivable from anything the lighting can
    // see, and the generous 8-cell pad is there so a wider FovMultiplier or an odd viewport
    // height cannot quietly clip a light off the top of the screen.
    private static int EmissiveRowReach =>
        DayNightCycle.FovRadius / DayNightCycle.FovMultiplier + MaxSourceRadius + 8;

    public void Update(GameMap map, int playerX, int playerY)
    {
        using var _ = Profiler.Begin("Lighting.Update");
        FillAmbientRegion(map, playerX, playerY);
        AddSource(map, playerX, playerY, TorchRadius(), TorchColor(), softenEdges: true);

        int sources = 1;   // the torch, added above
        int roiSq = LightingRoiRadius * LightingRoiRadius;
        // Shrine/Fountain radius breathes ±1 on a slow real-time sine (3-second cycle).
        double pulsePhase = (FrameClock.AmbientMs % 3000L) / 3000.0 * Math.PI * 2.0;
        int pulseDelta = (int)Math.Round(Math.Sin(pulsePhase));
        int emissiveCount = 0;
        // Enough 64-cell buckets to span each reach, +1 so a partially-covered edge bucket is kept.
        int rowReach = EmissiveRowReach;
        int bucketRadiusX = (LightingRoiRadius >> GameMap.EmissiveBucketShift) + 1;
        int bucketRadiusY = (rowReach >> GameMap.EmissiveBucketShift) + 1;
        foreach (var (sx, sy, stype, count) in
                 map.EmissiveSourcesNear(playerX, playerY, bucketRadiusX, bucketRadiusY))
        {
            emissiveCount++;
            var emission = GetEmission(stype);
            if (emission == null) continue;
            int dx = sx - playerX, dy = sy - playerY;
            if (dy > rowReach || dy < -rowReach) continue;   // too far up or down to reach a drawn cell
            if (dx * dx + dy * dy > roiSq) continue; // skip distant lights
            int radius = emission.Value.Radius;
            if (stype is TileType.Shrine or TileType.Fountain or TileType.EnchantShrine)
                radius = Math.Max(2, radius + pulseDelta);
            // A grouped lava source stands for several tiles, so it reaches further — but at the
            // SAME brightness. A wider fire, not a brighter one: scaling the colour by the count
            // is what produced the blown-out white field this grouping exists to remove.
            else if (count > 1)
                radius = Math.Min(MaxSourceRadius, radius + Math.Min(3, (count - 1) / 2));
            // Dimmed by daylight like the torch, but on a higher floor: a campfire or a lava pool
            // is something the player navigates BY, so it keeps a readable glow at noon where a
            // carried flame does not.
            float k = DayNightCycle.EmissiveInfluence;
            var c = emission.Value.Color;
            // A GROUPED source does not get a softened edge, and that is not a cost dodge.
            // The penumbra exists where ONE light defines a boundary — a torch behind a corner,
            // a campfire alone in a room. A lava group is several overlapping lights in the open,
            // whose edges already blend into each other under the screen combine, so softening
            // them buys an indistinguishable picture for a real share of the turn.
            AddSource(map, sx, sy, radius, LightRgb.Of(c.R * k, c.G * k, c.B * k),
                      softenEdges: count == 1);
            sources++;
        }
        Profiler.RecordCount("Lighting.EmissiveCount", emissiveCount);
        SourcesLastUpdate = sources;
    }

    // Only fill ambient in a region around the player — not the entire map.
    //
    // Flat, this is the sky and nothing else: one colour per cell, so a tile under a canopy came
    // out as bright as the clearing beside it. LightingSystem.Shading.cs adds the direction — a
    // sun bearing that lays graded shadows away from anything blocking sight, plus contact
    // shading in corners — and it writes into the same grid, so every consumer downstream reads
    // one lighting model rather than two.
    private void FillAmbientRegion(GameMap map, int px, int py)
    {
        using var _ = Profiler.Begin("Lighting.FillAmbient");
        var (ar, ag, ab) = DayNightCycle.Ambient;
        var ambient = LightRgb.Of(ar, ag, ab);
        int rows = ShadingRowReach;
        int x0 = Math.Max(0, px - LightingRoiRadius);
        int x1 = Math.Min(Width, px + LightingRoiRadius);
        int y0 = Math.Max(0, py - rows);
        int y1 = Math.Min(Height, py + rows);
        if (x1 <= x0 || y1 <= y0) return;

        if (ShadingEnabled)
        {
            FillShadedRegion(map, ambient, x0, y0, x1, y1);
            return;
        }

        var light = _light.AsSpan();
        for (int y = y0; y < y1; y++)
            light.Slice(y * Width + x0, x1 - x0).Fill(ambient);
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
        // Smoothstep rather than the raw linear ramp. Linear falloff ends in a hard rim — the
        // torch pool stopped at a visible circular edge one step wide — because the value is
        // still falling at full rate when it reaches zero. This flattens both ends: a steadier
        // core, and an edge that fades into the ambient instead of meeting it.
        falloff = falloff * falloff * (3f - 2f * falloff);
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

    private void AddSource(GameMap map, int sx, int sy, int radius, LightRgb color,
                           bool softenEdges)
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
        if (softenEdges) SoftenSourceEdges(map, sx, sy, radius);

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
                ref LightRgb cell = ref _light[y * Width + x];
                cell.R = Screen(cell.R, sr);
                cell.G = Screen(cell.G, sg);
                cell.B = Screen(cell.B, sb);
                _scratchR[idx] = 0; _scratchG[idx] = 0; _scratchB[idx] = 0;
            }
        }
    }

    // ── The penumbra on a point light ─────────────────────────────────────────────────────────
    // The shadowcaster answers one question per cell — lit or not — so the edge of a torch pool
    // behind a wall corner was a hard binary line. That is the same blockiness the sun's sweep
    // was built to remove, and at night it is the ONLY edge on screen, because the torch is all
    // there is to see by.
    //
    // Softened by dilating the shadowed side: a dark cell takes a share of the brightest
    // ORTHOGONAL neighbour, twice, so the boundary becomes a two-step gradient instead of a
    // step. Two rules keep it honest. Only transparent cells give or receive, so light rounds a
    // corner — which is what a penumbra is — and can never cross a wall one tile thick. And it
    // only ever RAISES a value, so the lit side keeps its full brightness; an averaging blur
    // would have dimmed the pool to buy the soft edge.
    // Only sources big enough to throw a boundary anyone can SEE. A floor carries 100-260
    // emissive tiles and most of them are lava at radius 5, each one its own point light: their
    // rims are already near zero under the smoothstep falloff and they overlap each other, so
    // softening them buys nothing and costs the pass two hundred times over. What reads as a
    // hard edge is the torch you see by and a campfire alone in a dark room.
    private const int PenumbraMinRadius = 6;

    private const float PenumbraShare = 0.55f;
    private const int PenumbraPasses = 2;

    private float[]? _softR, _softG, _softB;
    private bool[]? _softOpen;

    private void SoftenSourceEdges(GameMap map, int sx, int sy, int radius)
    {
        if (radius < PenumbraMinRadius) return;
        using var _ = Profiler.Begin("Lighting.Penumbra");
        _softR ??= new float[ScratchLen];
        _softG ??= new float[ScratchLen];
        _softB ??= new float[ScratchLen];
        _softOpen ??= new bool[ScratchLen];

        // Work over the source's own box rather than the whole 33x33 scratch, and keep one cell
        // of slack at each edge so the four neighbour reads are in range by construction.
        int lo = Math.Max(1, MaxSourceRadius - radius);
        int hi = Math.Min(ScratchStride - 2, MaxSourceRadius + radius);
        if (hi <= lo) return;

        // THE RING AROUND THE BOX IS MARKED CLOSED, and that is load-bearing rather than tidy:
        // the passes below only write inside the box, so a neighbour just outside it still holds
        // whatever the PREVIOUS source left there. Closed cells are never read, which turns a
        // stale-halo bug into a boundary condition.
        var op = map.OpacityTable;
        var open = _softOpen;
        for (int ly = lo - 1; ly <= hi + 1; ly++)
        {
            int wy = ly + _srcOriginY;
            bool rowInside = ly >= lo && ly <= hi && (uint)wy < (uint)Height;
            int rowBase = ly * ScratchStride;
            int mapRow = wy * Width;
            for (int lx = lo - 1; lx <= hi + 1; lx++)
            {
                int wx = lx + _srcOriginX;
                open[rowBase + lx] = rowInside && lx >= lo && lx <= hi
                                     && (uint)wx < (uint)Width && !op[mapRow + wx];
            }
        }

        // Ping-pong between the two buffer sets instead of copying rows back. Every cell of the
        // box is written on every pass, so the destination needs no clearing, and an EVEN number
        // of passes leaves the result in the scratch the merge below reads.
        float[] sr = _scratchR!, sg = _scratchG!, sb = _scratchB!;
        float[] dr = _softR, dg = _softG, db = _softB;

        for (int pass = 0; pass < PenumbraPasses; pass++)
        {
            for (int ly = lo; ly <= hi; ly++)
            {
                int rowBase = ly * ScratchStride;
                for (int lx = lo; lx <= hi; lx++)
                {
                    int i = rowBase + lx;
                    float vR = sr[i], vG = sg[i], vB = sb[i];
                    if (open[i])
                    {
                        float bR = 0f, bG = 0f, bB = 0f;
                        int w = i - 1, e = i + 1, n = i - ScratchStride, s = i + ScratchStride;
                        if (open[w]) { if (sr[w] > bR) bR = sr[w]; if (sg[w] > bG) bG = sg[w]; if (sb[w] > bB) bB = sb[w]; }
                        if (open[e]) { if (sr[e] > bR) bR = sr[e]; if (sg[e] > bG) bG = sg[e]; if (sb[e] > bB) bB = sb[e]; }
                        if (open[n]) { if (sr[n] > bR) bR = sr[n]; if (sg[n] > bG) bG = sg[n]; if (sb[n] > bB) bB = sb[n]; }
                        if (open[s]) { if (sr[s] > bR) bR = sr[s]; if (sg[s] > bG) bG = sg[s]; if (sb[s] > bB) bB = sb[s]; }

                        bR *= PenumbraShare; bG *= PenumbraShare; bB *= PenumbraShare;
                        if (bR > vR) vR = bR;
                        if (bG > vG) vG = bG;
                        if (bB > vB) vB = bB;
                    }
                    dr[i] = vR; dg[i] = vG; db[i] = vB;
                }
            }

            (sr, dr) = (dr, sr);
            (sg, dg) = (dg, sg);
            (sb, db) = (db, sb);
        }

        _scratchR = sr; _scratchG = sg; _scratchB = sb;
        _softR = dr; _softG = dg; _softB = db;
    }

    // ADD-AND-CLAMP THREW AWAY EVERY OVERLAP, AND ON LAVA THAT WAS MOST OF THE PICTURE. Summing
    // sources and clamping at 255 means the second light reaching a cell already at 200 is worth
    // 55 and the third is worth nothing — so around a cluster of lava, fifty-odd cells sat at
    // EXACTLY 255 with no falloff between them and the map read as a white hole.
    //
    // Screen blending is the combine that belongs in a space with a ceiling: each light darkens
    // what is left of the headroom rather than spending it, so the result approaches 255 without
    // ever reaching it and the gradient survives however many lights overlap. It is also
    // commutative and order-independent, which the source loop needs since nothing sorts it.
    private static float Screen(float a, float b) => 255f - (255f - a) * (255f - b) * (1f / 255f);

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
