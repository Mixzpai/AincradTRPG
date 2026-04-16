namespace SAOTRPG.Systems;

// Tracks all monster encounters and kills. Data is collected silently
// during gameplay and can be viewed via the kill stats dialog.
public static class Bestiary
{
    public record Entry(string Name, int Level, int MaxHp, int Atk, int TimesKilled, int TimesEncountered);

    private static readonly Dictionary<string, (int level, int maxHp, int atk, int killed, int encountered)> _entries = new();

    public static void RecordEncounter(string name, int level, int maxHp, int atk)
    {
        if (_entries.TryGetValue(name, out var e))
            _entries[name] = (e.level, e.maxHp, e.atk, e.killed, e.encountered + 1);
        else
            _entries[name] = (level, maxHp, atk, 0, 1);
    }

    public static void RecordKill(string name)
    {
        if (_entries.TryGetValue(name, out var e))
            _entries[name] = (e.level, e.maxHp, e.atk, e.killed + 1, e.encountered);
    }

    // Read-only access for UI display (bestiary dialog, kill stats).
    public static IReadOnlyList<Entry> GetAll() =>
        _entries.Select(kv => new Entry(kv.Key, kv.Value.level, kv.Value.maxHp,
            kv.Value.atk, kv.Value.killed, kv.Value.encountered))
        .OrderByDescending(e => e.TimesKilled)
        .ToList();

}
