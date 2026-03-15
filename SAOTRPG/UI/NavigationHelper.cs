using Terminal.Gui;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Shared UI helpers: W/S navigation for menu screens and legacy ButtonScheme reference.
/// </summary>
public static class NavigationHelper
{
    /// <summary>
    /// Color scheme for interactive buttons — delegates to centralized ColorSchemes.
    /// Kept for backward compatibility with screens that reference it directly.
    /// </summary>
    public static ColorScheme ButtonScheme => ColorSchemes.Button;

    /// <summary>
    /// Enables W/S and arrow key focus navigation on a container view.
    /// Used by TitleScreen, DifficultyScreen, and other menu screens.
    /// Respects RadioGroup — lets arrow keys control selection inside radio groups.
    /// </summary>
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
