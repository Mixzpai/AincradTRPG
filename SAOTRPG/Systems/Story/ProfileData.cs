using System.Text.Json;

namespace SAOTRPG.Systems.Story;

// Per-installation data (survives new-game). Ever-seen events for cutscene
// fast-forward. Stored at %LocalAppData%/AincradTRPG/profile.json.
public static class ProfileData
{
    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AincradTRPG");
    private static readonly string ProfilePath = Path.Combine(ProfileDir, "profile.json");

    public static HashSet<string> EverSeenEvents { get; private set; } = new();

    // Set on first F100 clear. Gates Run Modifiers UI (FB-564).
    public static bool HasCompletedGame { get; private set; }

    // ── Player Guide persistence ─────────
    // Keys "{Category}|{Title}" for known-topics; category names for expanded-state.

    // Gameplay-unlocked topics. Lifts the spoiler ??? mask via tag-match.
    public static HashSet<string> GuideKnownTopics { get; private set; } = new();

    // Sidebar categories the user has chosen to expand. Persisted across
    // sessions so the guide reopens to the same shape.
    public static HashSet<string> GuideExpandedCategories { get; private set; } = new();

    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            if (!File.Exists(ProfilePath)) return;
            var json = File.ReadAllText(ProfilePath);
            var dto = JsonSerializer.Deserialize<ProfileDto>(json);
            if (dto?.EverSeenEvents != null)
                EverSeenEvents = new HashSet<string>(dto.EverSeenEvents);
            HasCompletedGame = dto?.HasCompletedGame ?? false;
            if (dto?.GuideKnownTopics != null)
                GuideKnownTopics = new HashSet<string>(dto.GuideKnownTopics);
            if (dto?.GuideExpandedCategories != null)
                GuideExpandedCategories = new HashSet<string>(dto.GuideExpandedCategories);
        }
        catch { /* corrupt profile — start fresh rather than crash the game */ }
    }

    // ── Guide helpers ─────────────────────────────────────────────────

    // Persist sidebar expand-state. Idempotent.
    public static void SetCategoryExpanded(string category, bool expanded)
    {
        EnsureLoaded();
        bool changed = expanded
            ? GuideExpandedCategories.Add(category)
            : GuideExpandedCategories.Remove(category);
        if (changed) Save();
    }

    public static void MarkKnown(string topicKey)
    {
        EnsureLoaded();
        if (!GuideKnownTopics.Add(topicKey)) return;
        Save();
    }

    // Called on F100 clear. Permanently unlocks post-clear features.
    public static void MarkGameCompleted()
    {
        EnsureLoaded();
        if (HasCompletedGame) return;
        HasCompletedGame = true;
        Save();
    }

    public static bool WasSeenBefore(string eventId)
    {
        EnsureLoaded();
        return EverSeenEvents.Contains(eventId);
    }

    public static void MarkSeen(string eventId)
    {
        EnsureLoaded();
        if (!EverSeenEvents.Add(eventId)) return;
        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(ProfileDir);
            var dto = new ProfileDto
            {
                EverSeenEvents = EverSeenEvents.ToList(),
                HasCompletedGame = HasCompletedGame,
                GuideKnownTopics = GuideKnownTopics.ToList(),
                GuideExpandedCategories = GuideExpandedCategories.ToList(),
            };
            File.WriteAllText(ProfilePath, JsonSerializer.Serialize(dto));
        }
        catch { /* disk errors non-fatal — profile is a convenience, not a requirement */ }
    }

    private class ProfileDto
    {
        public List<string> EverSeenEvents { get; set; } = new();
        public bool HasCompletedGame { get; set; }

        // Player Guide persistence. Lists (not HashSets) so System.Text.Json
        // reflection round-trips cleanly without needing a source-gen context.
        // Legacy fields (GuideBookmarks, GuideRecentlyViewed, GuideVisitedTopics)
        // were dropped in the guide rewrite; the deserializer ignores unknown
        // properties so old profiles load cleanly.
        public List<string> GuideKnownTopics { get; set; } = new();
        public List<string> GuideExpandedCategories { get; set; } = new();
    }
}
