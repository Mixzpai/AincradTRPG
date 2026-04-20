namespace SAOTRPG.Systems.Story;

// Discrete story-state booleans set by player choices in cutscenes.
// Events gate their availability on these + floor + faction reputation.
public enum StoryFlag
{
    OwnedBeaterLabel,    // F1: accepted the Beater title publicly
    DeniedBeaterLabel,   // F1: rejected Kibaou's accusation
    MetKizmel,           // F3: bonded with the Dark Elf knight
    MournedFallen,       // F25: paid respects at the midpoint memorial
    SteeledResolve,      // F25: turned grief into drive
    KnowsLaughingCoffin, // F50: learned of the PK guild
    BlackSwordsman,      // F67: earned the title after 100 kills
    KnowsHeathcliff,     // F75: deduced Kayaba's identity
    AcceptedDuel,        // F75: agreed to Kayaba's final wager
    RefusedDuel,         // F75: refused — triggers alternate F100 path
}

// Faction/guild IDs used by StorySystem rep + FB-063 GuildSystem. None = no guild.
public enum Faction
{
    None,
    KnightsOfBlood,
    AincradLiberationForce,
    DivineDragonAlliance,
    Fuurinkazan,
    LegendBraves,
    SleepingKnights,
    LaughingCoffin,
    MoonlitBlackCats,
    PlayerGuild,
}

public enum StoryTrigger
{
    GameStart,     // Once on a new run (F1 prologue)
    FloorEntry,    // Each time AscendFloor lands on a new floor
    BossDefeat,    // A Boss has just died
    KillCount,     // Total player kills hit a threshold
}
