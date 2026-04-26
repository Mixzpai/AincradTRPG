using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI;

// Dramatic 3-second non-modal overlay; MapView.RenderDivineObtainBanner polls IsActive each frame.
// Input is never captured — the player can keep fighting while the banner fades.
public static class DivineObtainBanner
{
    // Default hold for 3 seconds; fade in/out absorbed into the total.
    public const int DefaultDurationMs = 3000;
    private const int FadeInMs  = 300;
    private const int FadeOutMs = 500;

    // Compact banner sizing — mirrors FloorTransitionOverlay scaled down (5 rows, 50 cols).
    public const int BannerWidth  = 50;
    public const int BannerHeight = 5;

    private static long _activatedAtMs;
    private static int  _durationMs;
    private static string _weaponName = "";
    private static string _rarityLabel = "";
    // Bundle 9: Awakening variant state. Set by TriggerAwakening; Trigger clears it.
    private static bool _isAwakening;
    private static int  _awakeningLevel;

    // True while the banner still has time remaining on the frame clock.
    public static bool IsActive { get; private set; }

    // Normalized elapsed time (0..1). Used by MapView to fade alpha in+out.
    public static float ElapsedNormalized
    {
        get
        {
            if (!IsActive || _durationMs <= 0) return 0f;
            long now = SAOTRPG.Systems.FrameClock.ElapsedMs;
            float t = (now - _activatedAtMs) / (float)_durationMs;
            return System.Math.Clamp(t, 0f, 1f);
        }
    }

    // Alpha curve — rises over FadeInMs, holds at 1, falls over FadeOutMs.
    public static float CurrentAlpha
    {
        get
        {
            if (!IsActive) return 0f;
            long now = SAOTRPG.Systems.FrameClock.ElapsedMs;
            long elapsed = now - _activatedAtMs;
            if (elapsed < 0) return 0f;
            if (elapsed < FadeInMs) return elapsed / (float)FadeInMs;
            long holdEnd = _durationMs - FadeOutMs;
            if (elapsed < holdEnd) return 1f;
            if (elapsed < _durationMs)
                return System.Math.Max(0f, 1f - (elapsed - holdEnd) / (float)FadeOutMs);
            return 0f;
        }
    }

    public static string WeaponName  => _weaponName;
    public static string RarityLabel => _rarityLabel;
    // Bundle 9: true when the active banner is an awakening fire (vs. obtain).
    public static bool IsAwakening => _isAwakening;
    public static int AwakeningLevel => _awakeningLevel;

    // Bundle 13 — particle level (1/2/3) for Wave 2 saotrpg-ui MapView.Particles consumer.
    // Set in TriggerAwakening; mirrors _awakeningLevel for explicit particle-system contract.
    public static int AwakeningParticleLevel { get; private set; }

    // Bundle 13 — true when the awakening banner is active and we are still inside the
    // first half of its duration window (emit phase). Consumed by saotrpg-ui Wave 2.
    public static bool ShouldEmitParticlesThisFrame
    {
        get
        {
            if (!IsActive || !_isAwakening || _durationMs <= 0) return false;
            long elapsed = SAOTRPG.Systems.FrameClock.ElapsedMs - _activatedAtMs;
            return elapsed >= 0 && elapsed < _durationMs / 2;
        }
    }

    // Fires the banner. Called from GameScreen.Events after TurnManager.DivineObtained.
    // Idempotent within a single active window: re-trigger just restarts the clock.
    public static void Trigger(Weapon divine, int durationMs = DefaultDurationMs)
    {
        _weaponName = string.IsNullOrWhiteSpace(divine.EnhancedName) ? (divine.Name ?? "Divine Weapon") : divine.EnhancedName;
        _rarityLabel = divine.Rarity ?? "Divine";
        _isAwakening = false;
        _awakeningLevel = 0;
        _durationMs = System.Math.Max(FadeInMs + FadeOutMs + 500, durationMs);
        _activatedAtMs = SAOTRPG.Systems.FrameClock.ElapsedMs;
        IsActive = true;
    }

    // Bundle 9: Awakening variant fire. Swaps header/subtitle to ◈ DIVINE AWAKENED ◈.
    // Reuses the same fade+hold timer as the obtain banner.
    public static void TriggerAwakening(Weapon divine, int newLevel, int durationMs = DefaultDurationMs)
    {
        _weaponName = string.IsNullOrWhiteSpace(divine.EnhancedName) ? (divine.Name ?? "Divine Weapon") : divine.EnhancedName;
        _rarityLabel = divine.Rarity ?? "Divine";
        _isAwakening = true;
        _awakeningLevel = newLevel;
        AwakeningParticleLevel = newLevel;
        _durationMs = System.Math.Max(FadeInMs + FadeOutMs + 500, durationMs);
        _activatedAtMs = SAOTRPG.Systems.FrameClock.ElapsedMs;
        IsActive = true;
    }

    // Advances the timer. Returns true while the banner is still drawable.
    public static bool Tick()
    {
        if (!IsActive) return false;
        long now = SAOTRPG.Systems.FrameClock.ElapsedMs;
        if (now - _activatedAtMs >= _durationMs) { IsActive = false; _weaponName = ""; _isAwakening = false; _awakeningLevel = 0; AwakeningParticleLevel = 0; }
        return IsActive;
    }

    // Manual dismiss (floor swap, death, etc.). Safe to call when inactive.
    public static void Clear()
    {
        IsActive = false;
        _weaponName = "";
        _rarityLabel = "";
        _isAwakening = false;
        _awakeningLevel = 0;
        AwakeningParticleLevel = 0;
        _durationMs = 0;
        _activatedAtMs = 0;
    }
}
