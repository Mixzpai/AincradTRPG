using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Interactive look mode -- press L to highlight visible monsters,
// Tab/arrows to cycle, shows stat overlay for the selected monster.
// A right-side Target List Panel (FB-462) shows all visible hostiles
// sorted by Dist / HP / Threat / Level / Index; T cycles sort, 1-9
// jump to numbered target. Panel is gated on viewport width >= 60 so
// small terminals fall back to cursor-only mode.
public partial class MapView
{
    // ── Target List Panel layout ──────────────────────────────────
    // Sidebar width sized to fit: 15-char name + 2 cursor/index + 4
    // mini-HP-bar + 2 dist + 3 column gutters = 22 cols incl. borders.
    private const int LookPanelW       = 22;
    private const int LookPanelMinVpW  = 60;
    private const int LookNameMaxLen   = 15;

    private enum LookSort { Dist, HP, Threat, Level, Index }

    private bool _lookModeActive;
    private List<Monster> _lookTargets = new();
    private int _lookIndex;
    // Default sort = Dist (closest first); cycle via T.
    private LookSort _lookSort = LookSort.Dist;

    public bool IsLookModeActive => _lookModeActive;
    public event Action<bool>? LookModeChanged;

    public void EnterLookMode(List<Monster> targets)
    {
        if (targets.Count == 0) { _lookModeActive = false; return; }
        _lookTargets = targets;
        _lookIndex = 0;
        _lookModeActive = true;
        // Apply default sort on entry so the first target under the
        // cursor is the closest one, not whatever iteration order
        // GetVisibleMonsters happened to return.
        ApplySort(preserveSelection: false);
        LookModeChanged?.Invoke(true);
        SetNeedsDraw();
    }

    // Sort the target list in-place. When preserveSelection is true
    // we re-find the currently-selected monster post-sort so Tab
    // cycling position stays anchored to the same target after the
    // user presses T. On EnterLookMode we skip preservation and snap
    // to index 0.
    //
    // Each sort uses strong tie-breaking (ThenBy chains) so that
    // homogeneous mob groups (e.g. 4 fresh F1 Kobolds all at 100% HP,
    // same level) still produce a visibly distinct order per sort
    // mode. Without tie-breaking, OrderBy's stable sort would leave
    // the list untouched and the feature would read as "doesn't work."
    private void ApplySort(bool preserveSelection)
    {
        if (_lookTargets.Count == 0) return;
        var current = preserveSelection ? _lookTargets[_lookIndex] : null;

        _lookTargets = _lookSort switch
        {
            // Dist: closest first, then weakest among ties
            // (lower-level mobs surface first when equidistant).
            LookSort.Dist => _lookTargets
                .OrderBy(m => Chebyshev(m))
                .ThenBy(m => m.Level)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList(),

            // HP %: lowest first (finish-off priority). Tie-break by
            // distance so nearby stragglers surface over distant ones.
            LookSort.HP => _lookTargets
                .OrderBy(m => HpPct(m))
                .ThenBy(m => Chebyshev(m))
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList(),

            // Threat: deadliest first (highest level-diff vs player).
            // Tie-break by distance (closer = more immediate threat).
            LookSort.Threat => _lookTargets
                .OrderByDescending(m => m.Level - _player.Level)
                .ThenBy(m => Chebyshev(m))
                .ThenBy(m => HpPct(m))
                .ToList(),

            // Level: absolute level desc. Tie-break by HP % asc so
            // low-HP high-level mobs surface (best gank targets).
            LookSort.Level => _lookTargets
                .OrderByDescending(m => m.Level)
                .ThenBy(m => HpPct(m))
                .ThenBy(m => Chebyshev(m))
                .ToList(),

            // Index: as GetVisibleMonsters returned them (no sort).
            // This is the one mode where we preserve the caller's
            // natural iteration order — acts as an "unsorted" reset.
            LookSort.Index => _lookTargets,

            _ => _lookTargets,
        };

        if (current != null)
        {
            int idx = _lookTargets.IndexOf(current);
            _lookIndex = idx >= 0 ? idx : 0;
        }
        else _lookIndex = 0;
    }

    // HP % helper used by sort keys. Defined once here to guarantee
    // identical behavior across Dist/HP/Threat/Level tie-breakers and
    // the mini-bar render (prevents drift bugs where one call site
    // rounds differently than another).
    private static int HpPct(Monster m) =>
        m.MaxHealth > 0 ? m.CurrentHealth * 100 / m.MaxHealth : 100;

    private void CycleSort()
    {
        _lookSort = (LookSort)(((int)_lookSort + 1) % 5);
        ApplySort(preserveSelection: true);
        SetNeedsDraw();
    }

