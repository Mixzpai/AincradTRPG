using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using Story = SAOTRPG.Systems.Story;
using Skills = SAOTRPG.Systems.Skills;
using SAOTRPG.UI.Dialogs;
using SAOTRPG.UI.Helpers;
using SAOTRPG.UI.Widgets;

namespace SAOTRPG.UI;

// Main gameplay screen — assembles map, minimap, stats, log, action bar.
// Borderless panels + inline titles; the outer window frame is the only container border.
public static partial class GameScreen
{
    private const int AnimationIntervalMs = 750, AutoExploreStepMs = 80;
    private const int HpBarWidth = 16, XpBarWidth = 10;

    // Fixed sidebar width so stats layout is stable across terminal widths.
    private const int SidebarWidth  = 56;
    private const int SidebarInner  = 54;   // SidebarWidth - 2 for L/R padding
    private const int MinimapHeight = 10;
    private const int StatsHeight   = 18;   // fixed row budget — leaves room for the log
    private static readonly string RuleLine = new('─', SidebarInner);

    private static readonly string[] WelcomeHelpLines =
    {
        "  Move: WASD/Arrows  |  Diag: QEZC  |  Wait: Space  |  Help: H",
        "  Items: I  |  Stats: P  |  Skills: F  |  Quests: J  |  Grab: G",
        "  Combat: Bump to attack  |  F1-F4: Sword Skills  |  V: Counter",
    };

    public static void Show(Window mainWindow, Player player, int difficulty = 3,
        SaveData? saveData = null, int saveSlot = 1)
    {
        try { ShowInternal(mainWindow, player, difficulty, saveData, saveSlot); }
        catch (Exception ex)
        {
            DebugLogger.LogError("GameScreen.Show", ex);
            MessageBox.ErrorQuery("Crash", $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace?[..Math.Min(ex.StackTrace?.Length ?? 0, 500)]}", "OK");
        }
    }

