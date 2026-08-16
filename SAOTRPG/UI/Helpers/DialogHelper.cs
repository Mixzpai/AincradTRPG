using Terminal.Gui;

namespace SAOTRPG.UI.Helpers;

// Shared dialog utilities: factory, separators, buttons, confirm, Esc-close, close footer.
public static class DialogHelper
{
    // Creates a horizontal separator string for dialogs.
    // The padding accounts for dialog border + label margins (default 8 chars).
    // Uses the shared hairline glyph so dialog dividers match the screens' rules.
    public static string Separator(int dialogWidth, int padding = 8)
        => "  " + new string(ScreenHeader.Hairline, Math.Max(0, dialogWidth - padding));

    // Creates a standard dialog button with consistent styling.
    public static Button CreateButton(string text, bool isDefault = false) => new()
    {
        Text = $" {text} ",
        SchemeName = ColorSchemes.ButtonName,
        IsDefault = isDefault,
        ShadowStyle = NoShadow,
    };

    // Terminal.Gui gives every Button an Opaque drop shadow (Button.DefaultShadow), which grows
    // its Frame by one column and one row. On this game's black background the shadow is drawn
    // as box glyphs in black-on-black, so it contributes nothing visible — but the cells are
    // still written, so it erases whatever sits under them, and the extra column throws off any
    // arithmetic that assumes a button is as wide as its text.
    //
    // It must be null, not ShadowStyles.None: None still leaves the Margin one cell thick.
    private static readonly ShadowStyles? NoShadow = null;

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
            ShadowStyle = NoShadow,
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

    // ── Vertical menu rows ───────────────────────────────────────────

    // Focus marker for a vertical menu row.
    //
    // Was U+258C LEFT HALF BLOCK, on the theory that a left-edge bar reads as a modern selection
    // idiom. Measured on the real screen it was present and drawn at 10.79:1 against the row — and
    // the dev still could not see it, so contrast was never the problem: a half-block one column
    // from the label, in the SAME colour as the label, does not read as a marker.
    //
    // U+25B8 is in Geometric Shapes, the same block as the U+25BA already used by the horizontal
    // button rows and known to render on this terminal, so coverage is near-certain. If it still
    // reads light, U+25BA is the proven glyph and this is the one constant to change.
    public const char MenuRowMarker = '▸';

    // Default width of a menu row. Wide enough for the longest label the game uses plus the
    // bar and its padding; the row tints across this whole width when focused.
    public const int MenuRowWidth = 24;

    // A row in a VERTICAL menu list: fixed width, label left-aligned, an accent bar at the
    // left edge and a surface tint across the full row when focused.
    //
    // This is the idiom for a stacked menu. Horizontal button rows keep CreateMenuButton and
    // its ► … ◄ marker — a left edge bar on a button sitting in a row of buttons points at
    // nothing, and the marker is the affordance that survives a terminal which renders neither
    // the accent colour nor the surface lift.
    //
    // Why the label is padded: Terminal.Gui's Button paints the Focus attribute only behind its
    // TEXT, not across its frame. Measured — a 24-column button with a 10-column label tinted
    // 10 cells and left the other 14 on the Normal attribute. Padding the label to the row
    // width makes the whole row text, which gives one continuous run and needs no custom View.
    public static Button CreateMenuRow(string text, int width = MenuRowWidth, bool isDefault = false)
    {
        var btn = new Button
        {
            Text = RowText(text, width, isDefault),
            Width = width, Height = 1,
            TextAlignment = Alignment.Start,
            SchemeName = ColorSchemes.MenuButtonName,
            IsDefault = isDefault,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = NoShadow,
        };
        btn.HasFocusChanged += (s, e) =>
        {
            if (s is not Button b) return;
            b.IsDefault = e.NewValue;
            b.Text = RowText(RowLabel(b.Text?.ToString() ?? ""), width, e.NewValue);
        };
        return btn;
    }

    // The bar occupies the first cell so the label never shifts between states.
    // Re-renders a menu row whose label changes while it is on screen, keeping the focus
    // decoration the row currently has. Callers must not assign Text directly: the row's own
    // focus handler re-wraps whatever it finds there, so a raw label would lose its padding and
    // with it the continuous focus tint.
    public static void SetMenuRowLabel(Button row, string label, int width = MenuRowWidth) =>
        row.Text = RowText(label, width, row.HasFocus);

    private static string RowText(string label, int width, bool focused) =>
        ((focused ? MenuRowMarker + " " : "  ") + label).PadRight(width);

    // Recovers the label from a rendered row so it can be re-wrapped on a focus change.
    private static string RowLabel(string rowText) =>
        rowText.Trim().TrimStart(MenuRowMarker).Trim();

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

    // A ListView opens with SelectedItem null. Nothing is highlighted, ValueChanged has never
    // fired so any detail pane beside it is still blank, and the player's first arrow key is
    // spent selecting row 0 instead of moving — measured, the first Down is reported handled and
    // changes nothing. Seed row 0 once the source is populated, after ValueChanged is wired.
    // Re-assigning Source resets it to null, so refresh paths call this too.
    public static void SelectFirstRow(ListView list)
    {
        if (list.Source is { Count: > 0 } && list.SelectedItem is null) list.SelectedItem = 0;
    }

