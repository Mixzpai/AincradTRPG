using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;
using SAOTRPG.Map.Spawning;
using SAOTRPG.Systems.Combat;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.Progression;
using SAOTRPG.Systems.RunState;
using SAOTRPG.Systems.Social;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Systems.Turns;

// The Cardinal-Marked boon rotation. Every member has a consumer: Surge in CalcMonsterDamage,
// Mend in TickAffixRegen, Phase in TryAffixEvade.
public enum CardinalBoon { Surge, Mend, Phase }

// Monster AI turn processing — status effect ticks, attacks, parry/dodge/block,
// status application, aggro/flee alerts, and movement.
public partial class TurnManager
{
    // ── Affix tuning ─────────────────────────────────────────────────
    // Each of these is the number the affix's own description promises the player.
    private const int RegenAffixPercent = 2;       // Regenerating: MaxHealth healed per turn
    private const int PackHunterBonusPercent = 30; // Pack Hunter: damage while adjacent to a mob
    private const int EtherealMissPercent = 30;    // Ethereal: incoming melee that whiffs
    private const int CardinalSurgePercent = 40;   // Cardinal-Marked, Surge turn: damage
    private const int CardinalMendPercent = 4;     // Cardinal-Marked, Mend turn: MaxHealth healed
    private const int CardinalPhaseMissPercent = 25; // Cardinal-Marked, Phase turn: melee whiff

    // Reused snapshot buffer (avoids per-turn List alloc).
    private readonly List<Entity> _entityTurnBuffer = new();

    private void ProcessEntityTurns()
    {
        using var _ = Profiler.Begin("Turn.EntityAI");
        _entityTurnBuffer.Clear();
        // AddRange on a List source copies through ICollection.CopyTo: one Array.Copy rather
        // than a bounds-and-version-checked Add per entity.
        _entityTurnBuffer.AddRange(_map.Entities);
        bool aggroLoggedThisTurn = false, fleeLoggedThisTurn = false;

        foreach (var entity in _entityTurnBuffer)
        {
            if (entity == _player || entity.IsDefeated) continue;

            // NPCs wander slowly — 25% chance to take a random step each turn.
            // Allies need no exclusion here: Ally derives from Entity, not NPC, so it can never
            // reach this branch.
            if (entity is NPC npc)
            {
                if (RunRng.Next(4) == 0) WanderNpc(npc);
                continue;
            }

            // Party allies act each turn via their own AI.
            if (entity is Ally ally)
            {
                var allyKill = PartySystem.ProcessAllyTurn(ally, _player, _map, _log);
                if (allyKill is { } k) HandleMonsterKill(k.Target, k.Reward, NonWeaponKill, _player.CurrentHealth);
                continue;
            }

            if (entity is not Monster monster) continue;
            if (!TickMobStatuses(monster)) continue;

            // SlowOnHit consumer. Slowed mobs act every other turn; mob.Id-XOR-TurnCount
            // parity ensures different mobs skip on different turns.
            if (_slowedMobs.Count != 0 && _slowedMobs.ContainsKey(monster.Id)
                && ((monster.Id + TurnCount) & 1) == 0)
                continue;

            // Gain the turn's speed, then act while any points are left. An attack costs
            // 100, a step the terrain's cost, so a fast monster sometimes acts twice in a turn.
            monster.Moves += monster.MoveSpeed;
            while (monster.Moves > 0 && !monster.IsDefeated)
            {
                int fromX = monster.X, fromY = monster.Y;
                // Invisibility+N SpecialEffect: treat active invis like stealth for aggro.
                bool concealedFromAggro = _stealthActive || _invisibilityTurnsLeft > 0;
                // Lunacy+N: confused mobs wander instead of pathing/attacking.
                bool confused = _confusedMobs.Count != 0 && _confusedMobs.ContainsKey(monster.Id);
                (int dx, int dy)? action = confused
                    ? PickConfusedStep(monster)
                    : SimpleAI.DecideAction(monster, _player, _map, concealedFromAggro);
                if (action == null)
                    ProcessMonsterAttack(monster);
                // Out of reach, a wound-up blow whiffs — before any Charge/Leap can carry it back in.
                else if (_telegraphedAttacks.Count != 0 && _telegraphedAttacks.Remove(monster.Id))
                {
                    _log.LogCombat($"{monster.Name}'s heavy attack whiffs — you moved away!");
                    CombatTextEvent?.Invoke(monster.X, monster.Y, "MISS!", Color.BrightCyan);
                }
                else if (confused || !TryGapCloser(monster, action.Value))
                    ProcessMonsterMovement(monster, action.Value, ref aggroLoggedThisTurn, ref fleeLoggedThisTurn);
                if (_player.IsDefeated) return;
                monster.Moves -= monster.X != fromX || monster.Y != fromY
                    ? MonsterStepCost(monster, fromX, fromY) : NormalSpeed;
            }
        }
    }

