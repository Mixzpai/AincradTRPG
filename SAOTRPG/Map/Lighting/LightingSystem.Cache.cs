
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Lighting;

// The ambient + shadow + contact field, cached over a padded region of the world.
//
// Why it is cacheable: that field is a pure function of the map's opacity, the sun's quantized
// state, the ambient colour, the weather's sunlight scale and whether the floor has sky. None of
// those move when the player attacks, rests, mines, waits or walks into a wall, and the field is
// in world space, so they do not move when the player walks either. Only the region being lit
// travels.
//
// Why the region is padded rather than exact: an exact region would miss on every step the
// player takes, which is most turns, and the refresh is the expensive half. Padding the cached
// region means a step usually lands inside what is already computed — the same trick the map's
// damage-tracked tile layer uses when the camera scrolls. The pad is paid for once per refresh
// and amortised over every turn until the player walks out of it.
//
// What is not cached: point lights. The torch's colour and radius follow the sun and the sources
// present move with the player, so they are re-added on top of the restored base every turn. The
// cache holds strictly what FillShadedRegion produces, before AddSource touches anything.
//
// How a hit restores the base: not by copying the whole served region every turn, but by
// re-basing only the boxes the sources wrote last turn. Between refreshes the grid over the
// cached region equals the cache everywhere except inside those boxes, because nothing else
// writes it: the base is a pure function of the world, and AddSource notes every box it merges.
// A refresh re-sweeps the whole cached region and empties the list. Anything else that writes
// the grid (the flat fill) must drop the cache, or the next hit would re-base too little.
//
// A missed dependency fails silently: it leaves the map lit by an earlier sun or an earlier
// floor, and stale light looks like light. --verify-light checks for exactly that.
public sealed partial class LightingSystem
{
    // How far past the lit region the cache reaches. Every cell of it is swept on a refresh, so
    // this trades a bigger refresh for rarer ones.
    private const int CachePad = 72;

    // Shadow history at the cached region's edge needs no inset: a cell near the swept region's
    // upwind edge carries only SweepMargin steps of history, but a cell is only served when the
    // whole needed region is inside the cached one, and the needed region reaches well past the
    // screen on both axes, so a served cell with short history is always off screen.

    private LightRgb[]? _cache;
    private int _cacheX0, _cacheY0, _cacheW, _cacheH;
    private bool _cacheValid;

    // Where the sources wrote since the base was last re-established: the only cells of the
    // cached region that differ from the cache on a hit. Every box lies inside the cached region
    // by construction — a source is admitted within the served region, its box reaches at most
    // MaxSourceReachX + 1 columns past it, and a hit requires that margin cached — and the region
    // only moves on a refresh, which empties the list before anything could be restored against
    // the new one.
    private readonly List<(int X0, int Y0, int X1, int Y1)> _sourceBoxes = new();

    // Everything the cached field depends on. If any of these differs the field must be rebuilt;
    // if one is missing from here the field goes stale in silence.
    //
    // HasSky is keyed because it is a public settable property, even though no caller flips it
    // on a live map.
    //
    // The Terrain Shading setting is not among them: FillFromCache is only reached when it is
    // on, so a key for it would be true at every write and every read. Turning the setting off
    // takes the flat path, which overwrites the served region and therefore drops the cache (a
    // hit after it would re-base only the source boxes and leave the flat fill under everything
    // else), so turning it back on refreshes.
    //
    // Whose floor this is, which a version stamp cannot stand in for: two GameMaps both open at
    // SightVersion 0, so a field cached on one would serve as a hit on the other and light the
    // map from an earlier floor. EnsureCasterHeights keeps the same guard, and a cache hit is the
    // path that skips the sweep and therefore skips it, so the guard has to exist on both sides.
    // A LightingSystem is constructed by one GameMap and only ever handed that one; the map key
    // keeps that from being an unchecked assumption.
    // The sky key is SkyShade: the sun's quantized state, the ambient triple and the weather's
    // sunlight scale, plus what the state derives.
    private GameMap? _cacheMap;
    private SkyShade _cacheSky;
    private int _cacheSightVersion;
    private bool _cacheHasSky;

    private LightRgb[]? _verifyScratch;

    // ── The snapshot a refresh is building ─────────────────────────────────────────────────
    // While the padded region is being re-swept, every row a fill writes into the light grid is
    // copied into the cache the moment it is written, while it is still in the CPU cache, rather
    // than re-reading the whole region afterwards once the fill has evicted it. Null outside a
    // refresh: the verification sweep runs the same fills and must not touch the cache.
    private LightRgb[]? _mirror;
    private int _mirrorX0, _mirrorY0, _mirrorW;

