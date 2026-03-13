using Terminal.Gui;

namespace SAOTRPG.UI;

public static class GameScreen
{
    public static void Show(Window mainWindow, Player player)
    {
        mainWindow.RemoveAll();

        // Combat log panel (left side)
        var combatLogFrame = new FrameView
        {
            Title = "Combat Log",
            X = 0,
            Y = 0,
            Width = Dim.Percent(65),
            Height = Dim.Fill(3)
        };

        var combatLogText = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Text = ""
        };
        combatLogFrame.Add(combatLogText);

        // Set up the game log that writes to the combat log panel
        var gameLog = new GameLogView(combatLogText);

        // Re-wire player and inventory to use the game log
        // (Player already has a log from creation, but we want combat output here)
        player = ReconnectPlayerLog(player, gameLog);

        // Player stats panel (top right)
        var playerStatsFrame = new FrameView
        {
            Title = "Player Stats",
            X = Pos.Percent(65),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(55)
        };

        var playerStatsText = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = player.GetStatsDisplay()
        };
        playerStatsFrame.Add(playerStatsText);

        // Enemy stats panel (bottom right)
        var enemyStatsFrame = new FrameView
        {
            Title = "Enemy",
            X = Pos.Percent(65),
            Y = Pos.Percent(55),
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };

        var enemyStatsText = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = ""
        };
        enemyStatsFrame.Add(enemyStatsText);

        // Action bar (bottom)
        var actionBar = new FrameView
        {
            Title = "Actions",
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            Height = 3
        };

        var attackBtn = new Button
        {
            Text = " Attack ",
            X = 1,
            Y = 0,
            IsDefault = true
        };

        var inventoryBtn = new Button
        {
            Text = " Inventory ",
            X = Pos.Right(attackBtn) + 2,
            Y = 0
        };

        actionBar.Add(attackBtn, inventoryBtn);

        // Spawn the boss encounter
        var currentMonster = new BossMonster();
        currentMonster.SetLog(gameLog);

        gameLog.Log($"<<{currentMonster.Name}>> Encountered!");
        gameLog.Log($"Level {currentMonster.Level} | HP: {currentMonster.CurrentHealth}/{currentMonster.MaxHealth}");
        gameLog.Log("");

        enemyStatsText.Text = currentMonster.GetStatusDisplay();

        // Attack button handler
        attackBtn.Accepting += (s, e) =>
        {
            if (currentMonster.IsDefeated)
            {
                gameLog.Log("The enemy is already defeated.");
                e.Cancel = true;
                return;
            }

            int damage = player.AttackMonster(currentMonster);
            var reward = currentMonster.TakeDamage(damage);
            gameLog.Log("");

            // Refresh panels
            playerStatsText.Text = player.GetStatsDisplay();
            enemyStatsText.Text = currentMonster.GetStatusDisplay();

            if (currentMonster.IsDefeated && reward != null)
            {
                player.GainExperience(reward.Experience);
                playerStatsText.Text = player.GetStatsDisplay();
                gameLog.Log("");
                gameLog.Log("Victory! Press any key to continue...");
            }

            e.Cancel = true;
        };

        // Inventory button handler
        inventoryBtn.Accepting += (s, e) =>
        {
            ShowInventoryDialog(player);
            e.Cancel = true;
        };

        mainWindow.Add(combatLogFrame, playerStatsFrame, enemyStatsFrame, actionBar);
        NavigationHelper.EnableGameNavigation(actionBar);
        attackBtn.SetFocus();
    }

    private static Player ReconnectPlayerLog(Player player, IGameLog gameLog)
    {
        // Create a new player with the same stats but connected to the game screen log
        var newPlayer = Player.CreateNewPlayer(
            player.FirstName, player.LastName, player.Gender,
            gameLog, new TerminalGuiInventoryLogger(gameLog));

        // Copy over any stat changes from skill allocation
        newPlayer.Vitality = player.Vitality;
        newPlayer.Strength = player.Strength;
        newPlayer.Endurance = player.Endurance;
        newPlayer.Dexterity = player.Dexterity;
        newPlayer.Agility = player.Agility;
        newPlayer.Intelligence = player.Intelligence;
        newPlayer.SkillPoints = player.SkillPoints;
        newPlayer.Level = player.Level;
        newPlayer.CurrentExperience = player.CurrentExperience;
        newPlayer.CurrentHealth = newPlayer.MaxHealth;
        newPlayer.ColOnHand = player.ColOnHand;

        return newPlayer;
    }

    private static void ShowInventoryDialog(Player player)
    {
        var dialog = new Dialog
        {
            Title = "Inventory",
            Width = 50,
            Height = 20
        };

        var inventoryText = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ReadOnly = true,
            Text = GetInventoryDisplay(player)
        };

        var closeBtn = new Button
        {
            Text = " Close ",
            IsDefault = true
        };

        closeBtn.Accepting += (s, e) =>
        {
            Application.RequestStop();
            e.Cancel = true;
        };

        dialog.Add(inventoryText);
        dialog.AddButton(closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }

    private static string GetInventoryDisplay(Player player)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Items ({player.Inventory.ItemCount}/{player.Inventory.MaxSlots}):");
        sb.AppendLine();

        if (player.Inventory.ItemCount == 0)
        {
            sb.AppendLine("  (Empty)");
        }
        else
        {
            foreach (var item in player.Inventory.Items)
            {
                string qty = item is YourGame.Items.StackableItem s ? $" x{s.Quantity}" : "";
                sb.AppendLine($"  {item.Name}{qty} ({item.Rarity})");
            }
        }

        return sb.ToString();
    }
}
