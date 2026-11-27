using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.Map.Lighting;

// The next refresh, built before it is needed. A refresh re-shades the whole padded region, and
// done on the turn that needs it (the sun stepping, or the player walking out of the cached
// region) it is long enough to drop a frame. A worker builds the next one into its own buffers
// (_ws) from the same inputs, and the turn that needs it adopts the result, but only when every
// input is exactly the one the synchronous refresh would use then. Adopting therefore cannot
// change a cell; anything else (a door opened, the weather turned, the player turned round)
// refreshes in place and the worker's result is dropped.
//
// The worker reads the map's opacity table and this system's caster heights, both brought up to
// date here before it starts. The main thread can patch either while it runs, which moves
// SightVersion, and a job built at another version is never adopted.
public sealed partial class LightingSystem
{
    // Toggled by the verification probes; the light grid must come out the same either way.
    public static bool SpeculationEnabled { get; set; } = true;

    // How far ahead the sky's next step is looked for (the night holds one rung for hours), and
    // how many steps short of the cache's edge a walk's exit is built.
    private const int SkyHorizon = 4 * 3600;
    private const int ExitLookahead = 12;

    public static int SpeculationsAdopted { get; private set; }
    public static int SpeculationsDropped { get; private set; }

    public static (int Adopted, int Dropped) DrainSpeculationCounts()
    {
        var r = (SpeculationsAdopted, SpeculationsDropped);
        SpeculationsAdopted = 0;
        SpeculationsDropped = 0;
        return r;
    }

    private sealed class Job
    {
        public GameMap Map = null!;
        public int Version, X0, Y0, W, H;
        public bool HasSky;
        public SkyShade Sky;
        public Task Task = Task.CompletedTask;
        public Exception? Error;
        // The worker's output, handed over on adoption: the field, and the sweep's terms for
        // TryGetShadingTerms.
        public LightRgb[]? Cache;
        public float[]? Shadow, Bounce;
        public int ShadowX0, ShadowY0, ShadowW, ShadowH;
        public bool Swept;

        public bool Is(GameMap map, in SkyShade sky, int x0, int y0, int w, int h) =>
            ReferenceEquals(Map, map) && Version == map.SightVersion && HasSky == map.HasSky
            && Sky == sky && X0 == x0 && Y0 == y0 && W == w && H == h;
    }

    private LightingSystem? _ws;
    private Job? _job;
    private int _skyFrom = int.MinValue, _skyNext = -1;
    private int _lastPx = int.MinValue, _lastPy;

    // The refresh this turn needs, if the worker has built (or is building) exactly it.
    private bool TryAdopt(GameMap map, in SkyShade sky, int x0, int y0, int w, int h)
    {
        if (_job is not { } job || !job.Is(map, sky, x0, y0, w, h)) return false;
        using var _ = Profiler.Begin("Lighting.Adopt");
        // Begun turns ago, so what is left of it costs less than sweeping from the start.
        job.Task.Wait();
        _job = null;
        if (job.Error is { } e)
        {
            DebugLogger.LogGame("LIGHT", $"the speculative refresh failed and was redone in place: {e.GetType().Name}: {e.Message}");
            return false;
        }
        var ws = _ws!;
        for (int y = 0; y < h; y++)
            Array.Copy(job.Cache!, y * w, _light, (y0 + y) * Width + x0, w);
        // Take the worker's output, and give it this side's buffers to build the next one in.
        (_cache, ws._cache) = (job.Cache, _cache);
        (_shadow, ws._shadow) = (job.Shadow, _shadow);
        (_bounce, ws._bounce) = (job.Bounce, _bounce);
        _shadowRx0 = job.ShadowX0; _shadowRy0 = job.ShadowY0;
        _shadowRw = job.ShadowW; _shadowRh = job.ShadowH;
        _shadowSwept = job.Swept;
        SetCacheKey(map, sky, x0, y0, w, h);
        SpeculationsAdopted++;
        return true;
    }

