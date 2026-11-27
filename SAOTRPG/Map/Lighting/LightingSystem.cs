
using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.Map.Lighting;

// Per-tile RGB light. Shadowcaster-driven FOV per emissive tile (+ player torch), smoothstep
// falloff, combined by screen blending rather than by adding and clamping. Owned by GameMap;
// recomputed on UpdateVisibility, and cached between turns by LightingSystem.Cache.cs.
public sealed partial class LightingSystem
{
    // Accumulated light at a tile, stored as float RGB so combining lights does not round per step.
    public struct LightRgb
    {
        public float R, G, B;
        public static LightRgb Of(float r, float g, float b) => new() { R = r, G = g, B = b };
    }

    public int Width { get; }
    public int Height { get; }

    // Nothing in this file may read a wall clock. The field is written by Update, which is
    // reached from GameMap.UpdateVisibility and nowhere else, only when a turn passes, so a
    // wall-clock term here would be sampled at turn boundaries whatever rate it claims, and
    // frozen for a player who has stopped walking. Making such a term live would force the frame
    // to repaint at the animation's rate for as long as any light is on screen (always: the
    // player carries one), and MapView's only light-related invalidator is the turn stamp, so
    // light moving without the turn would go stale on the frame-blit path. A turn-driven field is
    // also deterministic for a given seed.
    //
    // ── Headroom: a light may drive a cell past its own colour ────────────────────────────
    // LightFull is the light value at which ApplyLighting renders a tile at exactly its authored
    // colour (fg = base x light / LightFull). Everything in this block exists so that a value
    // above it is reachable: without headroom there is no such thing as bright, only less dark,
    // and standing in torchlight produces the same pixels as standing in full ambient noon.
    //
    // Two halves, and neither works alone. The combine's ceiling is LightCeiling, so overlapping
    // lights approach 510 rather than 255; a lone source is untouched, because Screen(0, b) = b
    // at any ceiling, which leaves open daylight unchanged. And a source emits above full,
    // because the combine cannot exceed its own inputs by much when one of them is already near
    // the ceiling.
    //
    // There is no second soft knee above full: the combine is already a soft knee and cannot
    // pass LightCeiling, so a second knee on an already-bounded quantity would only spend the
    // headroom the first one leaves.
    public const float LightFull = 255f;

    // Must stay above SourceOverdrive x the brightest emission: Screen(a, b) = a + b - ab/C has
    // slope (1 - b/C) in a, so an input past the ceiling makes the combine decrease as more light
    // arrives. That is a gradient inversion, not a clamp.
    public const float LightCeiling = LightFull * 2f;

    // How far past full a source emits at full strength. 1.6 puts a midnight torch's own cell at
    // about 1.4x the tile's authored colour and a campfire's at about 1.6x, while a noon torch
    // stays near ambient (about 1.1x).
    private const float SourceOverdrive = 1.6f;

    // Warm-white torch colour.
    private const float TorchBaseR = 220f, TorchBaseG = 200f, TorchBaseB = 160f;

    // The largest channel any source can put into the grid, overdrive included. Derived from the
    // emission table and the torch, so a new emissive type or a brighter torch is covered without
    // editing a list. Read by the verification probes.
    public static float MaxSourceEmission { get; } = ComputeMaxSourceEmission();

    private static float ComputeMaxSourceEmission()
    {
        float m = MathF.Max(TorchBaseR, MathF.Max(TorchBaseG, TorchBaseB));
        foreach (TileType t in Enum.GetValues<TileType>())
        {
            var e = GetEmission(t);
            if (e == null) continue;
            var c = e.Value.Color;
            m = MathF.Max(m, MathF.Max(c.R, MathF.Max(c.G, c.B)));
        }
        return m * SourceOverdrive;
    }

