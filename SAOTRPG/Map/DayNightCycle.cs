namespace SAOTRPG.Map;

// Day/night clock driven by turn counter. Cosine noon→midnight→noon loop of CycleLength turns.
// SunLevel ∈ [0,1] drives LightingSystem ambient + GameMap FOV radius. Runs start at noon (turn 0).
public static class DayNightCycle
{
    // Total turns per full day→night→day cycle.
    public const int CycleLength = 4000;

    // Total turns elapsed. TurnManager pushes each turn so lighting/FOV react without threading through calls.
    public static int CurrentTurn { get; set; }

    // Sun elevation in [0,1]: 1=noon, 0=midnight. Half-amplitude cosine → gentle dusk/dawn, plateaus at noon/midnight.
    // StarlessNight modifier forces perpetual deep night.
    public static float SunLevel
    {
        get
        {
            if (Systems.RunModifiers.IsActive(Systems.RunModifier.StarlessNight)) return 0f;
            float phase = (float)(CurrentTurn % CycleLength) / CycleLength;
            return 0.5f + 0.5f * (float)Math.Cos(phase * 2.0 * Math.PI);
        }
    }

    // Sun level is snapped to this many steps before it becomes ambient light.
    // The light grid is the map renderer's change-detection input: an unquantized ambient
    // shifts every lit cell by a fraction of a colour channel on every single turn, which
    // would mark the whole viewport changed and defeat incremental tile rendering. At 64
    // steps the largest jump is ~1.3/255 per channel, well under the step-8 quantization
    // the renderer already applies to lit colours.
    private const int AmbientSteps = 64;

    // Ambient RGB — lerped midnight blue → warm daylight by SunLevel.
    // Sun level snapped to AmbientSteps. Anything that scales with daylight reads THIS rather
    // than SunLevel, so it steps in lockstep with the ambient colour instead of adding a second,
    // finer churn source to the renderer's change detection.
    public static float QuantizedSunLevel => MathF.Round(SunLevel * AmbientSteps) / AmbientSteps;

    public static (float R, float G, float B) Ambient
    {
        get
        {
            float s = QuantizedSunLevel;
            // Night: cool moonlit blue. Day: warm off-white.
            float r = Lerp(18f,  180f, s);
            float g = Lerp(22f,  175f, s);
            float b = Lerp(45f,  150f, s);
            return (r, g, b);
        }
    }

    // 4× FOV multiplier — open-field daytime visibility extends well beyond the viewport.
    // Tactical reveal radius; the camera still tracks the player within the smaller viewport.
    public const int FovMultiplier = 4;

    // Open-field FOV in cell-units. Set per frame from MapView.Rendering.cs (= halfH × FovMultiplier).
    public static int FovRadius { get; set; } = 80 * FovMultiplier;

    // Night → torch bubble, Day → full FOV. Min kept playable.
    public const int MinVisibility = 18;
    public static int VisibilityRadius =>
        (int)Math.Round(Lerp(MinVisibility, FovRadius, SunLevel));

    // "Day", "Dusk", "Night", or "Dawn" — sun level + direction within cycle.
    public static string PhaseName
    {
        get
        {
            float s = SunLevel;
            bool descending = (CurrentTurn % CycleLength) < CycleLength / 2;
            if (s >= 0.80f) return "Day";
            if (s <= 0.20f) return "Night";
            return descending ? "Dusk" : "Dawn";
        }
    }

    // ── The directional sun ────────────────────────────────────────────────────────────────────
    // Ambient above is the sky: one colour everywhere, no direction, so a forest canopy lit the
    // ground exactly as brightly as the clearing beside it. This is the other half — a sun with a
    // BEARING, so anything that blocks sight throws a shadow away from it, and that shadow
    // stretches and swings through the day.
    //
    // Everything below derives from ONE quantized integer, SunState, for the same reason Ambient
    // snaps to 64 steps: the light grid is the map renderer's change-detection input. A sun that
    // rotated continuously would move every shadowed cell on every single turn and defeat
    // incremental tile rendering outright. At 24 steps across the daylight half of the cycle the
    // sun moves once per ~83 turns, which is rarer than the ambient steps the renderer already
    // absorbs. Deriving direction, reach AND strength from that one integer is what makes the
    // guarantee checkable: if SunState has not changed, no shadow has moved.
    private const int SunSteps = 24;