    private void MirrorRow(int y, int x0, int x1)
    {
        if (_mirror is not { } snap) return;
        Array.Copy(_light, y * Width + x0, snap, (y - _mirrorY0) * _mirrorW + (x0 - _mirrorX0), x1 - x0);
    }

    private bool CacheCovers(int x0, int y0, int x1, int y1) =>
        x0 >= _cacheX0 && y0 >= _cacheY0
        && x1 <= _cacheX0 + _cacheW && y1 <= _cacheY0 + _cacheH;

    // The region a refresh around this lit region sweeps: padded, clipped to the map.
    private (int X0, int Y0, int W, int H) PaddedRegion(int x0, int y0, int x1, int y1)
    {
        int px0 = Math.Max(0, x0 - CachePad), py0 = Math.Max(0, y0 - CachePad);
        int px1 = Math.Min(Width, x1 + CachePad), py1 = Math.Min(Height, y1 + CachePad);
        return (px0, py0, px1 - px0, py1 - py0);
    }

    // Re-establish the base over the served region: on a hit by re-basing the boxes the sources
    // wrote, otherwise by re-sweeping. The served region is only the trigger — the base is held
    // over the whole cached region either way. (px, py) is the player, for the worker's heading.
    private void FillFromCache(GameMap map, int px, int py, int x0, int y0, int x1, int y1)
    {
        var sky = DayNightCycle.ShadeAt(DayNightCycle.CurrentTurn);
        // Widened in X: a source admitted at the ROI edge writes a box up to MaxSourceReachX + 1
        // columns past it, and next turn's hit must be able to re-base that box.
        int reach = MaxSourceReachX + 1;
        bool covers = CacheCovers(Math.Max(0, x0 - reach), y0, Math.Min(Width, x1 + reach), y1);
        bool sameWorld = _cacheValid && ReferenceEquals(_cacheMap, map)
                         && _cacheSightVersion == map.SightVersion && _cacheHasSky == map.HasSky;
        bool hit = sameWorld && covers && _cacheSky == sky;
        if (hit) RestoreSourceBoxes();
        else
        {
            // A sky step alone re-shades the region already cached rather than re-centring it, so
            // the worker knows a turn ahead exactly which region the refresh will sweep.
            var (rx0, ry0, rw, rh) = sameWorld && covers
                ? (_cacheX0, _cacheY0, _cacheW, _cacheH) : PaddedRegion(x0, y0, x1, y1);
            if (!TryAdopt(map, sky, rx0, ry0, rw, rh)) RefreshCache(map, sky, rx0, ry0, rw, rh);
        }
        _sourceBoxes.Clear();

        CacheHits += hit ? 1 : 0;
        CacheMisses += hit ? 0 : 1;

        if (DebugMode.VerifyLight) VerifyAgainstFreshFill(map, sky, x0, y0, x1, y1, hit);
        Speculate(map, sky, px, py);
    }

    // Called by AddSource with the box it merged, clipped to the map.
    private void NoteSourceBox(int x0, int y0, int x1, int y1) => _sourceBoxes.Add((x0, y0, x1, y1));

    // Called when something other than a source has written the grid, so it does not hold the
    // base the cache describes: the next shaded turn must refresh rather than re-base the boxes.
    private void DropCache()
    {
        _cacheValid = false;
        _sourceBoxes.Clear();
    }

    private void RestoreSourceBoxes()
    {
        foreach (var (x0, y0, x1, y1) in _sourceBoxes)
        {
            // Loud rather than clamped: a box the cache does not cover would leave source light
            // baked into the grid, which is exactly the stale light this file exists to prevent.
            if (x0 < _cacheX0 || y0 < _cacheY0 || x1 > _cacheX0 + _cacheW || y1 > _cacheY0 + _cacheH)
                throw new InvalidOperationException(
                    $"source box ({x0},{y0})-({x1},{y1}) lies outside the cached light region "
                    + $"({_cacheX0},{_cacheY0}) {_cacheW}x{_cacheH} and cannot be re-based");
            int w = x1 - x0;
            for (int y = y0; y < y1; y++)
                Array.Copy(_cache!, (y - _cacheY0) * _cacheW + (x0 - _cacheX0),
                           _light, y * Width + x0, w);
        }
    }

    private void RefreshCache(GameMap map, in SkyShade sky, int px0, int py0, int w, int h)
    {
        var op = map.OpacityTable;
        using (Profiler.Begin("Lighting.Casters")) EnsureCasterHeights(map, op);

        // Swept straight into the light grid and mirrored into the cache row by row as the fills
        // write it, so the sweep needs no knowledge of the cache and there is one code path
        // producing the field rather than two that can drift. The grid is the base when this
        // returns; nothing is copied back.
        _cache = EnsureCapacity(_cache, w * h);
        _mirror = _cache; _mirrorX0 = px0; _mirrorY0 = py0; _mirrorW = w;
        try { FillShadedRegion(op, sky, map.HasSky, px0, py0, px0 + w, py0 + h); }
        finally { _mirror = null; }
        SetCacheKey(map, sky, px0, py0, w, h);
    }

