using Terminal.Gui;

namespace SAOTRPG.Systems;

// FB-450 particle effect types. Each enqueues a spawn pattern at a tile.
public enum ParticleEvent
{
    SwordSlash,
    CritShatter,
    MonsterDeath,
    SkillCastStart,
    ItemPickup,
    PoisonInflict,
    BleedInflict,
    BurnInflict,
    LevelUp,
    HealingTick,
    EnhancementSuccess,
    FloorTransition,
    // Bundle 13 (Item 2) — Divine Awakening burst. Level 1/2/3 scales count + duration.
    DivineAwakening,
}

// A single live particle — position, glyph, color, velocity, lifetime. Mutable for ring-buffer reuse.
public sealed class Particle
{
    public float X;
    public float Y;
    public char Glyph;
    public Color Color;
    public float AgeMs;
    public float DurationMs;
    public float Vx;
    public float Vy;
    public Color FadeTo;
    public bool UseFade;
    // True while this slot holds a live particle. Pool slots toggle this on Push and on expiry in Tick.
    public bool Active;
}

// Pre-allocated particle pool with oldest-drop wrap on overflow. Cap 40 concurrent per research §6.
public static class ParticleQueue
{
    public const int MaxConcurrent = 40;
    private static readonly Particle[] _pool = BuildPool();
    private static int _writeIdx;
    private static int _activeCount;

    private static Particle[] BuildPool()
    {
        var arr = new Particle[MaxConcurrent];
        for (int i = 0; i < MaxConcurrent; i++) arr[i] = new Particle();
        return arr;
    }

    // Iterator wrapper exposing only active slots (filters per yield, no allocation).
    public static IEnumerable<Particle> Active
    {
        get
        {
            for (int i = 0; i < _pool.Length; i++)
                if (_pool[i].Active) yield return _pool[i];
        }
    }

    // O(1) liveness probe — frame cache reads this to decide whether to keep dirty.
    public static bool HasAny => _activeCount > 0;

    public static void Clear()
    {
        for (int i = 0; i < _pool.Length; i++) _pool[i].Active = false;
        _activeCount = 0;
        _writeIdx = 0;
    }

    // F9 hot-reload latch reset — conservative full clear since Particle has no
    // Ambient category flag today. AmbientOverlayPass reseeds on map entry.
    public static void ClearAmbient()
    {
        int before = _activeCount;
        Clear();
        UI.DebugLogger.LogGame("RELOAD", $"ParticleQueue.ClearAmbient dropped {before}");
    }

    // Pull next slot in the ring. If full, overwrite the next slot (oldest-drop semantic).
    private static Particle NextSlot()
    {
        var p = _pool[_writeIdx];
        _writeIdx = (_writeIdx + 1) % MaxConcurrent;
        if (!p.Active) _activeCount++;
        p.Active = true;
        p.AgeMs = 0f;
        return p;
    }

    // Push a particle. Initializes a pool slot in-place — zero allocation per emit.
    private static void Push(float x, float y, char glyph, Color color, Color fadeTo, bool useFade,
        float durMs, float vx, float vy)
    {
        var slot = NextSlot();
        slot.X = x; slot.Y = y;
        slot.Glyph = glyph;
        slot.Color = color;
        slot.FadeTo = fadeTo;
        slot.UseFade = useFade;
        slot.DurationMs = durMs;
        slot.Vx = vx; slot.Vy = vy;
        UI.MapView.MarkFrameDirty();
    }

    // Per-biome ambient particle table: glyph, color, fade-to color, drift velocity, duration.
    // Keyed by particleId strings that appear in Content/Biomes/*.json ambientOverlay.particleId.
    private static readonly Dictionary<string, (char Glyph, Color Color, Color FadeTo, float Vx, float Vy, float DurMs)>
        _ambientSpecs = new()
    {
        ["wind_motes"]   = ('·', new Color(200, 200, 200), Color.DarkGray, 0.001f,  0f,       30000f),
        ["firefly"]      = ('.', Color.BrightYellow,       Color.Yellow,   0f,      -0.0005f, 30000f),
        ["fog_mote"]     = ('·', new Color(160, 180, 160), Color.DarkGray, 0.0005f, 0f,       30000f),
        ["sand_drift"]   = ('\'', new Color(220, 200, 120), Color.DarkGray, 0.002f,  0f,       30000f),
        ["snowflake"]    = ('*', Color.White,              Color.Gray,     0f,      0.001f,   30000f),
        ["ember"]        = ('*', new Color(255, 120, 40),  Color.Red,      0f,      -0.001f,  30000f),
        ["rain_mote"]    = ('\'', Color.Cyan,               Color.DarkGray, 0f,      0.002f,   30000f),
        ["dust_mote"]    = ('·', new Color(180, 160, 120), Color.DarkGray, 0.0003f, 0f,       30000f),
        ["shadow_wisp"]  = ('·', new Color(100, 80, 120),  Color.Black,    0f,      0f,       30000f),
        ["lantern_glow"] = ('·', new Color(255, 200, 100), Color.DarkGray, 0f,      0f,       30000f),
        ["void_spark"]   = ('*', new Color(180, 100, 220), Color.Black,    0.0005f, 0.0005f,  30000f),
    };

