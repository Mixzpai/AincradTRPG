using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Core gameplay loop controller -- owns the active map, floor state, and status effects.
public partial class TurnManager
{
    private GameMap _map;
    private readonly Player _player;
    public Player Player => _player;
    private readonly IGameLog _log;

    private int _turnCount;
    public int TurnCount { get => _turnCount; private set { _turnCount = value; _log.CurrentTurn = value; } }
    public int KillCount { get; private set; }
    public int TotalColEarned { get; private set; }
    public int CurrentFloor { get; private set; }

    public int Difficulty { get; }
    private readonly DifficultyData.DifficultyTier _diffTier;

    private int _killStreak, _idleTurns, _dodgeStreak, _lastCombatTurn, _comboTarget, _comboCount;
    public int KillStreak => _killStreak;
    public int DodgeStreak => _dodgeStreak;
    public int LastCombatTurn => _lastCombatTurn;

    private int _shrineBuff, _shrineBuffTurns;
    public int ShrineBuffTurns => _shrineBuffTurns;

    public int Satiety { get; private set; } = 100;
    public const int MaxSatiety = 100;
    private const int HungerDrainInterval = 3, HungerRegenThreshold = 30;
    private const int WellFedThreshold = 80, StarvingThreshold = 15;
    private bool _starvingWarned;
    public int SatietyAtkBonus => Satiety >= WellFedThreshold ? 2 : Satiety <= StarvingThreshold ? -2 : 0;
    public int SatietyDefBonus => Satiety >= WellFedThreshold ? 1 : Satiety <= StarvingThreshold ? -1 : 0;

    public string? LastKillerName { get; private set; }

    private int _restCounter;
    private bool _fatiguedWarned, _exhaustedWarned;
    private const int FatigueThreshold = 150, ExhaustionThreshold = 250;
    public int RestCounter => _restCounter;
    public int FatigueAtkPenalty => _restCounter >= ExhaustionThreshold ? -4 : _restCounter >= FatigueThreshold ? -2 : 0;
    public int FatigueSpdPenalty => _restCounter >= ExhaustionThreshold ? -2 : _restCounter >= FatigueThreshold ? -1 : 0;
    public int FatigueDefPenalty => _restCounter >= ExhaustionThreshold ? -1 : 0;

    private string? _bountyTarget;
    private int _bountyKillsNeeded, _bountyKillsCurrent, _bountyRewardCol, _bountyRewardXp;
    private bool _bountyComplete;
    public string? BountyTarget => _bountyTarget;
    public int BountyKillsNeeded => _bountyKillsNeeded;
    public int BountyKillsCurrent => _bountyKillsCurrent;
    public int BountyRewardCol => _bountyRewardCol;
    public int BountyRewardXp => _bountyRewardXp;
    public bool BountyComplete => _bountyComplete;

    private readonly HashSet<int> _discoveredLore = new();
    // Field bosses defeated this run — prevents re-spawn on floor re-entry.
    public HashSet<string> DefeatedFieldBosses { get; set; } = new();
    // Bundle 13 (Q16) — floor-boss kills this run. Per-floor granular so PG drop reveal
    // doesn't leak floors the player skipped via teleport.
    public HashSet<int> DefeatedFloorBosses { get; set; } = new();
    public IReadOnlyCollection<int> DiscoveredLore => _discoveredLore;
    private bool _floorFullyExplored;
    private readonly HashSet<int> _aggroAlerted = new(), _dangerWarned = new();
    // Iaijutsu first-strike gate (Katana proficiency 25 fork). One bonus per mob, per encounter.
    private readonly HashSet<int> _iaijutsuStruck = new();
    private bool _stairsDiscovered;
    // ExtraSearch passive: true after the first trap reveal this floor so we
    // only log the flavor line once per floor (subsequent reveals are silent).
    private bool _extraSearchRevealedThisFloor;
    private static readonly int[] FloorParTurns = { 200, 220, 250, 280, 320, 360, 400, 450, 500, 550 };
    private int _floorStartTurn;

    private bool _stealthActive, _lastMoveWasStealth;
    public bool IsStealthed => _stealthActive || _lastMoveWasStealth;
    public bool PlayerLowHp => !_player.IsDefeated && _player.CurrentHealth > 0
        && _player.CurrentHealth <= _player.MaxHealth / 4;

    private int _floorColStart;
    public int FloorColEarned => _player.ColOnHand - _floorColStart;
    private int _levelUpBuff, _levelUpBuffTurns;
    public int LevelUpBuffTurns => _levelUpBuffTurns;

