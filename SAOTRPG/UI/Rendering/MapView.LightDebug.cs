using SAOTRPG.UI.Helpers;
using SAOTRPG.Map.Lighting;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.Systems.World;

namespace SAOTRPG.UI.Rendering;

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
    // The rendered BACKGROUND, magnified. This is the surface banding shows on and the one mode
    // that shows what actually ships rather than what the model computed.
    Band,
    // The view-edge feather depth alone, 0-9. Two cells wide, so the frontier ring and the tier
    // behind it should read as two distinct shades; one flat ring means a tier stopped firing.
    Feather,
    // The indirect-light gather alone: how much direct light is landing NEAR each cell, before
    // any of it is spent. It is the one term with no visible signature of its own — it only ever
    // makes an existing shadow shallower — so without a mode of its own there is no way to tell
    // a gather that is working from one that is pinned.
    Indirect,
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

    // Deepest the contact term can reach, to normalise the Contact ramp.
    private const float LightAoFloor = LightingSystem.ContactFloor;

    internal static LightDebugMode CycleLightDebug()
    {
        LightDebug = LightDebug switch
        {
            LightDebugMode.Off     => LightDebugMode.Shadow,
            LightDebugMode.Shadow  => LightDebugMode.Contact,
            LightDebugMode.Contact => LightDebugMode.Luma,
            LightDebugMode.Luma    => LightDebugMode.Rgb,
            LightDebugMode.Rgb     => LightDebugMode.Band,
            LightDebugMode.Band    => LightDebugMode.Feather,
            LightDebugMode.Feather => LightDebugMode.Indirect,
            _                      => LightDebugMode.Off,
        };
        return LightDebug;
    }

    // ── Band mode ──────────────────────────────────────────────────────────────────────────────
    // A lit cell's background is light x BgGlowScale, so it lives in a 0-51 range and is nearly
    // black at its true value — which is why the quantizer that shapes it was never looked at.
    // Magnifying by 1/BgGlowScale fills the display range without changing a single step boundary,
    // so what shows here is exactly the staircase that ships, at a contrast a person can judge.
    private const float BandMagnify = 1f / BgGlowScale;

    // ApplyLighting steers the glow toward the TERRAIN colour, so a Band mode that skipped it
    // would stop showing what ships — which is the one thing this mode is for.
    private Color BandGroundColor(int mx, int my) => ResolveTerrain(_map.GetTile(mx, my), mx, my).fg;

    // Measured over the last painted Band frame. WORST is the EXCESS a rendered step carries over
    // the light field's own step between the same two cells — a real shadow edge is a legitimate
    // jump and must not be counted as a band.
    private int _bandLevels;
    private double _bandWorst;

    // Perceptual lightness. A fixed step in sRGB bytes is worth several times as much lightness
    // near black as near white, so a banding number stated in bytes means two different things at
    // the two ends of one gradient.
    private static double BandLStar(double r, double g, double b)
    {
        double y = 0.2126 * BandLin(r) + 0.7152 * BandLin(g) + 0.0722 * BandLin(b);
        return y <= 216.0 / 24389.0 ? y * 24389.0 / 27.0 : 116.0 * Math.Cbrt(y) - 16.0;
    }

    private static double BandLin(double c)
    {
        c /= 255.0;
        if (c < 0) c = 0;
        if (c > 1) c = 1;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private void MeasureBanding(int w, int h)
    {
        var levels = new HashSet<int>();
        double worst = 0;
        var lighting = _map.Lighting;
        for (int vy = 0; vy < h; vy++)
        {
            bool havePrev = false;
            double pr = 0, pg = 0, pb = 0, prr = 0, prg = 0, prb = 0;
            for (int vx = 0; vx < w; vx++)
            {
                int mx = VxToMap(vx), my = VyToMap(vy);
                if (!_map.InBounds(mx, my) || !_map.IsVisible(mx, my)) { havePrev = false; continue; }
                var l = lighting.GetLightUnchecked(mx, my);
                // The SHAPED glow, exactly as ApplyLighting builds it — vignette band and
                // view-edge feather folded in before the quantizer. Measuring the raw field
                // instead would report a staircase nobody is looking at.
                float g = BgGlowScale
                          * GlowScale(VignetteBand(mx, my), ViewEdgeOcclusion(mx, my)) * (1f / 255f);
                float fr = l.R * g, fg2 = l.G * g, fb = l.B * g;
                TintGlowToGround(BandGroundColor(mx, my), ref fr, ref fg2, ref fb);
                double cr = fr, cg = fg2, cb = fb;
                byte qr = Quantize(fr), qg = Quantize(fg2), qb = Quantize(fb);
                levels.Add((qr << 16) | (qg << 8) | qb);
                if (havePrev)
                {
                    double rendered = Math.Abs(BandLStar(qr, qg, qb) - BandLStar(pr, pg, pb));
                    double truth = Math.Abs(BandLStar(cr, cg, cb) - BandLStar(prr, prg, prb));
                    if (rendered - truth > worst) worst = rendered - truth;
                }
                pr = qr; pg = qg; pb = qb;
                prr = cr; prg = cg; prb = cb;
                havePrev = true;
            }
        }
        _bandLevels = levels.Count;
        _bandWorst = worst;
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

        // Before the layout: the readout reports these figures, so measuring after it would show
        // the previous frame's.
        if (mode == LightDebugMode.Band) MeasureBanding(w, h);

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
                case LightDebugMode.Indirect:
                {
                    // A cell gathering everything reads as a faint warm dot; one gathering
                    // nothing as a solid dark block. Deliberately the same direction as Shadow —
                    // dense means "little light here" in both — so the two can be flipped
                    // between without re-reading the ramp.
                    float gather = lighting.TryGetShadingTerms(_map, mx, my, out _, out _,
                                                               out float nl) ? nl : 0f;
                    glyph = RampGlyph(1f - gather);
                    fg = Mix(new Color(210, 170, 96), new Color(52, 48, 66), 1f - gather);
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
                case LightDebugMode.Band:
                {
                    var l = lighting.GetLightUnchecked(mx, my);
                    // Built exactly as ApplyLighting builds a background — shaped, then quantized
                    // — and only then magnified, so no step boundary moves. This is the mode that
                    // shows the vignette ramp and the view-edge feather; Luma and Rgb show the
                    // raw field underneath them.
                    float gs = BgGlowScale
                               * GlowScale(VignetteBand(mx, my), ViewEdgeOcclusion(mx, my))
                               * (1f / 255f);
                    float br = l.R * gs, bgc = l.G * gs, bb = l.B * gs;
                    TintGlowToGround(BandGroundColor(mx, my), ref br, ref bgc, ref bb);
                    byte qr = Quantize(br), qg = Quantize(bgc), qb = Quantize(bb);
                    glyph = '█';
                    fg = new Color(Chan(qr * BandMagnify), Chan(qg * BandMagnify),
                                   Chan(qb * BandMagnify));
                    break;
                }
                case LightDebugMode.Feather:
                {
                    // The DEPTH, not the scale it maps to: an interior cell is dark, the far tier
                    // mid, an adjacent contact bright. A frontier with only one bright ring and no
                    // mid ring behind it means the Chebyshev-2 tier is not firing.
                    byte depth = ViewEdgeOcclusion(mx, my);
                    glyph = depth == 0 ? '·' : RampGlyph(depth / 9f);
                    fg = depth == 0 ? new Color(40, 40, 50)
                       : depth == 1 ? new Color(70, 130, 200)
                                    : new Color(Chan(120 + 15 * depth), Chan(200 - 12 * depth), 70);
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
        // The gather and what it is worth at this cell, so "the shade here is shallow" can be
        // traced to its cause without switching modes.
        _map.Lighting.TryGetShadingTerms(_map, _player.X, _player.Y, out _, out _, out float pGi);
        t.Add(($"gi {pGi:0.00} -{LightingSystem.IndirectLift * pGi:P0}", LightReadoutFg));

        // WHAT IS CASTING NEARBY, AND HOW TALL IT COUNTS FOR. A caster's height decides how far
        // its shadow reaches, and there is otherwise no way to look at the map and tell a boulder
        // that is throwing half a shadow from a wall that is throwing all of one — the glyphs
        // differ but the shadow they explain is the thing being judged. Reports the nearest
        // sight-blocker to the player, which is what the dev will have walked up to.
        t.Add((NearestCasterToken(), LightReadoutFg));
        t.Add(($"lgt {(int)pl.R},{(int)pl.G},{(int)pl.B}", LightReadoutFg));
        if (mode == LightDebugMode.Feather)
        {
            byte pEdge = ViewEdgeOcclusion(_player.X, _player.Y);
            t.Add(($"@ depth {pEdge} scale {GlowScale(0, pEdge)}", LightReadoutFg));
        }
        if (mode == LightDebugMode.Band)
        {
            // What is in force and what it is costing, side by side — the four quantizers between
            // the light field and a drawn cell, then the worst EXCESS step a rendered pair carries
            // over the light field's own between the same two cells.
            t.Add(($"q{LightQuantizeStep} vig{VignetteBands} fade{MemoryFadeSteps}", LightReadoutFg));
            // The glow's own shaping at the player's cell — the band, the view-edge feather and
            // the combined scale, so a fringe seen on screen can be read off rather than guessed.
            byte pBand = VignetteBand(_player.X, _player.Y);
            byte pEdge = ViewEdgeOcclusion(_player.X, _player.Y);
            t.Add(($"@ band {pBand} edge {pEdge} glow {GlowScale(pBand, pEdge)}", LightReadoutFg));
            t.Add(($"bgLevels {_bandLevels}", LightReadoutFg));
            t.Add(($"worst step {_bandWorst:0.00}dL",
                   _bandWorst > 1.0 ? LightReadoutWarn : LightReadoutFg));
            t.Add(($"x{BandMagnify:0} magnified", LightReadoutFg));
        }
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

    // The nearest sight-blocking tile to the player and the height it casts at, for the readout.
    // Searched outward by ring so the first hit really is the nearest; the radius is small
    // because this is an "what am I standing next to" question, not a survey.
    private string NearestCasterToken()
    {
        for (int r = 1; r <= 4; r++)
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
            int x = _player.X + dx, y = _player.Y + dy;
            if (!_map.InBounds(x, y) || !_map.IsOpaque(x, y)) continue;
            var type = _map.Tiles[x, y].Type;
            return $"cast {type} h{LightingSystem.CasterHeightOf(type):0.00}";
        }
        return "cast none near";
    }

    private static string Compass(float dx, float dy)
    {
        string ns = dy < -0.383f ? "N" : dy > 0.383f ? "S" : "";
        string ew = dx > 0.383f ? "E" : dx < -0.383f ? "W" : "";
        string s = ns + ew;
        return s.Length == 0 ? "--" : s;
    }
}
