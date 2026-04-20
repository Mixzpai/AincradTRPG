using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Shared dialog utilities: factory, separators, buttons, confirm, Esc-close, close footer.
public static class DialogHelper
{
    // Creates a horizontal separator string for dialogs: "  ───────..."
    // The padding accounts for dialog border + label margins (default 8 chars).
    public static string Separator(int dialogWidth, int padding = 8)
        => "  " + new string('─', Math.Max(0, dialogWidth - padding));

    // Creates a standard dialog button with consistent styling.
    public static Button CreateButton(string text, bool isDefault = false) => new()
    {
        Text = $" {text} ",
        ColorScheme = ColorSchemes.Button,
        IsDefault = isDefault,
    };

    // Menu-selection button with ► Label ◄ focus marker; [ ] chrome stripped via NoDecorations/NoPadding.
    // Idle rows pad 2 spaces each side so horizontal alignment stays stable when focus moves.
    public static Button CreateMenuButton(string text, bool isDefault = false)
    {
        var btn = new Button
        {
            Text = isDefault ? $"► {text} ◄" : $"  {text}  ",
            ColorScheme = ColorSchemes.MenuButton,
            IsDefault = isDefault,
            NoDecorations = true,
            NoPadding = true,
        };
        btn.HasFocusChanged += (s, e) =>
        {
            if (s is not Button b) return;
            b.IsDefault = e.NewValue;
            string core = StripMenuMarkers(b.Text?.ToString() ?? "");
            b.Text = e.NewValue ? $"► {core} ◄" : $"  {core}  ";
        };
        return btn;
    }

    // Strips ► … ◄ mirror (and legacy single-direction) so core label can be rewrapped.
    private static string StripMenuMarkers(string text)
    {
        var s = text.Trim();
        if (s.StartsWith("► ")) s = s[2..];
        if (s.EndsWith(" ◄")) s = s[..^2];
        if (s.EndsWith(" ►")) s = s[..^2];  // legacy guard
        return s.Trim();
    }

    // Standard dialog factory — applies the shared color scheme and dimensions
    // so every popup looks identical from frame inward.
    public static Dialog Create(string title, int width, int height) => new()
    {
        Title = title,
        Width = width,
        Height = height,
        ColorScheme = ColorSchemes.Dialog,
    };

    // Standardized confirmation dialog. Returns true if the player confirmed.
    // Usage: if (!DialogHelper.ConfirmAction("Drop", "Iron Sword")) return;
    public static bool ConfirmAction(string action, string itemName)
        => MessageBox.Query(action, $"{action} {itemName}?", "Yes", "No") == 0;

    // Wires the Escape key to close a dialog via Application.RequestStop().
    // Call once after creating the dialog, before Application.Run().
    public static void CloseOnEscape(Dialog dialog)
    {
        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };
    }

    // Close footer: centered Close button + dim "[Esc] Close" hint bottom-right + Esc wiring.
    public static Button AddCloseFooter(Dialog dialog)
    {
        var closeBtn = CreateButton("Close", isDefault: true);
        closeBtn.X = Pos.Center();
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        var escHint = new Label
        {
            Text = "[Esc] Close",
            X = Pos.AnchorEnd(13), Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(closeBtn, escHint);
        CloseOnEscape(dialog);
        return closeBtn;
    }

    // Runs the dialog modally and disposes it. Convenience wrapper.
    public static void RunModal(Dialog dialog)
    {
        Application.Run(dialog);
        dialog.Dispose();
    }
}