    // Ambient via DayNightCycle.Ambient. The player casts no light of their own: the carried
    // light is whatever an equipped item declares (EquipmentBase.LightRadius), and a player
    // carrying none walks in whatever the sky gives them. The flame still shrinks with
    // TorchInfluence as the sun climbs, which is what stops it being a searchlight at noon.
    //
    // The sky is not overdriven: ambient tops out at (180, 175, 150), so open daylight renders
    // every tile at 0.71x its authored colour, and the banding, vignette and sight-edge tuning
    // all assume that. The headroom belongs to whatever is meant to read as a light source.
    private static LightRgb TorchColor()
    {
        // Scaled by how dark it is, so at noon the flame is no brighter than the daylight around
        // it and does not wash out the sun shadows around the player.
        float k = DayNightCycle.TorchInfluence * SourceOverdrive;
        return LightRgb.Of(TorchBaseR * k, TorchBaseG * k, TorchBaseB * k);
    }

    // Shrinks with the flame. A dim torch that still reached twelve tiles would read as a hole
    // in the daylight rather than as a flame; this also cuts the midday shadowcast.
    // `full` is the equipped item's own radius. Reach does not animate: a ring of cells flips
    // between lit and unlit whenever it moves.
    // The daylight floor is a third of the item's reach rather than a flat value, so a bigger
    // lantern stays bigger at noon; a flat floor would make every tier identical in daylight.
    private static int TorchRadius(int full)
    {
        int floorReach = Math.Max(2, full / 3);
        return Math.Max(floorReach,
            (int)MathF.Round(floorReach + (full - floorReach) * DayNightCycle.TorchInfluence));
    }

    // Row-major (y*Width + x), so the three hot loops (the ambient refill, AddSource's merge,
    // and the map tile loop's per-cell read) are contiguous walks and the refill can go through
    // Span.Fill.
    //
    // The picture depends on one RGB value per logical tile, multiplied onto cells in
    // ApplyLighting. Interpolating between tile centres would be a different look: the hard tile
    // edges are what make light read as roguelike illumination. The layout stays row-major to
    // match every other per-cell grid.
    private readonly LightRgb[] _light;

    // Emissive coords come from GameMap.EmissiveSourcesNear, which hands back lava already
    // grouped. Nothing is cached on this side: GetEmission is a switch over the tile type, so a
    // retuned lamp takes effect the next turn with nothing to invalidate.

    public LightingSystem(int width, int height)
    {
        Width = width;
        Height = height;
        _light = new LightRgb[width * height];
        // Fill entire map with default ambient once at construction.
        var (ar, ag, ab) = DayNightCycle.Ambient;
        _light.AsSpan().Fill(LightRgb.Of(ar, ag, ab));
    }

    // Used by the verification probes to mark a cell unwritten, since a cell the fill never
    // reached keeps whatever was in the grid and looks like light. Not a general setter: it
    // writes one fixed value and nothing else, so it cannot be used to fake a light level.
    private const float UnwrittenSentinel = -12345f;

    public void MarkUnwrittenForVerification(int x, int y) =>
        _light[y * Width + x] = LightRgb.Of(UnwrittenSentinel, UnwrittenSentinel, UnwrittenSentinel);

    // Any negative value, not the sentinel's own: AddSource screens a source onto whatever is in
    // the cell, and Screen(-12345, b) lands between -12345 and -2061 for every emission this game
    // can produce, so an unfilled cell inside a light pool holds some other negative value rather
    // than the sentinel itself. Nothing else in the field is ever negative: ambient is
    // non-negative, the sweep kernel is a partition of unity so `arriving` is at most 1, the
    // per-channel loss is at most the ambient it is subtracted from, and Screen of two
    // non-negatives is non-negative.
    public bool IsUnwritten(int x, int y) => _light[y * Width + x].R < 0f;

    // Fast path for render loops that have already done a bounds check.
    public LightRgb GetLightUnchecked(int x, int y) => _light[y * Width + x];

    // How many point lights the last Update added: the torch plus every emissive source that
    // survived the reach gates. GameMap.UpdateVisibility reads it to tell whether any placed
    // light is in reach; the verification probes read it too.
    public int SourcesLastUpdate { get; private set; }

    // How many times the field was evaluated since the last drain. Read by the verification
    // probes.
    public static int UpdatesRun { get; private set; }

