using System.Diagnostics;
using SAOTRPG.Systems.Diagnostics;

namespace SAOTRPG.Map.Generation.Prefabs;

// Eager-load cache of all Content/Prefabs/**/*.prefab files. First LoadAll
// parses every file; subsequent calls no-op. Biome + tag indices back fast lookup.
public sealed class PrefabLibrary
{
    public static readonly PrefabLibrary Shared = new();

    private readonly Dictionary<string, PrefabDefinition> _byName = new();
    private readonly Dictionary<string, List<PrefabDefinition>> _byBiome = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PrefabDefinition>> _byTag   = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    // The tag that marks a biome's guaranteed landmark. Declared here because both the filter
    // below and SignaturePrefabPass have to mean the same string by it.
    public const string SignatureTag = "signature";

    // The tag that marks a floor's guaranteed set-piece. Same contract as SignatureTag and
    // for the same reason: CanonOverlayPass owns the placement, so the general weighted draw must
    // not be able to stamp a second copy of the same landmark.
    public const string CanonOverlayTag = "canon-overlay";

    // Every tag whose prefabs belong to a guarantee pass. A prefab carrying one of these is
    // invisible to an untagged query. A set rather than special cases, so every guarantee pass
    // gets the whole contract from one entry here.
    private static readonly string[] GuaranteedTags = { SignatureTag, CanonOverlayTag };

    public int Count => _byName.Count;

    public void LoadAll()
    {
        if (_loaded) return;
        _loaded = true;

        string root = Path.Combine(AppContext.BaseDirectory, "Content", "Prefabs");
        if (!Directory.Exists(root))
        {
            DebugLogger.LogGame("PREFAB", $"no Content/Prefabs directory at {root}");
            return;
        }

        var sw = Stopwatch.StartNew();
        int files = 0, parsed = 0, dup = 0;
        foreach (string file in Directory.EnumerateFiles(root, "*.prefab", SearchOption.AllDirectories))
        {
            files++;
            try
            {
                var def = PrefabParser.ParseFile(file);
                if (def == null) continue;
                if (_byName.ContainsKey(def.Name))
                {
                    DebugLogger.LogGame("PREFAB", $"duplicate NAME '{def.Name}' in {file} — skipping");
                    dup++;
                    continue;
                }
                _byName[def.Name] = def;
                Index(def);
                parsed++;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"PrefabLibrary.LoadAll {file}", ex);
            }
        }
        sw.Stop();
        DebugLogger.LogGame("PREFAB",
            $"loaded {parsed}/{files} prefabs ({dup} duplicates) in {sw.Elapsed.TotalMilliseconds:F1}ms");
    }

    private void Index(PrefabDefinition def)
    {
        var biomes = def.Biomes.Count == 0 ? new[] { "any" } : def.Biomes;
        foreach (var b in biomes)
        {
            if (!_byBiome.TryGetValue(b, out var list)) _byBiome[b] = list = new();
            list.Add(def);
        }
        foreach (var t in def.Tags)
        {
            if (!_byTag.TryGetValue(t, out var list)) _byTag[t] = list = new();
            list.Add(def);
        }
    }

    // Pool of candidates matching biome + floor + optional tag filter.
    // "any"-biome prefabs always included. Empty Floors list = any floor.
    //
    // A prefab carrying a guaranteed tag (`signature`, `canon-overlay`) is excluded from an
    // untagged query. Each is placed by the pass that owns its guarantee; leaving one in the
    // general weighted draw as well would put a second copy of the same landmark on some floors,
    // against the MAX_PER_FLOOR the files declare, which nothing else enforces. Asking for the
    // tag explicitly still returns them.
    public IEnumerable<PrefabDefinition> CandidatesFor(string biome, int floor, IEnumerable<string>? tagsRequired = null)
    {
        LoadAll();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string[]? required = tagsRequired?.ToArray();
        bool wantsGuaranteed = false;
        if (required != null)
            foreach (var g in GuaranteedTags)
                if (Array.IndexOf(required, g) >= 0) { wantsGuaranteed = true; break; }

        foreach (var bucket in new[] { biome, "any" })
        {
            if (!_byBiome.TryGetValue(bucket, out var list)) continue;
            foreach (var p in list)
            {
                if (!seen.Add(p.Name)) continue;
                if (p.Floors.Count > 0 && !p.Floors.Contains(floor)) continue;
                if (!wantsGuaranteed && CarriesGuaranteedTag(p)) continue;
                if (required != null && required.Length > 0)
                {
                    bool ok = true;
                    foreach (var t in required) if (!p.Tags.Contains(t)) { ok = false; break; }
                    if (!ok) continue;
                }
                yield return p;
            }
        }
    }

    private static bool CarriesGuaranteedTag(PrefabDefinition p)
    {
        foreach (var g in GuaranteedTags) if (p.Tags.Contains(g)) return true;
        return false;
    }
}