    // Lunacy+N consumer — confused mob picks a random adjacent walkable tile,
    // ignoring the player. Returns (0,0) idle if all neighbors are blocked.
    private (int dx, int dy)? PickConfusedStep(Monster monster)
    {
        Span<int> idx = stackalloc int[8];
        for (int i = 0; i < 8; i++) idx[i] = i;
        for (int i = 7; i > 0; i--)
        {
            int j = RunRng.Next(i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }
        foreach (int i in idx)
        {
            var (dx, dy) = NpcDirs[i];
            int nx = monster.X + dx, ny = monster.Y + dy;
            if (!_map.InBounds(nx, ny)) continue;
            if (nx == _player.X && ny == _player.Y) continue;
            if (!IsMonsterWalkable(monster, nx, ny)) continue;
            return (dx, dy);
        }
        return (0, 0);
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
            int j = RunRng.Next(i + 1);
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
        // These tables are empty on almost every turn of almost every floor, and this runs once
        // per table per monster. An int compare beats a hash lookup that is going to miss.
        if (table.Count == 0) return true;
        if (!table.TryGetValue(monster.Id, out var entry)) return true;
        _log.LogCombat(messageFmt(monster.Name, entry.dmg));
        var reward = monster.TakeDamage(entry.dmg);
        if (reward != null) { HandleMonsterKill(monster, reward, HeldWeaponType, _player.CurrentHealth); return false; }
        if (entry.turns <= 1) table.Remove(monster.Id);
        else table[monster.Id] = (entry.turns - 1, entry.dmg);
        return true;
    }

    // Counts down a simple per-mob timer entry; no damage, no return value.
    private static void TickMobTimer(int mobId, Dictionary<int, int> table)
    {
        if (table.Count == 0) return;
        if (!table.TryGetValue(mobId, out int t)) return;
        if (t <= 1) table.Remove(mobId);
        else table[mobId] = t - 1;
    }

    // Which boon a Cardinal-Marked mob is carrying right now. Derived from (mob, turn) rather
    // than rolled: a roll would have to be stored to stay stable across the several reads inside
    // one turn, and drawing once per marked mob per turn would move every later roll in the run.
    // StableHash is the same FNV mix the seeded streams use, so this is reproducible.
    public static CardinalBoon CardinalBoonOf(Mob mob, int turn) =>
        (CardinalBoon)((uint)RunRng.StableHash(mob.Id, turn) % 3);

    private CardinalBoon CardinalBoonOf(Mob mob) => CardinalBoonOf(mob, TurnCount);

    // Regenerating, and Cardinal-Marked on a Mend turn. Both advertise a per-turn heal.
    private void TickAffixRegen(Monster monster)
    {
        if (monster is not Mob mob || mob.Affix == null) return;

        int pct = 0;
        if (mob.HasAffix(MobFactory.AffixRegenerating)) pct += RegenAffixPercent;
        if (mob.HasAffix(MobFactory.AffixCardinal) && CardinalBoonOf(mob) == CardinalBoon.Mend)
            pct += CardinalMendPercent;
        if (pct == 0 || mob.CurrentHealth >= mob.MaxHealth || mob.CurrentHealth <= 0) return;

        int heal = Math.Max(1, mob.MaxHealth * pct / 100);
        mob.CurrentHealth = Math.Min(mob.MaxHealth, mob.CurrentHealth + heal);

        // Told once, then silent. A line every turn would bury the combat log.
        if (!mob.RegenAnnounced && _map.IsVisible(mob.X, mob.Y))
        {
            mob.RegenAnnounced = true;
            _log.LogCombat($"  {mob.Name}'s wounds knit closed as you watch.");
        }
    }

    // Pack Hunter's condition: another living monster on one of the eight neighbouring tiles.
    private bool HasAdjacentMonster(Monster self)
    {
        var monsters = _map.Monsters;
        for (int i = 0; i < monsters.Count; i++)
        {
            var other = monsters[i];
            if (ReferenceEquals(other, self) || other.IsDefeated) continue;
            if (Math.Abs(other.X - self.X) <= 1 && Math.Abs(other.Y - self.Y) <= 1) return true;
        }
        return false;
    }

    private bool TickMobStatuses(Monster monster)
    {
        TickAffixRegen(monster);
        if (!TickMobDot(monster, _burningMobs, (n, d) => $"  {n} burns for {d} damage!")) return false;
        if (!TickMobDot(monster, _poisonedMobs, (n, d) => $"  {n} takes {d} poison damage!")) return false;
        if (_stunnedMobs.Count != 0 && _stunnedMobs.ContainsKey(monster.Id))
        { TickMobTimer(monster.Id, _stunnedMobs); return false; }
        TickMobTimer(monster.Id, _blindedMobs);
        TickMobTimer(monster.Id, _slowedMobs);
        return true;
    }

    private void ProcessMonsterAttack(Monster monster)
    {
        _lastCombatTurn = TurnCount;

        // Boss special ability check -- may use a phase ability instead of normal attack.
        if (monster is Boss boss2 && TryBossAbility(boss2, chasing: false))
        {
            if (_player.IsDefeated) { LastKillerName = monster.Name; RaisePlayerDied(KillCause(monster)); }
            return;
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
            int heavyHit = Math.Max(1, telegDmg - IncomingDefense);
            _log.LogCombat($"*** {monster.Name} unleashes a devastating blow for {heavyHit} damage! ***");
            _player.TakeDamage(heavyHit);
            DamageDealt?.Invoke(_player.X, _player.Y, heavyHit, true, false);
            CombatTextEvent?.Invoke(_player.X, _player.Y, "HEAVY!", Color.BrightRed);
            _floorDamageTaken += heavyHit;
            _killStreak = 0;
            if (_player.IsDefeated)
            {
                LastKillerName = monster.Name;
                _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
                RaisePlayerDied(KillCause(monster));
            }
            return;
        }

        // Chance to telegraph a heavy attack instead of normal attack
        // Bosses: 25% chance, strong mobs (level >= player+2): 15% chance
        bool canTelegraph = monster is Boss || (monster.Level >= _player.Level + 2);
        int telegraphChance = monster is Boss ? 25 : 15;
        if (canTelegraph && RunRng.Next(100) < telegraphChance)
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
        int reduced = Math.Max(0, rawDamage - IncomingDefense);
        int finalDamage = Math.Max(1, reduced);

        bool monsterCrit = RunRng.Next(100) < Math.Max(0, monster.CriticalRate + WeatherSystem.GetCritModifier());
        // CritImmune+N — at ≥100 cancels the crit outright; lower values roll.
        // MH + OH shield contribute, picking the max (cap-like, non-stacking).
        // Read under the crit branch: the lookup walks both slots' effects and only a crit needs
        // it. The RunRng draw sits behind the short-circuiting `&&`, so it is taken only on a
        // crit against partial immunity.
        if (monsterCrit)
        {
            int critImmune = MaxWeaponShield<EquipmentSpecialEffect.CritImmune>(c => c.ChancePercent);
            if (critImmune > 0 && (critImmune >= 100 || RunRng.Next(100) < critImmune))
            {
                monsterCrit = false;
                var aegis = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
                _log.LogCombat($"  {aegis?.Name ?? "your aegis"}'s aegis blunts the critical blow!");
            }
        }
        if (monsterCrit)
        {
            rawDamage += monster.CriticalHitDamage;
            reduced = Math.Max(0, rawDamage - IncomingDefense);
            finalDamage = Math.Max(1, reduced);
        }

        // Counter stance: the attack is cancelled and answered with a guaranteed riposte.
        if (_counterStance)
        {
            int riposteDmg = Math.Max(1, (int)(_player.Attack * _riposteMultiplier));
            var riposteReward = monster.TakeDamage(riposteDmg);
            _log.LogCombat($"RIPOSTE! You counter {monster.Name}'s attack for {riposteDmg} damage!");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "RIPOSTE!", Color.BrightYellow);
            DamageDealt?.Invoke(monster.X, monster.Y, riposteDmg, false, false);
            if (riposteReward != null) HandleMonsterKill(monster, riposteReward, HeldWeaponType, _player.CurrentHealth);
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

        if (monster is Mob { Affix: not null } affixMob)
        {
            if (affixMob.HasAffix(MobFactory.AffixPackHunter) && HasAdjacentMonster(affixMob))
                rawDamage = rawDamage * (100 + PackHunterBonusPercent) / 100;
            if (affixMob.HasAffix(MobFactory.AffixCardinal)
                && CardinalBoonOf(affixMob) == CardinalBoon.Surge)
                rawDamage = rawDamage * (100 + CardinalSurgePercent) / 100;
        }

        // Starless Night — enemies hit +10% harder.
        if (RunModifiers.IsActive(RunModifier.StarlessNight)) rawDamage = rawDamage * 110 / 100;
        // Heathcliff's Gauntlet — boss damage ×1.3 in addition to stat boost.
        if (monster is Boss && RunModifiers.IsActive(RunModifier.HeathcliffsGauntlet))
            rawDamage = rawDamage * 130 / 100;

        if (monster is Boss boss && boss.IsEnraged)
        {
            double enrageMul = Boss.EnrageAtkMultiplier;
            // Gleam Eyes Echo — enrage hits +50% harder.
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
        // BlockChance is additive across all equipped slots (armor pieces too).
        int blockFx = SumAllSlots<EquipmentSpecialEffect.BlockChanceBonus>(b => b.Percent);

        // Holy Sword — Heathcliff's stance: +15% block, active only while holding a one-handed
        // sword and a real shield. `shield` is `OffHand as Armor`, so a second sword is already
        // excluded.
        int holySwordBlock = Skills.UniqueSkillSystem.IsHolySwordActive(
            mainWpn?.WeaponType ?? "", shield != null) ? 15 : 0;

        int totalBlock = (shield?.BlockChance ?? 0) + blockFx + holySwordBlock;
        if (totalBlock <= 0) return false;
        if (RunRng.Next(100) >= totalBlock) return false;

        string blockSource = shield?.Name ?? mainWpn?.Name ?? "guard";
        _log.LogCombat($"Your {blockSource} blocks {monster.Name}'s attack!");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "BLOCK", Color.White);
        if (shield != null) DegradeEquipment(EquipmentSlot.OffHand);
        return true;
    }

    private (int Crit, int Parry, int Dodge) GetActiveWeaponPerks() => GetProficiencyPerks(HeldWeaponType);

    // The weapon type a kill off the main attack path (riposte, parry, reflect, DoT) credits.
    private string HeldWeaponType =>
        (_player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon)?.WeaponType ?? "Unarmed";

    private bool TryParry(Monster monster)
    {
        // ParryChance additive across all slots; armor with parry-flavor stacks.
        int parryFx = SumAllSlots<EquipmentSpecialEffect.ParryChance>(p => p.Percent);
        int parryChance = Math.Min(15, _player.Dexterity) + GetActiveWeaponPerks().Parry + parryFx;
        if (parryChance <= 0 || RunRng.Next(100) >= parryChance) return false;

        int counterDmg = Math.Max(1, _player.Attack / 4);
        var parryReward = monster.TakeDamage(counterDmg);
        _log.LogCombat($"You parry {monster.Name}'s attack! Counterstrike deals {counterDmg} damage!");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "PARRY", Color.BrightYellow);
        DamageDealt?.Invoke(monster.X, monster.Y, counterDmg, false, false);
        if (parryReward != null)
        {
            _log.LogCombat($"Your parry finishes off {monster.Name}!");
            HandleMonsterKill(monster, parryReward, HeldWeaponType, _player.CurrentHealth);
        }
        return true;
    }

