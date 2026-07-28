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
        SchemeName = ColorSchemes.ButtonName,
        IsDefault = isDefault,
    };

    // Menu-selection button with ► Label ◄ focus marker; [ ] chrome stripped via NoDecorations/NoPadding.
    // Idle rows pad 2 spaces each side so horizontal alignment stays stable when focus moves.
    public static Button CreateMenuButton(string text, bool isDefault = false)
    {
        var btn = new Button
        {
            Text = isDefault ? $"► {text} ◄" : $"  {text}  ",
            SchemeName = ColorSchemes.MenuButtonName,
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
    //
    // Requested dimensions are clamped to the screen. Most callers pass fixed constants
    // sized for a comfortable terminal, which overflow on a small one; clamping here fixes
    // every call site at once. Dialogs that already size themselves against Screen pass
    // values inside the bound, so the clamp is a no-op for them.
    public static Dialog Create(string title, int width, int height)
    {
        var screen = AppHost.App.Screen;
        return new()
        {
            Title = title,
            Width = Math.Min(width, Math.Max(MinDialogWidth, screen.Width - 2)),
            Height = Math.Min(height, Math.Max(MinDialogHeight, screen.Height - 2)),
            SchemeName = ColorSchemes.DialogName,
        };
    }

    // Floors for the clamp — below these a dialog is unusable anyway, and letting it
    // collapse to zero would be worse than overflowing a very small terminal.
    private const int MinDialogWidth = 24;
    private const int MinDialogHeight = 8;

    // Standardized confirmation dialog. Returns true if the player confirmed.
    // Usage: if (!DialogHelper.ConfirmAction("Drop", "Iron Sword")) return;
    public static bool ConfirmAction(string action, string itemName)
        => Query(action, $"{action} {itemName}?", "Yes", "No") == 0;

    // MessageBox wrappers: TG requires an IApplication and returns a nullable index.
    // Centralizing keeps call sites terse and gives one adaptation point for API moves.
    // Returns -1 when the box is dismissed without a button.
    public static int Query(string title, string message, params string[] buttons)
        => MessageBox.Query(AppHost.App, title, message, buttons) ?? -1;

    public static int ErrorQuery(string title, string message, params string[] buttons)
        => MessageBox.ErrorQuery(AppHost.App, title, message, buttons) ?? -1;

    // Wires the Escape key to close a dialog via AppHost.App.RequestStop().
    // Call once after creating the dialog, before AppHost.App.Run().
    public static void CloseOnEscape(Dialog dialog)
    {
        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                AppHost.App.RequestStop();
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
        closeBtn.Accepting += (s, e) => { e.Handled = true; AppHost.App.RequestStop(); };

        var escHint = new Label
        {
            Text = "[Esc] Close",
            X = Pos.AnchorEnd(13), Y = Pos.AnchorEnd(1),
            SchemeName = ColorSchemes.DimName,
        };

        dialog.Add(closeBtn, escHint);
        CloseOnEscape(dialog);
        return closeBtn;
    }

    // Dialogs currently inside RunModal. Timeouts outlive the dialog that scheduled them —
    // RunModal disposes as soon as the dialog closes, and Terminal.Gui's View.WasDisposed is
    // compiled out of release builds — so any AddTimeout that touches dialog state must check
    // this and stop once its dialog has closed. Reference equality; UI-thread only.
    private static readonly HashSet<Dialog> s_openModals = new();

    // True while the dialog is still running under RunModal.
    public static bool IsOpen(Dialog dialog) => s_openModals.Contains(dialog);

    // Runs the dialog modally and disposes it. Convenience wrapper.
    // Pauses FrameClock so animations freeze; restores on close + forces a
    // full repaint so dialog cells don't linger as stale frame-cache content.
    public static void RunModal(Dialog dialog)
    {
        SAOTRPG.Systems.FrameClock.Pause();
        s_openModals.Add(dialog);
        try
        {
            FadeInDialog(dialog, durationMs: 200,
                SAOTRPG.Systems.EasingHelper.EasingType.EaseOut);
            AppHost.App.Run(dialog);
        }
        finally
        {
            // Before Dispose, so any pending timeout sees the dialog as closed.
            s_openModals.Remove(dialog);
            SAOTRPG.Systems.FrameClock.Resume();
            dialog.Dispose();
            // Force underlying view redraw so dialog cells aren't served stale by the frame cache.
            SAOTRPG.UI.MapView.MarkFrameDirty();
            (AppHost.App.TopRunnable as View)?.SetNeedsDraw();
        }
    }

    // Schedules a per-frame Scheme ramp from black→baseline.
    // Stopwatch-driven so it runs independently of the paused FrameClock.
    private static void FadeInDialog(Dialog dialog, int durationMs,
        SAOTRPG.Systems.EasingHelper.EasingType easing)
    {
        var originalScheme = dialog.GetScheme();
        if (originalScheme == null) return; // belt-and-suspenders for safety
        long startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long target = (long)(durationMs *
            (System.Diagnostics.Stopwatch.Frequency / 1000.0));
        if (target <= 0) return; // degenerate guard

        // Snap to black at frame 0 so the first paint isn't full-color.
        dialog.SetScheme(SAOTRPG.Systems.EasingHelper.ScaleScheme(originalScheme, 0f));

        AppHost.App.AddTimeout(TimeSpan.FromMilliseconds(16), () =>
        {
            if (!IsOpen(dialog)) return false;

            long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - startTicks;
            float t = Math.Clamp((float)elapsed / target, 0f, 1f);
            float alpha = SAOTRPG.Systems.EasingHelper.Ease(t, easing);
            dialog.SetScheme(SAOTRPG.Systems.EasingHelper.ScaleScheme(originalScheme, alpha));
            dialog.SetNeedsDraw();
            if (t >= 1f)
            {
                dialog.SetScheme(originalScheme); // snap to baseline
                return false;
            }
            return true;
        });
    }
}
