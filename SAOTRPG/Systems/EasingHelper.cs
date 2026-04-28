using Terminal.Gui;

namespace SAOTRPG.Systems;

// Wave 2 — shared easing curves + color/scheme helpers for UI motion polish.
// Keep all curves clamped to [0,1] so callers never see out-of-range values.
public static class EasingHelper
{
    public enum EasingType
    {
        Linear,
        EaseIn,         // t*t — slow start
        EaseOut,        // 1-(1-t)^2 — fast start, soft settle (alias for EaseOutQuad)
        EaseInOut,      // smooth both ends
        EaseOutQuad,    // explicit alias
        EaseOutCubic,   // 1-(1-t)^3 — sharper deceleration than EaseOutQuad
    }

    // t in [0,1] returns eased value in [0,1]. Out-of-range t is clamped first.
    public static float Ease(float t, EasingType type)
    {
        t = Math.Clamp(t, 0f, 1f);
        return type switch
        {
            EasingType.Linear        => t,
            EasingType.EaseIn        => t * t,
            EasingType.EaseOut       => 1f - (1f - t) * (1f - t),
            EasingType.EaseOutQuad   => 1f - (1f - t) * (1f - t),
            EasingType.EaseOutCubic  => 1f - (1f - t) * (1f - t) * (1f - t),
            EasingType.EaseInOut     => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t),
            _                        => t,
        };
    }

    // Linear RGB blend. t=0 → from, t=1 → to. Clamped to [0,1].
    public static Color LerpColor(Color from, Color to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        byte r = (byte)(from.R + (to.R - from.R) * t);
        byte g = (byte)(from.G + (to.G - from.G) * t);
        byte b = (byte)(from.B + (to.B - from.B) * t);
        return new Color(r, g, b);
    }

    // Multiply each RGB component by alpha. alpha=0 → black, alpha=1 → unchanged.
    public static Color ScaleColor(Color c, float alpha)
    {
        alpha = Math.Clamp(alpha, 0f, 1f);
        return new Color((byte)(c.R * alpha), (byte)(c.G * alpha), (byte)(c.B * alpha));
    }

    // Returns a NEW ColorScheme with all four Attributes' fg + bg scaled by alpha.
    // Used by the dialog fade-in to ramp the whole scheme up from black.
    public static ColorScheme ScaleScheme(ColorScheme baseScheme, float alpha)
    {
        return new ColorScheme
        {
            Normal    = ScaleAttr(baseScheme.Normal, alpha),
            Focus     = ScaleAttr(baseScheme.Focus, alpha),
            HotNormal = ScaleAttr(baseScheme.HotNormal, alpha),
            HotFocus  = ScaleAttr(baseScheme.HotFocus, alpha),
            Disabled  = ScaleAttr(baseScheme.Disabled, alpha),
        };
    }

    private static Terminal.Gui.Attribute ScaleAttr(Terminal.Gui.Attribute a, float alpha)
        => new(ScaleColor(a.Foreground, alpha), ScaleColor(a.Background, alpha));
}
