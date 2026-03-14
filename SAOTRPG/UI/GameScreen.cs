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
        var skillsBtn = new Button { Text = " Skills ", X = Pos.Right(inventoryBtn) + 2, Y = 0, ColorScheme = NavigationHelper.ButtonScheme };
        actionBar.Add(attackBtn, inventoryBtn, skillsBtn);

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

        // Skills handler — opens modal dialog, refreshes stats on close
        skillsBtn.Accepting += (s, e) => { ShowSkillPointDialog(player); Refresh(); e.Cancel = true; };

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

    // Modal skill point allocation dialog — +/- buttons per stat with allocate and reset
    private static void ShowSkillPointDialog(Player player)
    {
        var dialog = new Dialog { Title = "Allocate Skill Points", Width = 80, Height = 30 };

        var pointsAvailableLabel = new Label { Text = $"Skill Points Available: {player.SkillPoints}", X = 1, Y = 1, Width = Dim.Auto(), Height = 1 };

        // Live stats preview — wide and tall enough for full GetStatsDisplay output
        var statsView = new TextView { X = 1, Y = 3, Width = 38, Height = 18, ReadOnly = true, Text = player.GetStatsDisplay() };

        // Per-stat +/- buttons with pending point counts
        var stats = new string[] { "Vitality", "Strength", "Endurance", "Dexterity", "Agility", "Intelligence" };
        var pending = new int[stats.Length];
        var pendingLabels = new Label[stats.Length];

        int startY = 3;
        int colX = 42;

        int TotalPending() { int sum = 0; foreach (var p in pending) sum += p; return sum; }

        void RefreshPendingDisplay()
        {
            for (int i = 0; i < stats.Length; i++)
                pendingLabels[i].Text = pending[i] > 0 ? $"+{pending[i]}" : " 0";
            pointsAvailableLabel.Text = $"Skill Points Available: {player.SkillPoints} (Queued: {TotalPending()})";
        }

        for (int i = 0; i < stats.Length; i++)
        {
            int row = startY + i * 2;
            int idx = i;

            var nameLabel = new Label { Text = $"{stats[i]}:", X = colX, Y = row, Width = 14, Height = 1 };

            var minusBtn = new Button { Text = " - ", X = colX + 14, Y = row, ColorScheme = NavigationHelper.ButtonScheme };
            pendingLabels[i] = new Label { Text = " 0", X = Pos.Right(minusBtn) + 1, Y = row, Width = 4, Height = 1 };
            var plusBtn = new Button { Text = " + ", X = Pos.Right(pendingLabels[i]) + 1, Y = row, ColorScheme = NavigationHelper.ButtonScheme };

            minusBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (pending[idx] > 0)
                {
                    pending[idx]--;
                    RefreshPendingDisplay();
                }
            };

            plusBtn.Accepting += (s, e) =>
            {
                e.Cancel = true;
                if (TotalPending() < player.SkillPoints)
                {
                    pending[idx]++;
                    RefreshPendingDisplay();
                }
            };

            dialog.Add(nameLabel, minusBtn, pendingLabels[i], plusBtn);
        }

        int controlsY = startY + stats.Length * 2 + 1;

        var allocateBtn = new Button { Text = " Allocate ", X = colX, Y = controlsY, ColorScheme = NavigationHelper.ButtonScheme };
        var resetBtn = new Button { Text = " Reset ", X = Pos.Right(allocateBtn) + 2, Y = controlsY, ColorScheme = NavigationHelper.ButtonScheme };

        var messageLabel = new Label { Text = "", X = colX, Y = controlsY + 2, Width = 35, Height = 2 };

        // Commit all pending points at once
        allocateBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (TotalPending() == 0)
            { messageLabel.Text = "No points queued."; return; }

            var results = new System.Text.StringBuilder();
            for (int i = 0; i < stats.Length; i++)
            {
                if (pending[i] <= 0) continue;
                if (player.SpendSkillPoints(stats[i], pending[i]))
                    results.Append($"+{pending[i]} {stats[i]}  ");
                else
                    results.Append($"{stats[i]}: not enough pts  ");
                pending[i] = 0;
            }

            RefreshPendingDisplay();
            statsView.Text = player.GetStatsDisplay();
            messageLabel.Text = results.ToString();
        };

        // Reset — reclaim all allocated stat points back to available pool
        resetBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            player.ResetStatAllocation();
            for (int i = 0; i < pending.Length; i++)
                pending[i] = 0;
            RefreshPendingDisplay();
            statsView.Text = player.GetStatsDisplay();
            messageLabel.Text = "Stats reset! Points refunded.";
        };

        var closeBtn = new Button { Text = " Close ", IsDefault = true, ColorScheme = NavigationHelper.ButtonScheme };
        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        dialog.Add(pointsAvailableLabel, statsView, allocateBtn, resetBtn, messageLabel);
        dialog.AddButton(closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }
}
