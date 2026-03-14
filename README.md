================================================================================
  AINCRAD TRPG — Project Documentation
  SAO-Themed Turn-Based ASCII Roguelike
================================================================================

  Engine:     C# / .NET 8.0 Console Application
  UI:         Terminal.Gui v2.0.0 (TUI framework)
  Platform:   Windows (with Win32 window sizing)
  Dependency: Terminal.Gui 2.0.0 (sole NuGet package)

================================================================================
  TABLE OF CONTENTS
================================================================================

  1. Overview
  2. Folder Structure
  3. Architecture & Design Patterns
  4. File Reference (every .cs file)
  5. Core Systems Breakdown
  6. UI Framework Notes (Terminal.Gui v2)
  7. Data Flow & Event Wiring
  8. Expandability Guide
  9. Build & Run

================================================================================
  1. OVERVIEW
================================================================================

  AincradTRPG is a Sword Art Online-themed turn-based ASCII roguelike.
  The player creates a character, allocates stats, and ascends through
  procedurally generated floors in the floating castle Aincrad (Floor 1 → 100).

  Core gameplay loop:
    Title Screen -> Difficulty Select -> Character Creation -> Gameplay
    Gameplay: Explore -> Fight Mobs -> Loot -> Level Up -> Ascend Stairs
    Death: Run Summary -> Return to Title

