using System.Text.Json;

namespace SAOTRPG.Systems.Story;

// Permanent per-installation data that survives new-game resets.
// Currently: ever-seen event IDs so repeat runs can fast-forward cutscenes
// the player has already read. Stored at %LocalAppData%/AincradTRPG/profile.json.
public static class ProfileData
{
    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AincradTRPG");
    private static readonly string ProfilePath = Path.Combine(ProfileDir, "profile.json");

    public static HashSet<string> EverSeenEvents { get; private set; } = new();

    // Set on first F100 clear. Gates Run Modifiers UI (FB-564).
    public static bool HasCompletedGame { get; private set; }

    // ── Player Guide persistence ──────────────────────────────────────
    // Keys are "{Category}|{Title}". Lists round-trip cleanly through
    // System.Text.Json; runtime uses the ordered List<string> for Bookmarks
    // and RecentlyViewed (order matters — newest-first recency, user-ordered
    // bookmarks) and HashSet<string> for the gating sets.

    // User-pinned topics (unlimited).
    public static List<string> GuideBookmarks { get; private set; } = new();

    // Most-recently-viewed topics, oldest at index 0, newest at end.
    // Capped at 10 with oldest-out eviction.
    public static List<string> GuideRecentlyViewed { get; private set; } = new();

    // Every topic the player has ever opened. Used for first-visit
    // "unread" marker decoration.
    public static HashSet<string> GuideVisitedTopics { get; private set; } = new();

    // Topics unlocked via gameplay (boss kills, area entry, etc.). Populated
    // at session start + on discovery events, and used to mask unseen
    // entries as "???" via the TreeView AspectGetter.
    public static HashSet<string> GuideKnownTopics { get; private set; } = new();

    private const int RecentlyViewedCap = 10;

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
            if (dto?.GuideBookmarks != null)
                GuideBookmarks = new List<string>(dto.GuideBookmarks);
            if (dto?.GuideRecentlyViewed != null)
                GuideRecentlyViewed = new List<string>(dto.GuideRecentlyViewed);
            if (dto?.GuideVisitedTopics != null)
                GuideVisitedTopics = new HashSet<string>(dto.GuideVisitedTopics);
            if (dto?.GuideKnownTopics != null)
                GuideKnownTopics = new HashSet<string>(dto.GuideKnownTopics);
        }
        catch { /* corrupt profile — start fresh rather than crash the game */ }
    }

    // ── Guide helpers ─────────────────────────────────────────────────

    // Toggle bookmark state. Returns the new state (true = now bookmarked).
    public static bool ToggleBookmark(string topicKey)
    {
        EnsureLoaded();
        if (GuideBookmarks.Remove(topicKey)) { Save(); return false; }
        GuideBookmarks.Add(topicKey);
        Save();
        return true;
    }

    public static bool IsBookmarked(string topicKey)
    {
        EnsureLoaded();
        return GuideBookmarks.Contains(topicKey);
    }

    // Record a topic view. Moves existing entries to the end (newest) and
    // evicts oldest when the list exceeds RecentlyViewedCap.
    public static void MarkRecentlyViewed(string topicKey)
    {
        EnsureLoaded();
        GuideRecentlyViewed.Remove(topicKey);
        GuideRecentlyViewed.Add(topicKey);
        while (GuideRecentlyViewed.Count > RecentlyViewedCap)
            GuideRecentlyViewed.RemoveAt(0);
        GuideVisitedTopics.Add(topicKey);
        Save();
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
                GuideBookmarks = new List<string>(GuideBookmarks),
                GuideRecentlyViewed = new List<string>(GuideRecentlyViewed),
                GuideVisitedTopics = GuideVisitedTopics.ToList(),
                GuideKnownTopics = GuideKnownTopics.ToList(),
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
        public List<string> GuideBookmarks { get; set; } = new();
        public List<string> GuideRecentlyViewed { get; set; } = new();
        public List<string> GuideVisitedTopics { get; set; } = new();
        public List<string> GuideKnownTopics { get; set; } = new();
    }
}
