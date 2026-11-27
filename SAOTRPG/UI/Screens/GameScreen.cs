using SAOTRPG.Entities;
using SAOTRPG.Map;
using Story = SAOTRPG.Systems.Story;
using Skills = SAOTRPG.Systems.Skills;
using SAOTRPG.UI.Helpers;
using SAOTRPG.UI.Widgets;
using SAOTRPG.Map.Generation;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.Persistence;
using SAOTRPG.Systems.Progression;
using SAOTRPG.Systems.RunState;
using SAOTRPG.Systems.Social;
using SAOTRPG.Systems.Turns;
using SAOTRPG.Systems.World;
using SAOTRPG.UI.Effects;
using SAOTRPG.UI.GameLog;
using SAOTRPG.UI.Rendering;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.UI.Screens;

// Main gameplay screen — assembles map, minimap, stats, log, action bar.
// Borderless panels + inline titles; the outer window frame is the only container border.
public static partial class GameScreen
{
    // AnimationIntervalMs paces the clock-driven-tile repaint. Must stay at or below half the
    // shortest tile animation step (TileDefinitions.LavaStepMs, 500ms) or phases get skipped.
    private const int AnimationIntervalMs = 250, AutoExploreStepMs = 80;
    // Turns of a wait or sleep run per frame: an hour in about half a minute of frames.
    private const int ActivityTurnsPerFrame = 120;

    // Bumped every time the game screen is built. Repeating timeouts capture the value
    // current at wiring time and retire themselves once it moves on: they are never
    // unregistered otherwise, so each new run — and permadeath makes those frequent —
    // would leave another pair firing forever, each pinning its run's MapView and, through
    // it, that run's GameMap (a 1000x1000 tile array on Floor 1).
    private static int s_screenGeneration;
    private const int HpBarWidth = 16, XpBarWidth = 10;
    // A HUD line's stack buffer: well past the widest screen, so nothing is cut off.
    private const int HudLineCapacity = 512;

    // Fixed sidebar width so stats layout is stable across terminal widths.
    internal const int SidebarWidth = 56;
    private const int SidebarInner  = 54;   // SidebarWidth - 2 for L/R padding
    private static readonly string RuleLine = new('─', SidebarInner);

    // Every key here is a {{KEY:Action}} token, resolved through KeyHints when the strip is
    // logged, so the strip follows a rebind.
    private static readonly string[] WelcomeHelpLines =
    {
        "  Move: {{MOVEKEYS}}  |  Diag: {{DIAGKEYS}}  |  Wait: {{KEY:Wait}}  |  Help: {{KEY:OpenHelp}}",
        "  Items: {{KEY:OpenInventory}}  |  Stats: {{KEY:OpenStats}}  |  Skills: {{KEY:OpenSwordSkills}}"
            + "  |  Quests: {{KEY:OpenQuestLog}}  |  Grab: {{KEY:Pickup}}",
        "  Combat: Bump to attack  |  {{KEY:SwordSkill1}}-{{KEY:SwordSkill4}}: Sword Skills"
            + "  |  {{KEY:Counter}}: Counter",
    };

    // runSeed: null mints one. Character creation passes a player-entered seed through so a run
    // can be replayed or shared.
    public static void Show(Window mainWindow, Player player, int difficulty = 3,
        SaveData? saveData = null, int saveSlot = 1, int? runSeed = null)
    {
        try { ShowInternal(mainWindow, player, difficulty, saveData, saveSlot, runSeed); }
        catch (Exception ex)
        {
            DebugLogger.LogError("GameScreen.Show", ex);
            DialogHelper.ErrorQuery("Crash", $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace?[..Math.Min(ex.StackTrace?.Length ?? 0, 500)]}", "OK");
        }
    }

