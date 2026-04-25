using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Bundle 13 Item 6 — Ranged Fire with Reticle.
// Modal targeting overlay: arrows move reticle within a Chebyshev radius (max range),
// Tab cycles between visible monsters in range, Enter/Space confirms (fires at the
// reticle if a valid target sits there), Esc cancels (no turn consumed).
// Used for Bow basic-attack (Q14 LOCK) and any sword skill with Range>1; melee
// (Range=1) bumps remain unchanged. Pattern forks MapView.LookMode.cs.
public partial class MapView
{
    // Sidebar gating mirrors LookMode — reticle works fine without it on narrow terms.
    private const int RangedSidebarMinVpW = 60;
    private const int RangedSidebarW      = 22;

    // What the confirm key does once the reticle lands on a target.
    private enum RangedFireKind
    {
        BowBasic,    // Q12: same dmg formula as melee. Routes via TurnManager.ExecuteBowShot.
        SwordSkill,  // Q14: Range>1 sword skill. Routes via SkillTargetOverride + ExecuteSwordSkill.
    }

    private bool _rangedFireActive;
    private int _rangedFireMaxRange = 1;
    private int _rangedReticleX, _rangedReticleY;
    private RangedFireKind _rangedKind;
    // Slot index for SwordSkill kind; -1 for BowBasic (unused).
    private int _rangedSkillSlot = -1;

    public bool IsRangedFireModeActive => _rangedFireActive;
    public event Action<bool>? RangedFireModeChanged;
    // Bow basic-attack confirm: (mapX, mapY) of reticle. GameScreen wires to TurnManager.ExecuteBowShot.
    public event Action<int, int>? RangedFireRequested;
    // Sword-skill confirm: (slotIdx, mapX, mapY). GameScreen sets SkillTargetOverride then ExecuteSwordSkill(slot).
    public event Action<int, int, int>? RangedSkillFireRequested;

    // Open the reticle for Bow basic-attack. maxRange should be Weapon.Range + BowRangeOverflow.
    public void EnterBowReticle(int maxRange)
    {
        if (_rangedFireActive) return;
        if (_lookModeActive) ExitLookMode();
        _rangedKind = RangedFireKind.BowBasic;
        _rangedSkillSlot = -1;
        OpenReticle(maxRange);
    }

    // Open the reticle for a sword skill at slot N. maxRange = skill.Range (+BowRangeOverflow if Bow).
    public void EnterSkillReticle(int slotIdx, int maxRange)
    {
        if (_rangedFireActive) return;
        if (_lookModeActive) ExitLookMode();
        _rangedKind = RangedFireKind.SwordSkill;
        _rangedSkillSlot = slotIdx;
        OpenReticle(maxRange);
    }

    private void OpenReticle(int maxRange)
    {
        _rangedFireMaxRange = Math.Max(1, maxRange);
        // Snap reticle to the closest visible enemy in range; otherwise to the player tile.
        var snap = NearestVisibleEnemyInRange();
        if (snap.HasValue) (_rangedReticleX, _rangedReticleY) = snap.Value;
        else (_rangedReticleX, _rangedReticleY) = (_player.X, _player.Y);
        _rangedFireActive = true;
        RangedFireModeChanged?.Invoke(true);
        SetNeedsDraw();
    }

    public void ExitRangedFireMode()
    {
        if (!_rangedFireActive) return;
        _rangedFireActive = false;
        _rangedSkillSlot = -1;
        RangedFireModeChanged?.Invoke(false);
        SetNeedsDraw();
    }

