
using SAOTRPG.Systems.Turns;

namespace SAOTRPG.UI.Helpers;

// Writes a compact status effect tag group for the HUD action bar.
// Each status effect has a short ASCII prefix for quick visual identification.
public static class StatusTagBuilder
{
    private const int CombatRecencyWindow = 2;

    // The HUD status tags from game state, nothing when no effect is active.
    // Example: "  [!PSN:3|!BLD:2|xSTK:5]"
    public static void Write(ref LineWriter w, TurnManager tm)
    {
        int start = w.Length, n = 0;
        w.Append("  [");

        if (tm.PlayerLowHp)             Tag(ref w, ref n, "!!LOW");

        if (tm.IsPoisoned)              Tag(ref w, ref n, "!PSN:", tm.PoisonTurnsLeft);
        if (tm.IsBleeding)              Tag(ref w, ref n, "!BLD:", tm.BleedTurnsLeft);
        if (tm.StunTurnsLeft > 0)       Tag(ref w, ref n, "!STN:", tm.StunTurnsLeft);
        if (tm.SlowTurnsLeft > 0)       Tag(ref w, ref n, "vSLW:", tm.SlowTurnsLeft);
        if (tm.Satiety <= TurnManager.StarvingThreshold) Tag(ref w, ref n, "!STARVING");
        else if (tm.Satiety < TurnManager.HungerRegenThreshold) Tag(ref w, ref n, "~HGR:", tm.Satiety);

        if (tm.RestCounter >= TurnManager.ExhaustionThreshold) Tag(ref w, ref n, "!EXH");
        else if (tm.RestCounter >= TurnManager.FatigueThreshold) Tag(ref w, ref n, "~FTG");

        if (tm.Satiety >= TurnManager.WellFedThreshold) Tag(ref w, ref n, "+FED");
        if (tm.ShrineBuffTurns > 0)     Tag(ref w, ref n, "+BLS:", tm.ShrineBuffTurns);
        if (tm.IsStealthed)             Tag(ref w, ref n, "~STL");
        if (tm.LevelUpBuffTurns > 0)    Tag(ref w, ref n, "+SRG:", tm.LevelUpBuffTurns);

        if (tm.BountyComplete)          Tag(ref w, ref n, "BTY:OK");
        else if (tm.BountyTarget != null)
        {
            Tag(ref w, ref n, "BTY:", tm.BountyKillsCurrent);
            w.Append('/');
            w.Append(tm.BountyKillsNeeded);
        }

        if (tm.KillStreak >= 2)         Tag(ref w, ref n, "xSTK:", tm.KillStreak);
        if (tm.DodgeStreak >= 2)        Tag(ref w, ref n, "xDGE:", tm.DodgeStreak);

        if (tm.TurnCount - tm.LastCombatTurn <= CombatRecencyWindow && tm.LastCombatTurn > 0)
            Tag(ref w, ref n, "!CMB");

        if (tm.Satiety >= TurnManager.HungerRegenThreshold && !tm.IsPoisoned && !tm.IsBleeding)
            Tag(ref w, ref n, "+RGN");

        if (n == 0) w.Truncate(start);
        else w.Append(']');
    }

    // One tag, '|' before every tag but the first.
    private static void Tag(ref LineWriter w, ref int n, string text)
    {
        if (n++ > 0) w.Append('|');
        w.Append(text);
    }

    private static void Tag(ref LineWriter w, ref int n, string text, int value)
    {
        Tag(ref w, ref n, text);
        w.Append(value);
    }
}
