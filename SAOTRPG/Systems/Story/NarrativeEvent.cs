using SAOTRPG.Entities;

namespace SAOTRPG.Systems.Story;

// Context snapshot passed to every event predicate + builder.
// Keeps NarrativeEvent lambdas pure and easy to author.
public record StoryContext(
    int Floor,
    int KillCount,
    Player Player,
    Monster? SourceMonster = null
);

// One scripted story event. Trigger decides when StorySystem checks it;
// CanFire filters on context + flag state; Build produces the cutscene.
public record NarrativeEvent(
    string Id,
    StoryTrigger Trigger,
    Func<StoryContext, bool> CanFire,
    Func<StoryContext, CutsceneScript> Build
);
