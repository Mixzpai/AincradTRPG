using Terminal.Gui;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Which lighting term the debug overlay paints instead of the world.
public enum LightDebugMode
{
    Off,
    // The swept sun-shadow term alone: shape, length, direction and penumbra.
    Shadow,
    // The contact-shading (ambient occlusion) term alone.
    Contact,
    // Final per-cell light as a brightness ramp, no tile colours.
    Luma,
    // Final per-cell light painted as its own RGB, so the cool tint of shade is visible.
    Rgb,
}

// Shift+F8 debug: paints one lighting TERM over the whole viewport plus a readout strip, so the
// sun model can be judged by eye instead of only from offline measurement. Dev instrumentation —
// deliberately NOT world-transformed by the active colour theme, because the point is to show the
// raw field the code computed rather than what a theme would make of it.
//
// PATH-D-PORT: diagnostic only, no player-visible contract. A port needs the same three reads
// available per logical tile: swept shadow, contact term, final light triple.
public partial class MapView
{
    // Off by default and read once per frame when off, exactly as HeightmapDebugEnabled is.
    internal static LightDebugMode LightDebug;

    // Denser glyph = larger value. Five steps: enough to read a gradient, few enough that each
    // step stays distinguishable in a terminal font.
    private const string LightRamp = "·░▒▓█";

    // Deepest the contact term can reach, mirroring LightingSystem's private AoDepth. Used only
    // to normalise the Contact ramp, so a drift here misreads that ramp and nothing else.
    private const float LightAoFloor = 1f - 0.28f;

    internal static LightDebugMode CycleLightDebug()
    {
        LightDebug = LightDebug switch
        {
            LightDebugMode.Off     => LightDebugMode.Shadow,
            LightDebugMode.Shadow  => LightDebugMode.Contact,
            LightDebugMode.Contact => LightDebugMode.Luma,
            LightDebugMode.Luma    => LightDebugMode.Rgb,
            _                      => LightDebugMode.Off,
        };
        return LightDebug;
    }

    private static char RampGlyph(float v)
    {
        int i = (int)(v * LightRamp.Length);
        if (i < 0) i = 0;
        if (i >= LightRamp.Length) i = LightRamp.Length - 1;
        return LightRamp[i];
    }

    private static byte Chan(float v) => (byte)(v < 0f ? 0f : v > 255f ? 255f : v);

    private static Color Mix(Color a, Color b, float t)
    {
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        return new Color(Chan(a.R + (b.R - a.R) * t),
                         Chan(a.G + (b.G - a.G) * t),
                         Chan(a.B + (b.B - a.B) * t));
    }

    // Rec.601 luma of a light triple against the 0-255 channel range the light grid uses. It can
    // exceed 1 where point lights stack, which the Luma mode flags rather than clips.
    private static float LightLuma(LightingSystem.LightRgb l)
        => (0.299f * l.R + 0.587f * l.G + 0.114f * l.B) / 255f;

    private void RenderLightDebugOverlay(int w, int h)
    {
        LightDebugMode mode = LightDebug;
        if (mode == LightDebugMode.Off) return;
        if (w < 4 || h < 3) return;

        // Laid out first: the readout claims rows off the bottom and the field has to know how
        // many before it paints. The map viewport is the terminal minus the 56-column sidebar,
        // so a fixed row count would clip its own numbers on a narrow terminal.
        int readoutRows = LayoutLightReadout(w, h, mode);
        int fieldRows = Math.Max(0, h - readoutRows);

        var lighting = _map.Lighting;
        for (int vy = 0; vy < fieldRows; vy++)
        for (int vx = 0; vx < w; vx++)
        {
            int mx = VxToMap(vx), my = VyToMap(vy);
            if (!_map.InBounds(mx, my)) continue;

            char glyph;
            Color fg;
            switch (mode)
            {
                case LightDebugMode.Shadow:
                {
                    // Open sun reads as a faint amber dot and full shadow as a solid cool block:
                    // density and hue carry the same value, so neither has to be read alone.
                    float s = lighting.TryGetShadingTerms(_map, mx, my, out float sh, out _) ? sh : 0f;
                    glyph = RampGlyph(s);
                    fg = Mix(new Color(120, 92, 40), new Color(120, 165, 255), s);
                    break;
                }
                case LightDebugMode.Contact:
                {
                    lighting.TryGetShadingTerms(_map, mx, my, out _, out float ao);
                    // ao runs [1 - AoDepth, 1]; normalised against the depth in force so the ramp
                    // spans the whole term without this file naming AoDepth.
                    float occl = ao >= 1f ? 0f : (1f - ao) / MathF.Max(1e-4f, 1f - LightAoFloor);
                    glyph = RampGlyph(occl);
                    fg = Mix(new Color(72, 64, 92), new Color(230, 210, 255), occl);
                    break;
                }
                case LightDebugMode.Luma:
                {
                    float lum = LightLuma(lighting.GetLightUnchecked(mx, my));
                    if (lum > 1.02f)
                    {
                        // Past the channel range: point lights stacking beyond what a cell shows.
                        glyph = '█';
                        fg = new Color(255, 215, 80);
                        break;
                    }
                    glyph = RampGlyph(lum);
                    byte g = Chan(40f + 215f * lum);
                    fg = new Color(g, g, g);
                    break;
                }
                default:
                {
                    var l = lighting.GetLightUnchecked(mx, my);
                    glyph = '█';
                    fg = new Color(Chan(l.R), Chan(l.G), Chan(l.B));
                    break;
                }
            }
            DrawGlyph_View(vx, vy, glyph, Gfx.Attr(fg, Color.Black));
        }

        PaintLightReadout(w, h, readoutRows);
    }

