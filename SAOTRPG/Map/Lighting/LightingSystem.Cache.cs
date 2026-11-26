
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Lighting;

// The ambient + shadow + contact field, cached over a PADDED region of the world.
//
// WHY IT IS CACHEABLE AT ALL: that field is a pure function of the map's opacity, the sun's
// quantized state, the ambient colour, the weather's sunlight scale and whether the floor has
// sky. NONE of those move when the player attacks, rests, mines, waits or walks into a wall —
// and the field is in WORLD space, so they do not move when the player walks either. Only the
// REGION being lit travels. That is the same observation that made the visible-set reuse in
// GameMap.UpdateVisibility worth about 2 ms a turn.
//
// WHY THE REGION IS PADDED RATHER THAN EXACT: an exact region would miss on every step the
// player takes, which is most turns, and the refresh is the expensive half. Padding the cached
// region means a step usually lands inside what is already computed — the same trick the map's
// damage-tracked tile layer uses when the camera scrolls. The pad is paid for once per refresh
// and amortised over every turn until the player walks out of it.
//
// WHAT IS NOT CACHED: point lights. The torch's colour and radius follow the sun and the sources
// present move with the player, so they are re-added on top of the restored base every turn. The
// cache holds strictly what FillShadedRegion produces, before AddSource touches anything.
//
// HOW A HIT RESTORES THE BASE: not by copying the served region — a 1 MB copy every turn, most
// of Lighting.Update on a hit — but by re-basing only the boxes the sources wrote last turn.
// Between refreshes the grid over the cached region equals the cache everywhere EXCEPT inside
// those boxes, because nothing else writes it: the base is a pure function of the world, and
// AddSource notes every box it merges. A refresh re-sweeps the whole cached region and empties
// the list. Anything else that writes the grid (the flat fill) must drop the cache, or the next
// hit would re-base too little.
//
// AND THE FAILURE MODE IS SILENT, which is why --verify-light exists and was written first: a
// missed dependency leaves the map lit by an earlier sun or an earlier floor, and stale light
// looks like light.
public sealed partial class LightingSystem
{
    // How far past the lit region the cache reaches. Every cell of it is swept on a refresh, so
    // this trades a bigger refresh for rarer ones.
    private const int CachePad = 72;

    // NOTE ON HISTORY, because the obvious worry here turns out to be unfounded. A cell near the
    // swept region's upwind edge carries only SweepMargin steps of history — but a cell is only
    // SERVED when the whole needed region is inside the cached one, and the needed region reaches
    // ~91 rows and ~186 columns from the player while the screen reaches 45 and 158. So a served
    // cell with short history is always off screen, exactly as it was before this cache existed.
    // An inset was tried and removed: it bought nothing and cost refreshes.

    private LightRgb[]? _cache;
    private int _cacheX0, _cacheY0, _cacheW, _cacheH;
    private bool _cacheValid;

    // Where the sources wrote since the base was last re-established: the only cells of the
    // cached region that differ from the cache on a hit. Every box lies inside the cached region
    // by construction — a source is admitted within the served region, its box reaches at most
    // MaxSourceRadius + 1 columns past it, and a hit requires that margin cached — and the region
    // only moves on a refresh, which empties the list before anything could be restored against
    // the new one.
    private readonly List<(int X0, int Y0, int X1, int Y1)> _sourceBoxes = new();

    // Everything the cached field depends on. If any of these differs the field must be rebuilt;
    // if one is MISSING from here the field goes stale in silence, which is the whole risk.
    //
    // HasSky is kept even though sabotage cannot make it fire — it is a public settable property
    // and nothing stops a future caller flipping it, so the key is defence rather than dead
    // weight. That is a different case from the Terrain Shading setting below, which is
    // unreachable BY CONSTRUCTION and was therefore deleted rather than kept.
    //
    // The Terrain Shading setting is deliberately NOT among them: FillFromCache is only reached
    // when it is on, so a key for it would be true at every write and every read. Turning the
    // setting off takes the flat path, which overwrites the served region and therefore DROPS the
    // cache — a hit after it would re-base only the source boxes and leave the flat fill under
    // everything else — so turning it back on refreshes.
    //
    // WHOSE FLOOR THIS IS, and a version stamp cannot stand in for it: two GameMaps both open at
    // SightVersion 0, so a field cached on one would serve as a HIT on the other and light the map
    // from an earlier floor. EnsureCasterHeights already keeps exactly this guard, and a cache hit
    // is precisely the path that skips the sweep and therefore skips it — so the guard has to
    // exist on both sides or the cheaper one defeats the other. Defence against a future caller,
    // the same standing this key gives HasSky: today a LightingSystem is constructed by one
    // GameMap and only ever handed that one.
    private GameMap? _cacheMap;
    private int _cacheSunState;
    private float _cacheAmbientR, _cacheAmbientG, _cacheAmbientB;
    private int _cacheSightVersion;
    private bool _cacheHasSky;
    private float _cacheSunlightScale;

    private LightRgb[]? _verifyScratch;

    // ── The snapshot a refresh is building ─────────────────────────────────────────────────
    // While the padded region is being re-swept, every row a fill writes into the light grid is
    // copied into the cache the moment it is written, while it is still in cache. Copying the
    // whole region afterwards re-read 2.3 MB the fill itself had just evicted, and cost as much
    // as the apply pass that produced it. Null outside a refresh: the verification sweep runs
    // the same fills and must not touch the cache.
    private LightRgb[]? _mirror;
    private int _mirrorX0, _mirrorY0, _mirrorW;

    private void MirrorRow(int y, int x0, int x1)
    {
        if (_mirror is not { } snap) return;
        Array.Copy(_light, y * Width + x0, snap, (y - _mirrorY0) * _mirrorW + (x0 - _mirrorX0), x1 - x0);
    }