Core Gameplay:
    - Procedural dungeon generation with rooms, corridors, and clearings
    - Fog of war with visibility radius and light falloff
    - Per-mob aggro ranges and simple AI (chase, flee, wander)
    - Inventory system with equipment, consumables, and materials
    - Shop system with floor-scaling vendor inventories
    - Traps (spike, teleport), campfire healing, and stairs confirmation
    - 5 floor tiers with unique mob sets and scaling difficulty
    - 8 difficulty levels (Story through Unwinnable) scaling mob stats, XP, regen
    - Hardcore permadeath toggle (death = quit, no retry)
    - Auto-explore with step delay (X key)
    - Rest command (R key) — skip 3 turns, heal 3 HP/turn, blocked near enemies
    - Boss gate — stairs sealed until floor boss is defeated

  Combat & Status Effects:
    - Turn-based combat with crits, dodges, and XP diminishing returns
    - Monster critical hits (mobs roll crit chance using CriticalRate/CriticalHitDamage)
    - Kill streak callouts (Double Kill, Triple Kill, Rampage, etc.)
    - Kill-based title progression (Adventurer -> Blooded -> ... -> Legend)
    - Dodge streak callouts (Matrix mode at 3, Untouchable at 5, Phantom at 7+)
    - Poison status effect (Nepenthes, Scavenger Toad, Cursed Blade)
    - Bleed status effect (Kobold Warrior, Killer Mantis, Undead Knight, Dragon Knight)
    - Antidote item — cures poison and bleed (vendor stock + 40% drop from poison mobs)
    - Trap proximity sense (Dexterity-based chance to detect adjacent traps)
    - Monster threat level in Look/tile info (Deadly/Dangerous/Strong/Even/Weaker/Trivial)
    - Near-death mob indicators ("One more hit!" at <=10% HP)

  UI & HUD:
    - Color-coded game log with keyword-based highlighting
    - Minimap with current floor + exploration % (F1 42%)
    - HP/XP bars and stat panels
    - Low HP warning — action bar turns red below 25% health
    - Col currency display (¢) in the action bar HUD
    - Inventory button shows live item count
    - Equipped weapon name shown in HUD ([Iron Sword] or [Fists])
    - Live monster count on current floor (Mobs:5)
    - HUD status indicators: [POISON:N], [BLEED:N]
    - Stairs tile info shows [SEALED] or [OPEN] based on boss status
    - Death screen shows killer name, run rating (S/A/B/C/D grade)
    - "Press Enter to continue" hint on death screen
    - Max 1 aggro alert per turn to prevent log spam
    - Health-based player '@' color (yellow→red→pulsing at critical HP)
    - Damage numbers flash on map (floating combat numbers)
    - Corpse markers ('†') fade at death locations (red→gray color fade)
    - Loot sparkle ('!' flash) on ground items
    - Auto-save indicator ("[Saved]") on floor descent

  Map Visual Effects:
    - Fog of war edge gradient (soft boundary + ░▒▓ density shading)
    - Ambient particles (wind dots on grass, mist on water, campfire embers)
    - Door open/closed visual state ('▌' closed, '╌' opened, affects LOS)
    - Terrain transition borders (',' glyphs at water/lava edges)
    - Wall shadow casting (1-tile DarkGray shadow below walls)
    - Shrine aura glow (Magenta tint within 2 tiles of shrines)
    - Water edge foam (BrightCyan shoreline on land-adjacent water)
    - Water lily pads ('●' green on ~10% of shallow water tiles)
    - Room lighting variation (per-region color tints on floor tiles)
    - Explored room glow (warm Yellow tint on well-explored floors)
    - Depth-based tinting (progressive color dimming at medium range)
    - Wall edge shading (bright walls facing open floor, dim interior walls)
    - Cobweb decorations (gray '%' on wall corner tiles)
    - Lava danger zone (DarkGray bg within 2 tiles of lava)
    - SAO shatter particles ('◇' diamond burst on monster death)
    - Kill streak flash (magenta '@' + '★' burst on multi-kills)
    - Interactable sparkle (✦/✧ pulse on doors, stairs, shrines)

  Flavor Text & Atmosphere:
    - SAO-themed floor descent banners with per-floor flavor text
    - Time-of-day atmosphere on floor descent (real clock: dawn/morning/noon/etc.)
    - Random gameplay tips on the title screen
    - Personalized welcome messages with player name
    - Idle atmosphere flavor text after standing still 10+ turns
    - Environmental ambient messages (~4% chance per move)
    - Ambient sound text (~1% per turn near water/campfire/wind)
    - Turn milestone callouts every 100 turns
    - Floor completion message at 100% explored
    - Floor cleared message when all monsters defeated
    - Floor entry mob count announcement
    - Staircase discovery announcement (one-time when first spotted)

  Flavor Text — Combat:
    - Varied player attack flavor (5 lines)
    - Varied critical hit flavor (5 lines: Devastating, Perfect Strike, etc.)
    - Varied combat dodge/miss flavor (6 lines)
    - Varied combat strong block flavor (5 lines when defense absorbs 50%+)
    - Overkill flavor variety (Obliterated, Annihilated, Destroyed, etc.)
    - Monster defeat flavor variety (5 lines: "crumbles to dust", "shatters into light")
    - Monster aggro alerts when enemies first spot the player
    - Monster flee alerts when enemies start running at low HP
    - Low HP survival encouragement (~40% chance when hit below 25%)
    - First kill celebration message

  Flavor Text — Items & Interaction:
    - Rare/Epic loot drop fanfare with star prefix and glow descriptions
    - Item pickup flavor variety (4 lines)
    - Healing potion use flavor (5 atmospheric lines)
    - Food consumption flavor (5 atmospheric lines)
    - Equipment equip flavor variety (4 lines with item/slot name)
    - Inventory full warning variety (4 messages)
    - Empty ground pickup flavor (4 lines when pressing G with nothing underfoot)
    - Vendor greeting variety (5 random lines with shop name)
    - Vendor farewell on shop close (5 goodbye lines)
    - NPC fallback dialogue variety (6 random lines)

  Flavor Text — Movement & Environment:
    - Terrain-aware footstep flavor (water, grass, flowers, mountain, rock — 12% chance)
    - Campfire rest quotes — random SAO-flavored lines on heal
    - Full-health campfire message variety (5 lines)
    - Door passage flavor (4 lines, 12% chance)
    - Wall bump flavor (10% chance for witty one-liner)
    - Spike trap flavor variety (5 lines with damage amount)
    - Teleport trap landing flavor (5 disorientation lines)
    - Auto-explore stop flavor variety (enemy, items, done — 4 lines each)

  Flavor Text — Progression:
    - Level up fanfare with full HP restore and decorated log banner
    - Level-up motivational flavor (5 lines inside level-up banner)
    - Passive regen notification flavor (30% chance per tick, 5 lines)
    - Boss defeat fanfare with decorated log banner and stairs-open announcement
    - Player death message variety (5 dramatic lines)

================================================================================
  2. FOLDER STRUCTURE
