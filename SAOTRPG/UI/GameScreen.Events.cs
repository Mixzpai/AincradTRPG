using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Dialogs;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

public static partial class GameScreen
{
    private static void WireVisualEffects(TurnManager turnManager, MapView mapView)
    {
        turnManager.DamageDealt += (x, y, dmg, isPlayer, isCrit) =>
        {
            if (isCrit) mapView.AddCritDamageFlash(x, y, dmg, isPlayer);
            else mapView.AddDamageFlash(x, y, dmg, isPlayer);
            if (isPlayer) mapView.AddScorchMark(x, y);
            if (isCrit) mapView.FlashBorder(isPlayer ? Color.BrightRed : Color.BrightCyan, 4);
            else if (isPlayer) mapView.FlashBorder(Color.Red, 2);
            // Red-BG flash on the tile that took damage.
            mapView.AddHitFlash(x, y);
            // Player crits trigger a single-frame screen-wide brightness boost.
            if (isCrit && !isPlayer) mapView.TriggerCritScreenFlash();
        };
        turnManager.WeaponSwing += (fx, fy, tx, ty, c) => mapView.AddWeaponSwing(fx, fy, tx, ty, c);
        turnManager.CombatTextEvent += (x, y, text, color) => mapView.AddTextFlash(x, y, text, color);
        turnManager.LeveledUp += () =>
        {
            mapView.TriggerLevelUpFlash();
            mapView.FlashBorder(Color.BrightYellow, 6);
            PartySystem.ScaleToPlayer(turnManager.Player.Level);
            turnManager.RequestTalentPick();
        };
        turnManager.MonsterKilled += (x, y) =>
        {
            mapView.AddCorpseMarker(x, y);
            mapView.AddShatterParticle(x, y);
            mapView.FlashBorder(Color.BrightCyan, 2); // SAO blue flash on kill
            if (turnManager.KillStreak >= 2)
                mapView.TriggerKillStreakFlash(turnManager.KillStreak);
        };
        turnManager.SkillActivated += (x, y, color) =>
        {
            mapView.AddSkillFlash(x, y, color);
            mapView.FlashBorder(color, 1);
        };
    }