    private static void ShowInternal(Window mainWindow, Player player, int difficulty,
        SaveData? saveData, int saveSlot, int? runSeed = null)
    {
        mainWindow.RemoveAll();
        SAOTRPG.UI.Helpers.GameWindow.RequestFullClear();
        s_screenGeneration++;
        // Unhook menu-screen Esc handlers so they don't shadow MapView's PauseRequested routing.
        NavigationHelper.UnhookScreenEscHandlers(mainWindow);
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
            // Fresh run → zero out per-prefab MAX_PER_GAME counts.
            MapGenerator.SetPrefabUseCounts(null);

            // Mint the run's seed here, before anything generates, so each run in one process
            // gets its own world.
            int seed = runSeed ?? RunRng.NewSeed();
            MapGenerator.SetGlobalSeed(seed);
            RunRng.Reseed(seed);
        }
        else
        {
            // Save load — RunModifiers restored by TurnManager.LoadFromSave, and the map seed by
            // SaveManager.LoadGame. The gameplay stream restarts from that same seed.
            RunRng.Reseed(saveData.GlobalSeed);
        }
        Story.StorySystem.Handler = Dialogs.CutsceneDialog.Show;

        int startFloor = saveData?.CurrentFloor ?? 1;

        // The seed has just been set (new run) or restored (load), so anything built ahead of it
        // belongs to another run. Invalidate joins any in-flight build before returning.
        FloorPrefetch.Invalidate();

        DebugLogger.LogGame("GAME", $"ShowInternal: floor={startFloor} diff={difficulty} save={saveData != null} seed={RunRng.Seed}");
        var (map, rooms) = MapGenerator.GenerateFloor(startFloor);
        DebugLogger.LogGame("GAME", $"Map generated: {map.Width}x{map.Height}, {rooms.Count} rooms");
        // Build the next floor while the player is on this one. Nothing waits on it.
        FloorPrefetch.Request(startFloor + 1);

        // ── Right column ── Fixed width, right-anchored: three cards, MAP, STATUS and LOG, whose
        // rows SidebarLayout derives from the column's own height so all three stay on screen.
        var rightPanel = new PanelView
        {
            X = Pos.AnchorEnd(SidebarWidth), Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(4),
            SchemeName = ColorSchemes.BodyName,
            CanFocus = true,
        };

        // A card is a frame, not a parent: each is added before its content, a sibling inset by
        // Card.InsideX. Rows are placed below, once the stats panel exists.
        var mapCard = new Card("MAP", 0, 0, Dim.Fill(), SidebarLayout.CardChrome + 1);
        var minimapView = new OvermapView(map, player)
        { X = Card.InsideX(0), Y = 1, Width = Dim.Fill(2), Height = 1 };
        var questTracker = new QuestTrackerWidget { X = Card.InsideX(0), Y = 1, Visible = false };
        var statusCard = new Card("STATUS", 0, 0, Dim.Fill(), SidebarLayout.CardChrome + 1);
        var logCard = new Card("LOG", 0, 0, Dim.Fill(), SidebarLayout.CardChrome + 1);
        var coloredLog = new ColoredLogView { X = Card.InsideX(0), Y = 1, Width = Dim.Fill(2), Height = 1 };
        const int IconRowHeight = 2;

        rightPanel.Add(mapCard, minimapView, questTracker, statusCard, logCard, coloredLog);

        var gameLog = new GameLogView(coloredLog);
        player.SetLog(gameLog);
        player.Inventory.SetLogger(new TerminalGuiInventoryLogger(gameLog));

        if (saveData == null) ApplyNewGameModifiers(player, gameLog);

        int activeDifficulty = saveData?.Difficulty ?? difficulty;
        MapGenerator.PopulateFloor(map, rooms, player, startFloor,
            activeDifficulty >= 0 && activeDifficulty <= 8 ? new[] { 40, 60, 80, 100, 130, 170, 220, 300, 50 }[activeDifficulty] : 100);
        // Remove already-defeated field bosses after populating, so the floor's spawn draws are unchanged.
        if (saveData?.DefeatedFieldBosses is { } deadBosses)
            foreach (var fb in map.Entities.OfType<FieldBoss>().Where(b => deadBosses.Contains(b.FieldBossId)).ToList())
                map.RemoveEntity(fb);

        foreach (var entity in map.Entities) entity.SetLog(gameLog);

