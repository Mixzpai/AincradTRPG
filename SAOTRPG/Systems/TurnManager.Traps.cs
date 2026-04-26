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
            case TileType.GasVent:      HandleGasVent(tile); break;
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
            FlavorText.SpikeTrapFlavors[Random.Shared.Next(FlavorText.SpikeTrapFlavors.Length)], trapDmg));
        _map.SetTileType(tx, ty, TileType.Floor);
        if (_player.IsDefeated)
        {
            LastKillerName = "a spike trap";
            _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
            PlayerDied?.Invoke();
            return true;
        }
        return false;
    }

    private void HandleTeleportTrap(Tile tile, int tx, int ty)
    {
        _log.LogCombat("A teleport trap activates! You're warped to a random location!");
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int rx = Random.Shared.Next(5, _map.Width - 5);
            int ry = Random.Shared.Next(5, _map.Height - 5);
            var rtile = _map.GetTile(rx, ry);
            if (!rtile.BlocksMovement && rtile.Occupant == null
                && rtile.Type is not (TileType.TrapTeleport or TileType.TrapSpike
                    or TileType.TrapPoison or TileType.TrapAlarm))
            {
                _map.MoveEntity(_player, rx, ry);
                _log.LogCombat(FlavorText.TeleportLandingFlavors[Random.Shared.Next(FlavorText.TeleportLandingFlavors.Length)]);
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

        if (ambusher != null && Random.Shared.Next(100) < ambushChance)
        {
            _log.LogCombat(FlavorText.AmbushFlavors[Random.Shared.Next(FlavorText.AmbushFlavors.Length)]);
            int ambushDmg = Math.Max(1, ambusher.BaseAttack - _player.Defense / 2);
            _player.TakeDamage(ambushDmg);
            DamageDealt?.Invoke(tx, ty, ambushDmg, true, false);
            _log.LogCombat($"{ambusher.Name} strikes for {ambushDmg} damage!");
            if (_player.IsDefeated)
            {
                LastKillerName = ambusher.Name;
                _log.LogSystem(FlavorText.DeathFlavors[Random.Shared.Next(FlavorText.DeathFlavors.Length)]);
                PlayerDied?.Invoke();
                return true;
            }
        }
        else if (ambusher != null)
            _log.Log(FlavorText.AmbushAvoidedFlavors[Random.Shared.Next(FlavorText.AmbushAvoidedFlavors.Length)]);

        return false;
    }

    private void HandleTrapDetection(int tx, int ty)
    {
        int baseChance = Math.Min(30, _player.Dexterity * 3) + WeatherSystem.GetTrapDetectionPenalty();
        if (Random.Shared.Next(100) >= baseChance) return;

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
                    if (Random.Shared.Next(100) < detectChance)
                    {
                        _map.SetTrapHidden(tx + tdx, ty + tdy, false);
                        _log.LogSystem("Your keen senses reveal a hidden trap nearby!");
                    }
                    else
                        _log.LogCombat(FlavorText.TrapSenseFlavors[Random.Shared.Next(FlavorText.TrapSenseFlavors.Length)]);
                }
                return;
            }
        }
    }
}
