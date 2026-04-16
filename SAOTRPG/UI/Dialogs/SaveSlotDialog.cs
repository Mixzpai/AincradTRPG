using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Save slot selection dialog — used for both loading and new game slot picking.
public static class SaveSlotDialog
{
    private const int DialogWidth = 50, DialogHeight = 18, SlotSpacing = 4;

    // Shows the load game dialog. Returns (slot, saveData) if selected, null if cancelled.
    public static (int Slot, SaveData Data)? ShowLoad()
    {
        (int Slot, SaveData Data)? result = null;
        var summaries = SaveManager.GetSlotSummaries();

        var dialog = DialogHelper.Create("Load Game", DialogWidth, DialogHeight);

        var header = new Label
        {
            Text = "  Select a save slot to load:",
            X = 0, Y = 0, Width = Dim.Fill(), Height = 1, ColorScheme = ColorSchemes.Body
        };

        var slotLabels = new Label[SaveManager.MaxSlots];
        var slotDetails = new Label[SaveManager.MaxSlots];
        var slotButtons = new Button[SaveManager.MaxSlots];

        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            int slot = i + 1;
            bool hasData = summaries[i] != null;
            int rowY = 2 + (i * SlotSpacing);

            BuildSlotCard(dialog, slot, summaries[i], rowY, out slotLabels[i], out slotDetails[i]);

            slotButtons[i] = new Button
            {
                Text = " Load ", X = Pos.AnchorEnd(10), Y = rowY,
                ColorScheme = ColorSchemes.Button, Visible = hasData
            };

            int capturedSlot = slot;
            slotButtons[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                var save = SaveManager.LoadGame(capturedSlot);
                if (save != null) { result = (capturedSlot, save); Application.RequestStop(); }
                else
                {
                    int fix = MessageBox.Query("Error", "Save file appears corrupted.\nDelete this save?", "Delete", "Cancel");
                    if (fix == 0)
                    {
                        SaveManager.DeleteSave(capturedSlot);
                        summaries[capturedSlot - 1] = null;
                        slotLabels[capturedSlot - 1].Text = FormatSlotLine(capturedSlot, null);
                        slotLabels[capturedSlot - 1].ColorScheme = ColorSchemes.Dim;
                        slotDetails[capturedSlot - 1].Text = "";
                        slotButtons[capturedSlot - 1].Visible = false;
                    }
                }
            };
        }

