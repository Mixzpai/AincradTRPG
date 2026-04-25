using SAOTRPG.Entities;

namespace SAOTRPG.Systems.Story;

// Scripted story events registry + dispatcher. Static state (matches
// Achievements/TutorialSystem SaveData pattern).
public static class StorySystem
{
    public static HashSet<string> FiredEventIds { get; set; } = new();
    public static HashSet<StoryFlag> Flags { get; set; } = new();
    public static Dictionary<Faction, int> Reputation { get; set; } = new();

    // GameScreen wires this on init so StorySystem.Fire can show a modal
    // cutscene synchronously. Null during headless tests or before UI init.
    public static Action<CutsceneScript>? Handler { get; set; }

    private static readonly List<NarrativeEvent> _events = new();
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;
        StoryEvents.RegisterAll(_events);
    }

    // Wipe run-state (called on new game). Permanent profile data is NOT cleared.
    public static void Reset()
    {
        FiredEventIds.Clear();
        Flags.Clear();
        Reputation.Clear();
    }

    // F9 hot-reload latch reset — drops "floor:"-prefixed fired ids so regenerated NPCs
    // re-trigger first-meet lines. Stub filter pending a proper floor-scoped id convention.
    public static void ClearPerFloorTriggers()
    {
        int before = FiredEventIds.Count;
        FiredEventIds.RemoveWhere(id => id.StartsWith("floor:"));
        UI.DebugLogger.LogGame("RELOAD", $"StorySystem.ClearPerFloorTriggers removed {before - FiredEventIds.Count}");
    }

    public static void SetFlag(StoryFlag f) => Flags.Add(f);
    public static bool HasFlag(StoryFlag f) => Flags.Contains(f);
    public static void AdjustRep(Faction f, int delta)
        => Reputation[f] = Reputation.GetValueOrDefault(f) + delta;
    public static int GetRep(Faction f) => Reputation.GetValueOrDefault(f);

    public static void TryFire(StoryTrigger trigger, StoryContext ctx)
    {
        EnsureRegistered();
        foreach (var e in _events)
        {
            if (e.Trigger != trigger) continue;
            if (FiredEventIds.Contains(e.Id)) continue;
            if (!e.CanFire(ctx)) continue;
            FireEvent(e, ctx);
            return; // Only one event per trigger per check.
        }
    }

    private static void FireEvent(NarrativeEvent e, StoryContext ctx)
    {
        FiredEventIds.Add(e.Id);
        bool isReplay = ProfileData.WasSeenBefore(e.Id);
        ProfileData.MarkSeen(e.Id);
        var script = e.Build(ctx) with { IsReplay = isReplay };
        Handler?.Invoke(script);
    }
}
