using Terminal.Gui;

namespace SAOTRPG.UI;

/// <summary>
/// Shared UI helpers: navigation, keyboard handling, and focus management.
/// Color schemes now live in Theme.cs — this file handles behavior only.
/// </summary>
public static class NavigationHelper
{
    /// <summary>
    /// Legacy accessor — points to Theme.MenuButton for any code still referencing this.
    /// Prefer Theme.MenuButton or Theme.SmallButton directly.
    /// </summary>
    public static ColorScheme ButtonScheme => Theme.MenuButton;

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
}
