using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SAOTRPG.Systems;

namespace SAOTRPG.Map;

// The directional half of the lighting model.
//
// Ambient is the sky: one colour everywhere, no bearing, so a forest canopy lit the ground
// exactly as brightly as the clearing beside it and nothing on the map cast a shadow at all
// except by withholding torchlight. This file adds the two terms that give the world relief —
// a sun with a bearing, and a small ambient-occlusion term so a cell hemmed in by walls sits
// darker than one in the open.
//
// WHY A SWEEP AND NOT A RAY WALK. Sampling backwards along the sun vector from every cell needs
// an inner loop per cell — on the order of a million opacity probes per turn across the lighting
// ROI, which is not affordable beside a turn that already costs about 4 ms. The same field falls
// out of a single ordered pass instead: the shadow ARRIVING at a cell is a weighted blend of the
// shadow LEAVING the line one step upwind of it. That is O(1) per cell.
//
// THE SWEEP RUNS ALONG THE SUN'S MAJOR AXIS, AND THAT IS THE LOAD-BEARING CHOICE. Sweeping
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

    // Deepest ambient occlusion, at a cell walled on all four sides. Small on purpose: this is
    // contact shading that reads as depth in a corner, not a second shadow system.
    private const float AoDepth = 0.28f;

    // Share of each step's shadow fed back from the two lateral neighbours rather than from the
    // upwind line directly. This is the penumbra dial: at 0 a shadow is a hard line one cell
    // wide, and past about 0.5 it stops having a direction at all.
    private const float PenumbraBlur = 0.20f;

    // Below this the sun contributes nothing worth a sweep — underground, at night, and through
    // the twilight ramp at either end of the day.
    private const float MinSunShadow = 0.002f;

    // A SHADOW IS NOT A DIMMER SWITCH. Losing the sun leaves a cell lit by the sky, which is
    // cool, so shadowed ground shifts blue as well as dark — that is what makes a shadow read as
    // light behaving rather than as the map being turned down. Red loses the most, blue least.
    // Scaling all three channels equally was tried first and comes out muddy: grey-brown
    // grassland just becomes darker grey-brown, and the eye reads that as a rendering fault.
    private const float ShadowLossR = 1.00f;
    private const float ShadowLossG = 0.92f;
    private const float ShadowLossB = 0.68f;

    // Shadow arriving at each cell of the swept region, row-major over that region alone. The
    // sweep fills it and the apply pass below reads it; keeping the two apart is what lets the
    // sweep run along whichever axis the sun needs while the light writes stay in storage order.
    private float[]? _shadow;

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

    // Sun shadows plus ambient occlusion over the region, written into the light grid.
    private void FillShadedRegion(GameMap map, LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        using var _ = Profiler.Begin("Lighting.Shading");
        var op = map.OpacityTable;
        _shadowSwept = false;

        // Cloud and fog scatter the sun, and scattered light casts no shadow — so an overcast
        // floor keeps its brightness and loses its relief. Render-only: weather cannot reach a
        // roll from here.
        float strength = map.HasSky
            ? DayNightCycle.ShadowStrength * WeatherSystem.SunlightScale
            : 0f;
        if (strength <= MinSunShadow)
        {
            FillOccludedRegion(op, ambient, x0, y0, x1, y1);
            return;
        }

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
        SweepShadow(op, rx0, ry0, rw, rh, ux, uy, sx, sy, decay);
        _shadowRx0 = rx0; _shadowRy0 = ry0; _shadowRw = rw; _shadowRh = rh;
        _shadowSwept = true;
        ApplyShading(op, ambient, strength, rx0, ry0, rw, x0, y0, x1, y1);
    }

    private void EnsureShadowBuffers(int rw, int rh)
    {
        int need = rw * rh;
        if (_shadow == null || _shadow.Length < need) _shadow = new float[need];
        int line = Math.Max(rw, rh) + 4;
        if (_linePrev == null || _linePrev.Length < line)
        {
            _linePrev = new float[line];
            _lineCur = new float[line];
        }
    }

    // One ordered pass along the sun's major axis, filling _shadow with what ARRIVES at each cell.
    private void SweepShadow(ReadOnlySpan<bool> op, int rx0, int ry0, int rw, int rh,
                             float ux, float uy, int sx, int sy, float decay)
    {
        bool majorIsX = MathF.Abs(ux) >= MathF.Abs(uy);
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
        float b = PenumbraBlur, f = drift;
        float wFar = f * b * 0.5f;                       // two steps along the drift
        float wDrift = f * (1f - b) + (1f - f) * b * 0.5f;
        float wCenter = (1f - f) * (1f - b) + f * b * 0.5f;
        float wBack = (1f - f) * b * 0.5f;               // one step against the drift

        var shadow = _shadow!;
        float[] prev = _linePrev!, cur = _lineCur!;
        Array.Clear(prev, 0, minorCount + 4);
        Array.Clear(cur, 0, minorCount + 4);

        ref bool opBase = ref MemoryMarshal.GetReference(op);
        ref float shBase = ref MemoryMarshal.GetArrayDataReference(shadow);

        for (int mj = 0; mj < majorCount; mj++)
        {
            int major = sMajor > 0 ? mj : majorCount - 1 - mj;
            // Re-taken per line because the two buffers swap; one added offset per line against
            // a few hundred cells of work inside it is not worth the ref juggling.
            ref float prevBase = ref MemoryMarshal.GetArrayDataReference(prev);
            ref float curBase = ref MemoryMarshal.GetArrayDataReference(cur);

            for (int mi = 0; mi < minorCount; mi++)
            {
                // Lines carry two cells of padding at each end, so index by mi + 2.
                int p = mi + 2;
                float arriving = (wCenter * Unsafe.Add(ref prevBase, p)
                                + wDrift * Unsafe.Add(ref prevBase, p - sMinor)
                                + wFar * Unsafe.Add(ref prevBase, p - 2 * sMinor)
                                + wBack * Unsafe.Add(ref prevBase, p + sMinor)) * decay;

                int lx = majorIsX ? major : mi;
                int ly = majorIsX ? mi : major;
                int shIdx = ly * rw + lx;
                int mapIdx = (ry0 + ly) * Width + (rx0 + lx);

                // A caster is not darkened by its OWN shadow: it emits a full one downwind while
                // rendering with whatever arrived at it. Cells otherwise inherit what arrived.
                bool blocks = Unsafe.Add(ref opBase, mapIdx);
                Unsafe.Add(ref curBase, p) = blocks ? 1f : arriving;
                Unsafe.Add(ref shBase, shIdx) = arriving;
            }

            (prev, cur) = (cur, prev);
        }

        _linePrev = prev;
        _lineCur = cur;
    }

    // Second pass, in storage order: fold the swept shadow and the contact term into the light.
    private void ApplyShading(ReadOnlySpan<bool> op, LightRgb ambient, float strength,
                              int rx0, int ry0, int rw, int x0, int y0, int x1, int y1)
    {
        // Light is written over the interior only, so the occlusion probes need no edge guards.
        // The outermost ring of a floor is border wall nobody stands on, and a cell there missing
        // its contact shading is not something anyone can see.
        int wx0 = Math.Max(1, x0), wx1 = Math.Min(Width - 1, x1);
        int wy0 = Math.Max(1, y0), wy1 = Math.Min(Height - 1, y1);
        if (wx1 <= wx0 || wy1 <= wy0)
        {
            // A region with no interior — a map one or two cells across. Returning here would
            // leave the light grid holding whatever was in it, which is the silent-stale failure
            // this whole layer is built to avoid.
            FillFlat(ambient, x0, y0, x1, y1);
            return;
        }

        float ar = ambient.R, ag = ambient.G, ab = ambient.B;
        // Pre-multiplied per-channel loss, so a shaded cell costs one multiply-subtract per
        // channel rather than a shared factor and then three multiplies.
        float lossR = ar * strength * ShadowLossR;
        float lossG = ag * strength * ShadowLossG;
        float lossB = ab * strength * ShadowLossB;

        var shadow = _shadow!;
        ref bool opBase = ref MemoryMarshal.GetReference(op);
        ref LightRgb lightBase = ref MemoryMarshal.GetArrayDataReference(_light);
        ref float shBase = ref MemoryMarshal.GetArrayDataReference(shadow);

        for (int y = wy0; y < wy1; y++)
        {
            int row = y * Width;
            int shRow = (y - ry0) * rw - rx0;
            for (int x = wx0; x < wx1; x++)
            {
                int i = row + x;
                float arriving = Unsafe.Add(ref shBase, shRow + x);

                // Contact shading counted BRANCHLESSLY. Whether a neighbour blocks sight is data,
                // not a pattern, so four `if`s here are four unpredictable branches on every one
                // of 150,000 cells. It is neutral across channels — it is the sky being blocked,
                // not the sun, so only the sun term carries the colour shift.
                int occl = Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i - 1))
                         + Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i + 1))
                         + Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i - Width))
                         + Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i + Width));
                float ao = 1f - AoDepth * 0.25f * occl;

                ref LightRgb cell = ref Unsafe.Add(ref lightBase, i);
                cell.R = (ar - lossR * arriving) * ao;
                cell.G = (ag - lossG * arriving) * ao;
                cell.B = (ab - lossB * arriving) * ao;
            }
        }

        // The interior clamp above leaves the region's outermost ring unwritten, which would show
        // as a stale rim once it scrolled inside the viewport. Fill it flat.
        FillEdgeRing(ambient, x0, y0, x1, y1, wx0, wy0, wx1, wy1);
    }

    private void FillFlat(LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        var light = _light.AsSpan();
        for (int y = y0; y < y1; y++) light.Slice(y * Width + x0, x1 - x0).Fill(ambient);
    }

    private void FillEdgeRing(LightRgb ambient, int x0, int y0, int x1, int y1,
                              int wx0, int wy0, int wx1, int wy1)
    {
        var light = _light.AsSpan();
        for (int y = y0; y < y1; y++)
        {
            if (y >= wy0 && y < wy1)
            {
                if (x0 < wx0) light.Slice(y * Width + x0, wx0 - x0).Fill(ambient);
                if (wx1 < x1) light.Slice(y * Width + wx1, x1 - wx1).Fill(ambient);
            }
            else
            {
                light.Slice(y * Width + x0, x1 - x0).Fill(ambient);
            }
        }
    }

    // Ambient occlusion alone — underground and at night, where there is no sun to sweep for.
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
                int occl = Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i - 1))
                         + Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i + 1))
                         + Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i - Width))
                         + Unsafe.As<bool, byte>(ref Unsafe.Add(ref opBase, i + Width));
                ref LightRgb cell = ref Unsafe.Add(ref lightBase, i);
                if (occl == 0)
                {
                    cell.R = ar; cell.G = ag; cell.B = ab;
                    continue;
                }
                float ao = 1f - AoDepth * 0.25f * occl;
                cell.R = ar * ao;
                cell.G = ag * ao;
                cell.B = ab * ao;
            }
        }

        if (wx1 > wx0 && wy1 > wy0) FillEdgeRing(ambient, x0, y0, x1, y1, wx0, wy0, wx1, wy1);
        else FillFlat(ambient, x0, y0, x1, y1);
    }

    // Read-only measurement seam for the in-game lighting debug overlay, in the same family as
    // SourcesLastUpdate: the two terms this file folds into the light grid, BEFORE they are
    // folded, so the overlay shows what the sweep computed rather than what survived the mix.
    // `shadow` is what ARRIVES at the cell in [0,1] and is only meaningful when this returns
    // true — there is no sweep at night, underground, or under weather flat enough to scatter
    // the sun away. `contact` is the ambient-occlusion multiplier and always applies.
    public bool TryGetShadingTerms(GameMap map, int x, int y, out float shadow, out float contact)
    {
        shadow = 0f;
        contact = 1f;
        if (x <= 0 || y <= 0 || x >= Width - 1 || y >= Height - 1) return false;

        var op = map.OpacityTable;
        int i = y * Width + x;
        int occl = (op[i - 1] ? 1 : 0) + (op[i + 1] ? 1 : 0)
                 + (op[i - Width] ? 1 : 0) + (op[i + Width] ? 1 : 0);
        contact = 1f - AoDepth * 0.25f * occl;

        if (!ShadingEnabled || !_shadowSwept || _shadow is null) return false;
        int lx = x - _shadowRx0, ly = y - _shadowRy0;
        if ((uint)lx >= (uint)_shadowRw || (uint)ly >= (uint)_shadowRh) return false;
        shadow = _shadow[ly * _shadowRw + lx];
        return true;
    }
}