    public static int DrainUpdatesRun()
    {
        int n = UpdatesRun;
        UpdatesRun = 0;
        return n;
    }

    // ROI radius matches active FOV so all visible tiles are lit (FOV 4× the viewport).
    private static int LightingRoiRadius => DayNightCycle.FovRadius + 10;

    // How many steps of upwind history a shadow needs before it is indistinguishable from one
    // swept from infinity: the smallest m for which `ambient x strength x decay^m` falls under
    // the renderer's colour quantizer half step, across every sky state (the factors' worst cases
    // never co-occur: a slow decay only arrives with a weak shadow under a dim sky, because both
    // ramp through twilight).
    //
    // It only widens Y, so only the Y-axis requirement justifies it. The sweep steps along the
    // sky body's major axis, so a state charges only the axis its own bearing runs along, and a
    // near-vertical bearing belongs to a high body, whose shadow decays fast; the X extent is the
    // full ROI either way. 45 rows is the history the Y axis needs at LightQuantizeStep 2, met by
    // the swept region alone rather than by leaning on CachePad. It must be re-derived if
    // LightQuantizeStep or DayNightCycle's shadow dials move.
    private const int ShadowHistorySteps = 45;

    // How far above or below the player the ground has to be lit at all.
    //
    // Same argument as EmissiveRowReach below, one level up: light is only read for cells the
    // tile loop draws, the camera is player-centred and unclamped, and the viewport is wide and
    // short, so the rows worth filling are half a viewport height plus enough history for the
    // shadows arriving at them to be right. The horizontal extent stays the full ROI because
    // viewport width is not derivable from anything the lighting can see.
    //
    // Two things this must clear: the drawn rows (FovRadius / FovMultiplier is h/2 + 2), and the
    // rows a light source may write into, which is EmissiveRowReach + MaxSourceRadius; filling
    // less than that would leave a source screening onto ambient nobody refreshed. The history
    // term covers the sources at every viewport size; the second arm binds only if
    // ShadowHistorySteps falls below 24.
    //
    // There is no such floor in X: FillAmbientRegion stops at px + ROI while the loop above
    // admits a source at dx = ROI exactly, so that source writes up to 32 columns past the fill.
    // Nothing draws them (a visible cell is inside FovRadius, which is ROI - 10), so those
    // columns are left unfilled.
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

    // How many rows above and below the player the sweep fills, and how many of them the
    // renderer draws. Read by the verification probes.
    public static int ShadowRowReach => ShadingRowReach;
    public static int DrawnRowReach => DayNightCycle.FovRadius / DayNightCycle.FovMultiplier;

    // How far above or below the player a light source can still matter.
    //
    // The ROI above is a square, but a source only changes cells within its own radius, and a
    // cell is only read if the renderer draws it, and the drawn region is a wide, short
    // viewport. `Camera.CenterOn` does not clamp, so a drawn cell is always within half a
    // viewport height of the player; MapView derives FovRadius from exactly that height as
    // (h/2 + 2) * FovMultiplier, so the row reach falls out here with nothing new having to be
    // published from the view.
    //
    // It matters because a floor is lit per tile and lava is a tile, so a square ROI would run a
    // shadowcast for every lava group across it. The horizontal reach stays the full ROI because
    // viewport width is not derivable from anything the lighting can see, and the 8-cell pad is
    // there so a wider FovMultiplier or an odd viewport height cannot quietly clip a light off
    // the top of the screen.
    private static int EmissiveRowReach =>
        DayNightCycle.FovRadius / DayNightCycle.FovMultiplier + MaxSourceRadius + 8;

    // Bumped when an Update's inputs differ from the last one's, so an unchanged stamp means an
    // unchanged field: sight past the night radius reads the field and keys its reuse on this.
    public int FieldStamp { get; private set; }
    private (GameMap?, SkyShade, float, int, bool, bool, int, int, int, int) _stampKey;

    // The region the last Update wrote, half-open; outside it the grid holds an earlier turn.
    public (int X0, int Y0, int X1, int Y1) LitRegion { get; private set; }