    private static void WireSystemEvents(
        Window mainWindow, TurnManager turnManager, Player player,
        GameLogView gameLog, ColoredLogView coloredLog, MapView mapView,
        MinimapView minimapView, Label mapTitleLabel, Label minimapTitleLabel,
        int[] saveFlash, int saveSlot, Action refreshHud)
    {
        void InvokeDialog(Action action, bool refresh = true) =>
            Application.Invoke(() => { action(); if (refresh) refreshHud(); });

        turnManager.PlayerDied += () =>
        {
            if (turnManager.IsHardcore) SaveManager.DeleteSave(saveSlot);
            InvokeDialog(() => DeathScreen.Show(
                mainWindow, player, turnManager.CurrentFloor,
                turnManager.KillCount, turnManager.TurnCount,
                turnManager.LastKillerName, turnManager.IsHardcore, turnManager, coloredLog), false);
        };

        turnManager.GameWon += () =>
        {
            // Permanent unlock: Run Modifiers (FB-564) + future post-clear features.
            SAOTRPG.Systems.Story.ProfileData.MarkGameCompleted();
            InvokeDialog(() => VictoryScreen.Show(
                mainWindow, player, turnManager.KillCount, turnManager.TurnCount, turnManager), false);
        };

        turnManager.AnvilInteraction += () =>
        {
            InvokeDialog(() => { CraftingDialog.Show(player, turnManager.CurrentFloor); refreshHud(); });
        };

        turnManager.CookingInteraction += () =>
        {
            InvokeDialog(() => { CookingScreen.Show(player, gameLog); refreshHud(); });
        };

        turnManager.VendorInteraction += (vendor) =>
        {
            InvokeDialog(() =>
            {
                ShopDialog.Show(player, vendor, turnManager.CurrentFloor);
                string[] farewells =
                {
                    "{0}: \"Come back anytime!\"", "{0}: \"Safe travels, adventurer.\"",
                    "{0}: \"Good luck out there!\"", "{0}: \"May your blade stay sharp.\"",
                    "{0}: \"See you next floor!\"",
                };
                gameLog.Log(string.Format(farewells[Random.Shared.Next(farewells.Length)], vendor.Name));
            });
        };

        turnManager.NpcDialogRequested += (npc) =>
            InvokeDialog(() => NpcDialogDialog.Show(npc), false);

        turnManager.TalentPickRequested += (perks) =>
            InvokeDialog(() =>
            {
                var picked = TalentPickDialog.Show(perks);
                if (picked != null)
                {
                    turnManager.ApplyTalent(picked);
                    gameLog.Log($"  Talent acquired: {picked.Name} — {picked.Description}");
                }
            });

        turnManager.StairsConfirmRequested += () =>
        {
            InvokeDialog(() =>
            {
                int nextFloor = turnManager.CurrentFloor + 1;
                bool ascend = FloorTransitionOverlay.Show(nextFloor, turnManager.LastFloorRecap, player.Level);
                if (ascend) turnManager.AscendFloor();
            });
        };

        turnManager.FloorChanged += (floor) =>
        {
            mainWindow.Title = $"Aincrad TRPG — Floor {floor}";
            mapTitleLabel.Text = $"Floor {floor} — Aincrad";
            minimapTitleLabel.Text = $"Minimap — F{floor}";
            mapTitleLabel.ColorScheme = FloorThemeColor(floor);
            mapView.SetMap(turnManager.Map);
            minimapView.SetMap(turnManager.Map);
            gameLog.Log($"Welcome to Floor {floor}, {player.FirstName}.");
            if (WeatherSystem.Current != WeatherType.Clear)
            {
                gameLog.Log($"Weather: {WeatherSystem.GetLabel()}");
                string weatherFlavor = WeatherSystem.GetFlavorDescription();
                if (weatherFlavor.Length > 0) gameLog.Log($"  {weatherFlavor}");
            }
            gameLog.Log("");
            saveFlash[0] = 5;
        };

        turnManager.MapSwapped += () =>
        {
            mapView.SetMap(turnManager.Map);
            minimapView.SetMap(turnManager.Map);
            string label = turnManager.InLabyrinth
                ? $"Labyrinth — F{turnManager.CurrentFloor}"
                : $"Floor {turnManager.CurrentFloor} — Aincrad";
            mapTitleLabel.Text = label;
            minimapTitleLabel.Text = turnManager.InLabyrinth
                ? $"Labyrinth — F{turnManager.CurrentFloor}"
                : $"Minimap — F{turnManager.CurrentFloor}";
            refreshHud();
        };
    }

