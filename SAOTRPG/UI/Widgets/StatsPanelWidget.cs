using SAOTRPG.Entities;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems.Input;
using SAOTRPG.Systems.Social;
using SAOTRPG.Systems.Turns;
using SAOTRPG.Systems.World;
using SAOTRPG.UI.Helpers;
using System.Runtime.InteropServices;
using EquipSlot = SAOTRPG.Inventory.Core.EquipmentSlot;

namespace SAOTRPG.UI.Widgets;

// The sidebar's character panel, inside the STATUS card, packed compactly: one labelled line per
// thing (who you are, your numbers, what you hold and wear, the date, the floor), then anything
// else in play. Painted cell by cell rather than through a Label.
//
// It fits whatever height the sidebar gives it (SidebarLayout): lines are kept by priority,
// painted in reading order, and a dropped one leaves a key hint to the full character sheet.
public class StatsPanelWidget : View
{
    private enum Part { Identity, Combat, Attributes, Gear, Time, Floor, Extras, Hint }

    private static readonly int[] Rows = { 1, 1, 1, 2, 1, 2, 0, 1 };      // per Part; Extras varies
    private static readonly Part[] Order =
        { Part.Identity, Part.Combat, Part.Attributes, Part.Gear, Part.Time, Part.Floor, Part.Extras, Part.Hint };
    private static readonly Part[] KeepFirst =
        { Part.Identity, Part.Floor, Part.Time, Part.Extras, Part.Combat, Part.Gear, Part.Attributes };
    private const int MaxExtras = 3;
    private const int FixedRows = 1 + 1 + 1 + 2 + 1 + 2;   // every part but Extras and Hint

    // The smallest panel: identity, the floor, and the hint row.
    public const int MinHeight = 1 + 2 + 1;

    private readonly Player _player;
    private readonly TurnManager _turns;
    private Snapshot _shown;
    private bool _hasShown;
    private List<Extra> _extras = new(), _scratch = new();

    // The panel holds the values it draws from, not the text: the HUD ticks several times a turn,
    // and an unchanged tick must compare in place and allocate nothing. Text is built when drawn.
    // Pct -1: does not wear. Name, Enh and Awk are what EnhancedName is built from.
    private readonly record struct Gear(EquipmentBase? Item, string? Name, int Enh, int Awk, int Pct)
    {
        public string Label => Item?.EnhancedName ?? "";
    }
    private static readonly Gear NoGear = new(null, null, 0, 0, -1);

    private enum ExtraKind { Exhausted, Fatigued, BountyDone, Bounty, AllyDown, Ally }
    private readonly record struct Extra(ExtraKind Kind, string? Name, int A, int B, int C);

    private readonly record struct Snapshot(
        string First, string Last, string Title, int Level, int SkillPoints,
        int Atk, int Def, int Spd, int Crt, int Cd, int Sd,
        int Vit, int Str, int End, int Dex, int Agi, int Int,
        Gear Weapon, Gear Armor, Gear OffHand, int Turn,
        int Floor, bool Labyrinth, string Biome, int Explored,
        int Kills, int FloorTurns, int Par, string Phase, string Danger);

    public StatsPanelWidget(Player player, TurnManager turns)
    {
        _player = player;
        _turns = turns;
        CanFocus = false;
    }

    // Every row with nothing cut, for the current extras.
    public int PreferredHeight => FixedRows + ExtrasRows(_extras.Count);

    private static int ExtrasRows(int count) => Math.Min(count, MaxExtras);

    // The rows the panel paints when given this height, for the current extras.
    public int RowsAt(int height)
    {
        Keep(height, _extras.Count, out _, out int used);
        return used;
    }

    // The draw pass paints every cell, and the framework clear would blank this widget during
    // the window where it skips painting beneath a dialog.
    protected override bool OnClearingViewport() => true;

    // Called every HUD tick; marks the panel for redraw only when something it shows changed.
    public void Refresh()
    {
        if (Capture()) SetNeedsDraw();
    }

    // Reads the panel's inputs; true when they changed since the last capture.
    private bool Capture()
    {
        var snap = Build(_scratch);
        if (_hasShown && snap == _shown
            && CollectionsMarshal.AsSpan(_scratch).SequenceEqual(CollectionsMarshal.AsSpan(_extras)))
            return false;
        _shown = snap;
        _hasShown = true;
        (_extras, _scratch) = (_scratch, _extras);
        return true;
    }

