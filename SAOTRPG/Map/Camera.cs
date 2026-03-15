namespace SAOTRPG.Map;

public class Camera
{
    public int ViewWidth { get; set; }
    public int ViewHeight { get; set; }
    public int OffsetX { get; private set; }
    public int OffsetY { get; private set; }

    public void CenterOn(int x, int y, int mapWidth, int mapHeight)
    {
        OffsetX = Math.Clamp(x - ViewWidth / 2, 0, Math.Max(0, mapWidth - ViewWidth));
        OffsetY = Math.Clamp(y - ViewHeight / 2, 0, Math.Max(0, mapHeight - ViewHeight));
    }
}
