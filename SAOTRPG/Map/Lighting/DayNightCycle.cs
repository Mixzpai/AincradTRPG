namespace SAOTRPG.Map.Lighting;

// Day/night clock driven by turn counter. Cosine noon→midnight→noon loop of CycleLength turns.
// A turn is one second (Systems/World/GameClock), so the loop is 24 hours.
// SunLevel ∈ [0,1] drives LightingSystem ambient + GameMap FOV radius. Runs start at noon (turn 0).
public static class DayNightCycle
{
    // Total turns per full day→night→day cycle: one day of seconds.
    public const int CycleLength = SAOTRPG.Systems.World.GameClock.TurnsPerDay;

    // Total turns elapsed. TurnManager pushes each turn so lighting/FOV react without threading through calls.
    public static int CurrentTurn { get; set; }

    // Sun elevation in [0,1]: 1=noon, 0=midnight. Half-amplitude cosine → gentle dusk/dawn, plateaus at noon/midnight.
    // StarlessNight modifier forces perpetual deep night.
    public static float SunLevel => SunLevelAt(CurrentTurn);

    // Every sky value below is a pure function of the turn, so the light cache's worker can shade
    // a sky that has not arrived yet (ShadeAt). The properties read the current turn.
    private static float SunLevelAt(int turn)
    {
        if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.StarlessNight)) return 0f;
        float phase = (float)(turn % CycleLength) / CycleLength;
        return 0.5f + 0.5f * (float)Math.Cos(phase * 2.0 * Math.PI);
    }

    // The sun level is snapped to a ladder of rungs before it becomes ambient light. The light
    // grid is the map renderer's change-detection input: an unquantized ambient shifts every lit
    // cell by a fraction of a colour channel on every turn, which would mark the whole viewport
    // changed and defeat incremental tile rendering.
    //
    // The ladder is perceptual, not uniform. A uniform step spends the same budget on the flat
    // night as on twilight, and twilight moves the sky by tens of dE in a twentieth of the cycle.
    // The rungs are placed where the sky itself moves instead: consecutive rungs differ by at
    // most AmbientStepBudget in CIE94 dE on a 200-grey tile, so twilight gets dense rungs, the
    // flat night gets none, and the worst visible jump is the same everywhere on the ladder.
    //
    // Every rung costs one light-cache refresh (the cache keys on the ambient triple, and every
    // change re-sweeps the padded region and re-resolves every lit cell), so the rung count is a
    // churn budget: the budget must not be set so fine that the ladder outgrows it.
    //
    // 0.66 keeps every rung step well under a just-noticeable difference (1.0).
    private const float AmbientStepBudget = 0.66f;

    // How finely the curve is sampled when the rungs are laid. A turn moves the sun level by at
    // most pi / (2 x CycleLength), about 0.07 samples, so a rung can never be skipped in one turn.
    private const int LadderSamples = 4096;

    // Sight is quantized separately and more coarsely, which is why the two are not one number.
    // A colour step costs a re-resolve; a radius step costs a full shadowcast, because the
    // visible set is only reused when the radius matches, so a finer sight ladder would make
    // still turns pay for shadowcasts. Sight also decides what is explored, so it stays on this
    // uniform ladder, and the sky ladder above is free to put its rungs wherever the colour moves.
    private const int RadiusSteps = 64;

    // The sun level snapped to the nearest rung of the sky ladder. Anything that scales with
    // daylight reads this rather than SunLevel, so it steps in lockstep with the ambient colour
    // instead of adding a second, finer churn source to the renderer's change detection.
    public static float QuantizedSunLevel => SnapToLadder(SunLevel);

    // The coarser level sight steps on. Everything that decides a colour reads QuantizedSunLevel.
    public static float RadiusSunLevel => MathF.Round(SunLevel * RadiusSteps) / RadiusSteps;

    // Golden hour. The base curve below is a straight lerp from moonlit blue to daylight white,
    // which on its own would make dawn and dusk a dimmer noon, though those are the hours the
    // sun sits on the horizon, throws its longest shadows and swings hardest across the compass.
    //
    // A warm bump peaking just above the horizon pairs with the shadow's cool tint: warm direct
    // light against blue shade is what makes low sun read as low sun rather than as the
    // brightness being turned down. It tapers to nothing by deep night and by high day, so noon
    // stays neutral and midnight stays blue.
    //
    // Driven by the quantized sun level like everything else here, so it steps with the ambient
    // colour rather than adding a second, finer churn source to the renderer's change detection.
    private const float GoldenPeak = 0.55f;   // sun level where the warmth is strongest
    private const float GoldenBand = 0.32f;   // half-width; zero outside it on the daylight side
    private const float GoldenR = 46f, GoldenG = 9f, GoldenB = -30f;

    // The two ends of the daylight lerp. Night: cool moonlit blue. Day: warm off-white.
    private const float FloorR = 18f, FloorG = 22f, FloorB = 45f;
    private const float NoonR = 180f, NoonG = 175f, NoonB = 150f;

    // Twilight. The night half is built from the sun's depth below the horizon (0 at sunset, 1
    // at the nadir, which for a full moon opposite the sun is also the moon's elevation), so the
    // ambient stays a pure function of the quantized sun level and nothing new reaches the cache
    // key. Two terms fade at two rates, the way twilight does: the horizon glow dies first, the
    // neutral skylight after it, and past that the sky sits on the moonlit floor until dawn's
    // mirror. The night joins the day half at the horizon by construction. The floor is the
    // anchor the night renderer is tuned against.
    //
    // The sky is one colour per turn, a pure function of the quantized sun level; a renderer
    // takes the triple as given and adds no direction to it.
    //
    // Depth by which the horizon glow has faded to nothing — civil twilight, about 28 minutes.
    private const float TwilightGlowDepth = 0.12f;
    // Depth by which the skylight has faded to the floor. Derived, not chosen: it is the depth
    // at which the rules below call it night (RadiusSunLevel at NightThreshold, i.e. sun level
    // 0.20), so the sky reaches its floor on the turn the sidebar turns to Night and sight
    // collapses, about 2.5 hours after sunset.
    private const float TwilightSkyDepth = 1f - 2f * NightThreshold;

    public static (float R, float G, float B) Ambient => SkyAt(QuantizedSunLevel);

    // The unquantized curve. The ladder is laid along it and Ambient samples it at a rung.
    private static (float R, float G, float B) SkyAt(float s)
    {
        if (s >= 0.5f) return DaySky(s);
        float depth = 1f - 2f * s;
        float sky = 1f - SmoothStep(depth / TwilightSkyDepth);
        float glow = 1f - SmoothStep(depth / TwilightGlowDepth);
        var (hr, hg, hb) = DayBase(0.5f);
        var (gr, gg, gb) = Golden(0.5f);
        return (Math.Clamp(FloorR + (hr - FloorR) * sky + gr * glow, 0f, 255f),
                Math.Clamp(FloorG + (hg - FloorG) * sky + gg * glow, 0f, 255f),
                Math.Clamp(FloorB + (hb - FloorB) * sky + gb * glow, 0f, 255f));
    }

    private static (float R, float G, float B) DayBase(float s) =>
        (Lerp(FloorR, NoonR, s), Lerp(FloorG, NoonG, s), Lerp(FloorB, NoonB, s));

    // The golden-hour warmth as a per-channel delta: smooth and flat-topped at the peak.
    private static (float R, float G, float B) Golden(float s)
    {
        float d = MathF.Abs(s - GoldenPeak) / GoldenBand;
        if (d >= 1f) return (0f, 0f, 0f);
        float warm = 1f - d * d;
        return (warm * GoldenR, warm * GoldenG, warm * GoldenB);
    }

    private static (float R, float G, float B) DaySky(float s)
    {
        var (r, g, b) = DayBase(s);
        var (gr, gg, gb) = Golden(s);
        return (Math.Clamp(r + gr, 0f, 255f), Math.Clamp(g + gg, 0f, 255f), Math.Clamp(b + gb, 0f, 255f));
    }

    // 4× FOV multiplier — open-field daytime visibility extends well beyond the viewport.
    // Tactical reveal radius; the camera still tracks the player within the smaller viewport.
    public const int FovMultiplier = 4;

    // Open-field FOV in cell-units. Set per frame from MapView.Rendering.cs (= halfH × FovMultiplier).
    public static int FovRadius { get; set; } = 80 * FovMultiplier;

    // Night → torch bubble, Day → full FOV. Min kept playable.
    //
    // Reads RadiusSunLevel, the coarser quantized level, so the integer radius moves only on a
    // rung step: the visible set is only reused when the radius matches, and the raw sun level
    // would make still turns pay a full shadowcast for a step nobody could see. The quantized
    // level still spans 0 to 1, so the range and the maximum reach are unchanged.
    public const int MinVisibility = 18;
    public static int VisibilityRadius =>
        (int)Math.Round(Lerp(MinVisibility, FovRadius, RadiusSunLevel));

    // ── What the hour does to the game, rather than to the picture ─────────────────────────
    // Everything else in this file decides a colour, a radius or a shadow. This decides
    // difficulty, and it is the only thing here that does.
    //
    // Night is a bucket, not a curve, and it reads RadiusSunLevel, the level VisibilityRadius
    // already steps on. A gameplay term driven by the raw sun level would move on nearly every
    // turn; and reading the same level as sight means the hour the player's vision collapses is
    // exactly the hour the world turns hostile.
    //
    // PhaseName below reads this rather than carrying its own threshold: PhaseName is the string
    // the sidebar prints, and if it could disagree with the phase the rules consult for even one
    // turn, the game would charge a night penalty while telling the player it is dusk.
    public const float NightThreshold = 0.20f;
    public const float DayThreshold = 0.80f;

    public static bool IsNight => RadiusSunLevel <= NightThreshold;

    // "Day", "Dusk", "Night", or "Dawn" — sun level + direction within cycle.
    public static string PhaseName
    {
        get
        {
            bool descending = (CurrentTurn % CycleLength) < CycleLength / 2;
            if (RadiusSunLevel >= DayThreshold) return "Day";
            if (IsNight) return "Night";
            return descending ? "Dusk" : "Dawn";
        }
    }

    // ── The directional sky ────────────────────────────────────────────────────────────────────
    // Ambient above is the sky: one colour everywhere, no direction. This is the other half: a
    // sun with a bearing, so anything that blocks sight throws a shadow away from it, and that
    // shadow stretches and swings through the day. The moon further down does the same for the
    // night.
    //
    // Everything below derives from one quantized integer, SunState, for the same reason Ambient
    // snaps to the sky ladder: the light grid is the map renderer's change-detection input. A sun
    // that rotated continuously would move every shadowed cell on every turn and defeat
    // incremental tile rendering. Deriving direction, reach and strength from that one integer is
    // what makes the guarantee checkable: if SunState has not changed, no shadow has moved.
    //
    // A bearing step is the largest temporal jump on the map (most of the viewport changes at
    // once), so the step count is a banding budget as well as a churn one: fine enough that an
    // ordinary bearing step stays under a just-noticeable difference, and no finer, since every
    // state costs a light-cache rebuild. The sweep swaps axes at 45 and 135 degrees, where the two
    // rasterisations of one diagonal disagree; LightingSystem.Shading blends them through that
    // seam, without which no step count could make the field continuous in the bearing.
    private const int SunSteps = 192;

    // Deepest a full shadow may take a cell, as a share of ambient. The map is text: a glyph in
    // shadow still has to be read, so this is a mood dial and not a physical one.
    private const float MaxShadowDepth = 0.48f;

    // How far into the day the shadows ramp up, and out again before dusk. Without it the effect
    // would snap on at first light and off at last, which is the one moment a shadow is most
    // obviously wrong.
    private const float TwilightRamp = 0.18f;

    // Position within the daylight half: 0 at dawn, 1 at dusk, above 1 while the sun is down.
    // Derived so that SunLevel == 0.5 + 0.5*sin(pi * DayFraction) — the two agree by construction
    // rather than by a second hand-tuned curve.
    private static float DayFractionAt(int turn)
    {
        float phase = (float)(turn % CycleLength) / CycleLength;   // 0 = noon
        float t = phase - 0.75f;
        if (t < 0f) t += 1f;
        return t * 2f;
    }

    // The quantized state of whichever body is in the sky. Every shadow parameter is a pure
    // function of this, so it doubles as the renderer's "has anything moved" stamp.
    //
    // Non-negative is the sun's own bearing step. Negative is the night: -1 when nothing
    // directional is up at all, and -2 - MoonState for each of the moon's bearings.
    //
    // The moon lives in this one integer because LightingSystem.Cache keys the entire ambient +
    // shadow + contact field on this value: a moon bearing the key could not see would leave the
    // map lit by an earlier moon for the rest of the night, and stale light looks like light.
    // Folding the night in here means the key covers it by construction.
    public static int SunState => SunStateAt(CurrentTurn);

    private static int SunStateAt(int turn)
    {
        if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.StarlessNight)) return -1;
        float d = DayFractionAt(turn);
        if (d <= 1f) return (int)MathF.Round(d * SunSteps);
        // The sun is down and the run is not starless, so the moon is up and MoonState >= 0.
        return -2 - MoonStateAt(turn);
    }

    public static bool SunIsUp => SunState >= 0;

    // The bearing of whichever body a state describes, as an angle in [0, pi]: 0 at its rise (due
    // east), pi/2 at its height, pi at its set. A night state is -2 - MoonState.
    private static float AngleOf(int state) =>
        state >= 0 ? MathF.PI * state / SunSteps : MathF.PI * (-2 - state) / MoonSteps;

    // ── The directional moon ───────────────────────────────────────────────────────────────────
    // A full moon sits opposite the sun, so it rises in the east as the sun sets, stands south at
    // midnight and sets in the west as the sun comes back up — the same half-circle, half a cycle
    // later. That is what lets it feed the same sweep rather than a second one: direction, decay
    // and strength below all answer for whichever body is up, and the shading pass has to know
    // only which per-channel loss to spend.
    //
    // The night's visual contract is long, faint, cool shadows laid away from the moon, an order
    // of magnitude weaker than the day's over an ambient an order of magnitude darker, so the two
    // read at similar contrast. Scaling the night shadow by the same physical share as the day's
    // would draw nothing at all.
    //
    // Twelve steps across the night. Moonlight is about a tenth of daylight, so a moon bearing
    // step twice as coarse in angle as the sun's still moves the light grid less than a sun step
    // does and stays under a just-noticeable difference; every extra step would cost a
    // light-cache refresh for a smoothness nobody can see.
    private const int MoonSteps = 12;

    // Deepest a full moon shadow may take a cell, as a share of ambient. Lower than the sun's,
    // but not by the ratio of the two ambients: night ambient runs 18-45 per channel, so a share
    // small enough to be physically faithful would be seen nowhere at all.
    //
    // The constraint is perceptual. At this depth the deepest reachable moon shadow is only a
    // little above a just-noticeable difference in the middle of the night, when directional
    // shading is the only structure on the screen, and an ordinary lone-tree shadow sits at about
    // the threshold; a shallower depth would delete the night's relief where it does the most
    // work. The foreground is the surface that has to carry it: the background glow is
    // light x BgGlowScale, so the same shadow reads fainter there.
    private const float MaxMoonShadowDepth = 0.34f;

    // Ramped in over moonrise and out over moonset, the way the sun's ramps over dawn and dusk.
    // Both are near zero at the handover, so neither body snaps its shadows on — which is also
    // what hides the 180-degree turn the shadows take there, since a full moon rises where the
    // sun has just set.
    //
    // Ambient falls faster than this ramp rises, so the deepest shadow in absolute light units
    // lands in early twilight rather than at the middle of the night; as a share of ambient (what
    // multiplies the tile colour) the deepest is still mid-night. A wider ramp would hold the
    // moon faint until the sky is genuinely dark, at the cost of a duller first half of the night.
    private const float MoonTwilightRamp = 0.20f;

    // Position within the night: 0 at dusk, 1 at dawn, at or below 0 while the sun has the sky.
    private static float NightFractionAt(int turn) => DayFractionAt(turn) - 1f;

    // The quantized moon, or -1 when there is none: the sun has the sky, or the run is starless.
    //
    // The boundary belongs to the sun. At NightFraction exactly 0 the sun is still up and
    // SunState is still reporting its last daylight bearing, so a moon there would give one turn
    // two sky bodies at once and put a positive daylight state inside the range the
    // light cache treats as night. Both halves read the boundary off the same DayFraction, so
    // they cannot disagree about where the day ends.
    public static int MoonState => MoonStateAt(CurrentTurn);

    private static int MoonStateAt(int turn)
    {
        if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.StarlessNight)) return -1;
        float n = NightFractionAt(turn);
        if (n <= 0f) return -1;
        return (int)MathF.Round(n * MoonSteps);
    }

    // ── What the sweep reads ───────────────────────────────────────────────────────────────────
    // Unit vector along which shadows fall, in screen coordinates (y grows downward).
    // Dawn -> west, noon -> north, dusk -> east; then moonrise -> west, midnight -> north,
    // moonset -> east. A shadow that swings a half-circle over the day, and again over the night,
    // and is diagonal for all but six instants of the whole cycle.
    public static (float X, float Y) ShadowDirection => DirectionOf(SunState);

    private static (float X, float Y) DirectionOf(int state)
    {
        float a = AngleOf(state);
        return (-MathF.Cos(a), -MathF.Sin(a));
    }

    // Shadow retained per tile travelled. A low sun casts a long shadow, so it decays slowly;
    // at noon the shadow is a short pool under the thing casting it.
    //
    // The moon's range is uniformly slower, which is what makes a night shadow long. It also has
    // to be: at a tenth of the day's amplitude a shadow on the day's decay would fall under the
    // renderer's colour quantizer within a couple of cells and read as a smudge at the caster's
    // foot rather than as a shadow lying across the ground.
    public static float ShadowDecay => DecayOf(SunState);

    // Elevation is the sine of the bearing angle: 0 on the horizon, 1 at the body's height.
    private static float DecayOf(int state) => state >= 0
        ? Lerp(0.955f, 0.865f, MathF.Sin(AngleOf(state)))
        : Lerp(0.975f, 0.930f, MathF.Sin(AngleOf(state)));

    // Share of ambient a fully shadowed tile loses. Ramped in over dawn and out over dusk, and
    // over moonrise and moonset in the same shape — without it the effect would snap on at first
    // light and off at last, which is the one moment a shadow is most obviously wrong.
    public static float ShadowStrength => StrengthOf(SunState);

    private static float StrengthOf(int state)
    {
        if (state >= 0)
        {
            float d = (float)state / SunSteps;
            return MaxShadowDepth * SmoothStep(d / TwilightRamp) * SmoothStep((1f - d) / TwilightRamp);
        }
        int moon = -2 - state;
        if (moon < 0) return 0f;
        float n = (float)moon / MoonSteps;
        return MaxMoonShadowDepth
               * SmoothStep(n / MoonTwilightRamp) * SmoothStep((1f - n) / MoonTwilightRamp);
    }

    // The sky as the shading sweep reads it, at any turn, with the present weather's sunlight.
    public static SkyShade ShadeAt(int turn)
    {
        int state = SunStateAt(turn);
        var (r, g, b) = SkyAt(SnapToLadder(SunLevelAt(turn)));
        var (ux, uy) = DirectionOf(state);
        return new SkyShade(state, r, g, b, StrengthOf(state), ux, uy, DecayOf(state),
                            SAOTRPG.Systems.World.WeatherSystem.SunlightScale);
    }

    // The first turn after `turn` whose sky differs (a bearing step or a new ambient rung), or -1
    // if there is none within `horizon` turns.
    public static int NextSkyChange(int turn, int horizon)
    {
        int state = SunStateAt(turn);
        float rung = SnapToLadder(SunLevelAt(turn));
        for (int t = turn + 1; t <= turn + horizon; t++)
            if (SunStateAt(t) != state || SnapToLadder(SunLevelAt(t)) != rung) return t;
        return -1;
    }

    // ── How much a carried or placed light still counts for ───────────────────────────────────
    // A torch is near-invisible at noon. A flame at full strength whatever the hour would put the
    // player inside a disc clipped at 255 at midday, a permanent spotlight that washes out the
    // sun shadows around the only thing that can see them.
    //
    // Both curves are driven by the quantized sun level so they step with the ambient colour.
    // They differ only in floor: a carried flame really does vanish in daylight, while a campfire
    // or a lava pool is a landmark the player navigates by and keeps a visible glow.
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

    // ── The sky ladder ─────────────────────────────────────────────────────────────────────────
    // Rungs of the sun level, ascending from exactly 0 to exactly 1, laid so that the sky at any
    // two consecutive rungs differs by at most AmbientStepBudget. Greedy along a fine sampling of
    // the curve: a rung goes down at the last sample still within budget of the previous rung.
    // A pure function of the constants above, built once.
    private static readonly float[] SkyLadder = BuildSkyLadder();

    private static float[] BuildSkyLadder()
    {
        var rungs = new List<float>(192) { 0f };
        var last = SkyLab(SkyAt(0f));
        float prev = 0f;
        for (int i = 1; i <= LadderSamples; i++)
        {
            float s = (float)i / LadderSamples;
            if (DeltaE(last, SkyLab(SkyAt(s))) > AmbientStepBudget)
            {
                rungs.Add(prev);
                last = SkyLab(SkyAt(prev));
            }
            prev = s;
        }
        if (rungs[^1] != 1f) rungs.Add(1f);
        return rungs.ToArray();
    }

    private static float SnapToLadder(float s)
    {
        var l = SkyLadder;
        int i = Array.BinarySearch(l, s);
        if (i >= 0) return l[i];
        i = ~i;                                   // first rung above s
        if (i == 0) return l[0];
        if (i == l.Length) return l[^1];
        return s - l[i - 1] <= l[i] - s ? l[i - 1] : l[i];
    }

    // CIE L*a*b* of a 200-grey tile lit by the sky: what the renderer's foreground multiply puts
    // on screen for a mid tile, in the space where a difference of ~1 is the threshold of vision.
    private static (float L, float A, float B) SkyLab((float R, float G, float B) sky)
    {
        static float Lin(float c)
        {
            c = Math.Clamp(c / 255f, 0f, 1f);
            return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
        static float F(float t) => t > 216f / 24389f ? MathF.Cbrt(t) : (24389f / 27f * t + 16f) / 116f;
        float r = Lin(200f * sky.R / 255f), g = Lin(200f * sky.G / 255f), b = Lin(200f * sky.B / 255f);
        float fx = F((0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f);
        float fy = F(0.2126f * r + 0.7152f * g + 0.0722f * b);
        float fz = F((0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f);
        return (116f * fy - 16f, 500f * (fx - fy), 200f * (fy - fz));
    }

    // CIE94 (graphic-arts weights), with p as the reference. Lightness counts in full; chroma and
    // hue are discounted as the colour gets more saturated, which is what the eye does. A plain
    // Euclidean dE would spend much of the ladder on the horizon glow's hue swing, and a
    // lightness-only budget would let the hue snap between rungs. Since the lightness term is
    // unweighted, a rung never moves L* by more than the budget either.
    private static float DeltaE((float L, float A, float B) p, (float L, float A, float B) q)
    {
        float c1 = MathF.Sqrt(p.A * p.A + p.B * p.B), c2 = MathF.Sqrt(q.A * q.A + q.B * q.B);
        float dL = p.L - q.L, dC = c1 - c2, da = p.A - q.A, db = p.B - q.B;
        float dH2 = MathF.Max(0f, da * da + db * db - dC * dC);
        float sc = 1f + 0.045f * c1, sh = 1f + 0.015f * c1;
        return MathF.Sqrt(dL * dL + (dC / sc) * (dC / sc) + dH2 / (sh * sh));
    }
}

// Everything the light cache's shading sweep reads from the sky, captured once: the state, the
// ambient triple, and the shadow's strength, direction and decay, plus the weather's sunlight.
public readonly record struct SkyShade(int SunState, float AmbR, float AmbG, float AmbB,
                                       float Strength, float Ux, float Uy, float Decay, float SunlightScale)
{
    public bool SunIsUp => SunState >= 0;
}
