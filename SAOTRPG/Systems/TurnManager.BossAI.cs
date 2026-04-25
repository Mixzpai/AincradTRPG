using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

// Boss-specific AI: phase transitions, ability execution, minion summoning.
public partial class TurnManager
{
    // Check for phase transitions and execute boss abilities.
    // Called from ProcessMonsterAttack when the monster is a Boss.
    private bool TryBossAbility(Boss boss)
    {
        // Phase transition announcement.
        if (boss.CurrentPhase > boss.LastAnnouncedPhase)
        {
            boss.LastAnnouncedPhase = boss.CurrentPhase;
            string phaseMsg = boss.CurrentPhase switch
            {
                2 => $"{boss.Name} shifts stance -- Phase 2!",
                3 => $"{boss.Name} roars with fury -- Phase 3!",
                4 => $"{boss.Name} enters a berserk rage -- FINAL PHASE!",
                _ => "",
            };
            if (phaseMsg.Length > 0)
            {
                _log.LogCombat($"!! {phaseMsg}");
                CombatTextEvent?.Invoke(boss.X, boss.Y, $"PHASE {boss.CurrentPhase}", Color.BrightMagenta);
            }
        }

        if (boss.Abilities.Length == 0) return false;

        // Find a usable ability for the current phase.
        foreach (var ability in boss.Abilities)
        {
            if (ability.MinPhase > boss.CurrentPhase) continue;
            if (TurnCount - ability.LastUsedTurn < ability.Cooldown) continue;

            // Random chance to use ability (higher in later phases).
            int useChance = 15 + boss.CurrentPhase * 10;
            if (Random.Shared.Next(100) >= useChance) continue;

            ability.LastUsedTurn = TurnCount;
            ExecuteBossAbility(boss, ability);
            return true;
        }
        return false;
    }

    private void ExecuteBossAbility(Boss boss, BossAbility ability)
    {
        switch (ability.Type)
        {
            case BossAbilityType.HeavyStrike:
                ExecuteHeavyStrike(boss, ability);
                break;
            case BossAbilityType.AoESlam:
                ExecuteAoESlam(boss, ability);
                break;
            case BossAbilityType.SummonMinions:
                ExecuteSummonMinions(boss, ability);
                break;
            case BossAbilityType.HealSelf:
                ExecuteHealSelf(boss, ability);
                break;
            case BossAbilityType.ChargeAttack:
                ExecuteChargeAttack(boss, ability);
                break;
            case BossAbilityType.StatusBreath:
                ExecuteStatusBreath(boss, ability);
                break;
        }
    }

    private void ExecuteHeavyStrike(Boss boss, BossAbility ability)
    {
        int dist = Math.Max(Math.Abs(_player.X - boss.X), Math.Abs(_player.Y - boss.Y));
        if (dist > 1) return; // must be adjacent

        int dmg = (int)(CalcMonsterDamage(boss) * ability.DamageMultiplier);
        int finalDmg = Math.Max(1, dmg - (_player.Defense + _shrineBuff) / 3);
        _log.LogCombat($"!! {boss.Name} uses {ability.Name}!");
        CombatTextEvent?.Invoke(boss.X, boss.Y, ability.Name, Color.BrightRed);
        _player.TakeDamage(finalDmg);
        DamageDealt?.Invoke(_player.X, _player.Y, finalDmg, true, false);
        MapViewShakeRequested?.Invoke(2);
        _floorDamageTaken += finalDmg;
    }

    private void ExecuteAoESlam(Boss boss, BossAbility ability)
    {
        _log.LogCombat($"!! {boss.Name} uses {ability.Name} -- ground shakes!");
        CombatTextEvent?.Invoke(boss.X, boss.Y, ability.Name, Color.BrightRed);
        MapViewShakeRequested?.Invoke(2);

        int baseDmg = (int)(CalcMonsterDamage(boss) * ability.DamageMultiplier * 0.7);
        int dist = Math.Max(Math.Abs(_player.X - boss.X), Math.Abs(_player.Y - boss.Y));
        if (dist <= ability.AoERadius)
        {
            int finalDmg = Math.Max(1, baseDmg - (_player.Defense + _shrineBuff) / 3);
            _log.LogCombat($"  The shockwave hits you for {finalDmg}!");
            _player.TakeDamage(finalDmg);
            DamageDealt?.Invoke(_player.X, _player.Y, finalDmg, true, false);
            _floorDamageTaken += finalDmg;
        }
        else
        {
            _log.LogCombat("  You were out of range!");
        }

        // Also damage allies in range.
        foreach (var ally in PartySystem.Members)
        {
            if (ally.IsDefeated) continue;
            int allyDist = Math.Max(Math.Abs(ally.X - boss.X), Math.Abs(ally.Y - boss.Y));
            if (allyDist <= ability.AoERadius)
            {
                int allyDmg = Math.Max(1, baseDmg / 2);
                bool ko = ally.TakeDamage(allyDmg);
                _log.LogCombat($"  {ally.Name} takes {allyDmg} from the shockwave!");
                if (ko) _log.LogCombat($"  {ally.Name} has been knocked out!");
            }
        }
    }