    private bool TryDodge(Monster monster)
    {
        int dodgeChance = Math.Min(20, _player.Agility * 2) + GetActiveWeaponPerks().Dodge;
        if (_slowTurnsLeft > 0) dodgeChance /= 2;
        // Naked Ingress — +25% evasion compensation for no armor.
        if (RunModifiers.IsActive(RunModifier.NakedIngress)) dodgeChance = dodgeChance * 125 / 100;
        if (RunRng.Next(100) >= dodgeChance) return false;

        _dodgeStreak++;
        TutorialSystem.ShowTip(_log, "first_dodge");
        CombatTextEvent?.Invoke(_player.X, _player.Y, "DODGE", Color.BrightCyan);
        string dodgeMsg = string.Format(
            FlavorText.DodgeFlavors[RunRng.Next(FlavorText.DodgeFlavors.Length)], monster.Name);
        _log.LogCombat(dodgeMsg);

        if (_dodgeStreak == 3) _log.LogCombat("*** Matrix mode! 3 dodges in a row! ***");
        else if (_dodgeStreak == 5) _log.LogCombat("*** Untouchable! 5 dodges! ***");
        else if (_dodgeStreak >= 7 && _dodgeStreak % 2 == 1) _log.LogCombat($"*** Phantom! {_dodgeStreak} dodge streak! ***");

        // EvadeRegen+N — heal N HP on a clean dodge. Additive across all slots.
        int evadeRegen = SumAllSlots<EquipmentSpecialEffect.EvadeRegen>(e => e.Percent);
        if (evadeRegen > 0)
        {
            int heal = Math.Min(evadeRegen, _player.MaxHealth - _player.CurrentHealth);
            if (heal > 0)
            {
                _player.CurrentHealth += heal;
                var dodgeWpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
                _log.LogCombat($"  {dodgeWpn?.Name ?? "Your evasion"} channels the dodge — +{heal} HP.");
            }
        }

        return true;
    }

