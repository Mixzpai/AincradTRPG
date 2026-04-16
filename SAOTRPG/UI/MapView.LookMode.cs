using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Interactive look mode -- press L to highlight visible monsters,
// Tab/arrows to cycle, shows stat overlay for the selected monster.
public partial class MapView
{
    private bool _lookModeActive;
    private List<Monster> _lookTargets = new();
    private int _lookIndex;

    public bool IsLookModeActive => _lookModeActive;
    public event Action<bool>? LookModeChanged;

    public void EnterLookMode(List<Monster> targets)
    {
        if (targets.Count == 0) { _lookModeActive = false; return; }
        _lookTargets = targets;
        _lookIndex = 0;
        _lookModeActive = true;
        LookModeChanged?.Invoke(true);
        SetNeedsDraw();
    }

    public void ExitLookMode()
    {
        _lookModeActive = false;
        _lookTargets.Clear();
        _lookIndex = 0;
        LookModeChanged?.Invoke(false);
        SetNeedsDraw();
    }

    private void LookNext()
    {
        if (_lookTargets.Count == 0) return;
        _lookIndex = (_lookIndex + 1) % _lookTargets.Count;
        SetNeedsDraw();
    }

    private void LookPrev()
    {
        if (_lookTargets.Count == 0) return;
        _lookIndex = (_lookIndex - 1 + _lookTargets.Count) % _lookTargets.Count;
        SetNeedsDraw();
    }

    private bool HandleLookModeKey(Key keyEvent)
    {
        if (!_lookModeActive) return false;
        var bareKey = keyEvent.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        switch (bareKey)
        {
            case KeyCode.Esc:
            case KeyCode.L:
                ExitLookMode();
                keyEvent.Handled = true;
                return true;
            case KeyCode.Tab:
            case KeyCode.D:
            case KeyCode.CursorRight:
            case KeyCode.S:
            case KeyCode.CursorDown:
                LookNext();
                keyEvent.Handled = true;
                return true;
            case KeyCode.A:
            case KeyCode.CursorLeft:
            case KeyCode.W:
            case KeyCode.CursorUp:
                LookPrev();
                keyEvent.Handled = true;
                return true;
            default:
                ExitLookMode();
                return false;
        }
    }

    private void RenderLookMode(int vpWidth, int vpHeight)
    {
        if (!_lookModeActive || _lookTargets.Count == 0) return;

        _lookTargets.RemoveAll(m => m.IsDefeated);
        if (_lookTargets.Count == 0) { ExitLookMode(); return; }
        if (_lookIndex >= _lookTargets.Count) _lookIndex = 0;

        for (int i = 0; i < _lookTargets.Count; i++)
        {
            var m = _lookTargets[i];
            int vx = MapToVx(m.X), vy = MapToVy(m.Y);
            if (vx < 0 || vy < 0 || vx >= vpWidth || vy >= vpHeight) continue;

            bool isSelected = i == _lookIndex;
            Color bg = isSelected ? Color.DarkGray : new Color(30, 30, 60);
            Color fg = isSelected ? Color.BrightYellow : Color.Yellow;
            char glyph = m is Boss ? 'B' : m.Name.Length > 0 ? char.ToUpper(m.Name[0]) : 'M';
            DrawGlyph(m.X, m.Y, glyph, Gfx.Attr(fg, bg), vpWidth, vpHeight);
        }

        var selected = _lookTargets[_lookIndex];
        int sx = MapToVx(selected.X), sy = MapToVy(selected.Y);

        var bracketAttr = Gfx.Attr(Color.BrightYellow, Color.Black);
        int bracketLeft = sx - 1, bracketRight = sx + 1;
        if (bracketLeft >= 0 && bracketRight < vpWidth && sy >= 0 && sy < vpHeight)
        {
            Driver!.SetAttribute(bracketAttr);
            Move(bracketLeft, sy);
            Driver!.AddRune(new System.Text.Rune('['));
            Move(bracketRight, sy);
            Driver!.AddRune(new System.Text.Rune(']'));
        }

        RenderStatPanel(selected, sx, sy, vpWidth, vpHeight);

        string hint = _lookTargets.Count > 1
            ? $"[{_lookIndex + 1}/{_lookTargets.Count}] Tab/Arrows: cycle  L/Esc: exit"
            : "[1/1] L/Esc: exit";
        int hintX = Math.Max(0, (vpWidth - hint.Length) / 2);
        DrawTextAtView(hintX, vpHeight - 1, hint,
            Gfx.Attr(Color.DarkGray, Color.Black), vpWidth, vpHeight);
    }