    private void ExecuteSummonMinions(Boss boss, BossAbility ability)
    {
        _log.LogCombat($"!! {boss.Name} uses {ability.Name} -- reinforcements arrive!");
        CombatTextEvent?.Invoke(boss.X, boss.Y, "SUMMON!", Color.BrightMagenta);

        int spawned = 0;
        for (int i = 0; i < ability.SummonCount; i++)
        {
            // Find open tile near boss.
            for (int r = 1; r <= 3; r++)
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                int nx = boss.X + dx, ny = boss.Y + dy;
                if (!_map.InBounds(nx, ny)) continue;
                var t = _map.GetTile(nx, ny);
                if (t.BlocksMovement || t.Occupant != null) continue;
                var mob = MobFactory.CreateFloorMob(CurrentFloor, _diffTier.MobStatPercent);
                _map.PlaceEntity(mob, nx, ny);
                mob.SetLog(_log);
                spawned++;
                goto nextMinion;
            }
            nextMinion:;
        }
        if (spawned > 0)
            _log.LogCombat($"  {spawned} minion(s) appeared!");
    }

    private void ExecuteHealSelf(Boss boss, BossAbility ability)
    {
        int heal = (int)(boss.MaxHealth * 0.1);
        boss.CurrentHealth = Math.Min(boss.MaxHealth, boss.CurrentHealth + heal);
        _log.LogCombat($"!! {boss.Name} uses {ability.Name} -- recovers {heal} HP!");
        CombatTextEvent?.Invoke(boss.X, boss.Y, $"+{heal} HP", Color.BrightGreen);
    }

    private void ExecuteChargeAttack(Boss boss, BossAbility ability)
    {
        int dist = Math.Max(Math.Abs(_player.X - boss.X), Math.Abs(_player.Y - boss.Y));
        if (dist <= 1 || dist > 4) return; // need gap to charge

        // Rush adjacent to player.
        var adj = FindAdjacentWalkable(boss, _player.X, _player.Y);
        if (!adj.HasValue) return;

        _map.MoveEntity(boss, adj.Value.x, adj.Value.y);
        int dmg = (int)(CalcMonsterDamage(boss) * ability.DamageMultiplier);
        int finalDmg = Math.Max(1, dmg - (_player.Defense + _shrineBuff) / 3);
        _log.LogCombat($"!! {boss.Name} charges with {ability.Name}!");
        CombatTextEvent?.Invoke(boss.X, boss.Y, "CHARGE!", Color.BrightRed);
        _player.TakeDamage(finalDmg);
        DamageDealt?.Invoke(_player.X, _player.Y, finalDmg, true, false);
        MapViewShakeRequested?.Invoke(2);
        _floorDamageTaken += finalDmg;
    }

    private void ExecuteStatusBreath(Boss boss, BossAbility ability)
    {
        _log.LogCombat($"!! {boss.Name} uses {ability.Name}!");
        CombatTextEvent?.Invoke(boss.X, boss.Y, ability.Name, Color.BrightMagenta);

        int dist = Math.Max(Math.Abs(_player.X - boss.X), Math.Abs(_player.Y - boss.Y));
        if (dist > ability.AoERadius) return;

        if (Random.Shared.NextDouble() < ability.StatusChance && ability.StatusEffect != null)
        {
            // Uninterruptible+N — chance to shrug off boss status breath.
            var breathWpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
            int breathUninterrupt = GetSpecialEffectValue(breathWpn, "Uninterruptible");
            if (breathUninterrupt > 0 && Random.Shared.Next(100) < breathUninterrupt)
            {
                _log.LogCombat($"  {breathWpn!.Name} shields you from {ability.Name}!");
                return;
            }
            switch (ability.StatusEffect)
            {
                case "Poison":
                    _poisonTurnsLeft = Math.Max(_poisonTurnsLeft, 5);
                    _poisonDamagePerTick = 2 + CurrentFloor;
                    _log.LogCombat($"  You are poisoned! ({_poisonDamagePerTick} dmg/turn)");
                    break;
                case "Stun":
                    _stunTurnsLeft = Math.Max(_stunTurnsLeft, 2);
                    _log.LogCombat("  You are stunned!");
                    break;
                case "Bleed":
                    _bleedTurnsLeft = Math.Max(_bleedTurnsLeft, 4);
                    _bleedDamagePerTick = 2 + CurrentFloor;
                    _log.LogCombat($"  You are bleeding! ({_bleedDamagePerTick} dmg/turn)");
                    break;
                case "Slow":
                    _slowTurnsLeft = Math.Max(_slowTurnsLeft, 4);
                    _log.LogCombat("  You are slowed! Dodge chance halved.");
                    break;
            }
        }
        else
        {
            // Still deals some damage even if status doesn't proc.
            int dmg = Math.Max(1, CalcMonsterDamage(boss) / 2);
            _player.TakeDamage(dmg);
            _log.LogCombat($"  The breath hits you for {dmg}!");
        }
    }
}
