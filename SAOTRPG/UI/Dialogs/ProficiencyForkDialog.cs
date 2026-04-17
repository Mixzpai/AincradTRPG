using Terminal.Gui;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Small modal shown when a weapon type crosses a fork threshold (L25/50/75/100).
// Player picks one of two passive bonuses that will apply for this save only.
// Return value: 1 or 2 for the picked option, 0 if cancelled (should rarely
// happen — the dialog has no cancel affordance beyond Esc, which just closes
// leaving the fork pending for the next StatsDialog open).
public static class ProficiencyForkDialog
{
    public static int Show(string weaponType, int forkLevel,
        ProficiencyFork opt1, ProficiencyFork opt2)
    {
        int picked = 0;
        var dlg = DialogHelper.Create(
            $"★ {weaponType} — Level {forkLevel} Fork ★", 60, 14);

        var headerLbl = new Label
        {
            Text = $"Pick one passive for {weaponType}:",
            X = 2, Y = 1, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Gold,
        };

        var opt1Btn = DialogHelper.CreateButton(opt1.Name, isDefault: true);
        opt1Btn.X = 2; opt1Btn.Y = 3;
        var opt1Desc = new Label
        {
            Text = $"  {opt1.Description}",
            X = 2, Y = 4, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Dim,
        };

        var opt2Btn = DialogHelper.CreateButton(opt2.Name);
        opt2Btn.X = 2; opt2Btn.Y = 6;
        var opt2Desc = new Label
        {
            Text = $"  {opt2.Description}",
            X = 2, Y = 7, Width = Dim.Fill(2),
            ColorScheme = ColorSchemes.Dim,
        };

        opt1Btn.Accepting += (s, e) =>
        {
            e.Cancel = true; picked = 1; Application.RequestStop();
        };
        opt2Btn.Accepting += (s, e) =>
        {
            e.Cancel = true; picked = 2; Application.RequestStop();
        };

        var hintLbl = new Label
        {
            Text = "Enter: pick | Esc: decide later",
            X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1),
            ColorScheme = ColorSchemes.Dim,
        };

        dlg.Add(headerLbl, opt1Btn, opt1Desc, opt2Btn, opt2Desc, hintLbl);
        DialogHelper.CloseOnEscape(dlg);
        opt1Btn.SetFocus();
        DialogHelper.RunModal(dlg);
        return picked;
    }
}
