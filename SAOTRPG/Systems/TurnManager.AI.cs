using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

// Monster AI turn processing — status effect ticks, attacks, parry/dodge/block,
// status application, aggro/flee alerts, and movement.
public partial class TurnManager
{
    // Reusable snapshot buffer so each turn's iteration doesn't allocate a
    // fresh List. Cleared + refilled every call; sized to the entity count
    // automatically by List<T>.Add growth.
    private readonly List<Entity> _entityTurnBuffer = new();

    private void ProcessEntityTurns()
    {
        _entityTurnBuffer.Clear();
        foreach (var e in _map.Entities) _entityTurnBuffer.Add(e);
        bool aggroLoggedThisTurn = false, fleeLoggedThisTurn = false;

        foreach (var entity in _entityTurnBuffer)
        {
            if (entity == _player || entity.IsDefeated) continue;

            // NPCs wander slowly — 25% chance to take a random step each turn.
            if (entity is NPC npc && entity is not Ally)
            {
                if (Random.Shared.Next(4) == 0) WanderNpc(npc);
                continue;
            }

            // Party allies act each turn via their own AI.
            if (entity is Ally ally)
            {
                PartySystem.ProcessAllyTurn(ally, _player, _map, _log);
                continue;
            }

            if (entity is not Monster monster) continue;
            if (!TickMobStatuses(monster)) continue;

            var action = SimpleAI.DecideAction(monster, _player, _map, _stealthActive);
            if (action == null)
                ProcessMonsterAttack(monster);
            else
                ProcessMonsterMovement(monster, action.Value, ref aggroLoggedThisTurn, ref fleeLoggedThisTurn);
            if (_player.IsDefeated) return;
        }
    }

