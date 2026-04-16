using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Shared dialog utilities — factory, separators, buttons, confirmation,
// Escape key close, and standard footer. Used by all dialog screens so
// popups share consistent look, sizing, and behavior.
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

    // Adds the uniform close footer — a centered Close button plus a dim
    // "[Esc] Close" hint anchored bottom-right — and wires Escape handling.
    // Returns the Close button so callers can `SetFocus()` or customize it.
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
