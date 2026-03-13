using System.Linq;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Inventory.Logging;

namespace SAOTRPG.UI;

public static class GameScreen
{
    public static void Show(Window mainWindow, Player player)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("GameScreen.Show");
        DebugLogger.LogScreen("GameScreen");

        // Layout — three panels: combat log (left), player stats (top-right), enemy (bottom-right)
        var (combatLogFrame, combatLogText) = CreatePanel("Combat Log", 0, 0, Dim.Percent(65), Dim.Fill(3));
        var (playerStatsFrame, playerStatsText) = CreatePanel("Player Stats", Pos.Percent(65), 0, Dim.Fill(), Dim.Percent(55));
        var (enemyStatsFrame, enemyStatsText) = CreatePanel("Enemy", Pos.Percent(65), Pos.Percent(55), Dim.Fill(), Dim.Fill(3));

        combatLogText.WordWrap = true;
        playerStatsText.Text = player.GetStatsDisplay();

        // Wire up game log to the combat log panel
        var gameLog = new GameLogView(combatLogText);
        player.SetLog(gameLog);
        player.Inventory.SetLogger(new TerminalGuiInventoryLogger(gameLog));

        // Action bar — bottom strip with combat and menu buttons
        var actionBar = new FrameView { Title = "Actions", X = 0, Y = Pos.AnchorEnd(3), Width = Dim.Fill(), Height = 3 };
        var attackBtn = new Button { Text = " Attack ", X = 1, Y = 0, IsDefault = true, ColorScheme = NavigationHelper.ButtonScheme };
        var inventoryBtn = new Button { Text = " Inventory ", X = Pos.Right(attackBtn) + 2, Y = 0, ColorScheme = NavigationHelper.ButtonScheme };
        actionBar.Add(attackBtn, inventoryBtn);

        // Spawn encounter — currently hardcoded to Illfang
        var currentMonster = new IllfangTheKobaldLord();
        currentMonster.SetLog(gameLog);
        gameLog.Log($"<<{currentMonster.Name}>> Encountered!");
        gameLog.Log($"Level {currentMonster.Level} | HP: {currentMonster.CurrentHealth}/{currentMonster.MaxHealth}");
        gameLog.Log("");
        enemyStatsText.Text = currentMonster.GetStatusDisplay();

        // Refresh — updates all stat panels after any state change
        void Refresh()
        {
            playerStatsText.Text = player.GetStatsDisplay();
            enemyStatsText.Text = currentMonster.GetStatusDisplay();
        }

        // Attack handler — deal damage, check for defeat, award XP
        attackBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (currentMonster.IsDefeated) { gameLog.Log("The enemy is already defeated."); return; }

            var reward = currentMonster.TakeDamage(player.AttackMonster(currentMonster));
            gameLog.Log("");
            Refresh();

            if (currentMonster.IsDefeated && reward != null)
            {
                player.GainExperience(reward.Experience);
                Refresh();
                gameLog.Log("");
                gameLog.Log("Victory! Press any key to continue...");
            }
        };

        // Inventory handler — opens modal dialog
        inventoryBtn.Accepting += (s, e) => { ShowInventoryDialog(player); e.Cancel = true; };

        mainWindow.Add(combatLogFrame, playerStatsFrame, enemyStatsFrame, actionBar);
        NavigationHelper.EnableGameNavigation(actionBar);
        attackBtn.SetFocus();

        // Log initial state + screen load time
        DebugLogger.LogState($"Player \"{player.FirstName}\"", $"LVL:{player.Level} HP:{player.CurrentHealth}/{player.MaxHealth} ATK:{player.Attack} DEF:{player.Defense} SPD:{player.Speed}");
        DebugLogger.LogState($"Enemy \"{currentMonster.Name}\"", $"LVL:{currentMonster.Level} HP:{currentMonster.CurrentHealth}/{currentMonster.MaxHealth}");
        DebugLogger.EndTimer("GameScreen.Show", sw);
    }

    // Creates a titled FrameView with a read-only TextView child — reusable for any info panel
    private static (FrameView frame, TextView text) CreatePanel(string title, Pos x, Pos y, Dim width, Dim height)
    {
        var frame = new FrameView { Title = title, X = x, Y = y, Width = width, Height = height };
        var text = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, Text = "" };
        frame.Add(text);
        return (frame, text);
    }

    // Modal inventory dialog — shows item list with quantity and rarity
    private static void ShowInventoryDialog(Player player)
    {
        var dialog = new Dialog { Title = "Inventory", Width = 50, Height = 20 };

        // Build item list — shows "(Empty)" or each item with stack count
        var items = player.Inventory.ItemCount == 0
            ? "  (Empty)"
            : string.Join("\n", player.Inventory.Items.Select(item =>
            {
                string qty = item is StackableItem s ? $" x{s.Quantity}" : "";
                return $"  {item.Name}{qty} ({item.Rarity})";
            }));

        var inventoryText = new TextView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1),
            ReadOnly = true, Text = $"Items ({player.Inventory.ItemCount}/{player.Inventory.MaxSlots}):\n\n{items}"
        };

        var closeBtn = new Button { Text = " Close ", IsDefault = true, ColorScheme = NavigationHelper.ButtonScheme };
        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        dialog.Add(inventoryText);
        dialog.AddButton(closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }
}