    // Jump directly to numbered target (1-based). Silent no-op if
    // the target number is out of range (user pressed 5 but there
    // are only 3 targets), so presses don't surprise-exit look mode.
    private void JumpToTarget(int oneBasedIdx)
    {
        int zeroIdx = oneBasedIdx - 1;
        if (zeroIdx < 0 || zeroIdx >= _lookTargets.Count) return;
        _lookIndex = zeroIdx;
        SetNeedsDraw();
    }

    // Chebyshev (king-move) distance — matches the inline calc in
    // BuildStatLines so the sidebar's Dist column stays consistent
    // with the floating stat panel's Dist:N readout.
    private int Chebyshev(Monster m) =>
        Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y));

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
            // T cycles sort mode (Dist → HP → Threat → Level → Index).
            // Outside look mode, T is bound to EquipmentRequested in
            // MapView.Input.cs — that binding is untouched because
            // HandleLookModeKey runs first and consumes the key here.
            case KeyCode.T:
                CycleSort();
                keyEvent.Handled = true;
                return true;
            // Numeric jump 1-9 → target by index. Outside look mode,
            // D1-D5 trigger QuickUseRequested (consumables hotbar) —
            // that binding is likewise shadowed only while look mode
            // is active. Out-of-range indices are silent no-ops so
            // the player doesn't get kicked out of look mode.
            case KeyCode.D1: JumpToTarget(1); keyEvent.Handled = true; return true;
            case KeyCode.D2: JumpToTarget(2); keyEvent.Handled = true; return true;
            case KeyCode.D3: JumpToTarget(3); keyEvent.Handled = true; return true;
            case KeyCode.D4: JumpToTarget(4); keyEvent.Handled = true; return true;
            case KeyCode.D5: JumpToTarget(5); keyEvent.Handled = true; return true;
            case KeyCode.D6: JumpToTarget(6); keyEvent.Handled = true; return true;
            case KeyCode.D7: JumpToTarget(7); keyEvent.Handled = true; return true;
            case KeyCode.D8: JumpToTarget(8); keyEvent.Handled = true; return true;
            case KeyCode.D9: JumpToTarget(9); keyEvent.Handled = true; return true;
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

        // When the Target List sidebar is visible, shrink the width
        // the floating stat panel can use so it prefers left-of-target
        // placement and doesn't overdraw the sidebar.
        bool sidebarVisible = vpWidth >= LookPanelMinVpW && _lookTargets.Count > 0;
        int effectiveVpW = sidebarVisible ? vpWidth - LookPanelW - 1 : vpWidth;
        RenderStatPanel(selected, sx, sy, effectiveVpW, vpHeight);

        if (sidebarVisible) RenderTargetListPanel(vpWidth, vpHeight);

        // Hint line reflects capabilities — multi-target shows sort
        // cycle + jump hints; single-target strips them since T/1-9
        // would be silent no-ops with one entry.
        string hint = _lookTargets.Count > 1
            ? $"[{_lookIndex + 1}/{_lookTargets.Count}] Tab/Arrows cycle  T sort  1-9 jump  L/Esc exit"
            : "[1/1] L/Esc exit";
        int hintX = Math.Max(0, (vpWidth - hint.Length) / 2);
        DrawTextAtView(hintX, vpHeight - 1, hint,
            Gfx.Attr(Color.DarkGray, Color.Black), vpWidth, vpHeight);
    }

    // FB-462 Target List Panel — 22-col right-side overview of all
    // visible hostiles. Rows render with threat-tier color, cursor
    // arrow on the selected target, mini 4-cell HP bar, and Chebyshev
    // distance. Footer shows active sort + key hints.
    private void RenderTargetListPanel(int vpWidth, int vpHeight)
    {
        int panelX = vpWidth - LookPanelW - 1;
        int panelY = 1;
        // Reserve 3 rows for header + 2 footer rows ("Sort: ..." + key
        // legend). Data rows scroll to keep _lookIndex visible so a
        // 20-target dungeon doesn't clip the selected row off-screen.
        int reservedChrome = 3 + 2;                              // header top + sort row + hint row
        int maxDataRows = Math.Max(3, vpHeight - reservedChrome - panelY - 2);
        int dataRows = Math.Min(_lookTargets.Count, maxDataRows);
        int panelH = dataRows + reservedChrome;

        var bg       = new Color(20, 20, 30);
        var boxAttr  = Gfx.Attr(Color.White,    bg);
        var borderAt = Gfx.Attr(Color.DarkGray, bg);
        var headerAt = Gfx.Attr(Color.BrightYellow, bg);
        var dimAt    = Gfx.Attr(Color.DarkGray, bg);

        // Fill background so map glyphs don't bleed through.
        Driver!.SetAttribute(boxAttr);
        for (int r = 0; r < panelH && panelY + r < vpHeight; r++)
        {
            for (int c = 0; c < LookPanelW && panelX + c < vpWidth; c++)
            {
                Move(panelX + c, panelY + r);
                Driver!.AddRune(new System.Text.Rune(' '));
            }
        }

        // Top border + title (minimal — "Targets" only). Count and sort
        // mode live in the footer readout below so the header stays clean.
        DrawHLine(panelX, panelY, LookPanelW, '─', borderAt);
        DrawTextAtView(panelX + 1, panelY, " Targets ", headerAt, vpWidth, vpHeight);

        // Scroll window keeps selected row centered when possible.
        int scrollStart = 0;
        if (_lookTargets.Count > dataRows)
        {
            scrollStart = Math.Max(0, _lookIndex - dataRows / 2);
            scrollStart = Math.Min(scrollStart, _lookTargets.Count - dataRows);
        }

        for (int r = 0; r < dataRows; r++)
        {
            int idx = scrollStart + r;
            if (idx >= _lookTargets.Count) break;
            var m = _lookTargets[idx];

            int rowY = panelY + 1 + r;
            if (rowY >= vpHeight) break;

            bool isSelected = idx == _lookIndex;
            var rowAttr = GetThreatAttribute(m);
            // Dim the row bg slightly on non-selected for readability.
            if (!isSelected) rowAttr = Gfx.Attr(ExtractFg(rowAttr), bg);

            char cursor = isSelected ? '▶' : ' ';
            // Show the 1-based index so the user can map visible rows
            // to the 1-9 jump keys directly.
            int displayNum = idx + 1;
            string numStr = displayNum <= 9 ? displayNum.ToString() : " ";
            string name = TruncName(m.Name, LookNameMaxLen);
            string hpMini = BuildMiniHpBar(m.CurrentHealth, m.MaxHealth, 4);
            int dist = Chebyshev(m);
            string distStr = dist > 99 ? "99" : dist.ToString();

            // Row layout: cursor + num + name(15) + hpBar(4) + dist(2)
            // = 1 + 1 + 15 + 4 + 2 + gutters — fits in 22 cols:
            //   "▶1 Ruin Kobold  ▓▓░░ 3"
            string row = $"{cursor}{numStr} {name,-15} {hpMini} {distStr,2}";
            if (row.Length > LookPanelW - 2) row = row[..(LookPanelW - 2)];
            DrawTextAtView(panelX + 1, rowY, row, rowAttr, vpWidth, vpHeight);
        }

        // Footer divider + sort row + hint row.
        int footerY = panelY + 1 + dataRows;
        if (footerY < vpHeight) DrawHLine(panelX, footerY, LookPanelW, '─', borderAt);

        // Footer row: count + total targets as a fallback readout, so
        // even on narrow panels where the title truncates, the user
        // has a second confirmation the sort cycled.
        int countRowY = footerY + 1;
        if (countRowY < vpHeight)
        {
            string countLabel = $" {_lookIndex + 1}/{_lookTargets.Count}  {SortLabel(_lookSort)}";
            if (countLabel.Length > LookPanelW - 2) countLabel = countLabel[..(LookPanelW - 2)];
            DrawTextAtView(panelX + 1, countRowY, countLabel, headerAt, vpWidth, vpHeight);
        }

        int hintRowY = footerY + 2;
        if (hintRowY < vpHeight)
        {
            string legend = " T:sort 1-9:jump";
            if (legend.Length > LookPanelW - 2) legend = legend[..(LookPanelW - 2)];
            DrawTextAtView(panelX + 1, hintRowY, legend, dimAt, vpWidth, vpHeight);
        }
    }

    // Short, readable sort-mode label for the header + footer readouts.
    // Using explicit strings (not enum ToString) so we can shorten or
    // decorate later without changing the enum values, which are also
    // saved implicitly in the sort cycle order.
    private static string SortLabel(LookSort s) => s switch
    {
        LookSort.Dist   => "Dist",
        LookSort.HP     => "HP%",
        LookSort.Threat => "Threat",
        LookSort.Level  => "Level",
        LookSort.Index  => "Index",
        _               => "?",
    };

    // Cosmetic: build a 4-cell HP bar without brackets, using the
    // same █/░ convention as the stat panel gradient bar. We need
    // a bracket-free variant because the sidebar column is tight.
    private static string BuildMiniHpBar(int cur, int max, int width)
    {
        if (max <= 0 || width <= 0) return new string('░', Math.Max(0, width));
        int filled = Math.Clamp(cur * width / max, 0, width);
        // Guarantee at least 1 filled cell if the monster is alive —
        // otherwise 1% HP reads as identical to dead.
        if (cur > 0 && filled == 0) filled = 1;
        return new string('█', filled) + new string('░', width - filled);
    }

    private static string TruncName(string name, int max)
    {
        if (string.IsNullOrEmpty(name)) return new string(' ', max);
        return name.Length > max ? name[..max] : name;
    }

    // Pull the foreground Color out of a Terminal.Gui Attribute so we
    // can re-pair it with the dimmer panel bg for non-selected rows.
    // Uses the public Foreground/Background record accessors.
    private static Color ExtractFg(Terminal.Gui.Attribute a) => a.Foreground;

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
