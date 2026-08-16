using Terminal.Gui;

namespace SAOTRPG.UI;

// Shared UI helpers: W/S navigation for menu screens, and Esc-handler teardown across transitions.
public static class NavigationHelper
{
    // Detaches every screen-level Esc handler from the main window.
    //
    // Four screens install one there, and a stale one still fires: Terminal.Gui raises KeyDown
    // as a plain multicast invoke and only consults Handled *after* the whole invocation list has
    // run, so `e.Handled = true` in the first subscriber does not stop the second. A leaked
    // handler therefore runs a second screen transition on top of the intended one.
    //
    // Consuming Esc is not optional for a menu screen: Terminal.Gui binds it to Command.Quit at
    // the application level (Application.cs), so a screen that ignores Esc exits the game. Only
    // TitleScreen leaves it unhandled, deliberately, and says so in its hint.
    //
    // Every transition used to name the other screens individually, which is quadratic and had
    // already been got wrong twice — DifficultyScreen never unhooked CharacterCreationScreen, and
    // ModifierSelectScreen never unhooked DifficultyScreen. Route all of them through here so a
    // new screen only has to be added in one place.
    //
    // Handlers attached to a screen's own subviews need no teardown: RemoveAll detaches the
    // subview from the tree and the handler dies with it. Only the long-lived window leaks.
    public static void UnhookScreenEscHandlers(Window mainWindow)
    {
        DifficultyScreen.UnhookEscHandler(mainWindow);
        CharacterCreationScreen.UnhookEscHandler(mainWindow);
        ModifierSelectScreen.UnhookEscHandler(mainWindow);
        OptionsScreen.UnhookEscHandler(mainWindow);
        // Menu focus navigation is window-level too; screens that want it re-enable it.
        DisableGameNavigation();
    }

    // The live game-navigation subscription. Callers pass the long-lived main window, and an
    // anonymous lambda on that is unremovable by construction — so the handler is stored and the
    // previous one detached before a new one goes on.
    //
    // Without this the handlers accumulated: TitleScreen.Show is re-entered from six places and
    // OptionsScreen.Show from two, each adding another subscription that RemoveAll cannot clear
    // (it removes subviews, never handlers on the window). After N visits a single W press ran
    // AdvanceFocus N times, so focus jumped N rows per keystroke and got worse the longer the
    // session ran. In-game movement was never affected: Terminal.Gui gives the focused view the
    // key first (View.Keyboard.cs, `Focused?.NewKeyDownEvent`), and MapView consumes it.
    //
    // One slot is enough because only one screen is live at a time.
    private static EventHandler<Key>? _navHandler;
    private static View? _navTarget;

    // Detaches the game-navigation handler, if any. Called by UnhookScreenEscHandlers so a screen
    // that does not want menu navigation simply never re-enables it.
    public static void DisableGameNavigation()
    {
        if (_navHandler != null && _navTarget != null) _navTarget.KeyDown -= _navHandler;
        _navHandler = null;
        _navTarget = null;
    }

    // W/S + arrow focus navigation on a container (TitleScreen, OptionsScreen, other menus).
    // Respects RadioGroup — arrow keys change selection inside radio groups.
    public static void EnableGameNavigation(View container)
    {
        DisableGameNavigation();
        _navTarget = container;
        _navHandler = (s, e) =>
        {
            // A selector owns its own arrow keys for changing selection — see
            // EnableSelectorArrowNav — so leave them alone while one holds focus.
            var focused = container.MostFocused;
            bool inRadioGroup = focused is OptionSelector || focused?.SuperView is OptionSelector;

            if (inRadioGroup && (e.KeyCode == KeyCode.CursorUp || e.KeyCode == KeyCode.CursorDown))
                return;

            switch (e.KeyCode)
            {
                case KeyCode.W:
                case KeyCode.CursorUp:
                    container.AdvanceFocus(NavigationDirection.Backward, TabBehavior.TabStop);
                    e.Handled = true;
                    break;
                case KeyCode.S:
                case KeyCode.CursorDown:
                    container.AdvanceFocus(NavigationDirection.Forward, TabBehavior.TabStop);
                    e.Handled = true;
                    break;
            }
        };
        container.KeyDown += _navHandler;
    }

