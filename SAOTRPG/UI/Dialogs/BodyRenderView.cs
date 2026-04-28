using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Read-only body renderer for PlayerGuideDialog. Replaces TextView so each
// token can hold its own Attribute — Terminal.Gui v2 TextView shares one
// ColorScheme across all content (quirks §5). Owns scroll position + focused
// See-also bullet cursor. Tokenizes the wrapped body once on SetContent.
internal sealed class BodyRenderView : View
{
    // Atomic [[Topic]] regex; matches the same shape MigrateBracketsToDouble emits.
    private static readonly Regex DoubleBracketRegex =
        new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    private sealed class Span
    {
        public string Text = "";
        public Color Fg;
    }

    private sealed class Line
    {
        public List<Span> Spans = new();
        public bool IsSeeAlsoBullet;
        public string? LinkTarget;
    }

    private List<Line> _lines = new();
    private List<int> _seeAlsoIdxs = new();
    private int _focused = -1;   // index into _seeAlsoIdxs
    private int _scrollY;

    // Per-token foreground colors. Background is always Color.Black.
    private static readonly Color FgBody       = Color.White;
    private static readonly Color FgTitle      = Color.BrightYellow;
    private static readonly Color FgSection    = Color.BrightYellow;
    private static readonly Color FgLink       = Color.BrightCyan;
    private static readonly Color FgDisclosure = Color.BrightYellow;
    private static readonly Color FgDim        = Color.Gray;
    // Whole-line foreground swap for the focused See-also bullet.
    private static readonly Color FgFocusLine  = Color.BrightYellow;

    public Action<string>? LinkActivated;
    public Action? RequestSidebarFocus;
    public Action? RequestPopNavStack;
    public Func<bool>? OnEnterToggleDisclosure; // returns true if a block was toggled

    public BodyRenderView()
    {
        CanFocus = true;
    }

    public bool HasSeeAlsoLinks => _seeAlsoIdxs.Count > 0;
    public int FocusedSeeAlsoIndex => _focused;

    // Plain message (no tokenization). Used for category-header / spoiler placeholder text.
    public void SetMessage(string text)
    {
        _lines = new List<Line>();
        foreach (var raw in (text ?? "").Replace("\r\n", "\n").Split('\n'))
        {
            var line = new Line();
            line.Spans.Add(new Span { Text = raw, Fg = FgBody });
            _lines.Add(line);
        }
        _seeAlsoIdxs.Clear();
        _focused = -1;
        _scrollY = 0;
        SetNeedsDraw();
    }

    // Topic body. Expects the already-wrapped string from PlayerGuideDialog's
    // RenderEntryBody (title + body + See also + Referenced by, all wrapped).
    public void SetContent(string wrappedBody)
    {
        _lines = Tokenize(wrappedBody ?? "");
        _seeAlsoIdxs = new List<int>();
        for (int i = 0; i < _lines.Count; i++)
            if (_lines[i].IsSeeAlsoBullet) _seeAlsoIdxs.Add(i);
        _focused = -1;
        _scrollY = 0;
        SetNeedsDraw();
    }

    public void FocusFirstSeeAlso()
    {
        if (_seeAlsoIdxs.Count == 0) return;
        _focused = 0;
        EnsureFocusedVisible();
        SetNeedsDraw();
    }

    public void ClearLinkFocus()
    {
        if (_focused == -1) return;
        _focused = -1;
        SetNeedsDraw();
    }

    private void EnsureFocusedVisible()
    {
        if (_focused < 0 || _focused >= _seeAlsoIdxs.Count) return;
        int target = _seeAlsoIdxs[_focused];
        int height = Math.Max(1, Frame.Height);
        if (target < _scrollY) _scrollY = target;
        else if (target >= _scrollY + height) _scrollY = target - height + 1;
        if (_scrollY < 0) _scrollY = 0;
    }

