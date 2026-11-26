
using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.Map.Lighting;

// Per-tile RGB light. Shadowcaster-driven FOV per emissive tile (+ player torch), smoothstep
// falloff, combined by SCREEN blending rather than by adding and clamping. Owned by GameMap;
// recomputed on UpdateVisibility, and cached between turns by LightingSystem.Cache.cs.
//
// The header used to say "linear falloff, summed per channel, clamped to [0,255]" — which is a
// description of the two behaviours the body's own comments now explain were defects.
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

    // NOTHING IN THIS FILE MAY READ A WALL CLOCK, and the reason is the call site rather than
    // taste. The field is written by Update, and Update is reached from GameMap.UpdateVisibility
    // and nowhere else — only when a TURN passes — so a wall-clock term in here is SAMPLED AT
    // TURN BOUNDARIES whatever rate it claims. This carried a torch "flickering at 4 Hz", a ±1
    // radius pulse on the same clock and a shrine breathing on a 3-second sine; driven, a still
    // player painted 46 frames across 2.3 s and ten 250 ms flicker phases while the field was
    // evaluated ZERO times. None of it could reach anyone who had stopped walking, which is
    // exactly when a player is looking at the screen.
    //
    // Making it live is unaffordable, not merely costly: the frame would have to repaint at the
    // animation's rate for as long as ANY light is on screen — always, the player carries one —
    // and a drawn frame costs a flat ~25-30 ms flush at 316x91 whatever changed in it. Four
    // forced frames a second is ~10% of wall clock, permanently, to move the 1.7% of the viewport
    // a point light actually touches. MapView agrees from the other side: its only light-related
    // invalidator is the TURN STAMP, so light moving without the turn would go stale on the
    // frame-blit path in silence.
    //
    // Removing the terms also removed real churn, and made the field deterministic for a given
    // seed. Measured at 316x91 on a midnight floor, Debug: up to 456 of 28,756 viewport cells
    // moved between two evaluations of the SAME turn (262 past the renderer's own 8/255
    // quantization), and thirty still turns re-resolved 2,540-2,912 tile-layer cells. Both are
    // now 0. TileVerifyProbe --light is the standing check, in both directions.
    //
    // ── Headroom: a light may drive a cell past its own colour ────────────────────────────
    // LightFull is the light value at which ApplyLighting renders a tile at EXACTLY its authored
    // colour (fg = base x light / LightFull). Everything in this block exists so that a value
    // ABOVE it is reachable: without headroom there is no such thing as bright, only less dark,
    // and standing in torchlight produces the same pixels as standing in full ambient noon.
    //
    // Two halves, and neither works alone. The COMBINE's ceiling moves to LightCeiling, so
    // overlapping lights approach 510 rather than 255; a lone source is untouched, because
    // Screen(0, b) = b at any ceiling, and that is what leaves open daylight byte-identical.
    // And a SOURCE emits above full, because the combine cannot exceed its own inputs by much
    // when one of them is already near the ceiling — raising the ceiling alone moves a campfire's
    // own cell by 3%. That is Brogue's shape: its light colours are declared at 150-180 on a
    // scale where 100 is full, and paintLight accumulates them with a bare +=.
    //
    // WHAT WAS NOT TAKEN FROM BROGUE: adjustedLightValue, its sqrt knee above 150%. That exists
    // because paintLight has NO ceiling at all, so a room with several lamps runs away. This
    // combine is already a soft knee and cannot pass LightCeiling, so a second knee on an
    // already-bounded quantity would only spend the headroom the first one leaves.
    public const float LightFull = 255f;

    // MUST STAY ABOVE SourceOverdrive x the brightest emission, and that is not a style rule:
    // Screen(a, b) = a + b - ab/C has slope (1 - b/C) in a, so an input past the ceiling makes
    // the combine DECREASE as more light arrives. That is a gradient inversion, not a clamp.
    // Asserted from MaxSourceEmission by TileVerifyProbe --overdrive rather than left as prose.
    public const float LightCeiling = LightFull * 2f;

    // How far past full a source emits at full strength. 1.6 puts a midnight torch's own cell at
    // 1.40x the tile's authored colour and a campfire's at 1.61x, while leaving noon at 1.10x
    // ambient, under the 1.25x SeedProbe holds the daylight torch to.
    private const float SourceOverdrive = 1.6f;

    // Mid-point of the four warm whites the old flicker alternated between; they spanned 15 per
    // channel, under two of the renderer's colour steps.
    private const float TorchBaseR = 220f, TorchBaseG = 200f, TorchBaseB = 160f;

    // MEASUREMENT SEAM, in the same family as SourcesLastUpdate: the largest channel any source
    // can put into the grid, overdrive included. DERIVED from the emission table and the torch
    // rather than hand-written, so a new emissive type or a brighter torch is covered without
    // anyone editing a list — the check that reads it is asserting a property of the combine, and
    // a hand-copied maximum would let that property lapse in silence.
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

    // Ambient via DayNightCycle.Ambient. The player casts NO light of their own: the carried
    // light is whatever an equipped item declares (EquipmentBase.LightRadius), and a player
    // carrying none walks in whatever the sky gives them. The flame still shrinks with
    // TorchInfluence as the sun climbs, which is what stops it being a searchlight at noon.
    //
    // THE SKY IS NOT OVERDRIVEN, deliberately. Ambient tops out at (180, 175, 150), so open
    // daylight renders every tile at 0.71x its authored colour and always has; lifting that
    // would move every cell of every floor and re-open the banding, vignette and sight-edge
    // profiles that are all measured against it. The headroom belongs to whatever is meant to
    // read as a light SOURCE.
    private static LightRgb TorchColor()
    {
        // Scaled by how dark it is. Flat, this added ~220 per channel at every hour, so at noon
        // the player stood at the centre of a thirteen-tile disc clipped at 255 — brighter than
        // the daylight around it, and washing out every sun shadow within reach of the one
        // viewer who could see them.
        float k = DayNightCycle.TorchInfluence * SourceOverdrive;
        return LightRgb.Of(TorchBaseR * k, TorchBaseG * k, TorchBaseB * k);
    }

    // Shrinks with the flame. A dim torch that still reached twelve tiles would read as a hole
    // in the daylight rather than as a flame; this also cuts the midday shadowcast.
    // `full` is the equipped item's own radius. Reach does NOT breathe: a ring of cells flips
    // between lit and unlit when it moves, which was the largest single contributor to the
    // churn above.
    // The daylight floor is a THIRD of the item's reach rather than a flat 4, so a bigger
    // lantern stays bigger at noon; a flat floor made every tier identical in daylight.
    private static int TorchRadius(int full)
    {
        int floorReach = Math.Max(2, full / 3);
        return Math.Max(floorReach,
            (int)MathF.Round(floorReach + (full - floorReach) * DayNightCycle.TorchInfluence));
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

    // ANY NEGATIVE VALUE, NOT THE SENTINEL'S OWN. Testing for the poked value back cannot see the
    // failure it exists to find: AddSource screens a source onto whatever is in the cell, and
    // Screen(-12345, b) lands between -12345 and -2061 for every emission this game can produce —
    // still absurd, but no longer the sentinel, so a cell the fill missed INSIDE A LIGHT POOL read
    // as written. That is the only place the defect is visible, since an unfilled cell nothing
    // lights just keeps an older ambient. Nothing else in the field is ever negative: ambient is
    // non-negative, the sweep kernel is a partition of unity so `arriving` is at most 1, the
    // per-channel loss is at most the ambient it is subtracted from, and Screen of two
    // non-negatives is non-negative.
    public bool IsUnwritten(int x, int y) => _light[y * Width + x].R < 0f;

    // Fast path for render loops that have already done a bounds check.
    public LightRgb GetLightUnchecked(int x, int y) => _light[y * Width + x];

    // How many point lights the last Update actually added — the torch plus every emissive tile
    // that survived the reach gates. A measurement seam, not state: a floor is lit PER TILE and
    // lava is a tile, so this is the number that decides what a turn costs, and a check that
    // recomputes the gate for itself instead of reading this cannot notice the gate widening.
    public int SourcesLastUpdate { get; private set; }

    // HOW MANY TIMES THE FIELD WAS EVALUATED. A measurement seam, and the only way to answer the
    // question this layer cannot answer from outside: the light grid is written by Update alone,
    // and Update is reached only from GameMap.UpdateVisibility — i.e. from a TURN. A frame does
    // not evaluate it, so anything in here that reads a wall clock is sampled at turn boundaries
    // and is frozen for a player who is standing still.
    public static int UpdatesRun { get; private set; }

    public static int DrainUpdatesRun()
    {
        int n = UpdatesRun;
        UpdatesRun = 0;
        return n;
    }

    // ROI radius matches active FOV so all visible tiles are lit (FOV 4× the viewport).
    private static int LightingRoiRadius => DayNightCycle.FovRadius + 10;

    // HOW MANY STEPS OF UPWIND HISTORY A SHADOW NEEDS BEFORE IT IS INDISTINGUISHABLE FROM ONE
    // SWEPT FROM INFINITY.
    //
    // THE BAR IS THE RENDERER'S OWN COLOUR QUANTIZER, so this number is not free-standing: it is
    // the smallest m for which `ambient x strength x decay^m` falls under that quantizer's half
    // step, evaluated across every sky state rather than bounded by combining each factor's worst
    // case (those never co-occur — a slow decay only ever arrives with a weak shadow under a dim
    // sky, because both ramp through twilight).
    //
    // IT ONLY EVER WIDENS Y, SO ONLY THE Y-AXIS REQUIREMENT CAN BE ITS JUSTIFICATION — and that
    // is what the value 76 got wrong. The sweep steps along the sky body's MAJOR axis, so a state
    // charges the axis its own bearing runs along and no other. Evaluated per axis over all 58
    // sky states: X (near-horizontal bearings) needs 76, worst at the first moon bearing after
    // sunset — a still-bright sky, ambient 129, under a weak but very long shadow (strength
    // 0.128, decay 0.963). Y (near-vertical bearings) needs 45, worst at moon bearing 4. A
    // near-vertical sun is high, and a high light decays fast.
    //
    // So 76 was the X number spent on the Y reach, and it bought nothing: driven end to end,
    // pinning this at 0 leaves the worst convergence error at a DRAWN cell exactly where 76 left
    // it — 0.1228 of 255 on F7 Forest and 0.0079 on F62 Volcanic, against a half step of 1 —
    // because the worst case is horizontal and the X extent is the full ROI either way.
    //
    // 45 is the measured Y requirement, and it is deliberately met by the swept region ALONE
    // (reach - drawn = 61 rows at FovRadius 176) rather than by leaning on CachePad's further 72.
    // SeedProbe recomputes the requirement from DayNightCycle's own dials and asserts this covers
    // it, so a slower decay, a deeper shadow or a brighter sky fails loudly instead of shimmering.
    //
    // IF LightQuantizeStep MOVES, THIS MOVES — and so does the X figure above. Measured:
    //
    //   half step   X needs   Y needs
    //         4.0        41        26
    //         2.0        57        34
    //         1.0        76        45      <- LightQuantizeStep 2, shipped
    //         0.5        94        55
    private const int ShadowHistorySteps = 45;

    // HOW FAR ABOVE OR BELOW THE PLAYER THE GROUND HAS TO BE LIT AT ALL.
    //
    // Same argument as EmissiveRowReach below, one level up: light is only READ for cells the
    // tile loop draws, the camera is player-centred and unclamped, and the viewport is wide and
    // SHORT — so the rows worth filling are half a viewport height plus enough history for the
    // shadows arriving at them to be right. The horizontal extent stays the full ROI because
    // viewport WIDTH is not derivable from anything the lighting can see.
    //
    // Two things this must clear: the drawn rows (FovRadius / FovMultiplier is h/2 + 2), and the
    // rows a light SOURCE may write into, which is EmissiveRowReach + MaxSourceRadius — filling
    // less than that would leave a source screening onto ambient nobody refreshed.
    //
    // DO NOT CREDIT THE SECOND ARM WITH DOING THAT: it is drawn + 40 against a history term of
    // drawn + 61, so the Max never picks it at any viewport size, and the outer Min would defeat
    // it where the ROI binds anyway. It is belt-and-braces that goes live only if
    // ShadowHistorySteps ever falls below 24. The history term is what covers the sources today.
    //
    // AND THERE IS NO SUCH FLOOR IN X, measured: FillAmbientRegion stops at px + ROI while the
    // loop above admits a source at dx = ROI exactly, so that source writes up to 16 columns past
    // the fill. Nothing draws them — a visible cell is inside FovRadius, which is ROI - 10 — so
    // this is left alone rather than paid for over columns nobody reads. TileVerifyProbe --light
    // section 5 is where it is pinned, because it is also what makes IsUnwritten testable.
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

    // MEASUREMENT SEAM, in the same family as SourcesLastUpdate: how many rows above and below
    // the player the sweep fills, and how many of them the renderer will draw. A check that
    // recomputed either for itself could not notice the reach being narrowed, which is the whole
    // failure this pair exists to catch. Read-only and derived — there is nothing here to set.
    public static int ShadowRowReach => ShadingRowReach;
    public static int DrawnRowReach => DayNightCycle.FovRadius / DayNightCycle.FovMultiplier;

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

    // `carriedLightRadius` is the widest radius the player's equipped gear declares. Zero is
    // the ordinary case for a player with no light: no source is added at all, and the only
    // thing lighting them is the sky.
    public void Update(GameMap map, int playerX, int playerY, int carriedLightRadius = 0)
    {
        using var _ = Profiler.Begin("Lighting.Update");
        UpdatesRun++;
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
            // Whether a source gets a soft edge is decided from its BASE radius, which is why
            // this is read before the grouping below widens it. A landmark's edge softness must
            // not depend on how many tiles happened to land in its group.
            bool bigEnoughToSoften = radius >= PenumbraMinRadius;
            // A grouped lava source stands for several tiles, so it reaches further — but at the
            // SAME brightness. A wider fire, not a brighter one: scaling the colour by the count
            // is what produced the blown-out white field this grouping exists to remove.
            if (count > 1)
                radius = Math.Min(MaxSourceRadius, radius + Math.Min(3, (count - 1) / 2));
            // Dimmed by daylight like the torch, but on a higher floor: a campfire or a lava pool
            // is something the player navigates BY, so it keeps a readable glow at noon where a
            // carried flame does not.
            float k = DayNightCycle.EmissiveInfluence * SourceOverdrive;
            var c = emission.Value.Color;
            // A GROUPED source does not get a softened edge, and that is not a cost dodge.
            // The penumbra exists where ONE light defines a boundary — a torch behind a corner,
            // a campfire alone in a room. A lava group is several overlapping lights in the open,
            // whose edges already blend into each other under the screen combine, so softening
            // them buys an indistinguishable picture for a real share of the turn.
            AddSource(map, sx, sy, radius, LightRgb.Of(c.R * k, c.G * k, c.B * k),
                      softenEdges: count == 1 && bigEnoughToSoften, isStatic: true);
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
            // Through the cache rather than straight to the sweep: the field is a pure function
            // of the map and the sky, so most turns can restore it instead of recomputing it.
            FillFromCache(map, ambient, x0, y0, x1, y1);
            return;
        }

        // The flat fill overwrites what the cache describes, and a later hit re-bases only the
        // source boxes — so the cache cannot survive this path.
        DropCache();
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

    // The per-cell reveal for one source, as a STRUCT sink so the shadowcaster inlines it. It was
    // a delegate — one indirect call per revealed cell, the same shape that was the largest
    // single cost of the player's own FOV until GameMap's sink became a struct.
    //
    // THE SHADOWCASTER REVEALS MORE THAN THE SOURCE LIGHTS. Its reach is an aspect-corrected
    // ellipse, twice as wide as it is tall, while a source's falloff is a plain circle of its
    // radius — so about a quarter of what it reveals (26% of 2.6M reveals, measured over four
    // walks) lies past the circle and contributes nothing. Every one of those cells used to pay a
    // sqrt to find out. `d2 >= r*r` says the same thing before any arithmetic, and it is exact:
    // for every radius up to MaxSourceRadius, every d2 >= r*r gave a falloff <= 0 on the old sqrt
    // path (19,320 pairs checked), so nothing that contributed is skipped.
    private struct SourceSink : Shadowcaster.IFovSink
    {
        public int Sx, Sy, RadiusSq;
        public float ColorR, ColorG, ColorB, InvRadius;
        public float[] R, G, B;

        public void Reveal(int x, int y)
        {
            int dx = x - Sx, dy = y - Sy;
            int d2 = dx * dx + dy * dy;
            if (d2 >= RadiusSq) return;
            float falloff = 1f - SqrtLut[d2] * InvRadius;
            if (falloff <= 0f) return;
            // Smoothstep rather than the raw linear ramp. Linear falloff ends in a hard rim — the
            // torch pool stopped at a visible circular edge one step wide — because the value is
            // still falling at full rate when it reaches zero. This flattens both ends: a steadier
            // core, and an edge that fades into the ambient instead of meeting it.
            falloff = falloff * falloff * (3f - 2f * falloff);
            // d2 < RadiusSq <= MaxSourceRadius^2 keeps |dx| and |dy| under MaxSourceRadius, so the
            // local index is inside the scratch box by construction.
            int idx = (dy + MaxSourceRadius) * ScratchStride + (dx + MaxSourceRadius);
            float cr = ColorR * falloff, cg = ColorG * falloff, cb = ColorB * falloff;
            if (cr > R[idx]) R[idx] = cr;
            if (cg > G[idx]) G[idx] = cg;
            if (cb > B[idx]) B[idx] = cb;
        }
    }

    // A REGION BUFFER GROWS GEOMETRICALLY, NEVER TO EXACTLY WHAT THIS FRAME NEEDS. The padded
    // region un-clips one row or one column at a time as the player walks away from a map edge,
    // so `new T[need]` reallocated a multi-megabyte LOH array every few steps for the first
    // several hundred steps of a floor: 293 MB over a 1,650-step traversal, mean 187 KB per step.
    // Doubling settles each buffer after a handful of growths. Only [0, need) is ever indexed, so
    // slack at the end is inert.
    //
    // It is invisible to a stationary or short-oscillation test — those never leave the region
    // already allocated and read a couple of hundred bytes a step.
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
        int originX = sx - MaxSourceRadius, originY = sy - MaxSourceRadius;

        // Additively merge scratch into grid, then clear scratch (local→world coord remap).
        int x0 = Math.Max(0, sx - radius), x1 = Math.Min(Width, sx + radius + 1);
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
        // Lazy-allocate ROI-sized scratch (cap at MaxSourceRadius). Reused across calls.
        if (_scratchR == null)
        {
            _scratchR = new float[ScratchLen];
            _scratchG = new float[ScratchLen];
            _scratchB = new float[ScratchLen];
        }
        // Scratch takes MAX per tile — overlapping quadrant boundaries don't double-add.
        // A non-positive radius reveals the origin alone and lights it at full strength, which
        // is what the 1-and-0 pair below preserves.
        var sink = new SourceSink
        {
            Sx = sx, Sy = sy,
            RadiusSq = radius > 0 ? radius * radius : 1,
            InvRadius = radius > 0 ? 1f / radius : 0f,
            ColorR = color.R, ColorG = color.G, ColorB = color.B,
            R = _scratchR, G = _scratchG!, B = _scratchB!,
        };
        Shadowcaster.Compute(sx, sy, radius, map.OpacityTable, Width, Height, ref sink);
        if (softenEdges)
            SoftenSourceEdges(map, radius, sx - MaxSourceRadius, sy - MaxSourceRadius);
    }

    // ── Static lights ─────────────────────────────────────────────────────────────────────────
    // An emissive tile does not move, so what it casts is a pure function of its position,
    // radius, colour, softening and the map's opacity. It is cast once and replayed until one of
    // those changes; SightVersion moves on every opacity change, a lamp's colour on every
    // quantized sun step. The torch moves with the player and is never cached.
    // Seam: off replays the uncached path, for the equality check and the A/B bench.
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
        e.X0 = Math.Max(0, sx - radius); e.Y0 = Math.Max(0, sy - radius);
        e.W = Math.Min(Width, sx + radius + 1) - e.X0;
        e.H = Math.Min(Height, sy + radius + 1) - e.Y0;
        int n = e.W * e.H;
        if (e.LR.Length < n) { e.LR = new float[n]; e.LG = new float[n]; e.LB = new float[n]; }

        CastIntoScratch(map, sx, sy, radius, color, soft);
        int originX = sx - MaxSourceRadius, originY = sy - MaxSourceRadius;
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
            int wy = ly + originY;
            bool rowInside = ly >= lo && ly <= hi && (uint)wy < (uint)Height;
            int rowBase = ly * ScratchStride;
            int mapRow = wy * Width;
            for (int lx = lo - 1; lx <= hi + 1; lx++)
            {
                int wx = lx + originX;
                open[rowBase + lx] = rowInside && lx >= lo && lx <= hi
                                     && (uint)wx < (uint)Width && !op[mapRow + wx];
            }
        }

        // Ping-pong between the two buffer sets instead of copying rows back. Every cell of the
        // box is written on every pass, so the destination needs no clearing, and an EVEN number
        // of passes leaves the result in the scratch the merge below reads. Four passes make the
        // edge a four-step ramp (0.55, 0.30, 0.17, 0.09 of the lit side) rather than two.
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
    // what is left of the headroom rather than spending it, so the result approaches the ceiling
    // without ever reaching it and the gradient survives however many lights overlap. It is also
    // commutative and order-independent, which the source loop needs since nothing sorts it.
    //
    // The ceiling is LightCeiling, not LightFull, and that is the whole of the field-side
    // headroom change. A lone source lands exactly where it always did, since Screen(0, b) = b
    // at any ceiling; only cells that two lights both reach move.
    private static float Screen(float a, float b) =>
        LightCeiling - (LightCeiling - a) * (LightCeiling - b) * (1f / LightCeiling);

    // sqrt LUT for integer d². SourceSink returns before the lookup for any d2 at or past the
    // source's own r*r, so the largest index ever taken is MaxSourceRadius^2 - 1 and the size is
    // DERIVED rather than guessed. The old table held 400 entries with a Math.Sqrt fallback for
    // the rest; the fallback was measured firing 0 times in 2.6M reveals, because the largest
    // source in the game is the radius-15 lantern and the shadowcaster's cones bound |dx| and
    // |dy| by the radius, so its farthest revealed cell is d2 = 369 (a radius-16 source would
    // reach 425 and fall back). There is no fallback now: an index past the end would throw,
    // which is the loud form of a source wider than the table.
    private const int SqrtLutSize = MaxSourceRadius * MaxSourceRadius;
    private static readonly float[] SqrtLut = BuildSqrtLut();
    private static float[] BuildSqrtLut()
    {
        var t = new float[SqrtLutSize];
        for (int i = 0; i < SqrtLutSize; i++) t[i] = (float)Math.Sqrt(i);
        return t;
    }

    // Emissive tiles: RGB tint + Euclidean shadowcasted radius. Generous overlap with torch for warm/cool blends.
    // The only emissive table: GameMap's emissive index is derived from it.
    internal static (LightRgb Color, int Radius)? GetEmission(TileType type) => type switch
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
