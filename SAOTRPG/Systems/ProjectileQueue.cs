using Terminal.Gui;

namespace SAOTRPG.Systems;

// Animated projectile (arrow, bolt, sword arc). Tweens along a Bresenham
// line start→end at MsPerCell. Trail glyph fades 1 cell behind the head.
public sealed class Projectile
{
    public int StartX, StartY, EndX, EndY;
    public char Glyph;
    public Color Color;
    public int MsPerCell;
    public char? TrailGlyph;
    public Color? TrailColor;
    public int ElapsedMs;
    // Pre-baked path so each frame just indexes by cell-step.
    public (int X, int Y)[] Path = Array.Empty<(int, int)>();

    public int CurrentCellIndex(int totalCells)
    {
        if (MsPerCell <= 0) return totalCells - 1;
        return Math.Min(totalCells - 1, ElapsedMs / MsPerCell);
    }
}