    private static readonly (int dx, int dy)[] NpcDirs =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0),
        (-1, -1), (1, -1), (-1, 1), (1, 1),
    };

    private void WanderNpc(NPC npc)
    {
        // Shuffle directions and pick first valid move.
        Span<int> idx = stackalloc int[8];
        for (int i = 0; i < 8; i++) idx[i] = i;
        for (int i = 7; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }
        foreach (int i in idx)
        {
            int nx = npc.X + NpcDirs[i].dx, ny = npc.Y + NpcDirs[i].dy;
            if (!_map.InBounds(nx, ny)) continue;
            var tile = _map.GetTile(nx, ny);
            if (tile.BlocksMovement || tile.Occupant != null) continue;
            // Don't wander onto special tiles
            if (tile.Type is TileType.StairsUp or TileType.StairsDown
                or TileType.LabyrinthEntrance or TileType.Water or TileType.WaterDeep
                or TileType.Lava) continue;
            _map.MoveEntity(npc, nx, ny);
            return;
        }
    }

    // Ticks one DoT entry for a mob. Returns false if the mob died from this tick.
    // messageFmt receives the monster name and damage: "  {name} burns for {dmg} damage!".
    private bool TickMobDot(Monster monster, Dictionary<int, (int turns, int dmg)> table, Func<string, int, string> messageFmt)
    {
        if (!table.TryGetValue(monster.Id, out var entry)) return true;
        monster.CurrentHealth -= entry.dmg;
        _log.LogCombat(messageFmt(monster.Name, entry.dmg));
        if (monster.CurrentHealth <= 0) { monster.CurrentHealth = 0; return false; }
        if (entry.turns <= 1) table.Remove(monster.Id);
        else table[monster.Id] = (entry.turns - 1, entry.dmg);
        return true;
    }

    // Counts down a simple per-mob timer entry; no damage, no return value.
    private static void TickMobTimer(int mobId, Dictionary<int, int> table)
    {
        if (!table.TryGetValue(mobId, out int t)) return;
        if (t <= 1) table.Remove(mobId);
        else table[mobId] = t - 1;
    }

    private bool TickMobStatuses(Monster monster)
    {
        if (!TickMobDot(monster, _burningMobs, (n, d) => $"  {n} burns for {d} damage!")) return false;
        if (!TickMobDot(monster, _poisonedMobs, (n, d) => $"  {n} takes {d} poison damage!")) return false;
        if (_stunnedMobs.ContainsKey(monster.Id)) { TickMobTimer(monster.Id, _stunnedMobs); return false; }
        TickMobTimer(monster.Id, _blindedMobs);
        return true;
    }

    private void ProcessMonsterAttack(Monster monster)
    {
        _lastCombatTurn = TurnCount;

        // Boss special ability check -- may use a phase ability instead of normal attack.
        if (monster is Boss boss2 && TryBossAbility(boss2))
        {
            if (_player.IsDefeated) { LastKillerName = monster.Name; PlayerDied?.Invoke(); }
            return;
        }

        // Special ability — 20% chance for mobs with a named ability
        if (monster is Mob abilityMob && abilityMob.SpecialAbility != null && Random.Shared.Next(100) < 20)
        {
            int dist = Math.Max(Math.Abs(_player.X - monster.X), Math.Abs(_player.Y - monster.Y));

            if (abilityMob.SpecialAbility == "Charge" && dist >= 2 && dist <= 3)
            {
                // Rush adjacent to player and deal 1.5x damage
                var adjTile = FindAdjacentWalkable(monster, _player.X, _player.Y);
                if (adjTile.HasValue)
                {
                    _map.MoveEntity(monster, adjTile.Value.x, adjTile.Value.y);
                    int chargeDmg = Math.Max(1, (int)(CalcMonsterDamage(monster) * 1.5));
                    int chargeHit = Math.Max(1, chargeDmg - (_player.Defense + _shrineBuff + SatietyDefBonus + FatigueDefPenalty) / 3);
                    _log.LogCombat($"{monster.Name} charges at you with devastating force!");
                    CombatTextEvent?.Invoke(monster.X, monster.Y, "CHARGE!", Color.BrightRed);
                    _player.TakeDamage(chargeHit);
                    DamageDealt?.Invoke(_player.X, _player.Y, chargeHit, true, false);
                    _floorDamageTaken += chargeHit;
                    _killStreak = 0;
                    if (_player.IsDefeated)
                    {
                        LastKillerName = monster.Name;
                        _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                        PlayerDied?.Invoke();
                    }
                    return;
                }
            }
            else if (abilityMob.SpecialAbility == "Leap" && dist >= 2 && dist <= 4)
            {
                // Teleport adjacent to player, then continue to normal attack
                var adjTile = FindAdjacentWalkable(monster, _player.X, _player.Y);
                if (adjTile.HasValue)
                {
                    _map.MoveEntity(monster, adjTile.Value.x, adjTile.Value.y);
                    _log.LogCombat($"{monster.Name} leaps through the air toward you!");
                    CombatTextEvent?.Invoke(monster.X, monster.Y, "LEAP!", Color.BrightCyan);
                    // Fall through to normal attack below
                }
            }
        }

        // Telegraphed attack release — if this mob wound up last turn, release the heavy hit
        if (_telegraphedAttacks.TryGetValue(monster.Id, out int telegDmg))
        {
            _telegraphedAttacks.Remove(monster.Id);
            int dist = Math.Max(Math.Abs(_player.X - monster.X), Math.Abs(_player.Y - monster.Y));
            if (dist > 1)
            {
                _log.LogCombat($"{monster.Name}'s heavy attack whiffs — you moved away!");
                CombatTextEvent?.Invoke(monster.X, monster.Y, "MISS!", Color.BrightCyan);
                return;
            }
            int heavyHit = Math.Max(1, telegDmg - (_player.Defense + _shrineBuff + SatietyDefBonus + FatigueDefPenalty) / 3);
            _log.LogCombat($"*** {monster.Name} unleashes a devastating blow for {heavyHit} damage! ***");
            _player.TakeDamage(heavyHit);
            DamageDealt?.Invoke(_player.X, _player.Y, heavyHit, true, false);
            CombatTextEvent?.Invoke(_player.X, _player.Y, "HEAVY!", Color.BrightRed);
            _floorDamageTaken += heavyHit;
            _killStreak = 0;
            if (_player.IsDefeated)
            {
                LastKillerName = monster.Name;
                _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                PlayerDied?.Invoke();
            }
            return;
        }

        // Chance to telegraph a heavy attack instead of normal attack
        // Bosses: 25% chance, strong mobs (level >= player+2): 15% chance
        bool canTelegraph = monster is Boss || (monster.Level >= _player.Level + 2);
        int telegraphChance = monster is Boss ? 25 : 15;
        if (canTelegraph && Random.Shared.Next(100) < telegraphChance)
        {
            int heavyDmg = (int)(CalcMonsterDamage(monster) * 1.8);
            _telegraphedAttacks[monster.Id] = heavyDmg;
            _log.LogCombat($"!!! {monster.Name} winds up a powerful attack! Move away to dodge! !!!");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "WINDING UP!", Color.BrightYellow);
            return;
        }

        if (monster is Mob rangeMob && rangeMob.AttackRange > 1)
            _log.LogCombat($"{monster.Name} attacks from a distance!");

        int rawDamage = CalcMonsterDamage(monster);
        int reduced = Math.Max(0, rawDamage - (_player.Defense + _shrineBuff + SatietyDefBonus + FatigueDefPenalty) / 3);
        int finalDamage = Math.Max(1, reduced);

        bool monsterCrit = Random.Shared.Next(100) < Math.Max(0, monster.CriticalRate + WeatherSystem.GetCritModifier());
        if (monsterCrit)
        {
            rawDamage += monster.CriticalHitDamage;
            reduced = Math.Max(0, rawDamage - (_player.Defense + _shrineBuff + SatietyDefBonus + FatigueDefPenalty) / 3);
            finalDamage = Math.Max(1, reduced);
        }

        // Counter stance — guaranteed riposte for 150% weapon damage
        if (_counterStance)
        {
            int riposteDmg = Math.Max(1, (int)(_player.Attack * 1.5));
            monster.CurrentHealth -= riposteDmg;
            _log.LogCombat($"RIPOSTE! You counter {monster.Name}'s attack for {riposteDmg} damage!");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "RIPOSTE!", Color.BrightYellow);
            DamageDealt?.Invoke(monster.X, monster.Y, riposteDmg, false, false);
            if (monster.CurrentHealth <= 0) monster.CurrentHealth = 0;
            return;
        }

        if (TryShieldBlock(monster)) return;
        if (TryParry(monster)) return;
        if (TryDodge(monster)) return;

        ApplyMonsterHit(monster, rawDamage, finalDamage, monsterCrit);
    }

    private int CalcMonsterDamage(Monster monster)
    {
        int rawDamage = monster.BaseAttack;
        if (_blindedMobs.ContainsKey(monster.Id)) rawDamage /= 2;

        // FB-564 Starless Night — enemies hit +10% harder.
        if (RunModifiers.IsActive(RunModifier.StarlessNight)) rawDamage = rawDamage * 110 / 100;
        // FB-564 Heathcliff's Gauntlet — boss damage ×1.3 in addition to stat boost.
        if (monster is Boss && RunModifiers.IsActive(RunModifier.HeathcliffsGauntlet))
            rawDamage = rawDamage * 130 / 100;

        if (monster is Boss boss && boss.IsEnraged)
        {
            double enrageMul = Boss.EnrageAtkMultiplier;
            // FB-564 Gleam Eyes Echo — enrage hits +50% harder.
            if (RunModifiers.IsActive(RunModifier.GleamEyesEcho)) enrageMul *= 1.5;
            rawDamage = (int)(rawDamage * enrageMul);
            if (!boss.EnrageAnnounced)
            {
                boss.EnrageAnnounced = true;
                _log.LogCombat($"*** {boss.Name} enters a furious rage! ***");
            }
        }
        return rawDamage;
    }

    private bool TryShieldBlock(Monster monster)
    {
        var shield = _player.Inventory.GetEquipped(EquipmentSlot.OffHand) as Armor;
        var mainWpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        int blockFx = GetSpecialEffectValue(mainWpn, "BlockChance");
        int totalBlock = (shield?.BlockChance ?? 0) + blockFx;
        if (totalBlock <= 0) return false;
        if (Random.Shared.Next(100) >= totalBlock) return false;

        string blockSource = shield?.Name ?? mainWpn?.Name ?? "guard";
        _log.LogCombat($"Your {blockSource} blocks {monster.Name}'s attack!");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "BLOCK", Color.White);
        if (shield != null) DegradeEquipment(EquipmentSlot.OffHand);
        return true;
    }

    private (int Crit, int Parry, int Dodge) GetActiveWeaponPerks()
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        return GetProficiencyPerks(wpn?.WeaponType ?? "Unarmed");
    }

    private bool TryParry(Monster monster)
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        int parryFx = GetSpecialEffectValue(wpn, "ParryChance");
        int parryChance = Math.Min(15, _player.Dexterity) + GetActiveWeaponPerks().Parry + parryFx;
        if (parryChance <= 0 || Random.Shared.Next(100) >= parryChance) return false;

        int counterDmg = Math.Max(1, _player.Attack / 4);
        monster.CurrentHealth -= counterDmg;
        _log.LogCombat($"You parry {monster.Name}'s attack! Counterstrike deals {counterDmg} damage!");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "PARRY", Color.BrightYellow);
        DamageDealt?.Invoke(monster.X, monster.Y, counterDmg, false, false);
        if (monster.CurrentHealth <= 0)
        {
            monster.CurrentHealth = 0;
            _log.LogCombat($"Your parry finishes off {monster.Name}!");
        }
        return true;
    }

    private bool TryDodge(Monster monster)
    {
        int dodgeChance = Math.Min(20, _player.Agility * 2) + GetActiveWeaponPerks().Dodge;
        if (_slowTurnsLeft > 0) dodgeChance /= 2;
        // FB-564 Naked Ingress — +25% evasion compensation for no armor.
        if (RunModifiers.IsActive(RunModifier.NakedIngress)) dodgeChance = dodgeChance * 125 / 100;
        if (Random.Shared.Next(100) >= dodgeChance) return false;

        _dodgeStreak++;
        TutorialSystem.ShowTip(_log, "first_dodge");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "DODGE", Color.BrightCyan);
        string dodgeMsg = string.Format(
            FlavorText.DodgeFlavors[Random.Shared.Next(FlavorText.DodgeFlavors.Length)], monster.Name);
        _log.LogCombat(dodgeMsg);

        if (_dodgeStreak == 3) _log.LogCombat("*** Matrix mode! 3 dodges in a row! ***");
        else if (_dodgeStreak == 5) _log.LogCombat("*** Untouchable! 5 dodges! ***");
        else if (_dodgeStreak >= 7 && _dodgeStreak % 2 == 1) _log.LogCombat($"*** Phantom! {_dodgeStreak} dodge streak! ***");

        return true;
    }

    private void ApplyMonsterHit(Monster monster, int rawDamage, int finalDamage, bool monsterCrit)
    {
        // Post-motion vulnerability: +50% incoming damage while recovering from a skill.
        if (_postMotionDelay > 0) finalDamage = (int)(finalDamage * 1.5);

        TutorialSystem.ShowTip(_log, "first_damage_taken");
        _dodgeStreak = 0;
        int blocked = rawDamage - finalDamage;
        string critTag = monsterCrit ? " CRITICAL HIT!" : "";
        _log.LogCombat($"{monster.Name} hits you for {finalDamage} damage!{critTag}" +
            (blocked > 0 ? $" ({blocked} blocked)" : ""));
        _player.TakeDamage(finalDamage);
        DamageDealt?.Invoke(_player.X, _player.Y, finalDamage, true, monsterCrit);
        if (monsterCrit) CombatTextEvent?.Invoke(_player.X, _player.Y, "CRIT!", Color.BrightRed);
        _floorDamageTaken += finalDamage;
        DegradeEquipment(EquipmentSlot.Chest);
        if (_killStreak >= 3) _log.LogCombat($"  Streak broken! ({_killStreak} kills)");
        _killStreak = 0;

        if (monster is Mob { Affix: not null } vampMob && vampMob.Affix.Contains("Vampiric"))
        {
            int vampHeal = Math.Max(1, finalDamage / 4);
            monster.CurrentHealth = Math.Min(monster.CurrentHealth + vampHeal, monster.MaxHealth);
            _log.LogCombat($"  {monster.Name} drains {vampHeal} HP from you!");
        }

        if (blocked > 0 && blocked >= rawDamage / 2)
            _log.LogCombat(FlavorText.BlockFlavors[Random.Shared.Next(FlavorText.BlockFlavors.Length)]);

        ApplyMobStatusEffects(monster);

        if (!_player.IsDefeated && _player.CurrentHealth <= _player.MaxHealth / 4 && Random.Shared.Next(100) < 40)
            _log.Log(FlavorText.LowHpEncouragements[Random.Shared.Next(FlavorText.LowHpEncouragements.Length)]);

        if (_player.IsDefeated)
        {
            LastKillerName = monster.Name;
            _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
            PlayerDied?.Invoke();
        }
    }

    private void ApplyMobStatusEffects(Monster monster)
    {
        if (monster is Mob { CanPoison: true } && _poisonTurnsLeft <= 0 && Random.Shared.Next(100) < 35)
        {
            _poisonTurnsLeft = 5;
            _poisonDamagePerTick = 1 + CurrentFloor;
            _log.LogCombat($"  {monster.Name}'s attack poisons you! ({_poisonDamagePerTick} dmg/turn for {_poisonTurnsLeft} turns)");
        }
        if (monster is Mob { CanBleed: true } && _bleedTurnsLeft <= 0 && Random.Shared.Next(100) < 30)
        {
            _bleedTurnsLeft = 4;
            _bleedDamagePerTick = 1 + CurrentFloor;
            _log.LogCombat($"  {monster.Name}'s slash opens a wound! (Bleed: {_bleedDamagePerTick} dmg/turn for {_bleedTurnsLeft} turns)");
        }
        if (monster is Mob { CanStun: true } && _stunTurnsLeft <= 0 && Random.Shared.Next(100) < 20)
        {
            _stunTurnsLeft = 1 + Random.Shared.Next(0, 2);
            _log.LogCombat($"  {monster.Name}'s heavy blow stuns you! ({_stunTurnsLeft} turns)");
        }
        if (monster is Mob { CanSlow: true } && _slowTurnsLeft <= 0 && Random.Shared.Next(100) < 25)
        {
            _slowTurnsLeft = 3;
            _log.LogCombat($"  {monster.Name}'s attack slows you! (Dodge halved for {_slowTurnsLeft} turns)");
        }
    }

    // Find the nearest walkable tile adjacent to (targetX, targetY) for a monster to move to.
    private (int x, int y)? FindAdjacentWalkable(Monster monster, int targetX, int targetY)
    {
        (int x, int y)? best = null;
        int bestDist = int.MaxValue;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = targetX + dx, ny = targetY + dy;
            if (!_map.InBounds(nx, ny) || !_map.GetTile(nx, ny).IsWalkable) continue;
            if (nx == _player.X && ny == _player.Y) continue;
            // Prefer tile closest to monster's current position (shortest travel)
            int d = Math.Abs(nx - monster.X) + Math.Abs(ny - monster.Y);
            if (d < bestDist) { bestDist = d; best = (nx, ny); }
        }
        return best;
    }

    private void ProcessMonsterMovement(Monster monster, (int dx, int dy) move,
        ref bool aggroLoggedThisTurn, ref bool fleeLoggedThisTurn)
    {
        var (mdx, mdy) = move;
        if (mdx == 0 && mdy == 0) return;

        int newX = monster.X + mdx, newY = monster.Y + mdy;
        int oldDist = Math.Max(Math.Abs(_player.X - monster.X), Math.Abs(_player.Y - monster.Y));
        int newDist = Math.Max(Math.Abs(_player.X - newX), Math.Abs(_player.Y - newY));

        // Record bestiary encounter silently (no log spam for aggro/flee).
        // Only warn for genuinely dangerous high-level mobs.
        if (newDist < oldDist && _aggroAlerted.Add(monster.Id))
        {
            Bestiary.RecordEncounter(monster.Name, monster.Level, monster.MaxHealth, monster.BaseAttack);
            if (monster.Level >= _player.Level + 3 && _dangerWarned.Add(monster.Id))
            {
                int diff = monster.Level - _player.Level;
                string warn = diff >= 6 ? "!! EXTREME DANGER !!" : "!! DANGER !!";
                _log.LogCombat($"{warn} {monster.Name} (Lv{monster.Level}) -- {diff} levels above you!");
            }
        }

        if (_map.InBounds(newX, newY) && _map.GetTile(newX, newY).IsWalkable)
            _map.MoveEntity(monster, newX, newY);
    }
}