    // Makes an OptionSelector's arrow keys move the selection, which is what every screen using
    // one assumes and what the hint footers advertise.
    //
    // Terminal.Gui 2.4.5 does not do this on its own. SelectorBase binds the arrows to
    // Command.Down/Up, but MoveNext/MovePrevious return early unless the rows carry
    // TabBehavior.NoStop, and the default is TabStop — so the keys are inert and bubble away
    // unhandled. Switching the rows to NoStop is not sufficient either: the arrows then move
    // focus only, while Value still changes on Space alone.
    //
    // That split is the dangerous part rather than the dead keys. Space activates the HIGHLIGHTED
    // row, and a selector opens with the highlight on row 0 whatever Value is — so on a list whose
    // default is not the first entry, Space as a first keystroke silently reselects row 0.
    //
    // Setting Value is enough to move the highlight with it: SelectorBase.OnValueChanged assigns
    // FocusedItem. Vertical lists take Up/Down, horizontal ones Left/Right, matching the axis the
    // rows are laid out on.
    public static void EnableSelectorArrowNav(OptionSelector selector)
    {
        selector.KeyDown += (s, e) =>
        {
            if (e.Handled) return;

            bool horizontal = selector.Orientation == Orientation.Horizontal;
            int step = e.KeyCode switch
            {
                KeyCode.CursorUp    when !horizontal => -1,
                KeyCode.CursorDown  when !horizontal => +1,
                KeyCode.CursorLeft  when  horizontal => -1,
                KeyCode.CursorRight when  horizontal => +1,
                _ => 0,
            };
            if (step == 0) return;

            int count = selector.SubViews.OfType<CheckBox>().Count();
            int next = (selector.Value ?? 0) + step;
            // Clamp rather than wrap. A screen that steps out of the list at either end wires its
            // own boundary handler; leaving the key unhandled here is what lets that one act.
            if (next < 0 || next >= count) return;

            selector.Value = next;
            e.Handled = true;
        };

        // Focus can re-enter the selector after the boundary handlers or Tab have taken it away,
        // and it lands on row 0 again each time, so the sync cannot be a one-off at build time.
        selector.HasFocusChanged += (s, e) => { if (e.NewValue) SyncSelectorHighlight(selector); };
        SyncSelectorHighlight(selector);
    }

    // Puts the highlight on the selected row.
    private static void SyncSelectorHighlight(OptionSelector selector)
    {
        int count = selector.SubViews.OfType<CheckBox>().Count();
        int value = selector.Value ?? 0;
        if (value >= 0 && value < count) selector.FocusedItem = value;
    }

    // Arrow/W/S focus wiring: Up/W → up, Down/S → down. Shared by menu screens.
    public static void WireUpDown(View control, View up, View down) =>
        WireVertical(control, up, down, wasd: true);

    // The same, minus W/S, for a control that has to keep the letter keys.
    //
    // A TextField treats W and S as text. Binding them as navigation does not merely swallow the
    // keystroke: it moves focus off the field, so every character after the first w or s misses
    // the field as well — typing "Kirwa" left "kir" behind and the caret on another control.
    public static void WireUpDownArrowsOnly(View control, View up, View down) =>
        WireVertical(control, up, down, wasd: false);

    private static void WireVertical(View control, View up, View down, bool wasd)
    {
        control.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.CursorUp || (wasd && e.KeyCode == KeyCode.W))
            {
                up.SetFocus(); e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.CursorDown || (wasd && e.KeyCode == KeyCode.S))
            {
                down.SetFocus(); e.Handled = true;
            }
        };
    }

    // Arrow focus wiring: Left → left, Right → right. Shared by menu screens.
    public static void WireLeftRight(View control, View left, View right)
    {
        control.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.CursorLeft:  left.SetFocus();  e.Handled = true; break;
                case KeyCode.CursorRight: right.SetFocus(); e.Handled = true; break;
            }
        };
    }
}
