using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

// Shared weapon proficiency display formatting used by DeathScreen,
// VictoryScreen, KillStatsDialog, and StatsDialog.
// Three output formats:
//   BoxRow          — fits inside a ┌┐ summary card (DeathScreen/VictoryScreen)
//   DetailLine      — compact progress: "Sword  Adept  +4 dmg  [12/20]"
//   DetailExpanded  — with next-rank preview: "Sword  Adept  +4 dmg  (12/20 → Expert)"
public static class ProficiencyHelper
{
    // Returns box-formatted proficiency lines for summary cards.
    // Each line: "  │  Sword            Adept      +4  │"
    public static string BuildBoxRows(TurnManager tm, int colWidth = 37)
    {
        if (tm.WeaponKills.Count == 0) return "";

        string rows = "  |" + "".PadRight(colWidth) + "|\n" +
                       "  |     [ Weapon Proficiency ]" + "".PadRight(colWidth - 27) + "|\n";
        foreach (var (wpnType, _) in tm.WeaponKills)
        {
            var info = tm.GetProficiencyInfo(wpnType);
            int bonus = tm.GetProficiencyBonus(wpnType);
            rows += $"  |  {wpnType,-16} {info.Rank,-10} +{bonus,2}  |\n";
        }
        return rows;
    }

    // Returns detail-formatted proficiency line with progress tracking.
    // Example: "  Sword              Adept          +4 dmg  [12/20]"
    public static string BuildDetailLine(TurnManager tm, string wpnType)
    {
        var (rank, bonus, progress) = GetDetailCore(tm, wpnType);
        string progressText = progress ?? "MAX";
        return $"  {wpnType,-16} {rank,-14} +{bonus} dmg  [{progressText}]";
    }

    // Returns detail-formatted proficiency line with next-rank preview.
    // Example: "  Sword              Adept     L32/110  +16 dmg  [F1 Bloodfever]"
    public static string BuildDetailLineExpanded(TurnManager tm, string wpnType)
    {
        var (rank, bonus, _) = GetDetailCore(tm, wpnType);
        int lvl = tm.GetProficiencyLevel(wpnType);
        var picks = tm.GetForkChoices(wpnType);
        // Fork tags: F1..F4 with P=picked / · = unpicked / ? = PENDING (level met).
        var tags = new System.Text.StringBuilder();
        int[] forkLevels = { 25, 50, 75, 100 };
        for (int i = 0; i < 4; i++)
        {
            if (tags.Length > 0) tags.Append(' ');
            char state = picks[i] switch
            {
                1 => '1', 2 => '2', _ => (lvl >= forkLevels[i] ? '?' : '·'),
            };
            tags.Append($"F{i + 1}:{state}");
        }
        return $"  {wpnType,-18} {rank,-14} L{lvl,3}/{TurnManager.MaxProfLevel}  +{bonus} dmg  [{tags}]";
    }

    // Shared core for detail line formatting — fetches rank, bonus, and compact progress string.
    // Returns (rankName, bonusDamage, progressOrNull). Null progress means MAX rank reached.
    private static (string Rank, int Bonus, string? Progress) GetDetailCore(TurnManager tm, string wpnType)
    {
        var info = tm.GetProficiencyInfo(wpnType);
        int bonus = tm.GetProficiencyBonus(wpnType);
        string? progress = info.NextAt > 0 ? $"{info.Kills}/{info.NextAt}" : null;
        return (info.Rank, bonus, progress);
    }
}
