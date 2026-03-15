using System.Linq;
using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Inventory.Logging;

namespace SAOTRPG.UI;

public static class GameScreen
{
    public static void Show(Window mainWindow, Player player)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("GameScreen.Show");
        DebugLogger.LogScreen("GameScreen");

        // ── Layout — three panels + action bar ──────────────────────────
        var (combatLogFrame, combatLogText) = CreatePanel($" {Theme.Sword} Combat Log ", 0, 0, Dim.Percent(65), Dim.Fill(3));
        var (playerStatsFrame, playerStatsText) = CreatePanel($" {Theme.Heart} {player.FirstName} {player.LastName} ", Pos.Percent(65), 0, Dim.Fill(), Dim.Percent(55));
        var (enemyStatsFrame, enemyStatsText) = CreatePanel($" {Theme.Sword} Enemy ", Pos.Percent(65), Pos.Percent(55), Dim.Fill(), Dim.Fill(3));

        combatLogFrame.ColorScheme = Theme.FrameSubtle;
        playerStatsFrame.ColorScheme = Theme.FrameSubtle;
        enemyStatsFrame.ColorScheme = Theme.FrameSubtle;

        combatLogText.WordWrap = true;
        combatLogText.ColorScheme = Theme.Body;
        playerStatsText.Text = player.GetStatsDisplay();
        playerStatsText.ColorScheme = Theme.Body;
        enemyStatsText.ColorScheme = Theme.Body;

        // Wire up game log to the combat log panel
        var gameLog = new GameLogView(combatLogText);
        player.SetLog(gameLog);
        player.Inventory.SetLogger(new TerminalGuiInventoryLogger(gameLog));

        // ── Action bar — bottom strip ──────────────────────────────────
        var actionBar = new FrameView
        {
            Title = $" {Theme.TriRight} Actions ",
            X = 0, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(), Height = 3,
            ColorScheme = Theme.FrameSubtle
        };

        var attackBtn = new Button { Text = $" {Theme.Sword} Attack ", X = 1, Y = 0, IsDefault = true, ColorScheme = Theme.MenuButton };
        var inventoryBtn = new Button { Text = $" {Theme.SquareFull} Inventory ", X = Pos.Right(attackBtn) + 2, Y = 0, ColorScheme = Theme.MenuButton };
        var skillsBtn = new Button { Text = $" {Theme.Sparkle} Skills ", X = Pos.Right(inventoryBtn) + 2, Y = 0, ColorScheme = Theme.MenuButton };

        Theme.AttachDiamondFocus(attackBtn, inventoryBtn, skillsBtn);
        actionBar.Add(attackBtn, inventoryBtn, skillsBtn);

        // ── Spawn encounter ────────────────────────────────────────────
        var currentMonster = new IllfangTheKobaldLord();
        currentMonster.SetLog(gameLog);

        enemyStatsFrame.Title = $" {Theme.Sword} {currentMonster.Name} ";

        // Encounter intro — decorative banner
        gameLog.Log("");
        gameLog.Log($"  {Theme.HeavyRule(36)}");
        gameLog.Log($"  {Theme.Diamond}  A shadow looms before you...");
        gameLog.Log($"  {Theme.HeavyRule(36)}");
        gameLog.Log("");
        gameLog.LogCombat($"{currentMonster.Name} Encountered!");
        gameLog.Log($"    Level {currentMonster.Level} | HP: {currentMonster.CurrentHealth}/{currentMonster.MaxHealth}");
        gameLog.Log($"    ATK: {currentMonster.BaseAttack} | DEF: {currentMonster.BaseDefense} | SPD: {currentMonster.BaseSpeed}");
        gameLog.Log("");
        gameLog.Log($"  {Theme.LightRule(36)}");
        gameLog.Log("");
        enemyStatsText.Text = currentMonster.GetStatusDisplay();

        // ── Refresh — updates all stat panels ──────────────────────────
        void Refresh()
        {
            playerStatsText.Text = player.GetStatsDisplay();
            enemyStatsText.Text = currentMonster.GetStatusDisplay();
        }

        // ── Attack handler ─────────────────────────────────────────────
        attackBtn.Accepting += (s, e) =>
        {
            e.Cancel = true;
            if (currentMonster.IsDefeated)
            {
                gameLog.Log($" {Theme.BulletOpen} The enemy is already defeated.");
                return;
            }

            var reward = currentMonster.TakeDamage(player.AttackMonster(currentMonster));
            gameLog.Log("");
            Refresh();

            if (currentMonster.IsDefeated && reward != null)
            {
                player.GainExperience(reward.Experience);
                Refresh();
                gameLog.Log("");
                gameLog.Log($"  {Theme.HeavyRule(36)}");
                gameLog.Log($"  {Theme.Star}  VICTORY!  {Theme.Star}");
                if (reward.WasOverkill)
                    gameLog.LogCombat($"OVERKILL! +{reward.OverkillDamage} bonus damage!");
                gameLog.Log($"  {Theme.LightRule(36)}");
                gameLog.LogLoot($"+{reward.Experience} EXP");
                gameLog.LogLoot($"+{reward.Col} Col");
                gameLog.Log($"  {Theme.HeavyRule(36)}");
                gameLog.Log("");
            }
        };

        // ── Inventory handler ──────────────────────────────────────────
        inventoryBtn.Accepting += (s, e) => { ShowInventoryDialog(player); e.Cancel = true; };

        // ── Skills handler ─────────────────────────────────────────────
        skillsBtn.Accepting += (s, e) => { ShowSkillPointDialog(player); Refresh(); e.Cancel = true; };

