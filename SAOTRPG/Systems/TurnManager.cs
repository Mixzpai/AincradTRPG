using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Definitions.Weapons;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

public class TurnManager
{
    private GameMap _map;
    private readonly Player _player;
    private readonly IGameLog _log;

    public int TurnCount { get; private set; }
    public int KillCount { get; private set; }
    public int CurrentFloor { get; private set; }

    // ── Difficulty system ───────────────────────────────────────────
    // Index: 0=Story,1=VeryEasy,2=Easy,3=Normal,4=Hard,5=VeryHard,6=Masochist,7=Unwinnable,8=Debug
    // Add new difficulty tiers by adding an entry to each array below.
    public int Difficulty { get; }
    public bool IsHardcore { get; }

    // Mob stat multiplier per difficulty (percent: 100 = normal)
    private static readonly int[] MobStatScale =
        { 40, 60, 80, 100, 130, 170, 220, 300, 50 };

    // XP multiplier per difficulty (percent: 100 = normal)
    private static readonly int[] XpScale =
        { 200, 150, 120, 100, 90, 75, 60, 50, 500 };

    // Regen interval (turns between 1 HP regen; lower = faster)
    private static readonly int[] RegenInterval =
        { 3, 3, 4, 5, 6, 8, 10, 0, 3 };

    // ── Kill streak tracking + Col multiplier ─────────────────────────
    // Difficulty-scaled Col bonus at streak milestones (5/10/15 kills).
    // ColStreakBonus[difficulty] = percentage bonus per tier. Harder = smaller bonus.
    private static readonly int[] ColStreakBonus =
        { 8, 7, 6, 5, 4, 3, 2, 1, 10 };  // Story..Debug
    private int _killStreak;
    /// <summary>Current consecutive kill count (resets when player takes damage).</summary>
    public int KillStreak => _killStreak;

    // ── Idle turn tracking ────────────────────────────────────────────
    private int _idleTurns;

    // ── Dodge streak tracking ─────────────────────────────────────────
    private int _dodgeStreak;

    // ── Combo attack tracking — consecutive hits on same target ──────
    // Bonus damage per combo hit: hit 1 = +0, hit 2 = +2, hit 3 = +4, etc.
    private int _comboTarget;   // monster ID of current combo target
    private int _comboCount;    // consecutive hits on that target

    // ── Shrine buff — temporary ATK/DEF boost ─────────────────────
    private int _shrineBuff;
    private int _shrineBuffTurns;
    public int ShrineBuffTurns => _shrineBuffTurns;

    // ── Hunger system — satiety drains per turn, food restores it ────
    // MaxSatiety, drain rate, and thresholds are all tunable here.
    public int Satiety { get; private set; } = 100;
    public const int MaxSatiety = 100;
    private const int HungerDrainInterval = 3;  // lose 1 satiety every N turns
    private const int HungerRegenThreshold = 30; // below this, no passive HP regen
    private bool _starvingWarned;

    // ── Last killer (for death screen) ────────────────────────────────
    public string? LastKillerName { get; private set; }

    // ── Floor completion tracking ───────────────────────────────────
    private bool _floorFullyExplored;

    // ── Aggro alert tracking (per-monster, once per floor) ──────────
    private readonly HashSet<int> _aggroAlerted = new();
    private readonly HashSet<int> _dangerWarned = new();  // danger sense — once per monster

    // ── Staircase discovery (once per floor) ────────────────────────
    private bool _stairsDiscovered;

    // ── Floor timer — par turn count for speed bonus Col ──────────
    // Par turns per floor — clear faster for bonus Col. Extend by adding entries.
    private static readonly int[] FloorParTurns = { 200, 220, 250, 280, 320, 360, 400, 450, 500, 550 };
    private int _floorStartTurn;

    // ── Weapon proficiency — kills per weapon type grant passive damage bonus ──
    // Milestones: (kills required, bonus damage, rank name). Extend by adding entries.
    private static readonly (int Kills, int Bonus, string Rank)[] ProficiencyRanks =
    {
        (10,   1,  "Novice"),
        (25,   2,  "Apprentice"),
        (50,   4,  "Journeyman"),
        (100,  7,  "Expert"),
        (200,  11, "Master"),
        (350,  16, "Grandmaster"),
        (500,  22, "Sword Saint"),
        (750,  29, "Blade Dancer"),
        (1000, 37, "Weapon Lord"),
        (1500, 46, "Legendary"),
        (2000, 56, "Mythic"),
        (3000, 67, "Transcendent"),
        (4500, 80, "Divine Edge"),
        (6000, 95, "Aincrad's Chosen"),
        (9999, 120, "The Black Swordsman"),
    };
    private readonly Dictionary<string, int> _weaponKills = new();

    /// <summary>Current proficiency bonus damage for the given weapon type.</summary>
    public int GetProficiencyBonus(string weaponType)
    {
        int kills = _weaponKills.GetValueOrDefault(weaponType, 0);
        int bonus = 0;
        foreach (var rank in ProficiencyRanks)
            if (kills >= rank.Kills) bonus = rank.Bonus;
        return bonus;
    }

    /// <summary>Read-only access to weapon kill counts for UI display.</summary>
    public IReadOnlyDictionary<string, int> WeaponKills => _weaponKills;

    /// <summary>Returns current rank name and next rank info for a weapon type.</summary>
    public (string Rank, int Kills, int NextAt, string NextRank) GetProficiencyInfo(string weaponType)
    {
        int kills = _weaponKills.GetValueOrDefault(weaponType, 0);
        string rank = "Unranked";
        int nextAt = ProficiencyRanks[0].Kills;
        string nextRank = ProficiencyRanks[0].Rank;
        for (int i = 0; i < ProficiencyRanks.Length; i++)
        {
            if (kills >= ProficiencyRanks[i].Kills)
            {
                rank = ProficiencyRanks[i].Rank;
                if (i + 1 < ProficiencyRanks.Length)
                { nextAt = ProficiencyRanks[i + 1].Kills; nextRank = ProficiencyRanks[i + 1].Rank; }
                else
                { nextAt = -1; nextRank = "MAX"; }
            }
        }
        return (rank, kills, nextAt, nextRank);
    }

    // ── Stealth — Ctrl+direction halves monster aggro range for that move ──
    private bool _stealthActive;
    public bool IsStealthed => _stealthActive;

    // ── Level-up ATK buff — 10-turn damage boost after leveling up ──
    private int _levelUpBuff;
    private int _levelUpBuffTurns;
    public int LevelUpBuffTurns => _levelUpBuffTurns;

    // ── Monster flee tracking (once per monster) ────────────────────
    private readonly HashSet<int> _fleeAlerted = new();

    // ── Poison status effect ──────────────────────────────────────
    private int _poisonTurnsLeft;
    private int _poisonDamagePerTick = 2;

    // ── Bleed status effect ───────────────────────────────────────
    private int _bleedTurnsLeft;
    private int _bleedDamagePerTick = 1;

    // ── Ambient sound cues — throttled, 1 per 5 turns ────────────
    private int _lastSoundCueTurn = -99;
    private const int SoundCueCooldown = 5;

    // ── Play time tracking ──────────────────────────────────────
    private DateTime _sessionStart = DateTime.Now;
    private TimeSpan _priorPlayTime;
    public TimeSpan TotalPlayTime => _priorPlayTime + (DateTime.Now - _sessionStart);

    // ── Active save slot (for auto-save in AscendFloor) ──────────
    public int ActiveSaveSlot { get; set; } = 1;