    // Seed ambient particles across walkable tiles. Caller computes `count` from
    // biome densityPermille * UserSettings multiplier. No-op if id unknown or count <= 0.
    public static void SeedAmbient(string particleId, IList<(int X, int Y)> tiles, Random rng, int count)
    {
        if (count <= 0 || tiles.Count == 0) return;
        if (!_ambientSpecs.TryGetValue(particleId, out var spec)) return;
        int cap = Math.Min(count, Math.Max(0, MaxConcurrent - 5));
        for (int i = 0; i < cap; i++)
        {
            var (tx, ty) = tiles[rng.Next(tiles.Count)];
            Push(tx, ty, spec.Glyph, spec.Color, spec.FadeTo, true, spec.DurMs, spec.Vx, spec.Vy);
        }
    }

    // Per-density tuning. Count scales + duration scales. Subtle has no hold.
    private static (int countMin, int countMax, float duration) Tuning()
    {
        return UserSettings.Current.ParticleDensity switch
        {
            ParticleDensity.Off => (0, 0, 0f),
            ParticleDensity.Subtle => (1, 3, 175f),
            ParticleDensity.Moderate => (3, 6, 400f),
            _ => (5, 10, 800f),
        };
    }

    // Primary entry — fan out to per-event spawn logic, honoring density scaling.
    public static void Emit(ParticleEvent ev, int x, int y,
        int? dirX = null, int? dirY = null, Color? tint = null)
    {
        var (cMin, cMax, dur) = Tuning();
        if (cMax == 0) return;

        switch (ev)
        {
            case ParticleEvent.SwordSlash: EmitSwordSlash(x, y, dirX ?? 0, dirY ?? 0, dur); break;
            case ParticleEvent.CritShatter: EmitCritShatter(x, y, cMax, dur); break;
            // MonsterDeath now routed through MapView.EmitMonsterDeathBurst (two-phase braille).
            // Kept for enum/back-compat; old ring-buffer path retired.
            case ParticleEvent.MonsterDeath: break;
            case ParticleEvent.SkillCastStart: EmitSkillCast(x, y, dur, tint); break;
            case ParticleEvent.ItemPickup: EmitItemPickup(x, y, dur); break;
            case ParticleEvent.PoisonInflict: EmitPoisonInflict(x, y, cMax, dur); break;
            case ParticleEvent.BleedInflict: EmitBleedInflict(x, y, cMax, dur); break;
            case ParticleEvent.BurnInflict: EmitBurnInflict(x, y, cMax, dur); break;
            case ParticleEvent.LevelUp: EmitLevelUp(x, y, dur); break;
            case ParticleEvent.HealingTick: EmitHealingTick(x, y, dur); break;
            case ParticleEvent.EnhancementSuccess: EmitEnhancement(x, y, dur); break;
            case ParticleEvent.FloorTransition: EmitFloorTransition(x, y, dur); break;
            case ParticleEvent.DivineAwakening: EmitDivineAwakening(x, y, 1); break;
        }
    }

    // Bundle 13 (Item 2) — Divine Awakening burst keyed by AwakeningLevel.
    // Level 1 = 3 particles / 600ms, Level 2 = 6 / 900ms, Level 3 = 12 / 1200ms.
    // Density scaling overrides count in Subtle/Moderate; Pronounced uses base.
    public static void EmitDivineAwakening(int x, int y, int level)
    {
        if (UserSettings.Current.ParticleDensity == ParticleDensity.Off) return;
        int baseCount = level switch { 1 => 3, 2 => 6, _ => 12 };
        float dur     = level switch { 1 => 600f, 2 => 900f, _ => 1200f };
        int n = ScaleCount(baseCount);
        char[] glyphs = { '✦', '◈', '*', '·' };
        Color[] palette = { Color.BrightMagenta, Color.BrightYellow };
        for (int i = 0; i < n; i++)
        {
            double angle = (2 * Math.PI * i) / Math.Max(1, n);
            float vx = (float)(Math.Cos(angle) * 0.0035f);
            float vy = (float)(Math.Sin(angle) * 0.0035f) - 0.0008f;
            Push(x, y, glyphs[i % glyphs.Length], palette[i % palette.Length],
                Color.DarkGray, true, dur, vx, vy);
        }
    }

    private static int ScaleCount(int baseCount)
    {
        return UserSettings.Current.ParticleDensity switch
        {
            ParticleDensity.Subtle => Math.Max(1, baseCount / 3),
            ParticleDensity.Moderate => Math.Max(1, baseCount * 2 / 3),
            _ => baseCount,
        };
    }

