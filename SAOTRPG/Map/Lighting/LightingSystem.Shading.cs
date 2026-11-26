using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Lighting;

// The directional half of the lighting model.
//
// Ambient is the sky: one colour everywhere, no bearing, so a forest canopy lit the ground
// exactly as brightly as the clearing beside it and nothing on the map cast a shadow at all
// except by withholding torchlight. This file adds the two terms that give the world relief —
// a sky body with a bearing, and a small ambient-occlusion term so a cell hemmed in by walls
// sits darker than one in the open.
//
// ONE SWEEP SERVES BOTH BODIES. The sun owns the day and a full moon owns the night, and because
// a full moon traces the same half-circle half a cycle later, DayNightCycle answers direction,
// decay and depth for whichever is up. The only thing this file branches on is which per-channel
// loss the shadow spends — moon shade goes cooler than sun shade, over an ambient a tenth as
// bright.
//
// WHY A SWEEP AND NOT A RAY WALK. Sampling backwards along the sun vector from every cell needs
// an inner loop per cell — on the order of a million opacity probes per turn across the lighting
// ROI, which is not affordable beside a turn that already costs about 4 ms. The same field falls
// out of a single ordered pass instead: the shadow ARRIVING at a cell is a weighted blend of the
// shadow LEAVING the line one step upwind of it. That is O(1) per cell.
//
// THE SWEEP RUNS ALONG THE SKY BODY'S MAJOR AXIS, AND THAT IS THE LOAD-BEARING CHOICE. Sweeping
// row-major whatever the bearing was tried first and is subtly wrong: the shadow's lateral
// spread has to come from cells on BOTH sides of it, and a causal pass only has the line it has
// already finished. When the bearing is near-vertical those two lines straddle the shadow and
// all is well, but when it is near-horizontal only one side exists — so at eight of the day's
// twenty-five bearings a shadow fringed upward and was hard-edged underneath. Measured: the
// downhill side read exactly 0.0% while the uphill side read 9%. Stepping along the major axis
// instead makes the upwind line perpendicular to the flow at EVERY bearing, so both lateral
// neighbours are always in hand and the penumbra straddles the shadow by construction.
//
// It also removes the loop-carried dependency the row-major version had — nothing in the inner
// loop reads a cell the inner loop just wrote — which is why the correct version is not the
// slower one.
//
// THE BLUR IS THE PENUMBRA. A purely directional chain is one cell wide with a hard edge, which
// is the same blockiness in a diagonal costume. Feeding a fixed share back from the two lateral
// neighbours spreads the shadow as it travels, so the core stays dark at the caster's foot and
// the edge softens with distance.
// PATH-D-PORT: the visual contract is a per-tile SHADOW SCALAR in [0,1] multiplied into the
// ambient term before any point light is added, plus a per-channel loss that makes shadow cooler
// than open ground, plus a contact term counting sight-blocking neighbours. A pixel renderer would
// naturally compute this per pixel and interpolate; that is a different look, not a free upgrade —
// hard tile edges are what make this read as roguelike illumination rather than as a lit 3D scene.
// Whatever computes it, the ordering must hold: shadow and contact scale AMBIENT ONLY, point
// lights add on top, so a torch fills a shadow back in.
public sealed partial class LightingSystem
{
    // How far upwind of the lit region the sweep starts, so a caster just outside it still lays
    // its shadow in.
    //
    // A truncated history is what would make shadows MOVE as the player walks — the sweep's
    // origin travels with them, so a cell with too few steps behind it changes value every step
    // and the damage-tracked tile layer rebuilds the viewport for nothing. But do not credit this
    // constant with preventing that: the lighting ROI is far wider than the viewport, so a cell
    // the player can actually SEE already carries forty-odd steps before this margin is added.
    // Sabotage proved the point — cutting it to 2 moved nothing past the renderer's quantizer.
    // It is belt-and-braces on top of the ROI, and it is the ROI that has to stay generous.
    // SeedProbe §24 measures the drift directly rather than trusting either.
    private const int SweepMargin = 20;

    // Deepest ambient occlusion, at a cell walled in on every side of its 5x5. Small on purpose:
    // this is contact shading that reads as depth in a corner, not a second shadow system.
    private const float AoDepth = 0.32f;

    // How far contact shading reaches. It counted the four orthogonal neighbours, so every wall
    // wore a hard one-cell dark ring and a diagonal wall shaded nothing; the 5x5 below, weighted
    // by inverse squared distance, fades it over two cells and lets corners shade round.
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

