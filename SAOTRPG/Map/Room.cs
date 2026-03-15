namespace SAOTRPG.Map;

public record struct Room(int X, int Y, int Width, int Height)
{
    public readonly int CenterX => X + Width / 2;
    public readonly int CenterY => Y + Height / 2;

    public readonly bool Intersects(Room other, int padding = 1) =>
        X - padding < other.X + other.Width &&
        X + Width + padding > other.X &&
        Y - padding < other.Y + other.Height &&
        Y + Height + padding > other.Y;
}