    // ── Floor recap tracking ─────────────────────────────────────
    private int _floorDamageTaken;
    private int _floorItemsFound;
    private int _floorKillsStart;

    public event Action? TurnCompleted;
    public event Action? PlayerDied;
    public event Action<int>? FloorChanged;
    public event Action<Vendor>? VendorInteraction;
    public event Action? StairsConfirmRequested;
    public event Action? GameWon;
    /// <summary>Fires when damage is dealt at a map position. Args: (x, y, damage, isPlayerTakingDamage)</summary>
    public event Action<int, int, int, bool>? DamageDealt;
    /// <summary>Fires when a monster is killed at a map position.</summary>
    public event Action<int, int>? MonsterKilled;

    public TurnManager(GameMap map, Player player, IGameLog log, int floor = 1,
        int difficulty = 3, bool hardcore = false)
    {
        _map = map;
        _player = player;
        _log = log;
        CurrentFloor = floor;
        Difficulty = Math.Clamp(difficulty, 0, MobStatScale.Length - 1);
        IsHardcore = hardcore;

        // Antidote handler — clears poison and bleed when used
        player.Inventory.Events.ConsumableUsed += (_, e) =>
        {
            if (e.Consumable is Potion { PotionType: "Antidote" })
            {
                bool hadEffect = _poisonTurnsLeft > 0 || _bleedTurnsLeft > 0;
                _poisonTurnsLeft = 0;
                _bleedTurnsLeft = 0;
                if (hadEffect)
                    _log.LogSystem("The antidote purges all toxins from your body!");
                else
                    _log.Log("You feel fine already, but better safe than sorry.");
            }

            // Food handler — restores satiety
            if (e.Consumable is Food food)
            {
                int restore = food.RegenerationDuration * 2;  // e.g. Bread=20, Meat=30
                Satiety = Math.Min(MaxSatiety, Satiety + restore);
                _log.Log($"You feel sated. (Satiety: {Satiety}/{MaxSatiety})");
                _starvingWarned = false;
            }
        };
    }

    /// <summary>
    /// Creates a TurnManager restored from save data.
    /// </summary>
    public static TurnManager LoadFromSave(SaveData save, GameMap map, Player player, IGameLog log)
    {
        var tm = new TurnManager(map, player, log, save.CurrentFloor, save.Difficulty, save.IsHardcore);
        tm.TurnCount = save.TurnCount;
        tm.KillCount = save.KillCount;
        tm.Satiety = save.Satiety;
        tm._killStreak = save.KillStreak;
        tm._poisonTurnsLeft = save.PoisonTurnsLeft;
        tm._bleedTurnsLeft = save.BleedTurnsLeft;
        tm._shrineBuffTurns = save.ShrineBuffTurns;
        tm._levelUpBuffTurns = save.LevelUpBuffTurns;
        tm._floorStartTurn = save.TurnCount;
        tm._floorKillsStart = save.KillCount;

        foreach (var kvp in save.WeaponKills)
            tm._weaponKills[kvp.Key] = kvp.Value;

        tm._priorPlayTime = TimeSpan.FromSeconds(save.PlayTimeSeconds);

        return tm;
    }

    public void UpdateVisibility() => _map.UpdateVisibility(_player.X, _player.Y);

    /// <summary>
    /// Rest — skip 3 turns, heal 3 HP per turn. Monsters still move.
    /// Cannot rest when adjacent to an enemy or at full HP.
    /// </summary>
    public void ProcessRest()
    {
        if (_player.IsDefeated) return;
        if (_player.CurrentHealth >= _player.MaxHealth)
        {
            _log.Log("You're already at full health. No need to rest.");
            return;
        }

        // Check if any monster is adjacent — too dangerous to rest
        bool adjacent = _map.Entities.Any(e => e is Monster m && !m.IsDefeated
            && Math.Abs(m.X - _player.X) <= 1 && Math.Abs(m.Y - _player.Y) <= 1);
        if (adjacent)
        {
            _log.LogCombat("Enemies are too close! You can't rest here.");
            return;
        }

        _log.Log("You sit down and rest...");
        int totalHealed = 0;
        for (int i = 0; i < 3; i++)
        {
            if (_player.IsDefeated) break;
            int heal = Math.Min(3, _player.MaxHealth - _player.CurrentHealth);
            if (heal > 0)
            {
                _player.CurrentHealth += heal;
                totalHealed += heal;
            }
            TurnCount++;
            TickPoison();
            TickBleed();
            if (_player.IsDefeated) return;
            ProcessEntityTurns();
            if (_player.IsDefeated) return;
        }
        _log.LogSystem($"You feel refreshed. (+{totalHealed} HP)");
        UpdateVisibility();
        TurnCompleted?.Invoke();
    }

    /// <summary>
    /// Sprint: move 2 tiles in one turn. Fails if either tile is blocked or occupied.
    /// </summary>
    public void ProcessSprint(int dx, int dy)
    {
        if (_player.IsDefeated) return;
        int x1 = _player.X + dx, y1 = _player.Y + dy;
        int x2 = _player.X + dx * 2, y2 = _player.Y + dy * 2;

        // Both tiles must be in-bounds, walkable, and unoccupied
        if (!_map.InBounds(x1, y1) || !_map.InBounds(x2, y2))
        { _log.Log("Can't sprint that way."); return; }
        var t1 = _map.GetTile(x1, y1);
        var t2 = _map.GetTile(x2, y2);
        if (t1.BlocksMovement || t2.BlocksMovement || t1.Occupant != null || t2.Occupant != null)
        { _log.Log("Can't sprint — path blocked!"); return; }

        // Move 2 tiles, costs 1 turn (skips tile effects — you're running!)
        _map.MoveEntity(_player, x2, y2);
        TurnCount++;
        TickPoison(); TickBleed();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        if (_player.IsDefeated) return;
        UpdateVisibility();
        TurnCompleted?.Invoke();
    }

    /// <summary>
    /// Stealth move: Ctrl+direction. Move 1 tile but halve monster aggro range this turn.
    /// </summary>
    public void ProcessStealthMove(int dx, int dy)
    {
        _stealthActive = true;
        _log.Log("You move silently...");
        ProcessPlayerMove(dx, dy);
        _stealthActive = false;
    }

