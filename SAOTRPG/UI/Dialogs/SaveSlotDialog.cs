using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

/// <summary>
/// Save slot selection dialog — used for both loading and new game slot picking.
/// Shows 3 slots with character name, level, floor, difficulty, and play time.
/// </summary>
public static class SaveSlotDialog
{
    private const int DialogWidth = 50;
    private const int DialogHeight = 18;

    /// <summary>
    /// Shows the load game dialog. Returns (slot, saveData) if a save was selected, null if cancelled.
    /// </summary>
    public static (int Slot, SaveData Data)? ShowLoad()
    {
        (int Slot, SaveData Data)? result = null;
        var summaries = SaveManager.GetSlotSummaries();

        var dialog = new Dialog
        {
            Title = "Load Game",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        // Header
        var header = new Label
        {
            Text = "  Select a save slot to load:",
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = 1,
            ColorScheme = ColorSchemes.Body
        };

        // Slot labels
        var slotLabels = new Label[SaveManager.MaxSlots];
        var slotDetails = new Label[SaveManager.MaxSlots];
        var slotButtons = new Button[SaveManager.MaxSlots];

        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            int slot = i + 1;
            bool hasData = summaries[i] != null;
            int rowY = 2 + (i * 4);

            slotLabels[i] = new Label
            {
                Text = FormatSlotLine(slot, summaries[i]),
                X = 2, Y = rowY,
                Width = Dim.Fill(10), Height = 1,
                ColorScheme = hasData ? ColorSchemes.Body : ColorSchemes.Dim
            };

            slotDetails[i] = new Label
            {
                Text = hasData ? FormatSlotDetail(summaries[i]!) : "",
                X = 2, Y = rowY + 1,
                Width = Dim.Fill(2), Height = 1,
                ColorScheme = ColorSchemes.Dim
            };

            slotButtons[i] = new Button
            {
                Text = hasData ? " Load " : "      ",
                X = Pos.AnchorEnd(10), Y = rowY,
                ColorScheme = hasData ? ColorSchemes.Button : ColorSchemes.Dim,
                Enabled = hasData
            };

            int capturedSlot = slot;
            slotButtons[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                var save = SaveManager.LoadGame(capturedSlot);
                if (save != null)
                {
                    result = (capturedSlot, save);
                    Application.RequestStop();
                }
                else
                {
                    MessageBox.Query("Error", "Save file is corrupted.", "OK");
                }
            };

            // Separator between slots
            if (i < SaveManager.MaxSlots - 1)
            {
                dialog.Add(new Label
                {
                    Text = new string('─', DialogWidth - 6),
                    X = 2, Y = rowY + 2,
                    Width = Dim.Fill(2), Height = 1,
                    ColorScheme = ColorSchemes.Dim
                });
            }
        }

        // Delete button
        var deleteBtn = new Button
        {
            Text = " Delete ",
            X = 1,
            Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Button
        };
        deleteBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            ShowDeletePicker(summaries, dialog, slotLabels, slotDetails, slotButtons);
        };

        var closeBtn = new Button
        {
            Text = " Cancel ",
            X = Pos.AnchorEnd(12),
            Y = Pos.AnchorEnd(1),
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        dialog.Add(header);
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            dialog.Add(slotLabels[i], slotDetails[i], slotButtons[i]);
        dialog.Add(deleteBtn, closeBtn);

        // Focus the first available load button
        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            if (slotButtons[i].Enabled)
            {
                slotButtons[i].SetFocus();
                break;
            }
        }

