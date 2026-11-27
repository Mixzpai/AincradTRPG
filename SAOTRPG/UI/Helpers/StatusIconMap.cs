using SAOTRPG.Systems.Turns;

namespace SAOTRPG.UI.Helpers;

// The 3-4 letter status codes, and the one list of them.
//
// AllCodes is the source: the Player Guide's "Status Effect Abbreviations" topic generates its
// table from it, and HelpDialog's legend must name only codes listed here.
// Reserved-but-unused codes (BRN/FRZ/IAI/MSE/DVN) are intentionally omitted — they have no
// backing TurnManager state.
public static class StatusIconMap
{
    public readonly record struct StatusIcon(string Abbrev, Color Color, int Count);

    // Every code the tray can emit, with the player-facing description. Buff codes are listed
    // once as a family rather than ten times: they are per-stat and mechanically identical.
    public static readonly (string Code, string Meaning)[] AllCodes =
    {
        ("BLD",  "Bleed — damage each turn until it stops"),
        ("PSN",  "Poison — damage each turn until it wears off"),
        ("STN",  "Stun — you lose your turn"),
        ("SLW",  "Slow — halved dodge chance"),
        ("FTG",  "Fatigued — -2 ATK. Rest or sleep to clear it"),
        ("EXH",  "Exhausted — -4 ATK and -1 DEF. Rest or sleep to clear it"),
        ("SHRN", "Shrine blessing — bonus ATK and DEF"),
        ("SRG",  "Level-up surge — bonus ATK"),
        ("REGN", "Food regen — HP each turn"),
        ("INV",  "Invisible — most mobs cannot see you"),
        ("BAR",  "Barrier — damage it absorbs before your HP, not turns"),
        // Must stay short enough to render on one line: a table row that crosses the Guide's wrap
        // is re-flowed by WrapTo, whose tokeniser collapses the padding and breaks the column.
        ("ATK",  "Timed buff on that stat — also DEF, SPD, STR, VIT, END, DEX, AGI, INT, CRT"),
    };

    // Build the active icon list. Order: hard debuffs first, then buffs.
    // Count = remaining turns; 0 = duration-less indicator. `into` is cleared and filled when
    // given, so a caller asking every draw reuses one list.
    public static List<StatusIcon> Collect(TurnManager tm, List<StatusIcon>? into = null)
    {
        var icons = into ?? new List<StatusIcon>();
        icons.Clear();

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
        // Barrier is a damage pool, not a duration — Count is absorb remaining, not turns.
        if (tm.BarrierRemaining > 0)
            icons.Add(new("BAR", Color.BrightCyan, tm.BarrierRemaining));

        // Timed potion buffs, one row per stat, so the player can see each one and when it is
        // about to run out.
        var buffs = tm.ActiveBuffs;
        for (int i = 0; i < buffs.Count; i++)
            icons.Add(new(BuffCode(buffs[i].Stat), Color.BrightMagenta, buffs[i].TurnsLeft));

        // Fatigue. FatigueAtkPenalty and FatigueDefPenalty feed six combat damage sites — the
        // attack roll, sword skills and four defence formulas — so the tray must show when they
        // apply. Count 0 because this is a threshold rather than a countdown: it clears by
        // resting or sleeping, not by waiting.
        if (tm.FatigueAtkPenalty <= -4)
            icons.Add(new("EXH", Color.BrightRed, 0));
        else if (tm.FatigueAtkPenalty < 0)
            icons.Add(new("FTG", Color.BrightYellow, 0));

        return icons;
    }

    // Three-letter tray code for a buffed stat. Falls back to the first three letters of the
    // enum name so a new StatType shows something rather than silently rendering nothing.
    private static string BuffCode(Items.StatType stat) => stat switch
    {
        Items.StatType.Attack       => "ATK",
        Items.StatType.Defense      => "DEF",
        Items.StatType.Speed        => "SPD",
        Items.StatType.Strength     => "STR",
        Items.StatType.Vitality     => "VIT",
        Items.StatType.Endurance    => "END",
        Items.StatType.Dexterity    => "DEX",
        Items.StatType.Agility      => "AGI",
        Items.StatType.Intelligence => "INT",
        Items.StatType.CritRate     => "CRT",
        _ => stat.ToString().Length >= 3 ? stat.ToString()[..3].ToUpperInvariant()
                                         : stat.ToString().ToUpperInvariant(),
    };
}