    // Bundle 10 (B1) — active food regen buff. Set when Food with RegenerationRate>0
    // is consumed; consumed in PassiveRegen each tick. 0 = inactive (legacy default).
    private int _foodRegenRate, _foodRegenTurnsLeft;
    public int FoodRegenRate => _foodRegenRate;
    public int FoodRegenTurnsLeft => _foodRegenTurnsLeft;
    private void ApplyFoodRegenBuff(int rate, int turns)
    { _foodRegenRate = Math.Max(_foodRegenRate, rate); _foodRegenTurnsLeft = Math.Max(_foodRegenTurnsLeft, turns); }

    // Counterattack stance — player presses V, next incoming attack triggers riposte
    private bool _counterStance;
    public bool IsCounterStance => _counterStance;

    private int _poisonTurnsLeft, _poisonDamagePerTick = 2;
    private int _bleedTurnsLeft, _bleedDamagePerTick = 1;
    private int _stunTurnsLeft, _slowTurnsLeft;
    // Weapon Barrier+N SpecialEffect: damage pool absorbed before HP hit.
    // Resets to wpn Barrier value on floor change (ReplaceMap / new floor).
    private int _barrierRemaining;
    // Invisibility+N SpecialEffect: set on crit, ticks down in AdvanceTurn.
    // Non-zero suppresses monster aggro via SimpleAI's playerStealthed hook.
    private int _invisibilityTurnsLeft;
    public int StunTurnsLeft => _stunTurnsLeft;
    public int SlowTurnsLeft => _slowTurnsLeft;
    public bool IsInvisible => _invisibilityTurnsLeft > 0;
    public int InvisibilityTurnsLeft => _invisibilityTurnsLeft;

    private readonly Dictionary<int, int> _blindedMobs = new(), _stunnedMobs = new();
    // Lunacy+N per-mob confusion counter: value = turns of random-wander AI left.
    private readonly Dictionary<int, int> _confusedMobs = new();
    // Bundle 10 (B11) — SlowOnHit per-mob slow counter: value = turns of every-other-turn skip left.
    private readonly Dictionary<int, int> _slowedMobs = new();
    private readonly Dictionary<int, (int turns, int dmg)> _burningMobs = new(), _poisonedMobs = new();
    // Telegraphed attacks — monster winds up for 1 turn, then deals heavy damage
    private readonly Dictionary<int, int> _telegraphedAttacks = new(); // mobId → damage queued

    private void CleanupMobStatus(int mobId)
    {
        _blindedMobs.Remove(mobId); _stunnedMobs.Remove(mobId);
        _burningMobs.Remove(mobId); _poisonedMobs.Remove(mobId);
        _telegraphedAttacks.Remove(mobId); _confusedMobs.Remove(mobId);
        _slowedMobs.Remove(mobId);
    }

    private void ClearAllMobStatuses()
    {
        _blindedMobs.Clear(); _stunnedMobs.Clear();
        _burningMobs.Clear(); _poisonedMobs.Clear();
        _telegraphedAttacks.Clear(); _confusedMobs.Clear();
        _slowedMobs.Clear();
    }

    private int _lastSoundCueTurn = -99;
    private const int SoundCueCooldown = 15; // reduced log spam -- was 5
    private DateTime _sessionStart = DateTime.Now;
    private TimeSpan _priorPlayTime;
    public TimeSpan TotalPlayTime => _priorPlayTime + (DateTime.Now - _sessionStart);
    private readonly Dictionary<string, int> _killsByName = new();
    public (string Name, int Count)? TopKill =>
        _killsByName.Count == 0 ? null : _killsByName.MaxBy(kv => kv.Value) is var top ? (top.Key, top.Value) : null;
    private DateTime _floorStartRealTime = DateTime.Now;
    public TimeSpan FloorRealTime => DateTime.Now - _floorStartRealTime;
    public int ActiveSaveSlot { get; set; } = 1;

    private int _floorDamageTaken, _floorItemsFound, _floorKillsStart;
    public int FloorKillsStart => _floorKillsStart;
    public record FloorRecapData(int Floor, int Kills, int Items, int DamageTaken, int Turns, int ExplorePercent,
        int ColEarned = 0, string? BountyTarget = null, int BountyProgress = 0, int BountyNeeded = 0, bool BountyDone = false,
        TimeSpan RealTime = default);
    public FloorRecapData? LastFloorRecap { get; private set; }

    // FB-469 — center-screen toast hooks. Fires from HandleMonsterKill so
    // GameScreen.Events can route to ToastQueue without reaching into combat.
    public event Action<string>? FloorBossCleared;
    public event Action<string>? SpeciesFirstKilled;

