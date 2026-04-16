namespace SAOTRPG.Map;

// Global day/night clock driven by the player's turn counter.
// Sun elevation follows a smooth cosine from noon → midnight → noon,
// looping every CycleLength turns. SunLevel
// is the normalized elevation in [0, 1] and drives both the ambient
// light color in LightingSystem and the effective player
// FOV radius used by GameMap.UpdateVisibility.
// The player always starts at noon (turn 0) so a new run opens in
// bright daylight, then gradually darkens as turns accumulate.
public static class DayNightCycle
{
    // Total turns per full day→night→day cycle.
    public const int CycleLength = 400;

    // Total game turns elapsed. TurnManager pushes this each turn so
    // lighting and visibility radius can react without taking a
    // parameter through the whole call chain.
    public static int CurrentTurn { get; set; }

    // Smooth sun elevation in [0, 1]. 1 = noon, 0 = midnight.
    // Uses a half-amplitude cosine so the transitions at dusk/dawn
    // are gentler than the plateaus at noon/midnight.
    // FB-564 StarlessNight modifier forces perpetual deep night.
    public static float SunLevel
    {
        get
        {
            if (Systems.RunModifiers.IsActive(Systems.RunModifier.StarlessNight)) return 0f;
            float phase = (float)(CurrentTurn % CycleLength) / CycleLength;
            return 0.5f + 0.5f * (float)Math.Cos(phase * 2.0 * Math.PI);
        }
    }

    // Ambient RGB for the LightingSystem — lerped between deep
    // midnight blue and warm daylight based on SunLevel.
    public static (float R, float G, float B) Ambient
    {
        get
        {
            float s = SunLevel;
            // Night: cool moonlit blue. Day: warm off-white.
            float r = Lerp(18f,  180f, s);
            float g = Lerp(22f,  175f, s);
            float b = Lerp(45f,  150f, s);
            return (r, g, b);
        }
    }

    // Viewport half-diagonal — set each frame by MapView so the FOV
    // can scale to the actual screen size.
    public static int ViewportRadius { get; set; } = 80;

    // Night shrinks vision to a torch bubble; day opens it to the full
    // viewport. The minimum is large enough to still feel playable.
    public const int MinVisibility = 18;
    public static int VisibilityRadius =>
        (int)Math.Round(Lerp(MinVisibility, ViewportRadius, SunLevel));

    // Short human-readable phase: "Day", "Dusk", "Night", or "Dawn".
    // Checks sun level plus direction of change within the cycle.
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

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
