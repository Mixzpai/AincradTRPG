using System.Text.Json;

namespace SAOTRPG.Systems;

// Persistent lifetime statistics tracked across all runs.
// Saved to %LocalAppData%/AincradTRPG/lifetime_stats.json.
// Updated on death and victory — never resets.
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

    public class Data
    {
        public int TotalRuns { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalVictories { get; set; }
        public int TotalKills { get; set; }
        public int HighestFloor { get; set; }
        public string BestGrade { get; set; } = "D";
        public long TotalPlayTimeSeconds { get; set; }
        public int HighestLevel { get; set; }
        public int TotalColEarned { get; set; }
        // RecentRuns = last N completed runs regardless of outcome (death
        // or victory). Cap is 10.
        public List<RunEntry> RecentRuns { get; set; } = new();
        // VictoryRuns = ONLY completed victories. Uncapped — the leaderboard
        // view can sort these by any field. Legacy saves start with an
        // empty list and get populated on the next victory.
        public List<RunEntry> VictoryRuns { get; set; } = new();
    }

    // Load stats from disk. Returns empty stats if file missing or corrupt.
    public static Data Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new Data();
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Data>(json, JsonOpts) ?? new Data();
        }
        catch
        {
            return new Data();
        }
    }

    // Save stats to disk. Silent on failure.
    public static void Save(Data data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string json = JsonSerializer.Serialize(data, JsonOpts);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex) { UI.DebugLogger.LogError("LifetimeStats.Save", ex); }
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

    // Record a completed run (death or victory). Victories are also
    // appended to the VictoryRuns list (uncapped) for the leaderboard view.
    // RecentRuns holds the last 10 of any outcome.
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

        // VictoryRuns: ONLY completed victories. Uncapped (Aincrad-climb
        // achievements are permanent brag-worthy). Leaderboard sort is
        // done at display time — stored in insertion order.
        if (victory) data.VictoryRuns.Add(entry);

        Save(data);
    }
}