    public void ProcessPlayerMove(int dx, int dy)
    {
        if (_player.IsDefeated) return;

        // ── Idle flavor text tracking ─────────────────────────────────
        if (dx == 0 && dy == 0)
        {
            _idleTurns++;
            if (_idleTurns >= FlavorText.IdleThreshold && _idleTurns % FlavorText.IdleRepeatInterval == 0)
                _log.Log(FlavorText.IdleFlavors[Random.Shared.Next(FlavorText.IdleFlavors.Length)]);
        }
        else
        {
            _idleTurns = 0;
        }

        int tx = _player.X + dx;
        int ty = _player.Y + dy;

        var tile = _map.GetTile(tx, ty);

        if (tile.BlocksMovement)
        {
            // Occasional wall bump flavor (10% chance, avoids log spam)
            if (Random.Shared.Next(100) < 10)
                _log.Log(FlavorText.WallBumpFlavors[Random.Shared.Next(FlavorText.WallBumpFlavors.Length)]);
            return;
        }

        if (tile.Occupant != null)
        {
            var occupant = tile.Occupant;

            if (occupant is Monster monster && !monster.IsDefeated)
            {
                int hpBefore = _player.CurrentHealth;
                // Apply weapon proficiency bonus to damage
                var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
                string wpnType = wpn?.WeaponType ?? "Unarmed";
                int profBonus = GetProficiencyBonus(wpnType);

                // Combo tracking — consecutive hits on same target deal bonus damage
                if (_comboTarget == monster.Id)
                    _comboCount++;
                else
                { _comboTarget = monster.Id; _comboCount = 1; }
                int comboBonus = Math.Max(0, (_comboCount - 1) * 2);

                int damage = _player.AttackMonster(monster) + profBonus + comboBonus + _shrineBuff + _levelUpBuff;
                if (_comboCount >= 3)
                    _log.LogCombat($"  {_comboCount}-hit combo! (+{comboBonus} bonus damage)");
                var reward = monster.TakeDamage(damage);
                DamageDealt?.Invoke(monster.X, monster.Y, damage, false);
                DegradeEquipment(EquipmentSlot.Weapon);

                if (monster.IsDefeated && reward != null)
                {
                    KillCount++;
                    _killStreak++;

                    // Weapon proficiency — track kills, announce rank-ups
                    _weaponKills[wpnType] = _weaponKills.GetValueOrDefault(wpnType, 0) + 1;
                    int wk = _weaponKills[wpnType];
                    foreach (var rank in ProficiencyRanks)
                    {
                        if (wk == rank.Kills)
                            _log.LogSystem($"  {wpnType} proficiency: {rank.Rank}! (+{rank.Bonus} damage)");
                    }
                    string defeatMsg = string.Format(
                        FlavorText.DefeatFlavors[Random.Shared.Next(FlavorText.DefeatFlavors.Length)], monster.Name);
                    _log.LogCombat(defeatMsg);

                    // First kill celebration
                    if (KillCount == 1)
                        _log.LogSystem("Your first kill in Aincrad! The journey begins.");

                    // Kill streak callouts
                    string? streakMsg = _killStreak switch
                    {
                        2 => "Double Kill!",
                        3 => "Triple Kill!",
                        4 => "Quad Kill!",
                        5 => "Rampage!",
                        >= 6 => $"UNSTOPPABLE! ({_killStreak} streak)",
                        _ => null
                    };
                    if (streakMsg != null)
                        _log.LogCombat($"*** {streakMsg} ***");

                    // XP diminishing returns: halve XP if player is 3+ levels above mob
                    int levelDiff = _player.Level - monster.Level;
                    int xp = reward.Experience;
                    if (levelDiff >= 5) xp /= 4;
                    else if (levelDiff >= 3) xp /= 2;
                    // Difficulty XP scaling
                    xp = xp * XpScale[Difficulty] / 100;
                    xp = Math.Max(1, xp);

                    // Col scales with floor
                    int col = reward.Col;

                    // Kill streak Col multiplier — small difficulty-scaled bonus at milestones
                    // Tiers: 5 kills = 1x bonus, 10 = 2x, 15 = 3x (capped)
                    int streakTier = Math.Min(_killStreak / 5, 3);
                    int streakPct = streakTier * ColStreakBonus[Difficulty];
                    int streakBonus = col * streakPct / 100;

                    // Perfect kill bonus — one-shot kill with no damage taken
                    bool perfectKill = _player.CurrentHealth == hpBefore;
                    int perfectBonus = perfectKill ? col / 2 : 0;  // +50% Col for perfect kills

                    int lvlBefore = _player.Level;
                    _player.GainExperience(xp);
                    // Level-up overflow bonus — ATK buff for 10 turns on level-up
                    if (_player.Level > lvlBefore)
                    {
                        _levelUpBuff = 3 + _player.Level;
                        _levelUpBuffTurns = 10;
                        _log.LogSystem($"  Level-up surge! +{_levelUpBuff} ATK for 10 turns!");
                    }
                    _player.ColOnHand += col + perfectBonus + streakBonus;

                    // Kill feed — consolidated reward summary line
                    int totalCol = col + perfectBonus + streakBonus;
                    string profTick = "";
                    foreach (var rank in ProficiencyRanks)
                    {
                        if (wk == rank.Kills)
                        { profTick = $" [{wpnType} +{rank.Bonus}]"; break; }
                    }
                    string extras = "";
                    if (perfectKill) extras += " PERFECT!";
                    if (streakBonus > 0) extras += $" STREAK+{streakBonus}";
                    _log.LogCombat($"  >> {monster.Name} slain! +{xp} XP +{totalCol} Col{profTick}{extras}");
                    if (reward.WasOverkill)
                    {
                        string okMsg = string.Format(
                            FlavorText.OverkillFlavors[Random.Shared.Next(FlavorText.OverkillFlavors.Length)],
                            reward.OverkillDamage * 2);
                        _log.LogCombat(okMsg);
                    }

                    // Boss defeat fanfare — unlocks stairs + reveals full map
                    if (monster is Boss boss)
                    {
                        _log.LogSystem("════════════════════════════════════");
                        _log.LogSystem($"  FLOOR BOSS DEFEATED: {boss.Name}!");
                        _log.LogSystem("  The stairs to the next floor are now open!");
                        _log.LogSystem("════════════════════════════════════");

                        // Reveal entire map on boss kill
                        for (int rx = 0; rx < _map.Width; rx++)
                        for (int ry = 0; ry < _map.Height; ry++)
                            _map.SetExplored(rx, ry);
                        _log.LogSystem("  The floor's layout is revealed on the minimap!");
                    }

                    // Loot drop chance
                    DropLoot(monster);

                    MonsterKilled?.Invoke(monster.X, monster.Y);
                    _map.RemoveEntity(monster);

                    // Floor cleared check
                    if (GetMonsterCount() == 0)
                        _log.LogSystem(FlavorText.FloorClearedMessages[Random.Shared.Next(FlavorText.FloorClearedMessages.Length)]);
                }
                else if (!monster.IsDefeated)
                {
                    // Show monster remaining HP with mini ASCII bar
                    int pct = (int)Math.Round(100.0 * monster.CurrentHealth / monster.MaxHealth);
                    string hpBar = BuildMobHpBar(monster.CurrentHealth, monster.MaxHealth, 12);
                    string condition = pct > 75 ? "healthy" : pct > 50 ? "wounded" : pct > 25 ? "badly hurt" : "near death";
                    _log.LogCombat($"  {monster.Name} {hpBar} {monster.CurrentHealth}/{monster.MaxHealth} ({condition})");

                    if (pct <= 10)
                        _log.LogCombat($"  >> {monster.Name} staggers! One more hit! <<");
                    else if (pct <= 25)
                        _log.LogCombat($"  {monster.Name} is barely standing...");
                }

                AdvanceTurn();
                TickPoison();
                TickBleed();
                if (_player.IsDefeated) return;
                ProcessEntityTurns();
                PassiveRegen();
                TurnCompleted?.Invoke();
                return;
            }

            if (occupant is Vendor vendor)
            {
                string greeting = string.Format(
                    FlavorText.VendorGreetings[Random.Shared.Next(FlavorText.VendorGreetings.Length)],
                    vendor.ShopName ?? "my shop");
                _log.Log($"{vendor.Name}: \"{greeting}\"");
                VendorInteraction?.Invoke(vendor);
                return;
            }

            if (occupant is NPC npc)
            {
                string dialogue = npc.Dialogue
                    ?? FlavorText.NpcFallbackDialogue[Random.Shared.Next(FlavorText.NpcFallbackDialogue.Length)];
                _log.Log($"{npc.Name}: \"{dialogue}\"");
                return;
            }

            return;
        }

        _map.MoveEntity(_player, tx, ty);
        _map.IncrementVisit(tx, ty);

        // ── Trap effects ──
        if (tile.Type == TileType.TrapSpike)
        {
            int trapDmg = 3 + CurrentFloor * 2;
            _player.TakeDamage(trapDmg);
            DamageDealt?.Invoke(tx, ty, trapDmg, true);
            _log.LogCombat(string.Format(
                FlavorText.SpikeTrapFlavors[Random.Shared.Next(FlavorText.SpikeTrapFlavors.Length)], trapDmg));
            // Disarm after triggering
            tile.Type = TileType.Floor;
            if (_player.IsDefeated)
            {
                LastKillerName = "a spike trap";
                _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                PlayerDied?.Invoke();
                return;
            }
        }
        else if (tile.Type == TileType.TrapTeleport)
        {
            _log.LogCombat("A teleport trap activates! You're warped to a random location!");
            // Find a random walkable spot
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int rx = Random.Shared.Next(5, _map.Width - 5);
                int ry = Random.Shared.Next(5, _map.Height - 5);
                var rtile = _map.GetTile(rx, ry);
                if (!rtile.BlocksMovement && rtile.Occupant == null
                    && rtile.Type != TileType.TrapTeleport && rtile.Type != TileType.TrapSpike)
                {
                    _map.MoveEntity(_player, rx, ry);
                    _log.LogCombat(FlavorText.TeleportLandingFlavors[Random.Shared.Next(FlavorText.TeleportLandingFlavors.Length)]);
                    break;
                }
            }
            // Disarm after triggering
            tile.Type = TileType.Floor;
        }

