using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAOTRPG.Systems;

// Combat log breakdown mode — cycled via OptionsScreen, consumed in
// TurnManager.Combat.FormatHitLog. Order matches OptionsScreen radio.
public enum DamageBreakdownMode { Off = 0, Concise = 1, Medium = 2, Verbose = 3 }

// FB-450 particle density. Off=disabled; Pronounced=default (5-10 particles, 800ms).
public enum ParticleDensity { Off = 0, Subtle = 1, Moderate = 2, Pronounced = 3 }

// FB-452 damage tag position in log lines. Prefix = default, reads best dense.
public enum DamageTagPosition { Prefix = 0, Suffix = 1, Inline = 2 }

// FB-452 damage tag bracket style. Brackets = default, Bare strips, Chip wraps ◆.
public enum DamageTagStyle { Brackets = 0, Bare = 1, Chip = 2 }

// Bundle 13 (Item 8) — footstep trail glyph style. Off = trail disabled.
public enum FootstepStyle { Off = 0, Dots = 1, Dashes = 2, Paws = 3, Bootprints = 4, Chevrons = 5 }

// Bundle 13 (Item 8) — footstep trail opacity (color tier). Subtle = DarkGray,
// Medium = Gray, Bold = White. Drives RenderFootstepTrail attribute.
public enum FootstepOpacity { Subtle = 0, Medium = 1, Bold = 2 }

// Global settings at %LocalAppData%/AincradTRPG/settings.json (not per-save).
// Add: property + OptionsScreen control + system wiring + Save() on change.
public class UserSettings
{
    // ── Versioning ───────────────────────────────────────────────────
    public int SettingsVersion { get; set; } = 2;

    // ── Gameplay ─────────────────────────────────────────────────────
    // Automatically pick up items when stepping on them.
    public bool AutoPickup { get; set; } = false;

    // Text speed multiplier: 0 = Fast, 1 = Normal, 2 = Slow.
    public int TextSpeed { get; set; } = 1;

    // ── Display ──────────────────────────────────────────────────────
    // Bundle 13 (Item 8) — footstep glyph style. Off = no trail.
    public FootstepStyle FootstepStyle { get; set; } = FootstepStyle.Dots;

    // Bundle 13 (Item 8) — trail length (turns). 0 = off; 1000 = "unlimited" hard ceiling per Q17.
    public int FootstepLength { get; set; } = 10;

    // Bundle 13 (Item 8) — opacity tier. Drives Subtle/Medium/Bold gray ramp.
    public FootstepOpacity FootstepOpacity { get; set; } = FootstepOpacity.Subtle;

    // Backing-compat alias for legacy callers — derived from FootstepStyle.
    // Setter routes to FootstepStyle (Off when false, Dots when true) so old saves
    // that wrote {"ShowFootsteps":true} still hydrate sensibly.
    [System.Text.Json.Serialization.JsonInclude]
    public bool ShowFootsteps
    {
        get => FootstepStyle != FootstepStyle.Off;
        set { if (!value) FootstepStyle = FootstepStyle.Off;
              else if (FootstepStyle == FootstepStyle.Off) FootstepStyle = FootstepStyle.Dots; }
    }

    // Show screen flash effect when taking damage.
    public bool ShowDamageFlash { get; set; } = true;

    // ── Accessibility ────────────────────────────────────────────────
    // Brief viewport jitter on crits / heavy hits / quakes. Disable if motion sensitive.
    public bool ScreenShakeEnabled { get; set; } = true;

    // Combat-log breakdown verbosity for player-initiated hits.
    public DamageBreakdownMode DamageBreakdownMode { get; set; } = DamageBreakdownMode.Off;

    // FB-450 particle density. Default Pronounced (5-10 particles @ 800ms).
    public ParticleDensity ParticleDensity { get; set; } = ParticleDensity.Pronounced;

    // FB-452 damage tag position — Prefix reads best in dense combat logs.
    public DamageTagPosition DamageTagPosition { get; set; } = DamageTagPosition.Prefix;

    // FB-452 damage tag bracket style — default [BRACKETS] for visual anchor.
    public DamageTagStyle DamageTagStyle { get; set; } = DamageTagStyle.Brackets;

    // Bundle 11 — fall back to ASCII stat bars if the terminal font renders
    // eighth-block unicode (█▉▊▋▌▍▎▏) incorrectly. Default false → unicode.
    public bool UseAsciiStatBars { get; set; } = false;

    // Player Guide — fall back to ASCII disclosure glyphs ('>' / 'v') instead
    // of '▸' / '▾' when the terminal font lacks the geometric arrow code points.
    // Default false → unicode.
    public bool UseAsciiDisclosureGlyphs { get; set; } = false;

    // Persistence: singleton + atomic JSON save/load.

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
