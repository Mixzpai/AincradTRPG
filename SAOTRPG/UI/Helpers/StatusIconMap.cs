using Terminal.Gui;
using SAOTRPG.Systems;

namespace SAOTRPG.UI.Helpers;

// The 3-4 letter status codes, and the ONE list of them.
//
// Three surfaces used to hand-maintain their own copy — this file, HelpDialog's legend and the
// Player Guide's "Status Effect Abbreviations" topic — and all three had drifted: BAR shipped
// without reaching either of the other two, and the timed-buff rows added later reached neither.
// AllCodes is the source now; the Guide generates its table from it and ContentProbe asserts the
// Help legend never names a code that does not exist.
// Reserved-but-unused codes (BRN/FRZ/IAI/MSE/DVN) intentionally omitted — they have no backing
// TurnManager state today. BAR was on that list and should not have been: _barrierRemaining is
// refilled every floor from equipped Barrier+N effects and spent in ProcessMonsterTurn, and the
// Player Guide tells the player to build around it — it simply had no accessor to read.
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
        // Must stay short enough to render on ONE line: a table row that crosses the Guide's wrap
        // is re-flowed by WrapTo, whose tokeniser collapses the padding and breaks the column.
        ("ATK",  "Timed buff on that stat — also DEF, SPD, STR, VIT, END, DEX, AGI, INT, CRT"),
    };

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
        // Barrier is a damage POOL, not a duration — Count is absorb remaining, not turns.
        if (tm.BarrierRemaining > 0)
            icons.Add(new("BAR", Color.BrightCyan, tm.BarrierRemaining));

        // Timed potion buffs, one row per stat. A buff the player cannot see is the same defect
        // Barrier had: live, logged, and invisible — and here it also has to be visible for the
        // player to know when it is about to run out.
        foreach (var buff in tm.ActiveBuffs)
            icons.Add(new(BuffCode(buff.Stat), Color.BrightMagenta, buff.TurnsLeft));

        // Fatigue. FatigueAtkPenalty and FatigueDefPenalty feed SIX combat damage sites — the
        // attack roll, sword skills and four defence formulas — so a tired player was taking
        // -4 ATK and -1 DEF with nothing on screen to explain it. Count 0 because this is a
        // THRESHOLD rather than a countdown: it clears by resting or sleeping, not by waiting.
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