    // `carriedLightRadius` is the widest radius the player's equipped gear declares. Zero is
    // the ordinary case for a player with no light: no source is added at all, and the only
    // thing lighting them is the sky.
    public void Update(GameMap map, int playerX, int playerY, int carriedLightRadius = 0)
    {
        using var _ = Profiler.Begin("Lighting.Update");
        UpdatesRun++;
        var key = (map, DayNightCycle.ShadeAt(DayNightCycle.CurrentTurn), DayNightCycle.QuantizedSunLevel,
                   map.SightVersion, map.HasSky, ShadingEnabled,
                   DayNightCycle.FovRadius, playerX, playerY, carriedLightRadius);
        if (key != _stampKey) { _stampKey = key; FieldStamp++; }
        LitRegion = Roi(playerX, playerY);
        FillAmbientRegion(map, playerX, playerY);
        int sources = 0;
        if (carriedLightRadius > 0)
        {
            AddSource(map, playerX, playerY, TorchRadius(carriedLightRadius), TorchColor(),
                      softenEdges: true);
            sources = 1;
        }

        int roiSq = LightingRoiRadius * LightingRoiRadius;
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
            // Whether a source gets a soft edge is decided from its base radius, which is why
            // this is read before the grouping below widens it. A landmark's edge softness must
            // not depend on how many tiles happened to land in its group.
            bool bigEnoughToSoften = radius >= PenumbraMinRadius;
            // A grouped lava source stands for several tiles, so it reaches further — but at the
            // same brightness. A wider fire, not a brighter one: scaling the colour by the count
            // would bring back the blown-out white field this grouping exists to prevent.
            if (count > 1)
                radius = Math.Min(MaxSourceRadius, radius + Math.Min(3, (count - 1) / 2));
            // Dimmed by daylight like the torch, but on a higher floor: a campfire or a lava pool
            // is something the player navigates by, so it keeps a readable glow at noon where a
            // carried flame does not.
            float k = DayNightCycle.EmissiveInfluence * SourceOverdrive;
            var c = emission.Value.Color;
            // A grouped source does not get a softened edge. The penumbra matters where one
            // light defines a boundary — a torch behind a corner, a campfire alone in a room. A
            // lava group is several overlapping lights in the open, whose edges already blend
            // into each other under the screen combine, so softening them would cost a share of
            // the turn for an indistinguishable picture.
            AddSource(map, sx, sy, radius, LightRgb.Of(c.R * k, c.G * k, c.B * k),
                      softenEdges: count == 1 && bigEnoughToSoften, isStatic: true);
            sources++;
        }
        Profiler.RecordCount("Lighting.EmissiveCount", emissiveCount);
        SourcesLastUpdate = sources;
    }

    // Only fill ambient in a region around the player — not the entire map.
    //
    // The flat fill is the sky alone, one colour per cell. LightingSystem.Shading.cs adds the
    // direction — a sun bearing that lays graded shadows away from anything blocking sight, plus
    // contact shading in corners — and writes into the same grid, so every consumer downstream
    // reads one lighting model rather than two.
    private void FillAmbientRegion(GameMap map, int px, int py)
    {
        using var _ = Profiler.Begin("Lighting.FillAmbient");
        var (x0, y0, x1, y1) = Roi(px, py);
        if (x1 <= x0 || y1 <= y0) return;

        if (ShadingEnabled)
        {
            // Through the cache rather than straight to the sweep: the field is a pure function
            // of the map and the sky, so most turns can restore it instead of recomputing it.
            FillFromCache(map, px, py, x0, y0, x1, y1);
            return;
        }

        // The flat fill overwrites what the cache describes, and a later hit re-bases only the
        // source boxes — so the cache cannot survive this path.
        DropCache();
        var (ar, ag, ab) = DayNightCycle.Ambient;
        var ambient = LightRgb.Of(ar, ag, ab);
        var light = _light.AsSpan();
        for (int y = y0; y < y1; y++)
            light.Slice(y * Width + x0, x1 - x0).Fill(ambient);
    }

    // The region lit around a player standing at (px, py), clipped to the map.
    private (int X0, int Y0, int X1, int Y1) Roi(int px, int py)
    {
        int rows = ShadingRowReach;
        return (Math.Max(0, px - LightingRoiRadius), Math.Max(0, py - rows),
                Math.Min(Width, px + LightingRoiRadius), Math.Min(Height, py + rows));
    }

    // Scratch per-source — diagonals visited by two quadrants don't double-light. A source's
    // radius counts rows; a cell is Aspect times taller than wide, so it reaches Aspect x radius
    // columns and the pool renders round, as sight does (see Shadowcaster). The box is
    // (2 x MaxSourceReachX + 1) columns by (2 x MaxSourceRadius + 1) rows, local = world - origin.
    private float[]? _scratchR, _scratchG, _scratchB;
    private const int MaxSourceRadius = 16;
    private const int Aspect = (int)Generation.FloorMask.CellAspectRatio;
    private const int MaxSourceReachX = Aspect * MaxSourceRadius;
    private const int ScratchStride = MaxSourceReachX * 2 + 1;
    private const int ScratchRows = MaxSourceRadius * 2 + 1;
    private const int ScratchLen = ScratchStride * ScratchRows;

    // The per-cell reveal for one source, as a struct sink so the shadowcaster inlines it rather
    // than making an indirect call per revealed cell.
    //
    // The falloff is computed in columns, so it is the shadowcaster's own aspect-corrected
    // ellipse: d2 = dx^2 + (Aspect dy)^2 against (Aspect r)^2 admits exactly the cells the cast
    // reveals bar its rim, where the falloff is 0. A circle in cells would draw as an oval twice
    // as tall as wide on a 2:1 cell and leave the cast's outer quarter unlit.
    private struct SourceSink : Shadowcaster.IFovSink
    {
        public int Sx, Sy, RadiusSq;
        public float ColorR, ColorG, ColorB, InvRadius;
        public float[] R, G, B;

        public void Reveal(int x, int y)
        {
            int dx = x - Sx, dy = y - Sy;
            int d2 = dx * dx + Aspect * Aspect * dy * dy;
            if (d2 >= RadiusSq) return;
            float falloff = 1f - SqrtLut[d2] * InvRadius;
            if (falloff <= 0f) return;
            // Smoothstep rather than the raw linear ramp. Linear falloff ends in a hard rim — a
            // visible circular edge one step wide — because the value is still falling at full
            // rate when it reaches zero. This flattens both ends: a steadier
            // core, and an edge that fades into the ambient instead of meeting it.
            falloff = falloff * falloff * (3f - 2f * falloff);
            // d2 < RadiusSq <= MaxSourceReachX^2 keeps |dx| under MaxSourceReachX and |dy| under
            // MaxSourceRadius, so the local index is inside the scratch box by construction.
            int idx = (dy + MaxSourceRadius) * ScratchStride + (dx + MaxSourceReachX);
            float cr = ColorR * falloff, cg = ColorG * falloff, cb = ColorB * falloff;
            if (cr > R[idx]) R[idx] = cr;
            if (cg > G[idx]) G[idx] = cg;
            if (cb > B[idx]) B[idx] = cb;
        }
    }

    // A region buffer grows geometrically, never to exactly what this frame needs. The padded
    // region un-clips one row or one column at a time as the player walks away from a map edge,
    // so sizing to the exact need would reallocate a multi-megabyte LOH array every few steps for
    // the first several hundred steps of a floor. Doubling settles each buffer after a handful
    // of growths. Only [0, need) is ever indexed, so slack at the end is inert.
    private static T[] EnsureCapacity<T>(T[]? buf, int need)
    {
        if (buf != null && buf.Length >= need) return buf;
        int grown = buf == null ? need : Math.Max(need, buf.Length * 2);
        return new T[grown];
    }

    private void AddSource(GameMap map, int sx, int sy, int radius, LightRgb color,
                           bool softenEdges, bool isStatic = false)
    {
        using var _ = Profiler.Begin("Lighting.AddSource");
        if (radius > MaxSourceRadius) radius = MaxSourceRadius;
        if (isStatic && CacheStaticLights)
        {
            MergeStatic(StaticLightFor(map, sx, sy, radius, color, softenEdges));
            return;
        }
        CastIntoScratch(map, sx, sy, radius, color, softenEdges);
        int originX = sx - MaxSourceReachX, originY = sy - MaxSourceRadius;

        // Additively merge scratch into grid, then clear scratch (local→world coord remap).
        int x0 = Math.Max(0, sx - Aspect * radius), x1 = Math.Min(Width, sx + Aspect * radius + 1);
        int y0 = Math.Max(0, sy - radius), y1 = Math.Min(Height, sy + radius + 1);
        // The cache re-bases exactly this box next turn; the penumbra only ever writes inside it.
        NoteSourceBox(x0, y0, x1, y1);
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            int idx = (y - originY) * ScratchStride + (x - originX);
            float sr = _scratchR![idx], sg = _scratchG![idx], sb = _scratchB![idx];
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

    // Shadowcast + penumbra for one source into the scratch box centred on (sx, sy).
    private void CastIntoScratch(GameMap map, int sx, int sy, int radius, LightRgb color,
                                 bool softenEdges)
    {
        // Lazy-allocate the scratch box (MaxSourceReachX by MaxSourceRadius). Reused across calls.
        if (_scratchR == null)
        {
            _scratchR = new float[ScratchLen];
            _scratchG = new float[ScratchLen];
            _scratchB = new float[ScratchLen];
        }
        // Scratch takes the max per tile — overlapping quadrant boundaries don't double-add.
        // A non-positive radius reveals the origin alone and lights it at full strength, which
        // is what the 1-and-0 pair below preserves.
        var sink = new SourceSink
        {
            Sx = sx, Sy = sy,
            RadiusSq = radius > 0 ? Aspect * radius * Aspect * radius : 1,
            InvRadius = radius > 0 ? 1f / (Aspect * radius) : 0f,
            ColorR = color.R, ColorG = color.G, ColorB = color.B,
            R = _scratchR, G = _scratchG!, B = _scratchB!,
        };
        Shadowcaster.Compute(sx, sy, radius, map.OpacityTable, Width, Height, ref sink);
        if (softenEdges)
            SoftenSourceEdges(map, radius, sx - MaxSourceReachX, sy - MaxSourceRadius);
    }

    // ── Static lights ─────────────────────────────────────────────────────────────────────────
    // An emissive tile does not move, so what it casts is a pure function of its position,
    // radius, colour, softening and the map's opacity. It is cast once and replayed until one of
    // those changes; SightVersion moves on every opacity change, a lamp's colour on every
    // quantized sun step. The torch moves with the player and is never cached.
    // Off replays the uncached path. Toggled, and the counters read, by the verification probes.
    public static bool CacheStaticLights { get; set; } = true;
    public static int StaticLightHits { get; private set; }
    public static int StaticLightCasts { get; private set; }

    private sealed class StaticLight
    {
        public int Radius, Version, X0, Y0, W, H;
        public float R, G, B;
        public bool Soft;
        public float[] LR = [], LG = [], LB = [];
    }

    private readonly Dictionary<int, StaticLight> _staticLights = new();
    private GameMap? _staticLightsMap;

    private StaticLight StaticLightFor(GameMap map, int sx, int sy, int radius, LightRgb color,
                                       bool soft)
    {
        if (!ReferenceEquals(_staticLightsMap, map)) { _staticLights.Clear(); _staticLightsMap = map; }
        int key = sy * Width + sx;
        if (_staticLights.TryGetValue(key, out var e) && e.Version == map.SightVersion
            && e.Radius == radius && e.Soft == soft
            && e.R == color.R && e.G == color.G && e.B == color.B)
        {
            StaticLightHits++;
            return e;
        }
        StaticLightCasts++;
        e ??= _staticLights[key] = new StaticLight();
        e.Version = map.SightVersion; e.Radius = radius; e.Soft = soft;
        e.R = color.R; e.G = color.G; e.B = color.B;
        e.X0 = Math.Max(0, sx - Aspect * radius); e.Y0 = Math.Max(0, sy - radius);
        e.W = Math.Min(Width, sx + Aspect * radius + 1) - e.X0;
        e.H = Math.Min(Height, sy + radius + 1) - e.Y0;
        int n = e.W * e.H;
        if (e.LR.Length < n) { e.LR = new float[n]; e.LG = new float[n]; e.LB = new float[n]; }

        CastIntoScratch(map, sx, sy, radius, color, soft);
        int originX = sx - MaxSourceReachX, originY = sy - MaxSourceRadius;
        for (int y = 0; y < e.H; y++)
        {
            int src = (e.Y0 + y - originY) * ScratchStride + (e.X0 - originX);
            Array.Copy(_scratchR!, src, e.LR, y * e.W, e.W);
            Array.Copy(_scratchG!, src, e.LG, y * e.W, e.W);
            Array.Copy(_scratchB!, src, e.LB, y * e.W, e.W);
            Array.Clear(_scratchR!, src, e.W);
            Array.Clear(_scratchG!, src, e.W);
            Array.Clear(_scratchB!, src, e.W);
        }
        return e;
    }

    // The same merge as AddSource's, read from the cached box.
    private void MergeStatic(StaticLight e)
    {
        NoteSourceBox(e.X0, e.Y0, e.X0 + e.W, e.Y0 + e.H);
        for (int y = 0; y < e.H; y++)
        {
            int row = (e.Y0 + y) * Width + e.X0, li = y * e.W;
            for (int x = 0; x < e.W; x++, li++)
            {
                float sr = e.LR[li], sg = e.LG[li], sb = e.LB[li];
                if (sr > 0 || sg > 0 || sb > 0)
                {
                    ref LightRgb cell = ref _light[row + x];
                    cell.R = Screen(cell.R, sr);
                    cell.G = Screen(cell.G, sg);
                    cell.B = Screen(cell.B, sb);
                }
            }
        }
    }

    // ── The penumbra on a point light ─────────────────────────────────────────────────────────
    // The shadowcaster answers one question per cell — lit or not — so the edge of a torch pool
    // behind a wall corner would be a hard binary line, and at night it is the only edge on
    // screen, because the torch is all there is to see by.
    //
    // Softened by dilating the shadowed side: on each pass a dark cell takes a share of the
    // brightest orthogonal neighbour, so the boundary becomes a gradient instead of a step. Two
    // rules keep it honest. Only transparent cells give or receive, so light rounds a corner —
    // which is what a penumbra is — and can never cross a wall one tile thick. And it only ever
    // raises a value, so the lit side keeps its full brightness; an averaging blur would dim the
    // pool to buy the soft edge.
    // Only sources big enough to throw a boundary anyone can see. Most emissive tiles are lava
    // at radius 5: their rims are already near zero under the smoothstep falloff and they overlap
    // each other, so softening them buys nothing and would run the pass once per tile. What
    // reads as a hard edge is the torch you see by and a campfire alone in a dark room.
    private const int PenumbraMinRadius = 6;

    private const float PenumbraShare = 0.55f;
    private const int PenumbraPasses = 4;

    private float[]? _softR, _softG, _softB;
    private bool[]? _softOpen;

    private void SoftenSourceEdges(GameMap map, int radius, int originX, int originY)
    {
        if (radius < PenumbraMinRadius) return;
        using var _ = Profiler.Begin("Lighting.Penumbra");
        _softR ??= new float[ScratchLen];
        _softG ??= new float[ScratchLen];
        _softB ??= new float[ScratchLen];
        _softOpen ??= new bool[ScratchLen];

        // Work over the source's own box rather than the whole scratch, and keep one cell of
        // slack at each edge so the four neighbour reads are in range by construction.
        int loX = Math.Max(1, MaxSourceReachX - Aspect * radius);
        int hiX = Math.Min(ScratchStride - 2, MaxSourceReachX + Aspect * radius);
        int loY = Math.Max(1, MaxSourceRadius - radius);
        int hiY = Math.Min(ScratchRows - 2, MaxSourceRadius + radius);
        if (hiX <= loX || hiY <= loY) return;

        // The ring around the box is marked closed: the passes below only write inside the box,
        // so a neighbour just outside it still holds whatever the previous source left there,
        // and closed cells are never read.
        var op = map.OpacityTable;
        var open = _softOpen;
        for (int ly = loY - 1; ly <= hiY + 1; ly++)
        {
            int wy = ly + originY;
            bool rowInside = ly >= loY && ly <= hiY && (uint)wy < (uint)Height;
            int rowBase = ly * ScratchStride;
            int mapRow = wy * Width;
            for (int lx = loX - 1; lx <= hiX + 1; lx++)
            {
                int wx = lx + originX;
                open[rowBase + lx] = rowInside && lx >= loX && lx <= hiX
                                     && (uint)wx < (uint)Width && !op[mapRow + wx];
            }
        }

        // Ping-pong between the two buffer sets instead of copying rows back. Every cell of the
        // box is written on every pass, so the destination needs no clearing, and an even number
        // of passes leaves the result in the scratch the merge below reads. Four passes make the
        // edge a four-step ramp (0.55, 0.30, 0.17, 0.09 of the lit side).
        float[] sr = _scratchR!, sg = _scratchG!, sb = _scratchB!;
        float[] dr = _softR, dg = _softG, db = _softB;

        for (int pass = 0; pass < PenumbraPasses; pass++)
        {
            for (int ly = loY; ly <= hiY; ly++)
            {
                int rowBase = ly * ScratchStride;
                for (int lx = loX; lx <= hiX; lx++)
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

    // Screen blending rather than add-and-clamp. Summing sources and clamping at 255 means the
    // second light reaching a cell already at 200 is worth 55 and the third is worth nothing, so
    // a cluster of overlapping lights flattens to the ceiling with no falloff between them.
    //
    // Screen blending is the combine that belongs in a space with a ceiling: each light darkens
    // what is left of the headroom rather than spending it, so the result approaches the ceiling
    // without ever reaching it and the gradient survives however many lights overlap. It is also
    // commutative and order-independent, which the source loop needs since nothing sorts it.
    //
    // The ceiling is LightCeiling, not LightFull, which is the field-side half of the headroom.
    // A lone source lands where it would under any ceiling, since Screen(0, b) = b; only cells
    // that two lights both reach use the extra room.
    private static float Screen(float a, float b) =>
        LightCeiling - (LightCeiling - a) * (LightCeiling - b) * (1f / LightCeiling);

    // sqrt LUT for integer d² in columns. SourceSink returns before the lookup for any d2 at or
    // past the source's own (Aspect r)^2, so the largest index ever taken is MaxSourceReachX^2 - 1
    // and the size is derived rather than guessed. There is no fallback: an index past the end
    // would throw, which is the loud form of a source wider than the table.
    private const int SqrtLutSize = MaxSourceReachX * MaxSourceReachX;
    private static readonly float[] SqrtLut = BuildSqrtLut();
    private static float[] BuildSqrtLut()
    {
        // Aspect is an integer cast of the cell aspect; a fractional one would skew every pool.
        if (Aspect != Generation.FloorMask.CellAspectRatio)
            throw new InvalidOperationException(
                $"LightingSystem.Aspect {Aspect} != CellAspectRatio {Generation.FloorMask.CellAspectRatio}: "
                + "point lights measure distance in whole columns per row");
        var t = new float[SqrtLutSize];
        for (int i = 0; i < SqrtLutSize; i++) t[i] = (float)Math.Sqrt(i);
        return t;
    }

    // Emissive tiles: RGB tint + shadowcast radius in rows (Aspect x that in columns). Generous overlap with torch for warm/cool blends.
    // The only emissive table: GameMap's emissive index is derived from it.
    public static (LightRgb Color, int Radius)? GetEmission(TileType type) => type switch
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
