namespace SAOTRPG.Map;

// Viewport camera — tracks which portion of the game map is visible on screen.
// Centered on the player, clamped to map boundaries.
public class Camera
{
    // Viewport width in tiles (set from terminal column count).
    public int ViewWidth { get; set; }
    // Viewport height in tiles (set from terminal row count).
    public int ViewHeight { get; set; }
    // Horizontal tile offset — top-left corner X of the visible region.
    public int OffsetX { get; private set; }
    // Vertical tile offset — top-left corner Y of the visible region.
    public int OffsetY { get; private set; }

    // Centers the viewport on (x, y). The camera is NOT clamped to the map —
    // the player stays strictly at the viewport center and the map scrolls
    // underneath. Out-of-bounds tiles render as black inside MapView.
    public void CenterOn(int x, int y)
    {
        OffsetX = x - ViewWidth / 2;
        OffsetY = y - ViewHeight / 2;
    }
}