        // ── Campfire healing + status cleanse + rest quotes ──
        if (tile.Type == TileType.Campfire)
        {
            // Cleanse poison/bleed
            bool hadStatus = _poisonTurnsLeft > 0 || _bleedTurnsLeft > 0;
            if (hadStatus)
            {
                _poisonTurnsLeft = 0;
                _bleedTurnsLeft = 0;
                _log.LogSystem("The campfire's warmth purges the toxins from your body!");
            }

            int heal = Math.Min(15 + CurrentFloor * 5, _player.MaxHealth - _player.CurrentHealth);
            if (heal > 0)
            {
                _player.CurrentHealth += heal;
                _log.Log($"The campfire's warmth restores {heal} HP.");
                _log.Log(FlavorText.CampfireQuotes[Random.Shared.Next(FlavorText.CampfireQuotes.Length)]);
            }
            else if (!hadStatus)
            {
                _log.Log(FlavorText.CampfireFullHpFlavors[Random.Shared.Next(FlavorText.CampfireFullHpFlavors.Length)]);
            }
        }

        // ── Fountain — free heal (becomes empty after use) ──
        if (tile.Type == TileType.Fountain)
        {
            int heal = Math.Min(30 + CurrentFloor * 10, _player.MaxHealth - _player.CurrentHealth);
            if (heal > 0)
            {
                _player.CurrentHealth += heal;
                _log.LogSystem($"You drink from the fountain and restore {heal} HP!");
            }
            else
                _log.Log("The fountain's water is refreshing, but you're already at full health.");
            Satiety = Math.Min(MaxSatiety, Satiety + 20);
            tile.Type = TileType.Floor;  // one use — fountain dries up
        }

        // ── Shrine — temporary ATK/DEF buff for 30 turns ──
        if (tile.Type == TileType.Shrine)
        {
            int buff = 3 + CurrentFloor;
            _shrineBuff = buff;
            _shrineBuffTurns = 30;
            _log.LogSystem($"The shrine empowers you! (+{buff} ATK & DEF for 30 turns)");
            tile.Type = TileType.Floor;  // one use
        }

        // ── Pillar — reveals a large area of the map ──
        if (tile.Type == TileType.Pillar)
        {
            int revealRadius = 15;
            for (int px = -revealRadius; px <= revealRadius; px++)
            for (int py = -revealRadius; py <= revealRadius; py++)
            {
                int rx = _player.X + px, ry = _player.Y + py;
                if (_map.InBounds(rx, ry))
                    _map.SetExplored(rx, ry);
            }
            _log.LogSystem("The ancient pillar hums — a vision of the surrounding area fills your mind!");
            tile.Type = TileType.Floor;  // one use
        }

        // ── Lava tile — deals damage every turn the player stands on it ──
        if (tile.Type == TileType.Lava)
        {
            int lavaDmg = 4 + CurrentFloor * 2;
            _player.TakeDamage(lavaDmg);
            _log.LogCombat($"The molten lava burns you! {lavaDmg} damage!");
            if (_player.IsDefeated)
            {
                LastKillerName = "lava";
                _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                PlayerDied?.Invoke();
                return;
            }
        }

        // ── Terrain footstep flavor (occasional) ──
        if (FlavorText.TerrainFlavors.TryGetValue(tile.Type, out var flavors)
            && Random.Shared.Next(100) < FlavorText.FootstepChance)
        {
            _log.Log(flavors[Random.Shared.Next(flavors.Length)]);
        }

        // ── Trap proximity sense (Dexterity-based) ──
        if (Random.Shared.Next(100) < Math.Min(30, _player.Dexterity * 3))
        {
            for (int tdx = -1; tdx <= 1; tdx++)
            for (int tdy = -1; tdy <= 1; tdy++)
            {
                if (tdx == 0 && tdy == 0) continue;
                if (_map.InBounds(tx + tdx, ty + tdy))
                {
                    var adjType = _map.GetTile(tx + tdx, ty + tdy).Type;
                    if (adjType == TileType.TrapSpike || adjType == TileType.TrapTeleport)
                    {
                        _log.LogCombat(FlavorText.TrapSenseFlavors[Random.Shared.Next(FlavorText.TrapSenseFlavors.Length)]);
                        goto doneTraps; // only warn once per move
                    }
                }
            }
        }
        doneTraps:

        // ── Ambient atmosphere (occasional) ──
        if (Random.Shared.Next(100) < FlavorText.AmbientChance)
            _log.Log(FlavorText.AmbientMessages[Random.Shared.Next(FlavorText.AmbientMessages.Length)]);

        // ── Staircase discovery (check adjacent tiles) ──
        if (!_stairsDiscovered)
        {
            for (int sdx = -1; sdx <= 1; sdx++)
            for (int sdy = -1; sdy <= 1; sdy++)
            {
                if (_map.InBounds(tx + sdx, ty + sdy)
                    && _map.GetTile(tx + sdx, ty + sdy).Type == TileType.StairsUp)
                {
                    _stairsDiscovered = true;
                    _log.LogSystem(FlavorText.StairsDiscoveryMessages[Random.Shared.Next(FlavorText.StairsDiscoveryMessages.Length)]);
                    break;
                }
            }
        }

        if (tile.Type == TileType.StairsUp)
        {
            // Boss gate — must defeat the floor boss before ascending
            bool bossAlive = _map.Entities.Any(e => e is Boss && !e.IsDefeated);
            if (bossAlive)
            {
                _log.LogCombat("The stairs are sealed by a powerful force. Defeat the Floor Boss first!");
                TurnCompleted?.Invoke();
                return;
            }
            StairsConfirmRequested?.Invoke();
            TurnCompleted?.Invoke();
            return;
        }

