
using SAOTRPG.Systems.Persistence;
using SAOTRPG.UI.Effects;

namespace SAOTRPG.UI.Rendering;

// Grid-native viewport shake. Tier-1 = 3f × 33ms × ±1 cell,
// tier-2 = 3f × 40ms × ±2 cell. Ignore incoming if active + same-or-lower tier.
public partial class MapView
{
    private int _shakeMsLeft;
    private int _shakeFrameMs;
    private int _shakeAmplitude;
    private int _shakeTier;
    private int _shakeOffsetX;
    private int _shakeOffsetY;
    private int _shakeElapsedInFrameMs;
    // Deterministic-ish shake RNG; decorative only.
    private static readonly Random _shakeRng = new();

    // The two tiers, named so the Player Guide's table can be checked against them rather than
    // transcribing the numbers — the prose had drifted to describing tier 1 as the only tier.
    public const int Tier1Ms = 100, Tier1FrameMs = 33, Tier1Cells = 1;
    public const int Tier2Ms = 120, Tier2FrameMs = 40, Tier2Cells = 2;

    // tier: 1 = a crit, or the player taking a big hit; 2 = a boss ability.
    // Ignored if a same-or-higher tier is already shaking (no stacking).
    public void RequestShake(int tier)
    {
        if (!UserSettings.Current.ScreenShakeEnabled || !Motion.Animate) return;
        if (_shakeMsLeft > 0 && tier <= _shakeTier) return;

        _shakeTier = tier;
        if (tier >= 2)
        { _shakeFrameMs = Tier2FrameMs; _shakeAmplitude = Tier2Cells; _shakeMsLeft = Tier2Ms; }
        else
        { _shakeFrameMs = Tier1FrameMs; _shakeAmplitude = Tier1Cells; _shakeMsLeft = Tier1Ms; }
        _shakeElapsedInFrameMs = 0;
        PickShakeOffset();
        DirtyFrame();
    }

    // Camera applies (shakeOffsetX, shakeOffsetY) as an extra viewport nudge.
    public int ShakeOffsetX => _shakeMsLeft > 0 ? _shakeOffsetX : 0;
    public int ShakeOffsetY => _shakeMsLeft > 0 ? _shakeOffsetY : 0;
    public bool IsShaking => _shakeMsLeft > 0;

    // Tick from the same ms clock that drives damage popups.
    private void TickShake(int dtMs)
    {
        if (_shakeMsLeft <= 0) return;
        _shakeMsLeft -= dtMs;
        _shakeElapsedInFrameMs += dtMs;
        if (_shakeElapsedInFrameMs >= _shakeFrameMs)
        {
            _shakeElapsedInFrameMs = 0;
            PickShakeOffset();
        }
        if (_shakeMsLeft <= 0)
        { _shakeOffsetX = 0; _shakeOffsetY = 0; _shakeTier = 0; }
    }

    private void PickShakeOffset()
    {
        int a = _shakeAmplitude;
        _shakeOffsetX = _shakeRng.Next(-a, a + 1);
        _shakeOffsetY = _shakeRng.Next(-a, a + 1);
        // Snap-back on final 3rd of lifetime: collapse toward (0,0) so the
        // viewport settles instead of cutting mid-offset.
        if (_shakeMsLeft <= _shakeFrameMs)
        { _shakeOffsetX = 0; _shakeOffsetY = 0; }
    }
}
