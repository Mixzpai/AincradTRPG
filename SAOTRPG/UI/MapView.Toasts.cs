using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Center banner render: reads ToastQueue.Peek() each frame, draws 40x3 box
// centered X at Y=vpHeight*0.3. Double-box for LevelUp/FloorBossCleared only.
public partial class MapView
{
    private const int ToastWidth = 40;
    private const int ToastHeight = 3;

    private void RenderToasts(int w, int h)
    {
        var toast = ToastQueue.Peek();
        if (toast == null) return;

        int y0 = Math.Max(1, (int)(h * 0.3));
        int x0 = Math.Max(0, (w - ToastWidth) / 2);
        int y1 = y0 + ToastHeight - 1;
        int x1 = x0 + ToastWidth - 1;
        if (y1 >= h || x1 >= w) return;

        // Fade alpha: 300ms in, 2400ms hold, 300ms out = 3s TTL.
        float alpha = 1f;
        if (toast.ElapsedMs < ToastQueue.FadeInMs)
            alpha = toast.ElapsedMs / (float)ToastQueue.FadeInMs;
        else if (toast.ElapsedMs > ToastQueue.FadeInMs + ToastQueue.HoldMs)
            alpha = 1f - (toast.ElapsedMs - ToastQueue.FadeInMs - ToastQueue.HoldMs)
                       / (float)ToastQueue.FadeOutMs;
        alpha = Math.Clamp(alpha, 0f, 1f);

        Color accent = ScaleColor(toast.Accent, alpha);
        Color textCol = ScaleColor(Color.White, alpha);

        bool doubleBox = toast.Category == ToastCategory.LevelUp
                      || toast.Category == ToastCategory.FloorBossCleared;
        (char tl, char tr, char bl, char br, char hz, char vt) = doubleBox
            ? ('╔', '╗', '╚', '╝', '═', '║')
            : ('┌', '┐', '└', '┘', '─', '│');

        var borderAttr = Gfx.Attr(accent, Color.Black);
        var textAttr = Gfx.Attr(textCol, Color.Black);

        // Top border
        Driver!.SetAttribute(borderAttr);
        Move(x0, y0); Driver!.AddRune(new System.Text.Rune(tl));
        for (int x = x0 + 1; x < x1; x++)
        { Move(x, y0); Driver!.AddRune(new System.Text.Rune(hz)); }
        Move(x1, y0); Driver!.AddRune(new System.Text.Rune(tr));

        // Sides + body
        for (int y = y0 + 1; y < y1; y++)
        {
            Move(x0, y); Driver!.AddRune(new System.Text.Rune(vt));
            for (int x = x0 + 1; x < x1; x++)
            { Move(x, y); Driver!.AddRune(new System.Text.Rune(' ')); }
            Move(x1, y); Driver!.AddRune(new System.Text.Rune(vt));
        }

        // Bottom border
        Move(x0, y1); Driver!.AddRune(new System.Text.Rune(bl));
        for (int x = x0 + 1; x < x1; x++)
        { Move(x, y1); Driver!.AddRune(new System.Text.Rune(hz)); }
        Move(x1, y1); Driver!.AddRune(new System.Text.Rune(br));

        // Message text — centered on the middle row, truncated if too long.
        string msg = toast.Message;
        if (msg.Length > ToastWidth - 4) msg = msg[..(ToastWidth - 4)];
        int mx = x0 + (ToastWidth - msg.Length) / 2;
        int my = y0 + 1;
        Driver!.SetAttribute(textAttr);
        for (int i = 0; i < msg.Length; i++)
        {
            Move(mx + i, my);
            Driver!.AddRune(new System.Text.Rune(msg[i]));
        }
    }

    private static Color ScaleColor(Color c, float alpha)
    {
        alpha = Math.Clamp(alpha, 0f, 1f);
        return new Color((byte)(c.R * alpha), (byte)(c.G * alpha), (byte)(c.B * alpha));
    }
}
