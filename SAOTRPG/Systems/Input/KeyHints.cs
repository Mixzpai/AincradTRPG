using System.Text.RegularExpressions;

namespace SAOTRPG.Systems.Input;

// Player-facing prose names a key by token, never by letter.
//
// Every key on the map is rebindable, so a written-out letter goes stale as soon as the player
// rebinds it. A token is resolved where the line is shown rather than where it is authored, so a
// rebind made mid-run is reflected the next time the line appears.
//
// Only real GameActions belong in a token. Esc, Enter and Tab are Terminal.Gui's contract and are
// not bindable (see the comment on GameAction), nor are a dialog's own positional keys; writing
// those out is accurate, and tokenising them would advertise a rebindability they do not have.
public static class KeyHints
{
    public const string MoveKeysToken = "{{MOVEKEYS}}";
    public const string DiagonalKeysToken = "{{DIAGKEYS}}";

    private static readonly Regex TokenRx = new(@"\{\{KEY:(\w+)\}\}", RegexOptions.Compiled);

    private static readonly GameAction[] Cardinals =
    {
        GameAction.MoveNorth, GameAction.MoveWest, GameAction.MoveSouth, GameAction.MoveEast,
    };

    private static readonly GameAction[] Diagonals =
    {
        GameAction.MoveNorthWest, GameAction.MoveNorthEast,
        GameAction.MoveSouthWest, GameAction.MoveSouthEast,
    };

    // The chord currently bound to an action, spelled exactly as the rebind screen spells it.
    // An unbound action says so rather than rendering an empty gap.
    public static string Key(GameAction action)
    {
        KeyChord primary = Keybinds.Get(action).Primary;
        return primary.IsBound ? primary.ToString() : "unbound";
    }

    // The movement keys as a player would say them. Both slots, because the game takes WASD and
    // the cursor keys and naming only one misdescribes half the bindings. The cursor set gets its
    // English name; any other alternate is listed, so a rebind stays readable.
    public static string MoveKeys()
    {
        string primary = Group(Cardinals, alternate: false);
        string alternate = Group(Cardinals, alternate: true);
        if (alternate.Length == 0) return primary;
        return IsCursorSet() ? primary + " or the arrow keys" : primary + " or " + alternate;
    }

    public static string DiagonalKeys() => Group(Diagonals, alternate: false);

    // Replaces every token in a line. An unresolvable {{KEY:…}} is left visible rather than
    // blanked: a wrong action name is an authoring bug and should look like one.
    public static string Resolve(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.Contains(MoveKeysToken, System.StringComparison.Ordinal))
            text = text.Replace(MoveKeysToken, MoveKeys());
        if (text.Contains(DiagonalKeysToken, System.StringComparison.Ordinal))
            text = text.Replace(DiagonalKeysToken, DiagonalKeys());
        if (!text.Contains("{{KEY:", System.StringComparison.Ordinal)) return text;

        return TokenRx.Replace(text, m =>
            System.Enum.TryParse(m.Groups[1].Value, out GameAction action) ? Key(action) : m.Value);
    }

    // Single-character chords read as a run, the way a player says "WASD"; anything longer needs
    // separators or the four cursor-key names run together into one unreadable word.
    private static string Group(GameAction[] actions, bool alternate)
    {
        var chords = new List<KeyChord>(actions.Length);
        foreach (GameAction a in actions)
        {
            ActionBinding b = Keybinds.Get(a);
            KeyChord c = alternate ? b.Alternate : b.Primary;
            if (!c.IsBound) return alternate ? "" : "unbound";
            chords.Add(c);
        }

        bool allSingle = chords.TrueForAll(c => c.ToString().Length == 1);
        var parts = chords.ConvertAll(c => c.ToString());
        return allSingle ? string.Concat(parts) : string.Join("/", parts);
    }

    private static bool IsCursorSet()
    {
        KeyCode[] wanted = { KeyCode.CursorUp, KeyCode.CursorLeft, KeyCode.CursorDown, KeyCode.CursorRight };
        for (int i = 0; i < Cardinals.Length; i++)
            if (Keybinds.Get(Cardinals[i]).Alternate != KeyChord.Of(wanted[i])) return false;
        return true;
    }
}
