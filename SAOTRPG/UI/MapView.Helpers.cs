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

    // ── Drawing primitives ─────────────────────────────────────────────
    // The overlay passes were written against the framework's SetAttribute/Move/AddRune
    // trio, which commits every cell unconditionally and marks its row for retransmit.
    // Overlays repaint identical content most frames, so that kept whole rows dirty
    // forever and defeated change detection. Shadowing the trio here reroutes every
    // existing call through Gfx (which skips unchanged cells) with no call-site edits.
    // Pen position mirrors the framework's: AddRune advances one column.
    private Attribute _penAttr;
    private int _penX, _penY;

    // Batch in force for the current paint, shared with the tile loop. Overlay writes go
    // through it so they skip PutCell's per-cell origin resolve and clip-region walk, which
    // takes a lock. Null outside a paint (and on the frame-cache blit path, which draws no
    // overlays), where the PutCell fallback applies.
    private Gfx.Batch? _frameBatch;

    private void PutCellFast(int vx, int vy, char ch, Attribute attr)
    {
        if (_frameBatch is { } batch) batch.Put(vx, vy, ch, attr);
        else Gfx.PutCell(this, vx, vy, ch, attr);
    }

    private new Attribute SetAttribute(Attribute attr)
    {
        _penAttr = attr;
        return attr;
    }

    private new bool Move(int col, int row)
    {
        _penX = col;
        _penY = row;
        return true;
    }

    private new void AddRune(System.Text.Rune rune)
    {
        // Cell writes are keyed by char; the game's glyph set is entirely BMP.
        if (rune.IsBmp) PutCellFast(_penX, _penY, (char)rune.Value, _penAttr);
        _penX++;
    }

    private new void AddRune(char c) => AddRune(new System.Text.Rune(c));

    private new void AddStr(string str)
    {
        foreach (char c in str) AddRune(c);
    }

    // ── Drawing helpers ────────────────────────────────────────────────

    // Draw a single glyph at map coordinates. Handles bounds checking.
    private void DrawGlyph(int mx, int my, char ch, Attribute attr, int vpW, int vpH)
    {
        int vx = MapToVx(mx);
        int vy = MapToVy(my);
        if (vx < 0 || vx >= vpW || vy < 0 || vy >= vpH) return;
        PutCellFast(vx, vy, ch, attr);
    }

    // Draw a text string at a viewport position.
    private void DrawTextAtView(int viewX, int viewY, string text, Attribute attr, int vpW, int vpH)
    {
        if (viewY < 0 || viewY >= vpH) return;
        SetAttribute(attr);
        for (int i = 0; i < text.Length; i++)
        {
            int vx = viewX + i;
            if (vx < 0 || vx >= vpW) continue;
            Move(vx, viewY);
            AddRune(new System.Text.Rune(text[i]));
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