        mainWindow.Add(combatLogFrame, playerStatsFrame, enemyStatsFrame, actionBar);
        NavigationHelper.EnableGameNavigation(actionBar);
        attackBtn.SetFocus();

        DebugLogger.LogState($"Player \"{player.FirstName}\"", $"LVL:{player.Level} HP:{player.CurrentHealth}/{player.MaxHealth} ATK:{player.Attack} DEF:{player.Defense} SPD:{player.Speed}");
        DebugLogger.LogState($"Enemy \"{currentMonster.Name}\"", $"LVL:{currentMonster.Level} HP:{currentMonster.CurrentHealth}/{currentMonster.MaxHealth}");
        DebugLogger.EndTimer("GameScreen.Show", sw);
    }

    // ── Panel factory ──────────────────────────────────────────────────
    private static (FrameView frame, TextView text) CreatePanel(string title, Pos x, Pos y, Dim width, Dim height)
    {
        var frame = new FrameView { Title = title, X = x, Y = y, Width = width, Height = height };
        var text = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, Text = "" };
        frame.Add(text);
        return (frame, text);
    }

    // ── Inventory dialog — rarity colors, equipped markers, structure ──
    private static void ShowInventoryDialog(Player player)
    {
        var dialog = new Dialog
        {
            Title = $" {Theme.SquareFull} Inventory ",
            Width = 55, Height = 26,
            ColorScheme = Theme.FrameSubtle
        };

        // Header — item count
        var headerLabel = new Label
        {
            Text = $" {Theme.Diamond} Items ({player.Inventory.ItemCount}/{player.Inventory.MaxSlots})",
            X = 1, Y = 0,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.StatHeader
        };

        var divider1 = new Label
        {
            Text = Theme.LightRule(48),
            X = 1, Y = 1,
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.Dim
        };

        dialog.Add(headerLabel, divider1);

        if (player.Inventory.ItemCount == 0)
        {
            var emptyLabel = new Label
            {
                Text = $"  {Theme.BulletOpen} Empty {Theme.DottedRule(6)} no items yet",
                X = 1, Y = 3,
                Width = Dim.Auto(), Height = 1,
                ColorScheme = Theme.Dim
            };
            dialog.Add(emptyLabel);
        }
        else
        {
            int row = 3;
            foreach (var item in player.Inventory.Items)
            {
                string qty = item is StackableItem s ? $" x{s.Quantity}" : "";
                char glyph = Theme.GetRarityGlyph(item.Rarity);
                string line = $"  {glyph} {item.Name}{qty}";
                string rarityTag = $" ({item.Rarity ?? "Common"})";

                var itemLabel = new Label
                {
                    Text = line,
                    X = 1, Y = row,
                    Width = Dim.Auto(), Height = 1,
                    ColorScheme = Theme.GetRarityScheme(item.Rarity)
                };

                var rarityLabel = new Label
                {
                    Text = rarityTag,
                    X = Pos.Right(itemLabel), Y = row,
                    Width = Dim.Auto(), Height = 1,
                    ColorScheme = Theme.Dim
                };

                dialog.Add(itemLabel, rarityLabel);
                row++;
            }
        }

        // ── Equipment section ───────────────────────────────────────────
        var equipDivider = new Label
        {
            Text = $" {Theme.HeavyRule(18)} Equipped {Theme.HeavyRule(18)}",
            X = 1, Y = Pos.AnchorEnd(8),
            Width = Dim.Auto(), Height = 1,
            ColorScheme = Theme.StatHeader
        };

        var equippedText = new Label
        {
            Text = BuildEquippedDisplay(player),
            X = 2, Y = Pos.AnchorEnd(7),
            Width = Dim.Auto(), Height = 6,
            ColorScheme = Theme.Subtitle
        };

        dialog.Add(equipDivider, equippedText);

        var closeBtn = new Button { Text = $" {Theme.ArrowLeft} Close ", IsDefault = true, ColorScheme = Theme.MenuButton };
        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        dialog.AddButton(closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }

    private static string BuildEquippedDisplay(Player player)
    {
        var slots = Enum.GetValues<EquipmentSlot>();
        var lines = new List<string>();

        foreach (var slot in slots)
        {
            var item = player.Inventory.GetEquipped(slot);
            if (item != null)
            {
                char glyph = Theme.GetRarityGlyph(item.Rarity);
                lines.Add($"  {glyph} {slot}: {item.Name}");
            }
            else
            {
                lines.Add($"  {Theme.BulletOpen} {slot}: {Theme.DottedRule(3)} empty");
            }
        }

        return string.Join("\n", lines);
    }

    // ── Skill point dialog — uses shared SkillAllocationPanel ──────────
    private static void ShowSkillPointDialog(Player player)
    {
        var dialog = new Dialog
        {
            Title = $" {Theme.Sparkle} Allocate Skill Points ",
            Width = 82, Height = 30,
            ColorScheme = Theme.FrameSubtle
        };

        var result = SkillAllocationPanel.Build(dialog, player, statsX: 1, statsY: 1, controlsX: 42, controlsStartY: 1);

        var closeBtn = new Button { Text = $" {Theme.ArrowLeft} Close ", IsDefault = true, ColorScheme = Theme.MenuButton };
        closeBtn.Accepting += (s, e) => { Application.RequestStop(); e.Cancel = true; };

        dialog.AddButton(closeBtn);
        Application.Run(dialog);
        dialog.Dispose();
    }
}
