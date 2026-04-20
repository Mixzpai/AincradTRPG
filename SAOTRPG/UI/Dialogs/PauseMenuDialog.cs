using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Systems;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI.Dialogs;

// Pause menu (Esc from map): Save / Load / Options / Exit. Destructive choices MessageBox-gated.
// Esc here resumes (DialogHelper.CloseOnEscape).
public static class PauseMenuDialog
{
    private const int DialogWidth = 44, DialogHeight = 14;

    // `saveFlash` is GameScreen's shared frame counter — we pulse it on save for F5-like feedback.
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
            // OptionsScreen rebuilds mainWindow → auto-save first, reload via onBack callback.
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

        // Up/Down isn't wired by default on buttons — attach to buttons (not Dialog) so presses land.
        NavigationHelper.WireUpDown(saveBtn, exitBtn, loadBtn);
        NavigationHelper.WireUpDown(loadBtn, saveBtn, optionsBtn);
        NavigationHelper.WireUpDown(optionsBtn, loadBtn, exitBtn);
        NavigationHelper.WireUpDown(exitBtn, optionsBtn, saveBtn);

        dialog.Add(header, saveBtn, loadBtn, optionsBtn, exitBtn, hint);
        DialogHelper.CloseOnEscape(dialog);
        saveBtn.SetFocus();
        DialogHelper.RunModal(dialog);

        // Post-close: runs after dialog exits so modal state is torn down first.
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

    // Shared CreateMenuButton ► label ◄ marker — consistent with TitleScreen/DifficultyScreen/etc.
    private static Button MakeMenuButton(string text, int y, bool isDefault = false)
    {
        var btn = DialogHelper.CreateMenuButton(text, isDefault);
        btn.X = Pos.Center();
        btn.Y = y;
        return btn;
    }
}
