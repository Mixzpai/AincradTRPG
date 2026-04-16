using SAOTRPG.Entities;

namespace SAOTRPG.Systems;

// Tracks milestone achievements and rewards players with Col on unlock.
public static class Achievements
{
    public record Achievement(string Id, string Name, string Description, int ColReward);

    private static readonly List<Achievement> All = new()
    {
        // Combat milestones
        new("first_kill",       "First Blood",          "Defeat your first monster",                     50),
        new("first_boss",       "Boss Slayer",          "Defeat your first floor boss",                 200),
        new("kills_100",        "Centurion",            "Defeat 100 monsters",                          500),
        new("kills_500",        "Walking Calamity",     "Defeat 500 monsters",                        2500),
        new("perfect_streak_5", "Untouchable",          "Achieve a 5-kill perfect streak",              300),

        // Floor milestones
        new("floor_5",          "Ascending",            "Reach floor 5",                                150),
        new("floor_10",         "Into the Unknown",     "Reach floor 10",                               300),
        new("floor_25",         "Veteran Climber",      "Reach floor 25",                              1000),
        new("floor_50",         "Halfway There",        "Reach floor 50",                              3000),
        new("floor_100",        "Apex of Aincrad",      "Reach floor 100",                            10000),
        new("speed_clear",      "Speed Demon",          "Speed-clear a floor under par time",           200),

        // Exploration & survival
        new("all_lore",         "Loremaster",           "Discover all lore stones",                    1500),
        new("bounty_complete",  "Bounty Hunter",        "Complete a bounty contract",                   250),
        new("survive_hemorrhage","Iron Will",           "Survive a hemorrhage combo",                   400),
    };

    private static readonly Dictionary<string, Achievement> ById =
        All.ToDictionary(a => a.Id);

    // Set of achievement IDs that have been unlocked this run. Persisted via save.
    public static HashSet<string> Unlocked { get; set; } = new();

    // Attempts to unlock an achievement by ID. Returns the Achievement if newly unlocked, null otherwise.
    public static Achievement? TryUnlock(string id)
    {
        if (!ById.TryGetValue(id, out var ach)) return null;
        return Unlocked.Add(id) ? ach : null;
    }

    // ── Combat-related checks (called after each monster kill) ───────

    // Checks combat-related achievements after a monster kill.
    // Returns all newly unlocked achievements.
    public static List<Achievement> CheckCombat(TurnManager tm, Player player, Monster monster)
    {
        var results = new List<Achievement>();

        // First kill
        if (tm.KillCount == 1)
            AddIfNew(results, "first_kill");

        // Boss kill
        if (monster is Boss)
            AddIfNew(results, "first_boss");

        // Kill milestones
        if (tm.KillCount >= 100) AddIfNew(results, "kills_100");
        if (tm.KillCount >= 500) AddIfNew(results, "kills_500");

        // Perfect kill streak (5+ kills without taking damage)
        if (tm.KillStreak >= 5)
            AddIfNew(results, "perfect_streak_5");

        return results;
    }

    // ── Floor-related checks (called on floor ascend) ────────────────

    // Checks floor-related achievements after ascending a floor.
    // Returns all newly unlocked achievements.
    public static List<Achievement> CheckFloor(TurnManager tm, Player player, bool speedClear = false)
    {
        var results = new List<Achievement>();

        // Speed clear
        if (speedClear) AddIfNew(results, "speed_clear");

        int floor = tm.CurrentFloor;
        if (floor >= 5)   AddIfNew(results, "floor_5");
        if (floor >= 10)  AddIfNew(results, "floor_10");
        if (floor >= 25)  AddIfNew(results, "floor_25");
        if (floor >= 50)  AddIfNew(results, "floor_50");
        if (floor >= 100) AddIfNew(results, "floor_100");

        // Bounty completion (checked here since bounty resolves during floor play)
        if (tm.BountyComplete)
            AddIfNew(results, "bounty_complete");

        // Lore collection
        if (tm.DiscoveredLore.Count >= FlavorText.LoreStoneEntries.Length)
            AddIfNew(results, "all_lore");

        return results;
    }

    private static void AddIfNew(List<Achievement> results, string id)
    {
        var ach = TryUnlock(id);
        if (ach != null) results.Add(ach);
    }
}
