using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// One line of HUD text, painted through Gfx in its scheme's Normal colours, for text that changes
// every turn. A Label's Text setter marks every ancestor for layout, which would re-lay out the
// whole game screen on every move. Setting Line only marks this view for redraw.
public sealed class HudLine : View
{
    // The text lives in a reused buffer, so a line rebuilt every turn into stack memory (Set)
    // allocates nothing unless it differs; Line is a string made only when read.
    private char[] _buf = new char[128];
    private int _len;
    private string? _lineCache = "";

    public HudLine()
    {
        CanFocus = false;
        Height = 1;
    }

    public string Line
    {
        get => _lineCache ??= new string(_buf, 0, _len);
        set => Set(value.AsSpan());
    }

    public void Set(ReadOnlySpan<char> text)
    {
        if (text.SequenceEqual(_buf.AsSpan(0, _len))) return;
        if (text.Length > _buf.Length) _buf = new char[Math.Max(text.Length, _buf.Length * 2)];
        text.CopyTo(_buf);
        _len = text.Length;
        _lineCache = null;
        SetNeedsDraw();
    }

    // Every cell is painted, so the framework clear only duplicates output.
    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above this view has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;
        var vp = Viewport;
        if (vp.Width <= 0 || vp.Height <= 0) return true;
        var attr = GetAttributeForRole(VisualRole.Normal);
        var batch = Gfx.Begin(this);
        for (int c = 0; c < vp.Width; c++)
        {
            char ch = c < _len ? _buf[c] : ' ';
            if (char.IsSurrogate(ch)) ch = '?';
            batch.Put(c, 0, ch, attr);
        }
        return true;
    }
}
