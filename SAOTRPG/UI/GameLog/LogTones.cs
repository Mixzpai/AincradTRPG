using SAOTRPG.UI.Helpers;
using static SAOTRPG.UI.Helpers.CddaColors;

namespace SAOTRPG.UI.GameLog;

// Message types: what a line means, which is what its colour says.
public enum LogTone { Good, Bad, Warning, Info, Neutral }

public static class LogTones
{
    // The legend, declared once: the Player Guide renders it ({{LOG_TONES}}) and the message
    // log's filter help lists the names, which are also the filter's type prefixes.
    public static readonly (LogTone Tone, string Name, string Meaning)[] Key =
    {
        (LogTone.Good,    "good",    "your hits and kills, healing, level-ups, gains"),
        (LogTone.Bad,     "bad",     "harm to you: hits taken, hazards, poison, broken gear"),
        (LogTone.Warning, "warning", "a threat or a limit: a strong foe, a boss move, a cooldown"),
        (LogTone.Info,    "info",    "the game itself: floors, quests, weather, what NPCs say"),
        (LogTone.Neutral, "neutral", "everything else: misses, dodges, blocks, pickups"),
    };

    public static string NameOf(LogTone tone) => Key.First(k => k.Tone == tone).Name;

    // A filter prefix names a type exactly, case-sensitive.
    public static LogTone? Parse(string name)
    {
        foreach (var (tone, n, _) in Key)
            if (n == name) return tone;
        return null;
    }

    // A type's colour while new, then while slightly old; older lines are DarkGray. Methods, not
    // a table, so a theme switch is followed.
    public static Color ColorOf(LogTone tone, bool old = false) => ColorSchemes.Theme.World(tone switch
    {
        LogTone.Good    => old ? Green : LightGreen,
        LogTone.Bad     => old ? Red : LightRed,
        LogTone.Warning => old ? Brown : Yellow,
        LogTone.Info    => old ? Blue : LightBlue,
        _               => old ? Gray : White,
    });

    public static Color Old() => ColorSchemes.Theme.World(DarkGray);
    public static Color Border() => ColorSchemes.Theme.World(Gray);
    public static Color Time() => ColorSchemes.Theme.World(LightBlue);
    public static Color Bracket() => ColorSchemes.Theme.World(DarkGray);
    public static Color Help() => ColorSchemes.Theme.World(Cyan);
    public static Color Bright() => ColorSchemes.Theme.World(White);
    public static Color Arrow() => ColorSchemes.Theme.World(LightGreen);

    // Ordered phrase rules, first case-insensitive match wins, then the category's default. The
    // phrases are the game's own wording: your hits and kills good, hits on you bad, misses,
    // dodges, blocks and pickups neutral, speech info.
    private static readonly (string Phrase, LogTone Tone)[] Rules =
    {
        // A kill is a gain whatever else the line says; disarming names the trap.
        ("BOSS DEFEATED", LogTone.Good), ("slain!", LogTone.Good), ("defeated", LogTone.Good),
        ("finishes off", LogTone.Good), ("is destroyed", LogTone.Good), ("disarm", LogTone.Good),

        // Harm to you.
        ("damage. HP:", LogTone.Bad), ("hits you", LogTone.Bad), ("burns you", LogTone.Bad),
        ("sears you", LogTone.Bad), ("strikes for", LogTone.Bad), ("into your cuts", LogTone.Bad),
        ("devastating blow", LogTone.Bad), ("stuns you", LogTone.Bad), ("slows you", LogTone.Bad),
        ("poisons you", LogTone.Bad), ("You are slowed", LogTone.Bad), ("You are poisoned", LogTone.Bad),
        ("opens a wound", LogTone.Bad), ("HP from you", LogTone.Bad), ("through your armour", LogTone.Bad),
        ("Poison deals", LogTone.Bad), ("Bleed deals", LogTone.Bad), ("starving", LogTone.Bad),
        ("EXHAUSTED", LogTone.Bad), ("has broken", LogTone.Bad), ("burns out", LogTone.Bad),
        ("near death", LogTone.Bad), ("Death penalty", LogTone.Bad), ("FAILURE!", LogTone.Bad),
        ("trap", LogTone.Bad), ("lava", LogTone.Bad),

        // A threat coming, or a limit reached.
        ("levels above you", LogTone.Warning), ("guttering", LogTone.Warning), ("Fatigue", LogTone.Warning),
        ("on cooldown", LogTone.Warning), ("requires a", LogTone.Warning), ("too close", LogTone.Warning),
        ("No enemy in range", LogTone.Warning), ("!! ", LogTone.Warning), ("furious rage", LogTone.Warning),
        ("charges at you", LogTone.Warning), ("leaps through the air", LogTone.Warning),
        ("attacks from a distance", LogTone.Warning), ("Post-motion", LogTone.Warning),
        ("nothing is in range", LogTone.Warning), ("critically low", LogTone.Warning),

        // Your attacks landing, and gains. A broken streak is only news.
        ("Streak broken", LogTone.Neutral),
        ("You hit ", LogTone.Good), ("You strike again", LogTone.Good), ("from the shadows", LogTone.Good),
        ("You counter", LogTone.Good), ("Counterstrike", LogTone.Good), ("strike lands", LogTone.Good),
        ("Sword Skill:", LogTone.Good), (" hits ", LogTone.Good), ("Cleave!", LogTone.Good),
        ("Iaijutsu", LogTone.Good), ("Pair Resonance", LogTone.Good), ("*** ", LogTone.Good),
        ("streak", LogTone.Good), ("combo!", LogTone.Good), ("UNSTOPPABLE", LogTone.Good),
        ("Rampage", LogTone.Good), ("PERFECT!", LogTone.Good), ("Double Kill", LogTone.Good),
        ("Triple Kill", LogTone.Good), ("◈", LogTone.Good), ("LEVEL UP", LogTone.Good),
        ("restores", LogTone.Good), ("healed", LogTone.Good), ("drains", LogTone.Good), ("SUCCESS!", LogTone.Good),
        ("Purchased", LogTone.Good), ("Sold", LogTone.Good), ("Skill Unlocked", LogTone.Good),
        ("proficiency", LogTone.Good), ("Enhancement", LogTone.Good), ("complete", LogTone.Good),

        // The game's own notices.
        ("[QUEST]", LogTone.Info), ("New quest", LogTone.Info), ("[TIP]", LogTone.Info),
        ("Welcome to Floor", LogTone.Info), ("Ascending to", LogTone.Info), ("Weather:", LogTone.Info),
        ("Biome:", LogTone.Info),
    };

    public static LogTone Classify(string text, LogCategory category)
    {
        foreach (var (phrase, tone) in Rules)
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase)) return tone;
        // System notices and speech are info; everything else unmatched is neutral.
        return category is LogCategory.System or LogCategory.Dialog ? LogTone.Info : LogTone.Neutral;
    }
}