================================================================================

  SAOTRPG/
  |
  |-- Program.cs                          Entry point, Win32 window setup
  |-- SAOTRPG.csproj                      Project file (.NET 8, Terminal.Gui)
  |-- SAOTRPG.sln                         Solution file
  |-- DOCUMENTATION.txt                   This file
  |-- debug.log                           Runtime debug output
  |
  |-- Entities/                           Game entities
  |   |-- Entity.cs                       Abstract base (position, stats, visuals)
  |   |-- IStatModifiable.cs              Interface for stat modification
  |   |-- Player.cs                       Player character (inventory, combat, XP)
  |   |-- Monster.cs                      Abstract monster (XP/col yields)
  |   |-- Boss.cs                         Boss with phase system
  |   |-- Mob.cs                          Standard mob (aggro range, appearance)
  |   |-- NPC.cs                          Abstract NPC (dialogue, interaction)
  |   |-- PC.cs                           Recruitable NPC
  |   |-- Vendor.cs                       Shop NPC (floor-scaled inventory)
  |   |-- WorldSpawn.cs                   Background decoration NPC
  |   |-- Bosses/
  |       |-- IllfangTheKobaldLord.cs     Floor 1 boss (multi-phase)
  |
  |-- Inventory/                          Inventory management system
  |   |-- Core/
  |   |   |-- Inventory.cs                Add/remove, equip/unequip, use, capacity
  |   |   |-- EquipmentSlot.cs            9 equipment slots enum
  |   |-- Equipment/
  |   |   |-- IEquipmentSlotResolver.cs   Interface for slot resolution
  |   |   |-- EquipmentSlotResolver.cs    Default string-to-slot mappings
  |   |-- Events/
  |   |   |-- InventoryEventArgs.cs       Event args (Item, Equipment, Consumable)
  |   |   |-- InventoryEvents.cs          Event hub for inventory changes
  |   |-- Logging/
  |   |   |-- IInventoryLogger.cs         Logging interface
  |   |   |-- ConsoleInventoryLogger.cs   Console output logger
  |   |-- Documentation/
  |       |-- Inventory.txt               Detailed design document
  |
  |-- Items/                              Item hierarchy
  |   |-- BaseItem.cs                     Abstract base (name, value, rarity)
  |   |-- abstract_BaseItem.cs            Legacy abstract base
  |   |-- StackableItem.cs               Stackable items with stacking logic
  |   |-- StatType.cs                     Stat enum (Health, Attack, Defense, etc.)
  |   |-- StatEffect.cs                   Single stat modification definition
  |   |-- StatModifierCollection.cs       Fluent API effect collection
  |   |
  |   |-- Equipment/
  |   |   |-- Equipment.cs                Abstract equipment (level req, bonuses)
  |   |   |-- EquipmentBase.cs            Alt base supporting IStatModifiable
  |   |   |-- Weapon.cs                   Attack power, damage type, speed, range
  |   |   |-- Armor.cs                    Defense, slot, weight
  |   |   |-- Accessory.cs               Accessory slot bonuses
  |   |
  |   |-- Consumables/
  |   |   |-- Consumable.cs               Abstract consumable with effects
  |   |   |-- Potion.cs                   Healing/buff with cooldowns
  |   |   |-- Food.cs                     Regen over time
  |   |   |-- DamageItem.cs              Throwable AoE damage
  |   |
  |   |-- Materials/
  |   |   |-- Material.cs                 Abstract crafting material
  |   |   |-- Gathered.cs                 Gathering drops (location, level req)
  |   |   |-- MobDrop.cs                  Monster drops (drop rate, boss flag)
  |   |
  |   |-- Definitions/                    Item data definitions (static catalogs)
  |   |   |-- AccessoryDefinitions.cs
  |   |   |-- ArmorDefinitions.cs
  |   |   |-- DamageItemDefinitions.cs
  |   |   |-- FoodDefinitions.cs
  |   |   |-- GatheredDefinitions.cs
  |   |   |-- MobDropDefinitions.cs
  |   |   |-- PotionDefinitions.cs
  |   |   |-- Weapons/
  |   |       |-- WeaponDefinitions.cs    Base weapon defs
  |   |       |-- AxeDefinitions.cs
  |   |       |-- BowDefinitions.cs
  |   |       |-- DaggerDefinitions.cs
  |   |       |-- KatanaDefinitions.cs
  |   |       |-- MaceDefinitions.cs
  |   |       |-- OneHandedSwordDefinitions.cs
  |   |       |-- RapierDefinitions.cs
  |   |       |-- ShieldDefinitions.cs
  |   |       |-- SpearDefinitions.cs
  |   |       |-- StaffDefinitions.cs
  |   |       |-- TwoHandedSwordDefinitions.cs
  |   |
  |   |-- Documentation/
  |       |-- DESIGN_CHOICES.txt          Items system architecture doc
  |
  |-- Map/                                Dungeon generation & management
  |   |-- Tile.cs                         Single tile (type, occupant, items)
  |   |-- TileType.cs                     30+ tile types enum (grass, wall, etc.)
  |   |-- Room.cs                         Room struct for generation
  |   |-- GameMap.cs                      Tile grid, fog of war, visibility
  |   |-- Camera.cs                       Viewport camera (centering, clamping)
  |   |-- MapGenerator.cs                 Procedural floor generator
  |   |-- MobFactory.cs                   Floor-tiered mob spawning
  |
  |-- Systems/                            Core game logic
  |   |-- TurnManager.cs                  Turn loop, combat, traps, stairs, XP
  |   |-- SimpleAI.cs                     Mob AI (aggro, chase, flee, wander)
  |
  |-- UI/                                 Terminal.Gui screens & components
      |-- TitleScreen.cs                  Main menu with ASCII art
      |-- DifficultyScreen.cs             Difficulty + hardcore selection
      |-- CharacterCreationScreen.cs      Identity input + stat allocation
      |-- GameScreen.cs                   Main gameplay (wires all components)
      |-- DeathScreen.cs                  Game over + run summary
      |-- OptionsScreen.cs               Placeholder options
      |-- MapView.cs                      Dungeon renderer (custom View)
      |-- MinimapView.cs                  Scaled floor overview (custom View)
      |-- ColoredLogView.cs              Keyword-colored log (custom View)
      |-- GameLogView.cs                  Log adapter (IGameLog -> ColoredLogView)
      |-- IGameLog.cs                     Log output interface
      |-- StringGameLog.cs               StringBuilder log sink
      |-- TerminalGuiInventoryLogger.cs   Inventory -> game log bridge
      |-- NavigationHelper.cs             W/S key navigation helper
      |-- DebugLogger.cs                  File-based debug logger
      |-- DebugMode.cs                    --debug flag detection
      |
      |-- Dialogs/                        Extracted modal dialogs
      |   |-- HelpDialog.cs               Keybindings + map legend (H key)
      |   |-- InventoryDialog.cs          Inventory management (I key)
      |   |-- StatsDialog.cs              Stat allocation (P key)
      |   |-- ShopDialog.cs               Buy/sell vendor interface
      |   |-- KillStatsDialog.cs          Run statistics (K key)
      |
      |-- Helpers/                        Shared UI utilities
          |-- BarBuilder.cs               Gradient bar renderer (HP/XP/SAT)
          |-- BoxDrawing.cs               Unicode box-drawing character library
          |-- ColorSchemes.cs             Centralized color palette
          |-- MapEffects.cs              Static map rendering post-effects
          |-- StatusTagBuilder.cs         Compact status effect tag builder

