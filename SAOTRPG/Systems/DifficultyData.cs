using Terminal.Gui;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Difficulty tier data. MobStatPercent 40-300% (~7.5× spread). XpPercent inverse.
// RegenInterval: lower=faster, 0=off. ColStreakBonus: per kill-streak tier (8% Story → 1% Unwinnable).
public static class DifficultyData
{
    // All properties that define a difficulty tier.
    public record DifficultyTier(
        string Name,
        string Description,
        Color ThemeColor,
        int MobStatPercent,     // Enemy HP/ATK/DEF multiplier (100 = normal)
        int XpPercent,          // XP reward multiplier (100 = normal)
        int RegenInterval,      // Turns between 1 HP regen (0 = no regen)
        int ColStreakBonus);    // % Col bonus per kill streak tier

    // Tier index: 0=Story, 1=VeryEasy, 2=Easy, 3=Normal, 4=Hard,
    //             5=VeryHard, 6=Masochist, 7=Unwinnable, 8=Debug
    private static readonly DifficultyTier[] BaseTiers =
    {
        new("Story",      "Sit back and enjoy the tale of Aincrad. Combat is an afterthought.",
            Color.BrightCyan,    40, 200, 3, 8),

        new("Very Easy",  "A carefree stroll through Aincrad. Enemies are weak, danger is minimal.",
            Color.BrightGreen,   60, 150, 3, 7),

        new("Easy",       "A relaxed adventure. Reduced enemy stats, forgiving combat.",
            Color.Green,         80, 120, 4, 6),

        new("Normal",     "The intended experience. Balanced risk and reward.",
            Color.White,        100, 100, 5, 5),

        new("Hard",       "Tougher enemies, less room for error.",
            Color.Yellow,       130,  90, 6, 4),

        new("Very Hard",  "Punishing combat. Every mistake costs you.",
            Color.BrightRed,    170,  75, 8, 3),

        new("Masochist",  "You asked for this. No mercy. No regrets.",
            Color.Red,          220,  60, 10, 2),

        new("Unwinnable", "You will not make it past Floor 1. Don't kid yourself.",
            Color.BrightMagenta, 300, 50, 0, 1),
    };

    private static readonly DifficultyTier DebugTier =
        new("Debug", "Developer mode. All restrictions lifted.",
            Color.Gray, 50, 500, 3, 10);

    // Returns all available tiers. Debug tier is appended when --debug is active.
    public static DifficultyTier[] GetTiers()
    {
        if (!DebugMode.IsEnabled) return BaseTiers;

        var tiers = new DifficultyTier[BaseTiers.Length + 1];
        BaseTiers.CopyTo(tiers, 0);
        tiers[^1] = DebugTier;
        return tiers;
    }

    // Safe accessor with bounds clamping. Out-of-range indices clamp to nearest valid tier.
    public static DifficultyTier Get(int index)
    {
        var tiers = GetTiers();
        return tiers[Math.Clamp(index, 0, tiers.Length - 1)];
    }

    // Tier name for index (SaveManager slot summaries etc.).
    public static string GetName(int index) => Get(index).Name;

    // Formatted stat breakdown for the [?] tooltip dialog.
    public static string GetStatsTooltip(int index)
    {
        var t = Get(index);
        string regen = t.RegenInterval > 0 ? $"Every {t.RegenInterval} turns" : "None";

        return $"  Enemy Stats:   {t.MobStatPercent}%\n" +
               $"  XP Rewards:    {t.XpPercent}%\n" +
               $"  HP Regen:      {regen}\n" +
               $"  Col Bonus:     +{t.ColStreakBonus}% per streak";
    }
}
