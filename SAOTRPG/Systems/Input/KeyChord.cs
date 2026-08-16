using Terminal.Gui;

namespace SAOTRPG.Systems.Input;

// One bindable keystroke: a base key plus its modifiers.
//
// Two ways to identify the base key, and both are needed. A key with a KeyCode member (cursor
// keys, F-keys, Space, Tab) is matched on that. A printable key is matched on its RUNE, because
// a printable key's KeyCode carries the character in its low bits but a driver may report either
// the produced character or the physical key with a modifier bit set — measured on Terminal.Gui
// 2.4.5, Key('+').WithShift is 0x1000002B, which no bare KeyCode compare matches, while AsRune
// still reads '+'. Some keys have no KeyCode member at all: '\' is one, and it is bound in game.
public readonly record struct KeyChord(KeyCode Code, char Rune, bool Shift, bool Ctrl, bool Alt)
{
    public static readonly KeyChord None = new(KeyCode.Null, '\0', false, false, false);

    public bool IsBound => Code != KeyCode.Null || Rune != '\0';

    // Builds a chord from a keystroke. Prefers the rune for printable keys.
    public static KeyChord From(Key key)
    {
        bool shift = key.IsShift, ctrl = key.IsCtrl, alt = key.IsAlt;
        KeyCode bare = key.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;

        // AsRune is empty under Alt/Ctrl, so a chord like Ctrl+G has to fall back to the code.
        char rune = (char)key.AsRune.Value;
        if (rune != '\0' && !char.IsControl(rune) && rune != ' ')
            return new KeyChord(KeyCode.Null, char.ToLowerInvariant(rune), shift, ctrl, alt);

        return new KeyChord(bare, '\0', shift, ctrl, alt);
    }

    public bool Matches(Key key)
    {
        if (!IsBound) return false;
        if (Shift != key.IsShift || Ctrl != key.IsCtrl || Alt != key.IsAlt) return false;

        if (Rune != '\0')
            return char.ToLowerInvariant((char)key.AsRune.Value) == Rune;

        KeyCode bare = key.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;
        return bare == Code;
    }

    // Matches the base key while ignoring modifiers. Movement takes Shift for sprint and Ctrl for
    // stealth, so those keystrokes still have to resolve to the direction they are bound to.
    public bool MatchesBase(Key key)
    {
        if (!IsBound) return false;

        if (Rune != '\0')
        {
            // AsRune is empty under Alt/Ctrl, so a modified letter has to be read off the code.
            char typed = (char)key.AsRune.Value;
            if (typed != '\0') return char.ToLowerInvariant(typed) == Rune;

            KeyCode letter = key.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask;
            return char.ToLowerInvariant((char)letter) == Rune;
        }

        return (key.KeyCode & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask & ~KeyCode.AltMask) == Code;
    }

    // Display and storage share one format, so what the rebind screen shows is exactly what
    // lands in the file — a binding a player cannot read back is a binding they cannot fix.
    public override string ToString()
    {
        if (!IsBound) return "—";

        string mods = (Ctrl ? "Ctrl+" : "") + (Alt ? "Alt+" : "") + (Shift ? "Shift+" : "");
        return mods + BaseName();
    }

    private string BaseName()
    {
        if (Rune != '\0') return char.ToUpperInvariant(Rune).ToString();

        return Code switch
        {
            KeyCode.Space     => "Space",
            KeyCode.Enter     => "Enter",
            KeyCode.Tab       => "Tab",
            KeyCode.Esc       => "Esc",
            KeyCode.Backspace => "Backspace",
            KeyCode.Delete    => "Delete",
            KeyCode.Insert    => "Insert",
            KeyCode.Home      => "Home",
            KeyCode.End       => "End",
            KeyCode.PageUp    => "PageUp",
            KeyCode.PageDown  => "PageDown",
            KeyCode.CursorUp    => "Up",
            KeyCode.CursorDown  => "Down",
            KeyCode.CursorLeft  => "Left",
            KeyCode.CursorRight => "Right",
            _ => Code.ToString(),
        };
    }

    public static KeyChord Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "—") return None;

        bool shift = false, ctrl = false, alt = false;
        string rest = text.Trim();
        while (true)
        {
            if (rest.StartsWith("Ctrl+", StringComparison.OrdinalIgnoreCase)) { ctrl = true; rest = rest[5..]; }
            else if (rest.StartsWith("Alt+", StringComparison.OrdinalIgnoreCase)) { alt = true; rest = rest[4..]; }
            else if (rest.StartsWith("Shift+", StringComparison.OrdinalIgnoreCase)) { shift = true; rest = rest[6..]; }
            else break;
        }
        if (rest.Length == 0) return None;

        // A single character is a RUNE binding, and this test has to come before the enum parse:
        // KeyCode has members named A-Z whose values are the UPPERCASE letters, so "W" would
        // otherwise parse to KeyCode.W (87) while a typed 'w' arrives as 119 — the binding would
        // survive a save and then silently stop matching after the next load.
        if (rest.Length == 1) return new KeyChord(KeyCode.Null, char.ToLowerInvariant(rest[0]), shift, ctrl, alt);

        KeyCode code = rest switch
        {
            "Space"     => KeyCode.Space,
            "Enter"     => KeyCode.Enter,
            "Tab"       => KeyCode.Tab,
            "Esc"       => KeyCode.Esc,
            "Backspace" => KeyCode.Backspace,
            "Delete"    => KeyCode.Delete,
            "Insert"    => KeyCode.Insert,
            "Home"      => KeyCode.Home,
            "End"       => KeyCode.End,
            "PageUp"    => KeyCode.PageUp,
            "PageDown"  => KeyCode.PageDown,
            "Up"        => KeyCode.CursorUp,
            "Down"      => KeyCode.CursorDown,
            "Left"      => KeyCode.CursorLeft,
            "Right"     => KeyCode.CursorRight,
            _ => Enum.TryParse(rest, ignoreCase: true, out KeyCode parsed) ? parsed : KeyCode.Null,
        };

        return code != KeyCode.Null ? new KeyChord(code, '\0', shift, ctrl, alt) : None;
    }

    // Convenience builders for the default table.
    public static KeyChord Of(char rune) => new(KeyCode.Null, char.ToLowerInvariant(rune), false, false, false);
    public static KeyChord Shifted(char rune) => new(KeyCode.Null, char.ToLowerInvariant(rune), true, false, false);
    public static KeyChord Of(KeyCode code) => new(code, '\0', false, false, false);
    public static KeyChord Shifted(KeyCode code) => new(code, '\0', true, false, false);
}
