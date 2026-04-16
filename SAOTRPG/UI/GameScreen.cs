using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Logging;
using SAOTRPG.Map;
using SAOTRPG.Systems;
using Story = SAOTRPG.Systems.Story;
using Skills = SAOTRPG.Systems.Skills;
using SAOTRPG.UI.Dialogs;
using SAOTRPG.UI.Helpers;

namespace SAOTRPG.UI;

// Main gameplay screen — assembles the map, minimap, stats, message log,
// and action bar. Borderless panels with inline title labels for a
// streamlined look; the outer window frame is the only container border.
public static partial class GameScreen
{
    private const int AnimationIntervalMs = 750, AutoExploreStepMs = 80;
    private const int HpBarWidth = 16, XpBarWidth = 10;

    // Fixed sidebar geometry — the right panel is always exactly this many
    // columns regardless of terminal width, so the stats layout is stable.
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

    public static void Show(Window mainWindow, Player player, int difficulty = 3, bool hardcore = false,
        SaveData? saveData = null, int saveSlot = 1)
    {
        try { ShowInternal(mainWindow, player, difficulty, hardcore, saveData, saveSlot); }
        catch (Exception ex)
        {
            DebugLogger.LogError("GameScreen.Show", ex);
            MessageBox.ErrorQuery("Crash", $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace?[..Math.Min(ex.StackTrace?.Length ?? 0, 500)]}", "OK");
        }
    }

    private static void ShowInternal(Window mainWindow, Player player, int difficulty, bool hardcore,
        SaveData? saveData, int saveSlot)
    {
        mainWindow.RemoveAll();
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
        }
        else
        {
            // Save load — RunModifiers restored by TurnManager.LoadFromSave.
        }
        Story.StorySystem.Handler = Dialogs.CutsceneDialog.Show;

        int startFloor = saveData?.CurrentFloor ?? 1;
        DebugLogger.LogGame("GAME", $"ShowInternal: floor={startFloor} diff={difficulty} hc={hardcore} save={saveData != null}");
        var (map, rooms) = MapGenerator.GenerateFloor(startFloor);
        DebugLogger.LogGame("GAME", $"Map generated: {map.Width}x{map.Height}, {rooms.Count} rooms");

        // ── Right column container ────────────────────────────────────
        // Fixed 50-col width anchored to the right edge — guarantees the
        // stats text always fits in its column and never depends on the
        // terminal's absolute width.
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

        int ruleAY       = MinimapHeight + 2;
        int statsTitleY  = MinimapHeight + 3;
        int statsTextY   = MinimapHeight + 4;

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
        var playerStatsText = new TextView
        {
            X = 1, Y = statsTextY, Width = Dim.Fill(1), Height = StatsHeight,
            ReadOnly = true, Text = player.GetStatsDisplay(),
            ColorScheme = ColorSchemes.Body,
        };

        var ruleB = new Label
        {
            Text = RuleLine, X = 1, Y = Pos.Bottom(playerStatsText),
            Width = Dim.Fill(1), Height = 1, ColorScheme = ColorSchemes.Dim,
        };

