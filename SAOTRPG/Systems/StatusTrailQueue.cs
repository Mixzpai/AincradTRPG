using Terminal.Gui;

namespace SAOTRPG.Systems;

// Stationary fading glyph: bleed drip / poison cloud / burn ember.
// 3-frame sequence cycled at FrameMs each; total life = glyphs.Length * FrameMs.
public sealed class StatusTrail
{
    public int X, Y;
    public char[] Glyphs = Array.Empty<char>();
    public Color Color;
    public int FrameMs;
    public int ElapsedMs;

    public char? CurrentGlyph()
    {
        if (Glyphs.Length == 0 || FrameMs <= 0) return null;
        int idx = ElapsedMs / FrameMs;
        return idx < Glyphs.Length ? Glyphs[idx] : (char?)null;
    }

    public int LifetimeMs => Glyphs.Length * FrameMs;
}
