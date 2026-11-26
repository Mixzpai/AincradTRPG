using System.Text.Json;

namespace SAOTRPG.Systems.World;

// Per-floor entry prose, loaded from Content/FloorEntries.json and keyed by floor number.
//
// A biome's entry message is written once and read on every floor that biome holds — Void spans 14
// of them — so the line that greets the player says the same thing about fourteen different places.
// An override here replaces it for one floor, which is what lets a canon-anchored floor introduce
// ITSELF rather than its band.
//
// Content only: the text is atmosphere, never a claim about a mechanic, a key or a number, because
// a floor's mechanics move and this file has no way to know when they do.
public static class FloorEntries
{
    private static IReadOnlyDictionary<int, string>? _cached;

    // Why the file was not read, or null when it was. Published rather than logged alone: a missing
    // override file degrades to the biome message on all 100 floors, which is exactly what "no
    // overrides authored yet" also looks like, and nothing on screen distinguishes them.
    // The getter has to force the load. Read before anything else touched the file it answered
    // null — "nothing went wrong" — for the one state it exists to report, and the sabotage that
    // deletes the file was caught only by a neighbouring precondition.
    public static string? LoadFailure { get { Load(); return _loadFailure; } }
    private static string? _loadFailure;

    public static int Count { get { Load(); return _cached!.Count; } }

    public static IReadOnlyDictionary<int, string> All { get { Load(); return _cached!; } }

    // F9 hot-reload. Called from BiomeGenConfigLoader.Invalidate, which is the one content-reload
    // seam the game already has.
    public static void Invalidate() { _cached = null; _loadFailure = null; }

    private static void Load()
    {
        if (_cached != null) return;

        string path = Path.Combine(AppContext.BaseDirectory, "Content", "FloorEntries.json");
        var map = new Dictionary<int, string>();
        try
        {
            if (!File.Exists(path))
            {
                _loadFailure = "no file at Content/FloorEntries.json";
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("FLOOR",
                    "Content/FloorEntries.json not found — every floor falls back to its biome's entry message");
            }
            else
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // A key that is not a floor, or an entry that is blank, is dropped LOUDLY. The
                    // alternative is an authored line that silently never shows.
                    if (!int.TryParse(prop.Name, out int floor) || floor < 1 || floor > 100)
                    {
                        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("FLOOR",
                            $"FloorEntries key '{prop.Name}' is not a floor 1-100 — entry ignored");
                        continue;
                    }
                    string? text = prop.Value.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("FLOOR", $"FloorEntries F{floor} is empty — entry ignored");
                        continue;
                    }
                    map[floor] = text!;
                }
            }
        }
        catch (Exception ex)
        {
            _loadFailure = ex.GetType().Name;
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogError("FloorEntries.Load", ex);
        }
        _cached = map;
    }

    // The floor's own entry line, or null when it has no override and the biome's message stands.
    public static string? For(int floor)
    {
        Load();
        return _cached!.TryGetValue(floor, out var text) ? text : null;
    }
}