        Application.Run(dialog);
        dialog.Dispose();
        return result;
    }

    /// <summary>
    /// Shows the new game slot picker. Returns the chosen slot (1-3), or -1 if cancelled.
    /// </summary>
    public static int ShowNewGameSlotPicker()
    {
        int chosenSlot = -1;
        var summaries = SaveManager.GetSlotSummaries();

        var dialog = new Dialog
        {
            Title = "Choose Save Slot",
            Width = DialogWidth,
            Height = DialogHeight,
            ColorScheme = ColorSchemes.Dialog
        };

        var header = new Label
        {
            Text = "  Select a slot for your new character:",
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = 1,
            ColorScheme = ColorSchemes.Body
        };

        var slotButtons = new Button[SaveManager.MaxSlots];

        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            int slot = i + 1;
            bool hasData = summaries[i] != null;
            int rowY = 2 + (i * 4);

            dialog.Add(new Label
            {
                Text = FormatSlotLine(slot, summaries[i]),
                X = 2, Y = rowY,
                Width = Dim.Fill(15), Height = 1,
                ColorScheme = hasData ? ColorSchemes.Body : ColorSchemes.Dim
            });

            if (hasData)
            {
                dialog.Add(new Label
                {
                    Text = FormatSlotDetail(summaries[i]!),
                    X = 2, Y = rowY + 1,
                    Width = Dim.Fill(2), Height = 1,
                    ColorScheme = ColorSchemes.Dim
                });
            }

            slotButtons[i] = new Button
            {
                Text = hasData ? " Overwrite " : "  Select   ",
                X = Pos.AnchorEnd(15), Y = rowY,
                ColorScheme = ColorSchemes.Button
            };

            int capturedSlot = slot;
            var capturedSummary = summaries[i];
            slotButtons[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (capturedSummary != null)
                {
                    int confirm = MessageBox.Query("Overwrite Save",
                        $"This will overwrite:\n{capturedSummary.Name} (Lv.{capturedSummary.Level} Floor {capturedSummary.Floor})\n\nContinue?",
                        "Overwrite", "Cancel");
                    if (confirm != 0) return;
                }
                chosenSlot = capturedSlot;
                Application.RequestStop();
            };

            // Separator
            if (i < SaveManager.MaxSlots - 1)
            {
                dialog.Add(new Label
                {
                    Text = new string('─', DialogWidth - 6),
                    X = 2, Y = rowY + 2,
                    Width = Dim.Fill(2), Height = 1,
                    ColorScheme = ColorSchemes.Dim
                });
            }
        }

        var closeBtn = new Button
        {
            Text = " Cancel ",
            X = Pos.AnchorEnd(12),
            Y = Pos.AnchorEnd(1),
            IsDefault = true,
            ColorScheme = ColorSchemes.Button
        };
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        dialog.Add(header);
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            dialog.Add(slotButtons[i]);
        dialog.Add(closeBtn);

        slotButtons[0].SetFocus();
        Application.Run(dialog);
        dialog.Dispose();
        return chosenSlot;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static string FormatSlotLine(int slot, SaveSlotSummary? summary)
    {
        if (summary == null)
            return $"  Slot {slot}:  [Empty]";

        return $"  Slot {slot}:  {summary.Name}  (Lv.{summary.Level})";
    }

    private static string FormatSlotDetail(SaveSlotSummary summary)
    {
        string hcTag = summary.IsHardcore ? " [HC]" : "";
        string playTime = FormatPlayTime(summary.PlayTime);
        string timeAgo = FormatTimeAgo(summary.Timestamp);

        return $"          Floor {summary.Floor}  {summary.Difficulty}{hcTag}  {playTime}  ({timeAgo})";
    }

    private static string FormatPlayTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes:D2}m";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}m";
        return "<1m";
    }

    private static string FormatTimeAgo(DateTime timestamp)
    {
        var elapsed = DateTime.Now - timestamp;
        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 30) return $"{(int)elapsed.TotalDays}d ago";
        return timestamp.ToString("MMM d");
    }

    private static void ShowDeletePicker(SaveSlotSummary?[] summaries, Dialog parent,
        Label[] slotLabels, Label[] slotDetails, Button[] slotButtons)
    {
        // Find which slots have data
        bool anyData = false;
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            if (summaries[i] != null) anyData = true;

        if (!anyData)
        {
            MessageBox.Query("Delete", "No saves to delete.", "OK");
            return;
        }

        // Ask which slot to delete
        string[] options = new string[SaveManager.MaxSlots + 1];
        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            int slot = i + 1;
            options[i] = summaries[i] != null
                ? $"Slot {slot}: {summaries[i]!.Name} (Lv.{summaries[i]!.Level})"
                : $"Slot {slot}: [Empty]";
        }
        options[SaveManager.MaxSlots] = "Cancel";

        int choice = MessageBox.Query("Delete Save", "Which slot do you want to delete?", options);
        if (choice < 0 || choice >= SaveManager.MaxSlots) return;
        if (summaries[choice] == null) return;

        // Confirm deletion
        int confirm = MessageBox.Query("Confirm Delete",
            $"Delete {summaries[choice]!.Name}'s save?\nThis cannot be undone.",
            "Delete", "Cancel");
        if (confirm != 0) return;

        int deletedSlot = choice + 1;
        SaveManager.DeleteSave(deletedSlot);
        summaries[choice] = null;

        // Update the UI
        slotLabels[choice].Text = FormatSlotLine(deletedSlot, null);
        slotLabels[choice].ColorScheme = ColorSchemes.Dim;
        slotDetails[choice].Text = "";
        slotButtons[choice].Text = "      ";
        slotButtons[choice].Enabled = false;
        slotButtons[choice].ColorScheme = ColorSchemes.Dim;
    }
}
