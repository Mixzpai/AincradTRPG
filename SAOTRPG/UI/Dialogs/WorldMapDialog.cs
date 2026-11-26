using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems.Input;
using SAOTRPG.UI.Helpers;
using SAOTRPG.UI.Rendering;

namespace SAOTRPG.UI.Dialogs;

// The full map, from the map's OpenWorldMap key: the whole screen given to WorldMapView, its keys
// along the bottom, and the standard Close footer.
public static class WorldMapDialog
{
    public static void Show(GameMap map, Player player, int floor)
    {
        var screen = AppHost.App.Screen;
        var dialog = DialogHelper.Create($"Floor {floor} — Map", screen.Width - 2, screen.Height - 2);

        var view = new WorldMapView(map, player, floor)
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2),
        };
        dialog.Add(view);

        string Key(GameAction a) => KeyHints.Key(a).ToLowerInvariant();
        string pan = Key(GameAction.MoveNorth) + Key(GameAction.MoveWest)
                   + Key(GameAction.MoveSouth) + Key(GameAction.MoveEast);
        var hints = new[]
        {
            (pan, "pan"),
            (Key(GameAction.WorldMapZoomIn) + "/" + Key(GameAction.WorldMapZoomOut), "zoom"),
            (Key(GameAction.WorldMapNextPoint), "next point"),
            (Key(GameAction.WorldMapCenter), "back to you"),
        };
        dialog.Add(ScreenHeader.KeyHints(1, Pos.AnchorEnd(1), hints).ToArray());
        DialogHelper.AddCloseFooter(dialog);

        // The framework assigns focus when the session starts, after this builder returns, so a
        // SetFocus here would not stick (rule 286).
        bool focused = false;
        dialog.SubViewsLaidOut += (_, _) =>
        {
            if (focused) return;
            focused = true;
            view.SetFocus();
        };

        DialogHelper.RunModal(dialog);
    }
}