        // ── Map area (left) ── Fills width minus right sidebar.
        var camera = new Camera();
        var mapArea = new PanelView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(SidebarWidth), Height = Dim.Fill(4),
            SchemeName = ColorSchemes.BodyName,
            CanFocus = true,
        };
        var mapTitleLabel = new Label
        {
            Text = $"Floor {startFloor} — Aincrad",
            X = 1, Y = 0, Width = Dim.Fill(), Height = 1,
        }.WithScheme(FloorThemeColor(startFloor));
        var mapView = new MapView(map, camera, player)
        { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(), Log = gameLog };
        mapArea.Add(mapTitleLabel, mapView);

        // ── Bottom rule + action bar ─────────────────────────────────
        var ruleBottom = new Label
        {
            Text = RuleLine, X = 0, Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(), Height = 1, SchemeName = ColorSchemes.DimName
        };
        var actionBar = new PanelView
        {
            X = 0, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(), Height = 3,
            SchemeName = ColorSchemes.BodyName,
            CanFocus = true,
        };
        // Row 0: HP/XP/Status bars + Inventory button (right)
        var hpLabel = new HudLine { X = 1, Y = 0, Width = 62, SchemeName = ColorSchemes.BodyName };
        var inventoryBtn = new PlainButton
        { Text = " Inventory ", X = Pos.AnchorEnd(16), Y = 0, SchemeName = ColorSchemes.ButtonName,
          ShadowStyle = null };
        // Row 1: Floor info + weapon + context
        var infoLabel = new HudLine { X = 1, Y = 1, Width = 62, SchemeName = ColorSchemes.DimName };
        // Row 2: Consumable quickbar (left, 29 cols) + sword-skill indicator
        // (right, ~32 cols with F1-F4 shrunk to [FN]).
        var hotbarLabel = new HudLine { X = 1, Y = 2, Width = 30, SchemeName = ColorSchemes.BodyName };
        var skillBarLabel = new HudLine { X = Pos.AnchorEnd(34), Y = 2, Width = 32, SchemeName = ColorSchemes.GoldName };
        actionBar.Add(hpLabel, inventoryBtn, infoLabel, hotbarLabel, skillBarLabel);

        // Seed session bestiary from lifetime store — completion counter spans all runs.
        SAOTRPG.Systems.Progression.Bestiary.LoadFromLifetimeStats();

        var turnManager = saveData != null
            ? TurnManager.LoadFromSave(saveData, map, player, gameLog)
            : new TurnManager(map, player, gameLog, startFloor, difficulty);
        turnManager.ActiveSaveSlot = saveSlot;
        mainWindow.Title = $"Aincrad TRPG — Floor {turnManager.CurrentFloor}";
        int[] saveFlash = { 0 };
        // Last HP zone colour applied to hpLabel. Swapping the scheme builds a new
        // Scheme, so only do it when the zone actually changes.
        Color[] lastHpZone = { Color.Black };
        // The minimap follows the turn: the player, the visible set and the reveals all move with it.
        int[] minimapTurn = { -1 };
        // RefreshHud asks whether any status icon is up on every refresh; one list serves it.
        var iconScratch = new List<StatusIconMap.StatusIcon>();

        // Status tray — starts col 64 (right of HP bars), 2-row tall.
        // Second-row wrap handled inside the widget when col budget exceeded.
        var statusTray = new StatusTrayWidget(turnManager, player)
        { X = 64, Y = 0, Width = Dim.Fill(18), Height = 2 };
        actionBar.Add(statusTray);

        // The STATUS card's content: the stats panel, and the status icons under it while any
        // are active.
        var statsPanel = new StatsPanelWidget(player, turnManager)
        { X = Card.InsideX(0), Y = 1, Width = Dim.Fill(2), Height = 1 };
        var statusIconRow = new StatusIconRowWidget(turnManager)
        { X = Card.InsideX(0), Y = 1, Width = Dim.Fill(2), Height = IconRowHeight, Visible = false };
        rightPanel.Add(statsPanel, statusIconRow);

        // Every row of the column, evaluated at layout time against the column's current height,
        // so a resize lands in the same pass. The parts that come and go are read off their
        // views' Visible flags; RefreshHud re-lays the column out when one changes.
        SidebarLayout.Heights Plan() => SidebarLayout.For(rightPanel.Viewport.Height,
            statsPanel.PreferredHeight, StatsPanelWidget.MinHeight,
            questTracker.Visible ? QuestTrackerWidget.TrackerHeight : 0,
            statusIconRow.Visible ? IconRowHeight : 0, statsPanel.RowsAt);
        mapCard.Height = Dim.Func(_ => Plan().MapCard);
        minimapView.Height = Dim.Func(_ => Plan().Minimap);
        questTracker.Y = Pos.Func(_ => 1 + Plan().Minimap);
        statusCard.Y = Pos.Func(_ => Plan().MapCard);
        statusCard.Height = Dim.Func(_ => Plan().StatusCard);
        statsPanel.Y = Pos.Func(_ => Plan().MapCard + 1);
        statsPanel.Height = Dim.Func(_ => Plan().Stats);
        statusIconRow.Y = Pos.Func(_ => Plan().MapCard + 1 + Plan().Stats);
        logCard.Y = Pos.Func(_ => Plan().MapCard + Plan().StatusCard);
        logCard.Height = Dim.Func(_ => Plan().LogCard);
        coloredLog.Y = Pos.Func(_ => Plan().MapCard + Plan().StatusCard + 1);
        coloredLog.Height = Dim.Func(_ => Plan().LogCard - SidebarLayout.CardChrome);
        int laidStatsHeight = -1;
        // Shift+S toggles verbose labels (persisted through UserSettings).
        mapView.StatusTrayVerboseToggleRequested += () => statusTray.ToggleVerbose();

        // F9 — biome JSON hot-reload. Re-reads Content/Biomes/*.json, regenerates
        // the current floor, and re-rolls mobs/loot via PopulateFloor.
        mapView.BiomeReloadRequested += () =>
        {
            // A floor built ahead of this point was built from the old JSON, and clearing the config
            // cache is unsafe while a worker reads it — so drop the prefetch and join it first.
            FloorPrefetch.Invalidate();
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
            mapTitleLabel.SetScheme(FloorThemeColor(turnManager.CurrentFloor));
            // Start the next floor again, now against the reloaded JSON. Without this the reload
            // silently switches the prefetch off for the rest of the floor.
            FloorPrefetch.Request(turnManager.CurrentFloor + 1);
            ToastQueue.Enqueue($"Biomes reloaded — {BiomeSystem.DisplayName}", Color.BrightCyan, ToastCategory.StatUp);
        };

        // Assigning View.Text re-runs text formatting and marks the view for redraw.
        // RefreshHud runs every frame while a stat bar tween is in flight, so writing
        // unchanged text would repaint the whole HUD each frame.
        static void SetTextIfChanged(View view, ReadOnlySpan<char> text)
        {
            if (!view.Text.AsSpan().SequenceEqual(text))
                view.Text = new string(text);
        }

        void RefreshHud()
        {
            using var prof = SAOTRPG.Systems.Diagnostics.Profiler.Begin("Hud.Refresh");
            // Trigger icon-row repaint each HUD tick so countdowns tick down.
            statusIconRow.SetNeedsDraw();
            using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Hud.StatsPanel")) statsPanel.Refresh();

            // A sidebar part that came or went re-lays the column out; Plan reads the flags.
            bool pinned = QuestSystem.PinnedQuest() != null;
            bool iconsOn = StatusIconMap.Collect(turnManager, iconScratch).Count > 0;
            if (pinned != questTracker.Visible || iconsOn != statusIconRow.Visible
                || statsPanel.PreferredHeight != laidStatsHeight)
            {
                questTracker.Visible = pinned;
                statusIconRow.Visible = iconsOn;
                laidStatsHeight = statsPanel.PreferredHeight;
                rightPanel.SetNeedsLayout();
            }

            // Eighth-block stat bar resolution (8x ASCII), with green/yellow/red
            // zones via StatBarHelper.ZoneColor. Bars read tweened "displayed"
            // values from MapView so HP/XP/SAT drift smoothly.
            mapView.SetPlayerBarTargets(player.CurrentHealth,
                player.CurrentExperience, turnManager.Satiety);
            int dispHp  = mapView.DisplayedPlayerHp;
            int dispXp  = mapView.DisplayedPlayerXp;
            int dispSat = mapView.DisplayedPlayerSat;

            // Every line is written into stack memory and handed to its HudLine, which copies it
            // only when it differs, so a refresh that changes nothing allocates nothing.
            Span<char> buf = stackalloc char[HudLineCapacity];

            // Row 0: HP | XP | SAT | Status effects (numerator uses tweened display).
            var w = new LineWriter(buf);
            w.Append("HP ");
            StatBarHelper.Write(ref w, dispHp, player.MaxHealth, HpBarWidth);
            w.Append(' '); w.Append(dispHp); w.Append('/'); w.Append(player.MaxHealth);
            w.Append(" | XP ");
            StatBarHelper.Write(ref w, dispXp, player.ExperienceRequired, XpBarWidth);
            w.Append(" Lv"); w.Append(player.Level);
            w.Append(" | SAT ");
            StatBarHelper.Write(ref w, dispSat, TurnManager.MaxSatiety, 6);
            StatusTagBuilder.Write(ref w, turnManager);
            DurabilityHelper.WriteWarningTags(ref w, player.Inventory);
            if (WeatherSystem.Current != WeatherType.Clear)
            {
                w.Append("  ["); w.Append(WeatherSystem.GetLabel()); w.Append(']');
            }
            if (saveFlash[0] > 0) { w.Append("  [Saved]"); saveFlash[0]--; }
            hpLabel.Set(w.Text);
            // HP zone color drives the row tint. Critical (<25%) escalates to
            // ColorSchemes.Danger so the existing low-HP redline behavior persists.
            var hpZone = StatBarHelper.ZoneColor(dispHp, player.MaxHealth);
            if (hpZone != lastHpZone[0])
            {
                lastHpZone[0] = hpZone;
                hpLabel.SetScheme(hpZone == Color.BrightRed
                    ? ColorSchemes.Danger
                    : (hpZone == Color.BrightYellow ? ColorSchemes.FromColor(Color.BrightYellow) : ColorSchemes.Body));
            }

            // Row 1: Floor context line
            var wpn = player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon);
            w = new LineWriter(buf);
            w.Append('F'); w.Append(turnManager.CurrentFloor);
            w.Append(" | "); w.Append(player.ColOnHand); w.Append('c');
            w.Append(" | WPN: ");
            if (wpn != null)
            {
                w.Append(wpn.Name);
                w.Append(' ');
                if (wpn is Weapon wpnWeapon && !string.IsNullOrEmpty(wpnWeapon.WeaponType))
                {
                    int bonus = turnManager.GetProficiencyBonus(wpnWeapon.WeaponType);
                    if (bonus > 0) { w.Append('+'); w.Append(bonus); }
                }
            }
            else w.Append("Fists");
            w.Append(" | Mobs: "); w.Append(turnManager.GetMonsterCount());

            // The danger compass the tips list describes: it points toward nearby threats, and
            // writes nothing with no monster on the floor, so it self-suppresses like the hint.
            Span<char> compass = stackalloc char[TurnManager.DangerCompassMaxLength];
            int compassLen = turnManager.WriteDangerCompass(compass);
            if (compassLen > 0) { w.Append(" | "); w.Append(compass[..compassLen]); }

            Span<char> clock = stackalloc char[GameClock.TimeOfDayMaxLength];
            w.Append(" | ");
            w.Append(clock[..GameClock.WriteTimeOfDay(turnManager.TurnCount, clock)]);
            string ctxHint = turnManager.GetContextHint();
            if (ctxHint.Length > 0) { w.Append(" | "); w.Append(ctxHint); }
            infoLabel.Set(w.Text);

            // Row 2 left: 10-slot quickbar. "N G" pairs separated by 1-col gutter
            // (29 cols total). The glyph encodes the slot's state (`·` when empty).
            w = new LineWriter(buf);
            for (int qs = 0; qs < QuickbarState.SlotCount; qs++)
            {
                var (glyph, _, _) = player.Quickbar.SlotDisplay(qs, player);
                // Slot number: 1..9 then 0 for slot 10.
                char slotDigit = qs == 9 ? '0' : (char)('1' + qs);
                if (qs > 0) w.Append(' ');
                w.Append(slotDigit); w.Append(glyph);
            }
            hotbarLabel.Set(w.Text);

            // Row 2 right: F1-F4 shrunk to 4-cell [FN] labels so quickbar fits.
            // Full skill name surfaces in the game log on use; cooldown turns
            // sit as a trailing ( N ) marker only when non-zero.
            w = new LineWriter(buf);
            for (int si = 0; si < turnManager.EquippedSkills.Length; si++)
            {
                var sk = turnManager.EquippedSkills[si];
                w.Append("[F"); w.Append(si + 1); w.Append(']');
                if (sk == null) { w.Append("·   "); continue; }
                int cd = turnManager.GetSkillCooldown(sk.Id);
                if (cd > 0) { w.Append('('); w.Append(cd); w.Append(')'); }
                else w.Append("   ");
                w.Append(' ');
            }
            if (turnManager.PostMotionDelay > 0) { w.Append("!DLY"); w.Append(turnManager.PostMotionDelay); }
            skillBarLabel.Set(w.Text);

            w = new LineWriter(buf);
            w.Append(" Inventory ("); w.Append(player.Inventory.Items.Count); w.Append(") ");
            SetTextIfChanged(inventoryBtn, w.Text);
            // The tracker repaints every refresh; the tray only when what it shows changed.
            questTracker.SetNeedsDraw();
            if (turnManager.TurnCount != minimapTurn[0])
            {
                minimapTurn[0] = turnManager.TurnCount;
                minimapView.SetNeedsDraw();
            }
            statusTray.Refresh();
            // The map is deliberately not marked here. Handing a frame to the terminal
            // blocks the loop — and therefore input — for as long as the console takes
            // to parse and repaint it, so a redraw per HUD tick costs responsiveness for
            // nothing when the map is unchanged. Player actions mark it dirty directly,
            // and the animation ticker covers time-based effects.
        }

        WireVisualEffects(turnManager, mapView);
        WireSystemEvents(mainWindow, turnManager, player, gameLog, coloredLog,
            mapView, minimapView, mapTitleLabel, saveFlash, saveSlot, RefreshHud);
        WireKeybindActions(turnManager, mapView, minimapView, player, gameLog, coloredLog,
            inventoryBtn, saveSlot, saveFlash, RefreshHud);
        WirePassiveSystems(turnManager, mapView, player, gameLog, RefreshHud);

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
        foreach (var helpLine in WelcomeHelpLines) gameLog.Log(Systems.Input.KeyHints.Resolve(helpLine));
        gameLog.Log("");

        WeatherSystem.RollWeather();
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
            // Fired once per fresh run at screen show, never from a key handler, so it cannot
            // stack; StorySystem's own fired-events set is the real idempotence.
            AppHost.AddOneShotTimeout(TimeSpan.FromMilliseconds(250), () =>
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

    // Apply Run Modifier start-of-run effects. Called once per new game
    // after player + inventory are wired up, before MapGenerator runs.
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

        // Hollow Ingress — sword skill unlocks are stripped by the turn code; announced here.
        if (RunModifiers.IsActive(RunModifier.HollowIngress))
        {
            log.LogSystem("  [Hollow Ingress] All Sword Skills locked. Earn them via combat.");
        }

        // Sword Art Only + Naked Ingress — equipment limits enforced by Inventory; announced here.
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

    private static Scheme FloorThemeColor(int floor) => ColorSchemes.FromColor(floor switch
    {
        <= 10 => Color.Green,
        <= 25 => Color.Yellow,
        <= 50 => Color.BrightRed,
        <= 75 => Color.Red,
        _     => Color.BrightMagenta,
    });
}
