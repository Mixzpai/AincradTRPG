using Terminal.Gui;
using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Rebinding screen: one row per action, grouped by the context the binding applies in.
//
// Each action carries two slots, because the game has always accepted WASD *and* the cursor keys
// for movement. Left/Right picks the slot, Enter arms it, and the next keystroke is captured.
public static class KeybindDialog
{
    private const int DialogWidth  = 72;
    private const int DialogHeight = 26;

    // Row layout: label, primary slot, alternate slot. Padded to one width so the focus tint
    // runs the whole row — see DialogHelper.CreateMenuRow.
    private const int LabelWidth = 26;
    private const int SlotWidth  = 16;
    private const int RowWidth   = 2 + LabelWidth + SlotWidth + SlotWidth;

    public static void Show()
    {
        var dialog = DialogHelper.Create("Key Bindings", DialogWidth, DialogHeight);

        // The row being edited, and which of its two slots. Null when nothing is armed.
        ActionBinding? capturing = null;
        bool captureAlternate = false;
        var rowFor = new Dictionary<Button, (ActionBinding Binding, bool[] Slot)>();

        var status = new Label
        {
            Text = "", X = 1, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(1), Height = 1, SchemeName = ColorSchemes.DimName,
        };

        // A scrolling container, driven from focus changes: Terminal.Gui scrolls on neither key
        // presses nor focus movement, so a scrollbar alone would be inert in a keyboard-only game.
        var form = new View
        {
            X = 1, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(4),
            CanFocus = true, TabStop = TabBehavior.NoStop,
        };
        form.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        void Reveal(View v)
        {
            int top = v.Frame.Y, bottom = v.Frame.Y + Math.Max(v.Frame.Height, 1);
            int viewTop = form.Viewport.Y, viewBottom = viewTop + form.Viewport.Height;
            if (top - 1 < viewTop) form.ScrollVertical(top - 1 - viewTop);
            else if (bottom + 1 > viewBottom) form.ScrollVertical(bottom + 1 - viewBottom);
        }

        string RowLabel(ActionBinding b, bool alternateSelected, bool armed)
        {
            string primary = b.Primary.ToString();
            string alternate = b.Alternate.ToString();
            if (armed) { if (alternateSelected) alternate = "press a key"; else primary = "press a key"; }

            // Brackets rather than a marker glyph: the row already carries the focus marker, and
            // two of the same arrow on one row would not say which one is armed.
            string p = alternateSelected ? $" {primary}" : $"[{primary}]";
            string a = alternateSelected ? $"[{alternate}]" : $" {alternate}";

            return b.Label.PadRight(LabelWidth) + p.PadRight(SlotWidth) + a.PadRight(SlotWidth);
        }

        void Redraw(Button row)
        {
            (ActionBinding b, bool[] slot) = rowFor[row];
            bool armed = ReferenceEquals(capturing, b);
            DialogHelper.SetMenuRowLabel(row, RowLabel(b, slot[0], armed), RowWidth);
        }

        int y = 0;
        var rows = new List<Button>();
        foreach ((InputContext context, string caption) in Keybinds.Groups)
        {
            var (sectionCaption, sectionRule) = ScreenHeader.Section(caption, 0, y, RowWidth);
            form.Add(sectionCaption, sectionRule);
            y++;

            foreach (ActionBinding b in Keybinds.InContext(context))
            {
                bool[] slot = [false];   // boxed so the row's handlers share one selection
                var row = DialogHelper.CreateMenuRow(RowLabel(b, false, false), RowWidth);
                row.X = 0;
                row.Y = y++;
                rowFor[row] = (b, slot);

                row.HasFocusChanged += (s, e) => { if (e.NewValue) Reveal(row); };
                row.KeyDown += (s, e) => OnRowKey(row, e);
                form.Add(row);
                rows.Add(row);
            }
            y++;   // a blank row between groups
        }
        form.SetContentSize(new System.Drawing.Size(RowWidth, y));

        // The handler belongs on the ROW, not on the dialog. A focused Button turns Enter and Space
        // into Command.Accept before the dialog's own KeyDown is raised, so an arm step on the
        // dialog can never run — measured: every other key reaches the dialog, those two do not.
        // Handling the key here also suppresses the row's Accepting and the dialog's Esc-to-close,
        // both of which a capture in progress has to own. While a slot is armed EVERY key belongs
        // to the capture, including the ones the row would otherwise act on.
        void OnRowKey(Button row, Key e)
        {
            (ActionBinding binding, bool[] slot) = rowFor[row];

            if (capturing is { } target)
            {
                e.Handled = true;

                // Esc cancels the capture rather than closing the dialog — closing out from under
                // an armed row would leave the player unsure whether the bind took.
                if (e.KeyCode == KeyCode.Esc)
                {
                    capturing = null;
                    status.Text = "Rebind cancelled.";
                    status.SchemeName = ColorSchemes.DimName;
                    Redraw(row);
                    return;
                }

                KeyChord chord = KeyChord.From(e);
                var clashes = Keybinds.Conflicts(target, chord).ToList();
                // Conflicts() only sees the binding table. Some dialogs match runes directly —
                // tag chips, tab digits, sort suffixes — and those are declared separately or a
                // rebind onto one would look free and then silently lose to the dialog.
                string? reserved = Keybinds.ReservedUse(target.Context, chord);
                Keybinds.Rebind(target, chord, captureAlternate);
                Keybinds.Save();
                capturing = null;

                status.Text = reserved != null
                    ? $"{chord} is already {reserved} on that screen — it will win first."
                    : clashes.Count == 0
                        ? $"{target.Label} bound to {chord}."
                        : $"{chord} also runs {string.Join(", ", clashes.Select(c => c.Label))} — that one wins first.";
                status.SchemeName = clashes.Count == 0 && reserved == null
                    ? ColorSchemes.SuccessName : ColorSchemes.DangerName;

                foreach (Button r in rows) Redraw(r);
                return;
            }

            switch (e.KeyCode)
            {
                case KeyCode.CursorLeft:
                    slot[0] = false; Redraw(row); e.Handled = true; return;

                case KeyCode.CursorRight:
                    slot[0] = true; Redraw(row); e.Handled = true; return;

                case KeyCode.Enter:
                    capturing = binding;
                    captureAlternate = slot[0];
                    status.Text = "Press the key to bind, or Esc to cancel.";
                    status.SchemeName = ColorSchemes.GoldName;
                    Redraw(row);
                    e.Handled = true;
                    return;

                case KeyCode.Delete:
                case KeyCode.Backspace:
                    Keybinds.Rebind(binding, KeyChord.None, slot[0]);
                    Keybinds.Save();
                    status.Text = $"{binding.Label} unbound.";
                    status.SchemeName = ColorSchemes.DimName;
                    Redraw(row);
                    e.Handled = true;
                    return;
            }
        }

        var resetBtn = DialogHelper.CreateButton("Reset All");
        resetBtn.X = Pos.Center() - 12;
        resetBtn.Y = Pos.AnchorEnd(2);
        resetBtn.Accepting += (s, e) =>
        {
            e.Handled = true;
            if (DialogHelper.Query("Reset Key Bindings",
                    "Restore every binding to its default?", "Cancel", "Reset") != 1) return;

            Keybinds.ResetToDefaults();
            foreach (Button r in rows) Redraw(r);
            status.Text = "All bindings restored to defaults.";
            status.SchemeName = ColorSchemes.SuccessName;
        };

        var closeBtn = DialogHelper.CreateButton("Close", isDefault: true);
        closeBtn.X = Pos.Right(resetBtn) + 2;
        closeBtn.Y = Pos.AnchorEnd(2);
        closeBtn.Accepting += (s, e) => { e.Handled = true; AppHost.App.RequestStop(); };

        var hintPairs = new[] { ("←→", "slot"), ("enter", "rebind"), ("del", "unbind") };
        var hint = ScreenHeader.KeyHints(
            ScreenHeader.Axis - ScreenHeader.KeyHintsWidth(hintPairs) / 2, Pos.AnchorEnd(1), hintPairs);

        dialog.Add(form, status, resetBtn, closeBtn);
        dialog.Add(hint.ToArray());
        DialogHelper.CloseOnEscape(dialog);
        if (rows.Count > 0) rows[0].SetFocus();
        DialogHelper.RunModal(dialog);
    }
}