    // Key-driven vertical scrolling for a dialog whose content is taller than its clamped height.
    //
    // Terminal.Gui scrolls a container on neither keys nor focus: ScrollVertical works but must be
    // called explicitly, and a scrollbar alone is inert in a keyboard-only game. OptionsScreen
    // drives itself from HasFocusChanged on its controls — that has nothing to hang on here,
    // where the content is Labels and there is often not one focusable view among them.
    //
    // Nothing is repositioned and no View is subclassed: the offset is applied by moving each
    // non-anchored child's Y, so the footer AddCloseFooter anchors stays pinned while the body
    // moves under it. `Pos.AnchorEnd` children are left alone by construction.
    public static void EnableKeyScroll(Dialog dialog)
    {
        var rows = new List<(View V, int BaseY)>();
        int offset = 0, maxOffset = 0;
        bool measured = false;

        dialog.SubViewsLaidOut += (s, e) =>
        {
            if (measured) return;
            measured = true;

            int interior = dialog.Viewport.Height;
            int lowest = 0;
            foreach (View v in dialog.SubViews)
            {
                // Anything anchored to the bottom is chrome and must not move.
                if (v.Y is PosAnchorEnd) continue;
                rows.Add((v, v.Frame.Y));
                int bottom = v.Frame.Y + v.Frame.Height;
                if (bottom > lowest) lowest = bottom;
            }
            // Two rows of slack so the last line does not sit against the footer.
            maxOffset = Math.Max(0, lowest - interior + 2);
            if (maxOffset > 0) s_scrollable.Add(dialog);
            dialog.Disposing += (_, _) => s_scrollable.Remove(dialog);
        };

        dialog.KeyDown += (s, k) =>
        {
            int page = Math.Max(1, dialog.Viewport.Height - 3);
            int delta = k.KeyCode switch
            {
                KeyCode.PageDown    => page,
                KeyCode.PageUp      => -page,
                KeyCode.CursorDown  => 1,
                KeyCode.CursorUp    => -1,
                KeyCode.End         => maxOffset,
                KeyCode.Home        => -maxOffset,
                _ => 0,
            };
            if (delta == 0 || maxOffset == 0) return;

            int wanted = Math.Clamp(offset + delta, 0, maxOffset);
            if (wanted == offset) { k.Handled = true; return; }
            offset = wanted;
            foreach (var (v, baseY) in rows) v.Y = baseY - offset;
            dialog.SetNeedsLayout();
            k.Handled = true;
        };
    }

    // True when the dialog has content below its clamped height AND no way to reach it.
    // EnableKeyScroll marks itself here so DiscloseClippedContent stops calling scrollable rows
    // "hidden" — that message is only honest while they are genuinely unreachable.
    private static readonly HashSet<Dialog> s_scrollable = new();

    // Says so in the title bar when a dialog's content runs past the height it was clamped to.
    //
    // Create() clamps a requested height to the screen, and a dialog that does not scroll then
    // draws everything below that clamp nowhere, with no key that reaches it. Silently looking
    // fine while withholding content is the one failure mode this project rules out; the title
    // is the only region guaranteed not to collide with the content being reported on.
    public static void DiscloseClippedContent(Dialog dialog)
    {
        string baseTitle = dialog.Title;
        bool measured = false;
        dialog.SubViewsLaidOut += (s, e) =>
        {
            if (measured) return;
            measured = true;

            int interior = dialog.Viewport.Height;
            int hidden = 0, lowest = 0;
            foreach (View v in dialog.SubViews)
            {
                int bottom = v.Frame.Y + v.Frame.Height;
                if (bottom <= interior) continue;
                hidden++;
                if (bottom > lowest) lowest = bottom;
            }
            if (hidden == 0) return;

            // A scrollable dialog is not withholding anything — say how to reach it instead.
            if (s_scrollable.Contains(dialog))
            {
                dialog.Title = string.IsNullOrWhiteSpace(baseTitle)
                    ? "PgUp/PgDn to scroll"
                    : $"{baseTitle} — PgUp/PgDn to scroll";
                return;
            }

            string note = $"{hidden} rows hidden, needs {lowest + 2} terminal rows";
            dialog.Title = string.IsNullOrWhiteSpace(baseTitle) ? note : $"{baseTitle} — {note}";
        };
    }

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

    // Close footer: centered Close button + a key hint bottom-right + Esc wiring.
    public static Button AddCloseFooter(Dialog dialog)
    {
        var closeBtn = CreateButton("Close", isDefault: true);
        closeBtn.X = Pos.Center();
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Handled = true; AppHost.App.RequestStop(); };

        // Same key/action treatment the screens use, rather than the older "[Esc] Close".
        var hintPairs = new[] { ("esc", "close") };
        var escHint = ScreenHeader.KeyHints(
            Pos.AnchorEnd(ScreenHeader.KeyHintsWidth(hintPairs) + 2), Pos.AnchorEnd(1), hintPairs);

        dialog.Add(closeBtn);
        dialog.Add(escHint.ToArray());
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
        // The nested run loop below keeps the outer main loop iterating normally, so a caller
        // that opened this dialog mid-turn would otherwise bill the player's reading time to
        // whatever profiler scope it is standing in — with nothing in the loop sampler to
        // contradict it.
        SAOTRPG.Systems.Profiler.BeginExclusion();
        s_openModals.Add(dialog);
        try
        {
            // Under reduce-motion the dialog simply appears. The fade is the transition, not the
            // dialog — skipping it changes nothing about what is on screen a moment later.
            if (SAOTRPG.Systems.Motion.Animate)
                FadeInDialog(dialog, durationMs: 200,
                    SAOTRPG.Systems.EasingHelper.EasingType.EaseOut);
            AppHost.App.Run(dialog);
        }
        finally
        {
            // Before Dispose, so any pending timeout sees the dialog as closed.
            s_openModals.Remove(dialog);
            SAOTRPG.Systems.Profiler.EndExclusion();
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
