using System.Text.Json;
using SAOTRPG.Systems.Progression;

namespace SAOTRPG.Systems.Persistence;

// Lifetime stats across runs at %LocalAppData%/AincradTRPG/lifetime_stats.json.
// Updated on death/victory; never resets.
public static class LifetimeStats
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AincradTRPG", "lifetime_stats.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    // DTO for lifetime stats persistence.
    public class RunEntry
    {
        public int Floor { get; set; }
        public int Level { get; set; }
        public int Kills { get; set; }
        public string Grade { get; set; } = "D";
        public int ColEarned { get; set; }
        public long PlayTimeSeconds { get; set; }
        public bool Victory { get; set; }
        public string Date { get; set; } = "";
        // Legacy entries default to "Unknown" / 0 since old saves lack
        // these fields. Deserializer leaves them at default; no migration needed.
        public string PlayerName { get; set; } = "Unknown";
        public int TurnCount { get; set; }
    }

    // Per-mob knowledge carried across runs. Survives permadeath so the
    // player's bestiary is cumulative. Populated by Bestiary.SaveToLifetimeStats.
    public class BestiaryKnowledge
    {
        public string Name { get; set; } = "";
        public int Kills { get; set; }
        public int Encounters { get; set; }
        public int FirstFloor { get; set; }
        public int LastFloor { get; set; }
        public int DeathsCaused { get; set; }
        public string LastSeenDate { get; set; } = "";
        // Encounter loot tag persisted across runs.
        // Nullable: legacy entries → null, resolved at load via MobFactory fallback.
        public string? LootTag { get; set; }
    }

    public class Data
    {
        public int TotalRuns { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalVictories { get; set; }
        public int TotalKills { get; set; }
        public int HighestFloor { get; set; }
        // Player Guide unlock gate. Updated on every floor ascent (not run-end).
        // Distinct from HighestFloor, which only updates in RecordRun.
        public int MaxFloorReached { get; set; }
        public string BestGrade { get; set; } = "D";
        public long TotalPlayTimeSeconds { get; set; }
        public int HighestLevel { get; set; }
        public int TotalColEarned { get; set; }
        // RecentRuns = last N completed runs regardless of outcome (death
        // or victory). Cap is 10.
        public List<RunEntry> RecentRuns { get; set; } = new();
        // Victory-only list, uncapped (leaderboard sorts at display time).
        public List<RunEntry> VictoryRuns { get; set; } = new();
        // Cross-run bestiary knowledge, keyed by mob Name. Legacy saves
        // start empty and get populated the first time Bestiary.Save runs.
        public Dictionary<string, BestiaryKnowledge> BestiaryKnown { get; set; } = new();

        // All milestone IDs the player has unlocked, persisted across permadeath.
        // Single source of truth for achievements / titles / life-skill bonuses /
        // collectables. Missing on legacy data → empty list.
        public List<string> UnlockedMilestones { get; set; } = new();

        // Cross-run best counts for QuestSubCategory turn-ins. Updated on each
        // tagged turn-in if the current run exceeds the prior best.
        public int IfImplementHighWaterMark { get; set; }
        public int HfMissionHighWaterMark { get; set; }

        // Lifetime counters behind two milestones that advertise an exact number.
        // FieldBoss is excluded from the boss count, matching the distinction
        // TurnManager.FloorBossAlive already draws: wilderness elites are optional
        // content and must not count toward "floor bosses".
        public int FloorBossLastHits { get; set; }
        public int WeaponUpgrades { get; set; }
    }

    // ── THE READ CACHE, AND WHY IT IS GUARDED BY THE FILE'S OWN TIMESTAMP ──────────────────
    //
    // Load used to read and re-deserialize the whole file on every call. It is not a UI-only
    // path: measured over a 900-turn seeded run that fought its way through a floor, it was
    // called 722 times at 0.22 ms and ~10 KB each -- 10% of all turn time, and about 7.6 MB of
    // garbage -- because Bestiary.RecordEncounter and the MilestoneSystem hooks reach it from
    // inside the turn loop.
    //
    // The cache is keyed on the file's LastWriteTimeUtc rather than trusting Save() alone. This
    // file is the permadeath ledger and it is moved aside and back by the probe harnesses, so a
    // cache that only invalidated on OUR writes would serve a previous run's stats after an
    // external swap -- silent, and in the worst direction. A missing file reads as a fixed
    // sentinel, so deleting it invalidates too. One attribute query against a read plus a parse.
    //
    // CALLERS MUST NOT MUTATE WITHOUT SAVING. Load now hands back the cached instance rather
    // than a private copy, which is what makes it free. Every mutating call site today follows
    // the read with Save() in the same block; Tools/ProgressProbe holds that as a check, by
    // comparing the cached view against the file after a run.
    private static Data? _cache;
    private static long _cacheTicks = -1;
    private static long _cacheLength = -1;
    // The last read of an existing file failed; Save must not overwrite it with defaults.
    private static bool _loadFailed;

    // Write time AND length, from one attribute query. Write time alone is not enough: NTFS
    // stamps come off the system clock, whose update granularity is coarser than a fast
    // write-read-write, so two writes can share a timestamp. A different length settles those,
    // and our own Save() refreshes the cache directly so it never depends on this at all.
    // ONE FileInfo, re-stated rather than reallocated. The cache removed the parse, so this guard
    // is now the entire cost of a Load — and Load runs 3.12 times per turn, so a fresh FileInfo at
    // 96 B each was ~300 B of per-turn garbage against the 8 KB budget ProgressProbe holds.
    // Refresh() re-stats the same object: 0 B and 39.5 us against 96 B and 41.7. FilePath is
    // static readonly, so the object can never be bound to a stale path.
    private static FileInfo? _stat;

    private static void ReadStamp(out long ticks, out long length)
    {
        try
        {
            var fi = _stat ??= new FileInfo(FilePath);
            fi.Refresh();   // without this the cached object answers from its first stat, forever
            if (!fi.Exists) { ticks = -1; length = -1; return; }
            ticks = fi.LastWriteTimeUtc.Ticks;
            length = fi.Length;
        }
        catch { ticks = -2; length = -2; }
    }

    // Times Load has been called this process. A measurement seam, in the same family as
    // LightingSystem.SourcesLastUpdate and MapView.DrainFramesDrawn: the cache makes a repeat
    // Load nearly free, so ALLOCATION can no longer tell "read once" apart from "read once per
    // row" -- 337 cache hits cost less than the refresh they sit inside. The call count still
    // separates them absolutely, and it is the property the caller actually cares about.
    public static long LoadCalls;

    // Load stats from disk. Returns empty stats if file missing or corrupt.
    public static Data Load()
    {
        LoadCalls++;
        ReadStamp(out long ticks, out long length);
        if (_cache != null && ticks == _cacheTicks && length == _cacheLength) return _cache;
        try
        {
            if (ticks == -1) { _loadFailed = false; _cache = new Data(); _cacheTicks = ticks; _cacheLength = length; return _cache; }
            string json = File.ReadAllText(FilePath);
            _cache = JsonSerializer.Deserialize<Data>(json, JsonOpts) ?? new Data();
            _loadFailed = false;
        }
        catch (Exception ex)
        {
            // Serve one stable empty instance (so unlocks do not repeat), never cache the stamp
            // (so the next Load retries), and refuse to Save over the unreadable file.
            if (!_loadFailed) { SAOTRPG.Systems.Diagnostics.DebugLogger.LogError("LifetimeStats.Load", ex); _cache = null; }
            _loadFailed = true;
            _cacheTicks = -3; _cacheLength = -3;
            return _cache ??= new Data();
        }
        // Re-stat AFTER reading: if something rewrote the file while it was being read, what was
        // parsed does not match the stamp taken before, so refuse to cache it.
        ReadStamp(out long after, out long afterLen);
        if (after == ticks && afterLen == length) { _cacheTicks = ticks; _cacheLength = length; }
        else { _cacheTicks = -3; _cacheLength = -3; }
        return _cache;
    }

    // Save stats to disk. Silent on failure.
    public static void Save(Data data)
    {
        if (_loadFailed)
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("ERROR", "LifetimeStats.Save refused: the file on disk could not be read");
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string json = JsonSerializer.Serialize(data, JsonOpts);
            // Write-and-rename, like SaveManager, UserSettings, Keybinds and ProfileData. This is
            // the permadeath ledger and it is written DURING PLAY, so its exposure window is the
            // widest of the set: WriteAllText truncates first, and Load answers a parse failure by
            // starting from defaults — a torn write would silently reset every milestone unlock,
            // victory run and bestiary entry, and the next Save would make that permanent.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, FilePath, overwrite: true);
            _cache = data;
            ReadStamp(out _cacheTicks, out _cacheLength);
        }
        catch (Exception ex)
        {
            // A failed write must not leave the cache claiming to match the file on disk.
            _cache = null; _cacheTicks = -1; _cacheLength = -1;
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogError("LifetimeStats.Save", ex);
        }
    }

    // Grade sort order — higher is better.
    private static int GradeRank(string grade) => grade switch
    {
        _ when grade.StartsWith("S+") => 6,
        _ when grade.StartsWith("S")  => 5,
        _ when grade.StartsWith("A")  => 4,
        _ when grade.StartsWith("B")  => 3,
        _ when grade.StartsWith("C")  => 2,
        _ => 1,
    };

    // True if the milestone Id is already unlocked in the persistent set.
    public static bool IsMilestoneUnlocked(string id)
    {
        var data = Load();
        return data.UnlockedMilestones.Contains(id);
    }

    // Record per-ascent floor reach. No-op when floor <= existing max.
    // Fires from TurnManager.AscendFloor immediately after CurrentFloor++.
    public static void RecordFloorReach(int floor)
    {
        var data = Load();
        if (floor <= data.MaxFloorReached) return;
        data.MaxFloorReached = floor;
        Save(data);
    }

    // Record run. Victories append to VictoryRuns (uncapped); RecentRuns caps at 10.
    public static void RecordRun(int kills, int floor, int level, string grade,
        TimeSpan playTime, int colEarned, bool victory,
        string playerName = "Unknown", int turnCount = 0)
    {
        var data = Load();
        data.TotalRuns++;
        if (victory) data.TotalVictories++;
        else data.TotalDeaths++;
        data.TotalKills += kills;
        if (floor > data.HighestFloor) data.HighestFloor = floor;
        if (level > data.HighestLevel) data.HighestLevel = level;
        if (GradeRank(grade) > GradeRank(data.BestGrade)) data.BestGrade = grade;
        data.TotalPlayTimeSeconds += (long)playTime.TotalSeconds;
        data.TotalColEarned += colEarned;

        var entry = new RunEntry
        {
            Floor = floor, Level = level, Kills = kills, Grade = grade,
            ColEarned = colEarned, PlayTimeSeconds = (long)playTime.TotalSeconds,
            Victory = victory, Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            PlayerName = playerName, TurnCount = turnCount,
        };

        // RecentRuns: last 10 of any outcome. Insert at head, trim tail.
        data.RecentRuns.Insert(0, entry);
        if (data.RecentRuns.Count > 10)
            data.RecentRuns.RemoveRange(10, data.RecentRuns.Count - 10);

        // Victory-only, uncapped. Stored insertion-order; sort at display.
        if (victory) data.VictoryRuns.Add(entry);

        Save(data);

        // Durable Bestiary write even without manual saves.
        Bestiary.SaveToLifetimeStats();
    }
}