    // Nearest visible monster (Chebyshev) inside _rangedFireMaxRange. Used for initial snap + Tab cycle.
    private (int X, int Y)? NearestVisibleEnemyInRange()
    {
        Monster? best = null; int bestD = int.MaxValue;
        foreach (var m in _map.Monsters)
        {
            if (m.IsDefeated) continue;
            if (!_map.IsVisible(m.X, m.Y)) continue;
            int d = Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y));
            if (d == 0 || d > _rangedFireMaxRange) continue;
            if (d < bestD) { bestD = d; best = m; }
        }
        return best != null ? (best.X, best.Y) : null;
    }

    private List<Monster> VisibleEnemiesInRange()
    {
        var list = new List<Monster>();
        foreach (var m in _map.Monsters)
        {
            if (m.IsDefeated) continue;
            if (!_map.IsVisible(m.X, m.Y)) continue;
            int d = Math.Max(Math.Abs(m.X - _player.X), Math.Abs(m.Y - _player.Y));
            if (d == 0 || d > _rangedFireMaxRange) continue;
            list.Add(m);
        }
        list.Sort((a, b) =>
        {
            int da = Math.Max(Math.Abs(a.X - _player.X), Math.Abs(a.Y - _player.Y));
            int db = Math.Max(Math.Abs(b.X - _player.X), Math.Abs(b.Y - _player.Y));
            return da != db ? da.CompareTo(db) : a.Id.CompareTo(b.Id);
        });
        return list;
    }

    private void CycleReticleToNextEnemy(int dir)
    {
        var enemies = VisibleEnemiesInRange();
        if (enemies.Count == 0) return;
        int idx = enemies.FindIndex(m => m.X == _rangedReticleX && m.Y == _rangedReticleY);
        if (idx < 0) idx = (dir > 0) ? -1 : enemies.Count;
        idx = (idx + dir + enemies.Count) % enemies.Count;
        _rangedReticleX = enemies[idx].X;
        _rangedReticleY = enemies[idx].Y;
        SetNeedsDraw();
    }

    // Move the reticle by (dx,dy). Clamp to player-centered Chebyshev <= max range
    // and to map bounds. Out-of-range pushes are ignored (silent — no surprise exit).
    private void MoveReticle(int dx, int dy)
    {
        int nx = _rangedReticleX + dx;
        int ny = _rangedReticleY + dy;
        if (!_map.InBounds(nx, ny)) return;
        int d = Math.Max(Math.Abs(nx - _player.X), Math.Abs(ny - _player.Y));
        if (d > _rangedFireMaxRange || d == 0) return;
        _rangedReticleX = nx;
        _rangedReticleY = ny;
        SetNeedsDraw();
    }

    private void ConfirmRangedFire()
    {
        // Confirm fires only when the reticle sits on a visible, undefeated monster.
        if (!_map.InBounds(_rangedReticleX, _rangedReticleY)) { ExitRangedFireMode(); return; }
        if (!_map.IsVisible(_rangedReticleX, _rangedReticleY)) return;
        var occ = _map.GetTile(_rangedReticleX, _rangedReticleY).Occupant;
        if (occ is not Monster m || m.IsDefeated) return;

        int tx = _rangedReticleX, ty = _rangedReticleY;
        var kind = _rangedKind;
        int slot = _rangedSkillSlot;
        // Close the overlay BEFORE firing — the resulting turn loop redraws the world.
        ExitRangedFireMode();

        if (kind == RangedFireKind.BowBasic)
            RangedFireRequested?.Invoke(tx, ty);
        else
            RangedSkillFireRequested?.Invoke(slot, tx, ty);
    }

    // Returns true to swallow the key from the main map handler. Mirrors HandleLookModeKey.
    private bool HandleRangedFireKey(Key keyEvent)
    {
        if (!_rangedFireActive) return false;
        var bareKey = keyEvent.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        // Backslash (rune match — v2 KeyCode has no Backslash member) toggles back off.
        if (keyEvent.AsRune.Value == '\\')
        {
            ExitRangedFireMode();
            keyEvent.Handled = true;
            return true;
        }

        switch (bareKey)
        {
            case KeyCode.Esc:
                ExitRangedFireMode();
                keyEvent.Handled = true;
                return true;
            case KeyCode.Enter:
            case KeyCode.Space:
                ConfirmRangedFire();
                keyEvent.Handled = true;
                return true;
            case KeyCode.Tab:
                CycleReticleToNextEnemy(keyEvent.IsShift ? -1 : 1);
                keyEvent.Handled = true;
                return true;
            // Cardinal nudges. WASD doubles as arrows so muscle memory carries over.
            case KeyCode.CursorUp:    case KeyCode.W: MoveReticle( 0, -1); keyEvent.Handled = true; return true;
            case KeyCode.CursorDown:  case KeyCode.S: MoveReticle( 0,  1); keyEvent.Handled = true; return true;
            case KeyCode.CursorLeft:  case KeyCode.A: MoveReticle(-1,  0); keyEvent.Handled = true; return true;
            case KeyCode.CursorRight: case KeyCode.D: MoveReticle( 1,  0); keyEvent.Handled = true; return true;
            // Diagonals — mirrors the QEZC main map binding so reticle aim feels native.
            case KeyCode.Q: MoveReticle(-1, -1); keyEvent.Handled = true; return true;
            case KeyCode.E: MoveReticle( 1, -1); keyEvent.Handled = true; return true;
            case KeyCode.Z: MoveReticle(-1,  1); keyEvent.Handled = true; return true;
            case KeyCode.C: MoveReticle( 1,  1); keyEvent.Handled = true; return true;
            default:
                // Unknown key: stay in mode (don't surprise-exit). Swallow so map keys don't fire.
                keyEvent.Handled = true;
                return true;
        }
    }

    private void RenderRangedFireMode(int vpWidth, int vpHeight)
    {
        if (!_rangedFireActive) return;

        var bgTile  = new Color(20, 20, 30);
        var inAttr  = Gfx.Attr(Color.BrightCyan, bgTile);
        var outAttr = Gfx.Attr(Color.DarkGray,   Color.Black);
        var retAttr = Gfx.Attr(Color.BrightYellow, Color.Black);

        // Range halo: tint every Chebyshev tile inside max range with a subtle BG.
        // Out-of-range halo isn't drawn (would overpaint the entire viewport).
        for (int dy = -_rangedFireMaxRange; dy <= _rangedFireMaxRange; dy++)
        for (int dx = -_rangedFireMaxRange; dx <= _rangedFireMaxRange; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int mx = _player.X + dx, my = _player.Y + dy;
            if (!_map.InBounds(mx, my)) continue;
            int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (d > _rangedFireMaxRange) continue;
            int vx = MapToVx(mx), vy = MapToVy(my);
            if (vx < 0 || vy < 0 || vx >= vpWidth || vy >= vpHeight) continue;

            // Don't paint over visible enemy tiles — keep their glyph readable.
            if (_map.IsVisible(mx, my) && _map.GetTile(mx, my).Occupant is Monster) continue;
            // Outline-style: only paint the ring on the perimeter (d == max), so interior tiles
            // remain unaltered and the silhouette of the firing arc is unambiguous.
            if (d == _rangedFireMaxRange)
            {
                bool inFov = _map.IsVisible(mx, my);
                Driver!.SetAttribute(inFov ? inAttr : outAttr);
                Move(vx, vy);
                Driver!.AddRune(new System.Text.Rune('·'));
            }
        }

        // Reticle glyph: '+' is wide-distinct on the SAO ASCII palette.
        int rvx = MapToVx(_rangedReticleX), rvy = MapToVy(_rangedReticleY);
        if (rvx >= 0 && rvy >= 0 && rvx < vpWidth && rvy < vpHeight)
        {
            Driver!.SetAttribute(retAttr);
            Move(rvx, rvy);
            Driver!.AddRune(new System.Text.Rune('+'));
        }

        // Aim sidebar — mirrors LookMode panel placement when terminal is wide enough.
        if (vpWidth >= RangedSidebarMinVpW)
            RenderRangedSidebar(vpWidth, vpHeight);

        // Hint row at the bottom — keyboard-only legend.
        string hint = $"AIM  Arrows/WASD/QEZC move  Tab next enemy  Enter fire  Esc cancel  (range {_rangedFireMaxRange})";
        if (hint.Length > vpWidth) hint = hint[..vpWidth];
        int hintX = Math.Max(0, (vpWidth - hint.Length) / 2);
        DrawTextAtView(hintX, vpHeight - 1, hint,
            Gfx.Attr(Color.DarkGray, Color.Black), vpWidth, vpHeight);
    }

    // Right-hand "Aim" panel — tile coords, distance, target name (or "empty").
    private void RenderRangedSidebar(int vpWidth, int vpHeight)
    {
        int panelX = vpWidth - RangedSidebarW - 1;
        int panelY = 1;
        int panelH = 7;
        var bg       = new Color(20, 20, 30);
        var boxAttr  = Gfx.Attr(Color.White,        bg);
        var borderAt = Gfx.Attr(Color.DarkGray,     bg);
        var headerAt = Gfx.Attr(Color.BrightYellow, bg);
        var dimAt    = Gfx.Attr(Color.DarkGray,     bg);

        Driver!.SetAttribute(boxAttr);
        for (int r = 0; r < panelH && panelY + r < vpHeight; r++)
        for (int c = 0; c < RangedSidebarW && panelX + c < vpWidth; c++)
        {
            Move(panelX + c, panelY + r);
            Driver!.AddRune(new System.Text.Rune(' '));
        }
        DrawHLine(panelX, panelY, RangedSidebarW, '─', borderAt);

        string title = _rangedKind == RangedFireKind.BowBasic ? " Aim: Bow " : " Aim: Skill ";
        DrawTextAtView(panelX + 1, panelY, title, headerAt, vpWidth, vpHeight);

        int dist = Math.Max(Math.Abs(_rangedReticleX - _player.X),
                            Math.Abs(_rangedReticleY - _player.Y));
        string targetName = "(empty)";
        Color targetFg = Color.DarkGray;
        if (_map.InBounds(_rangedReticleX, _rangedReticleY)
            && _map.IsVisible(_rangedReticleX, _rangedReticleY)
            && _map.GetTile(_rangedReticleX, _rangedReticleY).Occupant is Monster m
            && !m.IsDefeated)
        {
            targetName = m.Name;
            targetFg = Color.BrightYellow;
        }
        else if (_map.InBounds(_rangedReticleX, _rangedReticleY)
                 && !_map.IsVisible(_rangedReticleX, _rangedReticleY))
        {
            targetName = "(unseen)";
        }

        bool inRange = dist > 0 && dist <= _rangedFireMaxRange;
        Color distFg = inRange ? Color.White : Color.BrightRed;

        DrawTextAtView(panelX + 1, panelY + 1, $"Tile ({_rangedReticleX},{_rangedReticleY})",
            Gfx.Attr(Color.White, bg), vpWidth, vpHeight);
        DrawTextAtView(panelX + 1, panelY + 2, $"Dist {dist}/{_rangedFireMaxRange}",
            Gfx.Attr(distFg, bg), vpWidth, vpHeight);
        // Truncate target name to fit panel width minus borders.
        string nameLine = targetName.Length > RangedSidebarW - 2
            ? targetName[..(RangedSidebarW - 2)] : targetName;
        DrawTextAtView(panelX + 1, panelY + 3, nameLine,
            Gfx.Attr(targetFg, bg), vpWidth, vpHeight);
        DrawTextAtView(panelX + 1, panelY + 4, inRange ? "[in range]" : "[out of range]",
            Gfx.Attr(distFg, bg), vpWidth, vpHeight);
        DrawTextAtView(panelX + 1, panelY + 5, "Tab next  Enter fire",
            dimAt, vpWidth, vpHeight);

        int footerY = panelY + panelH - 1;
        if (footerY < vpHeight) DrawHLine(panelX, footerY, RangedSidebarW, '─', borderAt);
    }
}