    private static void WireKeybindActions(
        TurnManager turnManager, MapView mapView, MinimapView minimapView,
        Player player, GameLogView gameLog, ColoredLogView coloredLog,
        Button inventoryBtn, int saveSlot, int[] saveFlash, Action refreshHud)
    {
        mapView.PlayerMoveRequested += (dx, dy) => { turnManager.ProcessPlayerMove(dx, dy); refreshHud(); };
        turnManager.TurnCompleted += refreshHud;

        mapView.LookRequested += () =>
        {
            if (mapView.IsLookModeActive) { mapView.ExitLookMode(); return; }
            var targets = turnManager.GetVisibleMonsters();
            if (targets.Count == 0) { gameLog.Log("No enemies in sight."); return; }
            mapView.EnterLookMode(targets);
        };

        mapView.WaitRequested += () => { turnManager.ProcessPlayerMove(0, 0); refreshHud(); };
        inventoryBtn.Accepting += (s, e) => { InventoryDialog.Show(player, turnManager.CurrentFloor); refreshHud(); e.Cancel = true; };
        mapView.InventoryRequested += () => { InventoryDialog.Show(player, turnManager.CurrentFloor); refreshHud(); };
        mapView.StatsRequested += () => { StatsDialog.Show(player, turnManager); refreshHud(); };
        mapView.HelpRequested += () => HelpDialog.Show();
        mapView.PlayerGuideRequested += () => PlayerGuideDialog.Show();
        mapView.KillStatsRequested += () => { KillStatsDialog.Show(player, turnManager); };
        mapView.EquipmentRequested += () => { EquipmentDialog.Show(player); refreshHud(); };
        mapView.PickupRequested += () => { turnManager.PickupItems(); refreshHud(); };
        mapView.RestRequested += () => { turnManager.ProcessRest(); refreshHud(); };
        mapView.CounterRequested += () => { turnManager.EnterCounterStance(); refreshHud(); };
        mapView.SprintRequested += (dx, dy) => { turnManager.ProcessSprint(dx, dy); refreshHud(); };
        mapView.StealthMoveRequested += (dx, dy) => { turnManager.ProcessStealthMove(dx, dy); refreshHud(); };

        mapView.SwordSkillRequested += (slot) => { turnManager.ExecuteSwordSkill(slot); refreshHud(); };
        mapView.SwordSkillMenuRequested += () => { SwordSkillDialog.Show(turnManager); refreshHud(); };
        mapView.QuestLogRequested += () => { QuestLogDialog.Show(player); };

        mapView.SaveRequested += () =>
        {
            if (SaveManager.SaveGame(player, turnManager, saveSlot))
            { gameLog.LogSystem("[Game saved]"); saveFlash[0] = 5; }
            else gameLog.LogSystem("[Save failed!]");
        };

        mapView.QuickUseRequested += (slot) =>
        {
            string targetName = slot switch
            {
                1 => "Health Potion", 2 => "Greater Health Potion",
                3 => "Antidote", 4 => "Battle Elixir", 5 => "Escape Rope", _ => ""
            };
            var item = player.Inventory.Items
                .OfType<SAOTRPG.Items.Consumables.Consumable>()
                .FirstOrDefault(c => c.Name == targetName && c.Quantity > 0);
            if (item != null) { player.UseItem(item); gameLog.Log($"Used {item.Name}."); refreshHud(); }
            else gameLog.Log($"No {targetName} in inventory.");
        };

        mapView.LogScrollUpRequested += () => coloredLog.ScrollPageUp();
        mapView.LogScrollDownRequested += () => coloredLog.ScrollPageDown();

        mapView.AutoExploreRequested += () =>
        {
            Application.AddTimeout(TimeSpan.FromMilliseconds(AutoExploreStepMs), () =>
            {
                if (player.IsDefeated) return false;
                bool moved = turnManager.AutoExploreStep();
                refreshHud();
                minimapView.SetNeedsDraw();
                return moved;
            });
        };
    }

    private static void WirePassiveSystems(
        TurnManager turnManager, MapView mapView, Player player, GameLogView gameLog)
    {
        turnManager.TurnCompleted += () =>
        {
            var tile = turnManager.Map.GetTile(player.X, player.Y);
            if (tile.HasItems)
                gameLog.Log($"You see {tile.Items.Count} item(s) on the ground. Press G to pick up.");

            if (tile.Type == SAOTRPG.Map.TileType.Door)
            {
                mapView.MarkDoorOpened(player.X, player.Y);
                turnManager.Map.OpenedDoors.Add((player.X, player.Y));
            }

            if (turnManager.IsPoisoned && turnManager.IsBleeding) mapView.SetStatusTint(Color.Magenta);
            else if (turnManager.IsPoisoned) mapView.SetStatusTint(Color.Green);
            else if (turnManager.IsBleeding) mapView.SetStatusTint(Color.Red);
            else mapView.SetStatusTint(null);

            if (tile.Type == SAOTRPG.Map.TileType.Lava)
                mapView.AddScorchMark(player.X, player.Y);

            foreach (var entity in turnManager.Map.Entities)
            {
                if (entity is Monster m && !m.IsDefeated)
                    mapView.TrackMobPosition(m.Id, m.X, m.Y);
            }

            var ambientSound = MapEffects.GetAmbientSoundText(turnManager.Map, player.X, player.Y);
            if (ambientSound != null) gameLog.Log(ambientSound);
        };

        turnManager.MonsterKilled += (x, y) =>
        {
            foreach (var entity in turnManager.Map.Entities)
            {
                if (entity is Monster m && m.IsDefeated && m.X == x && m.Y == y)
                { mapView.ClearMobTrail(m.Id); break; }
            }
        };

        Application.AddTimeout(TimeSpan.FromMilliseconds(AnimationIntervalMs), () =>
        {
            mapView.SetNeedsDraw();
            return true;
        });
    }
}
