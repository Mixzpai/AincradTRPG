using SAOTRPG.Entities;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems.Input;
using SAOTRPG.Systems.Social;
using SAOTRPG.Systems.Turns;
using SAOTRPG.Systems.World;
using SAOTRPG.UI.Helpers;
using EquipSlot = SAOTRPG.Inventory.Core.EquipmentSlot;

namespace SAOTRPG.UI.Widgets;

// The sidebar's character panel, inside the STATUS card: who you are, your numbers, what you
// carry, the floor, and anything else in play. Painted cell by cell: the Label it replaced re-ran
// Terminal.Gui's text layout on every redraw, and its 16 rows held 18 lines (rule 463).
//
// It fits whatever height the sidebar gives it (SidebarLayout): sections are kept by priority,
// painted in reading order, and a dropped section leaves a key hint to the full character sheet.
public class StatsPanelWidget : View
{
    private enum Part { Identity, Combat, Attributes, Gear, Floor, Extras, Hint }

    private static readonly int[] Rows = { 2, 3, 3, 4, 3, 0, 1 };      // per Part; Extras varies
    private static readonly Part[] KeepFirst = { Part.Identity, Part.Floor, Part.Extras, Part.Combat, Part.Gear, Part.Attributes };
    private const int MaxExtras = 3;

    // The smallest panel: identity, the floor, and the hint row.
    public const int MinHeight = 2 + 3 + 1;

    private readonly Player _player;
    private readonly TurnManager _turns;
    private Snapshot? _shown;
    private List<(string Text, Color Fg)> _extras = new();

    private readonly record struct Gear(string Tag, string Name, int Pct);   // Pct -1: does not wear

    // Everything the panel shows, compared as a value so an unchanged HUD tick repaints nothing.
    private sealed record Snapshot(
        string Name, string Title, int Level, int SkillPoints,
        int Atk, int Def, int Spd, int Crt, int Cd, int Sd,
        int Vit, int Str, int End, int Dex, int Agi, int Int,
        Gear Weapon, Gear Armor, Gear OffHand,
        int Floor, bool Labyrinth, string Biome, string BiomeEffect, int Explored,
        int Kills, int FloorTurns, int Par, string Phase, string Danger, string ExtrasKey);

    public StatsPanelWidget(Player player, TurnManager turns)
    {
        _player = player;
        _turns = turns;
        CanFocus = false;
    }

    // Every row with nothing cut, for the current extras.
    public int PreferredHeight => Rows.Take(5).Sum() + ExtrasRows(_extras.Count);

    private static int ExtrasRows(int count) => Math.Min(count, MaxExtras);

    // The rows the panel paints when given this height, for the current extras.
    public int RowsAt(int height) => Plan(height, _extras.Count).RowsUsed;

    // Children repaint every cell they own, and the framework clear would blank this widget
    // during the window where it skips painting beneath a dialog (rule 23).
    protected override bool OnClearingViewport() => true;

    // Called every HUD tick; marks the panel for redraw only when something it shows changed.
    public void Refresh()
    {
        var snap = Build(out var extras);
        if (snap == _shown) return;
        _shown = snap;
        _extras = extras;
        SetNeedsDraw();
    }

    private Snapshot Build(out List<(string, Color)> extras)
    {
        var t = ColorSchemes.Theme;
        var p = _player;
        extras = new List<(string, Color)>();

        // The fatigue rule's own penalties decide the line, so it cannot disagree with combat.
        int atkPen = _turns.FatigueAtkPenalty, defPen = _turns.FatigueDefPenalty;
        if (defPen != 0) extras.Add(($"Exhausted  ATK {atkPen}  DEF {defPen}  rest soon", t.Crit));
        else if (atkPen != 0) extras.Add(($"Fatigued  ATK {atkPen}  rest when you can", t.Warn));

        if (_turns.BountyTarget is { } target)
            extras.Add(_turns.BountyComplete
                ? ($"Bounty  {target}  complete", t.Ok)
                : ($"Bounty  {target}  {_turns.BountyKillsCurrent}/{_turns.BountyKillsNeeded}", t.Body));

        foreach (var a in PartySystem.Members)
            extras.Add(a.IsDefeated
                ? ($"{a.Name}  Lv {a.Level}  down", t.Crit)
                : ($"{a.Name}  Lv {a.Level}  HP {a.CurrentHealth}/{a.MaxHealth}", t.Body));

        int floor = _turns.CurrentFloor;
        return new Snapshot(
            $"{p.FirstName} {p.LastName}".Trim(), p.EffectiveTitleName(), p.Level, p.SkillPoints,
            p.Attack, p.Defense, p.Speed, p.CriticalRate, p.CriticalHitDamage, p.SkillDamage,
            p.Vitality, p.Strength, p.Endurance, p.Dexterity, p.Agility, p.Intelligence,
            GearIn(EquipSlot.Weapon, "WPN"), GearIn(EquipSlot.Chest, "ARM"), GearIn(EquipSlot.OffHand, "OFF"),
            floor, _turns.InLabyrinth, BiomeSystem.DisplayName, BiomeSystem.EffectSummary,
            _turns.Map.GetExplorationPercent(),
            _turns.KillCount - _turns.FloorKillsStart, _turns.FloorTurns, TurnManager.GetFloorPar(floor),
            SAOTRPG.Map.Lighting.DayNightCycle.PhaseName, _turns.GetFloorDangerLabel(),
            string.Join("\n", extras.Select(e => e.Item1)));
    }

