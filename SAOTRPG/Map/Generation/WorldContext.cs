
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation;

// Mutable per-floor generation state shared across pipeline passes. Populated
// by MapGenerator.BuildContext; progressively filled by each pass.
public sealed class WorldContext
{
    public int FloorNumber { get; }
    public int GlobalSeed { get; }
    public int Width { get; }
    public int Height { get; }
    public BiomeType Biome { get; }
    public BiomeGenConfig Config { get; }
    public GameMap Map { get; }
    public List<Room> Rooms { get; } = new();
    public List<(int x, int y)> Clearings { get; } = new();

    // Populated by SeedDerivationPass. Use this everywhere inside the pipeline — never the ambient
    // run stream, because a pass must reproduce from the floor's derived seed alone.
    public Random Rng { get; set; } = null!;

    // Per-game prefab MAX_PER_GAME ledger, scoped to this generation. It starts as a copy of the
    // game's ledger and is committed back only when the finished floor is actually adopted, so a
    // floor built ahead of time and then discarded cannot leave the caps advanced.
    //
    // It also has to be per-context rather than static because generation may run off the UI
    // thread: concurrent increments on a shared Dictionary lose counts and can corrupt the
    // collection, so a MAX_PER_GAME:1 landmark could place twice and a floor could stop
    // reproducing, both in silence.
    public Dictionary<string, int> PrefabUseCounts { get; set; } = new();

    // Returns true if placement is allowed, and charges the cap. maxPerGame <= 0 = unlimited.
    public bool TryUsePrefab(string name, int maxPerGame)
    {
        if (maxPerGame <= 0) return true;
        int cur = PrefabUseCounts.TryGetValue(name, out int v) ? v : 0;
        if (cur >= maxPerGame) return false;
        PrefabUseCounts[name] = cur + 1;
        return true;
    }

    // Mirror of TryUsePrefab: the charge above is optimistic, so a placement that ultimately
    // fails gives it back rather than burning a cap on a prefab that never appeared.
    public void RefundPrefab(string name)
    {
        if (!PrefabUseCounts.TryGetValue(name, out int v) || v <= 0) return;
        if (v == 1) PrefabUseCounts.Remove(name);
        else PrefabUseCounts[name] = v - 1;
    }

    // Set by HeightmapPass. float[w,h] in [0..1]. Null until the pass runs.
    public float[,]? Heights { get; set; }

    // Set by SpecialAreaPass.
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }

    // Set by FeatureScatterPass mid-run when the boss room is carved.
    public Room? BossRoom { get; set; }


    // Set by PocketBiomePass on floors that get pockets. Per-tile override biome;
    // null entries (and null map) fall back to ctx.Biome in BaseTerrainPass.
    public BiomeType?[,]? PocketBiomeMap { get; set; }

    // Circular playable disk; null on F100 (no circular bound).
    // Built once per floor by GenerationPipeline.BuildCircleMask before passes run.
    public bool[,]? CircleMask { get; set; }

    public bool IsInsideCircle(int x, int y) =>
        CircleMask is null
        || ((uint)x < (uint)CircleMask.GetLength(0)
            && (uint)y < (uint)CircleMask.GetLength(1)
            && CircleMask[x, y]);

    // Town keep-out rect (footprint + gate-approach margin). Set by
    // GenerationPipeline.ComputeTownKeepOut before passes run. Cluster/Lake/River
    // skip stamping inside so trees don't block gates and rivers don't clip
    // through town. Default -1 = no keep-out (helper returns false).
    public int TownKeepOutX { get; set; } = -1;
    public int TownKeepOutY { get; set; }
    public int TownKeepOutW { get; set; }
    public int TownKeepOutH { get; set; }

    public bool IsInTownKeepOut(int x, int y) =>
        TownKeepOutX >= 0
        && x >= TownKeepOutX && x < TownKeepOutX + TownKeepOutW
        && y >= TownKeepOutY && y < TownKeepOutY + TownKeepOutH;

    public WorldContext(int floorNumber, int globalSeed, int width, int height,
        BiomeType biome, BiomeGenConfig config, GameMap map)
    {
        FloorNumber = floorNumber;
        GlobalSeed = globalSeed;
        Width = width;
        Height = height;
        Biome = biome;
        // A floor may declare a sub-variant of its biome, and the variant file replaces the base
        // config for that floor. The caller supplies the biome's own config, which is still what
        // every floor without a declared variant generates from — and what a declared variant
        // falls back to, loudly, when its file cannot be read.
        Config = BiomeGenConfigLoader.VariantFor(biome, floorNumber) ?? config;
        Map = map;
    }
}