================================================================================
  3. ARCHITECTURE & DESIGN PATTERNS
================================================================================

  EVENT-DRIVEN ARCHITECTURE
  -------------------------
  The game uses events to decouple systems. MapView fires input events,
  GameScreen wires them to TurnManager, which fires state-change events
  back to the UI. Inventory raises events through InventoryEvents, which
  TerminalGuiInventoryLogger translates into log messages.

    MapView.PlayerMoveRequested -> GameScreen -> TurnManager.ProcessPlayerMove
    MapView.InventoryRequested -> GameScreen -> InventoryDialog.Show()
    TurnManager.TurnCompleted  -> GameScreen -> RefreshHud()
    TurnManager.StairsConfirmRequested -> GameScreen -> ShowStairsDialog

  INTERFACE ABSTRACTIONS
  ----------------------
  IGameLog           Two implementations: GameLogView (live UI), StringGameLog
                     (character creation). Systems write to IGameLog without
                     knowing which display is active.

  IInventoryLogger   Decouples inventory events from display. Console logger
                     for testing, TerminalGuiInventoryLogger for gameplay.

  IEquipmentSlotResolver   Maps equipment types to slots. Default resolver
                           included, swappable for custom slot rules.

  IStatModifiable    Entities that can have stats modified by items.

  DATA-DRIVEN DESIGN
  ------------------
  MobFactory         Uses MobTemplate records: add a mob by adding one line.
  ColoredLogView     Keyword-color rules as a static array of tuples.
  StatsDialog        StatDef records: add a stat by adding one tuple.
  DifficultyScreen   Parallel arrays of names/descriptions, auto-indexed.
  Item Definitions   Static catalogs in Definitions/ folder.

  FACTORY PATTERN
  ---------------
  MobFactory.CreateMobsForFloor()   Spawns floor-appropriate mobs from templates.
  Vendor                            Generates inventory based on floor number.
  MapGenerator                      Procedural floor generation with rooms/corridors.

  CENTRALIZED THEMING
  -------------------
  ColorSchemes.cs defines 8 shared palettes (Button, Gold, Dim, Body, Title,
  Danger, MenuFrame, MenuButton). Every UI file references these instead of
  creating inline ColorScheme objects. Changing a color in one place updates
  the entire game.

  EXTRACTED HELPERS
  -----------------
  BarBuilder.BuildGradient() Gradient bar with █▓▒░· sub-cell precision.
  NavigationHelper          W/S key menu navigation, shared across screens.
  CreateMenuButton()        Factory in TitleScreen for consistent menu items.
  CreateField()             Factory in CharacterCreationScreen for form fields.
  StatRow()                 Factory in DeathScreen for summary card rows.

