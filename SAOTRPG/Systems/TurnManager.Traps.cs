using SAOTRPG.Entities;
using SAOTRPG.Map;
using Tile = SAOTRPG.Map.Tile;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    private bool HandleTrapEffects(Tile tile, int tx, int ty)
    {
        _map.SetTrapHidden(tx, ty, false);
        // Each trap encounter counts toward the Extra: Search unique skill unlock.
        var searchUnlock = Skills.UniqueSkillSystem.OnTrapDisarmed();
        if (searchUnlock != null) NotifyUniqueSkillUnlock(searchUnlock.Value);
        switch (tile.Type)
        {
            case TileType.TrapSpike:    return HandleSpikeTrap(tile, tx, ty);
            case TileType.TrapTeleport: HandleTeleportTrap(tile, tx, ty); break;
            case TileType.TrapPoison:   HandlePoisonTrap(tile, tx, ty); break;
            case TileType.TrapAlarm:    HandleAlarmTrap(tile, tx, ty); break;
            case TileType.TrapWeb:      HandleWebTrap(); break;
            case TileType.TrapMagnet:   HandleMagnetTrap(tx, ty); break;
            case TileType.TrapRune:     return HandleRuneTrap(tile, tx, ty);
            case TileType.GasVent:      HandleGasVent(tile); break;
        }
        return false;
    }

    // Web — pure movement denial, no damage. Slow already exists as a status with a tick and a
    // tray icon (SLW), so this reuses it rather than adding a second immobilise concept.
    private void HandleWebTrap()
    {
        int turns = 3 + CurrentFloor / 20;
        _slowTurnsLeft = Math.Max(_slowTurnsLeft, turns);
        _log.LogCombat("Thick web snares your legs!");
        _log.LogCombat($"  You are slowed for {_slowTurnsLeft} turns.");
    }

    // Magnet — drags every nearby monster two steps toward the player. Costs no HP directly; the
    // danger is what it collects. Uses the same passability rule mob movement uses, so it can
    // never push a monster into a wall or onto another occupant.
    private void HandleMagnetTrap(int tx, int ty)
    {
        _log.LogCombat("A pulse of force floods the chamber — something is pulling!");
        int dragged = 0;
        // ToList: MoveEntity mutates the map's monster bookkeeping while we walk it.
        foreach (var m in _map.Monsters.ToList())
        {
            if (m.IsDefeated) continue;
            int dx = tx - m.X, dy = ty - m.Y;
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) is < 2 or > 6) continue;
            bool moved = false;
            for (int step = 0; step < 2; step++)
            {
                int sx = m.X + Math.Sign(tx - m.X), sy = m.Y + Math.Sign(ty - m.Y);
                if (sx == tx && sy == ty) break;          // never stack onto the player
                if (!_map.InBounds(sx, sy) || !IsMonsterWalkable(m, sx, sy)) break;
                _map.MoveEntity(m, sx, sy);
                moved = true;
            }
            if (moved) dragged++;
        }
        _log.LogCombat(dragged > 0
            ? $"  {dragged} enemy(s) are dragged toward you!"
            : "  Nothing is close enough to be pulled.");
    }

    // Rune — burst damage that ignores armour, plus a brief stun. The armour bypass is what makes
    // it the late-floor hazard: it does not soften as gear improves.
    private bool HandleRuneTrap(Tile tile, int tx, int ty)
    {
        int dmg = 6 + CurrentFloor * 2;
        _log.LogCombat("A dormant rune flares white beneath your feet!");
        _player.TakeDamage(dmg);
        CombatTextEvent?.Invoke(tx, ty, $"RUNE -{dmg}", Color.BrightMagenta);
        ParticleQueue.Emit(ParticleEvent.CritShatter, tx, ty);
        _log.LogCombat($"  Raw force tears through your armour for {dmg} damage!");
        if (_stunTurnsLeft <= 0)
        {
            _stunTurnsLeft = 1;
            _log.LogCombat("  The blast leaves you reeling. (stunned 1 turn)");
        }
        _map.SetTileType(tx, ty, TileType.Floor);
        if (_player.IsDefeated)
        {
            LastKillerName = "a warding rune";
            _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
            RaisePlayerDied("trap");
            return true;
        }
        return false;
    }

    private void HandleGasVent(Tile tile)
    {
        _log.LogCombat("Toxic gas hisses from a vent beneath your feet!");
        if (_poisonTurnsLeft <= 0)
        {
            _poisonTurnsLeft = 3 + WeatherSystem.GetPoisonDurationBonus();
            _poisonDamagePerTick = 1 + CurrentFloor / 2;
            _log.LogCombat($"  The fumes poison you! ({_poisonDamagePerTick} dmg/turn for {_poisonTurnsLeft} turns)");
        }
        else
            _log.Log("  The poison in your system intensifies...");
    }

    private bool HandleSpikeTrap(Tile tile, int tx, int ty)
    {
        int trapDmg = 3 + CurrentFloor * 2;
        _player.TakeDamage(trapDmg);
        DamageDealt?.Invoke(tx, ty, trapDmg, true, false);
        _log.LogCombat(string.Format(
            FlavorText.SpikeTrapFlavors[RunRng.Next(FlavorText.SpikeTrapFlavors.Length)], trapDmg));
        _map.SetTileType(tx, ty, TileType.Floor);
        if (_player.IsDefeated)
        {
            LastKillerName = "a spike trap";
            _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
            RaisePlayerDied("trap");
            return true;
        }
        return false;
    }

    private void HandleTeleportTrap(Tile tile, int tx, int ty)
    {
        _log.LogCombat("A teleport trap activates! You're warped to a random location!");
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int rx = RunRng.Next(5, _map.Width - 5);
            int ry = RunRng.Next(5, _map.Height - 5);
            var rtile = _map.GetTile(rx, ry);
            if (!rtile.BlocksMovement && rtile.Occupant == null
                && rtile.Type is not (TileType.TrapTeleport or TileType.TrapSpike
                    or TileType.TrapPoison or TileType.TrapAlarm
                    or TileType.TrapWeb or TileType.TrapMagnet or TileType.TrapRune))
            {
                _map.MoveEntity(_player, rx, ry);
                _log.LogCombat(FlavorText.TeleportLandingFlavors[RunRng.Next(FlavorText.TeleportLandingFlavors.Length)]);
                break;
            }
        }
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void HandlePoisonTrap(Tile tile, int tx, int ty)
    {
        _log.LogCombat("A poison trap triggers! Toxic gas engulfs you!");
        if (_poisonTurnsLeft <= 0)
        {
            _poisonTurnsLeft = 3;
            _poisonDamagePerTick = 2 + CurrentFloor;
            _log.LogCombat($"  You are poisoned! ({_poisonDamagePerTick} dmg/turn for {_poisonTurnsLeft} turns)");
        }
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private void HandleAlarmTrap(Tile tile, int tx, int ty)
    {
        _log.LogCombat("An alarm trap sounds! Nearby monsters are alerted!");
        foreach (var entity in _map.Entities)
        {
            if (entity is Mob mob && !mob.IsDefeated)
            {
                int dist = Math.Max(Math.Abs(mob.X - tx), Math.Abs(mob.Y - ty));
                if (dist <= 10) _log.LogCombat($"  {mob.Name} heard the alarm!");
            }
        }
        _map.SetTileType(tx, ty, TileType.Floor);
    }

    private bool HandleTallGrassAmbush(Tile tile, int tx, int ty)
    {
        if (tile.Type != TileType.GrassTall) return false;
        int ambushChance = Math.Max(0, 25 - _player.Dexterity);
        if (ambushChance <= 0) return false;

        Monster? ambusher = null;
        int bestDist = int.MaxValue;
        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster m) continue;
            int md = Math.Abs(m.X - tx) + Math.Abs(m.Y - ty);
            if (md <= 2 && md < bestDist) { ambusher = m; bestDist = md; }
        }

        if (ambusher != null && RunRng.Next(100) < ambushChance)
        {
            _log.LogCombat(FlavorText.AmbushFlavors[RunRng.Next(FlavorText.AmbushFlavors.Length)]);
            int ambushDmg = Math.Max(1, ambusher.BaseAttack - _player.Defense / 2);
            _player.TakeDamage(ambushDmg);
            DamageDealt?.Invoke(tx, ty, ambushDmg, true, false);
            _log.LogCombat($"{ambusher.Name} strikes for {ambushDmg} damage!");
            if (_player.IsDefeated)
            {
                LastKillerName = ambusher.Name;
                _log.LogSystem(FlavorText.DeathFlavors[RunRng.Next(FlavorText.DeathFlavors.Length)]);
                RaisePlayerDied("monster");
                return true;
            }
        }
        else if (ambusher != null)
            _log.Log(FlavorText.AmbushAvoidedFlavors[RunRng.Next(FlavorText.AmbushAvoidedFlavors.Length)]);

        return false;
    }

    private void HandleTrapDetection(int tx, int ty)
    {
        int baseChance = Math.Min(30, _player.Dexterity * 3) + WeatherSystem.GetTrapDetectionPenalty();
        if (RunRng.Next(100) >= baseChance) return;

        for (int tdx = -1; tdx <= 1; tdx++)
        for (int tdy = -1; tdy <= 1; tdy++)
        {
            if (tdx == 0 && tdy == 0) continue;
            if (!_map.InBounds(tx + tdx, ty + tdy)) continue;
            var adjTile = _map.GetTile(tx + tdx, ty + tdy);
            if (adjTile.Type is TileType.TrapSpike or TileType.TrapTeleport
                or TileType.TrapPoison or TileType.TrapAlarm)
            {
                if (adjTile.TrapHidden)
                {
                    int detectChance = Math.Min(95, 30 + _player.Dexterity * 3);
                    if (RunRng.Next(100) < detectChance)
                    {
                        _map.SetTrapHidden(tx + tdx, ty + tdy, false);
                        _log.LogSystem("Your keen senses reveal a hidden trap nearby!");
                    }
                    else
                        _log.LogCombat(FlavorText.TrapSenseFlavors[RunRng.Next(FlavorText.TrapSenseFlavors.Length)]);
                }
                return;
            }
        }
    }
}
