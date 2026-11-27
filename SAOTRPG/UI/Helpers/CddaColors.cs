namespace SAOTRPG.UI.Helpers;

// A fixed curses-style colour palette, shared by the log (LogTones) and the sidebar overmap
// (OvermapView). Raw world colours: callers pass them through the theme.
public static class CddaColors
{
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color LightGreen = new(0, 255, 0), Green = new(0, 110, 0);
    public static readonly Color LightRed = new(255, 150, 150), Red = new(255, 0, 0);
    public static readonly Color Yellow = new(255, 255, 0), Brown = new(97, 56, 28);
    public static readonly Color LightBlue = new(100, 100, 255), Blue = new(51, 51, 255);
    public static readonly Color White = new(255, 255, 255), Gray = new(150, 150, 150);
    public static readonly Color DarkGray = new(99, 99, 99), Cyan = new(0, 150, 180);
}
