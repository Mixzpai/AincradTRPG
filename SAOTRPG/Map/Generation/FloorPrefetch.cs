using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.Map.Generation;

// Builds the next floor while the player is still on the current one, so the stairs do not stall.
//
// This is sound because a floor is a pure function of (seed, floor) plus the per-game prefab cap
// ledger, and nothing between arriving on a floor and leaving it moves the ledger: the labyrinth
// draws from its own derived stream and charges no caps. So the floor built on arrival is the
// floor the transition would have built.
//
// Two generations must never overlap. They share the biome-config cache and the prefab library,
// and concurrent use corrupts shared state silently (a landmark placing twice, a floor that stops
// reproducing). Take and Invalidate therefore join the running build before releasing the slot,
// and Request refuses while the slot is set.
//
// All three entry points are called from the UI thread, which would serialise them anyway. The
// locking does not rely on that: nothing enforces it.
//
// The fallback is the synchronous path, so a prefetch that is missing, stale, faulted or still
// running costs nothing but the time it would have cost anyway.
public static class FloorPrefetch
{
    private static readonly object Gate = new();

    private static Task<Result>? _task;
    private static int _taskFloor;
    private static int _taskSeed;
    private static long _taskEpoch;

    // Counts builds that are queued or executing, not slots that are occupied, so it observes the
    // worker itself rather than the slot bookkeeping around it.
    private static int _buildsRunning;

    // Bumped by anything that invalidates a floor built earlier: a new run, a load, an F9 biome
    // reload. A result whose epoch has moved is discarded rather than adopted.
    private static long _epoch;

    public static int Hits { get; private set; }
    public static int Misses { get; private set; }

    // The cap ledger, not the whole WorldContext. A finished floor is held for as long as the
    // player stays on the previous one, and the context retains per-pass scratch the map does not
    // need (PocketBiomeMap alone is 8 bytes a cell).
    private readonly record struct Result(GameMap Map, List<Room> Rooms, Dictionary<string, int> Counts);

    // Discards any in-flight or completed prefetch. Called on new run, load and biome hot-reload.
    public static void Invalidate()
    {
        Task<Result>? running;
        lock (Gate)
        {
            _epoch++;
            running = _task;
        }
        // Join with the slot still occupied. The work cannot be cancelled mid-pass, and letting it
        // run on while a fresh generation starts is the overlap this class prevents.
        WaitQuietly(running);
        lock (Gate) if (ReferenceEquals(_task, running)) _task = null;
    }

    // Start building `floor` in the background. No-op if a build is already in flight or ready, or
    // if the floor is one the pipeline does not own. Never throws into the caller.
    public static void Request(int floor)
    {
        if (floor < 1 || floor > 100) return;

        // F100 is the hand-authored Ruby Palace and does not run the pipeline, so there is no
        // detached form of it to build: a prefetch would return a pipeline floor instead of the
        // throne room. It is a 50x35 hand-stamped map, cheap to build on the spot. Every floor
        // that bypasses the pipeline must be excluded here.
        if (FloorScale.IsCastleFloor(floor)) return;

        // Warm every lazily-built cache the pipeline reads before any work is queued. Both the
        // biome table and the prefab library are read-only once populated, but their first
        // population is not synchronized and is documented single-threaded.
        Generation.Prefabs.PrefabLibrary.Shared.LoadAll();
        BiomeGenConfigLoader.LoadAll();

        var (w, h) = FloorScale.GetDimensions(floor);
        lock (Gate)
        {
            if (_task != null) return;
            int seed = MapGenerator.CurrentGlobalSeed;
            _taskFloor = floor;
            _taskSeed = seed;
            _taskEpoch = _epoch;
            // Counted before the task is queued, so there is no window in which a build is
            // pending and InFlight reads false.
            Interlocked.Increment(ref _buildsRunning);
            // Queued inside the lock so the slot can never be observed empty while a build is
            // starting. Task.Run only enqueues; the body takes no lock of ours.
            _task = Task.Run(() =>
            {
                try
                {
                    // The seed captured above, not the live static: a new run must not be able
                    // to change what this build produces after the slot recorded what it is.
                    var (map, rooms, ctx) = MapGenerator.GenerateFloorDetached(floor, w, h, seed);
                    return new Result(map, rooms, ctx.PrefabUseCounts);
                }
                finally { Interlocked.Decrement(ref _buildsRunning); }
            });
        }
    }

    // Hand back `floor` if it was prefetched, waiting for an in-flight build of it to finish.
    // Returns null when the caller must generate for itself — and even then, joins first, so the
    // synchronous path never runs alongside a prefetch.
    public static (GameMap Map, List<Room> Rooms)? Take(int floor)
    {
        Task<Result>? task;
        bool usable;
        lock (Gate)
        {
            task = _task;
            usable = task != null
                     && _taskFloor == floor
                     && _taskSeed == MapGenerator.CurrentGlobalSeed
                     && _taskEpoch == _epoch;
        }
        if (task == null) { Misses++; return null; }

        // Join before releasing the slot, whether or not the result is wanted.
        Result result = default;
        bool faulted = false;
        try { result = task.GetAwaiter().GetResult(); }
        catch (Exception ex)
        {
            // A prefetch that faulted is not a broken game: fall through to the synchronous path.
            faulted = true;
            DebugLogger.LogError($"FloorPrefetch({floor})", ex);
        }
        lock (Gate) if (ReferenceEquals(_task, task)) _task = null;

        if (!usable || faulted) { Misses++; return null; }

        // The caps are charged only now, when the floor is really adopted.
        MapGenerator.AdoptDetached(result.Counts);
        Hits++;
        DebugLogger.LogGame("MAPGEN", $"prefetch HIT floor {floor} (hits={Hits} misses={Misses})");
        return (result.Map, result.Rooms);
    }

    // True while a build is queued or executing. Read by the verification probes, not by gameplay.
    public static bool InFlight => Volatile.Read(ref _buildsRunning) > 0;

    public static void ResetCountersForTest() { Hits = 0; Misses = 0; }

    private static void WaitQuietly(Task? t)
    {
        if (t == null) return;
        try { t.GetAwaiter().GetResult(); }
        catch (Exception ex) { DebugLogger.LogError("FloorPrefetch.discard", ex); }
    }
}
