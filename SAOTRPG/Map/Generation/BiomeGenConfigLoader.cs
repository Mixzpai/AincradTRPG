using System.Text.Json;
using System.Text.Json.Serialization;
using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation;

// Loads Content/Biomes/*.json once and caches; F9 calls Invalidate() to re-read.
// Single-threaded by design — callers must not invoke LoadAll/Invalidate concurrently.
public static class BiomeGenConfigLoader
{
    private static IReadOnlyDictionary<BiomeType, BiomeGenConfig>? _cached;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    // Returns cached dict; populates on first call. Missing/malformed files
    // fall back to BiomeGenConfig.CreateDefault per biome with an error log.
    public static IReadOnlyDictionary<BiomeType, BiomeGenConfig> LoadAll()
    {
        if (_cached != null) return _cached;

        var map = new Dictionary<BiomeType, BiomeGenConfig>();
        string baseDir = Path.Combine(AppContext.BaseDirectory, "Content", "Biomes");

        foreach (BiomeType biome in Enum.GetValues<BiomeType>())
        {
            BiomeGenConfig? cfg = null;
            string path = Path.Combine(baseDir, biome.ToString().ToLowerInvariant() + ".json");
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    cfg = JsonSerializer.Deserialize<BiomeGenConfig>(json, JsonOpts);
                }
            }
            catch (Exception ex)
            {
                UI.DebugLogger.LogError($"BiomeGenConfigLoader.Load({biome})", ex);
            }
            map[biome] = cfg ?? BiomeGenConfig.CreateDefault(biome);
        }
        _cached = map;
        return _cached;
    }

    // F9 hot-reload: drops the cache so the next Get() re-reads the JSONs.
    public static void Invalidate() => _cached = null;

    public static BiomeGenConfig Get(BiomeType biome)
    {
        var all = LoadAll();
        return all.TryGetValue(biome, out var cfg) ? cfg : BiomeGenConfig.CreateDefault(biome);
    }
}
