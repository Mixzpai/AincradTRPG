using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Lighting;

// The directional half of the lighting model.
//
// Ambient is the sky: one colour everywhere, no bearing, so on its own a forest canopy would
// light the ground exactly as brightly as the clearing beside it. This file adds the two terms
// that give the world relief — a sky body with a bearing, and a small ambient-occlusion term so
// a cell hemmed in by walls sits darker than one in the open.
//
// One sweep serves both bodies. The sun owns the day and a full moon owns the night, and because
// a full moon traces the same half-circle half a cycle later, DayNightCycle answers direction,
// decay and depth for whichever is up. The only thing this file branches on is which per-channel
// loss the shadow spends — moon shade goes cooler than sun shade, over an ambient a tenth as
// bright.
//
// Why a sweep and not a ray walk: sampling backwards along the sun vector from every cell needs
// an inner loop per cell, on the order of a million opacity probes per turn across the lighting
// ROI. The same field falls out of a single ordered pass instead: the shadow arriving at a cell
// is a weighted blend of the shadow leaving the line one step upwind of it. That is O(1) per
// cell.
//
// The sweep runs along the sky body's major axis, and that is the load-bearing choice. The
// shadow's lateral spread has to come from cells on both sides of it, and a causal pass only has
// the line it has already finished; a row-major pass at a near-horizontal bearing has only one
// side, so the shadow would fringe on one side and be hard-edged on the other. Stepping along the
// major axis makes the upwind line perpendicular to the flow at every bearing, so both lateral
// neighbours are always in hand and the penumbra straddles the shadow by construction. It also
// leaves no loop-carried dependency: nothing in the inner loop reads a cell it just wrote.
//
// The blur is the penumbra. A purely directional chain is one cell wide with a hard edge.
// Feeding a fixed share back from the two lateral neighbours spreads the shadow as it travels, so
// the core stays dark at the caster's foot and the edge softens with distance.
//
// The picture depends on a per-tile shadow scalar in [0,1] multiplied into the ambient term
// before any point light is added, plus a per-channel loss that makes shadow cooler than open
// ground, plus a contact term counting sight-blocking neighbours. Computing this per pixel and
// interpolating would be a different look: hard tile edges are what make it read as roguelike
// illumination rather than as a lit 3D scene. The ordering must hold: shadow and contact scale
// ambient only, point lights add on top, so a torch fills a shadow back in.
public sealed partial class LightingSystem
{
    // How far upwind of the lit region the sweep starts, so a caster just outside it still lays
    // its shadow in.
    //
    // A truncated history would make shadows move as the player walks — the sweep's origin
    // travels with them, so a cell with too few steps behind it changes value every step and the
    // damage-tracked tile layer rebuilds the viewport for nothing. The lighting ROI is far wider
    // than the viewport, so a cell the player can see already carries plenty of history before
    // this margin is added: this is a margin on top of the ROI, and it is the ROI that has to
    // stay generous.
    private const int SweepMargin = 20;

    // Deepest ambient occlusion, at a cell walled in on every side of its 5x5. Small on purpose:
    // this is contact shading that reads as depth in a corner, not a second shadow system.
    private const float AoDepth = 0.32f;

    // How far contact shading reaches. The 5x5 below, weighted by inverse squared distance, fades
    // it over two cells and lets corners shade round; the four orthogonal neighbours alone would
    // give every wall a hard one-cell dark ring and shade nothing beside a diagonal wall.
    public const int ContactRadius = 2;
    public const float ContactFloor = 1f - AoDepth;

    // The sum of the weights: four at 1, four diagonals at 1/2, four at 1/4, eight knight's
    // moves at 1/5 and four corners at 1/8.
    private const float ContactNorm = 1f / 9.1f;

