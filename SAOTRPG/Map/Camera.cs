namespace SAOTRPG.Map;

// Viewport camera — visible map region, centered on player.
public class Camera
{
    public int ViewWidth { get; set; }
    public int ViewHeight { get; set; }
    public int OffsetX { get; private set; }
    public int OffsetY { get; private set; }

    // Unclamped: player stays at viewport center, map scrolls under. OOB tiles render black in MapView.
    public void CenterOn(int x, int y)
    {
        OffsetX = x - ViewWidth / 2;
        OffsetY = y - ViewHeight / 2;
    }
}
