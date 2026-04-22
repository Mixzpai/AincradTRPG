using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Shared UI helpers: W/S navigation for menu screens and legacy ButtonScheme reference.
public static class NavigationHelper
{
    // Color scheme for interactive buttons — delegates to centralized ColorSchemes.
    // Kept for backward compatibility with screens that reference it directly.
    public static ColorScheme ButtonScheme => ColorSchemes.Button;

    // W/S + arrow focus navigation on a container (TitleScreen, DifficultyScreen, other menus).
    // Respects RadioGroup — arrow keys change selection inside radio groups.
    public static void EnableGameNavigation(View container)
    {
        container.KeyDown += (s, e) =>
        {
            // Let RadioGroup handle arrow keys internally for changing selection
            var focused = container.MostFocused;
            bool inRadioGroup = focused is RadioGroup || focused?.SuperView is RadioGroup;

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
    }

    // Arrow/W/S focus wiring: Up/W → up, Down/S → down. Shared by menu screens.
    public static void WireUpDown(View control, View up, View down)
    {
        control.KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.CursorUp:   case KeyCode.W: up.SetFocus();   e.Handled = true; break;
                case KeyCode.CursorDown: case KeyCode.S: down.SetFocus(); e.Handled = true; break;
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