        var deleteBtn = new Button
        {
            Text = " Delete ", X = 1, Y = Pos.AnchorEnd(1), ColorScheme = ColorSchemes.Button
        };
        deleteBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            ShowDeletePicker(summaries, slotLabels, slotDetails, slotButtons);
        };

        var closeBtn = new Button
        {
            Text = " Cancel ", X = Pos.AnchorEnd(12), Y = Pos.AnchorEnd(1),
            IsDefault = true, ColorScheme = ColorSchemes.Button
        };
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        dialog.Add(header);
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            dialog.Add(slotLabels[i], slotDetails[i], slotButtons[i]);
        dialog.Add(deleteBtn, closeBtn);

        for (int i = 0; i < SaveManager.MaxSlots; i++)
            if (slotButtons[i].Visible) { slotButtons[i].SetFocus(); break; }

        DialogHelper.RunModal(dialog);
        return result;
    }

    // Shows the new game slot picker. Returns the chosen slot (1-3), or -1 if cancelled.
    public static int ShowNewGameSlotPicker()
    {
        int chosenSlot = -1;
        var summaries = SaveManager.GetSlotSummaries();

        var dialog = DialogHelper.Create("Choose Save Slot", DialogWidth, DialogHeight);

        var header = new Label
        {
            Text = "  Select a slot for your new character:",
            X = 0, Y = 0, Width = Dim.Fill(), Height = 1, ColorScheme = ColorSchemes.Body
        };

        var slotLabels = new Label[SaveManager.MaxSlots];
        var slotDetails = new Label[SaveManager.MaxSlots];
        var slotButtons = new Button[SaveManager.MaxSlots];

        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            int slot = i + 1;
            bool hasData = summaries[i] != null;
            int rowY = 2 + (i * SlotSpacing);

            BuildSlotCard(dialog, slot, summaries[i], rowY, out slotLabels[i], out slotDetails[i]);

            slotButtons[i] = new Button
            {
                Text = hasData ? " Overwrite " : "  Select   ",
                X = Pos.AnchorEnd(15), Y = rowY, ColorScheme = ColorSchemes.Button
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
        }

        var closeBtn = new Button
        {
            Text = " Cancel ", X = Pos.AnchorEnd(12), Y = Pos.AnchorEnd(1),
            ColorScheme = ColorSchemes.Button
        };
        closeBtn.Accepting += (s, e) => { e.Cancel = true; Application.RequestStop(); };

        dialog.Add(header);
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            dialog.Add(slotLabels[i], slotDetails[i], slotButtons[i]);
        dialog.Add(closeBtn);

        dialog.Loaded += (s, e) => slotButtons[0].SetFocus();
        DialogHelper.RunModal(dialog);
        return chosenSlot;
    }

    private static void BuildSlotCard(Dialog dialog, int slot, SaveSlotSummary? summary,
        int rowY, out Label slotLabel, out Label detailLabel)
    {
        bool hasData = summary != null;

        slotLabel = new Label
        {
            Text = FormatSlotLine(slot, summary), X = 2, Y = rowY,
            Width = Dim.Fill(15), Height = 1,
            ColorScheme = hasData ? ColorSchemes.Body : ColorSchemes.Dim
        };
        detailLabel = new Label
        {
            Text = hasData ? FormatSlotDetail(summary!) : "",
            X = 2, Y = rowY + 1, Width = Dim.Fill(2), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        if (hasData)
        {
            string hcTag = summary!.IsHardcore ? " [HC]" : "";
            dialog.Add(new Label
            {
                Text = $"{summary.Difficulty}{hcTag}", X = 13, Y = rowY + 1,
                Width = Dim.Auto(),
                ColorScheme = ColorSchemes.FromColor(GetDifficultyColor(summary.Difficulty))
            });
        }

        if (slot < SaveManager.MaxSlots)
        {
            dialog.Add(new Label
            {
                Text = new string('─', 60), X = 2, Y = rowY + 2,
                Width = Dim.Fill(2), Height = 1, ColorScheme = ColorSchemes.Dim
            });
        }
    }

    private static string FormatSlotLine(int slot, SaveSlotSummary? summary) =>
        summary == null ? $"  Slot {slot}:  [Empty]" : $"  Slot {slot}:  {summary.Name}  (Lv.{summary.Level})";

    private static string FormatSlotDetail(SaveSlotSummary summary)
    {
        string playTime = FormatPlayTime(summary.PlayTime);
        string timeAgo = FormatTimeAgo(summary.Timestamp);
        return $"           Floor {summary.Floor}  {playTime}  ({timeAgo})";
    }

    private static Color GetDifficultyColor(string diffName)
    {
        foreach (var tier in DifficultyData.GetTiers())
            if (tier.Name == diffName) return tier.ThemeColor;
        return Color.Gray;
    }

    private static string FormatPlayTime(TimeSpan time) => SummaryFormatter.FormatPlayTime(time);

    private static string FormatTimeAgo(DateTime timestamp)
    {
        var elapsed = DateTime.Now - timestamp;
        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 30) return $"{(int)elapsed.TotalDays}d ago";
        if (elapsed.TotalDays >= 90) return timestamp.ToString("MMM d, yyyy");
        return timestamp.ToString("MMM d");
    }

    private static void ShowDeletePicker(SaveSlotSummary?[] summaries,
        Label[] slotLabels, Label[] slotDetails, Button[] slotButtons)
    {
        if (!summaries.Any(s => s != null))
        {
            MessageBox.Query("Delete", "No saves to delete.", "OK");
            return;
        }

        string[] options = new string[SaveManager.MaxSlots + 1];
        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            var s = summaries[i];
            options[i] = s != null ? $"Slot {i + 1}: {s.Name} (Lv.{s.Level})" : $"Slot {i + 1}: [Empty]";
        }
        options[SaveManager.MaxSlots] = "Cancel";

        int choice = MessageBox.Query("Delete Save", "Which slot do you want to delete?", options);
        if (choice < 0 || choice >= SaveManager.MaxSlots) return;
        if (summaries[choice] == null) return;

        int confirm = MessageBox.Query("Confirm Delete",
            $"Delete {summaries[choice]!.Name}'s save?\nThis cannot be undone.", "Delete", "Cancel");
        if (confirm != 0) return;

        int deletedSlot = choice + 1;
        SaveManager.DeleteSave(deletedSlot);
        summaries[choice] = null;
        slotLabels[choice].Text = FormatSlotLine(deletedSlot, null);
        slotLabels[choice].ColorScheme = ColorSchemes.Dim;
        slotDetails[choice].Text = "";
        slotButtons[choice].Visible = false;
    }
}
