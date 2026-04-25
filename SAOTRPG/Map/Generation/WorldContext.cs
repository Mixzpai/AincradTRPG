using SAOTRPG.Systems;

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

    // Populated by SeedDerivationPass. Use this everywhere — not Random.Shared.
    public Random Rng { get; set; } = null!;

    // Set by HeightmapPass. float[w,h] in [0..1]. Null until the pass runs.
    public float[,]? Heights { get; set; }

    // Set by SpecialAreaPass.
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }

    // Set by FeatureScatterPass mid-run when the boss room is carved.
    public Room? BossRoom { get; set; }

    // Set by LakePass before lake stamping. Consumed by ConnectivityAuditPass for
    // Brogue's 85% post-lake-reachability rule. 0 until LakePass runs.
    public int PreLakeWalkableCount { get; set; }

    // Set by PocketBiomePass on band-edge floors. Per-tile override biome;
    // null entries (and null map) fall back to ctx.Biome in BaseTerrainPass.
    public BiomeType?[,]? PocketBiomeMap { get; set; }

    // Circular playable disk; null on towns + F100 (no circular bound).
    // Built once per floor by GenerationPipeline.BuildCircleMask before passes run.
    public bool[,]? CircleMask { get; set; }

    public bool IsInsideCircle(int x, int y) =>
        CircleMask is null
        || ((uint)x < (uint)CircleMask.GetLength(0)
            && (uint)y < (uint)CircleMask.GetLength(1)
            && CircleMask[x, y]);

    public WorldContext(int floorNumber, int globalSeed, int width, int height,
        BiomeType biome, BiomeGenConfig config, GameMap map)
    {
        FloorNumber = floorNumber;
        GlobalSeed = globalSeed;
        Width = width;
        Height = height;
        Biome = biome;
        Config = config;
        Map = map;
    }
}