    // How much the penumbra spreads per step of the sweep, as a VARIANCE in cells squared.
    //
    // IT IS A VARIANCE AND NOT A BLUR SHARE, AND THAT IS THE WHOLE POINT. The four taps below
    // reconstruct the upwind sample at a FRACTIONAL minor offset by linear interpolation, and
    // linear interpolation is not free: it adds f(1-f) of variance of its own, zero at drift 0
    // and 1 and a quarter of a cell squared at drift 0.5. So a fixed blur share made the
    // penumbra's WIDTH a function of the sun's bearing — hard-edged twice a quarter-day and
    // softest between, with nothing a player could see a cause for. Naming the total instead and
    // spending the blur on whatever the interpolation did NOT add holds the width constant at
    // exactly the same four taps: SeedProbe's light/the-penumbra-keeps-its-width-across-the-day
    // measures 1.40-1.43 cells over the whole day (1.02x), against 1.04-1.55 (1.50x) with the
    // share pinned.
    //
    // The value is the old dial's MEAN over a day (0.20 + the mean of f(1-f)), so the shadow
    // looks as it did on average and only stops breathing. Raising it softens every penumbra;
    // it must stay above 0.25 or the compensation would need a negative blur share at drift 0.5.
    private const float PenumbraSpread = 0.3667f;

    // Below this the sky contributes nothing worth a sweep — underground, at the two handovers
    // between sun and moon, and on a starless night where nothing directional is up at all.
    private const float MinSkyShadow = 0.002f;

    // A SHADOW IS NOT A DIMMER SWITCH. Losing the sun leaves a cell lit by the sky, which is
    // cool, so shadowed ground shifts blue as well as dark — that is what makes a shadow read as
    // light behaving rather than as the map being turned down. Red loses the most, blue least.
    // Scaling all three channels equally was tried first and comes out muddy: grey-brown
    // grassland just becomes darker grey-brown, and the eye reads that as a rendering fault.
    private const float ShadowLossR = 1.00f;
    private const float ShadowLossG = 0.92f;
    private const float ShadowLossB = 0.68f;

    // AND A MOON SHADOW GOES COOLER STILL. Night ambient is already blue-dominant (18, 22, 45),
    // so a shadow that spent the channels the way the sun's does would come out as a slightly
    // darker blue and read as the brightness being turned down. Taking nearly all of the red and
    // barely half of the blue keeps moonlit shade separable from open moonlit ground at a tenth
    // of the daylight amplitude, which is the only budget the night has.
    // PATH-D-PORT: the night's shade is a HUE shift as much as a level one. A renderer that
    // reproduces the depth and drops the per-channel split will draw a night with no relief in it.
    private const float MoonShadowLossR = 1.00f;
    private const float MoonShadowLossG = 0.86f;
    private const float MoonShadowLossB = 0.52f;

    // ── How tall the thing casting is ─────────────────────────────────────────────────────────
    // Everything used to cast the same shadow. A boulder threw exactly what a mountainside threw,
    // because the sweep asked one question — does this cell block sight — and a bool has no room
    // for a second answer.
    //
    // A caster now EMITS its own height instead of a flat 1, and because the shadow fades
    // geometrically that buys length as well as depth for free: a shadow is visible until
    // height * decay^m falls under the renderer's colour step, so halving the height takes
    // ln 2 / ln(1/decay) cells off the end of it — about seven at a low sun, five at noon.
    // Per-caster DECAY would be the other way to spend it and is not available: the sweep carries
    // one line of history and a cell's shadow is a blend of several origins, so there is nowhere
    // to keep whose shadow this was. Emission is the O(1) form.
    //
    // THE TABLE IS COVERED BY THE LIGHT CACHE WITHOUT THE CACHE KNOWING IT EXISTS, which is the
    // same shape as folding the moon into SunState. It is a pure function of the tile types, it
    // is rebuilt exactly when GameMap.SightVersion moves, and that version is already a cache
    // key — so a field cached under one version can never be served against a table built under
    // another. Adding a key would have been the alternative, and a second thing to remember.
    //
    // PATH-D-PORT: the contract is that a caster contributes a shadow scaled by its own height in
    // [0,1] and that the field takes the MAX of what a cell emits and what arrives at it, so a
    // low caster standing in a high caster's shadow never shortens it.
    // Anything that does not block sight casts nothing, so this is only ever asked about the six
    // types GameMap.ComputeOpaque admits. An unlisted one falls to full height, which is exactly
    // what every caster did before this table existed — so a new sight-blocker behaves as it
    // always would have until somebody decides otherwise. SeedProbe derives the opaque set from
    // the map itself and fails if one of them is missing a deliberate entry.
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

