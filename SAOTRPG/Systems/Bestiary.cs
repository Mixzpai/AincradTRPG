using Terminal.Gui;

namespace SAOTRPG.Systems;

// Tracks monster encounters/kills. Viewable via Bestiary dialog (Y); persists
// across runs via LifetimeStats (survives permadeath).
public static class Bestiary
{
    // Glyph/GlyphColor re-derive from renderer per session (not serialized).
    // DeathsCausedAcrossRuns comes from LifetimeStats at load.
    public record Entry(
        string Name,
        int Level,
        int MaxHp,
        int Atk,
        int TimesKilled,
        int TimesEncountered,
        int FirstFloorEncountered,
        int LastFloorEncountered,
        string LootTag,
        char Glyph,
        Color GlyphColor,
        bool IsBoss,
        bool IsFieldBoss,
        bool IsElite,
        int DeathsCausedAcrossRuns,
        bool CanPoison,
        bool CanBleed,
        bool CanStun,
        bool CanSlow,
        string LastSeenDate);

    // Internal row — mirrors Entry minus derived fields. Serializable state
    // used by both the session cache and the LifetimeStats crossover.
    private struct Row
    {
        public int Level;
        public int MaxHp;
        public int Atk;
        public int Killed;
        public int Encountered;
        public int FirstFloor;
        public int LastFloor;
        public string LootTag;
        public char Glyph;
        public Color GlyphColor;
        public bool IsBoss;
        public bool IsFieldBoss;
        public bool IsElite;
        public int DeathsCausedAcrossRuns;
        public bool CanPoison;
        public bool CanBleed;
        public bool CanStun;
        public bool CanSlow;
        public string LastSeenDate;
    }

    private static readonly Dictionary<string, Row> _entries = new();

    // Dialog-session memory: persists across dialog opens within a run so
    // reopening snaps back to last-read entry, sort, and tab.
    public static string SessionSort = "A";            // A/L/K/R/F
    public static string? SessionSelectedName;
    public static string SessionActiveTab = "Overview"; // Overview/Combat/Lore/History
    public static BestiaryFilterState SessionFilterState = new();

