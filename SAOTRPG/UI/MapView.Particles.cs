using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Particle render layer. Renders BELOW damage popups/projectiles but ABOVE base tiles
// — later passes overwrite conflicting cells, giving popups/projectiles z-order priority.
public partial class MapView
{
    // Bundle 13 (Item 2) — throttle for awakening emission. Banner fires for ~3s but the
    // emit window is its first half (per DivineObtainBanner.ShouldEmitParticlesThisFrame);
    // we want one burst per emit window, not one per frame.
    private int _lastAwakeningLevelEmitted;

    private void RenderParticles(int w, int h, int dtMs)
    {
        using var _particlesScope = Profiler.Begin("MapView.Particles");
        // Bundle 13 (Item 2) — fire awakening burst exactly once per emit window.
        // Edge: ShouldEmit goes true→false as the banner enters its hold half — reset
        // the latch then so a re-fired awakening (different weapon) emits again.
        if (DivineObtainBanner.ShouldEmitParticlesThisFrame
            && DivineObtainBanner.AwakeningParticleLevel > 0
            && _lastAwakeningLevelEmitted == 0)
        {
            ParticleQueue.EmitDivineAwakening(_player.X, _player.Y, DivineObtainBanner.AwakeningParticleLevel);
            _lastAwakeningLevelEmitted = DivineObtainBanner.AwakeningParticleLevel;
        }
        else if (!DivineObtainBanner.ShouldEmitParticlesThisFrame)
        {
            _lastAwakeningLevelEmitted = 0;
        }

        if (UserSettings.Current.ParticleDensity == ParticleDensity.Off)
        {
            ParticleQueue.Clear();
            _deathBursts.Clear();
            return;
        }
        ParticleQueue.Tick(dtMs);

        foreach (var p in ParticleQueue.Active)
        {
            int mx = (int)Math.Round(p.X);
            int my = (int)Math.Round(p.Y);
            if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) continue;

            Color c = p.Color;
            if (p.UseFade && p.DurationMs > 0)
            {
                float t = Math.Clamp(p.AgeMs / p.DurationMs, 0f, 1f);
                byte r = (byte)(p.Color.R + (p.FadeTo.R - p.Color.R) * t);
                byte g = (byte)(p.Color.G + (p.FadeTo.G - p.Color.G) * t);
                byte b = (byte)(p.Color.B + (p.FadeTo.B - p.Color.B) * t);
                c = new Color(r, g, b);
            }
            DrawGlyph(mx, my, p.Glyph, Gfx.Attr(c, Color.Black), w, h);
        }

