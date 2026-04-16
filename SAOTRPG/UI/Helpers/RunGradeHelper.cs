namespace SAOTRPG.UI.Helpers;

// Shared run-rating logic used by both DeathScreen and VictoryScreen.
// Score formula: (floor × 20) + (kills × 3) + efficiency bonus (50 − turns/10).
public static class RunGradeHelper
{
    // Grade thresholds checked in descending order — first match wins.
    // Score formula: (floor × 20) + (kills × 3) + max(0, 50 − turns/10).
    // Reference scores for a typical run:
    //   Floor 5, 15 kills, 100 turns → 185 (A)
    //   Floor 3, 8 kills, 150 turns  → 119 (B)
    //   Floor 1, 3 kills, 50 turns   →  74 (B)
    private static readonly (int MinScore, string Grade, string Comment)[] RunGrades =
    {
        (200, "S", "Legendary!"),   // Deep floors + high kills + efficient
        (120, "A", "Excellent"),    // Solid all-around performance
        (60,  "B", "Good run"),     // Decent progress with some kills
        (30,  "C", "Average"),      // Minimal progress or slow pace
        (0,   "D", "Keep trying"),  // Very early death or idle run
    };

    // Rate a run based on floor reached, kills, and turn count.
    // Returns a formatted string like "A (Excellent)".
    public static string Rate(int floor, int kills, int turns)
    {
        int score = (floor * 20) + (kills * 3) + Math.Max(0, 50 - turns / 10);
        foreach (var (minScore, grade, comment) in RunGrades)
        {
            if (score >= minScore)
                return $"{grade} ({comment})";
        }
        return "D (Keep trying)";
    }

    // Rate a victory run — guarantees at least S grade for clearing Floor 100.
    // Exceptionally efficient runs (score ≥ 300) earn S+ (Liberator!).
    public static string RateVictory(int floor, int kills, int turns)
    {
        int score = (floor * 20) + (kills * 3) + Math.Max(0, 50 - turns / 10);

        // Floor 100 clear is always S-tier minimum
        if (score >= 300)
            return "S+ (Liberator!)";

        return "S (Legendary!)";
    }
}
