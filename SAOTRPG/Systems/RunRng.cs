namespace SAOTRPG.Systems;

// The run's gameplay RNG. Every roll that changes game state draws from here, so a run is
// reproducible from its seed: the same seed gives the same floors, loot, mobs and fights.
//
// COSMETIC RNG MUST NOT DRAW FROM THIS STREAM. Flavour text, death-screen tips, ambient terrain
// lines and particle jitter stay on Random.Shared. If they shared this stream, toggling Reduce
// Motion or resizing the terminal would change how many draws had happened and shift every
// subsequent gameplay roll — the run would stop being reproducible for reasons the player cannot
// see. The boundary is the whole point of this type; keep cosmetic callers off it.
//
// Reproducibility is defined from the START of a run, not across a save. Loading re-seeds the
// stream from the run seed rather than restoring a draw position: the checkpoint model already
// regenerates the floor on load, and persisting a position would mean every draw had to go through
// one counted path or the position would silently drift.
public static class RunRng
{
    private static Random _rng = new(Environment.TickCount);

    // The seed this run was started from. Surfaced so a player can note or share it.
    public static int Seed { get; private set; } = Environment.TickCount;

    // Starts a fresh stream. Called at new-run init and after a load.
    public static void Reseed(int seed)
    {
        Seed = seed;
        _rng = new Random(seed);
        UI.DebugLogger.LogGame("RNG", $"run stream seeded with {seed}");
    }

    // A seed for a new run. Time-based, but drawn per call rather than latched at type init —
    // latching is what made every new run in one process share a world.
    public static int NewSeed() => Environment.TickCount ^ (Guid.NewGuid().GetHashCode() & 0xFFFF);

    public static int Next(int maxExclusive) => _rng.Next(maxExclusive);

    public static int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

    public static double NextDouble() => _rng.NextDouble();

    public static int Next() => _rng.Next();

    // The run stream as a Random, for APIs that take one explicitly. Read it at the call site and
    // never cache it: Reseed replaces the instance, so a stored reference goes on drawing from the
    // previous run's stream.
    public static Random Stream => _rng;

    // Derives an independent stream for work that must reproduce on its own terms — labyrinth
    // layout, for one, which is regenerated on entry rather than drawn inline with the turn loop.
    public static Random StreamFor(string purpose, int salt) =>
        new(StableHash(Seed, purpose, salt));

    // FNV-1a. Deterministic across processes and machines, which System.HashCode and
    // string.GetHashCode() are NOT: both randomise their seed per process to make hash-flooding
    // impractical. Measured, HashCode.Combine(4242, 3) gave three different answers on three
    // consecutive runs — so anything seeded through it produced a new world every launch.
    // EVERY seed derivation in the game goes through here. Never reintroduce HashCode.Combine or
    // string.GetHashCode() on a path that has to reproduce.
    private const uint FnvOffset = 2166136261;
    private const uint FnvPrime = 16777619;

    public static int StableHash(int a, int b) => (int)Mix(Mix(FnvOffset, a), b);

    public static int StableHash(int a, string text, int b) =>
        (int)Mix(Mix(Mix(FnvOffset, a), text), b);

    public static int StableHash(int a, int b, string text) =>
        (int)Mix(Mix(Mix(FnvOffset, a), b), text);

    public static int StableHash(string text) => (int)Mix(FnvOffset, text);

    private static uint Mix(uint h, int value)
    {
        uint v = (uint)value;
        for (int i = 0; i < 4; i++)
        {
            h ^= (v >> (i * 8)) & 0xFF;
            h *= FnvPrime;
        }
        return h;
    }

    private static uint Mix(uint h, string text)
    {
        foreach (char c in text)
        {
            h ^= (uint)(c & 0xFF);
            h *= FnvPrime;
            h ^= (uint)((c >> 8) & 0xFF);
            h *= FnvPrime;
        }
        return h;
    }
}