        var tabDefs = new (string Label, LogCategory? Filter)[]
        {
            ("All", null), ("Combat", LogCategory.Combat),
            ("System", LogCategory.System), ("Loot", LogCategory.Loot),
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
                for (int j = 0; j < tabButtons.Length; j++)
                    tabButtons[j].ColorScheme = j == idx ? ColorSchemes.Gold : ColorSchemes.Dim;
            };
        }

        rightPanel.Add(minimapTitleLabel, minimapView, minimapLegend,
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

        // ── Map area (left) ──────────────────────────────────────────
        // Fills all width except the fixed-width sidebar on the right.
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
        { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() };
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
        var hpLabel = new Label { Text = "", X = 1, Y = 0, ColorScheme = ColorSchemes.Body };
        var inventoryBtn = new Button
        { Text = " Inventory ", X = Pos.AnchorEnd(16), Y = 0, ColorScheme = ColorSchemes.Button };
        // Row 1: Floor info + weapon + context
        var infoLabel = new Label { Text = "", X = 1, Y = 1, ColorScheme = ColorSchemes.Dim };
        // Row 2: Consumable hotbar (left) + Skill hotbar (right)
        var hotbarLabel = new Label { Text = "", X = 1, Y = 2, ColorScheme = ColorSchemes.Body };
        var skillBarLabel = new Label { Text = "", X = Pos.AnchorEnd(58), Y = 2, Width = 56, ColorScheme = ColorSchemes.Gold };
        actionBar.Add(hpLabel, inventoryBtn, infoLabel, hotbarLabel, skillBarLabel);

        var turnManager = saveData != null
            ? TurnManager.LoadFromSave(saveData, map, player, gameLog)
            : new TurnManager(map, player, gameLog, startFloor, difficulty, hardcore);
        turnManager.ActiveSaveSlot = saveSlot;
        mainWindow.Title = $"Aincrad TRPG — Floor {turnManager.CurrentFloor}";
        int[] saveFlash = { 0 };

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

            // Active effects (only when present)
            var fx = new List<string>();
            if (turnManager.IsPoisoned)            fx.Add($"PSN:{turnManager.PoisonTurnsLeft}");
            if (turnManager.IsBleeding)            fx.Add($"BLD:{turnManager.BleedTurnsLeft}");
            if (turnManager.StunTurnsLeft > 0)     fx.Add($"STN:{turnManager.StunTurnsLeft}");
            if (turnManager.SlowTurnsLeft > 0)     fx.Add($"SLW:{turnManager.SlowTurnsLeft}");
            if (turnManager.ShrineBuffTurns > 0)   fx.Add($"+SHR:{turnManager.ShrineBuffTurns}");
            if (turnManager.LevelUpBuffTurns > 0)  fx.Add($"+SRG:{turnManager.LevelUpBuffTurns}");
            if (turnManager.RestCounter >= 250)    fx.Add("EXHAUSTED");
            else if (turnManager.RestCounter >= 150) fx.Add("FATIGUED");
            if (fx.Count > 0)
            {
                sidebar.Append("\n\n[ Effects ]");
                sidebar.Append($"\n  {string.Join("  ", fx)}");
            }

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

            string hpBar = BarBuilder.BuildHp(player.CurrentHealth, player.MaxHealth, HpBarWidth);
            string xpBar = BarBuilder.BuildXp(player.CurrentExperience, player.ExperienceRequired, XpBarWidth);
            string satBar = BarBuilder.Build(turnManager.Satiety, TurnManager.MaxSatiety, 6, '█', '░');
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
            bool lowHp = player.CurrentHealth <= player.MaxHealth / 4;
            hpLabel.ColorScheme = lowHp ? ColorSchemes.Danger : ColorSchemes.Body;

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

            // Single-pass hotbar count: walk the inventory once instead of
            // running FirstOrDefault five times per HUD refresh.
            var hotbarCounts = new int[5];
            foreach (var item in player.Inventory.Items)
            {
                if (item is not SAOTRPG.Items.Consumables.Consumable cons) continue;
                int slot = cons.Name switch
                {
                    "Health Potion"         => 0,
                    "Greater Health Potion" => 1,
                    "Antidote"              => 2,
                    "Battle Elixir"         => 3,
                    "Escape Rope"           => 4,
                    _                       => -1,
                };
                if (slot >= 0) hotbarCounts[slot] += cons.Quantity;
            }
            hotbarLabel.Text = $"[1]Heal:{hotbarCounts[0]}  [2]G.Heal:{hotbarCounts[1]}  [3]Antid:{hotbarCounts[2]}  [4]Elix:{hotbarCounts[3]}  [5]Rope:{hotbarCounts[4]}";

            // Row 2 right: Sword skill hotbar
            var sb = new System.Text.StringBuilder();
            for (int si = 0; si < turnManager.EquippedSkills.Length; si++)
            {
                var sk = turnManager.EquippedSkills[si];
                if (sk == null) { sb.Append($"[F{si + 1}]---  "); continue; }
                int cd = turnManager.GetSkillCooldown(sk.Id);
                string tag = cd > 0 ? $"({cd})" : "[OK]";
                sb.Append($"[F{si + 1}]{sk.Name} {tag}  ");
            }
            if (turnManager.PostMotionDelay > 0) sb.Append($"!! DELAY {turnManager.PostMotionDelay}T");
            skillBarLabel.Text = sb.ToString();

            inventoryBtn.Text = $" Inventory ({player.Inventory.Items.Count}) ";
            int explored = turnManager.Map.GetExplorationPercent();
            minimapTitleLabel.Text = $"Minimap — F{turnManager.CurrentFloor} ({explored}%)";
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

        bool activeHc = saveData?.IsHardcore ?? hardcore;
        string diffLabel = DifficultyData.Get(activeDifficulty)?.Name ?? "Normal";
        if (saveData != null)
        {
            gameLog.Log($"Save loaded — Welcome back, {player.FirstName}.");
            gameLog.Log($"Floor {startFloor} | Lv.{player.Level} | {diffLabel}{(activeHc ? " [HARDCORE]" : "")}");
        }
        else
        {
            gameLog.Log($"Welcome to Floor 1 of Aincrad, {player.FirstName}.");
            gameLog.Log($"Difficulty: {diffLabel}{(activeHc ? " [HARDCORE]" : "")}");
            TutorialSystem.ShowTip(gameLog, "floor1_welcome");
        }
        foreach (var helpLine in WelcomeHelpLines) gameLog.Log(helpLine);
        gameLog.Log("");

        WeatherSystem.RollWeather(startFloor);
        BiomeSystem.SetFloor(startFloor);
        if (saveData == null)
            gameLog.Log($"Biome: {BiomeSystem.DisplayName} -- {BiomeSystem.GetEntryMessage(startFloor)}");
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
        Show(mainWindow, player, save.Difficulty, save.IsHardcore, saveData: save, saveSlot: slot);
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
