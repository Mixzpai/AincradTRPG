using System.Text.Json;
using System.Text.Json.Serialization;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation;

// Loads Content/Biomes/*.json once and caches; F9 calls Invalidate() to re-read.
// Single-threaded by design — callers must not invoke LoadAll/Invalidate concurrently.
public static class BiomeGenConfigLoader
{
    private static IReadOnlyDictionary<BiomeType, BiomeGenConfig>? _cached;
    private static IReadOnlyDictionary<(BiomeType, string), BiomeGenConfig>? _variants;
    private static IReadOnlyList<string> _unresolvedVariants = Array.Empty<string>();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    // Variants BiomeSystem declares that could NOT be loaded, as "<biome>_<variant>: reason".
    //
    // A declared variant with no readable file falls back to the base biome, which is exactly the
    // silent degradation this codebase keeps finding: the floor would still generate, still look
    // like a Forest, and nothing on screen would say it was meant to be a jungle. The fallback is
    // logged AND published here so the content probe can turn it into a build-gate failure rather
    // than leaving it to somebody noticing that two floors look alike.
    public static IReadOnlyList<string> UnresolvedVariants
    {
        get { LoadAll(); return _unresolvedVariants; }
    }

    // Returns cached dict; populates on first call. Missing/malformed files
    // fall back to BiomeGenConfig.CreateDefault per biome with an error log.
    public static IReadOnlyDictionary<BiomeType, BiomeGenConfig> LoadAll()
    {
        if (_cached != null) return _cached;

        var map = new Dictionary<BiomeType, BiomeGenConfig>();
        string baseDir = Path.Combine(AppContext.BaseDirectory, "Content", "Biomes");

        foreach (BiomeType biome in Enum.GetValues<BiomeType>())
        {
            string path = Path.Combine(baseDir, biome.ToString().ToLowerInvariant() + ".json");
            map[biome] = ReadFile(path, $"BiomeGenConfigLoader.Load({biome})")
                         ?? BiomeGenConfig.CreateDefault(biome);
        }

        // Variant files are loaded from the DECLARATION, not from whatever happens to be on disk:
        // an unreferenced <biome>_<variant>.json is dead content, and a declared one with no file
        // is a defect. Only the second is worth failing on, and only the declaration can see it.
        var vmap = new Dictionary<(BiomeType, string), BiomeGenConfig>();
        var unresolved = new List<string>();
        foreach (var (biome, variant) in BiomeSystem.DeclaredVariants())
        {
            string key = VariantFileStem(biome, variant);
            string path = Path.Combine(baseDir, key + ".json");
            var cfg = ReadFile(path, $"BiomeGenConfigLoader.LoadVariant({key})");
            if (cfg == null)
            {
                unresolved.Add($"{key}: {(File.Exists(path) ? "unreadable" : "no file")}");
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("BIOME",
                    $"variant '{key}' declared by BiomeSystem.GetVariant but not loaded — "
                    + $"its floors fall back to plain {biome} and will generate as that biome");
                continue;
            }
            vmap[(biome, variant)] = cfg;
        }

        _variants = vmap;
        _unresolvedVariants = unresolved;
        _cached = map;
        return _cached;
    }

    private static BiomeGenConfig? ReadFile(string path, string errorContext)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<BiomeGenConfig>(File.ReadAllText(path), JsonOpts);
        }
        catch (Exception ex)
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogError(errorContext, ex);
            return null;
        }
    }

    public static string VariantFileStem(BiomeType biome, string variant) =>
        biome.ToString().ToLowerInvariant() + "_" + variant.ToLowerInvariant();

    // F9 hot-reload: drops the cache so the next Get() re-reads the JSONs. It also drops the
    // per-floor entry text, because F9 is the CONTENT reload and that file is content — without it
    // Content/Biomes edits would hot-reload while Content/FloorEntries.json needed a restart, which
    // is the kind of split an author discovers by wondering why their edit did nothing.
    public static void Invalidate() { _cached = null; _variants = null; FloorEntries.Invalidate(); }

    public static BiomeGenConfig Get(BiomeType biome)
    {
        var all = LoadAll();
        return all.TryGetValue(biome, out var cfg) ? cfg : BiomeGenConfig.CreateDefault(biome);
    }

    // The config for a floor that declares a sub-variant of its biome, or null when it declares
    // none — which is the ordinary case and the caller's cue to use the biome's own config.
    //
    // Null is also what a declared-but-unloadable variant returns. That path is disclosed twice
    // over (a log line at load, and UnresolvedVariants), because a silent fall back to the base
    // biome is indistinguishable from the floor never having declared a variant at all.
    public static BiomeGenConfig? VariantFor(BiomeType biome, int floor)
    {
        string? variant = BiomeSystem.GetVariant(floor);
        if (variant == null) return null;
        LoadAll();
        return _variants!.TryGetValue((biome, variant), out var cfg) ? cfg : null;
    }
}