================================================================================
  4. FILE REFERENCE
================================================================================

  74 source files organized into 6 top-level modules:

  ENTITIES (11 files)
  -------------------
  Entity.cs               Abstract base for all entities. Position (X, Y),
                          stats (HP, Attack, Defense), level, symbol, color.
                          Provides TakeDamage(), Heal(), IsDefeated.

  IStatModifiable.cs      Interface: ModifyStat(StatType, int).

  Player.cs               The player character. Manages inventory, equipment,
                          combat (Attack with crit chance), XP/leveling,
                          stat points, col currency. CreateNewPlayer() factory.
                          SpendSkillPoints() for allocation.

  Monster.cs              Abstract monster. XPValue, ColValue for rewards.
                          AttackPlayer() method for combat.

  Boss.cs                 Extends Monster with phase system, boss title,
                          and phase-specific stat changes.

  Mob.cs                  Standard mob entity. AggroRange (int, default 6),
                          SetAppearance(char, Color) for visual customization.
                          Used by MobFactory for all non-boss enemies.

  NPC.cs                  Abstract NPC with Name, Dialogue, Interact().

  PC.cs                   Player-recruitable NPC. IsRecruited flag.

  Vendor.cs               Shop NPC. GenerateInventory(floor) creates
                          floor-appropriate items for buying.

  WorldSpawn.cs           Non-interactive background NPC for atmosphere.

  IllfangTheKobaldLord.cs Floor 1 boss. Multi-phase with stat scaling.

  ITEMS (26 files)
  ----------------
  BaseItem.cs             Abstract: Name, Value, Rarity, Description.
  StackableItem.cs        Abstract: MaxStack, CurrentStack, CanStack().
  StatType.cs             Enum: Health, Attack, Defense, Vitality, Strength,
                          Endurance, Dexterity, Agility, Intelligence.
  StatEffect.cs           Record: StatType, Potency, Duration, IsPercentage.
  StatModifierCollection  List<StatEffect> with fluent .Add() chaining.

  Equipment/Weapon.cs     Damage, WeaponType, AttackSpeed, Range.
  Equipment/Armor.cs      Defense, ArmorSlot, Weight.
  Equipment/Accessory.cs  Various stat bonuses in accessory slots.
  Equipment/EquipmentBase Base with stat modifier support.

  Consumables/Potion.cs   HealAmount, BuffType, Cooldown.
  Consumables/Food.cs     RegenAmount, Duration, RegenPerTick.
  Consumables/DamageItem  Damage, AoERadius, ElementType.

  Materials/Gathered.cs   GatherLocation, LevelRequired.
  Materials/MobDrop.cs    DropRate (float), IsBossDrop.

  Definitions/ (13 files) Static item catalogs. Each file contains factory
                          methods returning pre-configured items. Weapon defs
                          split by type (Axe, Bow, Dagger, Katana, etc.)

  INVENTORY (6 files)
  -------------------
  Core/Inventory.cs       Central manager. AddItem(), RemoveItem(),
                          EquipItem(), UnequipItem(), UseConsumable().
                          Capacity tracking, equipped items dictionary.

  Core/EquipmentSlot.cs   Enum: Weapon, Head, Chest, Legs, Feet,
                          RightRing, LeftRing, Bracelet, Necklace.

  Events/InventoryEvents  Event hub: ItemAdded, ItemRemoved, ItemEquipped,
                          ItemUnequipped, ConsumableUsed.

  Logging/IInventoryLogger  Interface: LogItemAdded, LogItemEquipped, etc.

  MAP (7 files)
  -------------
  TileType.cs             30+ tile types: Floor, Wall, Grass, GrassTall,
                          GrassSparse, Flowers, Tree, TreePine, Bush, Water,
                          WaterDeep, Mountain, Rock, Path, Door, StairsUp,
                          StairsUp, TrapSpike, TrapTeleport, Campfire, etc.

  Tile.cs                 Single tile: Type, Occupant (Entity), Items list,
                          IsWalkable, IsExplored, IsVisible, HasItems.

  Room.cs                 Rectangle struct with Intersects() for generation.

  GameMap.cs              2D tile grid. InBounds(), GetTile(), IsExplored(),
                          IsVisible(), UpdateVisibility() with radius-based
                          fog of war.

  Camera.cs               Viewport offset. CenterOn(x, y, mapW, mapH).
                          Clamps to prevent showing out-of-bounds tiles.

  MapGenerator.cs         Procedural generation:
                          - Room placement with overlap rejection
                          - L-shaped corridor connections
                          - Clearings with campfires (67% chance)
                          - Trap placement (4 + floor*2 + rand, 8-tile spawn buffer)
                          - Mob spawning via MobFactory
                          - Stairs, vendors, items

  MobFactory.cs           5 floor tiers, each with 5 mob templates:
                          Floors 1-5:   Kobold Scout, Forest Wolf, etc.
                          Floors 6-10:  Iron Golem, Shadow Lurker, etc.
                          Floors 11-20: Flame Sprite, Sand Worm, etc.
                          Floors 21-50: Frost Wraith, Storm Eagle, etc.
                          Floors 51+:   Void Walker, Abyssal Serpent, etc.
                          Uses record MobTemplate(Name, Symbol, Color, Aggro).

  SYSTEMS (2 files)
  -----------------
  TurnManager.cs          Core turn loop. Processes player actions, then
                          runs AI for all mobs. Handles:
                          - Movement with collision detection
                          - Combat (attack, crit, dodge, damage)
                          - XP with diminishing returns (halved at 3+ level diff)
                          - Loot drops on kill
                          - Kill streak tracking (Double/Triple/Quad/Rampage/Unstoppable)
                          - Kill-based title progression (9 tiers, Adventurer to Legend)
                          - Near-death mob indicators (dramatic callouts at <=25% HP)
                          - Trap effects (spike damage, teleport)
                          - Campfire healing (15 + floor * 5 HP) with rest quotes
                          - Terrain footstep flavor (water, grass, flowers, etc.)
                          - Turn milestone callouts every 100 turns
                          - Stairs confirmation dialog
                          - Floor transition with SAO-themed flavor text (floors 2-10+)
                          - Idle flavor text (atmospheric lines after 10+ still turns)
                          Events: TurnCompleted, DamageDealt, MonsterKilled,
                          FloorChanged, PlayerDied, GameWon,
                          VendorInteraction, StairsConfirmRequested

  SimpleAI.cs             Per-mob AI using Chebyshev distance:
                          - Idle: wander randomly
                          - Aggro: chase player if within AggroRange
                          - Flee: retreat when low HP (< 20%)
                          - Attack: deal damage when adjacent
                          Variable aggro per mob (scouts 8, golems 3, etc.)

  UI (18 files + 6 in subfolders)
  --------------------------------
  TitleScreen.cs          ASCII art banner ("AINCRAD" / "TRPG"), menu buttons,
                          animated star field, random gameplay tips in footer.
                          CreateMenuButton() / CreateMenuSeparator() factories.
                          Tips[] array — add new tips by adding a string.

  DifficultyScreen.cs     8 difficulty tiers + debug mode. RadioGroup selector,
                          dynamic description. Hardcore toggle with flavor
                          popups for wild combos (Story+Hardcore, etc.)
                          W/S wrapping navigation wired across all controls.

  CharacterCreationScreen Phase 1: First/Last/Gender fields with validation.
                          Phase 2: Stat allocation with live preview.
                          CreateField() factory for form field pairs.

  GameScreen.cs           Main gameplay compositor (~570 lines).
                          Wires: MapView, MinimapView, ColoredLogView, HUD
                          labels, action bar. Handles dialog opens, HUD
                          refresh, auto-explore timer, event subscriptions.
                          Low HP warning: bar turns red when HP <= 25%.
                          Named constants: AnimationIntervalMs, AutoExploreStepMs.

  DeathScreen.cs          "YOU DIED" banner, player identity line, boxed run
                          summary (floor, kills, turns, col, items, level).
                          StatRow() factory for easy summary expansion.

  MapView.cs              Custom View with OnDrawingContent() override.
                          Per-character colored rendering via Driver.SetAttribute
                          + Driver.AddRune. ResolveTileVisual() extracts tile
                          appearance. GetDimColor() for explored-but-dark tiles.
                          Light sources (torches, campfires) with glow radius.
                          Named constants: VisibilityRadius=18, FalloffThreshold,
                          LightGlowRadius=2.

  MinimapView.cs          Custom View. Auto-scales full map into viewport.
                          3-pass rendering: exploration state -> entities -> terrain.
                          Switch-expression lookup tables for terrain glyphs/colors.

  ColoredLogView.cs       Custom View. Data-driven keyword color rules:
                          "CRITICAL HIT" -> BrightMagenta, "defeated!" -> BrightYellow,
                          etc. Category fallback dictionary. Scrolling with
                          Up/Down/PageUp/PageDown, auto-scroll on new entries.

  GameLogView.cs          IGameLog adapter: routes Log/LogCombat/LogSystem
                          to ColoredLogView with prefix tags ([!], [*]).

  IGameLog.cs             Interface: Log(), LogCombat(), LogSystem().
                          Expandability note: add LogLoot() etc. by adding
                          to interface + both implementations.

  StringGameLog.cs        StringBuilder sink for character creation phase.

  TerminalGuiInventoryLogger  Bridges IInventoryLogger to IGameLog. Translates
                          inventory events into readable log messages.

  NavigationHelper.cs     EnableGameNavigation(): W/S/Arrow focus cycling.
                          RadioGroup-aware (lets arrows control selection).
                          ButtonScheme delegates to ColorSchemes.Button.

  DebugLogger.cs          Writes to debug.log. Categories: SESSION, INPUT,
                          SCREEN, GAME, COMBAT, SYSTEM, PERF, ERROR.
                          StartTimer/EndTimer for performance profiling.

  DebugMode.cs            Static IsEnabled flag. Set by --debug CLI arg.

  Dialogs/HelpDialog.cs   Full keybinding reference: Movement, Actions,
                          Interaction, Map Legend. Opens with H key.

  Dialogs/InventoryDialog  Item list with rarity tags ([E]pic, [R]are, [U]ncommon).
                          Use, Equip, Drop actions. RefreshAfterChange() DRY helper.

  Dialogs/StatsDialog.cs  StatDef record array. Add stats by adding a tuple.
                          Live stat preview with effects column.

  Dialogs/ShopDialog.cs   Buy/sell mode toggle. CloneShopItem() for safe purchase.
                          CalcSellPrice() and FormatItemInfo() helpers.

  Helpers/BarBuilder.cs   BuildGradient(current, max, width) with █▓▒░· sub-cell.
                          Build(), BuildHp(), BuildXp() convenience wrappers.

  Helpers/ColorSchemes.cs 8 centralized palettes: Button, Gold, Dim, Body,
                          Title, Danger, MenuFrame, MenuButton. Every UI
                          file references these. Change once, update everywhere.

  Helpers/MapEffects.cs   Static helpers for map rendering post-effects.
                          Extracted from MapView for readability. 20+ methods:
                          GetFogEdgeColor() — fog edge gradient near unexplored
                          GetFogDensityGlyph() — ░▒▓ fog density shading
                          GetAmbientParticle() — wind/mist/ember particles
                          GetTransitionBorder() — biome transition glyphs
                          IsInWallShadow() — 1-tile shadow below walls
                          GetShrineDistance() — mystic glow radius check
                          IsWaterEdge() — shoreline foam detection
                          GetRoomTint() — per-region floor color variation
                          GetDepthTintedColor() — distance-based color dimming
                          GetRainParticle() — weather rain overlay
                          GetWeatherDimmedColor() — rain desaturation
                          GetWaterFlowGlyph() — animated wave propagation
                          GetCampfireFlickerRadius() — pulsing campfire light
                          GetCampfireGlowDistance() — campfire glow check
                          ShouldShowAggroIndicator() — monster chase indicator
                          GetInteractableSparkle() — ✦/✧ on interactables
                          GetHeatmapColor() — explored area heat map
                          GetWaterLilyPad() — static lily pad decoration
                          IsInLavaDangerZone() — lava proximity check
                          GetCobwebDecoration() — wall corner cobwebs
                          GetExploredRoomGlow() — warm tint on explored rooms
                          GetAmbientSoundText() — flavor text near terrain

