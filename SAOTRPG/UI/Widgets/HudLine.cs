using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// One line of HUD text, painted through Gfx in its scheme's Normal colours, for text that changes
// every turn. A Label's Text setter marks every ancestor for layout, so the action bar's turn
// counter re-laid out the whole game screen on every move (~146 KB of grapheme work a move,
// TileVerifyProbe --hud). Setting this only marks it for redraw.
public sealed class HudLine : View
{
    private string _line = "";

    public HudLine()
    {
        CanFocus = false;
        Height = 1;
    }

    public string Line
    {
        get => _line;
        set
        {
            if (string.Equals(_line, value, StringComparison.Ordinal)) return;
            _line = value;
            SetNeedsDraw();
        }
    }

    // Every cell is painted, so the framework clear only duplicates output.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;
        var vp = Viewport;
        if (vp.Width <= 0 || vp.Height <= 0) return true;
        var attr = GetAttributeForRole(VisualRole.Normal);
        var batch = Gfx.Begin(this);
        for (int c = 0; c < vp.Width; c++)
        {
            char ch = c < _line.Length ? _line[c] : ' ';
            if (char.IsSurrogate(ch)) ch = '?';
            batch.Put(c, 0, ch, attr);
        }
        return true;
    }
}
