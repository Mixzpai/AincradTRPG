using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

// Builds a compact status effect tag string for the HUD action bar.
// Each status effect has a short ASCII prefix for quick visual identification.
public static class StatusTagBuilder
{
    private const int CombatRecencyWindow = 2;

    // Builds the HUD status tag string from current game state.
    // Returns empty string when no effects are active.
    // Example output: "  [!PSN:3|!BLD:2|xSTK:5]  [!WPN:5]"
    public static string Build(TurnManager tm, string durWarnings = "")
    {
        var tags = new List<string>();

        if (tm.PlayerLowHp)             tags.Add("!!LOW");

        if (tm.IsPoisoned)              tags.Add($"!PSN:{tm.PoisonTurnsLeft}");
        if (tm.IsBleeding)              tags.Add($"!BLD:{tm.BleedTurnsLeft}");
        if (tm.StunTurnsLeft > 0)       tags.Add($"!STN:{tm.StunTurnsLeft}");
        if (tm.SlowTurnsLeft > 0)       tags.Add($"vSLW:{tm.SlowTurnsLeft}");
        if (tm.Satiety <= 15)           tags.Add("!STARVING");
        else if (tm.Satiety < 30)       tags.Add($"~HGR:{tm.Satiety}");

        if (tm.RestCounter >= 250)      tags.Add("!EXH");
        else if (tm.RestCounter >= 150) tags.Add("~FTG");

        if (tm.Satiety >= 80)           tags.Add("+FED");
        if (tm.ShrineBuffTurns > 0)     tags.Add($"+BLS:{tm.ShrineBuffTurns}");
        if (tm.IsStealthed)             tags.Add("~STL");
        if (tm.LevelUpBuffTurns > 0)    tags.Add($"+SRG:{tm.LevelUpBuffTurns}");

        if (tm.BountyComplete)          tags.Add("BTY:OK");
        else if (tm.BountyTarget != null) tags.Add($"BTY:{tm.BountyKillsCurrent}/{tm.BountyKillsNeeded}");

        if (tm.KillStreak >= 2)         tags.Add($"xSTK:{tm.KillStreak}");
        if (tm.DodgeStreak >= 2)        tags.Add($"xDGE:{tm.DodgeStreak}");

        if (tm.TurnCount - tm.LastCombatTurn <= CombatRecencyWindow && tm.LastCombatTurn > 0)
            tags.Add("!CMB");

        if (tm.Satiety >= 30 && !tm.IsPoisoned && !tm.IsBleeding)
            tags.Add("+RGN");

        string result = "";
        if (tags.Count > 0)
            result = "  [" + string.Join("|", tags) + "]";

        return result + durWarnings;
    }
}
