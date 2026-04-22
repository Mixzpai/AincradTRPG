using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Widgets;

// FB-479 status icon tray — lives in the action bar right of the HP bar.
// Aggregates 7 source categories: combat debuffs, combat buffs, satiety tiers,
// biome/weather debuffs, permanent gear buffs, quickbar cooldown placeholder,
// and active sword-skill buffs. Severity-ordered. Wraps to a second row when
// the first-row width budget is exceeded.
public class StatusTrayWidget : View
{
    private readonly TurnManager _tm;
    private readonly Player _player;
    // Session-local verbose toggle — Shift+S flips. Research §6 says don't persist.
    public bool VerboseMode { get; set; }

    private enum TrayClass { Debuff, Buff, Cooldown }

    // One tray entry: what to draw.
    private readonly struct TrayEntry
    {
        public readonly char Letter;
        public readonly Color Color;
        public readonly int Stacks;
        public readonly int Duration;
        public readonly TrayClass Class;
        public readonly int SeverityOrder;
        public readonly string VerboseName;
        public TrayEntry(char letter, Color color, int stacks, int duration,
            TrayClass c, int severity, string verboseName)
        {
            Letter = letter; Color = color; Stacks = stacks; Duration = duration;
            Class = c; SeverityOrder = severity; VerboseName = verboseName;
        }
    }

    public StatusTrayWidget(TurnManager tm, Player player)
    {
        _tm = tm;
        _player = player;
        CanFocus = false;
        ColorScheme = ColorSchemes.Body;
    }

    public void ToggleVerbose() { VerboseMode = !VerboseMode; SetNeedsDraw(); }

    // Pulls from all 7 sources, deduped by letter (first-wins). Ordering:
    // Debuffs first in severity order, then Buffs sorted soonest-expiring,
    // then Cooldowns last.
    private List<TrayEntry> CollectEntries()
    {
        var entries = new List<TrayEntry>();

        // ── 1) Combat debuffs ────────────────────────────────────────
        if (_tm.IsBleeding)
            entries.Add(new('B', Color.BrightRed, 1, _tm.BleedTurnsLeft,
                TrayClass.Debuff, 10, "Bleed"));
        if (_tm.StunTurnsLeft > 0)
            entries.Add(new('S', Color.BrightYellow, 1, _tm.StunTurnsLeft,
                TrayClass.Debuff, 20, "Stun"));
        if (_tm.IsPoisoned)
            entries.Add(new('P', Color.BrightGreen, 1, _tm.PoisonTurnsLeft,
                TrayClass.Debuff, 40, "Poison"));
        if (_tm.SlowTurnsLeft > 0)
            entries.Add(new('L', Color.Cyan, 1, _tm.SlowTurnsLeft,
                TrayClass.Debuff, 50, "Slow"));

        // ── 2) Satiety tier (hunger side) ────────────────────────────
        if (_tm.Satiety <= 15)
            entries.Add(new('H', Color.BrightRed, 1, 0,
                TrayClass.Debuff, 70, "Starving"));
        else if (_tm.Satiety < 30)
            entries.Add(new('H', Color.Yellow, 1, 0,
                TrayClass.Debuff, 70, "Hungry"));

        // Fatigue / exhaustion — wait-to-rest debuffs read like slow-ticking hunger.
        if (_tm.RestCounter >= 250)
            entries.Add(new('X', Color.BrightRed, 1, 0,
                TrayClass.Debuff, 75, "Exhausted"));
        else if (_tm.RestCounter >= 150)
            entries.Add(new('X', Color.Yellow, 1, 0,
                TrayClass.Debuff, 76, "Fatigued"));

        // ── 3) Biome / weather debuffs ───────────────────────────────
        if (BiomeSystem.Current == BiomeType.Ice)
            entries.Add(new('W', Color.BrightCyan, 1, 0,
                TrayClass.Debuff, 80, "Cold"));
        else if (BiomeSystem.Current == BiomeType.Volcanic)
            entries.Add(new('V', Color.BrightRed, 1, 0,
                TrayClass.Debuff, 81, "Heat"));
        else if (BiomeSystem.Current == BiomeType.Swamp)
            entries.Add(new('G', Color.Green, 1, 0,
                TrayClass.Debuff, 82, "Toxic Fog"));

        // ── 4) Combat buffs ──────────────────────────────────────────
        if (_tm.ShrineBuffTurns > 0)
            entries.Add(new('C', Color.BrightYellow, 1, _tm.ShrineBuffTurns,
                TrayClass.Buff, 100, "Shrine Blessing"));
        if (_tm.LevelUpBuffTurns > 0)
            entries.Add(new('D', Color.BrightMagenta, 1, _tm.LevelUpBuffTurns,
                TrayClass.Buff, 101, "Level Surge"));
        if (_tm.Satiety >= 80)
            entries.Add(new('F', new Color(255, 200, 60), 1, 0,
                TrayClass.Buff, 102, "Well-Fed"));

        // Passive regen tick buff (only when non-bleeding/poisoned).
        if (_tm.Satiety >= 30 && !_tm.IsPoisoned && !_tm.IsBleeding
            && _player.CurrentHealth < _player.MaxHealth)
            entries.Add(new('R', Color.BrightGreen, 1, 0,
                TrayClass.Buff, 103, "Regen"));

        // ── 5) Permanent / gear buffs ────────────────────────────────
        var mainWpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon)
            as Items.Equipment.Weapon;
        var offWpn = _player.Inventory.GetEquipped(EquipmentSlot.OffHand)
            as Items.Equipment.Weapon;
        if (mainWpn != null && offWpn != null
            && DualWieldPairs.IsCanonicalPair(mainWpn.DefinitionId, offWpn.DefinitionId))
            entries.Add(new('E', Color.BrightMagenta, 1, 0,
                TrayClass.Buff, 104, "Pair Resonance"));