    private void RenderStatPanel(Monster monster, int sx, int sy, int vpWidth, int vpHeight)
    {
        string[] lines = BuildStatLines(monster);
        if (lines.Length == 0) return;

        int panelW = 0;
        foreach (var line in lines) panelW = Math.Max(panelW, line.Length);
        panelW += 2;
        int panelH = lines.Length + 2;

        int px, gap = 3;
        if (sx + gap + panelW <= vpWidth) px = sx + gap;
        else if (sx - 2 - panelW >= 0) px = sx - 2 - panelW;
        else px = Math.Max(0, (vpWidth - panelW) / 2);

        int py = Math.Max(0, Math.Min(sy - panelH / 2, vpHeight - panelH));

        var boxAttr = Gfx.Attr(Color.White, new Color(20, 20, 30));
        var borderAttr = Gfx.Attr(Color.DarkGray, new Color(20, 20, 30));
        var threatAttr = GetThreatAttribute(monster);

        DrawHLine(px, py, panelW, '─', borderAttr);

        for (int r = 0; r < lines.Length; r++)
        {
            int rowY = py + 1 + r;
            if (rowY >= vpHeight) break;
            var attr = r == 0 ? threatAttr : boxAttr;

            Driver!.SetAttribute(attr);
            for (int col = 0; col < panelW && px + col < vpWidth; col++)
            {
                Move(px + col, rowY);
                Driver!.AddRune(new System.Text.Rune(' '));
            }
            string text = lines[r];
            for (int ci = 0; ci < text.Length && px + 1 + ci < vpWidth; ci++)
            {
                Move(px + 1 + ci, rowY);
                Driver!.AddRune(new System.Text.Rune(text[ci]));
            }
        }

        int bottomY = py + 1 + lines.Length;
        if (bottomY < vpHeight) DrawHLine(px, bottomY, panelW, '─', borderAttr);
    }

    private string[] BuildStatLines(Monster m)
    {
        int diff = m.Level - _player.Level;
        string threat = diff switch
        {
            >= 5  => "!! DEADLY !!",
            >= 3  => "! Dangerous",
            >= 1  => "Strong",
            0     => "Even match",
            >= -2 => "Weaker",
            _     => "Trivial",
        };

        int dist = Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y));
        int hpPct = m.MaxHealth > 0 ? m.CurrentHealth * 100 / m.MaxHealth : 0;
        string hpBar = BarBuilder.BuildGradient(m.CurrentHealth, m.MaxHealth, 12);

        var lines = new List<string>
        {
            $"{m.Name} (Lv.{m.Level}) [{threat}]",
            $"HP {hpBar} {hpPct}%",
            $"ATK:{m.BaseAttack}  DEF:{m.BaseDefense}  Dist:{dist}",
        };

        if (m is Mob mob)
        {
            var tags = new List<string>();
            if (mob.CanPoison) tags.Add("Poison");
            if (mob.CanBleed) tags.Add("Bleed");
            if (mob.CanStun) tags.Add("Stun");
            if (mob.CanSlow) tags.Add("Slow");
            if (tags.Count > 0) lines.Add("Skills: " + string.Join(", ", tags));
        }

        if (m is Boss boss && boss.IsEnraged) lines.Add("Status: ENRAGED");
        if (m is Mob afxMob && afxMob.Affix != null) lines.Add($"Affix: {afxMob.Affix}");

        return lines.ToArray();
    }

    private Terminal.Gui.Attribute GetThreatAttribute(Monster m)
    {
        int diff = m.Level - _player.Level;
        Color fg = diff switch
        {
            >= 5  => Color.BrightRed,
            >= 3  => Color.Red,
            >= 1  => Color.Yellow,
            0     => Color.White,
            >= -2 => Color.Green,
            _     => Color.DarkGray,
        };
        return Gfx.Attr(fg, new Color(20, 20, 30));
    }

    private void DrawHLine(int x, int y, int width, char ch, Terminal.Gui.Attribute attr)
    {
        Driver!.SetAttribute(attr);
        for (int i = 0; i < width; i++)
        {
            Move(x + i, y);
            Driver!.AddRune(new System.Text.Rune(ch));
        }
    }
}