    public event Action? TurnCompleted;
    public event Action? PlayerDied;
    public event Action<int>? FloorChanged;
    public event Action<Vendor>? VendorInteraction;
    public event Action? StairsConfirmRequested;
    public event Action? GameWon;
    public event Action<NPC>? NpcDialogRequested;
    public event Action<int, int, int, bool, bool>? DamageDealt;
    public event Action<int, int, int, int, Color>? WeaponSwing;
    public event Action<int, int>? MonsterKilled;
    // Tiered death-burst request: tier 0=standard, 1=elite, 2=floor boss.
    public event Action<int, int, int>? MonsterDeathBurstRequested;
    public event Action<int, int, string, Color>? CombatTextEvent;
    public event Action? LeveledUp;
    public event Action<int, int, Color>? SkillActivated;
    // Fires on level-up with 3 perk choices. UI should show picker, then call ApplyTalent.
    public event Action<PassiveTalents.Perk[]>? TalentPickRequested;

    // Fires when the active map swaps (enter/exit labyrinth). UI refreshes map references.
    public event Action? MapSwapped;
    // Bundle 8: fires once per run when a Divine Object enters inventory (boss drop or quest).
    // GameScreen.Events subscribes to trigger DivineObtainBanner + border flash + toast.
    public event Action<Items.Equipment.Weapon>? DivineObtained;
    // Bundle 9: Selka awakening dialog request. GameScreen.Events subscribes to open
    // DivineAwakeningDialog modal. Fires each time Selka is engaged while carrying a Divine.
    public event Action<Entities.Player>? DivineAwakeningRequested;
    // Fires when the player steps on an Anvil. UI opens CraftingDialog.
    public event Action? AnvilInteraction;
    public event Action? CookingInteraction;
    // Fires when the player talks to Lisbeth at Lindarth. UI opens LisbethCraftDialog.
    public event Action? LisbethInteraction;
    // FB-057 — fires when the player steps on the Monument of Swordsmen
    // tile (F1 Town of Beginnings). UI opens the MonumentDialog.
    public event Action? MonumentInteraction;

    // ── Sword Skills state ───────────────────────────────────────────
    private readonly Dictionary<string, int> _skillCooldowns = new();
    private int _postMotionDelay;
    public int PostMotionDelay => _postMotionDelay;
    public readonly SwordSkill?[] EquippedSkills = new SwordSkill?[4];
    // SwordSkillRequested and SwordSkillMenuRequested live on MapView,
    // not here — TurnManager only has ExecuteSwordSkill(slot).

    public int GetSkillCooldown(string skillId) =>
        _skillCooldowns.TryGetValue(skillId, out int cd) ? cd : 0;