    private void ApplyMonsterHit(Monster monster, int rawDamage, int finalDamage, bool monsterCrit)
    {
        // Post-motion vulnerability: +50% incoming damage while recovering from a skill.
        if (_postMotionDelay > 0) finalDamage = (int)(finalDamage * 1.5);

        // Barrier+N absorbs up to _barrierRemaining of the incoming hit.
        int barrierAbsorbed = 0;
        if (_barrierRemaining > 0 && finalDamage > 0)
        {
            barrierAbsorbed = Math.Min(finalDamage, _barrierRemaining);
            _barrierRemaining -= barrierAbsorbed;
            finalDamage -= barrierAbsorbed;
            if (barrierAbsorbed > 0)
                _log.LogCombat($"  Barrier absorbs {barrierAbsorbed} damage! ({_barrierRemaining} remaining)");
        }

        TutorialSystem.ShowTip(_log, "first_damage_taken");
        _dodgeStreak = 0;
        int blocked = rawDamage - finalDamage - barrierAbsorbed;
        string critTag = monsterCrit ? " CRITICAL HIT!" : "";
        string incomingLine = $"{monster.Name} hits you for {finalDamage} damage!{critTag}" +
            (blocked > 0 ? $" ({blocked} blocked)" : "");
        string incomingTag = BuildIncomingDamageTag(monster.Name);
        _log.LogCombat(ApplyDamageTag(incomingLine, incomingTag));

        // DamageReflect+N — return N% of post-mitigation damage to attacker.
        // Sourced from MH weapon + OH shield (e.g. Nox Fermat +5). Pre-HP-deduct for counter flavor.
        int reflectPct = SumWeaponShield<EquipmentSpecialEffect.DamageReflect>(r => r.Percent);
        if (reflectPct > 0 && finalDamage > 0 && !monster.IsDefeated)
        {
            int reflected = Math.Max(1, finalDamage * reflectPct / 100);
            var reflectReward = monster.TakeDamage(reflected);
            _log.LogCombat($"  Reflect! {monster.Name} takes {reflected} damage from the counterforce.");
            DamageDealt?.Invoke(monster.X, monster.Y, reflected, false, false);
            if (reflectReward != null) HandleMonsterKill(monster, reflectReward, HeldWeaponType, _player.CurrentHealth);
        }

        _player.TakeDamage(finalDamage);
        DamageDealt?.Invoke(_player.X, _player.Y, finalDamage, true, monsterCrit);
        // Shake when the player eats ≥20% max-HP in one blow. Mob attacks are
        // tier-1 — boss specials escalate to tier-2 in BossAI.
        if (_player.MaxHealth > 0 && finalDamage * 5 >= _player.MaxHealth)
            MapViewShakeRequested?.Invoke(1);
        if (monsterCrit) CombatTextEvent?.Invoke(_player.X, _player.Y, "CRIT!", Color.BrightRed);
        _floorDamageTaken += finalDamage;
        DegradeEquipment(EquipmentSlot.Chest);
        if (_killStreak >= 3) _log.LogCombat($"  Streak broken! ({_killStreak} kills)");
        _killStreak = 0;

        if (monster is Mob vampMob && !vampMob.IsDefeated && vampMob.HasAffix(MobFactory.AffixVampiric))
        {
            int vampHeal = Math.Max(1, finalDamage / 4);
            monster.CurrentHealth = Math.Min(monster.CurrentHealth + vampHeal, monster.MaxHealth);
            _log.LogCombat($"  {monster.Name} drains {vampHeal} HP from you!");
        }

        if (blocked > 0 && blocked >= rawDamage / 2)
            _log.LogCombat(FlavorText.BlockFlavors[RunRng.Next(FlavorText.BlockFlavors.Length)]);

        ApplyMobStatusEffects(monster);

        if (!_player.IsDefeated && _player.CurrentHealth <= _player.MaxHealth / 4 && RunRng.Next(100) < 40)
            _log.Log(FlavorText.LowHpEncouragements[RunRng.Next(FlavorText.LowHpEncouragements.Length)]);

        if (_player.IsDefeated)
        {
            LastKillerName = monster.Name;
            _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
            RaisePlayerDied(KillCause(monster));
        }
    }

