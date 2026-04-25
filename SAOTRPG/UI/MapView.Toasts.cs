using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Center banner render: reads ToastQueue.Peek() each frame, draws variable-width box
// centered X at Y=vpHeight*0.3. Double-box for LevelUp/FloorBossCleared only.
public partial class MapView
{
    // Bundle 12 — variable sizing: width adapts to message len; height grows to line count.
    // Bounds keep the banner readable but never wider than the viewport (clamped at render time).
    private const int MinBoxWidth = 24;
    private const int AbsMaxBoxWidth = 60;
    private const int VerticalPad = 2;

    private void RenderToasts(int w, int h)
    {
        var toast = ToastQueue.Peek();
        if (toast == null) return;

        // Split incoming message into lines (caller may have inserted '\n' for coalesce).
        string raw = toast.Message ?? "";
        string[] lines = raw.Split('\n');

        // Width budget: clamp absolute max to viewport, then size to longest line + padding.
        int maxBoxWidth = Math.Min(AbsMaxBoxWidth, Math.Max(MinBoxWidth, w - 4));
        int longestLen = 0;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].Length > longestLen) longestLen = lines[i].Length;
        int boxWidth = Math.Min(maxBoxWidth, Math.Max(MinBoxWidth, longestLen + 4));

        // Per-line ellipsis when forced to truncate at MaxBoxWidth (word-cut tolerated).
        int innerWidth = boxWidth - 4;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > innerWidth)
            {
                int keep = Math.Max(0, innerWidth - 1);
                lines[i] = lines[i][..keep] + "…";
            }
        }

        int boxHeight = lines.Length + VerticalPad;
        int y0 = Math.Max(1, (int)(h * 0.3));
        // Clamp box height so it never paints past viewport bottom.
        if (y0 + boxHeight - 1 >= h) boxHeight = Math.Max(3, h - y0);
        int x0 = Math.Max(0, (w - boxWidth) / 2);
        int y1 = y0 + boxHeight - 1;
        int x1 = x0 + boxWidth - 1;
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

        // Render each line independently centered. Skip lines that would overflow box height.
        Driver!.SetAttribute(textAttr);
        int maxRows = boxHeight - VerticalPad;
        int rendered = Math.Min(lines.Length, maxRows);
        for (int li = 0; li < rendered; li++)
        {
            string line = lines[li];
            int mx = x0 + (boxWidth - line.Length) / 2;
            int my = y0 + 1 + li;
            for (int i = 0; i < line.Length; i++)
            {
                Move(mx + i, my);
                Driver!.AddRune(new System.Text.Rune(line[i]));
            }
        }
    }

    private static Color ScaleColor(Color c, float alpha)
    {
        alpha = Math.Clamp(alpha, 0f, 1f);
        return new Color((byte)(c.R * alpha), (byte)(c.G * alpha), (byte)(c.B * alpha));
    }
}