    private static void ShowInternal(Window mainWindow, Player player, int difficulty,
        SaveData? saveData, int saveSlot)
    {
        mainWindow.RemoveAll();
        // Unhook menu-screen Esc handlers so they don't shadow MapView's PauseRequested routing.
        DifficultyScreen.UnhookEscHandler(mainWindow);
        CharacterCreationScreen.UnhookEscHandler(mainWindow);
        var sw = DebugLogger.StartTimer("GameScreen.Show");
        DebugLogger.LogScreen("GameScreen");

        // Clear static systems for new game (save loads restore their own state).
        if (saveData == null)
        {
            PartySystem.Clear();
            QuestSystem.ActiveQuests.Clear();
            QuestSystem.CompletedQuests.Clear();
            TutorialSystem.SeenTips.Clear();
            Story.StorySystem.Reset();
            Skills.UniqueSkillSystem.Reset();
            Skills.UniqueSkillSystem.TrapsDisarmed = 0;
            // Bundle 7: fresh run → zero out per-prefab MAX_PER_GAME counts.
            MapGenerator.SetPrefabUseCounts(null);
            // Bundle 8: fresh run → clear Divine one-per-run gate.
            LootGenerator.DivineObtainedThisRun = false;
        }
        else
        {
            // Save load — RunModifiers restored by TurnManager.LoadFromSave.
        }
        Story.StorySystem.Handler = Dialogs.CutsceneDialog.Show;

        int startFloor = saveData?.CurrentFloor ?? 1;
        DebugLogger.LogGame("GAME", $"ShowInternal: floor={startFloor} diff={difficulty} save={saveData != null}");
        var (map, rooms) = MapGenerator.GenerateFloor(startFloor);
        DebugLogger.LogGame("GAME", $"Map generated: {map.Width}x{map.Height}, {rooms.Count} rooms");

        // ── Right column container ── Fixed-width, right-anchored; stats never depend on terminal width.
        var rightPanel = new View
        {
            X = Pos.AnchorEnd(SidebarWidth), Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(4),
            ColorScheme = ColorSchemes.Body,
            CanFocus = true,
        };

        // Minimap block: title (1) + map (MinimapHeight) + legend (1) = MinimapHeight+2 rows
        var minimapTitleLabel = new Label
        {
            Text = $"Minimap — F{startFloor}",
            X = 1, Y = 0, Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.Gold,
        };
        var minimapView = new MinimapView(map, player)
        { X = 1, Y = 1, Width = Dim.Fill(1), Height = MinimapHeight };
        var minimapLegend = new Label
        {
            Text = "@ you  * mob  ! item  ◊ stairs",
            X = 1, Y = MinimapHeight + 1, Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.Dim,
        };

        // FB-474 quest tracker — anchored 2-row gap below minimap legend so
        // 80×24 terminals keep map breathing room. Widget hides at empty state.
        int trackerY    = MinimapHeight + 3;
        var questTracker = new QuestTrackerWidget
        { X = 1, Y = trackerY };

        // Stats block shifts down by tracker block (height 2 + trailing row).
        int ruleAY       = MinimapHeight + 6;
        int statsTitleY  = MinimapHeight + 7;
        int statsTextY   = MinimapHeight + 8;

        var ruleA = new Label
        {
            Text = RuleLine, X = 1, Y = ruleAY, Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.Dim,
        };
        var statsTitleLabel = new Label
        {
            Text = "Status", X = 1, Y = statsTitleY, Width = Dim.Fill(1), Height = 1,
            ColorScheme = ColorSchemes.Gold,
        };
        // Bundle 11: shrink stats text by 2 rows to make room for the status
        // icon row underneath (instantiated after turnManager exists below).
        // The [Effects] sidebar section now renders only when icon row hides.
        const int IconRowHeight = 2;
        var playerStatsText = new TextView
        {
            X = 1, Y = statsTextY, Width = Dim.Fill(1), Height = StatsHeight - IconRowHeight,
            ReadOnly = true, Text = player.GetStatsDisplay(),
            ColorScheme = ColorSchemes.Body,
        };

        var ruleB = new Label
        {
            Text = RuleLine, X = 1, Y = Pos.Bottom(playerStatsText) + IconRowHeight,
            Width = Dim.Fill(1), Height = 1, ColorScheme = ColorSchemes.Dim,
        };

        var tabDefs = new (string Label, LogCategory? Filter)[]
        {
            ("All", null), ("Combat", LogCategory.Combat),
            ("System", LogCategory.System), ("Item", LogCategory.Item),
            ("Dialog", LogCategory.Dialog),
        };

        var messagesTitleLabel = new Label
        {
            Text = "Messages", X = 1, Y = Pos.Bottom(ruleB),
            Width = 10, Height = 1, ColorScheme = ColorSchemes.Gold
        };

        var tabButtons = new Button[tabDefs.Length];
        int tabX = 12;
        for (int i = 0; i < tabDefs.Length; i++)
        {
            tabButtons[i] = new Button
            {
                Text = tabDefs[i].Label,
                X = tabX, Y = Pos.Bottom(ruleB),
                ColorScheme = i == 0 ? ColorSchemes.Gold : ColorSchemes.Dim,
                NoPadding = true
            };
            tabX += tabDefs[i].Label.Length + 3;
        }

        var coloredLog = new ColoredLogView
        {
            X = 1, Y = Pos.Bottom(messagesTitleLabel),
            Width = Dim.Fill(1), Height = Dim.Fill()
        };

        for (int i = 0; i < tabDefs.Length; i++)
        {
            int idx = i;
            tabButtons[i].Accepting += (s, e) =>
            {
                e.Cancel = true;
                coloredLog.SetFilter(tabDefs[idx].Filter);
            };
        }

        // Keep tab-button colors in sync whether filter changed via click or Tab-key cycling.
        coloredLog.FilterChanged += (filter) =>
        {
            for (int j = 0; j < tabButtons.Length; j++)
                tabButtons[j].ColorScheme = tabDefs[j].Filter.Equals(filter)
                    ? ColorSchemes.Gold : ColorSchemes.Dim;
        };

        rightPanel.Add(minimapTitleLabel, minimapView, minimapLegend, questTracker,
            ruleA, statsTitleLabel, playerStatsText, ruleB, messagesTitleLabel);
        foreach (var tb in tabButtons) rightPanel.Add(tb);
        rightPanel.Add(coloredLog);

        var gameLog = new GameLogView(coloredLog);
        player.SetLog(gameLog);
        player.Inventory.SetLogger(new TerminalGuiInventoryLogger(gameLog));

        if (saveData == null) ApplyNewGameModifiers(player, gameLog);

        int activeDifficulty = saveData?.Difficulty ?? difficulty;
        MapGenerator.PopulateFloor(map, rooms, player, startFloor,
            activeDifficulty >= 0 && activeDifficulty <= 8 ? new[] { 40, 60, 80, 100, 130, 170, 220, 300, 50 }[activeDifficulty] : 100);

        foreach (var entity in map.Entities) entity.SetLog(gameLog);

        // ── Map area (left) ── Fills width minus right sidebar.
        var camera = new Camera();
        var mapArea = new View
        {
            X = 0, Y = 0,
            Width = Dim.Fill(SidebarWidth), Height = Dim.Fill(4),
            ColorScheme = ColorSchemes.Body,
            CanFocus = true,
        };
        var mapTitleLabel = new Label
        {
            Text = $"Floor {startFloor} — Aincrad",
            X = 1, Y = 0, Width = Dim.Fill(), Height = 1,
            ColorScheme = FloorThemeColor(startFloor)
        };
        var mapView = new MapView(map, camera, player)
        { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(), Log = gameLog };
        mapArea.Add(mapTitleLabel, mapView);

        // ── Bottom rule + action bar ─────────────────────────────────
        var ruleBottom = new Label
        {
            Text = RuleLine, X = 0, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(), Height = 1, ColorScheme = ColorSchemes.Dim
        };
        var actionBar = new View
        {
            X = 0, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(), Height = 3,
            ColorScheme = ColorSchemes.Body,
            CanFocus = true,
        };
        // Row 0: HP/XP/Status bars + Inventory button (right)
        var hpLabel = new Label { Text = "", X = 1, Y = 0, Width = 62, ColorScheme = ColorSchemes.Body };
        var inventoryBtn = new Button
        { Text = " Inventory ", X = Pos.AnchorEnd(16), Y = 0, ColorScheme = ColorSchemes.Button };
        // Row 1: Floor info + weapon + context
        var infoLabel = new Label { Text = "", X = 1, Y = 1, Width = 62, ColorScheme = ColorSchemes.Dim };
        // Row 2: Consumable quickbar (left, 29 cols) + sword-skill indicator
        // (right, ~32 cols with F1-F4 shrunk to [FN]). See research §3 budget.
        var hotbarLabel = new Label { Text = "", X = 1, Y = 2, Width = 30, ColorScheme = ColorSchemes.Body };
        var skillBarLabel = new Label { Text = "", X = Pos.AnchorEnd(34), Y = 2, Width = 32, ColorScheme = ColorSchemes.Gold };
        actionBar.Add(hpLabel, inventoryBtn, infoLabel, hotbarLabel, skillBarLabel);

        // Seed session bestiary from lifetime store — completion counter spans all runs.
        SAOTRPG.Systems.Bestiary.LoadFromLifetimeStats();

        var turnManager = saveData != null
            ? TurnManager.LoadFromSave(saveData, map, player, gameLog)
            : new TurnManager(map, player, gameLog, startFloor, difficulty);
        turnManager.ActiveSaveSlot = saveSlot;
        mainWindow.Title = $"Aincrad TRPG — Floor {turnManager.CurrentFloor}";
        int[] saveFlash = { 0 };

        // FB-479 status tray — starts col 64 (right of HP bars), 2-row tall.
        // Second-row wrap handled inside the widget when col budget exceeded.
        var statusTray = new StatusTrayWidget(turnManager, player)
        { X = 64, Y = 0, Width = Dim.Fill(18), Height = 2 };
        actionBar.Add(statusTray);

        // Bundle 11 — sidebar status icon row, slotted between Status text and
        // ruleB. 2 rows: glyph row + countdown row. Hides on sidebar < 12 cols.
        var statusIconRow = new StatusIconRowWidget(turnManager)
        {
            X = 1, Y = Pos.Bottom(playerStatsText),
            Width = Dim.Fill(1), Height = IconRowHeight,
        };
        rightPanel.Add(statusIconRow);
        // Shift+S toggles verbose labels (session-local; research §6 says don't persist).
        mapView.StatusTrayVerboseToggleRequested += () => statusTray.ToggleVerbose();

        // F9 — biome JSON hot-reload. Re-reads Content/Biomes/*.json, regenerates
        // the current floor, and re-rolls mobs/loot via PopulateFloor.
        mapView.BiomeReloadRequested += () =>
        {
            SAOTRPG.Map.Generation.BiomeGenConfigLoader.Invalidate();
            var (reMap, reRooms) = MapGenerator.GenerateFloor(turnManager.CurrentFloor);
            MapGenerator.PopulateFloor(reMap, reRooms, player, turnManager.CurrentFloor,
                activeDifficulty >= 0 && activeDifficulty <= 8
                    ? new[] { 40, 60, 80, 100, 130, 170, 220, 300, 50 }[activeDifficulty] : 100);
            // Refresh CurrentGenConfig + tree glyph before MapSwapped fires so
            // the tint/ambient handlers pick up updated JSON values.
            BiomeSystem.SetFloor(turnManager.CurrentFloor);
            // ReplaceMap fires MapSwapped which already rewires mapView/minimapView
            // and updates titles via GameScreen.Events.cs — do not double-call SetMap.
            turnManager.ReplaceMap(reMap, player);
            mapTitleLabel.Text = $"Floor {turnManager.CurrentFloor} — Aincrad";
            mapTitleLabel.ColorScheme = FloorThemeColor(turnManager.CurrentFloor);
            ToastQueue.Enqueue($"Biomes reloaded — {BiomeSystem.DisplayName}", Color.BrightCyan, ToastCategory.StatUp);
        };

        void RefreshHud()
        {
            var wpnEquip   = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon);
            var chestEquip = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Chest);
            var offEquip   = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.OffHand);

            // ── Sidebar sections ─────────────────────────────────────
            const int nb = SidebarInner - 6;
            var sidebar = new System.Text.StringBuilder();

            // Core stats
            sidebar.Append(player.GetStatsDisplay());

            // Equipment (always shown)
            string sideWpnName = wpnEquip is SAOTRPG.Items.Equipment.EquipmentBase we
                ? we.EnhancedName : wpnEquip?.Name ?? "--";
            sidebar.Append("\n\n[ Equipment ]");
            sidebar.Append($"\n  WPN  {TextHelpers.Truncate(sideWpnName, nb)}");
            sidebar.Append($"\n  CHT  {TextHelpers.Truncate(chestEquip?.Name ?? "--", nb)}");
            sidebar.Append($"\n  OFF  {TextHelpers.Truncate(offEquip?.Name ?? "--", nb)}");

            // Floor progress
            int floorKills = turnManager.KillCount - turnManager.FloorKillsStart;
            int explPct = turnManager.Map.GetExplorationPercent();
            int par = TurnManager.GetFloorPar(turnManager.CurrentFloor);
            sidebar.Append("\n\n[ Floor ]");
            sidebar.Append($"\n  F{turnManager.CurrentFloor}  {BiomeSystem.DisplayName}");
            sidebar.Append($"\n  Kills:{floorKills}  Expl:{explPct}%  {SAOTRPG.Map.DayNightCycle.PhaseName}");
            sidebar.Append($"\n  T{turnManager.FloorTurns}/{par}  {turnManager.GetFloorDangerLabel()}");

            // Bundle 11 — Effects text moved to StatusIconRowWidget (rendered
            // beneath this sidebar block). Exhaustion/Fatigue stay as text since
            // they have no per-glyph icon mapping in StatusIconMap.
            if (turnManager.RestCounter >= 250)
                sidebar.Append("\n\n[ Effects ]\n  EXHAUSTED");
            else if (turnManager.RestCounter >= 150)
                sidebar.Append("\n\n[ Effects ]\n  FATIGUED");
            // Trigger icon-row repaint each HUD tick so countdowns tick down.
            statusIconRow.SetNeedsDraw();

            // Bounty (only when active)
            if (turnManager.BountyTarget != null)
            {
                sidebar.Append("\n\n[ Bounty ]");
                if (turnManager.BountyComplete)
                    sidebar.Append("\n  COMPLETE!");
                else
                    sidebar.Append($"\n  {turnManager.BountyTarget}: {turnManager.BountyKillsCurrent}/{turnManager.BountyKillsNeeded}");
            }

            // Party members
            if (PartySystem.Members.Count > 0)
            {
                sidebar.Append("\n\n[ Party ]");
                foreach (var a in PartySystem.Members)
                {
                    string hp = a.IsDefeated ? "KO" : $"{a.CurrentHealth}/{a.MaxHealth}";
                    sidebar.Append($"\n  {a.Name} Lv.{a.Level} HP:{hp}");
                }
            }

            playerStatsText.Text = sidebar.ToString();

            // Bundle 11: eighth-block stat bar resolution (8x ASCII), with green/
            // yellow/red zones via StatBarHelper.ZoneColor. Width unchanged so the
            // sidebar layout stays stable.
            string hpBar = StatBarHelper.Build(player.CurrentHealth, player.MaxHealth, HpBarWidth);
            string xpBar = StatBarHelper.Build(player.CurrentExperience, player.ExperienceRequired, XpBarWidth);
            string satBar = StatBarHelper.Build(turnManager.Satiety, TurnManager.MaxSatiety, 6);
            string durWarns = DurabilityHelper.BuildWarningTags(player.Inventory);
            string statusTags = StatusTagBuilder.Build(turnManager, durWarns);
            string saveTag = saveFlash[0] > 0 ? "  [Saved]" : "";
            if (saveFlash[0] > 0) saveFlash[0]--;
            string weatherTag = WeatherSystem.Current != WeatherType.Clear
                ? $"  [{WeatherSystem.GetLabel()}]" : "";

            // Row 0: HP | XP | SAT | Status effects
            hpLabel.Text = $"HP {hpBar} {player.CurrentHealth}/{player.MaxHealth}" +
                           $" | XP {xpBar} Lv{player.Level}" +
                           $" | SAT {satBar}" + statusTags + weatherTag + saveTag;
            // HP zone color drives the row tint. Critical (<25%) escalates to
            // ColorSchemes.Danger so the existing low-HP redline behavior persists.
            var hpZone = StatBarHelper.ZoneColor(player.CurrentHealth, player.MaxHealth);
            hpLabel.ColorScheme = hpZone == Color.BrightRed
                ? ColorSchemes.Danger
                : (hpZone == Color.BrightYellow ? ColorSchemes.FromColor(Color.BrightYellow) : ColorSchemes.Body);

            // Row 1: Floor context line
            var wpn = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon);
            string profTag = "";
            if (wpn is SAOTRPG.Items.Equipment.Weapon w && !string.IsNullOrEmpty(w.WeaponType))
            {
                int bonus = turnManager.GetProficiencyBonus(w.WeaponType);
                if (bonus > 0) profTag = $"+{bonus}";
            }
            string wpnName = wpn != null ? $"{wpn.Name} {profTag}" : "Fists";
            int mobsLeft = turnManager.GetMonsterCount();
            string ctxHint = turnManager.GetContextHint();
            string hintTag = ctxHint.Length > 0 ? $" | {ctxHint}" : "";

            infoLabel.Text = $"F{turnManager.CurrentFloor}" +
                             $" | {player.ColOnHand}c" +
                             $" | WPN: {wpnName}" +
                             $" | Mobs: {mobsLeft}" +
                             $" | T{turnManager.TurnCount}" +
                             hintTag;

            // Row 2 left: 10-slot quickbar. "N G" pairs separated by 1-col
            // gutter per research §3 (29 cols for 10 slots). Slot number
            // dim-gray, glyph bright when filled / `·` dim when empty.
            var qb = new System.Text.StringBuilder();
            for (int qs = 0; qs < QuickbarState.SlotCount; qs++)
            {
                var (glyph, filled, _) = player.Quickbar.SlotDisplay(qs, player);
                // Slot number: 1..9 then 0 for slot 10.
                char slotDigit = qs == 9 ? '0' : (char)('1' + qs);
                if (qs > 0) qb.Append(' ');
                qb.Append(slotDigit); qb.Append(glyph);
                _ = filled; // glyph already encodes state (· when empty)
            }
            hotbarLabel.Text = qb.ToString();

            // Row 2 right: F1-F4 shrunk to 4-cell [FN] labels so quickbar fits.
            // Full skill name surfaces in the game log on use; cooldown turns
            // sit as a trailing ( N ) marker only when non-zero.
            var sb = new System.Text.StringBuilder();
            for (int si = 0; si < turnManager.EquippedSkills.Length; si++)
            {
                var sk = turnManager.EquippedSkills[si];
                if (sk == null) { sb.Append($"[F{si + 1}]·   "); continue; }
                int cd = turnManager.GetSkillCooldown(sk.Id);
                string tag = cd > 0 ? $"({cd})" : "   ";
                sb.Append($"[F{si + 1}]{tag} ");
            }
            if (turnManager.PostMotionDelay > 0) sb.Append($"!DLY{turnManager.PostMotionDelay}");
            skillBarLabel.Text = sb.ToString();

            inventoryBtn.Text = $" Inventory ({player.Inventory.Items.Count}) ";
            int explored = turnManager.Map.GetExplorationPercent();
            minimapTitleLabel.Text = $"Minimap — F{turnManager.CurrentFloor} ({explored}%)";
            // FB-474/FB-479 widgets repaint every HUD refresh tick.
            questTracker.SetNeedsDraw();
            statusTray.SetNeedsDraw();
            mapView.SetNeedsDraw();
        }

        WireVisualEffects(turnManager, mapView);
        WireSystemEvents(mainWindow, turnManager, player, gameLog, coloredLog,
            mapView, minimapView, mapTitleLabel, minimapTitleLabel, saveFlash, saveSlot, RefreshHud);
        WireKeybindActions(turnManager, mapView, minimapView, player, gameLog, coloredLog,
            inventoryBtn, saveSlot, saveFlash, RefreshHud);
        WirePassiveSystems(turnManager, mapView, player, gameLog);

        mainWindow.Add(mapArea, rightPanel, ruleBottom, actionBar);
        mapView.SetFocus();

        string diffLabel = DifficultyData.Get(activeDifficulty)?.Name ?? "Normal";
        if (saveData != null)
        {
            gameLog.Log($"Save loaded — Welcome back, {player.FirstName}.");
            gameLog.Log($"Floor {startFloor} | Lv.{player.Level} | {diffLabel}");
        }
        else
        {
            gameLog.Log($"Welcome to Floor 1 of Aincrad, {player.FirstName}.");
            gameLog.Log($"Difficulty: {diffLabel}");
            TutorialSystem.ShowTip(gameLog, "floor1_welcome");
        }
        foreach (var helpLine in WelcomeHelpLines) gameLog.Log(helpLine);
        gameLog.Log("");

        WeatherSystem.RollWeather(startFloor);
        BiomeSystem.SetFloor(startFloor);
        if (saveData == null)
            gameLog.Log($"Biome: {BiomeSystem.DisplayName} -- {BiomeSystem.GetEntryMessage(startFloor)}");
        // Per-biome flavor toast on fresh entry; overrides the generic entry line.
        var startCfg = BiomeSystem.CurrentGenConfig;
        if (saveData == null && !string.IsNullOrEmpty(startCfg?.FloorEntryText))
            ToastQueue.Enqueue(startCfg!.FloorEntryText!, Color.BrightCyan, ToastCategory.Info);
        if (WeatherSystem.Current != WeatherType.Clear)
        {
            gameLog.Log($"Weather: {WeatherSystem.GetLabel()}");
            string weatherFlavor = WeatherSystem.GetFlavorDescription();
            if (weatherFlavor.Length > 0) gameLog.Log($"  {weatherFlavor}");
        }

        turnManager.UpdateVisibility();
        RefreshHud();

        // Fire the game-start cutscene (F1 prologue) on a fresh run. Skips
        // automatically if the event is already in FiredEventIds (save-loaded run).
        if (saveData == null)
        {
            Application.AddTimeout(TimeSpan.FromMilliseconds(250), () =>
            {
                Story.StorySystem.TryFire(Story.StoryTrigger.GameStart,
                    new Story.StoryContext(startFloor, 0, player));
                return false;
            });
        }

        DebugLogger.LogState($"Player \"{player.FirstName}\"",
            $"LVL:{player.Level} HP:{player.CurrentHealth}/{player.MaxHealth} " +
            $"ATK:{player.Attack} DEF:{player.Defense} SPD:{player.Speed}");
        DebugLogger.EndTimer("GameScreen.Show", sw);
    }

    // FB-564 — Apply Run Modifier start-of-run effects. Called once per new
    // game after player + inventory are wired up, before MapGenerator runs.
    private static void ApplyNewGameModifiers(Player player, IGameLog log)
    {
        if (RunModifiers.Active.Count == 0) return;

        log.LogSystem($"══════════════════════════════════════");
        log.LogSystem($"  Run Modifiers active: {RunModifiers.Active.Count} (×{RunModifiers.TotalScoreMultiplier():F2})");
        foreach (var mod in RunModifiers.Active)
            log.LogSystem($"    • {RunModifiers.Definitions[mod].Name}");
        log.LogSystem($"══════════════════════════════════════");

        // Beater — sour all factions immediately.
        if (RunModifiers.IsActive(RunModifier.Beater))
        {
            foreach (var faction in Enum.GetValues<Story.Faction>())
                Story.StorySystem.AdjustRep(faction, -50);
            log.LogSystem("  [Beater] All factions scorn you. Shop prices hiked.");
        }

        // Hollow Ingress — strip all sword skill unlocks + kill counts.
        if (RunModifiers.IsActive(RunModifier.HollowIngress))
        {
            log.LogSystem("  [Hollow Ingress] All Sword Skills locked. Earn them via combat.");
        }

        // Sword Art Only + Naked Ingress — strip invalid starting equipment.
        if (RunModifiers.IsActive(RunModifier.SwordArtOnly))
            log.LogSystem("  [Sword Art Only] Only One-Handed Swords may be equipped.");
        if (RunModifiers.IsActive(RunModifier.NakedIngress))
            log.LogSystem("  [Naked Ingress] No armor equippable. Move light, dodge hard.");
    }

    // Launches the game screen from a loaded save file.
    public static void ShowFromSave(Window mainWindow, SaveData save, int slot)
    {
        var tempLog = new StringGameLog(new System.Text.StringBuilder());
        var player = Player.LoadFromSave(save, tempLog);
        Show(mainWindow, player, save.Difficulty, saveData: save, saveSlot: slot);
    }

    private static ColorScheme FloorThemeColor(int floor) => ColorSchemes.FromColor(floor switch
    {
        <= 10 => Color.Green,
        <= 25 => Color.Yellow,
        <= 50 => Color.BrightRed,
        <= 75 => Color.Red,
        _     => Color.BrightMagenta,
    });
}