        TurnCount++;
        TickPoison();
        TickBleed();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        PassiveRegen();
        UpdateVisibility();
        CheckFloorCompletion();
        CheckSoundCues();
        TurnCompleted?.Invoke();
    }

    /// <summary>
    /// Ambient sound cues — log a hint when a nearby monster is out of sight.
    /// Throttled: max 1 cue per SoundCueCooldown turns, closest monster only.
    /// </summary>
    private void CheckSoundCues()
    {
        if (TurnCount - _lastSoundCueTurn < SoundCueCooldown) return;

        Monster? closest = null;
        int closestDist = int.MaxValue;

        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster monster) continue;
            if (_map.IsVisible(monster.X, monster.Y)) continue;  // must be unseen

            int dist = Math.Max(Math.Abs(monster.X - _player.X), Math.Abs(monster.Y - _player.Y));
            int aggro = monster is Mob mob ? mob.AggroRange : 6;
            if (dist <= aggro + 2 && dist < closestDist)
            {
                closest = monster;
                closestDist = dist;
            }
        }

        if (closest == null) return;

        string tag = closest is Mob m2 ? m2.LootTag : "generic";
        string[] cues = FlavorText.SoundCues.GetValueOrDefault(tag, FlavorText.GenericSoundCues)!;
        _log.Log(cues[Random.Shared.Next(cues.Length)]);
        _lastSoundCueTurn = TurnCount;
    }

    public string GetTileInfo(int x, int y)
    {
        if (!_map.InBounds(x, y)) return "Out of bounds";

        var tile = _map.GetTile(x, y);
        if (tile.Occupant != null && !tile.Occupant.IsDefeated)
        {
            var e = tile.Occupant;
            if (e is Monster m)
            {
                int diff = m.Level - _player.Level;
                string threat = diff switch
                {
                    >= 5  => "[!! DEADLY !!]",
                    >= 3  => "[! Dangerous]",
                    >= 1  => "[Strong]",
                    0     => "[Even match]",
                    >= -2 => "[Weaker]",
                    _     => "[Trivial]",
                };
                // Ability tags for mobs
                string abilities = "";
                if (m is Mob mob)
                {
                    var tags = new List<string>();
                    if (mob.CanPoison) tags.Add("Poison");
                    if (mob.CanBleed) tags.Add("Bleed");
                    if (tags.Count > 0) abilities = " [" + string.Join(",", tags) + "]";
                }
                return $"{m.Name} (Lv.{m.Level}) {threat} HP:{m.CurrentHealth}/{m.MaxHealth} ATK:{m.BaseAttack} DEF:{m.BaseDefense}{abilities}";
            }
            if (e is Vendor v)
                return $"{v.Name} — [{v.ShopName ?? "Shop"}] Bump to browse";
            if (e is NPC npc)
                return $"{npc.Name} — Bump to talk";
            return e.Name;
        }

        string terrain = tile.Type switch
        {
            TileType.Grass or TileType.GrassTall or TileType.GrassSparse => "Grassland",
            TileType.Flowers => "Wildflowers",
            TileType.Path => "Dirt path",
            TileType.Floor => "Stone floor",
            TileType.Wall => "Stone wall",
            TileType.Water or TileType.WaterDeep => "Water",
            TileType.Tree or TileType.TreePine => "Tree",
            TileType.Bush => "Bush",
            TileType.Mountain => "Mountain rock",
            TileType.Rock => "Boulder",
            TileType.Door => "Doorway",
            TileType.StairsUp => _map.Entities.Any(e => e is Boss && !e.IsDefeated)
                ? "Stairs leading up [SEALED — defeat the Boss]"
                : "Stairs leading up [OPEN]",
            TileType.TrapSpike => "Suspicious ground...",
            TileType.TrapTeleport => "Strange markings on the ground...",
            TileType.Lava => "Molten lava — standing here deals damage!",
            TileType.Campfire => "A warm campfire (heals HP, cures status)",
            TileType.Fountain => "A crystal fountain (step on to heal + restore hunger)",
            TileType.Shrine => "An ancient shrine (step on for a temporary buff)",
            TileType.Pillar => "A glowing pillar (step on to reveal the map)",
            _ => "Unknown terrain"
        };

        if (tile.HasItems)
            terrain += $" [{tile.Items.Count} item(s)]";

        return terrain;
    }

    public void AscendFloor()
    {
        // Floor speed bonus — reward clearing under par
        int elapsed = TurnCount - _floorStartTurn;
        int par = FloorParTurns[Math.Min(CurrentFloor - 1, FloorParTurns.Length - 1)];
        if (elapsed <= par)
        {
            int bonus = 50 + CurrentFloor * 30;
            _player.ColOnHand += bonus;
            _log.LogSystem($"  SPEED CLEAR! Finished in {elapsed} turns (par: {par}). +{bonus} Col!");
        }

        // Floor recap — summary of what happened on this floor
        int floorKills = KillCount - _floorKillsStart;
        int explored = _map.GetExplorationPercent();
        _log.LogSystem("── Floor Recap ──");
        _log.LogSystem($"  Kills: {floorKills} | Items: {_floorItemsFound} | Damage taken: {_floorDamageTaken}");
        _log.LogSystem($"  Turns: {elapsed} | Explored: {explored}%");

        CurrentFloor++;

        // Auto-save checkpoint on floor transition
        if (SaveManager.SaveGame(_player, this, ActiveSaveSlot))
            _log.LogSystem("  [Game saved]");

        // ── Victory — cleared all 100 floors of Aincrad ──
        if (CurrentFloor > 100)
        {
            UpdatePlayerTitle();
            _log.LogSystem("════════════════════════════════════");
            _log.LogSystem("  You have cleared all 100 floors of Aincrad!");
            _log.LogSystem("  The death game is over. You are free.");
            _log.LogSystem("════════════════════════════════════");
            GameWon?.Invoke();
            return;
        }

        _floorStartTurn = TurnCount;
        _floorFullyExplored = false;
        _stairsDiscovered = false;
        _aggroAlerted.Clear();
        _fleeAlerted.Clear();
        _dangerWarned.Clear();
        _poisonTurnsLeft = 0;
        _bleedTurnsLeft = 0;
        _floorDamageTaken = 0;
        _floorItemsFound = 0;
        _floorKillsStart = KillCount;
        _log.LogSystem("════════════════════════════════════");
        _log.LogSystem($"  Ascending to Floor {CurrentFloor} of Aincrad...");
        _log.LogSystem("════════════════════════════════════");
        GenerateNewFloor();
        UpdateVisibility();
        UpdatePlayerTitle();

        // Floor flavor text — SAO-themed per tier, generic fallback
        string flavor = CurrentFloor switch
        {
            2  => "The air grows warmer. Strange insects buzz in the distance.",
            3  => "Thick forest canopy blocks the sky. Something watches from the shadows.",
            4  => "Cold stone corridors stretch before you. The undead stir.",
            5  => "The ground trembles. A powerful presence guards this floor.",
            6  => "Iron doors creak open. The metallic stench of rust fills the air.",
            7  => "Shadows move on their own here. Trust nothing.",
            8  => "Crystal formations line the walls, glowing faintly.",
            9  => "The wind howls through collapsed archways. You press onward.",
            10 => "A vast cavern opens before you. This is the domain of something ancient.",
            <= 20 => "The floors of Aincrad grow more treacherous. Stay alert.",
            <= 50 => "Few players have ventured this deep. Every step is earned.",
            _  => "The air itself feels hostile. Only the strongest survive here."
        };
        _log.Log(flavor);

        // Time-of-day atmosphere based on real clock
        string timeNote = DateTime.Now.Hour switch
        {
            >= 5 and < 8   => "Dawn light filters through Aincrad's crystal ceiling.",
            >= 8 and < 12  => "Morning sun illuminates the floor above.",
            >= 12 and < 17 => "The midday glow casts sharp shadows on the walls.",
            >= 17 and < 20 => "Evening amber washes through the upper floors.",
            >= 20 and < 23 => "Aincrad's night sky glimmers far above.",
            _              => "Deep night. The dungeon is at its darkest.",
        };
        _log.Log(timeNote);

        // Announce enemy count on new floor
        int mobs = GetMonsterCount();
        if (mobs > 0)
        {
            string mobMsg = string.Format(
                FlavorText.FloorMobCountFlavors[Random.Shared.Next(FlavorText.FloorMobCountFlavors.Length)], mobs);
            _log.LogCombat(mobMsg);
        }

        // Roll weather for new floor
        WeatherSystem.RollWeather(CurrentFloor);
        if (WeatherSystem.Current != WeatherType.Clear)
            _log.LogSystem($"Weather: {WeatherSystem.GetLabel()}");

        FloorChanged?.Invoke(CurrentFloor);
        TurnCompleted?.Invoke();
    }

    public GameMap Map => _map;

    /// <summary>Turns elapsed on the current floor (resets each floor change).</summary>
    public int FloorTurns => TurnCount - _floorStartTurn;

    /// <summary>
    /// Returns a compass direction string toward the nearest living monster,
    /// or empty string if none found. For easy-difficulty HUD display.
    /// </summary>
    public string GetDangerCompass()
    {
        Monster? closest = null;
        int closestDist = int.MaxValue;
        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster m) continue;
            int dist = Math.Abs(m.X - _player.X) + Math.Abs(m.Y - _player.Y);
            if (dist < closestDist) { closest = m; closestDist = dist; }
        }
        if (closest == null) return "";
        int dx = closest.X - _player.X;
        int dy = closest.Y - _player.Y;
        string dir = (dx, dy) switch
        {
            ( > 0,  < 0) => "NE",
            ( < 0,  < 0) => "NW",
            ( > 0,  > 0) => "SE",
            ( < 0,  > 0) => "SW",
            (   0,  < 0) => "N",
            (   0,  > 0) => "S",
            ( > 0,    0) => "E",
            ( < 0,    0) => "W",
            _            => "?"
        };
        return $"Threat: {dir}({closestDist})";
    }

    /// <summary>Returns a contextual action hint based on surroundings.</summary>
    public string GetContextHint()
    {
        var tile = _map.GetTile(_player.X, _player.Y);

        // Underfoot hints first
        if (tile.HasItems) return "[G] Pick up";
        if (tile.Type == TileType.StairsUp) return "[Bump] Ascend";
        if (tile.Type == TileType.Campfire) return "Resting...";

        // Adjacent entity hints
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = _player.X + dx, ny = _player.Y + dy;
            if (!_map.InBounds(nx, ny)) continue;
            var adj = _map.GetTile(nx, ny);
            if (adj.Occupant is Vendor) return "[Bump] Shop";
            if (adj.Occupant is Monster m && !m.IsDefeated) return "[Bump] Attack";
        }

        return "";
    }

    /// <summary>Returns count of living monsters on the current floor.</summary>
    public int GetMonsterCount() =>
        _map.Entities.Count(e => e is Monster && !e.IsDefeated);

    private void GenerateNewFloor()
    {
        var (newMap, rooms) = MapGenerator.GenerateFloor(CurrentFloor);
        _map = newMap;

        // Place player at the first room's center (spawn clearing)
        MapGenerator.PopulateFloor(newMap, rooms, _player, CurrentFloor, MobStatScale[Difficulty]);

        // Wire logs for new entities
        foreach (var entity in newMap.Entities)
            entity.SetLog(_log);
    }

    // ── Loot tables, rarity scaling, and equipment creation live in LootGenerator.cs ──

    private void DropLoot(Monster monster)
    {
        var tile = _map.GetTile(monster.X, monster.Y);

        // Mob-type-specific themed drop (40% chance if loot tag exists)
        if (monster is Mob mob && LootGenerator.MobLootTable.TryGetValue(mob.LootTag, out var lootTable)
            && Random.Shared.Next(100) < 40)
        {
            var entry = lootTable[Random.Shared.Next(lootTable.Length)];
            var lootItem = new Items.Materials.MobDrop
            {
                Name = entry.Name, Value = entry.Value + CurrentFloor * 3,
                SourceMonster = monster.Name, Quantity = 1, MaxStacks = 10
            };
            DropItem(tile, lootItem, monster.Name);
        }

        // Consumable drop (30% potion, 15% food)
        int roll = Random.Shared.Next(100);
        if (roll < 30)
            DropItem(tile, PotionDefinitions.CreateHealthPotion(), monster.Name);
        else if (roll < 45)
            DropItem(tile, FoodDefinitions.CreateBread(), monster.Name);

        // Antidote drop from poisonous mobs (40% chance)
        if (monster is Mob { CanPoison: true } && Random.Shared.Next(100) < 40)
            DropItem(tile, PotionDefinitions.CreateAntidote(), monster.Name);

        // Equipment drop (10% chance)
        if (Random.Shared.Next(100) < 10)
        {
            var gear = LootGenerator.CreateRandomEquipment(CurrentFloor);
            if (gear != null)
                DropItem(tile, gear, monster.Name);
        }
    }

    private void DropItem(Tile tile, BaseItem item, string source)
    {
        tile.Items.Add(item);
        _log.LogSystem($"  {source} dropped a {item.Name}!");

        // Rare+ loot fanfare
        if (item.Rarity is "Rare" or "Epic")
        {
            string msg = string.Format(
                FlavorText.RareLootFlavors[Random.Shared.Next(FlavorText.RareLootFlavors.Length)],
                item.Rarity, item.Name);
            _log.LogSystem(msg);
        }
    }

    // ── Loot creation, rarity scaling, and equipment name tables live in LootGenerator.cs ──
    // ── Flavor text arrays live in FlavorText.cs ──

    // Mini HP bar for monsters: [####----] style
    private static string BuildMobHpBar(int current, int max, int width)
    {
        int filled = max > 0 ? (int)Math.Round((double)current / max * width) : 0;
        filled = Math.Clamp(filled, 0, width);
        return "[" + new string('#', filled) + new string('-', width - filled) + "]";
    }

    public void PickupItems()
    {
        var tile = _map.GetTile(_player.X, _player.Y);
        if (!tile.HasItems)
        {
            _log.Log(FlavorText.EmptyGroundFlavors[Random.Shared.Next(FlavorText.EmptyGroundFlavors.Length)]);
            return;
        }

        var picked = new List<BaseItem>();
        foreach (var item in tile.Items.ToList())
        {
            if (_player.Inventory.AddItem(item))
            {
                picked.Add(item);
                _floorItemsFound++;
                tile.Items.Remove(item);
                string pickMsg = string.Format(
                    FlavorText.PickupFlavors[Random.Shared.Next(FlavorText.PickupFlavors.Length)], item.Name);
                _log.LogSystem(pickMsg);
            }
            else
            {
                _log.Log(FlavorText.InventoryFullFlavors[Random.Shared.Next(FlavorText.InventoryFullFlavors.Length)]);
                break;
            }
        }
    }

    /// <summary>
    /// Auto-explore: BFS to nearest unexplored tile, take one step toward it.
    /// Returns false if no unexplored tiles reachable or interrupted by danger.
    /// </summary>
    public bool AutoExploreStep()
    {
        if (_player.IsDefeated) return false;

        // Check for visible monsters — stop auto-explore
        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster) continue;
            int dist = Math.Max(Math.Abs(entity.X - _player.X), Math.Abs(entity.Y - _player.Y));
            if (dist <= 6 && _map.IsVisible(entity.X, entity.Y))
            {
                _log.Log(FlavorText.AutoExploreEnemyFlavors[Random.Shared.Next(FlavorText.AutoExploreEnemyFlavors.Length)]);
                return false;
            }
        }

        // Check for items underfoot
        var currentTile = _map.GetTile(_player.X, _player.Y);
        if (currentTile.HasItems)
        {
            _log.Log(FlavorText.AutoExploreItemFlavors[Random.Shared.Next(FlavorText.AutoExploreItemFlavors.Length)]);
            return false;
        }

        // BFS from player to find nearest unexplored-adjacent walkable tile
        var visited = new bool[_map.Width, _map.Height];
        var parent = new (int x, int y)?[_map.Width, _map.Height];
        var queue = new Queue<(int x, int y)>();

        queue.Enqueue((_player.X, _player.Y));
        visited[_player.X, _player.Y] = true;

        (int x, int y) target = (-1, -1);

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();

            // Check if this tile is adjacent to an unexplored tile
            if (IsAdjacentToUnexplored(cx, cy))
            {
                target = (cx, cy);
                break;
            }

            // Expand neighbors (8 directions)
            for (int ndx = -1; ndx <= 1; ndx++)
            for (int ndy = -1; ndy <= 1; ndy++)
            {
                if (ndx == 0 && ndy == 0) continue;
                int nx = cx + ndx, ny = cy + ndy;
                if (!_map.InBounds(nx, ny) || visited[nx, ny]) continue;
                var tile = _map.GetTile(nx, ny);
                if (tile.BlocksMovement || (tile.Occupant != null && tile.Occupant != _player)) continue;
                visited[nx, ny] = true;
                parent[nx, ny] = (cx, cy);
                queue.Enqueue((nx, ny));
            }
        }

        if (target.x == -1)
        {
            _log.Log(FlavorText.AutoExploreDoneFlavors[Random.Shared.Next(FlavorText.AutoExploreDoneFlavors.Length)]);
            return false;
        }

        // Trace back to find the first step
        var step = target;
        while (parent[step.x, step.y] != null)
        {
            var p = parent[step.x, step.y]!.Value;
            if (p.x == _player.X && p.y == _player.Y)
                break;
            step = p;
        }

        int dx = step.x - _player.X;
        int dy = step.y - _player.Y;
        ProcessPlayerMove(dx, dy);
        return true;
    }

    private bool IsAdjacentToUnexplored(int x, int y)
    {
        for (int ndx = -1; ndx <= 1; ndx++)
        for (int ndy = -1; ndy <= 1; ndy++)
        {
            if (ndx == 0 && ndy == 0) continue;
            int nx = x + ndx, ny = y + ndy;
            if (_map.InBounds(nx, ny) && !_map.IsExplored(nx, ny))
                return true;
        }
        return false;
    }

    /// <summary>Fires a one-time message when the floor reaches 100% explored.</summary>
    private void CheckFloorCompletion()
    {
        if (_floorFullyExplored) return;
        if (_map.GetExplorationPercent() >= 100)
        {
            _floorFullyExplored = true;
            _log.LogSystem(FlavorText.FloorCompleteMessages[Random.Shared.Next(FlavorText.FloorCompleteMessages.Length)]);
        }
    }

    /// <summary>Increments turn count and checks for milestone callouts.</summary>
    private void AdvanceTurn()
    {
        TurnCount++;

        // Turn milestone callouts
        if (TurnCount > 0 && TurnCount % FlavorText.MilestoneInterval == 0)
        {
            string msg = FlavorText.MilestoneMessages[Random.Shared.Next(FlavorText.MilestoneMessages.Length)];
            _log.LogSystem(string.Format(msg, TurnCount));
        }

        // Hunger drain — satiety decreases over time
        if (TurnCount % HungerDrainInterval == 0 && Satiety > 0)
            Satiety--;

        // Starvation damage at 0 satiety
        if (Satiety <= 0 && !_player.IsDefeated)
        {
            _player.TakeDamage(1);
            if (!_starvingWarned)
            {
                _log.LogCombat("You're starving! Eat something before it's too late!");
                _starvingWarned = true;
            }
            if (_player.IsDefeated)
            {
                LastKillerName = "starvation";
                _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                PlayerDied?.Invoke();
            }
        }
        else if (Satiety == 15 && !_starvingWarned)
        {
            _log.Log("Your stomach growls... you're getting hungry.");
        }

        // Shrine buff tick-down
        if (_shrineBuffTurns > 0)
        {
            _shrineBuffTurns--;
            if (_shrineBuffTurns == 0)
            {
                _shrineBuff = 0;
                _log.Log("The shrine's blessing fades...");
            }
        }

        // Level-up ATK buff tick-down
        if (_levelUpBuffTurns > 0)
        {
            _levelUpBuffTurns--;
            if (_levelUpBuffTurns == 0)
            {
                _levelUpBuff = 0;
                _log.Log("The level-up surge fades.");
            }
        }
    }

    /// <summary>Updates the player's title based on highest floor reached.</summary>
    private void UpdatePlayerTitle()
    {
        foreach (var (floor, title) in FlavorText.TitleThresholds)
        {
            if (CurrentFloor >= floor)
            {
                if (_player.Title != title)
                {
                    _player.Title = title;
                    _log.LogSystem($"You have earned the title: {title}!");
                }
                break;
            }
        }
    }

    private void PassiveRegen()
    {
        // Regen 1 HP every N turns (difficulty-scaled). 0 = no regen.
        int interval = RegenInterval[Difficulty];
        if (_player.IsDefeated) return;
        if (interval <= 0) return;  // Unwinnable: no passive regen
        if (Satiety < HungerRegenThreshold) return;  // too hungry to regen
        if (TurnCount % interval != 0) return;
        if (_player.CurrentHealth >= _player.MaxHealth) return;

        _player.CurrentHealth = Math.Min(_player.CurrentHealth + 1, _player.MaxHealth);

        // Occasional regen flavor (30% chance to avoid log spam)
        if (Random.Shared.Next(100) < 30)
            _log.Log(FlavorText.RegenFlavors[Random.Shared.Next(FlavorText.RegenFlavors.Length)]);
    }

    private void TickPoison()
    {
        if (_player.IsDefeated || _poisonTurnsLeft <= 0) return;
        _player.TakeDamage(_poisonDamagePerTick);
        _poisonTurnsLeft--;
        string suffix = _poisonTurnsLeft > 0 ? $" ({_poisonTurnsLeft} turns left)" : " (poison wears off)";
        _log.LogCombat($"Poison deals {_poisonDamagePerTick} damage!{suffix}");
        if (_player.IsDefeated)
        {
            LastKillerName = "poison";
            _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
            PlayerDied?.Invoke();
        }
    }

    /// <summary>True if the player is currently poisoned.</summary>
    public bool IsPoisoned => _poisonTurnsLeft > 0;
    public int PoisonTurnsLeft => _poisonTurnsLeft;

    private void TickBleed()
    {
        if (_player.IsDefeated || _bleedTurnsLeft <= 0) return;
        _player.TakeDamage(_bleedDamagePerTick);
        _bleedTurnsLeft--;
        string suffix = _bleedTurnsLeft > 0 ? $" ({_bleedTurnsLeft} turns left)" : " (bleeding stops)";
        _log.LogCombat($"Bleed deals {_bleedDamagePerTick} damage!{suffix}");
        if (_player.IsDefeated)
        {
            LastKillerName = "bleeding";
            _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
            PlayerDied?.Invoke();
        }
    }

    /// <summary>True if the player is currently bleeding.</summary>
    public bool IsBleeding => _bleedTurnsLeft > 0;
    public int BleedTurnsLeft => _bleedTurnsLeft;

    // Equipment durability — weapons degrade on attack, armor degrades on hit.
    // At 0 durability the item breaks (unequipped + destroyed). Extend by adding more slots here.
    private void DegradeEquipment(EquipmentSlot slot)
    {
        var item = _player.Inventory.GetEquipped(slot);
        if (item == null) return;
        item.ItemDurability--;
        if (item.ItemDurability <= 0)
        {
            item.Unequip(_player);
            _player.Inventory.DestroyEquipped(slot);
            _log.LogCombat($"Your {item.Name} shatters from overuse!");
        }
        else if (item.ItemDurability == 5)
        {
            _log.Log($"Your {item.Name} is about to break! ({item.ItemDurability} durability)");
        }
    }

    private void ProcessEntityTurns()
    {
        var entities = _map.Entities.ToList();
        bool aggroLoggedThisTurn = false;
        bool fleeLoggedThisTurn = false;

        foreach (var entity in entities)
        {
            if (entity == _player) continue;
            if (entity.IsDefeated) continue;
            if (entity is not Monster monster) continue;

            var action = SimpleAI.DecideAction(monster, _player, _map, _stealthActive);

            if (action == null)
            {
                // Monster attack with defense reduction
                int rawDamage = monster.BaseAttack;

                // Monster critical hit check
                bool monsterCrit = Random.Shared.Next(100) < monster.CriticalRate;
                if (monsterCrit)
                    rawDamage += monster.CriticalHitDamage;

                int reduced = Math.Max(0, rawDamage - (_player.Defense + _shrineBuff) / 3);
                int finalDamage = Math.Max(1, reduced); // always at least 1

                // Shield block chance — equipping a shield gives a % chance to fully block
                var shield = _player.Inventory.GetEquipped(EquipmentSlot.OffHand) as Armor;
                bool blocked_by_shield = shield != null && shield.BlockChance > 0
                    && Random.Shared.Next(100) < shield.BlockChance;
                if (blocked_by_shield)
                {
                    _log.LogCombat($"Your {shield!.Name} blocks {monster.Name}'s attack!");
                    DegradeEquipment(EquipmentSlot.OffHand);
                    continue;  // skip to next entity — attack fully negated
                }

                // Dodge chance based on player agility
                bool dodged = Random.Shared.Next(100) < Math.Min(20, _player.Agility * 2);
                if (dodged)
                {
                    _dodgeStreak++;
                    string dodgeMsg = string.Format(
                        FlavorText.DodgeFlavors[Random.Shared.Next(FlavorText.DodgeFlavors.Length)], monster.Name);
                    _log.LogCombat(dodgeMsg);

                    // Dodge streak callouts
                    if (_dodgeStreak == 3)
                        _log.LogCombat("*** Matrix mode! 3 dodges in a row! ***");
                    else if (_dodgeStreak == 5)
                        _log.LogCombat("*** Untouchable! 5 dodges! ***");
                    else if (_dodgeStreak >= 7 && _dodgeStreak % 2 == 1)
                        _log.LogCombat($"*** Phantom! {_dodgeStreak} dodge streak! ***");
                }
                else
                {
                    _dodgeStreak = 0;
                    int blocked = rawDamage - finalDamage;
                    string critTag = monsterCrit ? " CRITICAL HIT!" : "";
                    _log.LogCombat($"{monster.Name} hits you for {finalDamage} damage!{critTag}" +
                        (blocked > 0 ? $" ({blocked} blocked)" : ""));
                    _player.TakeDamage(finalDamage);
                    DamageDealt?.Invoke(_player.X, _player.Y, finalDamage, true);
                    _floorDamageTaken += finalDamage;
                    DegradeEquipment(EquipmentSlot.Chest);
                    _killStreak = 0;  // Streak broken on hit

                    // Strong block flavor — when defense absorbs 50%+ of damage
                    if (blocked > 0 && blocked >= rawDamage / 2)
                        _log.LogCombat(FlavorText.BlockFlavors[Random.Shared.Next(FlavorText.BlockFlavors.Length)]);

                    // Poison chance — 35% when hit by a poisonous mob
                    if (monster is Mob { CanPoison: true } && _poisonTurnsLeft <= 0
                        && Random.Shared.Next(100) < 35)
                    {
                        _poisonTurnsLeft = 5;
                        _poisonDamagePerTick = 1 + CurrentFloor;
                        _log.LogCombat($"  {monster.Name}'s attack poisons you! ({_poisonDamagePerTick} dmg/turn for {_poisonTurnsLeft} turns)");
                    }

                    // Bleed chance — 30% when hit by a bleed-capable mob
                    if (monster is Mob { CanBleed: true } && _bleedTurnsLeft <= 0
                        && Random.Shared.Next(100) < 30)
                    {
                        _bleedTurnsLeft = 4;
                        _bleedDamagePerTick = 1 + CurrentFloor;
                        _log.LogCombat($"  {monster.Name}'s slash opens a wound! (Bleed: {_bleedDamagePerTick} dmg/turn for {_bleedTurnsLeft} turns)");
                    }

                    // Low HP survival encouragement
                    if (!_player.IsDefeated
                        && _player.CurrentHealth <= _player.MaxHealth / 4
                        && Random.Shared.Next(100) < 40)
                    {
                        _log.Log(FlavorText.LowHpEncouragements[Random.Shared.Next(FlavorText.LowHpEncouragements.Length)]);
                    }
                }

                if (_player.IsDefeated)
                {
                    LastKillerName = monster.Name;
                    _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                    PlayerDied?.Invoke();
                    return;
                }
            }
            else
            {
                var (mdx, mdy) = action.Value;
                if (mdx != 0 || mdy != 0)
                {
                    int newX = monster.X + mdx;
                    int newY = monster.Y + mdy;

                    // Aggro / flee alerts — once per monster, max 1 of each type per turn
                    int oldDist = Math.Max(Math.Abs(_player.X - monster.X), Math.Abs(_player.Y - monster.Y));
                    int newDist = Math.Max(Math.Abs(_player.X - newX), Math.Abs(_player.Y - newY));
                    if (newDist < oldDist && !aggroLoggedThisTurn && _aggroAlerted.Add(monster.Id))
                    {
                        string alert = FlavorText.AggroAlerts[Random.Shared.Next(FlavorText.AggroAlerts.Length)];
                        _log.LogCombat(string.Format(alert, monster.Name));
                        aggroLoggedThisTurn = true;

                        // Danger sense — warn once when a strong monster first aggros
                        if (monster.Level >= _player.Level + 3 && _dangerWarned.Add(monster.Id))
                        {
                            int diff = monster.Level - _player.Level;
                            string warn = diff >= 6 ? "!!! EXTREME DANGER !!!" : "!! DANGER !!";
                            _log.LogSystem($"{warn} {monster.Name} (Lv{monster.Level}) — {diff} levels above you!");
                        }
                    }
                    else if (newDist > oldDist && !fleeLoggedThisTurn && _fleeAlerted.Add(monster.Id))
                    {
                        string flee = FlavorText.FleeFlavors[Random.Shared.Next(FlavorText.FleeFlavors.Length)];
                        _log.LogCombat(string.Format(flee, monster.Name));
                        fleeLoggedThisTurn = true;
                    }

                    if (_map.InBounds(newX, newY) && _map.GetTile(newX, newY).IsWalkable)
                        _map.MoveEntity(monster, newX, newY);
                }
            }
        }
    }
}
