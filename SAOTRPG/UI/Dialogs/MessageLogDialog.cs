using SAOTRPG.UI.GameLog;
using SAOTRPG.UI.Helpers;
using SAOTRPG.UI.Screens;

namespace SAOTRPG.UI.Dialogs;

// The message log, from the map's OpenMessageLog key: the sidebar log's whole history in a
// MessageLogView, placed where CDDA places its own. The view paints its frame, so the dialog
// has no border, title or shadow.
public static class MessageLogDialog
{
    public static void Show(ColoredLogView log)
    {
        var screen = AppHost.App.Screen;
        // CDDA: inset by the sidebar's width on both sides, or, when that leaves it narrower
        // than three times its height, across the whole map with the sidebar left in view.
        int side = GameScreen.SidebarWidth, x = side, width = screen.Width - 2 * side;
        if (width < screen.Height * 3) { x = 0; width = screen.Width - side; }

        var dialog = new Dialog
        {
            X = x, Y = 0, Width = Math.Max(24, width), Height = screen.Height,
            BorderStyle = LineStyle.None, ShadowStyle = null,
            SchemeName = ColorSchemes.DialogName,
        };
        var view = new MessageLogView(log) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        dialog.Add(view);

        // The framework assigns focus when the session starts, after this builder returns (rule 286).
        bool focused = false;
        dialog.SubViewsLaidOut += (_, _) =>
        {
            if (focused) return;
            focused = true;
            view.SetFocus();
        };

        DialogHelper.RunModal(dialog);
        log.MarkSeen();
    }
}