    // Expanded encounter recorder — callers pass everything the mob knows
    // about itself so the Bestiary can render proper glyph, tags, flags.
    public static void RecordEncounter(
        string name, int level, int maxHp, int atk,
        int floor = 1,
        string lootTag = "generic",
        char glyph = '?',
        Color glyphColor = default,
        bool isBoss = false,
        bool isFieldBoss = false,
        bool isElite = false,
        bool canPoison = false,
        bool canBleed = false,
        bool canStun = false,
        bool canSlow = false)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (_entries.TryGetValue(name, out var r))
        {
            r.Encountered += 1;
            r.Level = level;        // refresh to most-recent seen
            r.MaxHp = maxHp;
            r.Atk   = atk;
            r.LastFloor = Math.Max(r.LastFloor, floor);
            if (r.FirstFloor == 0 || floor < r.FirstFloor) r.FirstFloor = floor;
            r.LootTag = lootTag;
            r.Glyph = glyph;
            r.GlyphColor = glyphColor;
            r.IsBoss      = r.IsBoss      || isBoss;
            r.IsFieldBoss = r.IsFieldBoss || isFieldBoss;
            r.IsElite     = r.IsElite     || isElite;
            r.CanPoison = r.CanPoison || canPoison;
            r.CanBleed  = r.CanBleed  || canBleed;
            r.CanStun   = r.CanStun   || canStun;
            r.CanSlow   = r.CanSlow   || canSlow;
            r.LastSeenDate = today;
            _entries[name] = r;
        }
        else
        {
            _entries[name] = new Row
            {
                Level = level, MaxHp = maxHp, Atk = atk,
                Killed = 0, Encountered = 1,
                FirstFloor = floor, LastFloor = floor,
                LootTag = lootTag,
                Glyph = glyph, GlyphColor = glyphColor,
                IsBoss = isBoss, IsFieldBoss = isFieldBoss, IsElite = isElite,
                CanPoison = canPoison, CanBleed = canBleed,
                CanStun = canStun, CanSlow = canSlow,
                DeathsCausedAcrossRuns = 0,
                LastSeenDate = today,
            };
        }
    }

    public static void RecordKill(string name)
    {
        if (_entries.TryGetValue(name, out var r))
        {
            r.Killed += 1;
            r.LastSeenDate = DateTime.Now.ToString("yyyy-MM-dd");
            _entries[name] = r;
        }
    }

    // Read-only snapshot for UI display. Ordered by TimesKilled desc to
    // preserve legacy behavior used by MonumentDialog and title-unlock code.
    public static IReadOnlyList<Entry> GetAll() =>
        _entries.Select(kv => ToEntry(kv.Key, kv.Value))
            .OrderByDescending(e => e.TimesKilled)
            .ToList();

    // Direct lookup for the dialog.
    public static Entry? Get(string name) =>
        _entries.TryGetValue(name, out var r) ? ToEntry(name, r) : null;

    // Lifetime-only rows arrive with GlyphColor == default (Color.Black),
    // which is invisible on dark terminals — fall back to Gray so the row renders.
    private static Entry ToEntry(string name, Row r) => new(
        name, r.Level, r.MaxHp, r.Atk,
        r.Killed, r.Encountered,
        r.FirstFloor, r.LastFloor,
        r.LootTag ?? "generic",
        r.Glyph == default ? '?' : r.Glyph,
        r.GlyphColor == default ? Color.Gray : r.GlyphColor,
        r.IsBoss, r.IsFieldBoss, r.IsElite,
        r.DeathsCausedAcrossRuns,
        r.CanPoison, r.CanBleed, r.CanStun, r.CanSlow,
        r.LastSeenDate ?? "");

    // ── Cross-run persistence (survives permadeath) ──
    // Serializes knowledge (name/counts/floors/date); visuals re-derive.

    // Merge the current session's entries into lifetime storage and save.
    // Called on death (before save-slot deletion) and on manual saves.
    public static void SaveToLifetimeStats()
    {
        var data = LifetimeStats.Load();
        foreach (var (name, r) in _entries)
        {
            if (!data.BestiaryKnown.TryGetValue(name, out var known))
                known = new LifetimeStats.BestiaryKnowledge { Name = name };

            known.Kills       = Math.Max(known.Kills,       r.Killed);
            known.Encounters  = Math.Max(known.Encounters,  r.Encountered);
            known.FirstFloor  = known.FirstFloor == 0 ? r.FirstFloor
                                : Math.Min(known.FirstFloor, r.FirstFloor);
            known.LastFloor   = Math.Max(known.LastFloor,   r.LastFloor);
            // DeathsCaused owned by LifetimeStats (RecordDeathCause); don't overwrite.
            known.LastSeenDate = string.IsNullOrEmpty(r.LastSeenDate)
                ? known.LastSeenDate : r.LastSeenDate;

            data.BestiaryKnown[name] = known;
        }
        LifetimeStats.Save(data);
    }

    // Seed session dict with lifetime-known counts so "discovered" totals reflect
    // every mob ever seen. Called at game start / new-run init.
    public static void LoadFromLifetimeStats()
    {
        var data = LifetimeStats.Load();
        foreach (var (name, k) in data.BestiaryKnown)
        {
            if (_entries.ContainsKey(name)) continue;  // session beats lifetime
            _entries[name] = new Row
            {
                Level = 0, MaxHp = 0, Atk = 0,
                Killed = k.Kills,
                Encountered = k.Encounters,
                FirstFloor = k.FirstFloor,
                LastFloor = k.LastFloor,
                LootTag = "generic",
                Glyph = '?',
                GlyphColor = default,
                DeathsCausedAcrossRuns = k.DeathsCaused,
                LastSeenDate = k.LastSeenDate ?? "",
            };
        }
    }

    // Tally death-cause into lifetime record so detail pane can show
    // "You've died to this N times" across runs.
    public static void RecordDeathCause(string killerName)
    {
        if (string.IsNullOrWhiteSpace(killerName)) return;
        var data = LifetimeStats.Load();
        if (!data.BestiaryKnown.TryGetValue(killerName, out var known))
            known = new LifetimeStats.BestiaryKnowledge { Name = killerName };
        known.DeathsCaused += 1;
        known.LastSeenDate = DateTime.Now.ToString("yyyy-MM-dd");
        data.BestiaryKnown[killerName] = known;
        LifetimeStats.Save(data);
    }

    // ── Totals for the completion counter ─────────────────────────────────
    public static int DiscoveredCount() => _entries.Count;

    // Total roster: MobFactory + BossFactory + FieldBoss (exact registry lengths,
    // not the old 100/30 stand-ins) so completion meter matches reality.
    public static int TotalRosterCount()
    {
        int total = 0;
        foreach (int tier in Enumerable.Range(0, 10))
        {
            try { total += Map.MobFactory.GetFloorMobNames(tier).Length; } catch { }
        }
        try { total += Map.BossFactory.RosterCount; } catch { total += 100; }
        try { total += Map.FieldBossFactory.RosterCount; } catch { total += 30; }
        return Math.Max(total, _entries.Count);  // never show < discovered
    }
}

// Filter state for the dialog. Persists across opens within a run.
public class BestiaryFilterState
{
    public HashSet<string> ActiveTags = new();   // loot tags the user toggled on
    public bool BossOnly;
    public bool ShowUndiscovered;
    public int FloorMin = 1;
    public int FloorMax = 100;
    public string Search = "";
    public bool HasAnyActive =>
        ActiveTags.Count > 0 || BossOnly || ShowUndiscovered ||
        FloorMin > 1 || FloorMax < 100 || !string.IsNullOrEmpty(Search);
    public void Clear()
    {
        ActiveTags.Clear();
        BossOnly = false;
        ShowUndiscovered = false;
        FloorMin = 1;
        FloorMax = 100;
        Search = "";
    }
}