    private Gear GearIn(EquipSlot slot, string tag)
    {
        var item = _player.Inventory.GetEquipped(slot);
        if (item == null) return new Gear(tag, "", -1);
        string name = (item is EquipmentBase eb ? eb.EnhancedName : item.Name) ?? "";
        int pct = item is EquipmentBase w && w.MaxDurability > 0
            ? Math.Clamp(w.ItemDurability * 100 / w.MaxDurability, 0, 100) : -1;
        return new Gear(tag, name, pct);
    }

    // Which parts a panel of this height shows, in reading order, and how many extras rows.
    // Public and pure so a probe can check every height.
    public static (List<int> Parts, int ExtrasShown, int RowsUsed) Plan(int height, int extrasCount)
    {
        int extras = ExtrasRows(extrasCount);
        int full = Rows.Take(5).Sum() + extras;
        var keep = new HashSet<Part>();
        int extrasShown = extras;
        if (height >= full)
        {
            keep.UnionWith(new[] { Part.Identity, Part.Combat, Part.Attributes, Part.Gear, Part.Floor, Part.Extras });
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
                    keep.Add(part);
                    continue;
                }
                if (Rows[(int)part] > budget) continue;
                budget -= Rows[(int)part];
                keep.Add(part);
            }
            keep.Add(Part.Hint);
        }
        var order = new[] { Part.Identity, Part.Combat, Part.Attributes, Part.Gear, Part.Floor, Part.Extras, Part.Hint };
        var parts = order.Where(keep.Contains).ToList();
        int used = parts.Sum(p => p == Part.Extras ? extrasShown : Rows[(int)p]);
        return (parts.Select(p => (int)p).ToList(), extrasShown, used);
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // A modal above us has already painted this pass; drawing now would erase it.
        if (AppHost.IsBeneathTopSession(this)) return true;
        int w = Viewport.Width, h = Viewport.Height;
        if (w <= 0 || h <= 0) return true;
        var s = _shown ??= Build(out _extras);

        var t = ColorSchemes.Theme;
        Attribute emph = Gfx.Attr(t.Emph, t.Base, TextStyle.Bold), body = Gfx.Attr(t.Body, t.Base),
                  value = Gfx.Attr(t.Emph, t.Base), muted = Gfx.Attr(t.Muted, t.Base),
                  accent = Gfx.Attr(t.Accent, t.Base);
        var batch = Gfx.Begin(this);

        int Text(int x, int y, string text, Attribute a)
        {
            int n = 0;
            for (; n < text.Length && x + n < w; n++) batch.Put(x + n, y, text[n], a);
            return x + n;
        }
        void Right(int y, string text, Attribute a) => Text(Math.Max(0, w - text.Length), y, text, a);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) batch.Put(x, y, ' ', body);

        void Section(int y, string caption, string right = "")
        {
            int end = Text(0, y, caption.ToUpperInvariant(), emph);
            int stop = right.Length > 0 ? w - right.Length - 1 : w;
            for (int x = end + 1; x < stop; x++) batch.Put(x, y, ScreenHeader.Hairline, muted);
            if (right.Length > 0) Right(y, right, body);
        }

        // Three label/value columns, the values right-aligned so the numbers line up down the panel.
        int colW = Math.Max(9, w / 3);
        void Stat(int col, int y, string label, string v)
        {
            int x = col * colW;
            Text(x, y, label, muted);
            Text(x + Math.Max(label.Length + 1, colW - 2 - v.Length), y, v, value);
        }

        var (parts, extrasShown, _) = Plan(h, _extras.Count);
        int row = 0;
        foreach (Part part in parts.Select(i => (Part)i))
        {
            switch (part)
            {
                case Part.Identity:
                {
                    string lv = $"Lv {s.Level}";
                    string sp = s.SkillPoints > 0 ? $"{s.SkillPoints} SP  " : "";
                    Text(0, row, TextHelpers.Truncate(s.Name, Math.Max(1, w - lv.Length - sp.Length - 1)), emph);
                    Right(row, lv, accent);
                    if (sp.Length > 0) Text(w - lv.Length - sp.Length, row, sp, Gfx.Attr(t.Ok, t.Base));
                    Text(0, row + 1, TextHelpers.Truncate(s.Title, w), muted);
                    break;
                }
                case Part.Combat:
                    Section(row, "Combat");
                    Stat(0, row + 1, "ATK", $"{s.Atk}"); Stat(1, row + 1, "DEF", $"{s.Def}"); Stat(2, row + 1, "SPD", $"{s.Spd}");
                    Stat(0, row + 2, "CRT", $"{s.Crt}%"); Stat(1, row + 2, "CD", $"+{s.Cd}"); Stat(2, row + 2, "SD", $"{s.Sd}");
                    break;
                case Part.Attributes:
                    Section(row, "Attributes");
                    Stat(0, row + 1, "VIT", $"{s.Vit}"); Stat(1, row + 1, "STR", $"{s.Str}"); Stat(2, row + 1, "END", $"{s.End}");
                    Stat(0, row + 2, "DEX", $"{s.Dex}"); Stat(1, row + 2, "AGI", $"{s.Agi}"); Stat(2, row + 2, "INT", $"{s.Int}");
                    break;
                case Part.Gear:
                {
                    Section(row, "Gear");
                    int gy = row + 1;
                    foreach (var g in new[] { s.Weapon, s.Armor, s.OffHand })
                    {
                        Text(0, gy, g.Tag, muted);
                        string pct = g.Pct >= 0 ? $"{g.Pct}%" : "";
                        if (g.Name.Length == 0) Text(5, gy, "none", muted);
                        else Text(5, gy, TextHelpers.Truncate(g.Name, Math.Max(1, w - 5 - pct.Length - 1)), body);
                        if (pct.Length > 0)
                            Right(gy, pct, Gfx.Attr(g.Pct < 25 ? t.Crit : g.Pct < 50 ? t.Warn : t.Muted, t.Base));
                        gy++;
                    }
                    break;
                }
                case Part.Floor:
                {
                    string where = s.Labyrinth ? $"Labyrinth · F{s.Floor}" : $"Floor {s.Floor} · {s.Biome}";
                    string mapped = $"{s.Explored}% mapped";
                    Section(row, TextHelpers.Truncate(where, Math.Max(1, w - mapped.Length - 3)), mapped);
                    int x = Text(0, row + 1, "Kills ", muted);
                    x = Text(x, row + 1, $"{s.Kills}", body);
                    x = Text(x, row + 1, "   Turn ", muted);
                    x = Text(x, row + 1, $"{s.FloorTurns}", s.FloorTurns > s.Par ? Gfx.Attr(t.Warn, t.Base) : body);
                    Text(x, row + 1, $"/{s.Par}", muted);
                    Right(row + 1, s.Phase, muted);
                    x = Text(0, row + 2, "Danger ", muted);
                    var dangerFg = s.Danger switch
                    {
                        "Deadly" or "Dangerous" => t.Crit,
                        "Hard" => t.Warn,
                        "Safe" or "Trivial" or "Cleared" => t.Ok,
                        _ => t.Body,
                    };
                    x = Text(x, row + 2, s.Danger, Gfx.Attr(dangerFg, t.Base));
                    if (s.BiomeEffect.Length > 0)
                        Text(x, row + 2, "  " + TextHelpers.Truncate(s.BiomeEffect, Math.Max(1, w - x - 2)), muted);
                    break;
                }
                case Part.Extras:
                {
                    // Whatever else is in play. A list the panel cannot fit says so (rule 220).
                    int shown = _extras.Count <= extrasShown ? _extras.Count : Math.Max(0, extrasShown - 1);
                    for (int i = 0; i < shown; i++)
                        Text(0, row + i, TextHelpers.Truncate(_extras[i].Text, w), Gfx.Attr(_extras[i].Fg, t.Base));
                    if (_extras.Count > shown && extrasShown > 0)
                        Text(0, row + shown, $"+{_extras.Count - shown} more", muted);
                    row += extrasShown;
                    continue;
                }
                case Part.Hint:
                {
                    // Something was cut to fit: say what holds the rest.
                    int x = Text(0, row, KeyHints.Key(GameAction.OpenStats).ToLowerInvariant(), accent);
                    x = Text(x, row, " full character sheet", muted);
                    if (extrasShown == 0 && _extras.Count > 0) Text(x, row, $"  +{_extras.Count} more", muted);
                    break;
                }
            }
            row += Rows[(int)part];
        }
        return true;
    }
}
