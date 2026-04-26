using SAOTRPG.Systems;

namespace SAOTRPG.UI;

// Grid-native viewport shake (FB-453). Tier-1 = 3f × 33ms × ±1 cell,
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

    // tier: 1 = crit / player took big hit; 2 = boss special / quake.
    // Ignored if a same-or-higher tier is already shaking (no stacking).
    public void RequestShake(int tier)
    {
        if (!UserSettings.Current.ScreenShakeEnabled) return;
        if (_shakeMsLeft > 0 && tier <= _shakeTier) return;

        _shakeTier = tier;
        if (tier >= 2)
        { _shakeFrameMs = 40; _shakeAmplitude = 2; _shakeMsLeft = 120; }
        else
        { _shakeFrameMs = 33; _shakeAmplitude = 1; _shakeMsLeft = 100; }
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
