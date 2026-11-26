namespace SAOTRPG.Map.Lighting;

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
            if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.StarlessNight)) return 0f;
            float phase = (float)(CurrentTurn % CycleLength) / CycleLength;
            return 0.5f + 0.5f * (float)Math.Cos(phase * 2.0 * Math.PI);
        }
    }

    // The sun level is snapped to a LADDER of rungs before it becomes ambient light. The light
    // grid is the map renderer's change-detection input: an unquantized ambient shifts every lit
    // cell by a fraction of a colour channel on every single turn, which would mark the whole
    // viewport changed and defeat incremental tile rendering.
    //
    // THE LADDER IS PERCEPTUAL, NOT UNIFORM. It used to be `AmbientSteps = 128` equal steps of
    // the sun level (`MathF.Round(SunLevel * AmbientSteps) / AmbientSteps`), sized so that the
    // worst whole-screen jump between two rungs was 0.66 dL* on a mid tile — under a
    // just-noticeable difference, where 64 steps measured 1.32. A uniform step spends the same
    // budget on the flat night as on twilight, though, and a proper twilight moves the sky by
    // tens of dE in a twentieth of the cycle, which 128 equal steps cannot carry under the line.
    // The rungs are placed where the sky itself moves instead: consecutive rungs differ by at
    // most AmbientStepBudget in CIE dE76 on a 200-grey tile (the metric the light checks
    // measure with), so twilight gets dense rungs, the flat night gets none, and the worst
    // visible jump is the same everywhere on the ladder.
    //
    // Every rung costs one light-cache refresh — the cache keys on the ambient triple and every
    // change re-sweeps the padded region and re-resolves every lit cell — so the rung count is a
    // churn budget. Measured over a cycle: 113 rungs against the uniform ladder's 129 levels,
    // 224 ambient refreshes a day against 256. Forty-one of them cover the whole daylight half,
    // the flat night takes one, and the rest sit in the two twilights, one every six turns or
    // so, which is where the sky is actually moving.
    //
    // 0.66 IS THE UNIFORM LADDER'S OWN WORST STEP, kept rather than the 1.0 the checks allow, so
    // no rung steps further than the sky already did. Measured on the same mid tile: worst
    // rung 0.65 dL*, and the worst hue step between rungs fell from 2.2 to 1.1 b*.
    private const float AmbientStepBudget = 0.66f;

    // How finely the curve is sampled when the rungs are laid. A turn moves the sun level by at
    // most pi / (2 x CycleLength), about 1.6 samples, so a rung can never be skipped in one turn.
    private const int LadderSamples = 4096;

    // Sight is quantized SEPARATELY and more coarsely, and that is the whole reason the two are
    // not one number. A colour step costs a re-resolve; a radius step costs a full shadowcast,
    // because the visible set is only reused when the radius matches. Putting sight on a ladder
    // twice this fine took radius changes from 129 a day to 257, most of the way back to the 341
    // the raw sun level gives, and would undo the measured 20% saving on still turns. Sight
    // stays on this uniform ladder: it decides what is EXPLORED, and the sky ladder above is
    // free to put its rungs wherever the colour moves.
    private const int RadiusSteps = 64;

    // The sun level snapped to the nearest rung of the sky ladder. Anything that scales with
    // daylight reads THIS rather than SunLevel, so it steps in lockstep with the ambient colour
    // instead of adding a second, finer churn source to the renderer's change detection.
    public static float QuantizedSunLevel => SnapToLadder(SunLevel);

    // The coarser level sight steps on. Everything that decides a COLOUR reads QuantizedSunLevel.
    public static float RadiusSunLevel => MathF.Round(SunLevel * RadiusSteps) / RadiusSteps;

    // GOLDEN HOUR. The base curve below is a straight lerp from moonlit blue to daylight white,
    // which makes dawn and dusk nothing but a DIMMER NOON — and those are the hours the sun sits
    // on the horizon, throws its longest shadows and swings hardest across the compass, so they
    // were the most interesting moments on the map and the least interesting colour.
    //
    // A warm bump peaking just above the horizon fixes that, and it pairs with the shadow's cool
    // tint: warm direct light against blue shade is what makes low sun read as low sun rather
    // than as the brightness being turned down. It tapers to nothing by deep night and by high
    // day, so noon stays neutral and midnight stays blue.
    //
    // Driven by the QUANTIZED sun level like everything else here, so it steps with the ambient
    // colour rather than adding a second, finer churn source to the renderer's change detection.
    private const float GoldenPeak = 0.55f;   // sun level where the warmth is strongest
    private const float GoldenBand = 0.32f;   // half-width; zero outside it on the daylight side
    private const float GoldenR = 46f, GoldenG = 9f, GoldenB = -30f;

    // The two ends of the daylight lerp. Night: cool moonlit blue. Day: warm off-white.
    private const float FloorR = 18f, FloorG = 22f, FloorB = 45f;
    private const float NoonR = 180f, NoonG = 175f, NoonB = 150f;

    // TWILIGHT. The night half used to be the same lerp run on down to midnight, with the golden
    // band reaching to sun level 0.23. Measured over a cycle, that put the sky at L* 49 when the
    // sun set (noon is 71), kept it fading for the whole first half of the night, made it
    // darkest with the moon at its zenith and brightest at moonrise, and left it orange
    // (red-over-blue 1.39) 250 turns after sunset with the moon thirty degrees up. Moonlight was
    // never in it: the whole night was the sun's after-glow on a cosine.
    //
    // The night is built from the sun's DEPTH below the horizon now — 0 at sunset, 1 at the
    // nadir, which for a full moon opposite the sun is also the moon's elevation — so the
    // ambient stays a pure function of the quantized sun level and nothing new reaches the
    // cache key. Two terms fade at two rates, the way twilight does: the horizon GLOW dies
    // first, the neutral SKYLIGHT after it, and past that the sky sits on the moonlit floor
    // until dawn's mirror. The horizon colour and the floor are exactly the shipped ones — the
    // day half is untouched and the night joins it at the horizon by construction — so only the
    // hours between them moved. The floor is the anchor the night renderer is tuned against and
    // is deliberately not lifted or deepened here.
    //
    // PATH-D-PORT: the sky is one colour per turn, a pure function of the quantized sun level;
    // a renderer takes the triple as given and adds no direction to it.
    //
    // Depth by which the horizon glow has faded to nothing — civil twilight, about 77 turns.
    // Old: the golden band ran to sun level 0.23, a depth of 0.54, 360 turns after sunset.
    private const float TwilightGlowDepth = 0.12f;
    // Depth by which the skylight has faded to the floor. DERIVED, not chosen: it is the depth
    // at which the rules below call it night (RadiusSunLevel at NightThreshold, i.e. sun level
    // 0.20), so the sky reaches its floor on the turn the sidebar turns to Night and sight
    // collapses — about 417 turns after sunset, against a real astronomical twilight's ~280.
    // Old: 1.0, the sky fading all the way to midnight.
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
    // Reads a QUANTIZED sun level — RadiusSunLevel, the coarser of the two. It used to read the
    // raw one, which made the integer radius change on 8.5% of a day's turns against 3.2%
    // quantized — and the visible set is only reused when the radius matches, so about one still
    // turn in twenty paid a full shadowcast for a sun step nobody could see.
    //
    // This was deferred once as gameplay-adjacent, on the grounds that it decides what the player
    // can SEE and an FOV-radius change has been reverted here before. Measuring settled it: the
    // RANGE is identical at every viewport size (18-188, 18-128, 18-68), the mean radius is
    // identical to two decimals, and the worst instantaneous difference is 2 tiles of 188. The
    // explore ceiling is set by the maximum reach, and the maximum reach does not move.
    public const int MinVisibility = 18;
    public static int VisibilityRadius =>
        (int)Math.Round(Lerp(MinVisibility, FovRadius, RadiusSunLevel));

    // ── What the hour does to the GAME, rather than to the picture ─────────────────────────
    // Everything else in this file decides a colour, a radius or a shadow. This decides
    // DIFFICULTY, and it is deliberately the only thing here that does.
    //
    // NIGHT IS A BUCKET, NOT A CURVE, and it reads RadiusSunLevel — the coarser quantized level,
    // the one VisibilityRadius already steps on. Two reasons, and the second is the load-bearing
    // one. A gameplay term driven by the raw sun level would move on nearly every turn, which is
    // the churn the quantization exists to prevent; and reading the same level as sight means the
    // hour the player's vision collapses is exactly the hour the world turns hostile, rather than
    // the two drifting apart by a few turns for no reason anybody can see.
    //
    // PhaseName BELOW READS THIS RATHER THAN CARRYING ITS OWN THRESHOLD, and that is not tidiness
    // either: PhaseName is the string the sidebar prints. If the printed phase and the phase the
    // rules consult could disagree for even one turn, the game would be charging a night penalty
    // while telling the player it is dusk. One predicate makes that disagreement unrepresentable.
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
    // Ambient above is the sky: one colour everywhere, no direction, so a forest canopy lit the
    // ground exactly as brightly as the clearing beside it. This is the other half — a sun with a
    // BEARING, so anything that blocks sight throws a shadow away from it, and that shadow
    // stretches and swings through the day. The moon further down does the same for the night.
    //
    // Everything below derives from ONE quantized integer, SunState, for the same reason Ambient
    // snaps to AmbientSteps: the light grid is the map renderer's change-detection input. A sun that
    // rotated continuously would move every shadowed cell on every single turn and defeat
    // incremental tile rendering outright. Deriving direction, reach AND strength from that one
    // integer is what makes the guarantee checkable: if SunState has not changed, no shadow has
    // moved.
    //
    // A BEARING STEP IS THE LARGEST TEMPORAL JUMP ON THE MAP — most of the viewport changes at
    // once — so the count is a banding budget as well as a churn one, and the curve is measured.
    // dL* on a mid tile with the ambient DIVIDED OUT, so the sun is not charged for the sky's own
    // fading; sampled over a 316x91 viewport at an OPEN position on F7 Forest.
    //
    //   steps   worst ordinary step   the step across 45 deg   states/day
    //      24            12.69                    -                39
    //      48             6.54                  8.24               63
    //      96             3.59                  5.66              111
    //     192             1.89                  4.33              207
    //     480             0.78                  4.15              495
    //
    // THE MIDDLE COLUMN WAS THE WHOLE STORY, AND IT IS NOT A QUANTIZATION STEP AT ALL. The sweep
    // runs along the sky's major axis and swaps axes at 45 and 135 degrees, and the two
    // rasterisations of one diagonal do not agree — so the field was DISCONTINUOUS in the
    // bearing, snapping twice a day by an amount that converged to 4.15 dL* however fine the
    // ladder got. Refinement could not reach it: at 480 steps the ordinary step was 0.78 and the
    // seam was still 4.15, six times its neighbours. LightingSystem.Shading blends the two
    // rasterisations through the seam now; measured after that fix, the same 480-step seam step
    // is 0.44 dL* and puts ZERO cells over a just-noticeable difference, against 1,034 before.
    //
    // 192 IS THE SHIPPED VALUE, and the two things that decided it are separate. Fixing the
    // seam is what made refining worth anything at all: before the blend the ordinary step
    // halved with every doubling while the seam sat at 4.15 whatever the ladder did, so a finer
    // sun bought a smoother field around a discontinuity that stayed the worst thing on screen.
    // With the seam an ordinary step, 192 puts the worst ordinary step at 1.89 dL* — and
    // measured over a 316x91 viewport, an ordinary bearing step at this ladder moves ZERO cells
    // past a just-noticeable difference, which 96 does not.
    //
    // IT WAS HELD AT 96 BY A BROKEN METRIC, and that is worth keeping written down because the
    // constraint was manufactured rather than found. SeedProbe's
    // light/the-penumbra-straddles-the-shadow measured 0.32 of a cell at 192 against its 0.30 bar
    // while 96 measured 0.28, so the four-tap penumbra kernel was blamed and widening it was
    // called the prerequisite. It was not the kernel. That metric weighted each sample by its
    // INDEX in a scan across the shadow, and a sample's index is not its distance from the spine:
    // the spine falls between columns, by up to half a cell, and the whole scan was reported as
    // leaning by that amount. A finer ladder simply lands more bearings where the offset is
    // largest, so the reading climbed with the step count while the renderer did not change.
    // Proven by running the same metric over a field symmetric BY CONSTRUCTION, which scored
    // 0.30 — the check was failing a renderer that was provably right. Weighted by where each
    // cell actually sits, the shipped renderer reads 0.16 at 48 bearings, 0.16 at 96 and 0.16 at
    // 192: flat, as a property of the kernel should be.
    //
    // WHAT IT COSTS, measured on F7 Forest in Release. The light cache rebuilds 456 times over a
    // 4,000-turn still day against 354 at 96, and 458 against 356 while walking — +28.8% either
    // way, and deterministic. A rebuild sweeps the padded region at about 1.4 ms, so the bill is
    // roughly +0.02 to +0.04 ms per turn against a turn costing 1 to 4 ms. Min of three
    // alternated builds: still 0.2353 -> 0.2721 ms/turn, walking 0.2372 -> 0.2545. Both sit
    // inside this machine's own +/-30% run-to-run spread, which is why the walking figure came
    // out at half the still one.
    //
    // AND IT COST THREE CHECKS A RE-TUNE, each a resolution floor rather than a regression.
    // light/the-shadow-falls-away-from-the-sun compares two cells with an absolute 0.02 margin,
    // and a finer ladder lands bearings in the twilight ramp whose ENTIRE available signal is
    // 0.051 — the shadow is still on the correct side there (0.017 against 0.000) and simply
    // cannot clear a fixed margin, so those bearings moved to a scale-free share of their own
    // strength rather than being dropped. And the 45-degree seam check counted cells crossing a
    // just-noticeable difference, which at this ladder is zero for the seam AND for its
    // neighbours; it divides mean dL* now, which is well defined at any ladder and separates the
    // blend being present from it being absent by 0.79 against 2.07.
    private const int SunSteps = 192;

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

    // The quantized state of whichever body is in the sky. Every shadow parameter is a pure
    // function of this, so it doubles as the renderer's "has anything moved" stamp.
    //
    // NON-NEGATIVE is the sun's own bearing step. NEGATIVE is the night: -1 when nothing
    // directional is up at all, and -2 - MoonState for each of the moon's bearings.
    //
    // THE MOON HAS TO LIVE IN THIS ONE INTEGER, and that is not tidiness. LightingSystem.Cache
    // keys the entire ambient + shadow + contact field on this value, so a moon bearing the key
    // could not see would leave the map lit by an earlier moon for the rest of the night — and
    // stale light looks like light. Folding the night in here means the existing key covers it by
    // construction, rather than by a second field somebody has to remember to add.
    public static int SunState
    {
        get
        {
            if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.StarlessNight)) return -1;
            float d = DayFraction;
            if (d <= 1f) return (int)MathF.Round(d * SunSteps);
            // The sun is down and the run is not starless, so the moon is up and MoonState >= 0.
            return -2 - MoonState;
        }
    }

    public static bool SunIsUp => SunState >= 0;

    // Sun bearing as an angle in [0, pi]: 0 at dawn (sun due east), pi/2 at noon, pi at dusk.
    private static float SunAngle => MathF.PI * SunState / SunSteps;

    // Sun elevation in [0,1] from the same angle — 0 on the horizon, 1 at noon.
    private static float SunElevation => MathF.Sin(SunAngle);

    // ── The directional moon ───────────────────────────────────────────────────────────────────
    // The night used to be flat. ShadowStrength opened by returning zero whenever the sun was
    // down, so half of every cycle carried no relief at all: ambient plus point lights, and the
    // only edge on screen was the torch's.
    //
    // A full moon sits opposite the sun, so it rises in the east as the sun sets, stands south at
    // midnight and sets in the west as the sun comes back up — the same half-circle, half a cycle
    // later. That is what lets it feed the SAME sweep rather than a second one: direction, decay
    // and strength below all answer for whichever body is up, and the shading pass has to know
    // only which per-channel loss to spend.
    //
    // PATH-D-PORT: the night's visual contract is long, faint, COOL shadows laid away from the
    // moon — an order of magnitude weaker than the day's, over an ambient an order of magnitude
    // darker, so the two read at similar contrast. A renderer that scales the night shadow by the
    // same physical share as the day's will draw nothing at all.
    //
    // TWELVE STEPS ACROSS THE NIGHT, and the count is measured rather than chosen. Re-measured
    // against the sun's own curve above, with the ambient divided out and the night re-lit in its
    // OWN sky rather than a noon reference: the worst moon bearing step is 0.96 dL*, already
    // under a just-noticeable difference, so refining it would buy nothing and cost a light-cache
    // refresh per extra step. The sun's is 2.33 at 24 and 1.52 at 48, which is why only that half
    // moved. A step count is a churn budget: every step moves
    // cells the renderer's change detection has to absorb, and costs a light-cache refresh.
    // Moonlight here is about a tenth of daylight, so a bearing step twice as coarse in angle
    // still moves the light grid LESS than a sun step does. Measured on a real floor, comparing
    // only state pairs whose ambient barely moves — otherwise the comparison charges the moon for
    // the sky's own fading — one moon step moves a channel by at most 6.9 of 255 against the
    // sun's 15.7. Going finer buys a smoothness nobody can see and pays a refresh for it.
    private const int MoonSteps = 12;

    // Deepest a full moon shadow may take a cell, as a share of ambient. Lower than the sun's,
    // but NOT by the ratio of the two ambients: night ambient runs 18-45 per channel, so a share
    // small enough to be physically faithful would be seen nowhere at all.
    //
    // THIS WAS RECORDED AS AN OPEN DECISION ON THE GROUNDS THAT THE CONSTRAINT HAD MOVED — the
    // depth was floored when the renderer quantized lit colour in steps of 8, and at a step of 2 a
    // shallower moon became REPRESENTABLE. It is representable and it is not visible, which is the
    // measurement that settles it. Taking the deepest reachable moon shadow (a 9x9 wall caster, so
    // the arriving shadow is ~0.9 rather than a lone tree's ~0.35) and reading it as the renderer
    // draws it, in dL* on a mid-grey glyph against open moonlit ground:
    //
    //   depth    at the middle of the night    at the deepest twilight bearing
    //   0.510            2.45                            10.67
    //   0.340            1.79                             6.87      <- shipped
    //   0.255            1.57                             5.08
    //   0.170            0.96                             3.71
    //   0.085            0.26                             1.73
    //
    // A just-noticeable difference is about 1.0 dL*. So at 0.34 the moon's deepest shadow is
    // ALREADY within 1.8x of invisibility in the hours when directional shading is the only
    // structure on the screen, and an ordinary lone-tree shadow there measures 0.8-1.1 — at or
    // under the line already. Halving the depth would delete the night's relief exactly where it
    // is doing the most work. KEPT AT 0.34; the constraint is perceptual, the quantizer change did
    // not loosen it, and the direction with any room in it is deeper rather than shallower.
    //
    // The foreground is the surface that has to carry it, and it is also the generous one: the
    // background glow is light x BgGlowScale, so the same shadow reads 0.16-1.05 dL* there.
    private const float MaxMoonShadowDepth = 0.34f;

    // Ramped in over moonrise and out over moonset, the way the sun's ramps over dawn and dusk.
    // Both are near zero at the handover, so neither body snaps its shadows on — which is also
    // what hides the 180-degree turn the shadows take there, since a full moon rises where the
    // sun has just set.
    //
    // A DIAL WITH A JUDGEMENT IN IT. Ambient falls faster than this ramp rises, so the deepest
    // shadow in ABSOLUTE light units lands in early twilight (28 of 255 at the third bearing)
    // rather than at the middle of the night (6). By the measure the renderer actually applies —
    // the SHARE of ambient a cell loses, which is what multiplies the tile colour — the ordering
    // is the expected one: 0.22 in twilight against 0.34 at the deepest. Widening this to 0.35
    // would hold the moon faint until the sky is genuinely dark, at the cost of a duller first
    // half of the night.
    private const float MoonTwilightRamp = 0.20f;

    // Position within the night: 0 at dusk, 1 at dawn, at or below 0 while the sun has the sky.
    private static float NightFraction => DayFraction - 1f;

    // The quantized moon, or -1 when there is none: the sun has the sky, or the run is starless.
    //
    // THE BOUNDARY BELONGS TO THE SUN. At NightFraction exactly 0 the sun is still up and
    // SunState is still reporting its last daylight bearing, so a moon there would give one turn
    // two sky bodies at once — and, worse, put a POSITIVE daylight state inside the range the
    // light cache treats as night. Both halves read the boundary off the same DayFraction, so
    // they cannot disagree about where the day ends.
    public static int MoonState
    {
        get
        {
            if (SAOTRPG.Systems.RunState.RunModifiers.IsActive(SAOTRPG.Systems.RunState.RunModifier.StarlessNight)) return -1;
            float n = NightFraction;
            if (n <= 0f) return -1;
            return (int)MathF.Round(n * MoonSteps);
        }
    }

    // Moon bearing as an angle in [0, pi]: 0 at moonrise (due east), pi/2 at midnight, pi at
    // moonset. The sun's own convention, so the shared direction below needs no second case.
    private static float MoonAngle => MathF.PI * MoonState / MoonSteps;

    // Moon elevation in [0,1] — 0 on the horizon, 1 at midnight.
    private static float MoonElevation => MathF.Sin(MoonAngle);

    // ── What the sweep reads ───────────────────────────────────────────────────────────────────
    // Unit vector along which shadows FALL, in screen coordinates (y grows downward).
    // Dawn -> west, noon -> north, dusk -> east; then moonrise -> west, midnight -> north,
    // moonset -> east. A shadow that swings a half-circle over the day, and again over the night,
    // and is diagonal for all but six instants of the whole cycle.
    public static (float X, float Y) ShadowDirection
    {
        get
        {
            float a = SunIsUp ? SunAngle : MoonAngle;
            return (-MathF.Cos(a), -MathF.Sin(a));
        }
    }

    // Shadow retained per tile travelled. A low sun casts a long shadow, so it decays slowly;
    // at noon the shadow is a short pool under the thing casting it.
    //
    // The moon's range is uniformly slower, which is what makes a night shadow LONG. It also has
    // to be: at a tenth of the day's amplitude a shadow on the day's decay would fall under the
    // renderer's colour quantizer within a couple of cells and read as a smudge at the caster's
    // foot rather than as a shadow lying across the ground.
    public static float ShadowDecay => SunIsUp
        ? Lerp(0.955f, 0.865f, SunElevation)
        : Lerp(0.975f, 0.930f, MoonElevation);

    // Share of ambient a fully shadowed tile loses. Ramped in over dawn and out over dusk, and
    // over moonrise and moonset in the same shape — without it the effect would snap on at first
    // light and off at last, which is the one moment a shadow is most obviously wrong.
    public static float ShadowStrength
    {
        get
        {
            if (SunIsUp)
            {
                float d = (float)SunState / SunSteps;
                return MaxShadowDepth * SmoothStep(d / TwilightRamp) * SmoothStep((1f - d) / TwilightRamp);
            }
            int moon = MoonState;
            if (moon < 0) return 0f;
            float n = (float)moon / MoonSteps;
            return MaxMoonShadowDepth
                   * SmoothStep(n / MoonTwilightRamp) * SmoothStep((1f - n) / MoonTwilightRamp);
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
    // hue are discounted as the colour gets more saturated, which is what the eye does — a
    // plain Euclidean dE spent half the ladder on the horizon glow's hue swing, where a
    // lightness-only budget would have let the hue snap by 4 b* a rung. Since the lightness
    // term is unweighted, a rung never moves L* by more than the budget either.
    private static float DeltaE((float L, float A, float B) p, (float L, float A, float B) q)
    {
        float c1 = MathF.Sqrt(p.A * p.A + p.B * p.B), c2 = MathF.Sqrt(q.A * q.A + q.B * q.B);
        float dL = p.L - q.L, dC = c1 - c2, da = p.A - q.A, db = p.B - q.B;
        float dH2 = MathF.Max(0f, da * da + db * db - dC * dC);
        float sc = 1f + 0.045f * c1, sh = 1f + 0.015f * c1;
        return MathF.Sqrt(dL * dL + (dC / sc) * (dC / sc) + dH2 / (sh * sh));
    }
}
