using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

/// <summary>
/// Builds a compact status effect tag string for the HUD action bar.
/// Each status effect has a Unicode icon prefix for quick visual identification.
///
/// Icons:
///   ☠ Poison  ♦ Bleed  ✧ Hungry  ◈ Shield/Buff  ♦ Stealth  ✦ Surge  ♥ Regen
/// </summary>
public static class StatusTagBuilder
{
    public static string Build(TurnManager tm, string durWarnings = "")
    {
        var tags = new List<string>();

        // Negative effects — skull, diamond, star
        if (tm.IsPoisoned)              tags.Add($"☠PSN:{tm.PoisonTurnsLeft}");
        if (tm.IsBleeding)              tags.Add($"♦BLD:{tm.BleedTurnsLeft}");
        if (tm.Satiety < 30)            tags.Add($"✧HGR:{tm.Satiety}");

        // Positive effects — shield, eye, lightning
        if (tm.ShrineBuffTurns > 0)     tags.Add($"◈BLS:{tm.ShrineBuffTurns}");
        if (tm.IsStealthed)             tags.Add("◌STL");
        if (tm.LevelUpBuffTurns > 0)    tags.Add($"✦SRG:{tm.LevelUpBuffTurns}");

        // Regen indicator — heart
        if (tm.Satiety >= 30 && !tm.IsPoisoned && !tm.IsBleeding)
            tags.Add("♥RGN");

        string result = "";
        if (tags.Count > 0)
            result = "  [" + string.Join("|", tags) + "]";

        return result + durWarnings;
    }
}
