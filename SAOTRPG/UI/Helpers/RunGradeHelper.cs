namespace SAOTRPG.UI.Helpers;

// Shared run-rating logic used by both DeathScreen and VictoryScreen.
// Score formula: (floor × 20) + (kills × 3) + efficiency bonus (50 − turns/10).
public static class RunGradeHelper
{
    // Descending thresholds, first match wins. Score = floor·20 + kills·3 + max(0, 50 − turns/10).
    // Ref: F5/15k/100t→185 (A), F3/8k/150t→119 (B), F1/3k/50t→74 (B).
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