    // The contact multiplier for cell i at (x, y), in [ContactFloor, 1]. Off the map reads as
    // opaque, as it does everywhere sight is asked about.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ContactAt(ref bool opBase, int i, int x, int y)
    {
        if (x < 2 || y < 2 || x >= Width - 2 || y >= Height - 2) return ContactAtEdge(ref opBase, x, y);
        ref byte b = ref Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i));
        int w = Width, w2 = 2 * Width;
        int d1 = Unsafe.Add(ref b, -1) + Unsafe.Add(ref b, 1) + Unsafe.Add(ref b, -w) + Unsafe.Add(ref b, w);
        int dg = Unsafe.Add(ref b, -w - 1) + Unsafe.Add(ref b, -w + 1)
               + Unsafe.Add(ref b, w - 1) + Unsafe.Add(ref b, w + 1);
        int d2 = Unsafe.Add(ref b, -2) + Unsafe.Add(ref b, 2) + Unsafe.Add(ref b, -w2) + Unsafe.Add(ref b, w2);
        int kn = Unsafe.Add(ref b, -w2 - 1) + Unsafe.Add(ref b, -w2 + 1)
               + Unsafe.Add(ref b, w2 - 1) + Unsafe.Add(ref b, w2 + 1)
               + Unsafe.Add(ref b, -w - 2) + Unsafe.Add(ref b, -w + 2)
               + Unsafe.Add(ref b, w - 2) + Unsafe.Add(ref b, w + 2);
        int dd = Unsafe.Add(ref b, -w2 - 2) + Unsafe.Add(ref b, -w2 + 2)
               + Unsafe.Add(ref b, w2 - 2) + Unsafe.Add(ref b, w2 + 2);
        if ((d1 | dg | d2 | kn | dd) == 0) return 1f;
        return 1f - AoDepth * ContactNorm * (d1 + 0.5f * dg + 0.25f * d2 + 0.2f * kn + 0.125f * dd);
    }

    private float ContactAtEdge(ref bool opBase, int x, int y)
    {
        float occ = 0f;
        for (int oy = -ContactRadius; oy <= ContactRadius; oy++)
        for (int ox = -ContactRadius; ox <= ContactRadius; ox++)
        {
            int d2 = ox * ox + oy * oy;
            if (d2 == 0) continue;
            int nx = x + ox, ny = y + oy;
            bool opaque = (uint)nx >= (uint)Width || (uint)ny >= (uint)Height
                          || Unsafe.Add(ref opBase, ny * Width + nx);
            if (opaque) occ += d2 == 2 ? 0.5f : d2 == 8 ? 0.125f : 1f / d2;
        }
        return 1f - AoDepth * ContactNorm * occ;
    }

    // How much the penumbra spreads per step of the sweep, as a variance in cells squared.
    //
    // A variance, not a blur share. The four taps below reconstruct the upwind sample at a
    // fractional minor offset by linear interpolation, and linear interpolation adds f(1-f) of
    // variance of its own: zero at drift 0 and 1, a quarter of a cell squared at drift 0.5. A
    // fixed blur share would therefore make the penumbra's width a function of the sun's bearing.
    // Naming the total and spending the blur on whatever the interpolation did not add holds the
    // width constant across the day with the same four taps.
    //
    // The value is 0.20 plus the day's mean of f(1-f). Raising it softens every penumbra; it
    // must stay above 0.25 or the compensation would need a negative blur share at drift 0.5.
    private const float PenumbraSpread = 0.3667f;

    // Below this the sky contributes nothing worth a sweep — underground, at the two handovers
    // between sun and moon, and on a starless night where nothing directional is up at all.
    private const float MinSkyShadow = 0.002f;

    // A shadow is not a dimmer switch. Losing the sun leaves a cell lit by the sky, which is
    // cool, so shadowed ground shifts blue as well as dark — that is what makes a shadow read as
    // light behaving rather than as the map being turned down. Red loses the most, blue least.
    // Scaling all three channels equally comes out muddy: grey-brown grassland just becomes
    // darker grey-brown, and the eye reads that as a rendering fault.
    private const float ShadowLossR = 1.00f;
    private const float ShadowLossG = 0.92f;
    private const float ShadowLossB = 0.68f;

    // A moon shadow goes cooler still. Night ambient is already blue-dominant (18, 22, 45), so a
    // shadow that spent the channels the way the sun's does would come out as a slightly darker
    // blue and read as the brightness being turned down. Taking nearly all of the red and barely
    // half of the blue keeps moonlit shade separable from open moonlit ground at a tenth of the
    // daylight amplitude, which is the only budget the night has. The night's shade is a hue
    // shift as much as a level one: the depth without the per-channel split draws a night with no
    // relief in it.
    private const float MoonShadowLossR = 1.00f;
    private const float MoonShadowLossG = 0.86f;
    private const float MoonShadowLossB = 0.52f;

    // ── How tall the thing casting is ─────────────────────────────────────────────────────────
    // A caster emits its own height instead of a flat 1, so a boulder does not throw what a
    // mountainside throws. Because the shadow fades geometrically that buys length as well as
    // depth: a shadow is visible until height * decay^m falls under the renderer's colour step,
    // so halving the height takes ln 2 / ln(1/decay) cells off the end of it — about seven at a
    // low sun, five at noon. Per-caster decay is not available: the sweep carries one line of
    // history and a cell's shadow is a blend of several origins, so there is nowhere to keep whose
    // shadow this was. Emission is the O(1) form.
    //
    // The table is covered by the light cache without the cache knowing it exists, the same shape
    // as folding the moon into SunState. It is a pure function of the tile types, it is rebuilt
    // exactly when GameMap.SightVersion moves, and that version is already a cache key, so a
    // field cached under one version can never be served against a table built under another.
    //
    // A caster contributes a shadow scaled by its own height in [0,1], and the field takes the max
    // of what a cell emits and what arrives at it, so a low caster standing in a high caster's
    // shadow never shortens it.
    // Anything that does not block sight casts nothing, so this is only ever asked about the six
    // types GameMap.ComputeOpaque admits. An unlisted one falls to full height, so a new
    // sight-blocker casts a full shadow until it is given an entry.
    public static float CasterHeightOf(TileType t) => t switch
    {
        TileType.Mountain => 1.00f,   // a mountainside: the longest thing on any floor
        TileType.Wall     => 1.00f,   // a built wall, and the closed Door that stands in one
        TileType.Door     => 1.00f,
        TileType.TreePine => 0.90f,   // a conifer, taller than it is wide
        TileType.Tree     => 0.78f,   // a broadleaf canopy, tall but not solid
        TileType.Rock     => 0.50f,   // a boulder: a stub of a shadow, not a wall's
        _                 => 1.00f,
    };

    // How many single-cell edits are worth patching before a full rebuild is cheaper anyway. It
    // exists to bound the list rather than to tune anything: map generation writes tile types in
    // bulk without the lighting ever running between them, so an uncapped list would grow to the
    // size of the floor before anything drained it.
    private const int CasterDirtyLimit = 4096;

    private byte[]? _casterHeight;
    private int _casterHeightVersion = -1;
    private GameMap? _casterHeightMap;
    private readonly List<int> _casterDirty = new();

    // One cell, not the whole floor: a full table rebuild per tile edit is too slow for a
    // repeated action like mining, so an edit patches one cell, as GameMap does for its own
    // opacity table.
    //
    // The patch is deferred rather than applied here, because GameMap raises this event before it
    // updates its opacity table, so at this moment the answer to "does that cell block sight now"
    // is still the old one, and the cell would be written from stale data.
    //
    // Dropping the cell without advancing the version is what makes the cap safe: the stamp then
    // trails the map's and the next sweep rebuilds everything.
    //
    // The stamp only advances when this event accounts for the whole move. SetTileType bumps the
    // sight version by one and then raises this, so a version exactly one past the stamp is this
    // edit and nothing else. Anything further is a bump that raised no event (OpenDoor at
    // runtime, InvalidateOpacity and RebuildOpacity in mapgen) whose cell is in no list; stamping
    // over it would turn the full rebuild it owes into a one-cell patch, and an opened door would
    // keep casting a closed door's shadow. Leaving the stamp behind makes the next sweep rebuild
    // everything.
    private void OnCasterTileChanged(int x, int y, TileType oldType, TileType newType)
    {
        if (_casterHeightMap is not { } m) return;
        if (_casterDirty.Count >= CasterDirtyLimit) return;
        _casterDirty.Add(y * Width + x);
        if (m.SightVersion == _casterHeightVersion + 1) _casterHeightVersion = m.SightVersion;
    }

    private byte HeightAt(ReadOnlySpan<bool> op, GameMap map, int i)
    {
        if (!op[i]) return 0;                     // opacity decides whether, the type decides how much
        int y = i / Width;
        return (byte)MathF.Round(CasterHeightOf(map.Tiles[i - y * Width, y].Type) * 255f);
    }

    // Rebuilt in full only when the map's sight version has moved for a reason no single-cell
    // event accounted for — a fresh opacity table, a door, or a burst past the cap. Everything
    // else is a handful of cells. Either way the table ends up matching the version the light
    // cache is about to record, which is what keeps the two from ever disagreeing.
    private void EnsureCasterHeights(GameMap map, ReadOnlySpan<bool> op)
    {
        int need = Width * Height;
        if (!ReferenceEquals(_casterHeightMap, map))
        {
            // A LightingSystem belongs to exactly one GameMap and is constructed by it, so the
            // subscription cannot outlive its publisher and adds no retention of its own. The
            // swap arm covers a caller that hands it a different map.
            if (_casterHeightMap != null) _casterHeightMap.TileTypeChanged -= OnCasterTileChanged;
            _casterHeightMap = map;
            map.TileTypeChanged += OnCasterTileChanged;
            _casterHeightVersion = -1;
        }
        if (_casterHeight == null || _casterHeight.Length < need)
        {
            _casterHeight = EnsureCapacity(_casterHeight, need);
            _casterHeightVersion = -1;
        }
        var h = _casterHeight;

        if (_casterHeightVersion == map.SightVersion)
        {
            if (_casterDirty.Count == 0) return;
            foreach (int i in _casterDirty) h[i] = HeightAt(op, map, i);
            _casterDirty.Clear();
            return;
        }

        _casterDirty.Clear();
        var tiles = map.Tiles;
        for (int y = 0; y < Height; y++)
        {
            int row = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int i = row + x;
                h[i] = op[i] ? (byte)MathF.Round(CasterHeightOf(tiles[x, y].Type) * 255f) : (byte)0;
            }
        }
        _casterHeightVersion = map.SightVersion;
    }

    // -- Indirect light -----------------------------------------------------------------------
    // The sweep answers "how much of the sun is blocked here" and nothing else, so on its own a
    // shadowed cell beside a brightly lit one would be exactly as dark as one in the middle of a
    // wood. Real shade is light that arrived by a longer route: a lone tree's shadow on a bright
    // lawn is pale because the lawn all around it throws light back into it, while the same tree
    // in the middle of a thicket casts a shadow with nothing to fill it. That is the cue
    // separating a leaf's shadow from a building's.
    //
    // What it is: one gather of the direct light landing nearby, given back as a reduction of the
    // shadow rather than as light of its own. That distinction is the whole safety argument.
    // Written as
    //
    //     effective = arriving * (1 - BounceLift * nearbyLit)
    //
    // it is exactly zero wherever `arriving` is zero, so open ground still reads the ambient
    // colour byte for byte, and exactly zero where `nearbyLit` is zero, so the middle of a wall
    // mass keeps its full depth. Only a shadow with light around it moves. An additive bounce term
    // would brighten the whole map instead.
    //
    // Why a gather and not a second sweep: indirect light has no direction to sweep along. It is
    // the low-frequency average of what is lit nearby, the first-order gather of grid-based
    // methods (one Jacobi iteration of a radiosity solve), computed per tile over a turn.
    //
    // It is O(1) per cell whatever the radius: a separable running window costs one add, one
    // subtract and one multiply per cell per axis whatever its width, so the radius below is
    // chosen for how it looks. Both passes run row-major — the vertical one carries a row-length
    // accumulator and streams rows in and out rather than walking columns — because a column walk
    // over a wide region misses cache on every read.
    //
    // A cell that blocks sight contributes nothing to the gather, so the term does two jobs. A
    // wall is not a light source, so a neighbourhood that is mostly wall gathers little and its
    // shade stays deep, while an open clearing gathers nearly everything and fills its shadows
    // in. That is large-scale occlusion at ~15 cells, against the contact term's one, and the two
    // do not overlap.
    //
    // A running window does not know about walls, so light can reach across a partition it should
    // not cross. A diffusion that respects opacity would cost a pass per cell of reach; the leak is
    // smaller than the effect itself and of the same sign as the sky bouncing over the wall.
    //
    // The picture depends on one low-frequency gather of `1 - shadow` over cells that do not
    // block sight, applied multiplicatively to the shadow and never additively to the light: it
    // is the multiplicative form that guarantees open ground is untouched.
    //
    // Nothing new enters the light cache's key. The field is a pure function of `_shadow` and the
    // opacity table, and both are covered already - the first through SunState and the ambient
    // triple, the second through SightVersion. It holds no per-tile state between turns either,
    // so it cannot fall into the blind spot --verify-light has for a table both sides share.
    private const int BounceRadius = 7;

    // Deepest share of a shadow its surroundings may give back.
    //
    // One value for both skies. The night's shadow is far shallower than the day's, but a
    // day-only lift would put the two skies on different lighting models, and the moon's shadow
    // would become a larger share of its own sky than the sun's. One sweep already serves both
    // bodies; one gather does too, which keeps the night a fainter version of the day rather than
    // a different renderer. What the night gives up is the lone-caster case in wholly open
    // ground; the deep shade it depends on comes from masses of wall and rock, which gather little
    // and are barely touched.
    private const float BounceLift = 0.30f;

    // The dials themselves, read-only. Read by the verification probes.
    public static float IndirectLift => BounceLift;
    public static int IndirectRadius => BounceRadius;

    // The gathered field, region-local like _shadow; the ring of horizontal-pass rows the
    // vertical window slides over; and two row-length scratches.
    private float[]? _bounce, _bounceRing, _bounceAcc, _bounceSrc;

    // The horizontal pass is streamed one row ahead of the vertical window, not materialised.
    // The window at output row y spans rows [y-R, y+R] and takes row y+1+R in as row y-R leaves,
    // so only 2R+2 gathered rows are ever live. Holding them in a ring keeps the pass out of main
    // memory: a row is applied the moment its gather completes, while it is still in cache,
    // instead of a region-sized intermediate being written in full and read back.
    private const int BounceRingRows = 2 * BounceRadius + 2;

    // One row of the horizontal gather — the direct light landing on transparent ground, averaged
    // over a window of 2R+1 columns — into dst[dstBase .. dstBase+rw).
    private void GatherRow(ReadOnlySpan<bool> op, int rx0, int ry0, int rw, int y, float[] dst, int dstBase)
    {
        const int R = BounceRadius;
        // One row of source values, not a whole region of them: a row is a couple of kilobytes and
        // stays in cache between being built and being slid over.
        //
        // Branchless, for the reason ApplyRow is: whether a cell blocks sight is data and not a
        // pattern, so a ternary is an unpredictable branch on every cell of the region. Reading
        // the bool as a byte and multiplying costs one instruction and never mispredicts.
        // Building the row's source once also means the slide does not re-derive each value from
        // both arrays twice, once entering the window and once leaving it.
        ref bool opRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(op), (ry0 + y) * Width + rx0);
        ref float shRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_shadow!), y * rw);
        ref float srcRef = ref MemoryMarshal.GetArrayDataReference(_bounceSrc!);
        ref float dstRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(dst), dstBase);
        for (int x = 0; x < rw; x++)
        {
            byte blocks = Unsafe.As<bool, byte>(ref Unsafe.Add(ref opRef, x));
            Unsafe.Add(ref srcRef, x) = (1f - Unsafe.Add(ref shRef, x)) * (1 - blocks);
        }

        // A sight-blocking cell contributes nothing but still occupies its place in the window,
        // which is what makes a wall-heavy neighbourhood gather less.
        //
        // The divisor is constant except at the two ends, so the interior takes a reciprocal
        // multiply and only the first and last R columns divide at all. A divide is several times
        // a multiply and this runs over every cell of the padded region.
        int lastX = rw - 1;
        bool wideEnough = rw >= 2 * R + 2;
        float invFull = 1f / (2 * R + 1);
        int midFrom = wideEnough ? R : rw;                 // [midFrom, midTo) is the full window
        int midTo = wideEnough ? rw - R - 1 : rw;

        float sum = 0f;
        int prime = Math.Min(R, lastX);
        for (int i = 0; i <= prime; i++) sum += Unsafe.Add(ref srcRef, i);

        if (!wideEnough)
        {
            // A region narrower than one window: every cell is an edge case.
            for (int x = 0; x < rw; x++)
            {
                int lo = x - R; if (lo < 0) lo = 0;
                int hi = x + R; if (hi > lastX) hi = lastX;
                Unsafe.Add(ref dstRef, x) = sum / (hi - lo + 1);
                int add = x + 1 + R;
                if (add < rw) sum += Unsafe.Add(ref srcRef, add);
                int sub = x - R;
                if (sub >= 0) sum -= Unsafe.Add(ref srcRef, sub);
            }
            return;
        }

        // Left edge: the window is clipped at 0, so nothing leaves it yet.
        for (int x = 0; x < midFrom; x++)
        {
            Unsafe.Add(ref dstRef, x) = sum / (x + R + 1);
            sum += Unsafe.Add(ref srcRef, x + 1 + R);
        }
        // Interior: a full window, one add, one subtract, one multiply.
        for (int x = midFrom; x < midTo; x++)
        {
            Unsafe.Add(ref dstRef, x) = sum * invFull;
            sum += Unsafe.Add(ref srcRef, x + 1 + R) - Unsafe.Add(ref srcRef, x - R);
        }
        // Right edge: the window is clipped at lastX, so nothing enters it any more.
        for (int x = midTo; x < rw; x++)
        {
            Unsafe.Add(ref dstRef, x) = sum / (lastX - x + R + 1);
            sum -= Unsafe.Add(ref srcRef, x - R);
        }
    }

    // Shadow arriving at each cell of the swept region, row-major over that region alone. The
    // sweep fills it and the apply pass below reads it; keeping the two apart is what lets the
    // sweep run along whichever axis the sky needs while the light writes stay in storage order.
    private float[]? _shadow;

    // The other rasterisation, held only while a bearing sits inside the diagonal seam so the two
    // can be blended. Allocated on first use — most bearings never touch it.
    private float[]? _shadowAlt;

    // ── The last sweep's region ────────────────────────────────────────────────────────────
    // Where the last sweep's region sat, so a reader can index _shadow at all: the buffer is
    // region-local and the region travels with the player. Written by the sweep, read only by
    // TryGetShadingTerms below.
    private int _shadowRx0, _shadowRy0, _shadowRw, _shadowRh;
    private bool _shadowSwept;

    // Shadow leaving the previous / current line of the sweep, indexed by the minor coordinate.
    // Padded by two at each end so the four taps never fall off.
    private float[]? _linePrev, _lineCur;

    // True when the flat Span.Fill still applies — the player has turned shading off.
    private static bool ShadingEnabled => UserSettings.Current.TerrainShading;

    // Sky shadows plus ambient occlusion over the region, written into the light grid. The sky is
    // a parameter and the caller has brought the caster heights up to date, so the light cache's
    // worker can run this for a sky that has not arrived yet (LightingSystem.Speculation).
    private void FillShadedRegion(ReadOnlySpan<bool> op, in SkyShade sky, bool hasSky,
                                  int x0, int y0, int x1, int y1)
    {
        using var _ = Profiler.Begin("Lighting.Shading");
        var ambient = LightRgb.Of(sky.AmbR, sky.AmbG, sky.AmbB);
        _shadowSwept = false;

        // Cloud and fog scatter whatever is up there, and scattered light casts no shadow — so an
        // overcast floor keeps its brightness and loses its relief, by night as much as by day.
        // Render-only: weather cannot reach a roll from here.
        float strength = hasSky ? sky.Strength * sky.SunlightScale : 0f;
        if (strength <= MinSkyShadow)
        {
            FillOccludedRegion(op, ambient, x0, y0, x1, y1);
            return;
        }

        // Which body is casting is the only thing the sweep needs to know, and it decides one
        // thing: how the shadow spends its three channels. Direction, reach and depth are already
        // the sky's own, whichever is up.
        var loss = sky.SunIsUp
            ? (R: ShadowLossR, G: ShadowLossG, B: ShadowLossB)
            : (R: MoonShadowLossR, G: MoonShadowLossG, B: MoonShadowLossB);

        float ux = sky.Ux, uy = sky.Uy;
        float decay = sky.Decay;
        int sx = ux >= 0f ? 1 : -1;
        int sy = uy >= 0f ? 1 : -1;

        // Extend only the upwind side of each axis — the downwind side needs no history.
        int rx0 = sx > 0 ? Math.Max(0, x0 - SweepMargin) : x0;
        int rx1 = sx > 0 ? x1 : Math.Min(Width, x1 + SweepMargin);
        int ry0 = sy > 0 ? Math.Max(0, y0 - SweepMargin) : y0;
        int ry1 = sy > 0 ? y1 : Math.Min(Height, y1 + SweepMargin);
        int rw = rx1 - rx0, rh = ry1 - ry0;
        if (rw <= 0 || rh <= 0) return;

        EnsureShadowBuffers(rw, rh);

        // ── The seam at 45 degrees ────────────────────────────────────────────────────────────
        // The sweep steps along the sky's major axis, and which axis that is flips at 45 and 135
        // degrees. Both sides of the flip are individually correct and the kernel is the same
        // shape at the boundary, but the two are different rasterisations of the same diagonal
        // (one staircases along x, the other along y), and a map is not mirror-symmetric about
        // its own diagonal, so they do not agree. Swept alone, the field would be discontinuous in
        // the sun's bearing, snapping twice a day by an amount no bearing step count can shrink.
        //
        // So both rasterisations are swept through the seam and blended. The weight is a pure
        // function of the bearing, reaching exactly 0.5 where the axes trade places, so the field
        // is continuous there by construction rather than by tuning. Away from the seam the
        // weight saturates and only one sweep runs, which is what keeps this cheap: the window is
        // about +/-6 degrees, so it costs a second sweep on a small share of bearings and nothing
        // on the rest. The window has to be narrow for a second reason: outside it the weaker axis
        // has a drift above 1, which SweepShadow clamps, and a clamped drift points the spine the
        // wrong way.
        //
        // The blend exists only because the field comes out of a one-line-history sweep over a
        // grid. The contract is that the shadow field is continuous in the sun's bearing; a ray
        // march or a per-pixel method gets that without a blend.
        var casters = _casterHeight!.AsSpan(0, Width * Height);
        float blend = DiagonalBlend(ux, uy);
        using (Profiler.Begin("Lighting.Sweep"))
        {
            if (blend <= 0f)
            {
                SweepShadow(casters, _shadow!, rx0, ry0, rw, rh, ux, uy, sx, sy, decay,
                            majorIsX: MathF.Abs(ux) >= MathF.Abs(uy));
            }
            else
            {
                // The X-major rasterisation is swept straight into the alternate buffer, so
                // nothing is copied between the two sweeps.
                int need = rw * rh;
                _shadowAlt = EnsureCapacity(_shadowAlt, need);
                SweepShadow(casters, _shadowAlt, rx0, ry0, rw, rh, ux, uy, sx, sy, decay, majorIsX: true);
                SweepShadow(casters, _shadow!, rx0, ry0, rw, rh, ux, uy, sx, sy, decay, majorIsX: false);
                ref float a = ref MemoryMarshal.GetArrayDataReference(_shadowAlt);
                ref float b = ref MemoryMarshal.GetArrayDataReference(_shadow!);
                for (int i = 0; i < need; i++)
                    Unsafe.Add(ref b, i) = Unsafe.Add(ref a, i) * blend + Unsafe.Add(ref b, i) * (1f - blend);
            }
        }

        _shadowRx0 = rx0; _shadowRy0 = ry0; _shadowRw = rw; _shadowRh = rh;
        _shadowSwept = true;
        // After the seam blend, never inside a sweep: at a bearing inside the diagonal window the
        // field is two rasterisations mixed, and gathering from one of them would light the map
        // from a sun that is only half there.
        ApplyShading(op, ambient, strength, loss, rx0, ry0, rw, rh, x0, y0, x1, y1);
    }

    // Half-width of the seam, as a ratio between the two components of the bearing. 0.25 makes
    // the window the bearings where the minor component is within 25% of the major one — about
    // 38.7 to 51.3 degrees, so the weaker axis never carries a drift worse than 1.25 before it is
    // clamped, and its share there is small.
    private const float DiagonalSeam = 0.25f;

    // How much of the X-major rasterisation to keep. 1 well inside the X regime, 0 well inside
    // the Y regime, and exactly 0.5 where |ux| == |uy| — which is what makes the field continuous
    // across the flip. Returns 0 outside the seam so the caller can skip the second sweep, and
    // that is the same value the blend would give there.
    private static float DiagonalBlend(float ux, float uy)
    {
        float ax = MathF.Abs(ux), ay = MathF.Abs(uy);
        float hi = MathF.Max(ax, ay);
        if (hi <= 0f) return 0f;
        float lo = MathF.Min(ax, ay);
        // Outside the seam the caller sweeps the major axis alone, which is what a saturated
        // weight would produce anyway — so one return value serves both regimes.
        if (lo < hi * (1f - DiagonalSeam)) return 0f;
        // t runs -1 (deep in the Y regime) .. 0 (the diagonal) .. +1 (deep in the X regime).
        float t = (ax - ay) / (hi * DiagonalSeam);
        if (t > 1f) t = 1f;
        if (t < -1f) t = -1f;
        float w = 0.5f + 0.5f * t;
        return w * w * (3f - 2f * w);          // smoothstep, so the weight has no corner either
    }

    private void EnsureShadowBuffers(int rw, int rh)
    {
        int need = rw * rh;
        _shadow = EnsureCapacity(_shadow, need);
        int line = Math.Max(rw, rh) + 4;
        _linePrev = EnsureCapacity(_linePrev, line);
        _lineCur = EnsureCapacity(_lineCur, line);
    }

    // One ordered pass along the given axis, filling _shadow with what arrives at each cell.
    // `majorIsX` is passed rather than derived because the diagonal seam above sweeps both
    // rasterisations of the same bearing and blends them; deriving it here would make that
    // impossible.
    private void SweepShadow(ReadOnlySpan<byte> casters, float[] shadow, int rx0, int ry0, int rw, int rh,
                             float ux, float uy, int sx, int sy, float decay, bool majorIsX)
    {
        int majorCount = majorIsX ? rw : rh;
        int minorCount = majorIsX ? rh : rw;
        int sMajor = majorIsX ? sx : sy;
        int sMinor = majorIsX ? sy : sx;

        // Minor drift per major step, at most one cell — which is what makes a single line of
        // history enough and keeps the whole pass O(1) per cell.
        float drift = majorIsX ? MathF.Abs(uy / ux) : MathF.Abs(ux / uy);
        if (drift > 1f) drift = 1f;

        // Four taps, because the spine moves between them. The upwind point sits at a
        // fractional minor offset — `drift` of a cell back along the minor axis — so the two
        // taps that straddle it are not a fixed pair. Three taps centred on the cell itself
        // straddle the spine only when drift is 0; at drift 1 the spine has slid onto a tap and
        // the fringe would collapse to one side at the diagonal bearings.
        //
        // So the directional term and each half of the blur are all sampled at the fractional
        // offset — one at the spine, one a cell either side of it — which is four integer taps
        // once the interpolations are folded together, and symmetric at every bearing by
        // construction.
        // The blur share is whatever the reconstruction did not already spend (see PenumbraSpread).
        // Clamped at both ends: 0 would be a negative share, and 0.5 is where a blur stops having
        // a direction. Neither bound is reachable while PenumbraSpread sits between 0.25 and 0.5.
        float f = drift;
        float b = PenumbraSpread - f * (1f - f);
        if (b < 0f) b = 0f;
        else if (b > 0.5f) b = 0.5f;
        float wFar = f * b * 0.5f;                       // two steps along the drift
        float wDrift = f * (1f - b) + (1f - f) * b * 0.5f;
        float wCenter = (1f - f) * (1f - b) + f * b * 0.5f;
        float wBack = (1f - f) * b * 0.5f;               // one step against the drift

        float[] prev = _linePrev!, cur = _lineCur!;
        Array.Clear(prev, 0, minorCount + 4);
        Array.Clear(cur, 0, minorCount + 4);

        ref byte castBase = ref MemoryMarshal.GetReference(casters);
        ref float shBase = ref MemoryMarshal.GetArrayDataReference(shadow);

        // One step along the minor axis is one float of a region row (Y-major) or one region row
        // (X-major), and the caster table is read at the matching stride — so a line is walked
        // by two additions rather than two multiplies and two selects per cell.
        int shStep = majorIsX ? rw : 1;
        int mapStep = majorIsX ? Width : 1;

        for (int mj = 0; mj < majorCount; mj++)
        {
            int major = sMajor > 0 ? mj : majorCount - 1 - mj;
            // Re-taken per line because the two buffers swap; one added offset per line against
            // a few hundred cells of work inside it is not worth the ref juggling.
            ref float prevBase = ref MemoryMarshal.GetArrayDataReference(prev);
            ref float curBase = ref MemoryMarshal.GetArrayDataReference(cur);
            int shIdx = majorIsX ? major : major * rw;
            int mapIdx = majorIsX ? ry0 * Width + rx0 + major : (ry0 + major) * Width + rx0;

            for (int mi = 0; mi < minorCount; mi++)
            {
                // Lines carry two cells of padding at each end, so index by mi + 2.
                int p = mi + 2;
                float arriving = (wCenter * Unsafe.Add(ref prevBase, p)
                                + wDrift * Unsafe.Add(ref prevBase, p - sMinor)
                                + wFar * Unsafe.Add(ref prevBase, p - 2 * sMinor)
                                + wBack * Unsafe.Add(ref prevBase, p + sMinor)) * decay;

                // A caster is not darkened by its own shadow: it emits its height downwind while
                // rendering with whatever arrived at it. Cells otherwise inherit what arrived.
                // The max matters — a boulder standing inside a mountain's shadow must not cut
                // that shadow down to a boulder's. Taken branchlessly: whether a cell casts is
                // data, so a compare-and-branch here mispredicts on a forest. `arriving` is never
                // negative or NaN (the kernel is non-negative and so is every history value), so
                // Max returns exactly what a select on "does this cell cast" would.
                byte hb = Unsafe.Add(ref castBase, mapIdx);
                float emit = hb * (1f / 255f);
                Unsafe.Add(ref curBase, p) = MathF.Max(emit, arriving);
                Unsafe.Add(ref shBase, shIdx) = arriving;
                shIdx += shStep;
                mapIdx += mapStep;
            }

            (prev, cur) = (cur, prev);
        }

        _linePrev = prev;
        _lineCur = cur;
    }

    // Second pass, in storage order: slide the vertical gather window down the region and, for
    // every row inside the written window, fold the swept shadow, the gathered light and the
    // contact term into the light grid the moment that row's gather is complete.
    private void ApplyShading(ReadOnlySpan<bool> op, LightRgb ambient, float strength,
                              (float R, float G, float B) loss,
                              int rx0, int ry0, int rw, int rh, int x0, int y0, int x1, int y1)
    {
        using var _ = Profiler.Begin("Lighting.Apply");
        const int R = BounceRadius;
        int need = rw * rh;
        _bounce = EnsureCapacity(_bounce, need);
        _bounceRing = EnsureCapacity(_bounceRing, BounceRingRows * rw);
        _bounceAcc = EnsureCapacity(_bounceAcc, rw);
        _bounceSrc = EnsureCapacity(_bounceSrc, rw);
        var ring = _bounceRing;
        var acc = _bounceAcc;

        // Light is written over the interior only, so the occlusion probes need no edge guards.
        // The outermost ring of a floor is border wall nobody stands on, and a cell there missing
        // its contact shading is not something anyone can see.
        int wx0 = Math.Max(1, x0), wx1 = Math.Min(Width - 1, x1);
        int wy0 = Math.Max(1, y0), wy1 = Math.Min(Height - 1, y1);
        // A region with no interior — a map one or two cells across — is filled flat at the end.
        // The gather still runs over it, so TryGetShadingTerms reads this region's field rather
        // than whatever an earlier one left in the buffer.
        bool interior = wx1 > wx0 && wy1 > wy0;

        float ar = ambient.R, ag = ambient.G, ab = ambient.B;
        // Pre-multiplied per-channel loss, so a shaded cell costs one multiply-subtract per
        // channel rather than a shared factor and then three multiplies.
        float lossR = ar * strength * loss.R;
        float lossG = ag * strength * loss.G;
        float lossB = ab * strength * loss.B;

        ref float accRef = ref MemoryMarshal.GetArrayDataReference(acc);
        ref float ringRef = ref MemoryMarshal.GetArrayDataReference(ring);
        ref float outRef = ref MemoryMarshal.GetArrayDataReference(_bounce);

        // Vertical, streamed row by row so every access stays in storage order. Prime the window
        // with rows 0..R, in row order.
        Array.Clear(acc, 0, rw);
        int primeRows = Math.Min(R, rh - 1);
        for (int y = 0; y <= primeRows; y++)
        {
            int rb = (y % BounceRingRows) * rw;
            GatherRow(op, rx0, ry0, rw, y, ring, rb);
            for (int x = 0; x < rw; x++) Unsafe.Add(ref accRef, x) += Unsafe.Add(ref ringRef, rb + x);
        }

        // One traversal per row, not three. Writing the row, then adding the row entering the
        // window, then subtracting the row leaving it, as three separate x-loops would walk the
        // region three times and re-read the accumulator each time. Fused, the accumulator is
        // touched once per cell and the two ring rows are plain forward streams.
        for (int y = 0; y < rh; y++)
        {
            int lo = y - R; if (lo < 0) lo = 0;
            int hi = y + R; if (hi > rh - 1) hi = rh - 1;
            float inv = 1f / (hi - lo + 1);
            int b = y * rw;
            int addRow = y + 1 + R, subRow = y - R;
            bool hasAdd = addRow < rh, hasSub = subRow >= 0;
            // The row entering the window is gathered now, into the slot the row that left 2R+2
            // steps ago has just vacated.
            int a = (addRow % BounceRingRows) * rw;
            int c = hasSub ? (subRow % BounceRingRows) * rw : 0;
            if (hasAdd) GatherRow(op, rx0, ry0, rw, addRow, ring, a);

            if (hasAdd && hasSub)
                for (int x = 0; x < rw; x++)
                {
                    Unsafe.Add(ref outRef, b + x) = Unsafe.Add(ref accRef, x) * inv;
                    Unsafe.Add(ref accRef, x) += Unsafe.Add(ref ringRef, a + x) - Unsafe.Add(ref ringRef, c + x);
                }
            else if (hasAdd)
                for (int x = 0; x < rw; x++)
                {
                    Unsafe.Add(ref outRef, b + x) = Unsafe.Add(ref accRef, x) * inv;
                    Unsafe.Add(ref accRef, x) += Unsafe.Add(ref ringRef, a + x);
                }
            else if (hasSub)
                for (int x = 0; x < rw; x++)
                {
                    Unsafe.Add(ref outRef, b + x) = Unsafe.Add(ref accRef, x) * inv;
                    Unsafe.Add(ref accRef, x) -= Unsafe.Add(ref ringRef, c + x);
                }
            else
                for (int x = 0; x < rw; x++) Unsafe.Add(ref outRef, b + x) = Unsafe.Add(ref accRef, x) * inv;

            int my = ry0 + y;
            if (interior && my >= wy0 && my < wy1)
                ApplyRow(op, my, wx0, wx1, b - rx0, ar, ag, ab, lossR, lossG, lossB);
        }

        if (!interior)
        {
            // Returning here would leave the light grid holding whatever was in it, which is the
            // silent-stale failure this whole layer is built to avoid.
            FillFlat(ambient, x0, y0, x1, y1);
            return;
        }
        // The interior clamp above leaves the region's outermost ring unwritten, which would show
        // as a stale rim once it scrolled inside the viewport. Fill it flat.
        FillEdgeRing(ambient, x0, y0, x1, y1, wx0, wy0, wx1, wy1);
    }

    // One map row of the fold, over [wx0, wx1): `shRow` indexes the shadow and gathered fields
    // so that shRow + x is this row's cell.
    private void ApplyRow(ReadOnlySpan<bool> op, int y, int wx0, int wx1, int shRow,
                          float ar, float ag, float ab, float lossR, float lossG, float lossB)
    {
        ref bool opBase = ref MemoryMarshal.GetReference(op);
        ref LightRgb lightBase = ref MemoryMarshal.GetArrayDataReference(_light);
        ref float shBase = ref MemoryMarshal.GetArrayDataReference(_shadow!);
        ref float bnBase = ref MemoryMarshal.GetArrayDataReference(_bounce!);
        int row = y * Width;
        for (int x = wx0; x < wx1; x++)
        {
            int i = row + x;
            float arriving = Unsafe.Add(ref shBase, shRow + x);

            // Indirect light, spent as a share of the shadow rather than as light of its own
            // — so a cell the sun reaches is untouched by construction, however bright its
            // surroundings, and a shadow with nothing lit near it keeps its full depth.
            arriving *= 1f - BounceLift * Unsafe.Add(ref bnBase, shRow + x);

            // Contact shading, neutral across channels: it is the sky being blocked, not the
            // sun, so only the sun term carries the colour shift.
            float ao = ContactAt(ref opBase, i, x, y);

            ref LightRgb cell = ref Unsafe.Add(ref lightBase, i);
            cell.R = (ar - lossR * arriving) * ao;
            cell.G = (ag - lossG * arriving) * ao;
            cell.B = (ab - lossB * arriving) * ao;
        }
        MirrorRow(y, wx0, wx1);
    }

    private void FillFlat(LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        var light = _light.AsSpan();
        for (int y = y0; y < y1; y++)
        {
            light.Slice(y * Width + x0, x1 - x0).Fill(ambient);
            MirrorRow(y, x0, x1);
        }
    }

    private void FillEdgeRing(LightRgb ambient, int x0, int y0, int x1, int y1,
                              int wx0, int wy0, int wx1, int wy1)
    {
        var light = _light.AsSpan();
        for (int y = y0; y < y1; y++)
        {
            if (y >= wy0 && y < wy1)
            {
                if (x0 < wx0) { light.Slice(y * Width + x0, wx0 - x0).Fill(ambient); MirrorRow(y, x0, wx0); }
                if (wx1 < x1) { light.Slice(y * Width + wx1, x1 - wx1).Fill(ambient); MirrorRow(y, wx1, x1); }
            }
            else
            {
                light.Slice(y * Width + x0, x1 - x0).Fill(ambient);
                MirrorRow(y, x0, x1);
            }
        }
    }

    // Ambient occlusion alone — underground, at the sun/moon handovers, and on a starless night,
    // where there is no sky body to sweep for.
    private void FillOccludedRegion(ReadOnlySpan<bool> op, LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        var light = _light;
        float ar = ambient.R, ag = ambient.G, ab = ambient.B;
        int wx0 = Math.Max(1, x0), wx1 = Math.Min(Width - 1, x1);
        int wy0 = Math.Max(1, y0), wy1 = Math.Min(Height - 1, y1);

        ref bool opBase = ref MemoryMarshal.GetReference(op);
        ref LightRgb lightBase = ref MemoryMarshal.GetArrayDataReference(light);

        for (int y = wy0; y < wy1; y++)
        {
            int row = y * Width;
            for (int x = wx0; x < wx1; x++)
            {
                int i = row + x;
                float ao = ContactAt(ref opBase, i, x, y);
                ref LightRgb cell = ref Unsafe.Add(ref lightBase, i);
                cell.R = ar * ao;
                cell.G = ag * ao;
                cell.B = ab * ao;
            }
            MirrorRow(y, wx0, wx1);
        }

        if (wx1 > wx0 && wy1 > wy0) FillEdgeRing(ambient, x0, y0, x1, y1, wx0, wy0, wx1, wy1);
        else FillFlat(ambient, x0, y0, x1, y1);
    }

    // Read-only view for the in-game lighting debug overlay and the verification probes: the two
    // terms this file folds into the light grid, before they are folded, so the overlay shows
    // what the sweep computed rather than what survived the mix.
    // `shadow` is what arrives at the cell in [0,1] and is only meaningful when this returns
    // true — there is no sweep underground, on a starless night, or under weather flat enough to
    // scatter the sky away. `contact` is the ambient-occlusion multiplier and always applies.
    public bool TryGetShadingTerms(GameMap map, int x, int y, out float shadow, out float contact)
        => TryGetShadingTerms(map, x, y, out shadow, out contact, out _);

    // `bounce` is the gathered share of direct light landing near the cell, in [0,1], before it
    // is spent — so a reader sees what was gathered rather than what survived the multiply, the
    // same way `shadow` is reported before the per-channel loss is applied to it.
    public bool TryGetShadingTerms(GameMap map, int x, int y, out float shadow, out float contact,
                                   out float bounce)
    {
        shadow = 0f;
        contact = 1f;
        bounce = 0f;
        if (x <= 0 || y <= 0 || x >= Width - 1 || y >= Height - 1) return false;

        ReadOnlySpan<bool> op = map.OpacityTable;
        contact = ContactAt(ref MemoryMarshal.GetReference(op), y * Width + x, x, y);

        if (!ShadingEnabled || !_shadowSwept || _shadow is null) return false;
        int lx = x - _shadowRx0, ly = y - _shadowRy0;
        if ((uint)lx >= (uint)_shadowRw || (uint)ly >= (uint)_shadowRh) return false;
        shadow = _shadow[ly * _shadowRw + lx];
        if (_bounce is not null) bounce = _bounce[ly * _shadowRw + lx];
        return true;
    }
}
