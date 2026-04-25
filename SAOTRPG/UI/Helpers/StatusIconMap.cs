using Terminal.Gui;
using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

// Bundle 13 (Item 9) — 3-4 letter abbreviations for the 8 currently-modeled
// status effects. REPLACES Bundle 11's single-glyph mapping.
// Reserved-but-unused codes (BRN/FRZ/IAI/MSE/DVN/BAR) intentionally omitted —
// they have no backing TurnManager state today.
public static class StatusIconMap
{
    public readonly record struct StatusIcon(string Abbrev, Color Color, int Count);

    // Build the active icon list. Order: hard debuffs first, then buffs.
    // Count = remaining turns; 0 = duration-less indicator.
    public static List<StatusIcon> Collect(TurnManager tm)
    {
        var icons = new List<StatusIcon>();

        // Debuffs
        if (tm.IsBleeding)
            icons.Add(new("BLD", Color.BrightRed, tm.BleedTurnsLeft));
        if (tm.IsPoisoned)
            icons.Add(new("PSN", Color.BrightGreen, tm.PoisonTurnsLeft));
        if (tm.StunTurnsLeft > 0)
            icons.Add(new("STN", Color.BrightYellow, tm.StunTurnsLeft));
        if (tm.SlowTurnsLeft > 0)
            icons.Add(new("SLW", Color.BrightCyan, tm.SlowTurnsLeft));

        // Buffs
        if (tm.ShrineBuffTurns > 0)
            icons.Add(new("SHRN", Color.BrightYellow, tm.ShrineBuffTurns));
        if (tm.LevelUpBuffTurns > 0)
            icons.Add(new("SRG", Color.BrightGreen, tm.LevelUpBuffTurns));
        if (tm.FoodRegenTurnsLeft > 0)
            icons.Add(new("REGN", Color.BrightGreen, tm.FoodRegenTurnsLeft));
        if (tm.IsInvisible)
            icons.Add(new("INV", Color.White, tm.InvisibilityTurnsLeft));

        return icons;
    }
}
