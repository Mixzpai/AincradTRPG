using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Modal dialog for picking a passive talent on level-up.
public static class TalentPickDialog
{
    public static PassiveTalents.Perk? Show(PassiveTalents.Perk[] choices)
    {
        PassiveTalents.Perk? picked = null;

        // Height: 1 top padding + (choices * 3 rows each) + 1 description + 2 hint/bottom
        int height = 3 + choices.Length * 3 + 3;
        var dlg = DialogHelper.Create("* Level Up -- Choose a Talent *", 48, height);

        // Description label -- shows the focused perk's effect.
        var descLabel = new Label
        {
            Text = "",
            X = Pos.Center(), Y = Pos.AnchorEnd(3),
            Width = 44,
            ColorScheme = ColorSchemes.Dim,
        };
        dlg.Add(descLabel);

        for (int i = 0; i < choices.Length; i++)
        {
            var perk = choices[i];
            var btn = DialogHelper.CreateButton(perk.Name);
            btn.X = 1;
            btn.Y = 1 + i * 3;
            btn.HasFocusChanged += (s, e) => { if (e.NewValue) descLabel.Text = perk.Description; };
            btn.Accepting += (s, e) => { e.Cancel = true; picked = perk; Application.RequestStop(); };
            dlg.Add(btn);
            if (i == 0)
            {
                btn.SetFocus();
                descLabel.Text = perk.Description;
            }
        }

        var hintLabel = new Label
        {
            Text = "Enter: choose talent",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = ColorSchemes.Dim,
        };
        dlg.Add(hintLabel);

        DialogHelper.RunModal(dlg);
        return picked;
    }
}
