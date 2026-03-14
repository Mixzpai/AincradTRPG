  **SUPER EXPERIMENTAL PLAY AT OWN RISK**
  
  
  AINCRAD TRPG — Project Documentation
  SAO-Themed Turn-Based ASCII Roguelike

  1. CORE SYSTEMS BREAKDOWN

  TURN LOOP
  ---------
  1. Player presses a movement/action key in MapView
  2. MapView fires an event (MoveRequested, InteractRequested, etc.)
  3. GameScreen routes the event to TurnManager
  4. TurnManager processes the player action:
     - Move: collision check, tile effects (traps, campfires, stairs)
     - Attack: damage calc with crit/dodge rolls
     - Interact: NPC dialogue, vendor shop
  5. TurnManager runs SimpleAI for each living mob
  6. TurnManager fires HudRefresh event
  7. GameScreen updates all HUD labels and redraws

  COMBAT
  ------
  Player attacks:
    damage = Attack - monster.Defense (minimum 1)
    crit chance based on Dexterity
    crit multiplier: 2x damage
  Monster attacks:
    damage = monster.Attack - player.Defense (minimum 1)
    dodge chance based on player Agility
  On kill:
    XP awarded with diminishing returns (halved at 3+ lvl diff, quartered at 5+)
    Col dropped
    Item drop chance

  MAP GENERATION
  --------------
  1. Fill grid with walls
  2. Place rooms (random size/position, reject overlaps)
  3. Connect rooms with L-shaped corridors
  4. Add clearings (open grassy areas with campfires)
  5. Place terrain features (trees, water, rocks, flowers)
  6. Place stairs up (farthest room from spawn)
  7. Spawn mobs via MobFactory (floor-tiered templates)
  8. Place traps (spike, teleport) away from spawn
  9. Place vendors and items

  VISIBILITY
  ----------
  - Radius-based fog of war (VisibilityRadius = 18)
  - Light falloff: tiles beyond FalloffThreshold (75%) are dimmed
  - Light sources (torches, campfires) have LightGlowRadius = 2
  - Explored tiles stay visible but dimmed (DarkGray)
  - Unexplored tiles are completely black

  MOB AI
  ------
  Per-tick decision for each mob:
  1. Calculate Chebyshev distance to player
  2. If distance <= mob.AggroRange: chase (pathfind toward player)
  3. If adjacent to player: attack
  4. If HP < 20% of max: flee (move away from player)
  5. Otherwise: wander randomly

  6. UI FRAMEWORK NOTES (Terminal.Gui v2)

  Terminal.Gui v2 key concepts used in this project:

  CUSTOM VIEWS
  - Subclass View and override OnDrawingContent()
  - Driver.SetAttribute(new Terminal.Gui.Attribute(fg, bg)) for colors
  - Driver.AddRune(new System.Text.Rune(ch)) for characters
  - Move(x, y) to position cursor within the view
  - SetNeedsDraw() to trigger redraw

  LAYOUT SYSTEM
  - Pos: Center(), Percent(), Bottom(view), AnchorEnd()
  - Dim: Fill(), Auto(), Percent()
  - Views are positioned relative to parent or sibling views

  MODAL DIALOGS
  - Application.Run(dialog) blocks until RequestStop()
  - Dialog class provides built-in button bar
  - MessageBox.Query() for simple confirmations

  TIMERS
  - Application.AddTimeout(TimeSpan, callback) for periodic updates
  - Returns token for cancellation
  - Used for animation cycling and auto-explore stepping

  COLOR SCHEMES
  - ColorScheme has 5 states: Normal, Focus, HotNormal, HotFocus, Disabled
  - Terminal.Gui.Attribute(Color fg, Color bg) — fully qualified to avoid
    System.Attribute ambiguity
  - All palettes centralized in ColorSchemes.cs

  INPUT HANDLING
  - KeyDown event on views
  - e.KeyCode for key identification
  - e.Handled = true to consume the event
  - Accepting event on Button for click handling
  - e.Cancel = true in Accepting to prevent default behavior

  DATA BINDING
  - ObservableCollection<T> with ListWrapper<T> for ListView
  - Manual refresh pattern: update data, call SetNeedsDraw()

  2. EXPANDABILITY GUIDE

  This section describes how to add common new features.

  ADDING A NEW MOB
  ----------------
  File: Map/MobFactory.cs
  1. Find the floor tier array (e.g. _floor1to5)
  2. Add a new MobTemplate record:
     new MobTemplate("Mob Name", 'x', Color.Green, 6)
     (name, symbol, color, aggro range)
  That's it. The factory randomly picks from the tier array.

  ADDING A NEW TILE TYPE
  ----------------------
  File: Map/TileType.cs
  1. Add entry to TileType enum
  File: UI/MapView.cs
  2. Add visual mapping in ResolveTileVisual() switch expression
  File: UI/MinimapView.cs
  3. Add glyph in GetTerrainGlyph() and color in GetTerrainColor()
  File: Map/MapGenerator.cs
  4. Add placement logic if it should spawn naturally

  ADDING A NEW LOG COLOR RULE
  ---------------------------
  File: UI/ColoredLogView.cs
  1. Add a tuple to the KeywordRules array:
     ("keyword", Color.SomeColor)
  Keywords are checked case-insensitively, first match wins.

  ADDING A NEW STAT TO THE DEATH SCREEN
  --------------------------------------
  File: UI/DeathScreen.cs
  1. Add a StatRow() line in BuildSummary():
     StatRow("New Stat", value)

  ADDING A NEW DIALOG
  -------------------
  1. Create UI/Dialogs/NewDialog.cs
  2. Static class with Show(Window, Player, ...) method
  3. Use ColorSchemes.Body/Title/Button for consistency
  4. Wire the open trigger in GameScreen (key event or button)

  ADDING A NEW LOG CATEGORY
  -------------------------
  File: UI/IGameLog.cs
  1. Add method (e.g. void LogLoot(string message))
  File: UI/GameLogView.cs
  2. Implement with prefix and LogCategory
  File: UI/StringGameLog.cs
  3. Implement (just _sb.AppendLine)
  File: UI/ColoredLogView.cs
  4. Add LogCategory enum entry and default color

  ADDING A NEW COLOR SCHEME
  -------------------------
  File: UI/Helpers/ColorSchemes.cs
  1. Add a new public static readonly ColorScheme field
  2. Define Normal, Focus, HotNormal, HotFocus, Disabled states
  3. Reference it anywhere as ColorSchemes.NewScheme

  ADDING A NEW EQUIPMENT SLOT
  ---------------------------
  File: Inventory/Core/EquipmentSlot.cs
  1. Add enum entry
  File: Inventory/Equipment/EquipmentSlotResolver.cs
  2. Add mapping in the resolver

  ADDING A NEW ITEM DEFINITION
  ----------------------------
  1. Find the appropriate Definitions/ file (or create one)
  2. Add a static factory method returning a configured item
  3. Reference it in MapGenerator or Vendor for spawning

  ADDING A NEW SCREEN
  -------------------
  1. Create UI/NewScreen.cs as a static class
  2. public static void Show(Window mainWindow) pattern
  3. mainWindow.RemoveAll() at the top
  4. Use ColorSchemes for all color definitions
  5. Add navigation from an existing screen

  ADDING A NEW BOSS
  -----------------
  File: Entities/Bosses/NewBoss.cs
  1. Extend Boss class
  2. Define phases with stat overrides
  3. Wire into MapGenerator for the appropriate floor

  ADDING A NEW PLAYER TITLE
  -------------------------
  File: Systems/TurnManager.cs
  1. Add a (Kills, Title) tuple to TitleThresholds array
     (sorted descending by kill count — first match wins)

  ADDING NEW TERRAIN FLAVOR TEXT
  ------------------------------
  File: Systems/TurnManager.cs
  1. Add a { TileType.X, new[] { "flavor1", "flavor2" } } entry
     to the TerrainFlavors dictionary
  2. FootstepChance (12%) controls how often flavor appears

  ADDING A NEW CAMPFIRE QUOTE
  ---------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the CampfireQuotes array

  ADDING A NEW TURN MILESTONE MESSAGE
  ------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to MilestoneMessages (use {0} for turn count)
  2. MilestoneInterval controls frequency (default: every 100 turns)

  ADDING A NEW AMBIENT ATMOSPHERE MESSAGE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the AmbientMessages array
  2. AmbientChance (4%) controls how often they appear per move

  ADDING A NEW DODGE FLAVOR LINE
  ------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the DodgeFlavors array (use {0} for monster name)

  ADDING A NEW FLOOR COMPLETION MESSAGE
  --------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the FloorCompleteMessages array

  ADDING A NEW FLOOR CLEARED MESSAGE
  -----------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the FloorClearedMessages array

  ADDING A NEW CRITICAL HIT FLAVOR LINE
  --------------------------------------
  File: Entities/Player.cs
  1. Add a string to the CritFlavors array (use {0}=player, {1}=monster, {2}=damage)

  ADDING A NEW MONSTER AGGRO ALERT
  ---------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the AggroAlerts array (use {0} for monster name)

  ADDING A NEW VENDOR GREETING
  ----------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the VendorGreetings array (use {0} for shop name)

  ADDING A NEW LOW HP ENCOURAGEMENT
  ----------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the LowHpEncouragements array

  ADDING A NEW STAIRCASE DISCOVERY MESSAGE
  -----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the StairsDiscoveryMessages array

  ADDING A NEW OVERKILL FLAVOR LINE
  ----------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the OverkillFlavors array (use {0} for bonus EXP)

  ADDING A NEW MONSTER FLEE ALERT
  --------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the FleeFlavors array (use {0} for monster name)

  ADDING A NEW INVENTORY FULL MESSAGE
  ------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the InventoryFullFlavors array

  ADDING A NEW NPC FALLBACK DIALOGUE LINE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the NpcFallbackDialogue array

  ADDING A NEW NORMAL ATTACK FLAVOR LINE
  ----------------------------------------
  File: Entities/Player.cs
  1. Add a string to the AttackFlavors array (use {0}=player, {1}=monster, {2}=damage)

  ADDING A NEW RARE LOOT FANFARE LINE
  -------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the RareLootFlavors array (use {0}=rarity, {1}=item name)

  ADDING A NEW TRAP SENSE FLAVOR LINE
  -------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the TrapSenseFlavors array

  ADDING A NEW POTION USE FLAVOR LINE
  ------------------------------------
  File: UI/TerminalGuiInventoryLogger.cs
  1. Add a string to the PotionFlavors array

  ADDING A NEW MONSTER DEFEAT FLAVOR LINE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the DefeatFlavors array (use {0} for monster name)

  ADDING A NEW ITEM PICKUP FLAVOR LINE
  --------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the PickupFlavors array (use {0} for item name)

  ADDING A NEW WALL BUMP FLAVOR LINE
  ------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the WallBumpFlavors array

  ADDING A NEW FOOD USE FLAVOR LINE
  -----------------------------------
  File: UI/TerminalGuiInventoryLogger.cs
  1. Add a string to the FoodFlavors array

  ADDING A NEW EQUIP FLAVOR LINE
  --------------------------------
  File: UI/TerminalGuiInventoryLogger.cs
  1. Add a string to the EquipFlavors array (use {0}=item, {1}=slot)

  ADDING A NEW PASSIVE REGEN FLAVOR LINE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the RegenFlavors array

  ADDING A NEW DEATH SCREEN RUN GRADE
  -------------------------------------
  File: UI/DeathScreen.cs
  1. Add a (MinScore, Grade, Comment) tuple to RunGrades array
     (sorted descending by min score — first match wins)

  ADDING A NEW AUTO-EXPLORE STOP FLAVOR LINE
  --------------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to AutoExploreEnemyFlavors (enemy interrupt)
  2. Add a string to AutoExploreItemFlavors (items found)
  3. Add a string to AutoExploreDoneFlavors (fully explored)

  ADDING A NEW TELEPORT LANDING FLAVOR LINE
  -------------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the TeleportLandingFlavors array

  ADDING A NEW DOOR PASSAGE FLAVOR LINE
  ---------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the Door entry in the TerrainFlavors dictionary

  ADDING A NEW CAMPFIRE FULL HP FLAVOR LINE
  -------------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the CampfireFullHpFlavors array

  ADDING A NEW FLOOR MOB COUNT ANNOUNCEMENT
  -------------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the FloorMobCountFlavors array (use {0} for mob count)

  ADDING A NEW PLAYER DEATH FLAVOR LINE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the DeathFlavors array

  ADDING A NEW STRONG BLOCK FLAVOR LINE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the BlockFlavors array

  ADDING A NEW SPIKE TRAP FLAVOR LINE
  -------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the SpikeTrapFlavors array (use {0} for damage amount)

  ADDING A NEW EMPTY GROUND FLAVOR LINE
  ----------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the EmptyGroundFlavors array

  ADDING A NEW VENDOR FAREWELL LINE
  -----------------------------------
  File: UI/GameScreen.cs
  1. Add a string to the farewells array in the VendorInteraction handler
     (use {0} for vendor name)

  ADDING A NEW LEVEL-UP FLAVOR LINE
  -----------------------------------
  File: Entities/Player.cs
  1. Add a string to the LevelUpFlavors array

  ADDING MOUNTAIN/ROCK TERRAIN FLAVOR
  -------------------------------------
  File: Systems/TurnManager.cs
  1. Add a string to the Mountain or Rock entry in the TerrainFlavors dictionary

  ADDING A NEW DIFFICULTY TIER
  ----------------------------
  File: Systems/TurnManager.cs
  1. Add an entry to each of: MobStatScale, XpScale, RegenInterval arrays
     (index must match the DifficultyScreen radio group order)
  File: UI/DifficultyScreen.cs
  2. Add the tier name + description in BuildDifficultyData()
  File: UI/GameScreen.cs
  3. Add the tier name to the diffNames array

  ADDING A NEW POISONOUS MOB
  ---------------------------
  File: Map/MobFactory.cs
  1. Set Poison: true in the MobTemplate constructor:
     new MobTemplate("Toxic Snake", 's', Color.Green, 5, Poison: true)

  ADDING A NEW STATUS EFFECT
  ---------------------------
  File: Systems/TurnManager.cs
  1. Add tracking fields (_effectTurnsLeft, _effectDamagePerTick)
  2. Add a TickEffect() method (see TickPoison() or TickBleed() for reference)
  3. Call TickEffect() from both turn advancement points and ProcessRest()
  4. Reset in AscendFloor()
  5. Expose IsFoo / FooTurnsLeft for HUD display
  6. Add to antidote handler if it should be curable

  ADDING A NEW BLEEDING MOB
  --------------------------
  File: Map/MobFactory.cs
  1. Set Bleed: true in the MobTemplate constructor:
     new MobTemplate("Sharp Claw", 'c', Color.Red, 5, Bleed: true)

  ADDING A NEW CONSUMABLE TO VENDOR
  ----------------------------------
  File: Entities/Vendor.cs
  1. Add a ShopStock.Add(...) line in GenerateStock()
  2. Optionally gate behind a floor check (e.g. if (floor >= 3))

  3. BUILD & RUN

  REQUIREMENTS
  - .NET 8.0 SDK
  - Windows (Win32 window sizing in Program.cs)

  BUILD
    cd SAOTRPG
    dotnet build

  RUN
    dotnet run

  RUN WITH DEBUG MODE
    dotnet run -- --debug
    (Enables debug difficulty tier, verbose logging to debug.log)

  DEBUG LOG
    The debug.log file in the project root captures:
    - Session start/end timestamps
    - Screen transitions
    - Input events
    - Combat calculations
    - Performance timers
    - Errors and exceptions
