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
}

// A single live particle — position, glyph, color, velocity, lifetime.
// Velocity is cells-per-second at Pronounced density; duration scales tier.
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
}

// Global oldest-drop particle ring. Cap 40 concurrent per research §6.
public static class ParticleQueue
{
    public const int MaxConcurrent = 40;
    public static readonly List<Particle> Active = new();

    public static void Clear() => Active.Clear();

    // F9 hot-reload latch reset — conservative full clear since Particle has no
    // Ambient category flag today. AmbientOverlayPass reseeds on map entry.
    public static void ClearAmbient()
    {
        int before = Active.Count;
        Active.Clear();
        UI.DebugLogger.LogGame("RELOAD", $"ParticleQueue.ClearAmbient dropped {before}");
    }

    // Enforce cap by dropping oldest when adding a new particle.
    private static void Push(Particle p)
    {
        if (Active.Count >= MaxConcurrent) Active.RemoveAt(0);
        Active.Add(p);
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
            Push(new Particle
            {
                X = tx,
                Y = ty,
                Glyph = spec.Glyph,
                Color = spec.Color,
                FadeTo = spec.FadeTo,
                UseFade = true,
                DurationMs = spec.DurMs,
                Vx = spec.Vx,
                Vy = spec.Vy,
            });
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
            case ParticleEvent.MonsterDeath: EmitMonsterDeath(x, y, cMax, dur); break;
            case ParticleEvent.SkillCastStart: EmitSkillCast(x, y, dur, tint); break;
            case ParticleEvent.ItemPickup: EmitItemPickup(x, y, dur); break;
            case ParticleEvent.PoisonInflict: EmitPoisonInflict(x, y, cMax, dur); break;
            case ParticleEvent.BleedInflict: EmitBleedInflict(x, y, cMax, dur); break;
            case ParticleEvent.BurnInflict: EmitBurnInflict(x, y, cMax, dur); break;
            case ParticleEvent.LevelUp: EmitLevelUp(x, y, dur); break;
            case ParticleEvent.HealingTick: EmitHealingTick(x, y, dur); break;
            case ParticleEvent.EnhancementSuccess: EmitEnhancement(x, y, dur); break;
            case ParticleEvent.FloorTransition: EmitFloorTransition(x, y, dur); break;
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
        {
            Push(new Particle
            {
                X = x + dx * 0.3f, Y = y + dy * 0.3f,
                Glyph = glyphs[i % glyphs.Length], Color = Color.White,
                FadeTo = Color.Gray, UseFade = true,
                DurationMs = dur * 0.6f, Vx = vx, Vy = vy,
            });
        }
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
            Push(new Particle
            {
                X = x, Y = y, Glyph = '◇', Color = Color.BrightYellow,
                FadeTo = Color.Gray, UseFade = true,
                DurationMs = dur, Vx = vx, Vy = vy,
            });
        }
    }

    // 8-ray polygon ring for monster deaths. Supplementary to existing shatter.
    private static void EmitMonsterDeath(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(8));
        char[] glyphs = { '◇', '·' };
        for (int i = 0; i < n; i++)
        {
            double angle = (2 * Math.PI * i) / n;
            float vx = (float)(Math.Cos(angle) * 0.003f);
            float vy = (float)(Math.Sin(angle) * 0.003f);
            Push(new Particle
            {
                X = x, Y = y, Glyph = glyphs[i % 2], Color = Color.BrightCyan,
                FadeTo = Color.DarkGray, UseFade = true,
                DurationMs = dur, Vx = vx, Vy = vy,
            });
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
            Push(new Particle
            {
                X = x + (float)Math.Cos(angle), Y = y + (float)Math.Sin(angle),
                Glyph = '·', Color = c, UseFade = false,
                DurationMs = dur, Vx = 0, Vy = 0,
            });
        }
    }

    // 3-star burst on item tile. BrightYellow → Yellow.
    private static void EmitItemPickup(int x, int y, float dur)
    {
        int n = ScaleCount(3);
        float[] vxs = { -0.002f, 0.002f, 0f };
        float[] vys = { -0.002f, -0.002f, -0.003f };
        for (int i = 0; i < n; i++)
        {
            Push(new Particle
            {
                X = x, Y = y, Glyph = '✦', Color = Color.BrightYellow,
                FadeTo = Color.Yellow, UseFade = true,
                DurationMs = dur * 0.5f,
                Vx = vxs[i % 3], Vy = vys[i % 3],
            });
        }
    }

    private static void EmitPoisonInflict(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(4));
        for (int i = 0; i < n; i++)
        {
            float jx = (i % 2 == 0 ? -0.001f : 0.001f);
            Push(new Particle
            {
                X = x, Y = y, Glyph = '·', Color = Color.BrightGreen,
                FadeTo = Color.Gray, UseFade = true,
                DurationMs = dur, Vx = jx, Vy = -0.002f,
            });
        }
    }

    // SAO theme: bleed is WHITE/CYAN ◇, not red blood.
    private static void EmitBleedInflict(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(3));
        for (int i = 0; i < n; i++)
        {
            float jx = (i - 1) * 0.001f;
            Push(new Particle
            {
                X = x, Y = y, Glyph = '◇', Color = Color.BrightCyan,
                FadeTo = Color.White, UseFade = true,
                DurationMs = dur * 0.75f, Vx = jx, Vy = 0.002f,
            });
        }
    }

    private static void EmitBurnInflict(int x, int y, int maxCount, float dur)
    {
        int n = Math.Min(maxCount, ScaleCount(3));
        for (int i = 0; i < n; i++)
        {
            float jx = (i - 1) * 0.0008f;
            Push(new Particle
            {
                X = x, Y = y, Glyph = '\'', Color = Color.BrightRed,
                FadeTo = Color.BrightYellow, UseFade = true,
                DurationMs = dur * 0.75f, Vx = jx, Vy = -0.003f,
            });
        }
    }

    // Rising column of . * from player tile — levelup pulse.
    private static void EmitLevelUp(int x, int y, float dur)
    {
        int n = ScaleCount(7);
        char[] glyphs = { '·', '*', '·' };
        for (int i = 0; i < n; i++)
        {
            Push(new Particle
            {
                X = x, Y = y, Glyph = glyphs[i % glyphs.Length],
                Color = Color.White, FadeTo = Color.BrightCyan, UseFade = true,
                DurationMs = dur, Vx = 0, Vy = -0.003f - (i * 0.0005f),
            });
        }
    }

    private static void EmitHealingTick(int x, int y, float dur)
    {
        int n = ScaleCount(2);
        for (int i = 0; i < n; i++)
        {
            float jx = (i == 0 ? -0.001f : 0.001f);
            Push(new Particle
            {
                X = x, Y = y, Glyph = i == 0 ? '+' : '·',
                Color = Color.BrightGreen, UseFade = false,
                DurationMs = dur * 0.5f, Vx = jx, Vy = -0.001f,
            });
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
            Push(new Particle
            {
                X = x, Y = y, Glyph = glyphs[i % 2], Color = Color.BrightMagenta,
                FadeTo = Color.Magenta, UseFade = true,
                DurationMs = dur, Vx = vx, Vy = vy,
            });
        }
    }

    private static void EmitFloorTransition(int x, int y, float dur)
    {
        int n = ScaleCount(5);
        for (int i = 0; i < n; i++)
        {
            Push(new Particle
            {
                X = x, Y = y, Glyph = i % 2 == 0 ? ':' : '.',
                Color = Color.BrightCyan, FadeTo = Color.Cyan, UseFade = true,
                DurationMs = dur * 0.8f, Vx = 0, Vy = -0.002f - i * 0.0003f,
            });
        }
    }

    // Ticks every particle by dtMs; culls expired entries. Called from render.
    public static void Tick(int dtMs)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var p = Active[i];
            p.AgeMs += dtMs;
            p.X += p.Vx * dtMs;
            p.Y += p.Vy * dtMs;
            if (p.AgeMs >= p.DurationMs) Active.RemoveAt(i);
        }
    }
}
