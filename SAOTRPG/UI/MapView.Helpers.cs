using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Dungeon-map coord conversion + low-level draw helpers. DrawGlyph/DrawTextAtView route via Gfx.PutCell
// so a future Terminal.Gui upgrade only touches Gfx.cs, not every overlay.
public partial class MapView
{
    // ── Coordinate conversion ──────────────────────────────────────────
    // Shake offset applied to every mapping so the viewport jitters in lockstep.

    // Map tile X → viewport column.
    private int MapToVx(int mx) => mx - _camera.OffsetX + ShakeOffsetX;

    // Map tile Y → viewport row.
    private int MapToVy(int my) => my - _camera.OffsetY + ShakeOffsetY;

    // Viewport column → map tile X.
    private int VxToMap(int vx) => vx + _camera.OffsetX - ShakeOffsetX;

    // Viewport row → map tile Y.
    private int VyToMap(int vy) => vy + _camera.OffsetY - ShakeOffsetY;

    // ── Drawing helpers ────────────────────────────────────────────────

    // Draw a single glyph at map coordinates. Handles bounds checking.
    private void DrawGlyph(int mx, int my, char ch, Terminal.Gui.Attribute attr, int vpW, int vpH)
    {
        int vx = MapToVx(mx);
        int vy = MapToVy(my);
        if (vx < 0 || vx >= vpW || vy < 0 || vy >= vpH) return;
        Gfx.PutCell(this, vx, vy, ch, attr);
    }

    // Draw a text string at a viewport position.
    private void DrawTextAtView(int viewX, int viewY, string text, Terminal.Gui.Attribute attr, int vpW, int vpH)
    {
        if (viewY < 0 || viewY >= vpH) return;
        Driver!.SetAttribute(attr);
        for (int i = 0; i < text.Length; i++)
        {
            int vx = viewX + i;
            if (vx < 0 || vx >= vpW) continue;
            Move(vx, viewY);
            Driver!.AddRune(new System.Text.Rune(text[i]));
        }
    }

    // Check if a map position falls within the current viewport.
    private bool MapInView(int mx, int my, int vpW, int vpH)
    {
        int vx = MapToVx(mx);
        int vy = MapToVy(my);
        return vx >= 0 && vx < vpW && vy >= 0 && vy < vpH;
    }
}
