using Terminal.Gui;

namespace SAOTRPG.UI;

/// <summary>
/// Shared UI helpers: navigation and color schemes.
/// </summary>
public static class NavigationHelper
{
    /// <summary>
    /// Color scheme for interactive buttons — gives a visible highlight on focus.
    /// </summary>
    public static readonly ColorScheme ButtonScheme = new()
    {
        Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        HotNormal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

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
