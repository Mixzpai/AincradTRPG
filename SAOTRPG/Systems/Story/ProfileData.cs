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

    // Set on first F100 clear. Gates Run Modifiers UI.
    public static bool HasCompletedGame { get; private set; }

    // ── Player Guide persistence ─────────
    // Keys "{Category}|{Title}" for known-topics; category names for expanded-state.

    // Gameplay-unlocked topics. Lifts the spoiler ??? mask via tag-match.
    public static HashSet<string> GuideKnownTopics { get; private set; } = new();

    // Sidebar categories the user has chosen to expand. Persisted across
    // sessions so the guide reopens to the same shape.
    public static HashSet<string> GuideExpandedCategories { get; private set; } = new();

    private static bool _loaded;

    // A read that failed is not the same as no file at all, and the difference decides whether the
    // next write is allowed. A missing profile is a fresh install and must save normally; a file
    // that exists and could not be read is corrupt or transiently locked, and overwriting it turns
    // a recoverable problem into a permanent one -- HasCompletedGame gates a whole feature and
    // there is no second copy of it anywhere. Save refuses while this is set, and says so.
    private static bool _loadFailed;

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
        catch (Exception ex)
        {
            // Disclosed, not swallowed: the game carries on from defaults, but a profile that
            // exists and would not parse is said out loud rather than looking like a fresh start.
            _loadFailed = true;
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogError("ProfileData.EnsureLoaded", ex);
        }
    }

    // Re-read the profile from disk, discarding whatever is in memory. Read by the verification
    // probes, so a round trip goes through this class's own load rather than a copy of it.
    public static void ReloadFromDisk()
    {
        _loaded = false;
        _loadFailed = false;
        EverSeenEvents = new HashSet<string>();
        HasCompletedGame = false;
        GuideKnownTopics = new HashSet<string>();
        GuideExpandedCategories = new HashSet<string>();
        EnsureLoaded();
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

    // Written through a temporary file and renamed into place, the same shape UserSettings,
    // Keybinds and SaveManager use. A plain WriteAllText truncates first, so a crash between the
    // truncate and the write leaves a partial file that will not parse -- and the loader's
    // fallback is to start fresh, which would destroy HasCompletedGame and every guide unlock.
    //
    // The rename form is slower than a direct write, which is affordable because the write count
    // is bounded: Save runs only when a collection gains a member, so it is one write per newly
    // discovered topic, capped by the roster, not per turn. Writes are not deferred: a deferred
    // write that never flushes would lose progress silently.
    private static void Save()
    {
        if (_loadFailed)
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogError("ProfileData.Save", new InvalidOperationException(
                "refusing to overwrite a profile that could not be read — the file on disk is "
                + "left intact so it can still be recovered"));
            return;
        }
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
            string tmp = ProfilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(dto));
            File.Move(tmp, ProfilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogError("ProfileData.Save", ex);
        }
    }

    private class ProfileDto
    {
        public List<string> EverSeenEvents { get; set; } = new();
        public bool HasCompletedGame { get; set; }

        // Player Guide persistence. Lists (not HashSets) so System.Text.Json
        // reflection round-trips cleanly without needing a source-gen context.
        // The deserializer ignores unknown properties, so a profile carrying
        // extra fields still loads cleanly.
        public List<string> GuideKnownTopics { get; set; } = new();
        public List<string> GuideExpandedCategories { get; set; } = new();
    }
}
