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
            if (isCrit) mapView.FlashBorder(isPlayer ? Color.BrightRed : Color.BrightCyan, 132);
            else if (isPlayer) mapView.FlashBorder(Color.Red, 66);
            // Red-BG flash on the tile that took damage.
            mapView.AddHitFlash(x, y);
            // Player crits trigger a single-frame screen-wide brightness boost.
            if (isCrit && !isPlayer) mapView.TriggerCritScreenFlash();

            // Subtle tween popup (research §1): dim element tint, crit = bright+◇.
            int maxHp = ResolveMaxHpAt(turnManager, turnManager.Player, x, y);
            Color tint = ResolveDamageTint(turnManager, turnManager.Player, isPlayer);
            mapView.EnqueueDamagePopup(x, y, dmg, isCrit, tint, maxHp);
        };
        turnManager.WeaponSwing += (fx, fy, tx, ty, c) => mapView.AddWeaponSwing(fx, fy, tx, ty, c);
        turnManager.CombatTextEvent += (x, y, text, color) => mapView.AddTextFlash(x, y, text, color);
        turnManager.LeveledUp += () =>
        {
            mapView.TriggerLevelUpFlash();
            mapView.FlashBorder(Color.BrightYellow, 200);
            PartySystem.ScaleToPlayer(turnManager.Player.Level);
            turnManager.RequestTalentPick();
            ToastQueue.EnqueueLevelUp(turnManager.Player.Level);
            // FB-450 rising column of sparkles from player tile.
            ParticleQueue.Emit(ParticleEvent.LevelUp, turnManager.Player.X, turnManager.Player.Y);
        };
        turnManager.MonsterKilled += (x, y) =>
        {
            mapView.AddCorpseMarker(x, y);
            mapView.AddShatterParticle(x, y);
            mapView.FlashBorder(Color.BrightCyan, 66); // SAO blue flash on kill
            if (turnManager.KillStreak >= 2)
                mapView.TriggerKillStreakFlash(turnManager.KillStreak);
        };
        // Floor-boss cleared toast — the TurnManager side logs the banner,
        // we mirror it into the toast channel for the center-screen readout.
        turnManager.FloorBossCleared += (name) =>
            ToastQueue.EnqueueFloorBoss(name);
        // Species first-kill toast. Bestiary is a static — the TurnManager
        // event fires post-RecordKill so we can read TimesKilled==1.
        turnManager.SpeciesFirstKilled += (name) =>
            ToastQueue.EnqueueBestiaryFirst(name);
        // ◈ Divine weapon obtained — fires exactly once per run. Non-modal
        // banner + gold border flash + crit screen flash + toast readout.
        turnManager.DivineObtained += (weapon) =>
        {
            DivineObtainBanner.Trigger(weapon);
            mapView.FlashBorder(Color.BrightYellow, 264);
            mapView.TriggerCritScreenFlash();
            string display = string.IsNullOrWhiteSpace(weapon.EnhancedName) ? (weapon.Name ?? "Divine Weapon") : weapon.EnhancedName;
            ToastQueue.Enqueue($"◈ DIVINE: {display} ◈", Color.BrightYellow, ToastCategory.FloorBossCleared);
        };
        // Selka awakening dialog — modal opens when player carries Divine and engages
        // Selka on F65 (post-base-quest, not mid-chain).
        turnManager.DivineAwakeningRequested += (player) => DivineAwakeningDialog.Show(player);
        turnManager.SkillActivated += (x, y, color) =>
        {
            mapView.AddSkillFlash(x, y, color);
            mapView.FlashBorder(color, 33);
            // FB-450 skill-cast ring of particles at the caster tile.
            ParticleQueue.Emit(ParticleEvent.SkillCastStart, x, y, tint: color);
        };
        // FB-453 shake + FB-454 projectiles/status motes.
        turnManager.MapViewShakeRequested += tier => mapView.RequestShake(tier);
        turnManager.ProjectileRequested += (sx, sy, ex, ey, glyph, color, msPerCell, isArrow) =>
        {
            if (isArrow) mapView.EnqueueArrow(sx, sy, ex, ey, color);
            else mapView.EnqueueProjectile(sx, sy, ex, ey, glyph, color, msPerCell);
        };
        turnManager.StatusTrailRequested += (x, y, kind) =>
        {
            // SAO-theme bleed: ◇ shatter glyph in BrightCyan — NO red blood.
            switch (kind)
            {
                case "bleed":
                    mapView.EnqueueStatusTrail(x, y, new[] { '◇', '·', ' ' }, Color.BrightCyan); break;
                case "poison":
                    mapView.EnqueueStatusTrail(x, y, new[] { '░', '·', ' ' }, Color.BrightGreen); break;
                case "burn":
                    mapView.EnqueueStatusTrail(x, y, new[] { '¤', '*', ' ' }, Color.BrightRed); break;
            }
        };
        turnManager.MultiHitStreamRequested += (x, y, damage, hitIndex, delayMs) =>
            mapView.EnqueueMultiHitPopup(x, y, damage, hitIndex, delayMs);
        turnManager.MultiHitAggregateRequested += (x, y, hits, total, delayMs) =>
            mapView.EnqueueCascadeAggregate(x, y, hits, total, delayMs);
        turnManager.MultiHitDamageDealt += (x, y, dmg, isPlayer, isCrit) =>
        {
            // Multi-hit side effects only — NO EnqueueDamagePopup (cascade owns it).
            mapView.AddHitFlash(x, y);
            mapView.AddScorchMark(x, y);
            if (isCrit) mapView.FlashBorder(Color.BrightCyan, 132);
        };
    }

    private static void WireSystemEvents(
        Window mainWindow, TurnManager turnManager, Player player,
        GameLogView gameLog, ColoredLogView coloredLog, MapView mapView,
        MinimapView minimapView, Label mapTitleLabel, Label minimapTitleLabel,
        int[] saveFlash, int saveSlot, Action refreshHud)
    {
        void InvokeDialog(Action action, bool refresh = true) =>
            Application.Invoke(() =>
            {
                // Modal dialog opening: flush any in-flight damage popup so
                // the overlay layer doesn't leak past the dialog's z-order.
                mapView.ClearDamagePopups();
                action();
                if (refresh) refreshHud();
            });

        turnManager.PlayerDied += () =>
        {
            // Universal permadeath — delete the save slot before DeathScreen renders,
            // so it's already gone when the player dismisses the summary.
            SaveManager.DeleteSave(saveSlot);
            InvokeDialog(() => DeathScreen.Show(
                mainWindow, player, turnManager.CurrentFloor,
                turnManager.KillCount, turnManager.TurnCount,
                turnManager.LastKillerName, turnManager, coloredLog), false);
        };

        mapView.PauseRequested += () =>
        {
            mapView.ClearDamagePopups();
            InvokeDialog(() => Dialogs.PauseMenuDialog.Show(
                mainWindow, player, turnManager, saveSlot, gameLog, saveFlash), false);
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
                ShopDialog.Show(player, vendor, turnManager.CurrentFloor, gameLog);
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

        turnManager.LisbethInteraction += () =>
            InvokeDialog(() => LisbethCraftDialog.Show(player, gameLog));

        // FB-057 Monument of Swordsmen — opens kill log + title browser.
        turnManager.MonumentInteraction += () =>
            InvokeDialog(() => { MonumentDialog.Show(player); refreshHud(); });

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

        // Proficiency fork — fires at weapon L25/50/75/100. Modal picks 1 of 2 passives;
        // Esc leaves it pending, resolvable later from StatsDialog.
        turnManager.ProficiencyForkRequested += (weaponType, forkLevel, opt1, opt2) =>
            InvokeDialog(() =>
            {
                int picked = ProficiencyForkDialog.Show(weaponType, forkLevel, opt1, opt2);
                if (picked == 1 || picked == 2)
                {
                    turnManager.ApplyProficiencyFork(weaponType, forkLevel, picked);
                    var chosen = picked == 1 ? opt1 : opt2;
                    gameLog.LogSystem(
                        $"  {weaponType} L{forkLevel} fork: {chosen.Name} — {chosen.Description}");
                }
                else
                {
                    gameLog.Log($"  (Fork for {weaponType} L{forkLevel} pending — resume from Stats.)");
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
            // Biome flavor toast on floor swap; suppressed in labyrinth runs.
            if (!turnManager.InLabyrinth)
            {
                var cfg = BiomeSystem.CurrentGenConfig;
                if (!string.IsNullOrEmpty(cfg?.FloorEntryText))
                    ToastQueue.Enqueue(cfg!.FloorEntryText!, Color.BrightCyan, ToastCategory.Info);
            }
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
        inventoryBtn.Accepting += (s, e) => { mapView.ClearDamagePopups(); InventoryDialog.Show(player, turnManager.CurrentFloor); refreshHud(); e.Cancel = true; };
        mapView.InventoryRequested += () => { mapView.ClearDamagePopups(); InventoryDialog.Show(player, turnManager.CurrentFloor); refreshHud(); };
        mapView.StatsRequested += () => { mapView.ClearDamagePopups(); StatsDialog.Show(player, turnManager); refreshHud(); };
        mapView.HelpRequested += () => { mapView.ClearDamagePopups(); HelpDialog.Show(); };
        mapView.PlayerGuideRequested += () => { mapView.ClearDamagePopups(); PlayerGuideDialog.Show(turnManager, player); };
        // Bundle 13 (Item 1) — Shift+L opens the Legendary Collectables panel.
        mapView.LegendaryCollectablesRequested += () =>
        {
            mapView.ClearDamagePopups();
            LegendaryCollectablesDialog.Show(turnManager.CurrentFloor);
        };
        mapView.KillStatsRequested += () => { mapView.ClearDamagePopups(); KillStatsDialog.Show(player, turnManager); };
        mapView.BestiaryRequested += () => { mapView.ClearDamagePopups(); BestiaryDialog.Show(player, turnManager); };
        mapView.EquipmentRequested += () => { mapView.ClearDamagePopups(); EquipmentDialog.Show(player); refreshHud(); };
        mapView.PickupRequested += () => { turnManager.PickupItems(); refreshHud(); };
        mapView.RestRequested += () => { turnManager.ProcessRest(); refreshHud(); };
        mapView.CounterRequested += () => { turnManager.EnterCounterStance(); refreshHud(); };
        mapView.SprintRequested += (dx, dy) => { turnManager.ProcessSprint(dx, dy); refreshHud(); };
        mapView.StealthMoveRequested += (dx, dy) => { turnManager.ProcessStealthMove(dx, dy); refreshHud(); };

        // Bundle 13 Item 6 — sword skill activation. Range>1 skills route through the reticle
        // first; Range=1 (melee bump-skills) keep the legacy nearest-target path. Eligibility
        // (stun/post-motion/cooldown/weapon) is rechecked inside ExecuteSwordSkill on confirm,
        // so we only gate reticle entry on the cheap structural facts.
        mapView.SwordSkillRequested += (slot) =>
        {
            if (slot >= 0 && slot < turnManager.EquippedSkills.Length
                && !mapView.IsRangedFireModeActive)
            {
                var skill = turnManager.EquippedSkills[slot];
                var wpnNow = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon)
                                as SAOTRPG.Items.Equipment.Weapon;
                string wtypeNow = wpnNow?.WeaponType ?? "Unarmed";
                int bowOver = wpnNow?.WeaponType == "Bow" ? player.BowRangeOverflow : 0;
                bool weaponMatches = skill != null
                    && (skill.WeaponType == wtypeNow || skill.WeaponType == "Any");
                if (skill != null && weaponMatches
                    && skill.Range > 1 && skill.Type != SkillType.AoE
                    && skill.Type != SkillType.Counter)
                {
                    mapView.EnterSkillReticle(slot, skill.Range + bowOver);
                    return;
                }
            }
            turnManager.ExecuteSwordSkill(slot);
            refreshHud();
        };
        mapView.SwordSkillMenuRequested += () => { SwordSkillDialog.Show(turnManager); refreshHud(); };
        mapView.QuestLogRequested += () => { QuestLogDialog.Show(player); };

        // Bundle 13 Item 6 — `\` opens the Bow basic-attack reticle when a Bow is equipped.
        // Anything else (melee weapon, unarmed, already-aiming) silently no-ops with a hint line.
        mapView.RangedFireKeyPressed += () =>
        {
            if (mapView.IsRangedFireModeActive) { mapView.ExitRangedFireMode(); return; }
            var wpn = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon)
                        as SAOTRPG.Items.Equipment.Weapon;
            if (wpn?.WeaponType != "Bow")
            {
                gameLog.Log("You need a bow equipped to fire at range.");
                return;
            }
            int range = Math.Max(1, wpn.Range + player.BowRangeOverflow);
            mapView.EnterBowReticle(range);
        };
        // Reticle confirm — Bow basic-attack at the picked tile. ExecuteBowShot validates
        // range/visibility/target again as a defensive guard against stale UI state.
        mapView.RangedFireRequested += (tx, ty) =>
        {
            turnManager.ExecuteBowShot(tx, ty);
            refreshHud();
        };
        // Reticle confirm — Range>1 sword skill. Stash the override on the engine so
        // FindSkillTargets uses the picked tile instead of the nearest enemy.
        mapView.RangedSkillFireRequested += (slot, tx, ty) =>
        {
            turnManager.SkillTargetOverride = (tx, ty);
            turnManager.ExecuteSwordSkill(slot);
            refreshHud();
        };

        mapView.SaveRequested += () =>
        {
            if (SaveManager.SaveGame(player, turnManager, saveSlot))
            {
                // Persist bestiary knowledge alongside the run save so it
                // survives unexpected crashes without requiring a death.
                Bestiary.SaveToLifetimeStats();
                gameLog.LogSystem("[Game saved]"); saveFlash[0] = 5;
            }
            else gameLog.LogSystem("[Save failed!]");
        };

        mapView.QuickUseRequested += (slotKey) =>
        {
            // slotKey is the pressed digit (1..9 then 0→10). Quickbar indices 0-9.
            int slotIdx = slotKey - 1;
            var item = player.Quickbar.ResolveItem(slotIdx, player);
            if (item != null)
            {
                player.UseItem(item);
                gameLog.Log($"Used {item.Name}.");
                refreshHud();
            }
            else
            {
                string bound = slotIdx >= 0 && slotIdx < SAOTRPG.Systems.QuickbarState.SlotCount
                    ? player.Quickbar.SlotItemDefIds[slotIdx] ?? "" : "";
                if (!string.IsNullOrEmpty(bound))
                    gameLog.Log($"Slot {slotKey} bind has no matching item in inventory.");
                else
                    gameLog.Log($"Slot {slotKey} is empty. Bind a consumable with Shift+{(slotKey == 10 ? "0" : slotKey.ToString())} in Inventory.");
            }
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
            if (turnManager.Map.HasItemsAt(player.X, player.Y))
                gameLog.Log($"You see {turnManager.Map.GetItemCountAt(player.X, player.Y)} item(s) on the ground. Press G to pick up.");

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

        // Fast 50ms ticker redraws while any real-time animation is active (Wave 1).
        // Gate now covers tile animations, particles, popups, toasts, flashes, shake.
        Application.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
        {
            if (mapView.HasActiveRealtimeAnimations())
                mapView.SetNeedsDraw();
            return true;
        });
    }

    // Finds the max-HP of whatever entity currently stands on (x,y) so the
    // popup helper can apply the 2% chip-damage suppression rule. Falls back
    // to player max HP when no monster occupies the tile (self-damage).
    private static int ResolveMaxHpAt(TurnManager tm, Player player, int x, int y)
    {
        if (x == player.X && y == player.Y) return player.MaxHealth;
        foreach (var entity in tm.Map.Entities)
        {
            if (entity is Entities.Monster m && !m.IsDefeated && m.X == x && m.Y == y)
                return m.MaxHealth;
        }
        return 0;
    }

    // Resolves dim element tint for the popup. Player-taking-damage paints
    // white; player-dealing-damage uses weapon SpecialEffect -> element map.
    private static Color ResolveDamageTint(TurnManager tm, Player player, bool isPlayerTookDamage)
    {
        if (isPlayerTookDamage) return Color.White;
        var wpn = player.Inventory.GetEquipped(
            SAOTRPG.Inventory.Core.EquipmentSlot.Weapon)
            as SAOTRPG.Items.Equipment.Weapon;
        return MapView.ResolveElementalTint(wpn?.SpecialEffect);
    }
}