    // ── Tokenizer ────────────────────────────────────────────────────────
    // Walk the wrapped body once. Track section context so bullet rows under
    // a "--- See also ---" rule become focusable; rows under any other rule
    // (or no rule) stay plain.
    private static List<Line> Tokenize(string wrappedBody)
    {
        var result = new List<Line>();
        var raws = wrappedBody.Replace("\r\n", "\n").Split('\n');
        bool seenTitle = false;
        bool inSeeAlso = false;

        foreach (var raw in raws)
        {
            string trimmed = raw.TrimEnd();
            var line = new Line();

            // Title: first non-empty line of the body (rendered uppercase by caller).
            if (!seenTitle && trimmed.Length > 0)
            {
                line.Spans.Add(new Span { Text = raw, Fg = FgTitle });
                seenTitle = true;
                result.Add(line);
                continue;
            }

            // Section rule: starts with "---" followed by a name.
            if (trimmed.StartsWith("---") && trimmed.Length >= 5)
            {
                string upper = trimmed.ToUpperInvariant();
                inSeeAlso = upper.StartsWith("--- SEE ALSO");
                line.Spans.Add(new Span { Text = raw, Fg = FgSection });
                result.Add(line);
                continue;
            }

            // Disclosure markers ▸ / ▾ (or ASCII fallback >, v).
            if (trimmed.StartsWith("▸ ") || trimmed.StartsWith("▾ ")
                || trimmed.StartsWith("> ") || trimmed.StartsWith("v "))
            {
                int leadCount = 0;
                while (leadCount < raw.Length && (raw[leadCount] == ' ' || raw[leadCount] == '\t')) leadCount++;
                if (leadCount > 0)
                    line.Spans.Add(new Span { Text = raw.Substring(0, leadCount), Fg = FgBody });
                line.Spans.Add(new Span { Text = raw.Substring(leadCount, 2), Fg = FgDisclosure });
                EmitInlineWithLinks(raw.Substring(leadCount + 2), line, FgBody);
                result.Add(line);
                continue;
            }

            // See-also bullet: "  * [[Topic]]" inside an active See-also section.
            if (inSeeAlso)
            {
                int starIdx = raw.IndexOf("* ");
                int linkStart = raw.IndexOf("[[");
                int linkEnd = linkStart >= 0 ? raw.IndexOf("]]", linkStart) : -1;
                if (starIdx >= 0 && linkStart > starIdx && linkEnd > linkStart)
                {
                    line.IsSeeAlsoBullet = true;
                    line.LinkTarget = raw.Substring(linkStart + 2, linkEnd - linkStart - 2);
                    // Bullet prefix (incl. "* ") in body, link span in cyan, trailing in body.
                    line.Spans.Add(new Span { Text = raw.Substring(0, linkStart), Fg = FgBody });
                    line.Spans.Add(new Span
                    {
                        Text = raw.Substring(linkStart, linkEnd - linkStart + 2),
                        Fg = FgLink,
                    });
                    if (linkEnd + 2 < raw.Length)
                        line.Spans.Add(new Span { Text = raw.Substring(linkEnd + 2), Fg = FgBody });
                    result.Add(line);
                    continue;
                }
            }

            // Default: body text with inline [[brackets]] highlighted cyan.
            EmitInlineWithLinks(raw, line, FgBody);
            result.Add(line);
        }

        return result;
    }

    // Splits a line into Plain/Link spans by walking [[...]] tokens.
    private static void EmitInlineWithLinks(string text, Line line, Color baseFg)
    {
        if (string.IsNullOrEmpty(text))
        {
            line.Spans.Add(new Span { Text = "", Fg = baseFg });
            return;
        }
        int i = 0;
        while (i < text.Length)
        {
            int linkStart = text.IndexOf("[[", i, StringComparison.Ordinal);
            if (linkStart < 0)
            {
                line.Spans.Add(new Span { Text = text.Substring(i), Fg = baseFg });
                return;
            }
            int linkEnd = text.IndexOf("]]", linkStart, StringComparison.Ordinal);
            if (linkEnd < 0)
            {
                line.Spans.Add(new Span { Text = text.Substring(i), Fg = baseFg });
                return;
            }
            if (linkStart > i)
                line.Spans.Add(new Span { Text = text.Substring(i, linkStart - i), Fg = baseFg });
            line.Spans.Add(new Span
            {
                Text = text.Substring(linkStart, linkEnd - linkStart + 2),
                Fg = FgLink,
            });
            i = linkEnd + 2;
        }
    }

