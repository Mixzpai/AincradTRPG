using System.Diagnostics;
using SAOTRPG.UI;

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

    public int Count => _byName.Count;
    public bool Loaded => _loaded;

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

    // Forces a reload — used by dev tools only. Not thread-safe.
    public void Reload()
    {
        _byName.Clear();
        _byBiome.Clear();
        _byTag.Clear();
        _loaded = false;
        LoadAll();
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

    // Lookup by exact NAME. Null if not present.
    public PrefabDefinition? GetByName(string name) =>
        _byName.TryGetValue(name, out var def) ? def : null;

    // Pool of candidates matching biome + floor + optional tag filter.
    // "any"-biome prefabs always included. Empty Floors list = any floor.
    public IEnumerable<PrefabDefinition> CandidatesFor(string biome, int floor, IEnumerable<string>? tagsRequired = null)
    {
        LoadAll();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string[]? required = tagsRequired?.ToArray();

        foreach (var bucket in new[] { biome, "any" })
        {
            if (!_byBiome.TryGetValue(bucket, out var list)) continue;
            foreach (var p in list)
            {
                if (!seen.Add(p.Name)) continue;
                if (p.Floors.Count > 0 && !p.Floors.Contains(floor)) continue;
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

    public IReadOnlyCollection<string> AllNames() => _byName.Keys;
}
