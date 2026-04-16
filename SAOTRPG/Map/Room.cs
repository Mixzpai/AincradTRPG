namespace SAOTRPG.Map;

// Axis-aligned rectangular region used during dungeon generation.
// Rooms are connected by corridors carved between their centers.
public record struct Room(int X, int Y, int Width, int Height)
{
    // Horizontal center tile of the room.
    public readonly int CenterX => X + Width / 2;
    // Vertical center tile of the room.
    public readonly int CenterY => Y + Height / 2;

    // Inclusive point-in-rect test.
    public readonly bool Contains(int px, int py) =>
        px >= X && px < X + Width && py >= Y && py < Y + Height;
}