    private bool CacheKeyMatches(GameMap map, LightRgb ambient) =>
        _cacheValid
        && ReferenceEquals(_cacheMap, map)
        && _cacheSunState == DayNightCycle.SunState
        && _cacheAmbientR == ambient.R
        && _cacheAmbientG == ambient.G
        && _cacheAmbientB == ambient.B
        && _cacheSightVersion == map.SightVersion
        && _cacheHasSky == map.HasSky
        && _cacheSunlightScale == WeatherSystem.SunlightScale;

    private bool CacheCovers(int x0, int y0, int x1, int y1) =>
        x0 >= _cacheX0 && y0 >= _cacheY0
        && x1 <= _cacheX0 + _cacheW && y1 <= _cacheY0 + _cacheH;

    // Re-establish the base over the served region: on a hit by re-basing the boxes the sources
    // wrote, otherwise by re-sweeping the padded region. The served region is only the TRIGGER —
    // the base is held over the whole cached region either way.
    private void FillFromCache(GameMap map, LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        // Widened in X: a source admitted at the ROI edge writes a box up to MaxSourceRadius + 1
        // columns past it, and next turn's hit must be able to re-base that box.
        int reach = MaxSourceRadius + 1;
        bool hit = CacheKeyMatches(map, ambient)
                   && CacheCovers(Math.Max(0, x0 - reach), y0, Math.Min(Width, x1 + reach), y1);
        if (hit) RestoreSourceBoxes();
        else RefreshCache(map, ambient, x0, y0, x1, y1);
        _sourceBoxes.Clear();

        CacheHits += hit ? 1 : 0;
        CacheMisses += hit ? 0 : 1;

        if (DebugMode.VerifyLight) VerifyAgainstFreshFill(map, ambient, x0, y0, x1, y1, hit);
    }

    // Called by AddSource with the box it merged, clipped to the map.
    private void NoteSourceBox(int x0, int y0, int x1, int y1) => _sourceBoxes.Add((x0, y0, x1, y1));

    // The grid no longer holds the base the cache describes — something other than a source
    // wrote it — so the next shaded turn must refresh rather than re-base the boxes.
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

    private void RefreshCache(GameMap map, LightRgb ambient, int x0, int y0, int x1, int y1)
    {
        int pad = CachePad;
        int px0 = Math.Max(0, x0 - pad);
        int py0 = Math.Max(0, y0 - pad);
        int px1 = Math.Min(Width, x1 + pad);
        int py1 = Math.Min(Height, y1 + pad);
        int w = px1 - px0, h = py1 - py0;

        // Swept straight into the light grid, exactly as it was before this cache existed, and
        // mirrored into the cache row by row as the fills write it — so the sweep needs no
        // knowledge of the cache and there is one code path producing the field rather than two
        // that can drift. The grid IS the base when this returns; nothing is copied back.
        _cache = EnsureCapacity(_cache, w * h);
        _mirror = _cache; _mirrorX0 = px0; _mirrorY0 = py0; _mirrorW = w;
        try { FillShadedRegion(map, ambient, px0, py0, px1, py1); }
        finally { _mirror = null; }

        _cacheX0 = px0; _cacheY0 = py0; _cacheW = w; _cacheH = h;
        _cacheMap = map;
        _cacheSunState = DayNightCycle.SunState;
        _cacheAmbientR = ambient.R; _cacheAmbientG = ambient.G; _cacheAmbientB = ambient.B;
        _cacheSightVersion = map.SightVersion;
        _cacheHasSky = map.HasSky;
        _cacheSunlightScale = WeatherSystem.SunlightScale;
        _cacheValid = true;
    }

    // How often the sweep was skipped. A measurement seam: the hit rate is the whole value of
    // this layer, and it is not visible from anywhere else.
    public static int CacheHits { get; private set; }
    public static int CacheMisses { get; private set; }

    public static (int Hits, int Misses) DrainCacheCounts()
    {
        var r = (CacheHits, CacheMisses);
        CacheHits = 0;
        CacheMisses = 0;
        return r;
    }

    // Mismatches found by --verify-light. Authoritative: a probe that greps the log instead can
    // be silenced by a change to the log format, which has bitten this project once already.
    public static int VerifyBadTurns { get; private set; }

    // Recompute the field from scratch and compare it against what was just served. The FRESH
    // result is what stays in the grid, so a wrong cache cannot corrupt play while verifying.
    // The served grid is the product of the restore, so a box it failed to re-base shows here
    // as a mismatch wherever that box meets the served region — no separate check is needed.
    private void VerifyAgainstFreshFill(GameMap map, LightRgb ambient,
                                        int x0, int y0, int x1, int y1, bool wasHit)
    {
        int w = x1 - x0, h = y1 - y0;
        int need = w * h;
        _verifyScratch = EnsureCapacity(_verifyScratch, need);
        for (int y = 0; y < h; y++)
            Array.Copy(_light, (y0 + y) * Width + x0, _verifyScratch, y * w, w);

        // THE REFERENCE MUST BE SWEPT OVER THE REGION THE CACHE CLAIMS TO COVER, not over the
        // smaller region being served. Sweeping the served region gives its edge cells LESS
        // upwind history than the cache has, so the comparison measures the pad's extra accuracy
        // rather than the cache's correctness — the first version did exactly that and reported
        // 108 of 400 turns "stale" with a worst channel sum of 4.3, which is under the renderer's
        // own colour quantization and was the padding working, not a bug. The question this has
        // to ask is: does the cache hold what a fresh sweep of ITS OWN region would produce?
        FillShadedRegion(map, ambient, _cacheX0, _cacheY0,
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

        // AND THE CACHE ITSELF, against the same fresh sweep. It is built by mirroring each row
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
