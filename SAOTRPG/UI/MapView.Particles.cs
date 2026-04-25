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
    }
}
