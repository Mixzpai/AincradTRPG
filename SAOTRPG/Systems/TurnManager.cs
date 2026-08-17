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

// Player-death broadcast payload. Cause is a short tag ("monster" / "trap" / "bleed" / "starvation" / "biome" / "tile").
public record DeathContext(string Cause, string KillerName, int FloorAtDeath, int TurnAtDeath);

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

    // The magnitude halves, exposed for the save. A timed effect that persists its duration and
    // not its strength resumes as a countdown over nothing.
    public int ShrineBuffAmount => _shrineBuff;
    public int LevelUpBuffAmount => _levelUpBuff;
    public int PoisonDamagePerTick => _poisonDamagePerTick;
    public int BleedDamagePerTick => _bleedDamagePerTick;

    // Timed potion buffs. One entry per stat: a second dose REFRESHES rather than stacking, which
    // is what "Increases Defense by 10 for 30 turns" says and is what stops twenty potions from
    // one inventory slot granting +200. Applied straight to the player's base stat and taken back
    // on expiry, so every consumer sees them without a second code path.
    private readonly List<TimedBuff> _timedBuffs = new();
    public IReadOnlyList<TimedBuff> ActiveBuffs => _timedBuffs;

    public sealed class TimedBuff
    {
        public Items.StatType Stat { get; set; }
        public int Potency { get; set; }
        public int TurnsLeft { get; set; }
    }

    // Grant or refresh a timed buff. Potency takes the stronger of the two so a weaker potion
    // cannot cut an active stronger one short, and the duration is refreshed either way.
    public void ApplyTimedBuff(Items.StatType stat, int potency, int turns)
    {
        if (potency == 0 || turns <= 0) return;

        var existing = _timedBuffs.FirstOrDefault(b => b.Stat == stat);
        if (existing == null)
        {
            Items.StatModifierCollection.ApplySingle(_player, stat, potency, add: true);
            _timedBuffs.Add(new TimedBuff { Stat = stat, Potency = potency, TurnsLeft = turns });
            _log.Log($"{stat} +{potency} for {turns} turns.");
            return;
        }

        if (potency > existing.Potency)
        {
            Items.StatModifierCollection.ApplySingle(
                _player, stat, potency - existing.Potency, add: true);
            existing.Potency = potency;
        }
        existing.TurnsLeft = Math.Max(existing.TurnsLeft, turns);
        _log.Log($"{stat} +{existing.Potency} refreshed to {existing.TurnsLeft} turns.");
    }

    // Rebuild the buff list from a save. The stat deltas are NOT re-applied: the player's
    // BaseAttack and friends were serialized WITH the buff already folded in, so applying again
    // would double it every load — the same laundering this whole feature exists to stop.
    private void RestoreTimedBuffs(List<TimedBuffSave>? saved)
    {
        _timedBuffs.Clear();
        if (saved == null) return;
        foreach (var b in saved)
        {
            if (b.TurnsLeft <= 0) continue;
            if (!Enum.TryParse<Items.StatType>(b.Stat, out var stat)) continue;
            _timedBuffs.Add(new TimedBuff { Stat = stat, Potency = b.Potency, TurnsLeft = b.TurnsLeft });
        }
    }

    // Take a buff back when it runs out. Kept separate from the tick so the load path can rebuild
    // the list without double-applying.
    private void ExpireTimedBuffs()
    {
        for (int i = _timedBuffs.Count - 1; i >= 0; i--)
        {
            var b = _timedBuffs[i];
            if (--b.TurnsLeft > 0) continue;
            Items.StatModifierCollection.ApplySingle(_player, b.Stat, b.Potency, add: false);
            _timedBuffs.RemoveAt(i);
            _log.Log($"The {b.Stat} elixir wears off.");
        }
    }

    // Remaining Barrier+N absorb for this floor. Exposed so the status tray can show it: the
    // pool is live (refilled per floor from equipped Barrier effects, spent in ProcessMonsterTurn)
    // and the Player Guide tells the player to build around it, but nothing displayed it.
    public int BarrierRemaining => _barrierRemaining;

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
    // Floor-boss kills this run. Per-floor granular so PG drop reveal doesn't leak
    // floors the player skipped via teleport.
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
    // Chests Extra Skill: Search has examined on this floor. Opening one of these rolls a
    // tier better. Positions rather than a Tile flag — see the note in RevealNearbyTraps.
    private readonly HashSet<(int X, int Y)> _scoutedChests = new();
    private bool _extraSearchScoutedThisFloor;
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

    // Active food regen buff. Set when Food with RegenerationRate>0 is consumed;
    // consumed in PassiveRegen each tick. 0 = inactive (legacy default).
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
    // SlowOnHit per-mob slow counter: value = turns of every-other-turn skip left.
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

    // Run-scoped: any ally KO'd at least once. Cleared on new run via TurnManager
    // construction (default false). Used by if_no_ally_death F100 milestone.
    public bool AnyAllyKOdThisRun { get; internal set; }

    // Per-run completion tallies for QuestSubCategory turn-ins. Drive progressive
    // toasts and milestone unlocks (hf_element_researcher / hf_debug_field_tester).
    // Round-tripped via SaveData; reset to 0 on new run.
    public int IfImplementQuestsCompletedThisRun { get; internal set; }
    public int HfMissionsCompletedThisRun { get; internal set; }
    public record FloorRecapData(int Floor, int Kills, int Items, int DamageTaken, int Turns, int ExplorePercent,
        int ColEarned = 0, string? BountyTarget = null, int BountyProgress = 0, int BountyNeeded = 0, bool BountyDone = false,
        TimeSpan RealTime = default);
    public FloorRecapData? LastFloorRecap { get; private set; }

    // Center-screen toast hooks. Fires from HandleMonsterKill so
    // GameScreen.Events can route to ToastQueue without reaching into combat.
    public event Action<string>? FloorBossCleared;
    public event Action<string>? SpeciesFirstKilled;

    public event Action? TurnCompleted;
    public event Action? PlayerDied;
    // Detailed death broadcast — fires alongside PlayerDied with full context.
    public event Action<DeathContext>? PlayerDiedDetailed;
    private void RaisePlayerDied(string cause)
    {
        PlayerDied?.Invoke();
        PlayerDiedDetailed?.Invoke(new DeathContext(cause, LastKillerName ?? "", CurrentFloor, TurnCount));
    }
    public event Action<int>? FloorChanged;
    public event Action<Vendor>? VendorInteraction;
    public event Action? StairsConfirmRequested;
    public event Action? GameWon;
    public event Action<NPC>? NpcDialogRequested;
    public event Action<int, int, int, bool, bool>? DamageDealt;
    // (fromX, fromY, toX, toY, colour, durationMs) — duration carries the weapon's weight.
    public event Action<int, int, int, int, Color, int>? WeaponSwing;
    public event Action<int, int>? MonsterKilled;
    // Tiered death-burst request: tier 0=standard, 1=elite, 2=floor boss.
    public event Action<int, int, int>? MonsterDeathBurstRequested;
    public event Action<int, int, string, Color>? CombatTextEvent;
    public event Action? LeveledUp;
    public event Action<int, int, Color>? SkillActivated;
    // Fires on level-up with 3 perk choices. UI should show picker, then call ApplyTalent.
    public event Action<PendingTalent>? TalentPickRequested;

    // Fires when the active map swaps (enter/exit labyrinth). UI refreshes map references.
    public event Action? MapSwapped;
    // Fires once per run when a Divine Object enters inventory (boss drop or quest).
    // GameScreen.Events subscribes to trigger DivineObtainBanner + border flash + toast.
    public event Action<Items.Equipment.Weapon>? DivineObtained;
    // Selka awakening dialog request. GameScreen.Events subscribes to open
    // DivineAwakeningDialog modal. Fires each time Selka is engaged while carrying a Divine.
    public event Action<Entities.Player>? DivineAwakeningRequested;
    // Fires when the player steps on an Anvil. UI opens CraftingDialog.
    public event Action? AnvilInteraction;
    public event Action? CookingInteraction;
    // Fires when the player talks to Lisbeth at Lindarth. UI opens LisbethCraftDialog.
    public event Action? LisbethInteraction;
    // Fires when the player steps on the Monument of Swordsmen tile
    // (F1 Town of Beginnings). UI opens the MonumentDialog.
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

    // Talents offered but not yet taken, oldest first.
    //
    // The entry stores the PERK IDS THAT WERE OFFERED, not just a count. RollChoices is a fresh
    // shuffle every call, so re-rolling on resume would turn dismissing the prompt into a free
    // reroll — dismiss until the perk you want appears. Storing the offer makes "the same three
    // come back" true by construction instead of by a reproducibility argument, and it needs no
    // run seed. Keyed by the level it was earned at, which is unique per offer.
    private readonly List<(int Id, int Level, string[] PerkIds)> _pendingTalents = new();

    // Identity is a sequence number, NOT the level. Two offers can share a level — anything that
    // grants a talent outside a level-up, or a level-up that fires twice — and keying by level
    // would then resolve both entries as one and orphan the other.
    private int _nextPendingTalentId = 1;

    public int PendingTalentCount => _pendingTalents.Count;

    public void RequestTalentPick()
    {
        var choices = PassiveTalents.RollChoices();
        var entry = new PendingTalent(_nextPendingTalentId++, _player.Level, choices);
        _pendingTalents.Add((entry.Id, entry.Level, choices.Select(c => c.Id).ToArray()));
        TalentPickRequested?.Invoke(entry);
    }

    // Every talent still owed, with the exact three perks its offer originally rolled.
    public List<PendingTalent> EnumeratePendingTalents()
    {
        var list = new List<PendingTalent>();
        foreach (var (id, level, ids) in _pendingTalents)
        {
            var choices = ids.Select(PassiveTalents.ById)
                             .Where(p => p is not null)
                             .Select(p => p!)
                             .ToArray();
            if (choices.Length > 0) list.Add(new PendingTalent(id, level, choices));
        }
        return list;
    }

    // Takes one pending talent. No-op if it was already taken, so a stale dialog cannot grant
    // the same perk twice.
    public void ResolveTalentPick(int pendingId, PassiveTalents.Perk picked)
    {
        int i = _pendingTalents.FindIndex(e => e.Id == pendingId);
        if (i < 0) return;
        _pendingTalents.RemoveAt(i);
        ApplyTalent(picked);
    }

    public void ApplyTalent(PassiveTalents.Perk perk) => perk.Apply(_player);

    // ── Persistence ──
    public List<PendingTalentSave> SnapshotPendingTalents() =>
        _pendingTalents.Select(e => new PendingTalentSave
        {
            Id = e.Id,
            Level = e.Level,
            PerkIds = e.PerkIds.ToList(),
        }).ToList();

    public void RehydratePendingTalents(List<PendingTalentSave>? saved)
    {
        _pendingTalents.Clear();
        _nextPendingTalentId = 1;
        if (saved is null) return;
        foreach (var e in saved)
        {
            _pendingTalents.Add((e.Id, e.Level, e.PerkIds.ToArray()));
            // Keep issuing ids above anything restored, or a new offer would collide with one.
            if (e.Id >= _nextPendingTalentId) _nextPendingTalentId = e.Id + 1;
        }
    }

    // Fires after a Divine enters inventory (boss drop or quest reward).
    // Every Divine pickup gets full ceremony — no per-run cap.
    private void NotifyDivineObtained(Items.Equipment.Weapon divine)
    {
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

        // Milestone dispatcher — global event subscriptions are idempotent;
        // turn-scoped PlayerDiedDetailed binds per-instance.
        MilestoneSystem.Initialize();
        MilestoneSystem.HookTurnManager(this);

        // Barrier+N per-floor pool — seed from starting weapon so fresh runs
        // have the pool ready before the first hit. Refilled on floor entry.
        _barrierRemaining = GetBarrierCapacity();

        // Track Legendary collectables on pickup. Routes through MilestoneSystem;
        // the unlock guard inside LifetimeStats prevents double-fire.
        player.Inventory.Events.ItemAdded += (_, e) =>
        {
            if (e.Item.Rarity == "Legendary" && !string.IsNullOrEmpty(e.Item.DefinitionId))
            {
                MilestoneSystem.OnLegendaryCollected(player, e.Item.DefinitionId);
            }
        };

        // Equipment-state milestones (sealed/legendary/divine/Kirito-pair).
        player.Inventory.Events.ItemEquipped += (_, e) =>
            MilestoneSystem.OnItemEquipped(player, e.Equipment);

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
            // Timed effects are the buff system's business — Consumable.Use deliberately applies
            // only the instant ones, so nothing here double-applies.
            foreach (var timed in e.Consumable.Effects.TimedEffects)
                ApplyTimedBuff(timed.Type, timed.Potency, timed.Duration);

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
                // RegenerationRate buff: tick HP regen for RegenerationDuration turns.
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
        // Anti-Crystal Tyranny — all crystals inert.
        if (RunModifiers.IsActive(RunModifier.AntiCrystalTyranny))
        {
            _log.Log("The crystal hums but refuses to activate. Anti-Crystal field suppresses it.");
            return;
        }
        // Kayaba's Wager — teleport/corridor crystals specifically inert.
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
            // Anti-Crystal and Mirage Sphere set NO state and nothing reads them: they print a
            // sentence and end. Both are held unobtainable in ContentProbe's knownSourceless for
            // that reason rather than being given an invented mechanic — the Anti-Crystal is a
            // Laughing Coffin tool used ON a victim, so "suppresses your escape" would make a
            // 2,000 Col purchase self-harm, and the Mirage Sphere's evidence-recording has no
            // system behind it. Giving either a real effect is a design decision.
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
        tm._shrineBuff = save.ShrineBuffAmount; tm._levelUpBuff = save.LevelUpBuffAmount;
        tm._poisonDamagePerTick = save.PoisonDamagePerTick;
        tm._bleedDamagePerTick = save.BleedDamagePerTick;
        tm._invisibilityTurnsLeft = save.InvisibilityTurnsLeft;
        tm.RestoreTimedBuffs(save.TimedBuffs);
        // Restore food regen buff. 0 = inactive on legacy saves.
        tm._foodRegenRate = save.FoodRegenRate; tm._foodRegenTurnsLeft = save.FoodRegenTurnsLeft;
        tm._floorStartTurn = save.TurnCount; tm._floorStartRealTime = DateTime.Now;
        tm._floorKillsStart = save.KillCount; tm._restCounter = save.RestCounter;
        tm._bountyTarget = save.BountyTarget; tm._bountyKillsNeeded = save.BountyKillsNeeded;
        tm._bountyKillsCurrent = save.BountyKillsCurrent; tm._bountyRewardCol = save.BountyRewardCol;
        tm._bountyRewardXp = save.BountyRewardXp; tm._bountyComplete = save.BountyComplete;
        foreach (var kvp in save.WeaponKills) tm._weaponKills[kvp.Key] = kvp.Value;
        // IF Proficiency forks — rehydrate per-weapon pick state.
        tm.RehydrateForkChoices(save.WeaponProficiencyForks);
        tm.RehydratePendingTalents(save.PendingTalents);
        if (save.DiscoveredLore != null) foreach (var idx in save.DiscoveredLore) tm._discoveredLore.Add(idx);
        if (save.SeenTutorialTips != null) TutorialSystem.SeenTips = new HashSet<string>(save.SeenTutorialTips);
        if (save.ActiveQuests != null) QuestSystem.ActiveQuests = new List<Quest>(save.ActiveQuests);
        if (save.CompletedQuests != null) QuestSystem.CompletedQuests = new List<Quest>(save.CompletedQuests);
        QuestSystem.PinnedQuestId = save.PinnedQuestId;
        tm.IfImplementQuestsCompletedThisRun = save.IfImplementQuestsCompletedThisRun;
        tm.HfMissionsCompletedThisRun = save.HfMissionsCompletedThisRun;
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
                // Faction rename: legacy saves use "AincradLiberationSquad".
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
        // Per-floor boss-clear flags. Null on legacy = empty set.
        if (save.DefeatedFloorBosses != null)
            tm.DefeatedFloorBosses = new HashSet<int>(save.DefeatedFloorBosses);
        RunModifiers.LoadFromSave(save.ActiveRunModifiers);
        // Shop Tiering hydrates cross-save progress; legacy saves default to 0
        // (no tiered stock until the next F50+ clear).
        ShopTierSystem.SetForLoad(save.HighestFloorBossCleared);
        // Investments hydrate per-vendor; legacy saves = cleared state.
        VendorInvestmentSystem.SetForLoad(save.VendorInvestments);
        // Restore current-floor mining vein strikes (legacy = noop).
        SaveManager.RestoreVeinStrikes(save, map);

        // Restore party members
        PartySystem.Clear();
        if (save.PartyMembers != null)
        {
            foreach (var ad in save.PartyMembers)
            {
                var ally = new Entities.Ally(ad.Symbol, (Color)ad.SymbolColor)
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