    // ONE CELL, NOT THE WHOLE FLOOR. Rebuilding the table whenever the sight version moved was
    // correct and cost 3.6 ms per tile edit in Release on a 945x945 floor — against a turn of 1
    // to 4 ms, and mining is a repeated action, so it roughly doubled a mining strike. GameMap
    // patches its own opacity table one cell at a time for exactly this reason; this follows it.
    //
    // THE PATCH IS DEFERRED RATHER THAN APPLIED HERE, because GameMap raises this event BEFORE it
    // updates its opacity table — so at this moment the answer to "does that cell block sight
    // now" is still the old one, and the cell would be written from stale data.
    //
    // Dropping the cell without advancing the version is what makes the cap safe: the stamp then
    // trails the map's and the next sweep rebuilds everything.
    //
    // THE STAMP ONLY ADVANCES WHEN THIS EVENT ACCOUNTS FOR THE WHOLE MOVE. SetTileType bumps the
    // sight version by one and then raises this, so a version exactly one past the stamp is this
    // edit and nothing else. Anything further is a bump that raised no event — OpenDoor at
    // runtime, InvalidateOpacity and RebuildOpacity in mapgen — whose cell is in no list.
    // Stamping over it turned the full rebuild it owed into a one-cell patch, so an opened door
    // kept casting a closed door's shadow whenever a mining strike or a trap flip landed in the
    // following turn. Leaving the stamp behind is what makes the next sweep rebuild everything.
    // TileVerifyProbe --light section 6 drives both routes.
    private void OnCasterTileChanged(int x, int y, TileType oldType, TileType newType)
    {
        if (_casterHeightMap is not { } m) return;
        if (_casterDirty.Count >= CasterDirtyLimit) return;
        _casterDirty.Add(y * Width + x);
        if (m.SightVersion == _casterHeightVersion + 1) _casterHeightVersion = m.SightVersion;
    }

