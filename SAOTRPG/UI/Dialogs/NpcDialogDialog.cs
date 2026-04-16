using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Modal conversation dialog for NPCs with branching dialogue.
// Shows a colored NPC name header, bordered dialogue text,
// optional player choice buttons, and a continue/goodbye button.
public static class NpcDialogDialog
{
    private const int DialogWidth  = 56;
    private const int DialogHeight = 16;

    // Show a modal conversation dialog with the given NPC.
    // Advances through dialogue lines sequentially; supports player choice branches.
    public static void Show(NPC npc)
    {
        // ── Early exit — no dialogue configured ───────────────────────
        var lines = npc.DialogueLines;
        if (lines == null || lines.Length == 0)
        {
            MessageBox.Query(npc.Name, "The NPC has nothing to say.", "OK");
            return;
        }

        // ── State: tracks current position in the dialogue sequence ──
        int lineIndex = 0;

        var dialog = DialogHelper.Create($" {npc.Symbol} {npc.Name} ", DialogWidth, DialogHeight);

        // ── NPC name header (colored by NPC's symbol color) ─────────
        var nameHeader = new Label
        {
            Text = $"  {npc.Symbol}  {npc.Name}",
            X = 1, Y = 0,
            Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.FromColor(npc.SymbolColor)
        };

        // ── Separator ───────────────────────────────────────────────
        var separator = new Label
        {
            Text = DialogHelper.Separator(DialogWidth),
            X = 1, Y = 1,
            Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.Dim
        };

        // ── NPC dialogue text area ──────────────────────────────────
        var npcText = new Label
        {
            Text = "",
            X = 2, Y = 2,
            Width = Dim.Fill(2),
            Height = 4,
            ColorScheme = ColorSchemes.Body
        };

        // ── Choice button area ──────────────────────────────────────
        var choiceArea = new View
        {
            X = 0, Y = 7,
            Width = Dim.Fill(),
            Height = 3
        };

        // ── Continue / Goodbye button ───────────────────────────────
        var continueBtn = DialogHelper.CreateButton("Continue", isDefault: true);
        continueBtn.X = Pos.Center();
        continueBtn.Y = Pos.AnchorEnd(2);

        // ── Dialogue state machine — advances through lines, handles choices ──
        void ShowLine(int idx)
        {
            if (idx >= lines.Length)
            {
                Application.RequestStop();
                return;
            }

            var line = lines[idx];
            npcText.Text = $"\"{line.Text}\"";
            choiceArea.RemoveAll();

            if (line.Choices != null && line.Choices.Length > 0)
            {
                // Show player choices as buttons
                continueBtn.Visible = false;
                int btnY = 0;
                foreach (var choice in line.Choices)
                {
                    var btn = DialogHelper.CreateButton(choice.Label);
                    btn.X = Pos.Center();
                    btn.Y = btnY++;
                    var capturedChoice = choice;
                    btn.Accepting += (s, e) =>
                    {
                        e.Cancel = true;
                        // Show NPC response, then advance
                        npcText.Text = $"\"{capturedChoice.Response}\"";
                        choiceArea.RemoveAll();
                        continueBtn.Visible = true;
                        continueBtn.Text = lineIndex < lines.Length - 1 ? " Continue " : " Goodbye  ";
                        continueBtn.SetFocus();
                    };
                    choiceArea.Add(btn);
                }
                if (choiceArea.Subviews.Any())
                    choiceArea.Subviews.First().SetFocus();
            }
            else
            {
                // Simple continue/goodbye
                continueBtn.Visible = true;
                continueBtn.Text = idx < lines.Length - 1 ? " Continue " : " Goodbye  ";
            }
        }

        continueBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            lineIndex++;
            ShowLine(lineIndex);
        };

        var hintLabel = new Label
        {
            Text = "Enter: continue | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };

        dialog.Add(nameHeader, separator, npcText, choiceArea, continueBtn, hintLabel);
        DialogHelper.CloseOnEscape(dialog);
        ShowLine(0);
        DialogHelper.RunModal(dialog);
    }
}