    private void SetCacheKey(GameMap map, in SkyShade sky, int x0, int y0, int w, int h)
    {
        _cacheX0 = x0; _cacheY0 = y0; _cacheW = w; _cacheH = h;
        _cacheMap = map;
        _cacheSky = sky;
        _cacheSightVersion = map.SightVersion;
        _cacheHasSky = map.HasSky;
        _cacheValid = true;
    }

    // How often the sweep was skipped. Read by the verification probes.
    public static int CacheHits { get; private set; }
    public static int CacheMisses { get; private set; }

    public static (int Hits, int Misses) DrainCacheCounts()
    {
        var r = (CacheHits, CacheMisses);
        CacheHits = 0;
        CacheMisses = 0;
        return r;
    }

    // Turns on which --verify-light found a mismatch. Read by the verification probes, so they
    // need not depend on the log format.
    public static int VerifyBadTurns { get; private set; }

    // Recompute the field from scratch and compare it against what was just served. The fresh
    // result is what stays in the grid, so a wrong cache cannot corrupt play while verifying.
    // The served grid is the product of the restore, so a box it failed to re-base shows here
    // as a mismatch wherever that box meets the served region — no separate check is needed.
    private void VerifyAgainstFreshFill(GameMap map, in SkyShade sky,
                                        int x0, int y0, int x1, int y1, bool wasHit)
    {
        int w = x1 - x0, h = y1 - y0;
        int need = w * h;
        _verifyScratch = EnsureCapacity(_verifyScratch, need);
        for (int y = 0; y < h; y++)
            Array.Copy(_light, (y0 + y) * Width + x0, _verifyScratch, y * w, w);

        // The reference is swept over the region the cache claims to cover, not over the smaller
        // region being served: sweeping the served region would give its edge cells less upwind
        // history than the cache has, so the comparison would measure the pad's extra accuracy
        // rather than the cache's correctness. The question is whether the cache holds what a
        // fresh sweep of its own region would produce.
        var op = map.OpacityTable;
        EnsureCasterHeights(map, op);
        FillShadedRegion(op, sky, map.HasSky, _cacheX0, _cacheY0,
                         _cacheX0 + _cacheW, _cacheY0 + _cacheH);

        int bad = 0;
        float worst = 0f;
        for (int y = 0; y < h; y++)
        {
            int row = (y0 + y) * Width + x0;
            for (int x = 0; x < w; x++)
            {
                var served = _verifyScratch[y * w + x];
                var fresh = _light[row + x];
                float d = Math.Abs(served.R - fresh.R)
                        + Math.Abs(served.G - fresh.G)
                        + Math.Abs(served.B - fresh.B);
                if (d > 0.01f) { bad++; if (d > worst) worst = d; }
            }
        }

        // And the cache itself, against the same fresh sweep. It is built by mirroring each row
        // as a fill writes it, and a row a fill forgot to mirror is invisible to the compare
        // above until a source box happens to be re-based from it — on a hit, and only there.
        // The cache must equal a fresh sweep of its own region whether this turn hit or not.
        int badCache = 0;
        int cacheCells = _cacheW * _cacheH;
        for (int y = 0; y < _cacheH; y++)
        {
            int row = (_cacheY0 + y) * Width + _cacheX0;
            int crow = y * _cacheW;
            for (int x = 0; x < _cacheW; x++)
            {
                var held = _cache![crow + x];
                var fresh = _light[row + x];
                float d = Math.Abs(held.R - fresh.R)
                        + Math.Abs(held.G - fresh.G)
                        + Math.Abs(held.B - fresh.B);
                if (d > 0.01f) badCache++;
            }
        }

        if (bad > 0 || badCache > 0) VerifyBadTurns++;
        if (bad > 0)
            DebugLogger.LogGame("VERIFY-LIGHT",
                $"{bad} of {need} cells stale after a {(wasHit ? "cache hit" : "refresh")}, "
                + $"worst channel sum {worst:F1} (sun {DayNightCycle.SunState}, "
                + $"weather {WeatherSystem.GetLabel()})");
        if (badCache > 0)
            DebugLogger.LogGame("VERIFY-LIGHT",
                $"the cache holds {badCache} of {cacheCells} cells a fresh sweep of its own region "
                + $"disagrees with, after a {(wasHit ? "cache hit" : "refresh")} — a fill wrote the "
                + "grid without mirroring the row");
    }
}