        RenderMonsterDeathBursts(w, h, dtMs);
    }

    // Two-phase canon-aligned monster death burst:
    //   Phase 1 (0..flashMs)   — fracture flash: blue-white core + dense braille around silhouette
    //   Phase 2 (flashMs..end) — spread/fade: shards burst outward, density falls with radius
    private void RenderMonsterDeathBursts(int w, int h, int dtMs)
    {
        if (_deathBursts.Count == 0) return;

        // Canon palette anchors. Core ≈ #E6F5FF (blue-white), then BrightCyan, then dim cyan, then DarkGray.
        Color coreColor   = new Color(230, 245, 255);
        Color brightCyan  = Color.BrightCyan;
        Color cyan        = Color.Cyan;
        Color dimCyan     = new Color(60, 110, 130);
        Color darkGray    = Color.DarkGray;
        Color black       = Color.Black;

        for (int i = _deathBursts.Count - 1; i >= 0; i--)
        {
            var burst = _deathBursts[i];
            burst.ElapsedMs += dtMs;
            float t = burst.ElapsedMs;

            bool inFlash = t <= burst.FlashMs;
            // Phase progress 0..1 (post-flash; clamped during flash).
            float spreadT = inFlash ? 0f : Math.Clamp((t - burst.FlashMs) / Math.Max(1f, burst.TotalMs - burst.FlashMs), 0f, 1f);

            // ── Phase 1: fracture flash ─────────────────────────────────
            if (inFlash)
            {
                // Bright core — blue-white braille FullCell at kill point, with corona of dense braille.
                DrawGlyph(burst.X, burst.Y, BrailleCanvas.Braille(BrailleCanvas.FullCell),
                    Gfx.Attr(coreColor, black), w, h);
                // Silhouette corona: scales with tier radius.
                int silR = burst.Tier switch
                {
                    MobTier.FloorBoss => 2,
                    MobTier.Elite     => 1,
                    _                 => 1,
                };
                for (int dx = -silR; dx <= silR; dx++)
                for (int dy = -silR; dy <= silR; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int cheb = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    if (cheb > silR) continue;
                    int px = burst.X + dx, py = burst.Y + dy;
                    if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;
                    int seed = (px * 31 + py * 17 + (int)burst.ElapsedMs / 24);
                    // Density: 6/8 dots near center, 4/8 at edge.
                    int dots = cheb == silR ? 4 : 6;
                    byte mask = BrailleCanvas.SpreadDots(seed, dots);
                    // Color crossfades core → brightCyan over the flash window.
                    float ft = t / Math.Max(1f, burst.FlashMs);
                    Color cc = LerpColor(coreColor, brightCyan, ft);
                    DrawGlyph(px, py, BrailleCanvas.Braille(mask), Gfx.Attr(cc, black), w, h);
                }
            }
            else
            {
                // ── Phase 2: spread + fade ──────────────────────────────
                // Each shard's polar position derives from elapsed-since-flash * speed.
                float since = t - burst.FlashMs;
                foreach (var s in burst.Shards)
                {
                    float life = Math.Clamp(since / (Math.Max(1f, burst.TotalMs - burst.FlashMs) * s.LifeBoost), 0f, 1f);
                    if (life >= 1f) continue;
                    float dist = s.SpawnRadius + s.Speed * since;
                    float fx = s.OriginX + (float)Math.Cos(s.Angle) * dist;
                    float fy = s.OriginY + (float)Math.Sin(s.Angle) * dist;
                    int px = (int)Math.Round(fx);
                    int py = (int)Math.Round(fy);
                    if (!_map.InBounds(px, py) || !_map.IsVisible(px, py)) continue;

                    // Color fade: brightCyan → cyan → dimCyan → darkGray.
                    Color color = life < 0.33f ? LerpColor(brightCyan, cyan, life / 0.33f)
                                : life < 0.66f ? LerpColor(cyan, dimCyan, (life - 0.33f) / 0.33f)
                                :                LerpColor(dimCyan, darkGray, (life - 0.66f) / 0.34f);

                    char glyph;
                    if (s.IsHero)
                    {
                        // Hero shards: ◆ first half, ◊ second — solid → outline as it dissolves.
                        glyph = life < 0.55f ? '◆' : '◊';
                    }
                    else
                    {
                        // Density falls with radius — outer shards thin out.
                        float density = 1f - life;
                        // Subtle stochastic flicker via seed + integer time bucket.
                        int seed = s.Seed + (int)(since / 32f);
                        byte mask = BrailleCanvas.DensityDots(seed, density);
                        if (mask == 0) continue;
                        glyph = BrailleCanvas.Braille(mask);
                    }
                    DrawGlyph(px, py, glyph, Gfx.Attr(color, black), w, h);
                }
            }

            if (burst.ElapsedMs >= burst.TotalMs) _deathBursts.RemoveAt(i);
            else _deathBursts[i] = burst;
        }
    }

    // Linear RGB lerp. t clamped to [0,1].
    private static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        byte r = (byte)(a.R + (b.R - a.R) * t);
        byte g = (byte)(a.G + (b.G - a.G) * t);
        byte bl = (byte)(a.B + (b.B - a.B) * t);
        return new Color(r, g, bl);
    }
}