        // ── 6) Active sword-skill buff ───────────────────────────────
        if (_tm.IsCounterStance)
            entries.Add(new('K', Color.BrightCyan, 1, 0,
                TrayClass.Buff, 105, "Counter Stance"));

        // ── 7) Quickbar cooldown placeholder (infrastructure, dormant) ──
        // No-op: Q slot reserved for future cooldown system.

        // Sort: Debuffs (by severity asc) → Buffs (by duration asc, 0 last) → Cooldowns.
        entries.Sort((a, b) =>
        {
            int c = ((int)a.Class).CompareTo((int)b.Class);
            if (c != 0) return c;
            if (a.Class == TrayClass.Debuff) return a.SeverityOrder.CompareTo(b.SeverityOrder);
            if (a.Class == TrayClass.Buff)
            {
                int aDur = a.Duration <= 0 ? int.MaxValue : a.Duration;
                int bDur = b.Duration <= 0 ? int.MaxValue : b.Duration;
                return aDur.CompareTo(bDur);
            }
            return a.SeverityOrder.CompareTo(b.SeverityOrder);
        });

        return entries;
    }

    private string FormatEntry(TrayEntry e)
    {
        if (VerboseMode)
        {
            string dur = e.Duration > 0 ? $":{e.Duration}" : "";
            string stacks = e.Stacks > 1 ? $" ×{e.Stacks}" : "";
            return $"[{e.VerboseName}{stacks}{dur}]";
        }
        // Compact: "P×3:4" = 3 stacks, 4 turns. Drop ×N when single-stack.
        string compactStacks = e.Stacks > 1 ? $"×{e.Stacks}" : "";
        string compactDur = e.Duration > 0 ? $":{e.Duration}" : "";
        return $"{e.Letter}{compactStacks}{compactDur}";
    }

    protected override bool OnDrawingContent()
    {
        var vp = Viewport;
        if (vp.Width <= 0 || vp.Height <= 0) return true;

        // Blank out both rows so stale glyphs don't bleed through.
        for (int row = 0; row < vp.Height; row++)
        {
            Driver!.SetAttribute(Gfx.Attr(Color.Black, Color.Black));
            Move(0, row);
            for (int c = 0; c < vp.Width; c++) Driver!.AddRune(new System.Text.Rune(' '));
        }

        var entries = CollectEntries();
        if (entries.Count == 0) return true;

        int col = 0;
        int row1 = 0;
        int row2 = vp.Height > 1 ? 1 : 0;
        int curRow = row1;

        foreach (var e in entries)
        {
            string token = FormatEntry(e);
            int tokenLen = token.Length + 1; // +1 for space gutter
            // Row wrap: if token won't fit on current row, move to second row.
            if (col + tokenLen > vp.Width)
            {
                if (curRow == row1 && vp.Height > 1) { curRow = row2; col = 0; }
                else break; // out of rows — drop remaining
            }
            DrawText(col, curRow, token, e.Color);
            col += tokenLen;
        }
        return true;
    }

    private void DrawText(int x, int y, string text, Color fg)
    {
        Driver!.SetAttribute(Gfx.Attr(fg, Color.Black));
        Move(x, y);
        foreach (var ch in text)
            Driver!.AddRune(new System.Text.Rune(ch));
    }
}