    private Snapshot Build(List<Extra> extras)
    {
        var p = _player;
        extras.Clear();

        // The fatigue rule's own penalties decide the line, so it cannot disagree with combat.
        int atkPen = _turns.FatigueAtkPenalty, defPen = _turns.FatigueDefPenalty;
        if (defPen != 0) extras.Add(new(ExtraKind.Exhausted, null, atkPen, defPen, 0));
        else if (atkPen != 0) extras.Add(new(ExtraKind.Fatigued, null, atkPen, 0, 0));

        if (_turns.BountyTarget is { } target)
            extras.Add(_turns.BountyComplete
                ? new(ExtraKind.BountyDone, target, 0, 0, 0)
                : new(ExtraKind.Bounty, target, _turns.BountyKillsCurrent, _turns.BountyKillsNeeded, 0));

        foreach (var a in PartySystem.Members)
            extras.Add(a.IsDefeated
                ? new(ExtraKind.AllyDown, a.Name, a.Level, 0, 0)
                : new(ExtraKind.Ally, a.Name, a.Level, a.CurrentHealth, a.MaxHealth));

        int floor = _turns.CurrentFloor;
        return new Snapshot(
            p.FirstName, p.LastName, p.EffectiveTitleName(), p.Level, p.SkillPoints,
            p.Attack, p.Defense, p.Speed, p.CriticalRate, p.CriticalHitDamage, p.SkillDamage,
            p.Vitality, p.Strength, p.Endurance, p.Dexterity, p.Agility, p.Intelligence,
            GearIn(EquipSlot.Weapon), GearIn(EquipSlot.Chest), GearIn(EquipSlot.OffHand),
            _turns.TurnCount, floor, _turns.InLabyrinth, BiomeSystem.DisplayName,
            _turns.Map.GetExplorationPercent(),
            _turns.KillCount - _turns.FloorKillsStart, _turns.FloorTurns, TurnManager.GetFloorPar(floor),
            SAOTRPG.Map.Lighting.DayNightCycle.PhaseName, _turns.GetFloorDangerLabel());
    }

    // Writes an extra's line and returns its colour.
    private static Color Line(ref LineWriter o, Extra e, ColorTheme t)
    {
        switch (e.Kind)
        {
            case ExtraKind.Exhausted:
                o.Append("Exhausted  ATK "); o.Append(e.A); o.Append("  DEF "); o.Append(e.B); o.Append("  rest soon");
                return t.Crit;
            case ExtraKind.Fatigued:
                o.Append("Fatigued  ATK "); o.Append(e.A); o.Append("  rest when you can");
                return t.Warn;
            case ExtraKind.BountyDone:
                o.Append("Bounty  "); o.Append(e.Name); o.Append("  complete");
                return t.Ok;
            case ExtraKind.Bounty:
                o.Append("Bounty  "); o.Append(e.Name); o.Append("  "); o.Append(e.A); o.Append('/'); o.Append(e.B);
                return t.Body;
            case ExtraKind.AllyDown:
                o.Append(e.Name); o.Append("  Lv "); o.Append(e.A); o.Append("  down");
                return t.Crit;
            default:
                o.Append(e.Name); o.Append("  Lv "); o.Append(e.A); o.Append("  HP "); o.Append(e.B); o.Append('/'); o.Append(e.C);
                return t.Body;
        }
    }

    private Gear GearIn(EquipSlot slot)
    {
        var item = _player.Inventory.GetEquipped(slot);
        if (item == null) return NoGear;
        int pct = item.MaxDurability > 0 ? Math.Clamp(item.ItemDurability * 100 / item.MaxDurability, 0, 100) : -1;
        return new Gear(item, item.Name, item.EnhancementLevel, (item as Weapon)?.AwakeningLevel ?? 0, pct);
    }

    // Which parts a panel of this height shows, in reading order, and how many extras rows.
    // Public and pure; read by the verification probes.
    public static (List<int> Parts, int ExtrasShown, int RowsUsed) Plan(int height, int extrasCount)
    {
        int keep = Keep(height, extrasCount, out int extrasShown, out int used);
        return (Order.Where(p => Has(keep, p)).Select(p => (int)p).ToList(), extrasShown, used);
    }

    // Plan as a bit per Part, allocating nothing: layout asks at every height it tries.
    private static int Keep(int height, int extrasCount, out int extrasShown, out int used)
    {
        int extras = ExtrasRows(extrasCount);
        int keep = 0;
        extrasShown = extras;
        if (height >= FixedRows + extras)
        {
            foreach (var p in Order) if (p != Part.Hint) keep |= 1 << (int)p;
        }
        else
        {
            int budget = height - Rows[(int)Part.Hint];
            foreach (var part in KeepFirst)
            {
                if (part == Part.Extras)
                {
                    extrasShown = Math.Clamp(budget, 0, extras);
                    budget -= extrasShown;
                    keep |= 1 << (int)part;
                    continue;
                }
                if (Rows[(int)part] > budget) continue;
                budget -= Rows[(int)part];
                keep |= 1 << (int)part;
            }
            keep |= 1 << (int)Part.Hint;
        }
        used = 0;
        foreach (var p in Order) if (Has(keep, p)) used += p == Part.Extras ? extrasShown : Rows[(int)p];
        return keep;
    }