================================================================================
  5. CORE SYSTEMS BREAKDOWN
================================================================================

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

================================================================================
  6. UI FRAMEWORK NOTES (Terminal.Gui v2)
================================================================================

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

================================================================================
  7. DATA FLOW & EVENT WIRING
================================================================================

  GAME STARTUP FLOW
  -----------------
  Program.Main()
    -> Application.Init()
    -> Window (fullscreen, borderless)
    -> TitleScreen.Show(mainWindow)

  SCREEN TRANSITIONS
  ------------------
  TitleScreen       -> DifficultyScreen.Show()     [New Game button]
  TitleScreen       -> OptionsScreen.Show()        [Options button]
  DifficultyScreen  -> CharacterCreationScreen     [Continue button]
  DifficultyScreen  -> TitleScreen                 [Back button]
  CharacterCreation -> GameScreen.Show(player)     [Start Game button]
  GameScreen        -> DeathScreen                 [Player HP <= 0]
  DeathScreen       -> TitleScreen                 [Return button]

  GAMEPLAY EVENT CHAIN
  --------------------
  MapView events:
    PlayerMoveRequested(dx,dy) -> TurnManager.ProcessPlayerMove()
    SprintRequested(dx,dy)     -> TurnManager.ProcessSprint()
    StealthMoveRequested(dx,dy)-> TurnManager.ProcessStealthMove()
    InventoryRequested         -> InventoryDialog.Show()
    StatsRequested             -> StatsDialog.Show()
    HelpRequested              -> HelpDialog.Show()
    PickupRequested            -> TurnManager.PickupItems()
    RestRequested              -> TurnManager.ProcessRest()
    AutoExploreRequested       -> Auto-explore timer loop
    WaitRequested              -> TurnManager.ProcessPlayerMove(0,0)
    LookRequested              -> Log surrounding tile info
    KillStatsRequested         -> KillStatsDialog.Show()

  TurnManager events:
    TurnCompleted              -> GameScreen refreshes HUD
    DamageDealt(x,y,dmg,bool)  -> MapView damage flash + scorch marks
    MonsterKilled(x,y)         -> MapView corpse/shatter + kill streak flash
    StairsConfirmRequested     -> GameScreen shows stairs dialog
    FloorChanged(floor)        -> Update map refs, title, weather
    PlayerDied                 -> DeathScreen.Show()
    GameWon                    -> VictoryScreen.Show()
    VendorInteraction(vendor)  -> ShopDialog.Show()

  Inventory events:
    ItemAdded/Removed         -> TerminalGuiInventoryLogger -> GameLogView
    ItemEquipped/Unequipped   -> TerminalGuiInventoryLogger -> GameLogView
    ConsumableUsed            -> TerminalGuiInventoryLogger -> GameLogView

================================================================================
  8. EXPANDABILITY GUIDE
================================================================================

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

================================================================================
  9. BUILD & RUN
================================================================================

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