    // ── Drawing ──────────────────────────────────────────────────────────
    protected override bool OnDrawingContent()
    {
        int height = Frame.Height;
        int width = Frame.Width;
        for (int row = 0; row < height; row++)
        {
            int lineIdx = _scrollY + row;
            if (lineIdx < 0 || lineIdx >= _lines.Count) continue;

            var line = _lines[lineIdx];
            bool isFocused = _focused >= 0 && _focused < _seeAlsoIdxs.Count
                          && lineIdx == _seeAlsoIdxs[_focused];

            int col = 0;
            foreach (var span in line.Spans)
            {
                if (col >= width) break;
                Color fg = isFocused ? FgFocusLine : span.Fg;
                Driver!.SetAttribute(new Terminal.Gui.Attribute(fg, Color.Black));
                Move(col, row);
                int writable = Math.Min(span.Text.Length, width - col);
                if (writable <= 0) break;
                if (writable < span.Text.Length)
                    Driver!.AddStr(span.Text.Substring(0, writable));
                else
                    Driver!.AddStr(span.Text);
                col += writable;
            }
        }
        return true;
    }

    // ── Input ────────────────────────────────────────────────────────────
    protected override bool OnKeyDown(Key keyEvent)
    {
        switch (keyEvent.KeyCode)
        {
            case KeyCode.CursorUp:
                if (_seeAlsoIdxs.Count > 0 && _focused > 0)
                {
                    _focused--;
                    EnsureFocusedVisible();
                    SetNeedsDraw();
                    keyEvent.Handled = true;
                    return true;
                }
                if (_seeAlsoIdxs.Count > 0 && _focused == 0)
                {
                    _focused = -1;  // unhighlight; next Up scrolls
                    SetNeedsDraw();
                    keyEvent.Handled = true;
                    return true;
                }
                if (_scrollY > 0) { _scrollY--; SetNeedsDraw(); keyEvent.Handled = true; return true; }
                break;

            case KeyCode.CursorDown:
                if (_seeAlsoIdxs.Count > 0)
                {
                    if (_focused < 0) _focused = 0;
                    else if (_focused < _seeAlsoIdxs.Count - 1) _focused++;
                    EnsureFocusedVisible();
                    SetNeedsDraw();
                    keyEvent.Handled = true;
                    return true;
                }
                if (_scrollY + Frame.Height < _lines.Count)
                { _scrollY++; SetNeedsDraw(); keyEvent.Handled = true; return true; }
                break;

            case KeyCode.PageUp:
                _scrollY = Math.Max(0, _scrollY - Math.Max(1, Frame.Height - 2));
                SetNeedsDraw();
                keyEvent.Handled = true;
                return true;

            case KeyCode.PageDown:
                _scrollY = Math.Min(Math.Max(0, _lines.Count - 1),
                                    _scrollY + Math.Max(1, Frame.Height - 2));
                SetNeedsDraw();
                keyEvent.Handled = true;
                return true;

            case KeyCode.Home:
                _scrollY = 0;
                _focused = -1;
                SetNeedsDraw();
                keyEvent.Handled = true;
                return true;

            case KeyCode.End:
                _scrollY = Math.Max(0, _lines.Count - Frame.Height);
                SetNeedsDraw();
                keyEvent.Handled = true;
                return true;

            case KeyCode.CursorLeft:
                _focused = -1;
                RequestSidebarFocus?.Invoke();
                keyEvent.Handled = true;
                return true;

            case KeyCode.Enter:
                if (_focused >= 0 && _focused < _seeAlsoIdxs.Count)
                {
                    var line = _lines[_seeAlsoIdxs[_focused]];
                    if (!string.IsNullOrEmpty(line.LinkTarget))
                    {
                        LinkActivated?.Invoke(line.LinkTarget);
                        keyEvent.Handled = true;
                        return true;
                    }
                }
                if (OnEnterToggleDisclosure?.Invoke() == true)
                {
                    keyEvent.Handled = true;
                    return true;
                }
                break;

            case KeyCode.Backspace:
            case KeyCode.O | KeyCode.CtrlMask:
                RequestPopNavStack?.Invoke();
                keyEvent.Handled = true;
                return true;
        }
        return base.OnKeyDown(keyEvent);
    }
}
