using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Rendering;

// The row under the sidebar minimap: the marks a route is planned around, and the full map's key.
// Its own file because MinimapView paints only world colours (render/overlay-colours-are-themed);
// the words here are UI chrome in theme roles, and only the marks take the world transform.
public static class MinimapLegendRow
{
    public static void Paint(Gfx.Batch batch, int row, int width, (char Glyph, Color Fg, string Word)[] marks)
    {
        var t = ColorSchemes.Theme;
        Attribute muted = Gfx.Attr(t.Muted, t.Base), key = Gfx.Attr(t.Accent, t.Base);
        string mapKey = KeyHints.Key(GameAction.OpenWorldMap).ToLowerInvariant();
        const string Hint = " full map";
        int hintAt = width - mapKey.Length - Hint.Length;
        int x = 0;
        void Put(string s, Attribute a, int limit) { foreach (char c in s) if (x < limit) batch.Put(x++, row, c, a); }

        for (int i = 0; i < marks.Length; i++)
        {
            var (g, fg, word) = marks[i];
            if (x + (i > 0 ? 2 : 0) + 2 + word.Length > hintAt - 1) break;   // the key hint keeps its room
            if (i > 0) Put("  ", muted, hintAt);
            batch.Put(x++, row, g, Gfx.Attr(MapView.OverlayColor(fg), t.Base));
            Put(" " + word, muted, hintAt);
        }
        while (x < width) batch.Put(x++, row, ' ', muted);
        if (hintAt < 0) return;
        x = hintAt;
        Put(mapKey, key, width);
        Put(Hint, muted, width);
    }
}