    private static bool Has(int keep, Part p) => (keep & 1 << (int)p) != 0;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above this view has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;
        int w = Viewport.Width, h = Viewport.Height;
        if (w <= 0 || h <= 0) return true;
        if (!_hasShown) Capture();
        var s = _shown;

        var t = ColorSchemes.Theme;
        Attribute emph = Gfx.Attr(t.Emph, t.Base, TextStyle.Bold), body = Gfx.Attr(t.Body, t.Base),
                  value = Gfx.Attr(t.Emph, t.Base), muted = Gfx.Attr(t.Muted, t.Base),
                  accent = Gfx.Attr(t.Accent, t.Base);
        var batch = Gfx.Begin(this);
        // Text is composed in stack memory, one slice per piece a line needs at once, so a
        // redraw allocates nothing.
        const int Piece = 128;
        Span<char> scratch = stackalloc char[3 * Piece];
        Span<char> p0 = scratch[..Piece], p1 = scratch[Piece..(2 * Piece)], p2 = scratch[(2 * Piece)..];

        int Text(int x, int y, ReadOnlySpan<char> text, Attribute a)
        {
            int n = 0;
            for (; n < text.Length && x + n < w; n++) batch.Put(x + n, y, text[n], a);
            return x + n;
        }
        // Text cut to maxLen with a trailing ellipsis, as TextHelpers.Truncate cuts it.
        int Fit(int x, int y, ReadOnlySpan<char> text, int maxLen, Attribute a)
        {
            if (text.Length <= maxLen) return Text(x, y, text, a);
            if (maxLen <= 1) return Text(x, y, text[..Math.Max(0, maxLen)], a);
            return Text(Text(x, y, text[..(maxLen - 1)], a), y, "…", a);
        }
        void Right(int y, ReadOnlySpan<char> text, Attribute a) => Text(Math.Max(0, w - text.Length), y, text, a);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) batch.Put(x, y, ' ', body);

        // A "Str 8  Dex 8" line: label and value pairs, two spaces apart, one if that is all
        // the width allows. Each value is Pre, the number, then Post.
        void Pairs(int y, Span<char> num, params ReadOnlySpan<(string L, string Pre, int V, string Post)> pairs)
        {
            int len = 2 * (pairs.Length - 1);
            foreach (var (l, pre, v, post) in pairs) len += l.Length + 1 + Value(num, pre, v, post).Length;
            int gap = len <= w ? 2 : 1, x = 0;
            foreach (var (l, pre, v, post) in pairs)
            {
                x = Text(x, y, l, muted);
                x = Text(x + 1, y, Value(num, pre, v, post), value) + gap;
            }
        }
        static ReadOnlySpan<char> Value(Span<char> num, string pre, int v, string post)
        {
            var o = new LineWriter(num);
            o.Append(pre); o.Append(v); o.Append(post);
            return num[..o.Length];
        }

        // A labelled gear line: each item's name, then its wear, sharing the width evenly.
        void GearLine(int y, string label, Gear a, Gear b, Span<char> pctBuf)
        {
            int x = Text(0, y, label, muted);
            int count = (a.Label.Length > 0 ? 1 : 0) + (b.Label.Length > 0 ? 1 : 0);
            if (count == 0) { Text(x, y, "nothing", muted); return; }
            int slot = (w - x - 2 * (count - 1)) / count;
            ReadOnlySpan<Gear> both = [a, b];
            foreach (var g in both)
            {
                string name = g.Label;
                if (name.Length == 0) continue;
                ReadOnlySpan<char> pct = g.Pct >= 0 ? Value(pctBuf, " ", g.Pct, "%") : default;
                int end = x + slot;
                x = Fit(x, y, name, Math.Max(1, slot - pct.Length), body);
                if (pct.Length > 0)
                    x = Text(x, y, pct, Gfx.Attr(g.Pct < 25 ? t.Crit : g.Pct < 50 ? t.Warn : t.Muted, t.Base));
                x = Math.Max(x, end) + 2;
            }
        }

        int keep = Keep(h, _extras.Count, out int extrasShown, out _);
        int row = 0;
        foreach (Part part in Order)
        {
            if (!Has(keep, part)) continue;
            switch (part)
            {
                case Part.Identity:
                {
                    var lv = new LineWriter(p0);
                    lv.Append("Lv "); lv.Append(s.Level);
                    var sp = new LineWriter(p1);
                    if (s.SkillPoints > 0) { sp.Append(s.SkillPoints); sp.Append(" SP  "); }
                    int room = Math.Max(1, w - lv.Length - sp.Length - 1);
                    var name = new LineWriter(p2);
                    name.Append(s.First); name.Append(' '); name.Append(s.Last);
                    int x = Fit(0, row, name.Text.Trim(), room, emph);
                    if (s.Title.Length > 0 && x + 3 < room)
                        Fit(x + 2, row, s.Title, room - x - 2, muted);
                    Right(row, lv.Text, accent);
                    if (sp.Length > 0) Text(w - lv.Length - sp.Length, row, sp.Text, Gfx.Attr(t.Ok, t.Base));
                    break;
                }
                case Part.Combat:
                    Pairs(row, p0, ("Atk", "", s.Atk, ""), ("Def", "", s.Def, ""), ("Spd", "", s.Spd, ""),
                                   ("Crt", "", s.Crt, "%"), ("Cd", "+", s.Cd, ""), ("Sd", "", s.Sd, ""));
                    break;
                case Part.Attributes:
                    Pairs(row, p0, ("Vit", "", s.Vit, ""), ("Str", "", s.Str, ""), ("End", "", s.End, ""),
                                   ("Dex", "", s.Dex, ""), ("Agi", "", s.Agi, ""), ("Int", "", s.Int, ""));
                    break;
                case Part.Gear:
                    GearLine(row, "Wield: ", s.Weapon, s.OffHand, p0);
                    GearLine(row + 1, "Wear:  ", s.Armor, NoGear, p0);
                    break;
                case Part.Time:
                {
                    var clock = new LineWriter(p0);
                    clock.Append(s.Phase); clock.Append("  ");
                    clock.Append(p1[..GameClock.WriteTimeOfDay(s.Turn, p1)]);
                    ReadOnlySpan<char> date = p2[..GameClock.WriteDate(s.Turn, p2)];
                    Fit(0, row, date, Math.Max(1, w - clock.Length - 1), body);
                    Right(row, clock.Text, muted);
                    break;
                }
                case Part.Floor:
                {
                    var mapped = new LineWriter(p0);
                    mapped.Append(s.Explored); mapped.Append("% mapped");
                    int x = Text(0, row, "Place: ", muted);
                    var where = new LineWriter(p1);
                    if (s.Labyrinth) { where.Append("Labyrinth of Floor "); where.Append(s.Floor); }
                    else { where.Append("Floor "); where.Append(s.Floor); where.Append(" · "); where.Append(s.Biome); }
                    Fit(x, row, where.Text, Math.Max(1, w - x - mapped.Length - 1), body);
                    Right(row, mapped.Text, muted);

                    x = Text(0, row + 1, "Danger: ", muted);
                    var dangerFg = s.Danger switch
                    {
                        "Deadly" or "Dangerous" => t.Crit,
                        "Hard" => t.Warn,
                        "Safe" or "Trivial" or "Cleared" => t.Ok,
                        _ => t.Body,
                    };
                    x = Text(x, row + 1, s.Danger, Gfx.Attr(dangerFg, t.Base));
                    x = Text(x, row + 1, "  Kills ", muted);
                    x = Text(x, row + 1, Value(p0, "", s.Kills, ""), body);
                    x = Text(x, row + 1, "  Turn ", muted);
                    x = Text(x, row + 1, Value(p0, "", s.FloorTurns, ""), s.FloorTurns > s.Par ? Gfx.Attr(t.Warn, t.Base) : body);
                    x = Text(x, row + 1, Value(p0, "/", s.Par, ""), muted);
                    string effect = BiomeSystem.EffectSummary;
                    if (effect.Length > 0 && x + 3 < w)
                        Fit(x + 2, row + 1, effect, w - x - 2, muted);
                    break;
                }
                case Part.Extras:
                {
                    // Whatever else is in play. A list the panel cannot fit says so.
                    int shown = _extras.Count <= extrasShown ? _extras.Count : Math.Max(0, extrasShown - 1);
                    for (int i = 0; i < shown; i++)
                    {
                        var line = new LineWriter(p0);
                        Color fg = Line(ref line, _extras[i], t);
                        Fit(0, row + i, line.Text, w, Gfx.Attr(fg, t.Base));
                    }
                    if (_extras.Count > shown && extrasShown > 0)
                        Text(0, row + shown, Value(p0, "+", _extras.Count - shown, " more"), muted);
                    row += extrasShown;
                    continue;
                }
                case Part.Hint:
                {
                    // Something was cut to fit: say what holds the rest.
                    int x = Text(0, row, KeyHints.Key(GameAction.OpenStats).ToLowerInvariant(), accent);
                    x = Text(x, row, " full character sheet", muted);
                    if (extrasShown == 0 && _extras.Count > 0) Text(x, row, Value(p0, "  +", _extras.Count, " more"), muted);
                    break;
                }
            }
            row += Rows[(int)part];
        }
        return true;
    }
}
