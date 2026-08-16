using System.Text.Json;
using Terminal.Gui;

namespace SAOTRPG.Systems.Input;

// One rebindable action: what it is, where it applies, and the two chords bound to it.
//
// Two chords rather than one because the game has always accepted WASD *and* the cursor keys for
// movement, and collapsing that to a single binding would be a feature removal disguised as a
// refactor. Either slot may be cleared; an action with neither is simply unbound.
public sealed class ActionBinding
{
    public required GameAction Action { get; init; }
    public required InputContext Context { get; init; }
    public required string Label { get; init; }
    public required KeyChord DefaultPrimary { get; init; }
    public KeyChord DefaultAlternate { get; init; } = KeyChord.None;

    public KeyChord Primary { get; set; }
    public KeyChord Alternate { get; set; }

    public bool Matches(Key key) => Primary.Matches(key) || Alternate.Matches(key);

    public string Display =>
        Alternate.IsBound ? $"{Primary}  /  {Alternate}" : Primary.ToString();
}

// The binding table: defaults, persistence, and the lookup every key handler goes through.
public static class Keybinds
{
    private static readonly List<ActionBinding> All = BuildDefaults();

    // Resolution index, rebuilt whenever a binding changes. Key handlers run on every keystroke,
    // so this must not be a linear scan over the whole table.
    private static Dictionary<InputContext, List<ActionBinding>> _byContext = Index();

    public static IReadOnlyList<ActionBinding> Bindings => All;

    public static ActionBinding Get(GameAction action) =>
        All.First(b => b.Action == action);

    public static IEnumerable<ActionBinding> InContext(InputContext context) =>
        All.Where(b => b.Context == context);

    // The hot path: which action, if any, does this keystroke trigger here?
    public static GameAction Resolve(InputContext context, Key key)
    {
        if (!_byContext.TryGetValue(context, out List<ActionBinding>? list)) return GameAction.None;

        foreach (ActionBinding b in list)
            if (b.Matches(key)) return b.Action;

        return GameAction.None;
    }

    public static bool IsPressed(GameAction action, Key key) => Get(action).Matches(key);

    // Directions only, ignoring modifiers, and only after an exact Resolve has come back empty.
    //
    // Movement is the one place where a modifier means "the same action, differently": Shift is
    // sprint and Ctrl is stealth. Everything else is exact-match, which is what makes Shift+M
    // bindable to the milestones dialog without also making Shift+M walk north-east.
    public static GameAction ResolveDirectionLoose(InputContext context, Key key)
    {
        if (!_byContext.TryGetValue(context, out List<ActionBinding>? list)) return GameAction.None;

        foreach (ActionBinding b in list)
        {
            if (!IsDirection(b.Action)) continue;
            if (b.Primary.MatchesBase(key) || b.Alternate.MatchesBase(key)) return b.Action;
        }

        return GameAction.None;
    }

    public static bool IsDirection(GameAction a) => a
        is GameAction.MoveNorth or GameAction.MoveSouth or GameAction.MoveWest or GameAction.MoveEast
        or GameAction.MoveNorthWest or GameAction.MoveNorthEast
        or GameAction.MoveSouthWest or GameAction.MoveSouthEast
        or GameAction.ReticleNorth or GameAction.ReticleSouth
        or GameAction.ReticleWest or GameAction.ReticleEast
        or GameAction.ReticleNorthWest or GameAction.ReticleNorthEast
        or GameAction.ReticleSouthWest or GameAction.ReticleSouthEast;

    // Grid delta for a direction action; (0,0) for anything else.
    public static (int dx, int dy) Delta(GameAction a) => a switch
    {
        GameAction.MoveNorth or GameAction.ReticleNorth         => (0, -1),
        GameAction.MoveSouth or GameAction.ReticleSouth         => (0, 1),
        GameAction.MoveWest or GameAction.ReticleWest           => (-1, 0),
        GameAction.MoveEast or GameAction.ReticleEast           => (1, 0),
        GameAction.MoveNorthWest or GameAction.ReticleNorthWest => (-1, -1),
        GameAction.MoveNorthEast or GameAction.ReticleNorthEast => (1, -1),
        GameAction.MoveSouthWest or GameAction.ReticleSouthWest => (-1, 1),
        GameAction.MoveSouthEast or GameAction.ReticleSouthEast => (1, 1),
        _ => (0, 0),
    };

