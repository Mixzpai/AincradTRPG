using Terminal.Gui;
using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

// Bundle 11 — compact single-glyph status-effect icons + countdowns.
// Pulls live state from TurnManager and yields a list of (glyph, color, count)
// triples for the icon-row widget that lives near the HP bar.
public static class StatusIconMap
{
    public readonly record struct StatusIcon(char Glyph, Color Color, int Count);

    // Build the active icon list. Order: hard debuffs first (so eye lands on
    // them), then buffs. Count = remaining turns; 0 = duration-less indicator.
    public static List<StatusIcon> Collect(TurnManager tm)
    {
        var icons = new List<StatusIcon>();

        // ── Debuffs ──────────────────────────────────────────────────
        // Bleed = lowercase 'b' red — distinct from Burn 'B' which we don't
        // currently model but reserve the slot for future poison/burn split.
        if (tm.IsBleeding)
            icons.Add(new('b', Color.BrightRed, tm.BleedTurnsLeft));
        if (tm.IsPoisoned)
            icons.Add(new('P', Color.BrightGreen, tm.PoisonTurnsLeft));
        if (tm.StunTurnsLeft > 0)
            icons.Add(new('S', Color.BrightYellow, tm.StunTurnsLeft));
        if (tm.SlowTurnsLeft > 0)
            icons.Add(new('s', Color.Cyan, tm.SlowTurnsLeft));

        // ── Buffs ────────────────────────────────────────────────────
        if (tm.ShrineBuffTurns > 0)
            icons.Add(new('B', Color.BrightYellow, tm.ShrineBuffTurns));
        if (tm.LevelUpBuffTurns > 0)
            icons.Add(new('R', Color.BrightGreen, tm.LevelUpBuffTurns));

        return icons;
    }
}
