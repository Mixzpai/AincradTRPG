using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using SAOTRPG.UI.Dialogs;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

/// <summary>
/// Main gameplay screen — assembles the map, minimap, stats panel, message log,
/// action bar, and wires all player/turn events together.
///
/// Layout (approximate):
///   ┌──────────────── 70% ────────────────┬───── 30% ─────┐
///   │                                     │   Minimap      │
///   │            Map View                 ├────────────────┤
///   │                                     │  Player Stats  │
///   │                                     ├────────────────┤
///   │                                     │   Messages     │
///   │                                     │  (colored log) │
///   ├─────────────────────────────────────┴────────────────┤
///   │  [Inventory]   HP [||||....] 90/100  XP [===---] Lv2 │
///   └─────────────────────────────────────────────────────-┘
/// </summary>
public static class GameScreen
{
    // ── Timing constants ─────────────────────────────────────────────
    private const int AnimationIntervalMs = 750;    // Water/flower shimmer
    private const int AutoExploreStepMs   = 80;     // Delay between auto-explore steps

    // ── HUD bar widths ───────────────────────────────────────────────
    private const int HpBarWidth = 16;
    private const int XpBarWidth = 10;

    public static void Show(Window mainWindow, Player player, int difficulty = 3, bool hardcore = false,
        SaveData? saveData = null)
    {
        mainWindow.RemoveAll();
        var sw = DebugLogger.StartTimer("GameScreen.Show");
        DebugLogger.LogScreen("GameScreen");

        int startFloor = saveData?.CurrentFloor ?? 1;

        // ══════════════════════════════════════════════════════════════
        //  MAP GENERATION
        // ══════════════════════════════════════════════════════════════

        var (map, rooms) = MapGenerator.GenerateFloor(startFloor);

        // ══════════════════════════════════════════════════════════════
        //  RIGHT PANEL — Minimap, Stats, Messages
        // ══════════════════════════════════════════════════════════════

        // ── Minimap ──────────────────────────────────────────────────
        var minimapFrame = new FrameView
        {
            Title = $"Minimap — F{startFloor}",
            X = Pos.Percent(70), Y = 0,
            Width = Dim.Fill(), Height = 14
        };
        var minimapView = new MinimapView(map, player)
        {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        minimapFrame.Add(minimapView);

        // ── Player stats panel ───────────────────────────────────────
        var (playerStatsFrame, playerStatsText) = CreatePanel("Player Stats",
            Pos.Percent(70), 14, Dim.Fill(), Dim.Percent(25));
        playerStatsText.Text = player.GetStatsDisplay();

        // ── Message log (colored) ────────────────────────────────────
        var messageFrame = new FrameView
        {
            Title = "Messages",
            X = Pos.Percent(70), Y = Pos.Bottom(playerStatsFrame),
            Width = Dim.Fill(), Height = Dim.Fill(3)
        };
        var coloredLog = new ColoredLogView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        messageFrame.Add(coloredLog);

        // ══════════════════════════════════════════════════════════════
        //  GAME LOG & ENTITY SETUP
        // ══════════════════════════════════════════════════════════════

        var gameLog = new GameLogView(coloredLog);
        player.SetLog(gameLog);
        player.Inventory.SetLogger(new TerminalGuiInventoryLogger(gameLog));

        int activeDifficulty = saveData?.Difficulty ?? difficulty;
        MapGenerator.PopulateFloor(map, rooms, player, startFloor,
            activeDifficulty >= 0 && activeDifficulty <= 8 ? new[] { 40, 60, 80, 100, 130, 170, 220, 300, 50 }[activeDifficulty] : 100);

        foreach (var entity in map.Entities)
            entity.SetLog(gameLog);

        // ══════════════════════════════════════════════════════════════
        //  LEFT PANEL — Map View
        // ══════════════════════════════════════════════════════════════

        var camera = new Camera();
        var mapFrame = new FrameView
        {
            Title = $"Floor {startFloor} — Aincrad",
            X = 0, Y = 0,
            Width = Dim.Percent(70),
            Height = Dim.Fill(3)
        };
        var mapView = new MapView(map, camera, player)
        {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        mapFrame.Add(mapView);

        // ══════════════════════════════════════════════════════════════
        //  ACTION BAR (bottom strip)
        // ══════════════════════════════════════════════════════════════

        var actionBar = new FrameView
        {
            Title = "Actions",
            X = 0, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(), Height = 3
        };
        var inventoryBtn = new Button
        {
            Text = " Inventory ",
            X = 1, Y = 0,
            ColorScheme = ColorSchemes.Button
        };
        var hpLabel   = new Label { Text = "", X = Pos.Right(inventoryBtn) + 2, Y = 0 };
        var infoLabel = new Label { Text = "", X = Pos.Right(hpLabel) + 2,      Y = 0 };
        actionBar.Add(inventoryBtn, hpLabel, infoLabel);

        // ══════════════════════════════════════════════════════════════
        //  TURN MANAGER
        // ══════════════════════════════════════════════════════════════

        var turnManager = saveData != null
            ? TurnManager.LoadFromSave(saveData, map, player, gameLog)
            : new TurnManager(map, player, gameLog, startFloor, difficulty, hardcore);

        // ── Auto-save indicator — flash "[Saved]" for a few turns after floor change ──
        int saveFlashTurns = 0;

        // ══════════════════════════════════════════════════════════════
        //  HUD REFRESH
        // ══════════════════════════════════════════════════════════════

        void RefreshHud()
        {
            // Stats panel with XP progress bar
            string xpProgress = BarBuilder.BuildXp(player.CurrentExperience, player.ExperienceRequired, 14);
            playerStatsText.Text = player.GetStatsDisplay()
                + $"\n\n── Progress ──\nXP {xpProgress} {player.CurrentExperience}/{player.ExperienceRequired}";

            // Action bar — HP/XP bars + floor info
            string hpBar = BarBuilder.BuildHp(player.CurrentHealth, player.MaxHealth, HpBarWidth);
            string xpBar = BarBuilder.BuildXp(player.CurrentExperience, player.ExperienceRequired, XpBarWidth);
            // Durability warnings — alert when any equipped item is below 10
            string durWarnings = "";
            foreach (SAOTRPG.Inventory.Core.EquipmentSlot dslot in Enum.GetValues(typeof(SAOTRPG.Inventory.Core.EquipmentSlot)))
            {
                var eq = player.Inventory.GetEquipped(dslot);
                if (eq != null && eq.ItemDurability < 10)
                {
                    string tag = dslot switch
                    {
                        SAOTRPG.Inventory.Core.EquipmentSlot.Weapon  => "WPN",
                        SAOTRPG.Inventory.Core.EquipmentSlot.Chest   => "ARM",
                        SAOTRPG.Inventory.Core.EquipmentSlot.Head    => "HLM",
                        SAOTRPG.Inventory.Core.EquipmentSlot.OffHand => "SLD",
                        SAOTRPG.Inventory.Core.EquipmentSlot.Feet    => "BTS",
                        _                                            => dslot.ToString()[..3].ToUpper()
                    };
                    durWarnings += $"  [!{tag}:{eq.ItemDurability}]";
                }
            }

            // Status effect tags — short colored-style tags for active effects
            string statusEffects = StatusTagBuilder.Build(turnManager, durWarnings);
            string satBar = BarBuilder.Build(turnManager.Satiety, TurnManager.MaxSatiety, 6, '█', '░');
            // Auto-save indicator
            string saveTag = saveFlashTurns > 0 ? "  [Saved]" : "";
            if (saveFlashTurns > 0) saveFlashTurns--;

            // Weather indicator
            string weatherTag = WeatherSystem.Current != WeatherType.Clear
                ? $"  [{WeatherSystem.GetLabel()}]" : "";

            hpLabel.Text = $"HP {hpBar} {player.CurrentHealth}/{player.MaxHealth}" +
                           $"  XP {xpBar} Lv{player.Level}" +
                           $"  SAT {satBar}" +
                           statusEffects +
                           $"  |  Fl:{turnManager.CurrentFloor} K:{turnManager.KillCount} T:{turnManager.TurnCount}" +
                           $"  ¢{player.ColOnHand}" +
                           weatherTag +
                           saveTag;

            // Update inventory button with item count
            inventoryBtn.Text = $" Inventory ({player.Inventory.Items.Count}) ";

            // Update minimap with exploration percentage
            int explored = turnManager.Map.GetExplorationPercent();
            minimapFrame.Title = $"Minimap — F{turnManager.CurrentFloor} ({explored}%)";

            // Low HP warning — turn bar red when below 25%
            bool lowHp = player.CurrentHealth <= player.MaxHealth / 4;
            hpLabel.ColorScheme = lowHp ? ColorSchemes.Danger : ColorSchemes.Body;

            // Equipped weapon + monster count + underfoot info
            var wpn = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon);
            string wpnInfo = wpn != null ? $"{wpn.Name} ({wpn.ItemDurability})" : "Fists";
            int mobsLeft = turnManager.GetMonsterCount();
            string underfoot = turnManager.GetTileInfo(player.X, player.Y);
            // Danger compass — only on Story/Very Easy/Easy (difficulty 0-2)
            string compass = difficulty <= 2 ? turnManager.GetDangerCompass() : "";
            string compassTag = compass.Length > 0 ? $"  {compass}" : "";
            string ctxHint = turnManager.GetContextHint();
            string hintTag = ctxHint.Length > 0 ? $"  {ctxHint}" : "";
            infoLabel.Text = $"[{wpnInfo}] Mobs:{mobsLeft} T:{turnManager.FloorTurns}{compassTag} ({player.X},{player.Y}) {underfoot}{hintTag}";

            mapView.SetNeedsDraw();
        }

        // ══════════════════════════════════════════════════════════════
        //  EVENT WIRING — Visual Effects
        // ══════════════════════════════════════════════════════════════

        // Damage flash — show numbers at combat locations + scorch marks for trap damage
        turnManager.DamageDealt += (x, y, dmg, isPlayer) =>
        {
            mapView.AddDamageFlash(x, y, dmg, isPlayer);
            // Add scorch mark when player takes trap/environmental damage
            if (isPlayer)
                mapView.AddScorchMark(x, y);
        };

        // Corpse markers — '†' where monsters died + shatter particles (SAO polygon burst)
        turnManager.MonsterKilled += (x, y) =>
        {
            mapView.AddCorpseMarker(x, y);
            mapView.AddShatterParticle(x, y);

            // Kill streak visual flash — magenta @ on Double Kill+, ★ burst on Triple Kill+
            if (turnManager.KillStreak >= 2)
                mapView.TriggerKillStreakFlash(turnManager.KillStreak);
        };

        // ══════════════════════════════════════════════════════════════
        //  EVENT WIRING — Player Actions
        // ══════════════════════════════════════════════════════════════

        // Movement (WASD/arrows/QEZC)
        mapView.PlayerMoveRequested += (dx, dy) =>
        {
            turnManager.ProcessPlayerMove(dx, dy);
            RefreshHud();
        };

        // Turn completed — always refresh HUD
        turnManager.TurnCompleted += RefreshHud;

        // Floor transition — update map references, title, and theme color
        turnManager.FloorChanged += (floor) =>
        {
            mapFrame.Title = $"Floor {floor} — Aincrad";
            minimapFrame.Title = $"Minimap — F{floor}";

            // Floor theme color — visual progression indicator
            var floorScheme = FloorThemeColor(floor);
            mapFrame.ColorScheme = floorScheme;

            mapView.SetMap(turnManager.Map);
            minimapView.SetMap(turnManager.Map);
            gameLog.Log($"Welcome to Floor {floor}, {player.FirstName}.");
            gameLog.Log("");
            saveFlashTurns = 5;  // Show [Saved] for 5 turns after floor change
        };

        // ══════════════════════════════════════════════════════════════
        //  EVENT WIRING — System Events
        // ══════════════════════════════════════════════════════════════

        // Player death → show death screen with run stats + combat log recap
        turnManager.PlayerDied += () =>
        {
            // Hardcore permadeath — delete save on death
            if (turnManager.IsHardcore)
                SaveManager.DeleteSave();

            Application.Invoke(() => DeathScreen.Show(
                mainWindow, player, turnManager.CurrentFloor,
                turnManager.KillCount, turnManager.TurnCount,
                turnManager.LastKillerName, turnManager.IsHardcore, turnManager, coloredLog));
        };

        // Victory — cleared floor 100 → show victory screen
        turnManager.GameWon += () =>
        {
            Application.Invoke(() => VictoryScreen.Show(
                mainWindow, player,
                turnManager.KillCount, turnManager.TurnCount));
        };

        // Vendor bump → open shop dialog + farewell
        turnManager.VendorInteraction += (vendor) =>
        {
            Application.Invoke(() =>
            {
                ShopDialog.Show(player, vendor, turnManager.CurrentFloor);
                // Add new farewell lines by adding a string (use {0} for vendor name)
                string[] farewells =
                {
                    "{0}: \"Come back anytime!\"",
                    "{0}: \"Safe travels, adventurer.\"",
                    "{0}: \"Good luck out there!\"",
                    "{0}: \"May your blade stay sharp.\"",
                    "{0}: \"See you next floor!\"",
                };
                gameLog.Log(string.Format(
                    farewells[Random.Shared.Next(farewells.Length)], vendor.Name));
                RefreshHud();
            });
        };

        // Stairs → confirm before ascending
        turnManager.StairsConfirmRequested += () =>
        {
            Application.Invoke(() =>
            {
                int result = MessageBox.Query("Ascend",
                    $"Ascend to Floor {turnManager.CurrentFloor + 1}?", "Yes", "No");
                if (result == 0)
                {
                    turnManager.AscendFloor();
                    RefreshHud();
                }
            });
        };

        // ══════════════════════════════════════════════════════════════
        //  EVENT WIRING — Keybind Actions
        // ══════════════════════════════════════════════════════════════

        // Look (L) — report surroundings
        mapView.LookRequested += () =>
        {
            gameLog.Log("── Look around ──");
            (int dx, int dy, string dir)[] dirs =
            {
                (0, -1, "North"), (0, 1, "South"), (-1, 0, "West"), (1, 0, "East")
            };
            foreach (var (dx, dy, dir) in dirs)
            {
                string info = turnManager.GetTileInfo(player.X + dx, player.Y + dy);
                gameLog.Log($"  {dir}: {info}");
            }
            gameLog.Log($"  Here: {turnManager.GetTileInfo(player.X, player.Y)}");

            // Equipment durability snapshot
            var durParts = new List<string>();
            foreach (SAOTRPG.Inventory.Core.EquipmentSlot eslot in Enum.GetValues(typeof(SAOTRPG.Inventory.Core.EquipmentSlot)))
            {
                var eq = player.Inventory.GetEquipped(eslot);
                if (eq == null) continue;
                string tag = eslot switch
                {
                    SAOTRPG.Inventory.Core.EquipmentSlot.Weapon => "Wpn",
                    SAOTRPG.Inventory.Core.EquipmentSlot.Head   => "Hd",
                    SAOTRPG.Inventory.Core.EquipmentSlot.Chest  => "Ch",
                    SAOTRPG.Inventory.Core.EquipmentSlot.Legs   => "Lg",
                    SAOTRPG.Inventory.Core.EquipmentSlot.Feet   => "Ft",
                    _ => eslot.ToString()[..2]
                };
                durParts.Add($"{tag}:{eq.ItemDurability}");
            }
            if (durParts.Count > 0)
                gameLog.Log($"  Gear: {string.Join("  ", durParts)}");
        };

        // Wait (Space) — skip turn
        mapView.WaitRequested += () =>
        {
            turnManager.ProcessPlayerMove(0, 0);
            RefreshHud();
        };

        // Inventory (I key or button)
        inventoryBtn.Accepting += (s, e) => { InventoryDialog.Show(player); RefreshHud(); e.Cancel = true; };
        mapView.InventoryRequested += () => { InventoryDialog.Show(player); RefreshHud(); };

        // Stats (P key)
        mapView.StatsRequested += () => { StatsDialog.Show(player, turnManager); RefreshHud(); };

        // Help (H key)
        mapView.HelpRequested += () => HelpDialog.Show();

        // Kill Stats (K key)
        mapView.KillStatsRequested += () => { KillStatsDialog.Show(player, turnManager); };

        // Equipment (T key)
        mapView.EquipmentRequested += () => { EquipmentDialog.Show(player); RefreshHud(); };

        // Pickup (G key)
        mapView.PickupRequested += () => { turnManager.PickupItems(); RefreshHud(); };

        // Rest (R key) — skip 3 turns, heal 3 HP/turn
        mapView.RestRequested += () => { turnManager.ProcessRest(); RefreshHud(); };

        // Sprint (Shift+direction) — move 2 tiles in one turn
        mapView.SprintRequested += (dx, dy) => { turnManager.ProcessSprint(dx, dy); RefreshHud(); };

        // Stealth (Ctrl+direction) — move silently, halves monster aggro range
        mapView.StealthMoveRequested += (dx, dy) => { turnManager.ProcessStealthMove(dx, dy); RefreshHud(); };

        // Quick save (F5)
        mapView.SaveRequested += () =>
        {
            if (SaveManager.SaveGame(player, turnManager))
            {
                gameLog.LogSystem("[Game saved]");
                saveFlashTurns = 5;
            }
            else
            {
                gameLog.LogSystem("[Save failed!]");
            }
        };

        // Log scrollback (PageUp/PageDown)
        mapView.LogScrollUpRequested += () => coloredLog.ScrollPageUp();
        mapView.LogScrollDownRequested += () => coloredLog.ScrollPageDown();

        // Auto-explore (X key) — step every 80ms until interrupted
        mapView.AutoExploreRequested += () =>
        {
            Application.AddTimeout(TimeSpan.FromMilliseconds(AutoExploreStepMs), () =>
            {
                if (player.IsDefeated) return false;
                bool moved = turnManager.AutoExploreStep();
                RefreshHud();
                minimapView.SetNeedsDraw();
                return moved;   // keep ticking while progressing
            });
        };

        // ══════════════════════════════════════════════════════════════
        //  PASSIVE SYSTEMS
        // ══════════════════════════════════════════════════════════════

        // Notify player of items underfoot after each turn + door tracking + status tint
        turnManager.TurnCompleted += () =>
        {
            var tile = turnManager.Map.GetTile(player.X, player.Y);
            if (tile.HasItems)
                gameLog.Log($"You see {tile.Items.Count} item(s) on the ground. Press G to pick up.");
            // Mark door as opened when player stands on it
            if (tile.Type == SAOTRPG.Map.TileType.Door)
            {
                mapView.MarkDoorOpened(player.X, player.Y);
                turnManager.Map.OpenedDoors.Add((player.X, player.Y));
            }

            // Status effect screen tint — poison=green, bleed=red, both=magenta
            if (turnManager.IsPoisoned && turnManager.IsBleeding)
                mapView.SetStatusTint(Color.Magenta);
            else if (turnManager.IsPoisoned)
                mapView.SetStatusTint(Color.Green);
            else if (turnManager.IsBleeding)
                mapView.SetStatusTint(Color.Red);
            else
                mapView.SetStatusTint(null);

            // Scorch marks on trap/lava tiles
            if (tile.Type == SAOTRPG.Map.TileType.Lava)
                mapView.AddScorchMark(player.X, player.Y);

            // Track mob positions for patrol trails
            foreach (var entity in turnManager.Map.Entities)
            {
                if (entity is Monster m && !m.IsDefeated)
                    mapView.TrackMobPosition(m.Id, m.X, m.Y);
            }

            // Ambient sound text — ~1% chance per turn, soft flavor near water/fire/wind
            var ambientSound = MapEffects.GetAmbientSoundText(turnManager.Map, player.X, player.Y);
            if (ambientSound != null)
                gameLog.Log(ambientSound);
        };

        // Clean up mob trail on kill
        turnManager.MonsterKilled += (x, y) =>
        {
            // Find the dead monster at this position to clear its trail
            foreach (var entity in turnManager.Map.Entities)
            {
                if (entity is Monster m && m.IsDefeated && m.X == x && m.Y == y)
                {
                    mapView.ClearMobTrail(m.Id);
                    break;
                }
            }
        };

        // Animation timer — periodic redraw for water/flower shimmer
        Application.AddTimeout(TimeSpan.FromMilliseconds(AnimationIntervalMs), () =>
        {
            mapView.SetNeedsDraw();
            return true;    // keep ticking forever
        });

        // ══════════════════════════════════════════════════════════════
        //  ASSEMBLE & LAUNCH
        // ══════════════════════════════════════════════════════════════

        mainWindow.Add(mapFrame, minimapFrame, playerStatsFrame, messageFrame, actionBar);
        mapView.SetFocus();

        // Welcome messages
        string[] diffNames = { "Story", "Very Easy", "Easy", "Normal", "Hard", "Very Hard", "Masochist", "Unwinnable", "Debug" };
        int activeDiff = saveData?.Difficulty ?? difficulty;
        bool activeHc = saveData?.IsHardcore ?? hardcore;
        string diffLabel = activeDiff >= 0 && activeDiff < diffNames.Length ? diffNames[activeDiff] : "Normal";
        if (saveData != null)
        {
            gameLog.Log($"Save loaded — Welcome back, {player.FirstName}.");
            gameLog.Log($"Floor {startFloor} | Lv.{player.Level} | {diffLabel}{(activeHc ? " [HARDCORE]" : "")}");
        }
        else
        {
            gameLog.Log($"Welcome to Floor 1 of Aincrad, {player.FirstName}.");
            gameLog.Log($"Difficulty: {diffLabel}{(activeHc ? " [HARDCORE]" : "")}");
        }
        gameLog.Log("WASD/Arrows: move | QEZC: diag | X: explore | H: help");
        gameLog.Log("I: inventory | G: grab | P: stats | R: rest | Space: wait | Bump: talk/shop");
        gameLog.Log("Shift+dir: sprint | Ctrl+dir: stealth | F5: save");
        gameLog.Log("");

        // Roll weather for starting floor
        WeatherSystem.RollWeather(startFloor);

        // Initial visibility & HUD
        turnManager.UpdateVisibility();
        RefreshHud();

        DebugLogger.LogState($"Player \"{player.FirstName}\"",
            $"LVL:{player.Level} HP:{player.CurrentHealth}/{player.MaxHealth} " +
            $"ATK:{player.Attack} DEF:{player.Defense} SPD:{player.Speed}");
        DebugLogger.EndTimer("GameScreen.Show", sw);
    }

    /// <summary>
    /// Launches the game screen from a loaded save file.
    /// Rebuilds player from save data, then delegates to Show with save context.
    /// </summary>
    public static void ShowFromSave(Window mainWindow, SaveData save)
    {
        // Temporary log for loading phase — replaced by Show's GameLogView
        var tempLog = new StringGameLog(new System.Text.StringBuilder());
        var player = Player.LoadFromSave(save, tempLog);
        Show(mainWindow, player, save.Difficulty, save.IsHardcore, saveData: save);
    }

    // ══════════════════════════════════════════════════════════════════
    //  UTILITY — Panel factory
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a FrameView containing a read-only TextView — used for the stats panel.
    /// </summary>
    private static (FrameView frame, TextView text) CreatePanel(
        string title, Pos x, Pos y, Dim width, Dim height)
    {
        var frame = new FrameView { Title = title, X = x, Y = y, Width = width, Height = height };
        var text = new TextView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(),
            ReadOnly = true, Text = ""
        };
        frame.Add(text);
        return (frame, text);
    }

    // ══════════════════════════════════════════════════════════════════
    //  UTILITY — Floor theme color
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a ColorScheme for the map frame based on floor tier.
    /// Green (1-10), Yellow (11-25), Orange/Red (26-50), Red (51-75), Magenta (76-100).
    /// </summary>
    private static ColorScheme FloorThemeColor(int floor)
    {
        Color fg = floor switch
        {
            <= 10 => Color.Green,
            <= 25 => Color.Yellow,
            <= 50 => Color.BrightRed,
            <= 75 => Color.Red,
            _     => Color.BrightMagenta
        };
        return new ColorScheme
        {
            Normal    = new Terminal.Gui.Attribute(fg, Color.Black),
            Focus     = new Terminal.Gui.Attribute(fg, Color.Black),
            HotNormal = new Terminal.Gui.Attribute(fg, Color.Black),
            HotFocus  = new Terminal.Gui.Attribute(fg, Color.Black),
            Disabled  = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
        };
    }
}
