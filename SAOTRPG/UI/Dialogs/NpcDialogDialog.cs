using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Modal NPC conversation with branching dialogue — colored name header, bordered text,
// optional choice buttons, continue/goodbye button.
public static class NpcDialogDialog
{
    private const int DialogWidth  = 56;
    private const int DialogHeight = 16;

    // Show a modal conversation dialog with the given NPC.
    // Advances through dialogue lines sequentially; supports player choice branches.
    public static void Show(NPC npc) => Show(npc, null);

    // `player` is optional so an unconditioned conversation still works without one; when it is
    // absent every gated line and choice is simply shown, which is the safe direction — a missing
    // player must never silently hide content.
    public static void Show(NPC npc, Player? player)
    {
        // ── Early exit — no dialogue configured ───────────────────────
        var lines = npc.DialogueLines;
        if (player != null && lines != null)
            lines = lines.Where(l => l.Condition?.IsMet(player) ?? true).ToArray();
        if (lines == null || lines.Length == 0)
        {
            DialogHelper.Query(npc.Name, "The NPC has nothing to say.", "OK");
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
        }.WithScheme(ColorSchemes.FromColor(npc.SymbolColor));

        // ── Separator ───────────────────────────────────────────────
        var separator = new Label
        {
            Text = DialogHelper.Separator(DialogWidth),
            X = 1, Y = 1,
            Width = Dim.Fill(1), Height = 1,
            SchemeName = ColorSchemes.DimName
        };

        // ── Optional ASCII portrait (Klein/Asuna/Silica/Argo/etc.) ──
        string? portraitKey = AsciiPortraits.KeyForName(npc.Name);
        string[] portrait = portraitKey != null ? AsciiPortraits.Get(portraitKey) : Array.Empty<string>();
        bool hasPortrait = portrait.Length > 0;
        var portraitLabel = new Label
        {
            Text = hasPortrait ? string.Join("\n", portrait) : "",
            X = 2, Y = 2, Width = 10, Height = hasPortrait ? portrait.Length : 0,
            Visible = hasPortrait,
        }.WithScheme(ColorSchemes.FromColor(npc.SymbolColor));

        // ── NPC dialogue text area ──────────────────────────────────
        // Shift right when a portrait is shown so text doesn't overlap.
        int textX = hasPortrait ? 12 : 2;
        var npcText = new Label
        {
            Text = "",
            X = textX, Y = 2,
            Width = Dim.Fill(2),
            Height = 4,
            SchemeName = ColorSchemes.BodyName
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
                AppHost.App.RequestStop();
                return;
            }

            var line = lines[idx];
            npcText.Text = $"\"{line.Text}\"";
            choiceArea.RemoveAll();

            var choices = line.Choices;
            if (player != null && choices != null)
                choices = choices.Where(c => c.Condition?.IsMet(player) ?? true).ToArray();

            if (choices != null && choices.Length > 0)
            {
                // Show player choices as buttons
                continueBtn.Visible = false;
                int btnY = 0;
                foreach (var choice in choices)
                {
                    var btn = DialogHelper.CreateButton(choice.Label);
                    btn.X = Pos.Center();
                    btn.Y = btnY++;
                    var capturedChoice = choice;
                    btn.Accepting += (s, e) =>
                    {
                        e.Handled = true;
                        // Show NPC response, then advance
                        npcText.Text = $"\"{capturedChoice.Response}\"";
                        choiceArea.RemoveAll();
                        continueBtn.Visible = true;
                        continueBtn.Text = lineIndex < lines.Length - 1 ? " Continue " : " Goodbye  ";
                        continueBtn.SetFocus();
                    };
                    choiceArea.Add(btn);
                }
                if (choiceArea.SubViews.Any())
                    choiceArea.SubViews.First().SetFocus();
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
            e.Handled = true;
            lineIndex++;
            ShowLine(lineIndex);
        };

        var hintLabel = new Label
        {
            Text = "Enter: continue | Esc: close",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), SchemeName = ColorSchemes.DimName,
        };

        dialog.Add(nameHeader, separator, portraitLabel, npcText, choiceArea, continueBtn, hintLabel);
        DialogHelper.CloseOnEscape(dialog);
        ShowLine(0);
        DialogHelper.RunModal(dialog);
    }
}
