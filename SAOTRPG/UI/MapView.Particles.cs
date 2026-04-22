using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// FB-450 particle render layer. Renders BELOW damage popups and projectiles
// but ABOVE base tiles / tile-animations — later passes overwrite conflicting
// cells, giving popups/projectiles automatic z-order priority.
public partial class MapView
{
    private void RenderParticles(int w, int h, int dtMs)
    {
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