    private void ApplyMobStatusEffects(Monster monster)
    {
        // Uninterruptible+N — weapon grants N% chance to shrug off any
        // incoming status application from the monster hit just taken.
        var uninterruptWpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        int uninterruptChance = uninterruptWpn?.FirstEffect<EquipmentSpecialEffect.Uninterruptible>()?.ChancePercent ?? 0;
        if (uninterruptChance > 0 && RunRng.Next(100) < uninterruptChance)
        {
            _log.LogCombat($"  {uninterruptWpn!.Name}'s resolve shrugs off {monster.Name}'s effect.");
            return;
        }
        if (monster is Mob { CanPoison: true } && _poisonTurnsLeft <= 0 && RunRng.Next(100) < 35)
        {
            _poisonTurnsLeft = 5;
            _poisonDamagePerTick = 1 + CurrentFloor;
            _log.LogCombat($"  {monster.Name}'s attack poisons you! ({_poisonDamagePerTick} dmg/turn for {_poisonTurnsLeft} turns)");
        }
        if (monster is Mob { CanBleed: true } && _bleedTurnsLeft <= 0 && RunRng.Next(100) < 30)
        {
            _bleedTurnsLeft = 4;
            _bleedDamagePerTick = 1 + CurrentFloor;
            _log.LogCombat($"  {monster.Name}'s slash opens a wound! (Bleed: {_bleedDamagePerTick} dmg/turn for {_bleedTurnsLeft} turns)");
        }
        if (monster is Mob { CanStun: true } && _stunTurnsLeft <= 0 && RunRng.Next(100) < 20)
        {
            _stunTurnsLeft = 1 + RunRng.Next(0, 2);
            _log.LogCombat($"  {monster.Name}'s heavy blow stuns you! ({_stunTurnsLeft} turns)");
        }
        if (monster is Mob { CanSlow: true } && _slowTurnsLeft <= 0 && RunRng.Next(100) < 25)
        {
            _slowTurnsLeft = 3;
            _log.LogCombat($"  {monster.Name}'s attack slows you! (Dodge halved for {_slowTurnsLeft} turns)");
        }
    }

