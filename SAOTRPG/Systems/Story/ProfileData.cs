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
        }
        catch { /* corrupt profile — start fresh rather than crash the game */ }
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
            };
            File.WriteAllText(ProfilePath, JsonSerializer.Serialize(dto));
        }
        catch { /* disk errors non-fatal — profile is a convenience, not a requirement */ }
    }

    private class ProfileDto
    {
        public List<string> EverSeenEvents { get; set; } = new();
        public bool HasCompletedGame { get; set; }
    }
}
