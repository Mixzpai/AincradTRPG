using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Pause menu — opens on Esc from the map view. Four vertical actions:
// Save / Load / Options / Exit. Each destructive choice gates behind a
// MessageBox confirmation so a mis-press never wipes progress. Esc on
// the menu closes back to the map (DialogHelper.CloseOnEscape).
public static class PauseMenuDialog
{
    private const int DialogWidth = 44, DialogHeight = 14;

    // The map view wires a single PauseRequested handler that routes here.
    // `saveFlash` is the shared frame-counter array GameScreen uses to
    // briefly tint the save indicator after a successful save — we pulse
    // it on save so the player gets the same feedback as F5 quick-save.
    public static void Show(Window mainWindow, Player player, TurnManager turnManager,
        int saveSlot, GameLogView gameLog, int[] saveFlash)
    {
        bool loadRequested = false;
        bool exitRequested = false;

        var dialog = DialogHelper.Create("Paused", DialogWidth, DialogHeight);

        var header = new Label
        {
            Text = "  Game Paused",
            X = 0, Y = 0, Width = Dim.Fill(), Height = 1,
            ColorScheme = ColorSchemes.Gold,
        };

        // Four vertical buttons, centered. Each row is one action.
        var saveBtn = MakeMenuButton("Save Game", 2, isDefault: true);
        var loadBtn = MakeMenuButton("Load Game", 4);
        var optionsBtn = MakeMenuButton("Options", 6);
        var exitBtn = MakeMenuButton("Exit Game", 8);

        var hint = new Label
        {
            Text = "Enter: select   Esc: resume",
            X = Pos.Center(), Y = Pos.AnchorEnd(1),
            Width = Dim.Auto(), ColorScheme = ColorSchemes.Dim,
        };

        saveBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int choice = MessageBox.Query("Save Game", $"Save to slot {saveSlot}?", "Yes", "No");
            if (choice != 0) return;

            if (SaveManager.SaveGame(player, turnManager, saveSlot))
            {
                gameLog.LogSystem("[Game saved]");
                saveFlash[0] = 5;
            }
            else
            {
                gameLog.LogSystem("[Save failed!]");
            }
            Application.RequestStop();
        };

        loadBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (!SaveManager.SaveExists(saveSlot))
            {
                MessageBox.Query("Load Game",
                    $"No save in slot {saveSlot}.", "OK");
                return;
            }
            int choice = MessageBox.Query("Load Game",
                "Load last save? Current progress will be lost.", "Yes", "No");
            if (choice != 0) return;
            loadRequested = true;
            Application.RequestStop();
        };

        optionsBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            // OptionsScreen RemoveAlls and rebuilds mainWindow, so we must
            // close the pause dialog first AND give Options a way back to
            // the live run. Strategy: auto-save before opening Options so
            // the in-memory run is preserved on disk, then pass an onBack
            // callback that reloads that save. The player sees a brief
            // [Auto-saved] log line and returns to their run on Back.
            SaveManager.SaveGame(player, turnManager, saveSlot);
            gameLog.LogSystem("[Auto-saved for Options]");
            saveFlash[0] = 5;
            Application.RequestStop();
            OptionsScreen.Show(mainWindow, onBack: () =>
            {
                var save = SaveManager.LoadGame(saveSlot);
                if (save != null) GameScreen.ShowFromSave(mainWindow, save, saveSlot);
                else TitleScreen.Show(mainWindow);
            });
        };

        exitBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            int choice = MessageBox.Query("Exit Game",
                "Exit to desktop? Unsaved progress will be lost.", "Yes", "No");
            if (choice != 0) return;
            exitRequested = true;
            Application.RequestStop();
        };

        // Up/Down arrows between the four menu buttons. Terminal.Gui's
        // default Tab order handles left/right but Up/Down on buttons is
        // not wired — follow memory #416: attach key handling to the
        // buttons themselves (children), not the Dialog, so presses land.
        NavigationHelper.WireUpDown(saveBtn, exitBtn, loadBtn);
        NavigationHelper.WireUpDown(loadBtn, saveBtn, optionsBtn);
        NavigationHelper.WireUpDown(optionsBtn, loadBtn, exitBtn);
        NavigationHelper.WireUpDown(exitBtn, optionsBtn, saveBtn);

        dialog.Add(header, saveBtn, loadBtn, optionsBtn, exitBtn, hint);
        DialogHelper.CloseOnEscape(dialog);
        saveBtn.SetFocus();
        DialogHelper.RunModal(dialog);

        // Post-close handling — done after the dialog exits so any lingering
        // modal state is torn down first.
        if (exitRequested)
        {
            Application.RequestStop();
            return;
        }

        if (loadRequested)
        {
            var save = SaveManager.LoadGame(saveSlot);
            if (save != null) GameScreen.ShowFromSave(mainWindow, save, saveSlot);
            else gameLog.LogSystem("[Load failed!]");
        }
    }

    private static Button MakeMenuButton(string text, int y, bool isDefault = false) => new()
    {
        Text = $" {text} ",
        X = Pos.Center(), Y = y,
        ColorScheme = ColorSchemes.MenuButton,
        IsDefault = isDefault,
    };
}