    // Deepest a full shadow may take a cell, as a share of ambient. The map is text: a glyph in
    // shadow still has to be READ, so this is a mood dial and not a physical one.
    private const float MaxShadowDepth = 0.48f;

    // How far into the day the shadows ramp up, and out again before dusk. Without it the effect
    // would snap on at first light and off at last, which is the one moment a shadow is most
    // obviously wrong.
    private const float TwilightRamp = 0.18f;

    // Position within the daylight half: 0 at dawn, 1 at dusk, above 1 while the sun is down.
    // Derived so that SunLevel == 0.5 + 0.5*sin(pi * DayFraction) — the two agree by construction
    // rather than by a second hand-tuned curve.
    private static float DayFraction
    {
        get
        {
            float phase = (float)(CurrentTurn % CycleLength) / CycleLength;   // 0 = noon
            float t = phase - 0.75f;
            if (t < 0f) t += 1f;
            return t * 2f;
        }
    }

    // The quantized sun, or -1 when it is below the horizon. Every shadow parameter is a pure
    // function of this, so it doubles as the renderer's "has anything moved" stamp.
    public static int SunState
    {
        get
        {
            if (Systems.RunModifiers.IsActive(Systems.RunModifier.StarlessNight)) return -1;
            float d = DayFraction;
            if (d > 1f) return -1;
            return (int)MathF.Round(d * SunSteps);
        }
    }

    public static bool SunIsUp => SunState >= 0;

    // Sun bearing as an angle in [0, pi]: 0 at dawn (sun due east), pi/2 at noon, pi at dusk.
    private static float SunAngle => MathF.PI * SunState / SunSteps;

    // Unit vector along which shadows FALL, in screen coordinates (y grows downward).
    // Dawn -> west, noon -> north, dusk -> east: a shadow that swings a half-circle over the day
    // and is diagonal for all but three instants of it.
    public static (float X, float Y) ShadowDirection
    {
        get
        {
            float a = SunAngle;
            return (-MathF.Cos(a), -MathF.Sin(a));
        }
    }

    // Sun elevation in [0,1] from the same angle — 0 on the horizon, 1 at noon.
    private static float SunElevation => MathF.Sin(SunAngle);

    // Shadow retained per tile travelled. A low sun casts a long shadow, so it decays slowly;
    // at noon the shadow is a short pool under the thing casting it.
    public static float ShadowDecay => Lerp(0.955f, 0.865f, SunElevation);

    // Share of ambient a fully shadowed tile loses. Ramped in over dawn and out over dusk.
    public static float ShadowStrength
    {
        get
        {
            if (!SunIsUp) return 0f;
            float d = (float)SunState / SunSteps;
            return MaxShadowDepth * SmoothStep(d / TwilightRamp) * SmoothStep((1f - d) / TwilightRamp);
        }
    }

    // ── How much a carried or placed light still counts for ───────────────────────────────────
    // A torch is invisible at noon. It was not: the flame added a flat ~220 per channel whatever
    // the hour, so at midday the player walked inside a thirteen-tile disc CLIPPED AT 255 — 1.42x
    // the ambient around it — which is a permanent spotlight, and it washed out the sun shadows
    // within thirteen tiles of the only thing that can see them.
    //
    // Both curves are driven by the quantized sun level so they step with the ambient colour.
    // They differ only in floor: a carried flame really does vanish in daylight, while a campfire
    // or a lava pool is a LANDMARK the player navigates by and keeps a visible glow.
    private static float DarknessCurve
    {
        get
        {
            float s = QuantizedSunLevel;
            float d = 1f - s;
            return d * MathF.Sqrt(d);          // (1-s)^1.5 — falls away quickly as the sun climbs
        }
    }

    public static float TorchInfluence => 0.08f + 0.92f * DarknessCurve;

    public static float EmissiveInfluence => 0.25f + 0.75f * DarknessCurve;

    private static float SmoothStep(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return t * t * (3f - 2f * t);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