    // 3-cell arc in swing direction. Glyphs ' ` , . White→Gray fade.
    private static void EmitSwordSlash(int x, int y, int dx, int dy, float dur)
    {
        int n = ScaleCount(3);
        char[] glyphs = { '\'', '`', ',' };
        float vx = dx * 0.004f, vy = dy * 0.004f;
        for (int i = 0; i < n; i++)
            Push(x + dx * 0.3f, y + dy * 0.3f, glyphs[i % glyphs.Length], Color.White,
                Color.Gray, true, dur * 0.6f, vx, vy);
    }

    // Ring of 6-8 ◇ around target. Yellow → Gray fade.
    private static void EmitCritShatter(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(6));
        for (int i = 0; i < n; i++)
        {
            double angle = (2 * Math.PI * i) / n;
            float vx = (float)(Math.Cos(angle) * 0.002f);
            float vy = (float)(Math.Sin(angle) * 0.002f);
            Push(x, y, '◇', Color.BrightYellow, Color.Gray, true, dur, vx, vy);
        }
    }

    // 8-particle ring around caster, stationary, tinted. Cyan-default.
    private static void EmitSkillCast(int x, int y, float dur, Color? tint)
    {
        int n = ScaleCount(8);
        Color c = tint ?? Color.BrightCyan;
        for (int i = 0; i < n; i++)
        {
            double angle = (2 * Math.PI * i) / n;
            Push(x + (float)Math.Cos(angle), y + (float)Math.Sin(angle), '·', c,
                default, false, dur, 0, 0);
        }
    }

    // 3-star burst on item tile. BrightYellow → Yellow.
    private static void EmitItemPickup(int x, int y, float dur)
    {
        int n = ScaleCount(3);
        float[] vxs = { -0.002f, 0.002f, 0f };
        float[] vys = { -0.002f, -0.002f, -0.003f };
        for (int i = 0; i < n; i++)
            Push(x, y, '✦', Color.BrightYellow, Color.Yellow, true, dur * 0.5f, vxs[i % 3], vys[i % 3]);
    }

    private static void EmitPoisonInflict(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(4));
        for (int i = 0; i < n; i++)
        {
            float jx = (i % 2 == 0 ? -0.001f : 0.001f);
            Push(x, y, '·', Color.BrightGreen, Color.Gray, true, dur, jx, -0.002f);
        }
    }

    // SAO theme: bleed is WHITE/CYAN ◇, not red blood.
    private static void EmitBleedInflict(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(3));
        for (int i = 0; i < n; i++)
        {
            float jx = (i - 1) * 0.001f;
            Push(x, y, '◇', Color.BrightCyan, Color.White, true, dur * 0.75f, jx, 0.002f);
        }
    }

    private static void EmitBurnInflict(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(3));
        for (int i = 0; i < n; i++)
        {
            float jx = (i - 1) * 0.0008f;
            Push(x, y, '\'', Color.BrightRed, Color.BrightYellow, true, dur * 0.75f, jx, -0.003f);
        }
    }

    // Rising column of . * from player tile — levelup pulse.
    private static void EmitLevelUp(int x, int y, float dur)
    {
        int n = ScaleCount(7);
        char[] glyphs = { '·', '*', '·' };
        for (int i = 0; i < n; i++)
            Push(x, y, glyphs[i % glyphs.Length], Color.White, Color.BrightCyan, true,
                dur, 0, -0.003f - (i * 0.0005f));
    }

    private static void EmitHealingTick(int x, int y, float dur)
    {
        int n = ScaleCount(2);
        for (int i = 0; i < n; i++)
        {
            float jx = (i == 0 ? -0.001f : 0.001f);
            Push(x, y, i == 0 ? '+' : '·', Color.BrightGreen, default, false, dur * 0.5f, jx, -0.001f);
        }
    }

    private static void EmitEnhancement(int x, int y, float dur)
    {
        int n = ScaleCount(8);
        char[] glyphs = { '◇', '✦' };
        for (int i = 0; i < n; i++)
        {
            double angle = (2 * Math.PI * i) / n;
            float vx = (float)(Math.Cos(angle) * 0.003f);
            float vy = (float)(Math.Sin(angle) * 0.003f);
            Push(x, y, glyphs[i % 2], Color.BrightMagenta, Color.Magenta, true, dur, vx, vy);
        }
    }

    private static void EmitFloorTransition(int x, int y, float dur)
    {
        int n = ScaleCount(5);
        for (int i = 0; i < n; i++)
            Push(x, y, i % 2 == 0 ? ':' : '.', Color.BrightCyan, Color.Cyan, true,
                dur * 0.8f, 0, -0.002f - i * 0.0003f);
    }

    // Ticks every particle by dtMs; culls expired by toggling Active=false (slot reused on next Push).
    public static void Tick(int dtMs)
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            var p = _pool[i];
            if (!p.Active) continue;
            p.AgeMs += dtMs;
            p.X += p.Vx * dtMs;
            p.Y += p.Vy * dtMs;
            if (p.AgeMs >= p.DurationMs)
            {
                p.Active = false;
                if (_activeCount > 0) _activeCount--;
            }
        }
    }
}