    // ── Readout ────────────────────────────────────────────────────────────────────────────
    // The strip claims rows off the BOTTOM of the map viewport on purpose: every other region of
    // the screen belongs to something the readout would collide with, and the rows it costs are
    // rows the overlay has already replaced with a debug field.

    private static readonly Color LightReadoutBg = new(16, 16, 24);
    private static readonly Color LightReadoutFg = new(196, 198, 210);
    private static readonly Color LightReadoutHead = new(255, 200, 90);
    private static readonly Color LightReadoutWarn = new(255, 130, 110);

    // Reused across frames so a per-frame overlay costs no allocation for its own layout.
    private readonly List<(string Text, Color Fg)> _lightTokens = new();
    private readonly List<(int X, int Y, int Index)> _lightPlaced = new();

    // Builds the token list, flows it into rows of the current width, and returns how many rows
    // it needs. Tokens are ordered by importance: the ones that fall off the end when the strip
    // runs out of rows are the ones worth losing.
    private int LayoutLightReadout(int w, int h, LightDebugMode mode)
    {
        var t = _lightTokens;
        t.Clear();
        _lightPlaced.Clear();

        t.Add(($"LIGHT {mode.ToString().ToUpperInvariant()}", LightReadoutHead));

        int state = DayNightCycle.SunState;
        var (ux, uy) = DayNightCycle.ShadowDirection;
        t.Add((state < 0 ? "sun down" : $"sun {state} {Compass(ux, uy)}({ux:0.00},{uy:0.00})", LightReadoutFg));
        t.Add(($"str {DayNightCycle.ShadowStrength:0.000}", LightReadoutFg));
        t.Add(($"dec {DayNightCycle.ShadowDecay:0.000}", LightReadoutFg));

        // Why the field can legitimately be flat, stated rather than left to be guessed at.
        if (!UserSettings.Current.TerrainShading) t.Add(("[shading off]", LightReadoutWarn));
        if (!_map.HasSky) t.Add(("[no sky]", LightReadoutWarn));

        t.Add(($"{WeatherSystem.GetLabel()} x{WeatherSystem.SunlightScale:0.00}", LightReadoutFg));
        t.Add(($"src {_map.Lighting.SourcesLastUpdate}", LightReadoutFg));

        bool swept = _map.Lighting.TryGetShadingTerms(_map, _player.X, _player.Y,
                                                      out float pShadow, out float pAo);
        var pl = _map.Lighting.GetLightUnchecked(_player.X, _player.Y);
        t.Add(($"@ shd {(swept ? pShadow.ToString("0.000") : "-")}", LightReadoutFg));
        t.Add(($"ao {pAo:0.000}", LightReadoutFg));
        t.Add(($"lgt {(int)pl.R},{(int)pl.G},{(int)pl.B}", LightReadoutFg));
        t.Add(($"lo {LightRamp} hi", Color.White));
        t.Add(($"torch {DayNightCycle.TorchInfluence:0.00}", LightReadoutFg));
        t.Add(($"emis {DayNightCycle.EmissiveInfluence:0.00}", LightReadoutFg));
        t.Add(($"{DayNightCycle.PhaseName} t{DayNightCycle.CurrentTurn}", LightReadoutFg));
        t.Add(("Shift+F8 next", LightReadoutHead));

        int maxRows = Math.Clamp(h / 4, 1, 5);
        int row = 0, col = 1;
        for (int i = 0; i < t.Count; i++)
        {
            int len = t[i].Text.Length;
            if (col > 1 && col + len > w)
            {
                row++;
                col = 1;
                if (row >= maxRows) break;
            }
            _lightPlaced.Add((col, row, i));
            col += len + 2;
        }
        return _lightPlaced.Count == 0 ? 1 : _lightPlaced[^1].Y + 1;
    }

    private void PaintLightReadout(int w, int h, int rows)
    {
        int y0 = h - rows;
        if (y0 < 0) return;

        var band = Gfx.Attr(LightReadoutBg, LightReadoutBg);
        for (int y = y0; y < h; y++)
        for (int x = 0; x < w; x++)
            DrawGlyph_View(x, y, ' ', band);

        foreach (var (x, y, index) in _lightPlaced)
        {
            var (text, fg) = _lightTokens[index];
            DrawTextAtView(x, y0 + y, text, Gfx.Attr(fg, LightReadoutBg), w, h);
        }
    }

    // Eight-point compass for the shadow vector. Screen coords, so -y is north.
    private static string Compass(float dx, float dy)
    {
        string ns = dy < -0.383f ? "N" : dy > 0.383f ? "S" : "";
        string ew = dx > 0.383f ? "E" : dx < -0.383f ? "W" : "";
        string s = ns + ew;
        return s.Length == 0 ? "--" : s;
    }
}