    // Charge/Leap from the chase branch, on a closing step with line of sight. True = turn spent.
    private bool TryGapCloser(Monster monster, (int dx, int dy) step)
    {
        int dist = Math.Max(Math.Abs(_player.X - monster.X), Math.Abs(_player.Y - monster.Y));
        if (dist < 2 || dist > 4) return false;
        int next = Math.Max(Math.Abs(_player.X - monster.X - step.dx), Math.Abs(_player.Y - monster.Y - step.dy));
        if (next >= dist || !_map.IsVisible(monster.X, monster.Y)) return false;
        // A wander step can close the gap too; only a mob that has noticed the player charges.
        int aggro = SimpleAI.EffectiveAggroRange(monster);
        if (_stealthActive || _invisibilityTurnsLeft > 0) aggro /= 2;
        if (dist > aggro) return false;

        if (monster is Boss boss)
        {
            if (!TryBossAbility(boss, chasing: true)) return false;
            _lastCombatTurn = TurnCount;
            if (_player.IsDefeated) { LastKillerName = monster.Name; RaisePlayerDied(KillCause(monster)); }
            return true;
        }

        // Special ability — 20% chance for mobs with a named ability
        if (monster is not Mob { SpecialAbility: "Charge" or "Leap" } abilityMob) return false;
        bool leap = abilityMob.SpecialAbility == "Leap";
        if (!leap && dist > 3) return false;
        if (RunRng.Next(100) >= 20) return false;
        var adjTile = FindAdjacentWalkable(monster, _player.X, _player.Y);
        if (!adjTile.HasValue) return false;
        _map.MoveEntity(monster, adjTile.Value.x, adjTile.Value.y);

        if (leap)
        {
            // Teleport adjacent to player, then the normal attack
            _log.LogCombat($"{monster.Name} leaps through the air toward you!");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "LEAP!", Color.BrightCyan);
            ProcessMonsterAttack(monster);
            return true;
        }