    public IReadOnlyList<SwordSkill> GetUnlockedSkills()
    {
        var wpn = _player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.Weapon)
            as Items.Equipment.Weapon;
        string wtype = wpn?.WeaponType ?? "Unarmed";
        int kills = GetWeaponKills(wtype);
        return SwordSkillDatabase.UnlockedFor(wtype, kills).ToList();
    }

    // ── Labyrinth (dungeon) state ────────────────────────────────────
    private GameMap? _overworldMap;
    private (int X, int Y) _overworldPlayerPos;
    private bool _inLabyrinth;
    public bool InLabyrinth => _inLabyrinth;

    public void EnterLabyrinth()
    {
        _overworldMap = _map;
        _overworldPlayerPos = (_player.X, _player.Y);

        var (labMap, labRooms) = MapGenerator.GenerateLabyrinth(CurrentFloor);
        _map = labMap;
        // Labyrinths never host field bosses — those roam the overworld only.
        MapGenerator.PopulateFloor(labMap, labRooms, _player, CurrentFloor, _diffTier.MobStatPercent,
            skipFieldBosses: true);
        foreach (var e in labMap.Entities) e.SetLog(_log);

        _inLabyrinth = true;
        UpdateVisibility();
        _log.LogSystem("You step through the archway into the Labyrinth...");
        _log.LogSystem("The air grows cold. Pale lights flicker far above.");
        MapSwapped?.Invoke();
        TurnCompleted?.Invoke();
    }

    public void ExitLabyrinth()
    {
        if (_overworldMap == null) return;
        _map = _overworldMap;
        _map.MoveEntity(_player, _overworldPlayerPos.X, _overworldPlayerPos.Y);
        _overworldMap = null;
        _inLabyrinth = false;
        UpdateVisibility();
        _log.LogSystem("You emerge from the Labyrinth into daylight.");
        MapSwapped?.Invoke();
        TurnCompleted?.Invoke();
    }

    // F9 biome hot-reload helper: swap the active overworld map in place.
    // Caller is responsible for player placement (typically PopulateFloor did it).
    // No-op while _inLabyrinth — F9 during labyrinth would orphan the overworld.
    public void ReplaceMap(GameMap newMap, Player player)
    {
        if (_inLabyrinth) return;
        _map = newMap;
        // F9 hot-reload mirrors fresh floor entry: reset per-floor flags so the
        // player doesn't inherit stairs-revealed state or stale kill/damage
        // counters from the pre-regen floor.
        _stairsDiscovered = false;
        _floorStartTurn = TurnCount;
        _floorKillsStart = KillCount;
        _floorDamageTaken = 0;
        _floorItemsFound = 0;
        _floorFullyExplored = false;
        // Barrier+N regenerates fully each floor from the equipped weapon.
        _barrierRemaining = GetBarrierCapacity();
        // Extended per-floor state resets — stale latches would mask regen on F9.
        Story.StorySystem.ClearPerFloorTriggers();
        ParticleQueue.ClearAmbient();
        TutorialSystem.ClearPerFloorLatches();
        _log.Clear();
        UI.DebugLogger.LogGame("RELOAD", "Cleared per-floor latches");
        UpdateVisibility();
        MapSwapped?.Invoke();
    }

    public void RequestTalentPick()
    {
        var choices = PassiveTalents.RollChoices();
        TalentPickRequested?.Invoke(choices);
    }

    public void ApplyTalent(PassiveTalents.Perk perk) => perk.Apply(_player);

    // Fires after a Divine enters inventory (boss drop or quest reward).
    // The flag is kept for save-compat round-trip but no longer gates the banner —
    // the cap is gone, so every Divine pickup gets full ceremony.
    private void NotifyDivineObtained(Items.Equipment.Weapon divine)
    {
        LootGenerator.DivineObtainedThisRun = true;
        DivineObtained?.Invoke(divine);
    }

    public TurnManager(GameMap map, Player player, IGameLog log, int floor = 1,
        int difficulty = 3)
    {
        _map = map; _player = player; _log = log;
        CurrentFloor = floor;
        // Player Guide unlock — record on construct so new games + loads at any floor
        // mark themselves reached (otherwise F1 stays locked at game start).
        LifetimeStats.RecordFloorReach(floor);
        TileDefinitions.CurrentFloor = floor;
        Difficulty = difficulty;
        _diffTier = DifficultyData.Get(difficulty);
        _floorColStart = _player.ColOnHand;

        // Shop/Invest statics live across runs; reset on construct,
        // LoadFromSave overwrites below if this is a load path.
        ShopTierSystem.SetForLoad(0);
        VendorInvestmentSystem.Clear();

        // Wire Life Skill + Title hooks before gameplay systems fire so the
        // first rest/walk/sprint/food grant and first kill are observed.
        WireLifeSkillHooks();

        // Barrier+N per-floor pool — seed from starting weapon so fresh runs
        // have the pool ready before the first hit. Refilled on floor entry.
        _barrierRemaining = GetBarrierCapacity();

        // Bundle 13 (Item 1) — track Legendary collectables on pickup. HashSet.Add
        // is idempotent, so dupes via stack/sell/repurchase don't double-count.
        player.Inventory.Events.ItemAdded += (_, e) =>
        {
            if (e.Item.Rarity == "Legendary" && !string.IsNullOrEmpty(e.Item.DefinitionId))
                CollectablesTracker.MarkCollected(e.Item.DefinitionId);
        };

        player.Inventory.Events.ConsumableUsed += (_, e) =>
        {
            if (e.Consumable is Potion { PotionType: "Antidote" })
            {
                bool hadEffect = _poisonTurnsLeft > 0 || _bleedTurnsLeft > 0 || _slowTurnsLeft > 0;
                _poisonTurnsLeft = 0; _bleedTurnsLeft = 0; _slowTurnsLeft = 0;
                _log.Log(hadEffect ? "The antidote purges all toxins from your body!" : "You feel fine already, but better safe than sorry.");
            }
            if (e.Consumable is Potion { PotionType: "Teleport" })
            {
                if (RunModifiers.IsActive(RunModifier.KayabasWager))
                    _log.Log("The Escape Rope frays and breaks. Kayaba's Wager forbids retreat.");
                else
                {
                    _map.MoveEntity(_player, _map.Width / 2, _map.Height / 2);
                    _log.LogSystem("You use the Escape Rope and warp back to the entrance!");
                }
            }
            if (e.Consumable is SAOTRPG.Items.Consumables.Crystal crystal)
                HandleCrystal(crystal);
            if (e.Consumable is SAOTRPG.Items.Consumables.CorruptionStone stone)
                HandleCorruptionStone(stone);
            if (e.Consumable is Food food)
            {
                // Eating skill scales satiety: L10 +10%, L25 +25%, L50 +50%, L99 +100%.
                int baseGain = food.RegenerationDuration * 2;
                int bonusPct = _player.LifeSkills.EatingFoodPotencyPercent();
                int scaledGain = baseGain + (baseGain * bonusPct / 100);
                Satiety = Math.Min(MaxSatiety, Satiety + scaledGain);
                string bonusTag = bonusPct > 0 ? $" [+{bonusPct}% Eating]" : "";
                _log.Log($"You feel sated. (Satiety: {Satiety}/{MaxSatiety}){bonusTag}");
                _starvingWarned = false;
                // Bundle 10 (B1) — RegenerationRate buff: tick HP regen for RegenerationDuration turns.
                // Eating bonusPct scales the regen rate (L99 → +100% rate). 0 = inert.
                int scaledRate = food.RegenerationRate + (food.RegenerationRate * bonusPct / 100);
                if (scaledRate > 0 && food.RegenerationDuration > 0)
                    ApplyFoodRegenBuff(scaledRate, food.RegenerationDuration);
            }
            if (e.Consumable is DamageItem dmgItem) HandleThrowable(dmgItem);
        };
    }

    private void HandleThrowable(DamageItem dmgItem)
    {
        int finalDmg = dmgItem.BaseDamage + _player.Intelligence;
        int hitCount = 0;
        string intTag = _player.Intelligence > 0 ? $" (+{_player.Intelligence} INT)" : "";

        foreach (var entity in _map.Entities.ToList())
        {
            if (entity is not Monster mob || mob.IsDefeated) continue;
            int dist = Math.Max(Math.Abs(mob.X - _player.X), Math.Abs(mob.Y - _player.Y));
            if (dist > dmgItem.AreaOfEffect) continue;

            if (finalDmg > 0)
            {
                mob.CurrentHealth -= finalDmg;
                _log.LogCombat($"You throw a {dmgItem.Name}! It hits {mob.Name} for {finalDmg} damage!{intTag}");
                DamageDealt?.Invoke(mob.X, mob.Y, finalDmg, false, false);
                if (mob.CurrentHealth <= 0)
                { mob.CurrentHealth = 0; _log.LogCombat($"  {mob.Name} is destroyed by the blast!"); }
            }

            switch (dmgItem.DamageType)
            {
                case "Fire":   _burningMobs[mob.Id] = (2, 5 + CurrentFloor); _log.LogCombat($"  {mob.Name} catches fire!"); break;
                case "Poison": _poisonedMobs[mob.Id] = (3, 2 + CurrentFloor); _log.LogCombat($"  {mob.Name} is poisoned!"); break;
                case "Smoke":  _blindedMobs[mob.Id] = 3; _log.LogCombat($"  Smoke engulfs {mob.Name}! Their attacks are weakened for 3 turns!"); break;
                case "Stun":   _stunnedMobs[mob.Id] = 1; _log.LogCombat($"  The flash stuns {mob.Name}!"); break;
            }
            hitCount++;
        }
        if (hitCount == 0) _log.LogCombat($"You throw a {dmgItem.Name}, but nothing is in range!");
    }

    private void HandleCrystal(SAOTRPG.Items.Consumables.Crystal crystal)
    {
        // FB-564 Anti-Crystal Tyranny — all crystals inert.
        if (RunModifiers.IsActive(RunModifier.AntiCrystalTyranny))
        {
            _log.Log("The crystal hums but refuses to activate. Anti-Crystal field suppresses it.");
            return;
        }
        // FB-564 Kayaba's Wager — teleport/corridor crystals specifically inert.
        if (RunModifiers.IsActive(RunModifier.KayabasWager)
            && crystal.CrystalType is "Teleport" or "Corridor")
        {
            _log.Log("Kayaba's Wager forbids retreat. The crystal crumbles to dust.");
            return;
        }
        switch (crystal.CrystalType)
        {
            case "Teleport":
                _map.MoveEntity(_player, _map.Width / 2, _map.Height / 2);
                _log.LogSystem($"The {crystal.Destination ?? "teleport"} crystal shatters — you warp to safety!");
                break;
            case "Corridor":
                _map.MoveEntity(_player, _map.Width / 2, _map.Height / 2);
                _log.LogSystem("A corridor portal opens beneath you. You step through to the entrance.");
                break;
            case "AntiCrystal":
                _log.LogSystem("The Anti-Crystal hums. Teleport effects are suppressed in this area.");
                break;
            case "Healing":
                int heal = crystal.Magnitude > 0 ? crystal.Magnitude : 100;
                _player.CurrentHealth = Math.Min(_player.CurrentHealth + heal, _player.MaxHealth);
                _log.Log($"The healing crystal restores {heal} HP.");
                break;
            case "Antidote":
                bool hadPoison = _poisonTurnsLeft > 0 || _bleedTurnsLeft > 0;
                _poisonTurnsLeft = 0; _bleedTurnsLeft = 0;
                _log.Log(hadPoison ? "The antidote crystal purges all toxins!" : "You felt fine anyway.");
                break;
            case "ParalysisCure":
                bool hadPara = _stunTurnsLeft > 0 || _slowTurnsLeft > 0;
                _stunTurnsLeft = 0; _slowTurnsLeft = 0;
                _log.Log(hadPara ? "The paralysis cure crystal restores your movement!" : "Your limbs feel unusually limber.");
                break;
            case "Mirage":
                _log.LogSystem("The Mirage Sphere activates — the next encounter will be recorded as evidence.");
                break;
            case "Revive":
                _player.CurrentHealth = _player.MaxHealth;
                _log.LogSystem($"{crystal.Name} glows white-hot — you are restored to full health!");
                break;
        }
    }

    // Corruption Stone (HF canon workaround: F100 ends game, post-F100 boss unreachable).
    // Swaps target weapon → Corrupted variant, preserving EnhancementLevel + RefinementSlots.
    private void HandleCorruptionStone(SAOTRPG.Items.Consumables.CorruptionStone stone)
    {
        if (string.IsNullOrEmpty(stone.TargetWeaponDefId) || string.IsNullOrEmpty(stone.CorruptedWeaponDefId))
        {
            _log.Log("The corruption stone crumbles. It had no target bound to it.");
            return;
        }

        // Search inventory + Weapon/OffHand slots; prefer equipped.
        SAOTRPG.Items.Equipment.Weapon? target = null;
        SAOTRPG.Inventory.Core.EquipmentSlot? equippedSlot = null;

        var mainWeapon = _player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.Weapon)
            as SAOTRPG.Items.Equipment.Weapon;
        if (mainWeapon != null && mainWeapon.DefinitionId == stone.TargetWeaponDefId)
        {
            target = mainWeapon;
            equippedSlot = SAOTRPG.Inventory.Core.EquipmentSlot.Weapon;
        }
        if (target == null)
        {
            var offWeapon = _player.Inventory.GetEquipped(SAOTRPG.Inventory.Core.EquipmentSlot.OffHand)
                as SAOTRPG.Items.Equipment.Weapon;
            if (offWeapon != null && offWeapon.DefinitionId == stone.TargetWeaponDefId)
            {
                target = offWeapon;
                equippedSlot = SAOTRPG.Inventory.Core.EquipmentSlot.OffHand;
            }
        }
        if (target == null)
        {
            foreach (var item in _player.Inventory.Items)
            {
                if (item is SAOTRPG.Items.Equipment.Weapon w && w.DefinitionId == stone.TargetWeaponDefId)
                {
                    target = w;
                    break;
                }
            }
        }

        if (target == null)
        {
            // Fail-loud: surface error; refund charge (ConsumableUsed already decremented).
            string needName = Items.ItemRegistry.Create(stone.TargetWeaponDefId)?.Name ?? stone.TargetWeaponDefId;
            _log.Log($"The corruption stone finds nothing to corrupt. You need {needName} in your inventory or equipped.");
            stone.Quantity++;
            return;
        }

        // Build the Corrupted variant and transfer preserved state.
        var corrupted = Items.ItemRegistry.Create(stone.CorruptedWeaponDefId)
            as SAOTRPG.Items.Equipment.Weapon;
        if (corrupted == null)
        {
            _log.Log("The corruption stone flickers and fails. (Corrupted weapon def not found.)");
            stone.Quantity++;
            return;
        }

        corrupted.EnhancementLevel = target.EnhancementLevel;
        corrupted.EnhancementOreHistory = new List<string>(target.EnhancementOreHistory);
        for (int i = 0; i < SAOTRPG.Items.Equipment.EquipmentBase.RefinementSlotCount; i++)
            corrupted.RefinementSlots[i] = target.RefinementSlots[i];

        // Replay ore bonuses (fresh Bonuses from Create). Mirrors SaveManager.DeserializeItem:
        // each ore in EnhancementOreHistory = +3 to its stat per level.
        if (corrupted.EnhancementLevel > 0)
        {
            const int weaponLevelBonus = 3;
            for (int i = 0; i < corrupted.EnhancementLevel; i++)
            {
                string oreId = i < corrupted.EnhancementOreHistory.Count
                    ? corrupted.EnhancementOreHistory[i] : "ore_crimson_flame";
                var stat = Items.Definitions.EnhancementOreDefinitions
                    .OreDefIdToStat.TryGetValue(oreId, out var s) ? s : StatType.Attack;
                corrupted.Bonuses.Add(stat, weaponLevelBonus);
            }
        }
        // Replay ingot bonuses (Create gave fresh Bonuses). Same helper as SaveManager.
        Refinement.RehydrateBonuses(corrupted);

        // Remove the target and grant the corrupted variant.
        if (equippedSlot != null)
        {
            // Unequip to the inventory, then swap.
            _player.Inventory.Unequip(equippedSlot.Value, _player);
            _player.Inventory.RemoveItem(target);
            if (!_player.Inventory.AddItem(corrupted))
            {
                _map.AddItem(_player.X, _player.Y, corrupted);
                _log.LogLoot($"  ◆ The {target.Name} transforms... it is now Corrupted. (Inventory full — dropped at your feet.)");
            }
            else
            {
                _log.LogLoot($"  ◆ The {target.Name} transforms... it is now Corrupted.");
            }
        }
        else
        {
            _player.Inventory.RemoveItem(target);
            if (!_player.Inventory.AddItem(corrupted))
            {
                _map.AddItem(_player.X, _player.Y, corrupted);
                _log.LogLoot($"  ◆ The {target.Name} transforms... it is now Corrupted. (Inventory full — dropped at your feet.)");
            }
            else
            {
                _log.LogLoot($"  ◆ The {target.Name} transforms... it is now Corrupted.");
            }
        }
    }

    public static TurnManager LoadFromSave(SaveData save, GameMap map, Player player, IGameLog log)
    {
        var tm = new TurnManager(map, player, log, save.CurrentFloor, save.Difficulty);
        tm.TurnCount = save.TurnCount; tm.KillCount = save.KillCount; tm.TotalColEarned = save.TotalColEarned;
        tm.Satiety = save.Satiety; tm._killStreak = save.KillStreak;
        tm._poisonTurnsLeft = save.PoisonTurnsLeft; tm._bleedTurnsLeft = save.BleedTurnsLeft;
        tm._stunTurnsLeft = save.StunTurnsLeft; tm._slowTurnsLeft = save.SlowTurnsLeft;
        tm._shrineBuffTurns = save.ShrineBuffTurns; tm._levelUpBuffTurns = save.LevelUpBuffTurns;
        // Bundle 10 (B1) — restore food regen buff. 0 = inactive on legacy saves.
        tm._foodRegenRate = save.FoodRegenRate; tm._foodRegenTurnsLeft = save.FoodRegenTurnsLeft;
        tm._floorStartTurn = save.TurnCount; tm._floorStartRealTime = DateTime.Now;
        tm._floorKillsStart = save.KillCount; tm._restCounter = save.RestCounter;
        tm._bountyTarget = save.BountyTarget; tm._bountyKillsNeeded = save.BountyKillsNeeded;
        tm._bountyKillsCurrent = save.BountyKillsCurrent; tm._bountyRewardCol = save.BountyRewardCol;
        tm._bountyRewardXp = save.BountyRewardXp; tm._bountyComplete = save.BountyComplete;
        foreach (var kvp in save.WeaponKills) tm._weaponKills[kvp.Key] = kvp.Value;
        // IF Proficiency forks — rehydrate per-weapon pick state.
        tm.RehydrateForkChoices(save.WeaponProficiencyForks);
        if (save.DiscoveredLore != null) foreach (var idx in save.DiscoveredLore) tm._discoveredLore.Add(idx);
        if (save.UnlockedAchievements != null) Achievements.Unlocked = new HashSet<string>(save.UnlockedAchievements);
        if (save.SeenTutorialTips != null) TutorialSystem.SeenTips = new HashSet<string>(save.SeenTutorialTips);
        if (save.ActiveQuests != null) QuestSystem.ActiveQuests = new List<Quest>(save.ActiveQuests);
        if (save.CompletedQuests != null) QuestSystem.CompletedQuests = new List<Quest>(save.CompletedQuests);
        QuestSystem.PinnedQuestId = save.PinnedQuestId;
        if (save.FiredStoryEventIds != null)
            Story.StorySystem.FiredEventIds = new HashSet<string>(save.FiredStoryEventIds);
        if (save.StoryFlags != null)
        {
            Story.StorySystem.Flags.Clear();
            foreach (var s in save.StoryFlags)
                if (Enum.TryParse<Story.StoryFlag>(s, out var f)) Story.StorySystem.Flags.Add(f);
        }
        if (save.FactionReputation != null)
        {
            Story.StorySystem.Reputation.Clear();
            foreach (var kvp in save.FactionReputation)
            {
                // FB-063 faction rename: legacy saves use "AincradLiberationSquad".
                string key = kvp.Key == "AincradLiberationSquad"
                    ? "AincradLiberationForce" : kvp.Key;
                if (Enum.TryParse<Story.Faction>(key, out var f))
                    Story.StorySystem.Reputation[f] = kvp.Value;
            }
        }
        if (save.UnlockedUniqueSkills != null)
        {
            Skills.UniqueSkillSystem.Unlocked.Clear();
            foreach (var s in save.UnlockedUniqueSkills)
                if (Enum.TryParse<Skills.UniqueSkill>(s, out var u)) Skills.UniqueSkillSystem.Unlocked.Add(u);
        }
        Skills.UniqueSkillSystem.TrapsDisarmed = save.TrapsDisarmed;
        if (save.DefeatedFieldBosses != null)
            tm.DefeatedFieldBosses = new HashSet<string>(save.DefeatedFieldBosses);
        // Bundle 13 (Q16) — per-floor boss-clear flags. Null on legacy = empty set.
        if (save.DefeatedFloorBosses != null)
            tm.DefeatedFloorBosses = new HashSet<int>(save.DefeatedFloorBosses);
        RunModifiers.LoadFromSave(save.ActiveRunModifiers);
        // Shop Tiering hydrates cross-save progress; legacy saves default to 0
        // (no tiered stock until the next F50+ clear).
        ShopTierSystem.SetForLoad(save.HighestFloorBossCleared);
        // Investments hydrate per-vendor; legacy saves = cleared state.
        VendorInvestmentSystem.SetForLoad(save.VendorInvestments);
        // Bundle 12 (C6) — restore current-floor mining vein strikes (legacy = noop).
        SaveManager.RestoreVeinStrikes(save, map);

        // Restore party members
        PartySystem.Clear();
        if (save.PartyMembers != null)
        {
            foreach (var ad in save.PartyMembers)
            {
                var ally = new Entities.Ally(ad.Symbol, (Terminal.Gui.Color)ad.SymbolColor)
                {
                    Name = ad.Name, WeaponType = ad.WeaponType, Title = ad.Title,
                    Level = ad.Level, MaxHealth = ad.MaxHealth, CurrentHealth = ad.CurrentHealth,
                    BaseAttack = 5 + ad.Level * 2, BaseDefense = 3 + ad.Level,
                    Behavior = (Entities.AllyBehavior)ad.Behavior,
                };
                PartySystem.Members.Add(ally);
            }
            PartySystem.PlaceAllies(map, player.X, player.Y);
        }

        // Restore equipped sword skills from save
        if (save.EquippedSkillIds != null)
            for (int i = 0; i < Math.Min(save.EquippedSkillIds.Count, tm.EquippedSkills.Length); i++)
                tm.EquippedSkills[i] = save.EquippedSkillIds[i] != null
                    ? SwordSkillDatabase.Get(save.EquippedSkillIds[i]!) : null;
        if (save.SkillCooldowns != null)
            foreach (var kvp in save.SkillCooldowns) tm._skillCooldowns[kvp.Key] = kvp.Value;
        tm._priorPlayTime = TimeSpan.FromSeconds(save.PlayTimeSeconds);
        // Rebuild tag-kill cache from Bestiary so Title tag-milestones stay
        // consistent across sessions without a parallel persisted dict.
        tm.RebuildTagKillsFromBestiary();
        return tm;
    }

    public void UpdateVisibility()
    {
        using var _ = Profiler.Begin("TurnManager.UpdateVisibility");
        // Sync the global clock before recomputing — ambient light and
        // effective FOV both read from DayNightCycle.
        SAOTRPG.Map.DayNightCycle.CurrentTurn = TurnCount;
        SAOTRPG.Map.TileAnimator.CombatActive = TurnCount - _lastCombatTurn <= 5;
        int visRadius = Math.Max(8, SAOTRPG.Map.DayNightCycle.VisibilityRadius + BiomeSystem.VisionModifier * SAOTRPG.Map.DayNightCycle.FovMultiplier);
        _map.UpdateVisibility(_player.X, _player.Y, visRadius);
    }
}