    // After a turn is served: build whichever refresh comes next, the player walking out of the
    // cache on their present heading or the sky's next step, unless it is already built.
    private void Speculate(GameMap map, in SkyShade sky, int px, int py)
    {
        int dx = Math.Sign(px - _lastPx), dy = Math.Sign(py - _lastPy);
        // A step or a sprint has a heading; a teleport or the first turn does not.
        bool heading = _lastPx != int.MinValue && Math.Abs(px - _lastPx) <= 2 && Math.Abs(py - _lastPy) <= 2
                       && (dx != 0 || dy != 0);
        _lastPx = px; _lastPy = py;
        if (!SpeculationEnabled || !_cacheValid) return;
        if (_job is { Task.IsCompleted: false }) return;   // one at a time

        int qx = px, qy = py;
        int exitIn = heading ? StepsToExit(px, py, dx, dy, out qx, out qy) : int.MaxValue;
        int skyIn = TurnsToSkyStep();
        SkyShade want;
        int x0, y0, w, h;
        if (exitIn <= ExitLookahead && exitIn <= skyIn)
        {
            var (rx0, ry0, rx1, ry1) = Roi(qx, qy);
            (x0, y0, w, h) = PaddedRegion(rx0, ry0, rx1, ry1);
            want = sky;
        }
        else if (skyIn != int.MaxValue)
        {
            (x0, y0, w, h) = (_cacheX0, _cacheY0, _cacheW, _cacheH);
            want = DayNightCycle.ShadeAt(_skyNext);
        }
        else return;

        if (_job != null && _job.Is(map, want, x0, y0, w, h)) return;
        if (_job != null) SpeculationsDropped++;
        Launch(map, want, x0, y0, w, h);
    }

    // Steps along (dx, dy) until the lit region, widened by a source's reach, leaves the cache:
    // where the synchronous refresh would fire. int.MaxValue when that is past the lookahead.
    private int StepsToExit(int px, int py, int dx, int dy, out int qx, out int qy)
    {
        int reach = MaxSourceReachX + 1;
        for (int k = 1; k <= ExitLookahead; k++)
        {
            qx = Math.Clamp(px + k * dx, 0, Width - 1);
            qy = Math.Clamp(py + k * dy, 0, Height - 1);
            var (x0, y0, x1, y1) = Roi(qx, qy);
            if (!CacheCovers(Math.Max(0, x0 - reach), y0, Math.Min(Width, x1 + reach), y1)) return k;
        }
        qx = px; qy = py;
        return int.MaxValue;
    }

    // Turns until the sky next differs from now; int.MaxValue past the horizon. The scan is a few
    // thousand turns at most, so it runs once per sky step, not once per turn.
    private int TurnsToSkyStep()
    {
        int now = DayNightCycle.CurrentTurn;
        bool stale = now < _skyFrom || (_skyNext >= 0 ? now >= _skyNext : now > _skyFrom + SkyHorizon);
        if (stale)
        {
            _skyFrom = now;
            _skyNext = DayNightCycle.NextSkyChange(now, SkyHorizon);
        }
        return _skyNext < 0 ? int.MaxValue : _skyNext - now;
    }

    private void Launch(GameMap map, in SkyShade sky, int x0, int y0, int w, int h)
    {
        bool[] op = map.OpacityArray;
        EnsureCasterHeights(map, op);
        _ws ??= new LightingSystem(Width, Height);
        var job = new Job
        {
            Map = map, Version = map.SightVersion, HasSky = map.HasSky, Sky = sky,
            X0 = x0, Y0 = y0, W = w, H = h,
        };
        LightingSystem ws = _ws;
        byte[] casters = _casterHeight!;
        job.Task = Task.Run(() => ws.Build(job, op, casters));
        _job = job;
    }

    // On the worker, in the worker's own buffers: exactly what RefreshCache does on the main
    // thread, reading the caster heights it was handed rather than its own.
    private void Build(Job job, bool[] op, byte[] casters)
    {
        Profiler.OffThisThread = true;
        try
        {
            _casterHeight = casters;
            _cache = EnsureCapacity(_cache, job.W * job.H);
            _mirror = _cache; _mirrorX0 = job.X0; _mirrorY0 = job.Y0; _mirrorW = job.W;
            FillShadedRegion(op, job.Sky, job.HasSky, job.X0, job.Y0, job.X0 + job.W, job.Y0 + job.H);
            job.Cache = _cache;
            job.Shadow = _shadow;
            job.Bounce = _bounce;
            job.ShadowX0 = _shadowRx0; job.ShadowY0 = _shadowRy0;
            job.ShadowW = _shadowRw; job.ShadowH = _shadowRh;
            job.Swept = _shadowSwept;
        }
        catch (Exception e)
        {
            job.Error = e;
        }
        finally
        {
            _mirror = null;
            Profiler.OffThisThread = false;
        }
    }
}