    private byte HeightAt(ReadOnlySpan<bool> op, GameMap map, int i)
    {
        if (!op[i]) return 0;                     // opacity decides WHETHER, the type decides how much
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
            // swap arm is defence against a future caller, not a case that happens today.
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
    // A SHADOWED CELL BESIDE A BRIGHTLY LIT ONE WAS EXACTLY AS DARK AS ONE IN THE MIDDLE OF A
    // WOOD. Measured in the plainest geometry there is - a long wall with a single gap in it,
    // under a noon sun - the shadow arriving four cells to the side of the bright streak through
    // the gap and eight cells to the side read 0.8650 and 0.8650: identical to four decimal
    // places, though one of them is twice as far from any light as the other. The sweep answers
    // "how much of the sun is blocked here" and nothing else, so light that lands on open ground
    // stays there.
    //
    // Real shade is not the absence of light, it is light that arrived by a longer route: a lone
    // tree's shadow on a bright lawn is pale because the lawn all around it throws light back
    // into it, while the same tree in the middle of a thicket casts a shadow with nothing to fill
    // it. That is the cue separating a leaf's shadow from a building's, and it was missing - both
    // read at exactly the depth the caster's height dictates.
    //
    // WHAT IT IS: one gather of the direct light landing nearby, given back as a REDUCTION of the
    // shadow rather than as light of its own. That distinction is the whole safety argument.
    // Written as
    //
    //     effective = arriving * (1 - BounceLift * nearbyLit)
    //
    // it is exactly zero wherever `arriving` is zero, so open ground still reads the ambient
    // colour byte for byte, and exactly zero where `nearbyLit` is zero, so the middle of a wall
    // mass keeps the full depth it had. Only a shadow with light around it moves. Adding a bounce
    // TERM instead would have brightened the whole map, which is a gamma knob wearing a lighting
    // feature's clothes.
    //
    // WHY A GATHER AND NOT A SECOND SWEEP: indirect light has no direction to sweep along. It is
    // the low-frequency average of what is lit nearby, which is the first-order gather every
    // grid-based method makes - one Jacobi iteration of a radiosity solve, an irradiance probe, a
    // propagation volume's first bounce. What is borrowed is the SHAPE; nothing else transfers,
    // because those work per pixel over a frame and this is per tile over a turn.
    //
    // AND IT IS O(1) PER CELL, WHICH THE RADIUS DOES NOT CHANGE. A separable running window costs
    // one add, one subtract and one multiply per cell per axis whatever its width, so the radius
    // below is chosen for how it LOOKS and buys nothing back by being small. Both passes run
    // row-major - the vertical one carries a row-length accumulator and streams rows in and out
    // rather than walking columns - because a column walk over a 500-cell-wide region misses
    // cache on every read.
    //
    // A CELL THAT BLOCKS SIGHT CONTRIBUTES NOTHING TO THE GATHER, and that is what makes the term
    // do two jobs for one price. A wall is not a light source, so a neighbourhood that is mostly
    // wall gathers little and its shade stays deep, while an open clearing gathers nearly
    // everything and fills its shadows in. That is large-scale occlusion at ~15 cells, against
    // the contact term's one, and the two do not overlap.
    //
    // WHAT IT GETS WRONG, said plainly: a running window does not know about walls, so light can
    // reach across a partition it should not have crossed. A diffusion that respects opacity
    // would remove it and costs a pass per cell of reach; that is not worth paying to delete an
    // artefact smaller than the effect itself and of the same sign as the sky bouncing over the
    // wall in the first place. SeedProbe measures the leak rather than leaving it as prose.
    //
    // PATH-D-PORT: the contract is one low-frequency gather of `1 - shadow` over cells that do
    // not block sight, applied MULTIPLICATIVELY to the shadow and never additively to the light.
    // A renderer with a real GI pass should replace the gather and keep the application - it is
    // the multiplicative form that guarantees open ground is untouched.
    //
    // Nothing new enters the light cache's key. The field is a pure function of `_shadow` and the
    // opacity table, and both are covered already - the first through SunState and the ambient
    // triple, the second through SightVersion. It holds no per-tile state between turns either,
    // so it cannot fall into the blind spot --verify-light has for a table both sides share.
    private const int BounceRadius = 7;

    // Deepest share of a shadow its surroundings may give back.
    //
    // ONE VALUE FOR BOTH SKIES, AND THAT WAS NOT THE FIRST ANSWER. Splitting it looked obviously
    // right: the night's shadow is only 4.1 of 255 deep at the foot of a lone caster, about two
    // of the renderer's own colour steps, while the day's is 47 - so the day can spend a fifth of
    // its depth without anyone noticing and the night plainly cannot. A day-only lift was built
    // and measured, and it is wrong for a reason that has nothing to do with either number: it
    // puts the two skies on DIFFERENT LIGHTING MODELS, so every day-against-night comparison
    // stops being commensurable. SeedProbe found it immediately - the moon's shadow became a
    // LARGER share of its own sky than the sun's (0.22 against 0.20), not because the night had
    // changed but because only the day had. One sweep already serves both bodies; one gather does
    // too, and that is what keeps the night a fainter version of the day rather than a different
    // renderer.
    //
    // Measured with a single lift, on a lone caster in a wholly open field - the geometry that
    // gathers most, so the worst case for the night:
    //
    //              loss at the caster's foot      share of its own sky      shallowest / deepest
    //   lift 0.00    day 47.0    night 4.1          0.26 / 0.22               1.81 / 6.98 dL*
    //   lift 0.30    day 36.8    night 3.3          0.20 / 0.17               1.59 / 6.21
    //
    // The ratio the night is held to is preserved (0.85 of the day's share, against 0.85 before),
    // and the night's shade still clears the ~1.0 dL* threshold of vision by 1.6x at its faintest
    // bearing and 6.2x at its deepest. What the night gives up is the LONE-CASTER case in wholly
    // open ground; the deep shade it depends on comes from masses of wall and rock, which gather
    // little and are barely touched. That is the feature working rather than a cost of it.
    private const float BounceLift = 0.30f;

    // The dials themselves, so a check can state its expectation in terms of the shipped values
    // rather than a copy of them. Read-only: these are measurement seams, not setters.
    public static float IndirectLift => BounceLift;
    public static int IndirectRadius => BounceRadius;

    // The gathered field, region-local like _shadow; the ring of horizontal-pass rows the
    // vertical window slides over; and two row-length scratches.
    private float[]? _bounce, _bounceRing, _bounceAcc, _bounceSrc;

    // THE HORIZONTAL PASS IS STREAMED ONE ROW AHEAD OF THE VERTICAL WINDOW, NOT MATERIALISED.
    // The window at output row y spans rows [y-R, y+R] and takes row y+1+R in as row y-R leaves,
    // so only 2R+2 gathered rows are ever live. Holding them in a ring keeps the pass out of main
    // memory: the region-sized intermediate used to be written in full and read back (851 KB
    // each way at 316x91), and the gathered field was then read a third time by the apply pass.
    // Now a row is applied the moment its gather completes, while it is still in cache. Every
    // row is gathered exactly as before and the window's adds and subtracts run in the same
    // order, so the field is byte-identical.
    private const int BounceRingRows = 2 * BounceRadius + 2;

    // One row of the horizontal gather — the direct light landing on transparent ground, averaged
    // over a window of 2R+1 columns — into dst[dstBase .. dstBase+rw).
    private void GatherRow(ReadOnlySpan<bool> op, int rx0, int ry0, int rw, int y, float[] dst, int dstBase)
    {
        const int R = BounceRadius;
        // ONE ROW OF SOURCE VALUES, NOT A WHOLE REGION OF THEM: a row is a couple of kilobytes and
        // stays in cache between being built and being slid over.
        //
        // BRANCHLESS, for the reason ApplyRow is: whether a cell blocks sight is DATA and not a
        // pattern, so a ternary is an unpredictable branch on every cell of the region. Reading
        // the bool as a byte and multiplying costs one instruction and never mispredicts.
        // Building the row's source once also halves the loads — the slide used to re-derive
        // every value from both arrays twice, once entering the window and once leaving it.
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
        // THE DIVISOR IS CONSTANT EXCEPT AT THE TWO ENDS, so the interior takes a reciprocal
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

    // ── Measurement seam ───────────────────────────────────────────────────────────────────
    // Where the last sweep's region sat, so a reader can index _shadow at all: the buffer is
    // region-local and the region travels with the player. Written by the sweep, read only by
    // TryGetShadingTerms below.
    private int _shadowRx0, _shadowRy0, _shadowRw, _shadowRh;
    private bool _shadowSwept;

    // Shadow leaving the previous / current line of the sweep, indexed by the MINOR coordinate.
    // Padded by two at each end so the four taps never fall off.
    private float[]? _linePrev, _lineCur;

    // True when the flat Span.Fill still applies — the player has turned shading off.
    private static bool ShadingEnabled => UserSettings.Current.TerrainShading;

    // Sky shadows plus ambient occlusion over the region, written into the light grid.
    private void FillShadedRegion(GameMap map, LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        using var _ = Profiler.Begin("Lighting.Shading");
        var op = map.OpacityTable;
        using (Profiler.Begin("Lighting.Casters")) EnsureCasterHeights(map, op);
        _shadowSwept = false;

        // Cloud and fog scatter whatever is up there, and scattered light casts no shadow — so an
        // overcast floor keeps its brightness and loses its relief, by night as much as by day.
        // Render-only: weather cannot reach a roll from here.
        float strength = map.HasSky
            ? DayNightCycle.ShadowStrength * WeatherSystem.SunlightScale
            : 0f;
        if (strength <= MinSkyShadow)
        {
            FillOccludedRegion(op, ambient, x0, y0, x1, y1);
            return;
        }

        // WHICH BODY IS CASTING is the only thing the sweep needs to know, and it decides one
        // thing: how the shadow spends its three channels. Direction, reach and depth are already
        // the sky's own, whichever is up.
        var loss = DayNightCycle.SunIsUp
            ? (R: ShadowLossR, G: ShadowLossG, B: ShadowLossB)
            : (R: MoonShadowLossR, G: MoonShadowLossG, B: MoonShadowLossB);

        var (ux, uy) = DayNightCycle.ShadowDirection;
        float decay = DayNightCycle.ShadowDecay;
        int sx = ux >= 0f ? 1 : -1;
        int sy = uy >= 0f ? 1 : -1;

        // Extend only the UPWIND side of each axis — the downwind side needs no history.
        int rx0 = sx > 0 ? Math.Max(0, x0 - SweepMargin) : x0;
        int rx1 = sx > 0 ? x1 : Math.Min(Width, x1 + SweepMargin);
        int ry0 = sy > 0 ? Math.Max(0, y0 - SweepMargin) : y0;
        int ry1 = sy > 0 ? y1 : Math.Min(Height, y1 + SweepMargin);
        int rw = rx1 - rx0, rh = ry1 - ry0;
        if (rw <= 0 || rh <= 0) return;

        EnsureShadowBuffers(rw, rh);

        // ── The seam at 45 degrees ────────────────────────────────────────────────────────────
        // The sweep steps along the sky's MAJOR axis, and which axis that is flips at 45 and 135
        // degrees. Both sides of the flip are individually correct and the kernel is the same
        // shape at the boundary — but the two are different RASTERISATIONS of the same diagonal
        // (one staircases along x, the other along y), and a map is not mirror-symmetric about
        // its own diagonal, so they do not agree. The field was therefore DISCONTINUOUS in the
        // sun's bearing: it snapped twice a day, and no bearing step count could fix it.
        //
        // Measured on F7 Forest at a 480-step ladder, where one step is 0.375 degrees: ordinary
        // steps either side of 45 move 0.63-0.78 dL* and put ZERO cells over a just-noticeable
        // difference, while the single step across the flip moved 4.15 dL* and 1,034 cells —
        // 6x its neighbours, and it does not shrink as the ladder is refined (8.24 at 48 steps,
        // 5.66 at 96, 4.33 at 192, 4.15 at 480). It is a floor, not a step.
        //
        // So both rasterisations are swept through the seam and blended. The weight is a pure
        // function of the bearing, reaching exactly 0.5 where the axes trade places, so the field
        // is continuous there by construction rather than by tuning. Away from the seam the
        // weight saturates and only one sweep runs, which is what keeps this cheap: the window is
        // about +/-6 degrees, so it costs a second sweep on ~7% of bearings and nothing at all on
        // the rest. It is also why the window has to be NARROW for a second reason — outside it
        // the weaker axis has a drift above 1, which SweepShadow clamps, and a clamped drift
        // points the spine the wrong way.
        //
        // PATH-D-PORT: this exists only because the field comes out of a one-line-history sweep
        // over a grid. The CONTRACT is that the shadow field is continuous in the sun's bearing —
        // a renderer that marches a ray, or works per pixel, gets that for free and should drop
        // the blend rather than reproduce it.
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
        // AFTER the seam blend, never inside a sweep: at a bearing inside the diagonal window the
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

    // One ordered pass along the given axis, filling _shadow with what ARRIVES at each cell.
    // `majorIsX` is passed rather than derived because the diagonal seam above sweeps BOTH
    // rasterisations of the same bearing and blends them; deriving it here would make that
    // impossible and is what left the field discontinuous at 45 degrees.
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

        // FOUR TAPS, BECAUSE THE SPINE MOVES BETWEEN THEM. The upwind point sits at a
        // FRACTIONAL minor offset — `drift` of a cell back along the minor axis — so the two
        // taps that straddle it are not a fixed pair. Three taps centred on the cell itself
        // straddle the spine only when drift is 0; at drift 1 the spine has slid onto a tap and
        // the fringe collapses to one side again, which is exactly the defect this sweep exists
        // to fix, reappearing at the diagonal bearings instead of the horizontal ones (measured:
        // 0.0% against 8.8% at a 45-degree sun).
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

                // A caster is not darkened by its OWN shadow: it emits its height downwind while
                // rendering with whatever arrived at it. Cells otherwise inherit what arrived.
                // The MAX matters — a boulder standing inside a mountain's shadow must not cut
                // that shadow down to a boulder's. Taken branchlessly: whether a cell casts is
                // data, so a compare-and-branch here mispredicts on a forest. `arriving` is never
                // negative or NaN (the kernel is non-negative and so is every history value), so
                // Max returns exactly what the select did.
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
        // The gather still runs over it, so the measurement seam holds THIS region's field rather
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

        // ONE TRAVERSAL PER ROW, NOT THREE. Writing the row, then adding the row entering the
        // window, then subtracting the row leaving it, as three separate x-loops walks the region
        // three times and re-reads the accumulator each time. Fused, the accumulator is touched
        // once per cell and the two ring rows are plain forward streams. Measured by direct
        // profiler attribution rather than by differencing two runs: 1.62 -> 1.25 ms per cache
        // rebuild, which is where most of the remaining saving on this pass was.
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

    // Read-only measurement seam for the in-game lighting debug overlay, in the same family as
    // SourcesLastUpdate: the two terms this file folds into the light grid, BEFORE they are
    // folded, so the overlay shows what the sweep computed rather than what survived the mix.
    // `shadow` is what ARRIVES at the cell in [0,1] and is only meaningful when this returns
    // true — there is no sweep underground, on a starless night, or under weather flat enough to
    // scatter the sky away. `contact` is the ambient-occlusion multiplier and always applies.
    public bool TryGetShadingTerms(GameMap map, int x, int y, out float shadow, out float contact)
        => TryGetShadingTerms(map, x, y, out shadow, out contact, out _);

    // `bounce` is the gathered share of direct light landing near the cell, in [0,1], BEFORE it
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