        // Rush adjacent to player and deal 1.5x damage
        _lastCombatTurn = TurnCount;
        int chargeDmg = Math.Max(1, (int)(CalcMonsterDamage(monster) * 1.5));
        int chargeHit = Math.Max(1, chargeDmg - IncomingDefense);
        _log.LogCombat($"{monster.Name} charges at you with devastating force!");
        CombatTextEvent?.Invoke(monster.X, monster.Y, "CHARGE!", Color.BrightRed);
        _player.TakeDamage(chargeHit);
        DamageDealt?.Invoke(_player.X, _player.Y, chargeHit, true, false);
        _floorDamageTaken += chargeHit;
        _killStreak = 0;
        if (_player.IsDefeated)
        {
            LastKillerName = monster.Name;
            _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
            RaisePlayerDied(KillCause(monster));
        }
        return true;
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
            string lootTag = monster is Mob mb ? mb.LootTag : "generic";
            bool isBoss      = monster is Boss && monster is not FieldBoss;
            bool isFieldBoss = monster is FieldBoss;
            bool isElite     = monster is Mob me && (me.Variant == "Elite" || me.Variant == "Champion");
            bool pz = monster is Mob mpz && mpz.CanPoison;
            bool bl = monster is Mob mbl && mbl.CanBleed;
            bool st = monster is Mob mst && mst.CanStun;
            bool sl = monster is Mob msl && msl.CanSlow;
            Bestiary.RecordEncounter(
                monster.Name, monster.Level, monster.MaxHealth, monster.BaseAttack,
                floor: CurrentFloor,
                lootTag: lootTag,
                glyph: monster.Symbol, glyphColor: monster.SymbolColor,
                isBoss: isBoss, isFieldBoss: isFieldBoss, isElite: isElite,
                canPoison: pz, canBleed: bl, canStun: st, canSlow: sl);
            MilestoneSystem.OnBestiaryDiscovered(_player);
            if (monster.Level >= _player.Level + 3 && _dangerWarned.Add(monster.Id))
            {
                int diff = monster.Level - _player.Level;
                string warn = diff >= 6 ? "!! EXTREME DANGER !!" : "!! DANGER !!";
                _log.LogCombat($"{warn} {monster.Name} (Lv{monster.Level}) -- {diff} levels above you!");
            }
        }

        if (_map.InBounds(newX, newY) && IsMonsterWalkable(monster, newX, newY))
            _map.MoveEntity(monster, newX, newY);
    }

    // Mob passability. Land uses Tile.IsWalkable; CanSwim adds Water/WaterDeep.
    private bool IsMonsterWalkable(Monster monster, int x, int y)
    {
        var tile = _map.GetTile(x, y);
        if (tile.IsWalkable) return true;
        if (!monster.CanSwim) return false;
        if (tile.Occupant != null) return false;
        return tile.Type is TileType.Water or TileType.WaterDeep;
    }
}
