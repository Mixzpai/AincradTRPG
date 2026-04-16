using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAOTRPG.Systems;

// Global user settings — persisted to %LocalAppData%/AincradTRPG/settings.json.
// Separate from save slots (settings are shared across all playthroughs).
// Adding a new setting:
//   1. Add a property with a default value below
//   2. Add a UI control in OptionsScreen.cs
//   3. Wire it into the relevant system (TurnManager, MapView, etc.)
//   4. Call UserSettings.Save() when the value changes
public class UserSettings
{
    // ── Versioning ───────────────────────────────────────────────────
    public int SettingsVersion { get; set; } = 1;

    // ── Gameplay ─────────────────────────────────────────────────────
    // Automatically pick up items when stepping on them.
    public bool AutoPickup { get; set; } = false;

    // Text speed multiplier: 0 = Fast, 1 = Normal, 2 = Slow.
    public int TextSpeed { get; set; } = 1;

    // ── Display ──────────────────────────────────────────────────────
    // Show footstep trail markers behind the player.
    public bool ShowFootsteps { get; set; } = true;

    // Show screen flash effect when taking damage.
    public bool ShowDamageFlash { get; set; } = true;

    // ══════════════════════════════════════════════════════════════════
    //  PERSISTENCE — singleton + atomic JSON save/load
    // ══════════════════════════════════════════════════════════════════

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AincradTRPG");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Global singleton — loaded once at startup, saved on change.
    public static UserSettings Current { get; private set; } = new();

    // Load settings from disk, or create defaults if missing.
    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<UserSettings>(json, JsonOpts) ?? new();
            }
        }
        catch (Exception ex)
        {
            UI.DebugLogger.LogError("UserSettings.Load", ex);
            Current = new UserSettings();
        }
    }

    // Reset all settings to factory defaults and save to disk.
    public static void ResetToDefaults()
    {
        Current = new UserSettings();
        Save();
    }

    // Save current settings to disk (atomic write).
    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(Current, JsonOpts);
            string tmpPath = SettingsPath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, SettingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            UI.DebugLogger.LogError("UserSettings.Save", ex);
        }
    }
}