    // Every OTHER action in the same context already using this chord. The rebind screen shows
    // these rather than refusing the change: a player who wants to move a binding needs to see
    // what they are about to displace.
    public static IEnumerable<ActionBinding> Conflicts(ActionBinding target, KeyChord chord)
    {
        if (!chord.IsBound) yield break;

        foreach (ActionBinding b in InContext(target.Context))
        {
            if (ReferenceEquals(b, target)) continue;
            if (b.Primary == chord || b.Alternate == chord) yield return b;
        }
    }

    public static void Rebind(ActionBinding binding, KeyChord chord, bool alternate)
    {
        if (alternate) binding.Alternate = chord;
        else binding.Primary = chord;
        _byContext = Index();
    }

    public static void ResetToDefaults()
    {
        foreach (ActionBinding b in All)
        {
            b.Primary = b.DefaultPrimary;
            b.Alternate = b.DefaultAlternate;
        }
        _byContext = Index();
        Save();
    }

    public static bool IsDefault(ActionBinding b) =>
        b.Primary == b.DefaultPrimary && b.Alternate == b.DefaultAlternate;

    // Contexts in the order a player meets them, with their captions. Shared so the rebind screen
    // and the Player Guide's controls page cannot disagree about what a group is called.
    // Log is absent because nothing binds to it — log scrolling lives in the Map context.
    public static readonly (InputContext Context, string Caption)[] Groups =
    {
        (InputContext.Map,         "On the map"),
        (InputContext.LookMode,    "Look mode"),
        (InputContext.RangedFire,  "Ranged fire"),
        (InputContext.Inventory,   "Inventory"),
        (InputContext.PlayerGuide, "Player guide"),
        (InputContext.Milestones,  "Milestones"),
        (InputContext.Bestiary,    "Bestiary"),
    };

    // ── Reserved keys ────────────────────────────────────────────────
    // Runes a dialog matches DIRECTLY rather than through a binding, declared so Conflicts()
    // can see them. They are deliberately not rebindable, for two reasons that no rebind table
    // can express: a tag chip or a tab digit is POSITIONAL (it means "the Nth of these"), and a
    // sort suffix is SEQUENTIAL (it means nothing except immediately after the sort key). Listing
    // either as a standalone action would put rows in the rebind screen that do nothing on their
    // own — the advertises-what-does-not-work defect this codebase keeps deleting.
    //
    // Adding a rune to a dialog's own switch without adding it here is drift, and
    // Tools/DialogKeyProbe sweeps every letter and digit against this table to catch it.
    // Sequential = the rune only means anything immediately after another key, so pressing it
    // on its own does nothing. That is what makes it unbindable, and it is also why a probe
    // sweeping bare keypresses cannot observe it.
    private static readonly Dictionary<InputContext, (char Rune, string Use, bool Sequential)[]>
        ReservedKeys = new()
    {
        [InputContext.Bestiary] = new[]
        {
            ('1', "tag chip 1", false), ('2', "tag chip 2", false), ('3', "tag chip 3", false),
            ('4', "tag chip 4", false), ('5', "tag chip 5", false), ('6', "tag chip 6", false),
            ('7', "tag chip 7", false), ('8', "tag chip 8", false), ('9', "tag chip 9", false),
            ('0', "tag chip 10", false), ('a', "tag chip 11, and sort: name", false),
            ('w', "tag chip 12", false),
            ('l', "sort suffix: level", true), ('k', "sort suffix: kills", true),
            ('r', "sort suffix: recent", true),
        },
        [InputContext.Milestones] = new[]
        {
            ('1', "jump to tab 1", false), ('2', "jump to tab 2", false),
            ('3', "jump to tab 3", false), ('4', "jump to tab 4", false),
            ('5', "jump to tab 5", false), ('6', "jump to tab 6", false),
            ('7', "jump to tab 7", false), ('8', "jump to tab 8", false),
            ('9', "jump to tab 9", false), ('0', "jump to tab 10", false),
        },
    };

    public static IEnumerable<(char Rune, string Use, bool Sequential)> ReservedIn(
        InputContext context) =>
        ReservedKeys.TryGetValue(context, out var list)
            ? list : Enumerable.Empty<(char, string, bool)>();

