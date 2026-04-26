namespace SAOTRPG.Map;

// Day/night clock driven by turn counter. Cosine noon→midnight→noon loop of CycleLength turns.
// SunLevel ∈ [0,1] drives LightingSystem ambient + GameMap FOV radius. Runs start at noon (turn 0).
public static class DayNightCycle
{
    // Total turns per full day→night→day cycle. Wave 1 — 10× extension; the prior 400 felt too brisk.
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

    // Ambient RGB — lerped midnight blue → warm daylight by SunLevel.
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

    // Viewport half-diagonal — set each frame by MapView. Drives ROI scratch sizing.
    public static int ViewportRadius { get; set; } = 80;

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

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
