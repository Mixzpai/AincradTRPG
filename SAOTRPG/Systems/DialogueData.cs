using SAOTRPG.Entities;
using SAOTRPG.Systems.Story;

namespace SAOTRPG.Systems;

// Data structures for NPC branching dialogue.
// A dialogue is a sequence of lines, each optionally having player choices.

// What a line or a choice needs to be true before the player is shown it. Every field is
// optional and they AND together, so `new DialogueCondition(MinFloorReached: 20)` reads as the
// whole requirement rather than as one clause of a form the author has to complete.
//
// The conditions map onto state that already exists — karma, faction reputation, story flags and
// lifetime floor progress — deliberately: a gate on a concept the game does not model would be a
// promise nothing can keep, and this project has spent a long time removing exactly that shape.
public sealed record DialogueCondition(
    int? MinFloorReached = null,
    int? KarmaAtLeast = null,
    int? KarmaAtMost = null,
    Faction? Faction = null,
    int? MinReputation = null,
    StoryFlag? RequiresFlag = null,
    StoryFlag? ForbidsFlag = null)
{
    public bool IsMet(Player player)
    {
        if (MinFloorReached is int f && LifetimeStats.Load().MaxFloorReached < f) return false;
        if (KarmaAtLeast is int lo && player.Karma < lo) return false;
        if (KarmaAtMost is int hi && player.Karma > hi) return false;
        if (Faction is Faction fac && MinReputation is int rep
            && StorySystem.Reputation.GetValueOrDefault(fac) < rep) return false;
        if (RequiresFlag is StoryFlag need && !StorySystem.Flags.Contains(need)) return false;
        if (ForbidsFlag is StoryFlag no && StorySystem.Flags.Contains(no)) return false;
        return true;
    }
}

// A single page of NPC dialogue with optional player choices.
// A line whose Condition is unmet is skipped entirely; a choice whose Condition is unmet is not
// offered. A line that declares choices and has none left after filtering still renders — it
// simply falls through to Continue, which is why the author is not required to leave a fallback.
public record DialogueLine(string Text, DialogueChoice[]? Choices = null,
                           DialogueCondition? Condition = null);

// A player response option in a dialogue.
public record DialogueChoice(string Label, string Response, DialogueCondition? Condition = null);