    // What already claims this chord in this context outside the binding table, if anything.
    // Conflicts() alone cannot answer that, which is why adding a Bestiary key needed a hand
    // check against a rune switch.
    public static string? ReservedUse(InputContext context, KeyChord chord)
    {
        if (chord.Rune == '\0' || chord.Shift || chord.Ctrl || chord.Alt) return null;
        foreach (var (rune, use, _) in ReservedIn(context))
            if (rune == chord.Rune) return use;
        return null;
    }

    private static Dictionary<InputContext, List<ActionBinding>> Index() =>
        All.GroupBy(b => b.Context).ToDictionary(g => g.Key, g => g.ToList());

    // ── Persistence ──────────────────────────────────────────────────
    // Its own file rather than a field on UserSettings: the table is a different shape from the
    // flat settings record, and a corrupt keybind file must not take the display settings with it.

    private static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AincradTRPG");

    private static string Path_ => System.IO.Path.Combine(Dir, "keybinds.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static void Load()
    {
        try
        {
            if (!File.Exists(Path_)) return;

            var stored = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
                File.ReadAllText(Path_), JsonOpts);
            if (stored is null) return;

            foreach (ActionBinding b in All)
            {
                if (!stored.TryGetValue(b.Action.ToString(), out string[]? chords) || chords.Length == 0)
                    continue;

                b.Primary = KeyChord.Parse(chords[0]);
                b.Alternate = chords.Length > 1 ? KeyChord.Parse(chords[1]) : KeyChord.None;
            }
            _byContext = Index();
        }
        catch (Exception ex)
        {
            // Disclosed fallback: the defaults are already in place, but say so rather than
            // letting a player wonder why their bindings reverted.
            UI.DebugLogger.LogError("Keybinds.Load", ex);
            ResetToDefaults();
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var payload = All.ToDictionary(
                b => b.Action.ToString(),
                b => new[] { b.Primary.ToString(), b.Alternate.ToString() });
            string tmp = Path_ + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(payload, JsonOpts));
            File.Move(tmp, Path_, overwrite: true);
        }
        catch (Exception ex)
        {
            UI.DebugLogger.LogError("Keybinds.Save", ex);
        }
    }

    // ── Defaults ─────────────────────────────────────────────────────

    private static ActionBinding B(GameAction a, InputContext c, string label,
                                   KeyChord primary, KeyChord alternate = default) =>
        new()
        {
            Action = a, Context = c, Label = label,
            DefaultPrimary = primary, DefaultAlternate = alternate,
            Primary = primary, Alternate = alternate,
        };

    private static List<ActionBinding> BuildDefaults()
    {
        KeyChord C(char c) => KeyChord.Of(c);
        KeyChord K(KeyCode k) => KeyChord.Of(k);
        KeyChord S(char c) => KeyChord.Shifted(c);
        KeyChord S2(KeyCode k) => KeyChord.Shifted(k);

        return
        [
            // ── Map: movement ────────────────────────────────────────
            B(GameAction.MoveNorth,     InputContext.Map, "Move north",      C('w'), K(KeyCode.CursorUp)),
            B(GameAction.MoveSouth,     InputContext.Map, "Move south",      C('s'), K(KeyCode.CursorDown)),
            B(GameAction.MoveWest,      InputContext.Map, "Move west",       C('a'), K(KeyCode.CursorLeft)),
            B(GameAction.MoveEast,      InputContext.Map, "Move east",       C('d'), K(KeyCode.CursorRight)),
            B(GameAction.MoveNorthWest, InputContext.Map, "Move north-west", C('q')),
            B(GameAction.MoveNorthEast, InputContext.Map, "Move north-east", C('e')),
            B(GameAction.MoveSouthWest, InputContext.Map, "Move south-west", C('z')),
            B(GameAction.MoveSouthEast, InputContext.Map, "Move south-east", C('c')),
            B(GameAction.Wait,          InputContext.Map, "Wait a turn",     K(KeyCode.Space)),

            // ── Map: actions ─────────────────────────────────────────
            B(GameAction.Look,             InputContext.Map, "Look around",       C('l')),
            B(GameAction.OpenInventory,    InputContext.Map, "Inventory",         C('i')),
            B(GameAction.Pickup,           InputContext.Map, "Pick up",           C('g')),
            B(GameAction.AutoExplore,      InputContext.Map, "Auto-explore",      C('x')),
            B(GameAction.OpenBestiary,     InputContext.Map, "Bestiary",          C('y')),
            B(GameAction.OpenStats,        InputContext.Map, "Character sheet",   C('p')),
            B(GameAction.OpenHelp,         InputContext.Map, "Help / key list",   C('h')),
            B(GameAction.OpenPlayerGuide,  InputContext.Map, "Player guide",      C('b')),
            B(GameAction.Rest,             InputContext.Map, "Rest",              C('r')),
            B(GameAction.OpenQuestLog,     InputContext.Map, "Quest log",         C('j')),
            B(GameAction.OpenKillStats,    InputContext.Map, "Kill statistics",   C('k')),
            B(GameAction.OpenEquipment,    InputContext.Map, "Equipment",         C('t')),
            B(GameAction.Counter,          InputContext.Map, "Counter stance",    C('v')),
            B(GameAction.OpenSwordSkills,  InputContext.Map, "Sword skills",      C('f')),
            B(GameAction.RangedFire,       InputContext.Map, "Ranged fire",       C('\\')),
            B(GameAction.QuickSave,        InputContext.Map, "Save",              K(KeyCode.F5)),
            B(GameAction.OpenMilestones,   InputContext.Map, "Milestones",        S('m')),
            B(GameAction.OpenCollectables, InputContext.Map, "Collectables",      S('l')),
            B(GameAction.ToggleStatusTray, InputContext.Map, "Verbose status",    S('s')),
            B(GameAction.ToggleHeightmap,  InputContext.Map, "Heightmap overlay", S('g')),

            B(GameAction.SwordSkill1, InputContext.Map, "Sword skill slot 1", K(KeyCode.F1)),
            B(GameAction.SwordSkill2, InputContext.Map, "Sword skill slot 2", K(KeyCode.F2)),
            B(GameAction.SwordSkill3, InputContext.Map, "Sword skill slot 3", K(KeyCode.F3)),
            B(GameAction.SwordSkill4, InputContext.Map, "Sword skill slot 4", K(KeyCode.F4)),

            B(GameAction.QuickUse1,  InputContext.Map, "Quick-use slot 1",  C('1')),
            B(GameAction.QuickUse2,  InputContext.Map, "Quick-use slot 2",  C('2')),
            B(GameAction.QuickUse3,  InputContext.Map, "Quick-use slot 3",  C('3')),
            B(GameAction.QuickUse4,  InputContext.Map, "Quick-use slot 4",  C('4')),
            B(GameAction.QuickUse5,  InputContext.Map, "Quick-use slot 5",  C('5')),
            B(GameAction.QuickUse6,  InputContext.Map, "Quick-use slot 6",  C('6')),
            B(GameAction.QuickUse7,  InputContext.Map, "Quick-use slot 7",  C('7')),
            B(GameAction.QuickUse8,  InputContext.Map, "Quick-use slot 8",  C('8')),
            B(GameAction.QuickUse9,  InputContext.Map, "Quick-use slot 9",  C('9')),
            B(GameAction.QuickUse10, InputContext.Map, "Quick-use slot 10", C('0')),

            B(GameAction.LogScrollUp,   InputContext.Map, "Scroll log up",   K(KeyCode.PageUp)),
            B(GameAction.LogScrollDown, InputContext.Map, "Scroll log down", K(KeyCode.PageDown)),

            // ── Look mode ────────────────────────────────────────────
            B(GameAction.LookNextTarget, InputContext.LookMode, "Next target",     C('d'), K(KeyCode.CursorRight)),
            B(GameAction.LookPrevTarget, InputContext.LookMode, "Previous target", C('a'), K(KeyCode.CursorLeft)),
            B(GameAction.LookInspect,    InputContext.LookMode, "Inspect target",  C('t')),

            // ── Ranged fire ──────────────────────────────────────────
            B(GameAction.FireConfirm,        InputContext.RangedFire, "Fire",            K(KeyCode.Enter), K(KeyCode.Space)),
            B(GameAction.FireNextTarget,     InputContext.RangedFire, "Next target",     K(KeyCode.Tab)),
            B(GameAction.ReticleNorth,       InputContext.RangedFire, "Reticle north",   C('w'), K(KeyCode.CursorUp)),
            B(GameAction.ReticleSouth,       InputContext.RangedFire, "Reticle south",   C('s'), K(KeyCode.CursorDown)),
            B(GameAction.ReticleWest,        InputContext.RangedFire, "Reticle west",    C('a'), K(KeyCode.CursorLeft)),
            B(GameAction.ReticleEast,        InputContext.RangedFire, "Reticle east",    C('d'), K(KeyCode.CursorRight)),
            B(GameAction.ReticleNorthWest,   InputContext.RangedFire, "Reticle NW",      C('q')),
            B(GameAction.ReticleNorthEast,   InputContext.RangedFire, "Reticle NE",      C('e')),
            B(GameAction.ReticleSouthWest,   InputContext.RangedFire, "Reticle SW",      C('z')),
            B(GameAction.ReticleSouthEast,   InputContext.RangedFire, "Reticle SE",      C('c')),

            // ── Inventory ────────────────────────────────────────────
            B(GameAction.InvFilterWeapons,     InputContext.Inventory, "Filter: weapons",     C('1')),
            B(GameAction.InvFilterArmor,       InputContext.Inventory, "Filter: armor",       C('2')),
            B(GameAction.InvFilterMaterials,   InputContext.Inventory, "Filter: materials",   C('3')),
            B(GameAction.InvFilterConsumables, InputContext.Inventory, "Filter: consumables", C('4')),
            B(GameAction.InvFilterAll,         InputContext.Inventory, "Filter: all",         C('5')),
            B(GameAction.InvCompare,           InputContext.Inventory, "Compare with equipped", C('l')),

            // ── Player Guide ─────────────────────────────────────────
            B(GameAction.GuideSearch, InputContext.PlayerGuide, "Search", C('/')),
            B(GameAction.GuideBack,   InputContext.PlayerGuide, "Back",   K(KeyCode.Backspace)),
            // Its own key rather than Enter: entering the body focuses the first See-also link, and
            // Enter follows a focused link before it reaches a collapsed block — so on any topic
            // with cross-references (all of them) Enter could only expand after arrowing past every
            // link in the list.
            B(GameAction.GuideExpand, InputContext.PlayerGuide, "Expand section", C('x')),

            // ── Milestones ───────────────────────────────────────────
            B(GameAction.MilestoneEquipTitle,   InputContext.Milestones, "Equip title",   C('e')),
            B(GameAction.MilestoneUnequipTitle, InputContext.Milestones, "Unequip title", C('u')),
            B(GameAction.MilestoneCycleBucket,   InputContext.Milestones, "Cycle collectable bucket", C('b')),
            B(GameAction.MilestoneFocusKillLog,  InputContext.Milestones, "Focus the kill log",       C('k')),
            // Bound rather than hardcoded, and this does NOT breach the rule that keeps Tab
            // fixed: that rule protects Tab in its LIST-NAVIGATION role, and this dialog claimed
            // it for category cycling, which is exactly what removed focus movement from the
            // dialog. Making the claim rebindable lets a player hand Tab back to the framework.
            // It is also the only way the claim can be seen at all — ReservedKeys is keyed by
            // rune, and Tab has none, so it could never have been declared there.
            // Two actions, not one with Shift reversing it: exact-match is the default and
            // movement's loose match is the documented single exception, and separate actions
            // keep both directions independently visible to Conflicts.
            B(GameAction.MilestoneNextTab, InputContext.Milestones, "Next category",     K(KeyCode.Tab)),
            B(GameAction.MilestonePrevTab, InputContext.Milestones, "Previous category", S2(KeyCode.Tab)),

            // ── Bestiary ─────────────────────────────────────────────
            B(GameAction.BestiarySearch,    InputContext.Bestiary, "Search",             C('/')),
            // Conflicts() cannot vet this: every other key on that dialog is matched by a bare
            // rune switch rather than bound here. Checked by hand against it — y b u c s and the
            // tag chips 1-9 0 a w are taken, 'f' appears only inside the sort-suffix branch,
            // which returns early and is reachable only after 's'.
            B(GameAction.BestiaryFloorBand, InputContext.Bestiary, "Cycle floor band", C('f')),
            B(GameAction.BestiaryBossOnly,          InputContext.Bestiary, "Boss-only filter",   C('b')),
            B(GameAction.BestiaryShowUndiscovered,  InputContext.Bestiary, "Show undiscovered",  C('u')),
            B(GameAction.BestiaryClearFilters,      InputContext.Bestiary, "Clear all filters",  C('c')),
            B(GameAction.BestiarySortPrefix,        InputContext.Bestiary, "Sort (then a letter)", C('s')),
        ];
    }
}
