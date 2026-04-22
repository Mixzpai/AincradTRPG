using Terminal.Gui;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Modal cutscene — typewriter text + optional portrait. Space/Enter finishes current line, next Enter advances.
// Choices appear after line completes. Esc skips unless Unskippable.
public static class CutsceneDialog
{
    private const int DialogWidth  = 74;
    private const int DialogHeight = 20;
    private const int FirstTimeMs  = 30;
    private const int ReplayMs     = 8;

    public static void Show(CutsceneScript script)
    {
        if (script.Beats.Length == 0) return;

        int beatIdx = 0;
        int typingGen = 0;
        string revealText = "";
        bool lineDone = false;
        bool choicesShown = false;
        CutsceneBeat beat = script.Beats[0];
        int charMs = script.IsReplay ? ReplayMs : FirstTimeMs;

        var dialog = DialogHelper.Create($" {script.Title} ", DialogWidth, DialogHeight);

        var portraitLabel = new Label
        {
            X = 2, Y = 1, Width = 10, Height = 7,
            Text = "", ColorScheme = ColorSchemes.Body
        };
        var speakerLabel = new Label
        {
            X = 2, Y = 8, Width = 12, Height = 1,
            Text = "", ColorScheme = ColorSchemes.Body
        };
        var textLabel = new Label
        {
            X = 13, Y = 1, Width = Dim.Fill(2), Height = 9,
            Text = "", ColorScheme = ColorSchemes.Body
        };
        var choiceArea = new View
        {
            X = 2, Y = 11, Width = Dim.Fill(2), Height = 4
        };
        var continueBtn = DialogHelper.CreateButton("Next ►", isDefault: true);
        continueBtn.X = Pos.Center();
        continueBtn.Y = Pos.AnchorEnd(2);

        var hintLabel = new Label
        {
            X = 2, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1),
            Text = script.Unskippable
                ? "Enter: continue | Space: finish line"
                : "Enter: continue | Space: finish | Esc: skip",
            ColorScheme = ColorSchemes.Dim,
        };

        void FinishCurrentLine()
        {
            typingGen++;
            textLabel.Text = revealText;
            textLabel.SetNeedsDraw();
            lineDone = true;
        }

        void StartTyping()
        {
            typingGen++;
            int myGen = typingGen;
            int idx = 0;
            lineDone = false;
            textLabel.Text = "";
            Application.AddTimeout(TimeSpan.FromMilliseconds(charMs), () =>
            {
                if (myGen != typingGen) return false;
                if (idx >= revealText.Length) { lineDone = true; return false; }
                idx++;
                textLabel.Text = revealText[..idx];
                textLabel.SetNeedsDraw();
                return true;
            });
        }

        void ShowChoices(CutsceneChoice[] choices)
        {
            choicesShown = true;
            choiceArea.RemoveAll();
            continueBtn.Visible = false;
            int y = 0;
            foreach (var choice in choices)
            {
                var captured = choice;
                var btn = DialogHelper.CreateButton(captured.Label);
                btn.X = Pos.Center();
                btn.Y = y++;
                btn.Accepting += (s, e) =>
                {
                    e.Cancel = true;
                    captured.Apply?.Invoke();
                    choiceArea.RemoveAll();
                    choicesShown = false;
                    if (!string.IsNullOrEmpty(captured.Response))
                    {
                        revealText = captured.Response!;
                        // Response becomes a mini-beat before advancing.
                        beat = beat with { Choices = null, Text = captured.Response! };
                        StartTyping();
                        continueBtn.Visible = true;
                        continueBtn.SetFocus();
                    }
                    else
                    {
                        AdvanceBeat();
                    }
                };
                choiceArea.Add(btn);
            }
            if (choiceArea.Subviews.Any()) choiceArea.Subviews.First().SetFocus();
        }

        void ShowBeat(int idx)
        {
            if (idx >= script.Beats.Length) { Application.RequestStop(); return; }
            beat = script.Beats[idx];
            revealText = beat.Text;
            choicesShown = false;
            choiceArea.RemoveAll();

            if (!string.IsNullOrEmpty(beat.PortraitKey))
            {
                var portrait = AsciiPortraits.Get(beat.PortraitKey);
                portraitLabel.Text = string.Join("\n", portrait);
                portraitLabel.ColorScheme = ColorSchemes.FromColor(beat.NameColor);
            }
            else portraitLabel.Text = "";
            speakerLabel.Text = beat.Speaker ?? "";
            speakerLabel.ColorScheme = ColorSchemes.FromColor(beat.NameColor);

            StartTyping();
            continueBtn.Visible = true;
            continueBtn.Text = idx < script.Beats.Length - 1 ? " Next ► " : " Close  ";
            continueBtn.SetFocus();
        }

        void AdvanceBeat()
        {
            if (!lineDone) { FinishCurrentLine(); return; }
            if (beat.Choices != null && beat.Choices.Length > 0 && !choicesShown)
            {
                ShowChoices(beat.Choices);
                return;
            }
            beatIdx++;
            ShowBeat(beatIdx);
        }

        continueBtn.Accepting += (s, e) => { e.Cancel = true; AdvanceBeat(); };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Space && !lineDone)
            {
                FinishCurrentLine();
                e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.Esc && !script.Unskippable)
            {
                typingGen++;
                Application.RequestStop();
                e.Handled = true;
            }
        };

        dialog.Add(portraitLabel, speakerLabel, textLabel, choiceArea, continueBtn, hintLabel);
        if (!script.Unskippable) DialogHelper.CloseOnEscape(dialog);
        ShowBeat(0);
        DialogHelper.RunModal(dialog);
        typingGen++;
    }
}
