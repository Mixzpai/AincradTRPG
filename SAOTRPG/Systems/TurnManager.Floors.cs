using SAOTRPG.Entities;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    public void AscendFloor()
    {
        int elapsed = TurnCount - _floorStartTurn;
        int par = FloorParTurns[Math.Min(CurrentFloor - 1, FloorParTurns.Length - 1)];
        if (elapsed <= par)
        {
            int bonus = 50 + CurrentFloor * 30;
            _player.ColOnHand += bonus;
            TotalColEarned += bonus;
            _log.LogLoot($"  SPEED CLEAR! Finished in {elapsed} turns (par: {par}). +{bonus} Col!");
        }

        int floorKills = KillCount - _floorKillsStart;
        int explored = _map.GetExplorationPercent();
        LastFloorRecap = new FloorRecapData(CurrentFloor, floorKills, _floorItemsFound,
            _floorDamageTaken, elapsed, explored, FloorColEarned,
            _bountyTarget, _bountyKillsCurrent, _bountyKillsNeeded, _bountyComplete,
            FloorRealTime);
        _log.LogSystem("[ Floor Recap ]");
        _log.LogSystem($"  Kills: {floorKills} | Items: {_floorItemsFound} | Damage taken: {_floorDamageTaken}");
        _log.LogSystem($"  Turns: {elapsed} | Explored: {explored}%");

        if (explored >= 90)
        {
            int xpBonus = 50 * CurrentFloor;
            int colBonus = 100 * CurrentFloor;
            int expLvl = _player.Level;
            _player.GainExperience(xpBonus);
            if (_player.Level > expLvl) LeveledUp?.Invoke();
            _player.ColOnHand += colBonus;
            TotalColEarned += colBonus;
            _log.LogLoot($"  Thorough exploration rewarded! +{xpBonus} XP, +{colBonus} Col!");
        }

        foreach (var ach in Achievements.CheckFloor(this, _player, speedClear: elapsed <= par))
        {
            _player.ColOnHand += ach.ColReward;
            TotalColEarned += ach.ColReward;
            _log.LogSystem($"  **ACHIEVEMENT: {ach.Name} — {ach.Description} (+{ach.ColReward} Col)");
        }

        CurrentFloor++;
        SAOTRPG.Map.TileDefinitions.CurrentFloor = CurrentFloor;

        if (SaveManager.SaveGame(_player, this, ActiveSaveSlot))
            _log.LogSystem("  [Game saved]");

        if (CurrentFloor > 100)
        {
            UpdatePlayerTitle();
            _log.LogSystem("====================================");
            _log.LogSystem("  You have cleared all 100 floors of Aincrad!");
            _log.LogSystem("  The death game is over. You are free.");
            _log.LogSystem("====================================");
            GameWon?.Invoke();
            return;
        }

        ResetFloorState();
        // Ascending always generates a fresh overworld — discard any
        // saved labyrinth/overworld map from the previous floor.
        _inLabyrinth = false;
        _overworldMap = null;
        GenerateNewFloor();
        UpdateVisibility();
        UpdatePlayerTitle();

        _log.Log(FlavorText.GetFloorEntryMessage(CurrentFloor));

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

        int mobs = GetMonsterCount();
        if (mobs > 0)
        {
            string mobMsg = string.Format(
                FlavorText.FloorMobCountFlavors[Random.Shared.Next(FlavorText.FloorMobCountFlavors.Length)], mobs);
            _log.LogCombat(mobMsg);
        }

        if (_map.Entities.Any(e => e is Vendor))
            _log.Log("A vendor has set up shop on this floor. New stock available!");

        _log.Log($"  [{DateTime.Now:h:mm tt}]");

        WeatherSystem.RollWeather(CurrentFloor);
        BiomeSystem.SetFloor(CurrentFloor);
        _log.Log($"Biome: {BiomeSystem.DisplayName} -- {BiomeSystem.GetEntryMessage(CurrentFloor)}");
        if (WeatherSystem.Current != WeatherType.Clear)
            _log.LogSystem($"Weather: {WeatherSystem.GetLabel()}");

        // Field boss flavor announcement.
        foreach (var entity in _map.Entities)
        {
            if (entity is Entities.FieldBoss fb && !fb.IsDefeated && !string.IsNullOrEmpty(fb.EncounterFlavor))
                _log.LogSystem($"  ‹ {fb.EncounterFlavor} ›");
        }

        FloorChanged?.Invoke(CurrentFloor);
        Story.StorySystem.TryFire(Story.StoryTrigger.FloorEntry,
            new Story.StoryContext(CurrentFloor, KillCount, _player));

        // FB-564 Laughing Coffin modifier — PK squad ambush every 5 floors.
        if (RunModifiers.IsActive(RunModifier.LaughingCoffin) && CurrentFloor % 5 == 0)
            SpawnLaughingCoffinSquad();

        TurnCompleted?.Invoke();
    }

    private void SpawnLaughingCoffinSquad()
    {
        _log.LogSystem("══════════════════════════════════════");
        _log.LogSystem("  A red-cloaked squad fades into the treeline.");
        _log.LogSystem("  Laughing Coffin has found you.");
        _log.LogSystem("══════════════════════════════════════");
        int squadSize = 2 + CurrentFloor / 20;
        for (int i = 0; i < squadSize; i++)
        {
            var pker = MobFactory.CreateFloorMob(CurrentFloor, _diffTier.MobStatPercent);
            pker.Name = $"Laughing Coffin PKer";
            pker.LootTag = "humanoid";
            pker.SetAppearance('P', Terminal.Gui.Color.BrightRed);
            int tries = 0;
            while (tries < 40)
            {
                int x = Random.Shared.Next(10, _map.Width - 10);
                int y = Random.Shared.Next(10, _map.Height - 10);
                if (_map.InBounds(x, y) && _map.GetTile(x, y).IsWalkable && _map.GetTile(x, y).Occupant == null)
                {
                    _map.PlaceEntity(pker, x, y);
                    break;
                }
                tries++;
            }
        }
    }

    private void ResetFloorState()
    {
        _floorStartTurn = TurnCount;
        _floorStartRealTime = DateTime.Now;
        _floorFullyExplored = false;
        _stairsDiscovered = false;
        _extraSearchRevealedThisFloor = false;
        _aggroAlerted.Clear();
        _dangerWarned.Clear();
        ClearAllMobStatuses();
        _poisonTurnsLeft = 0;
        _bleedTurnsLeft = 0;
        _floorDamageTaken = 0;
        _floorItemsFound = 0;
        _floorKillsStart = KillCount;
        _restCounter = 0;
        _fatiguedWarned = false;
        _exhaustedWarned = false;
        _bountyTarget = null;
        _bountyKillsNeeded = 0;
        _bountyKillsCurrent = 0;
        QuestSystem.OnFloorChange();
        _bountyRewardCol = 0;
        _bountyRewardXp = 0;
        _bountyComplete = false;
        _floorColStart = _player.ColOnHand;
        ClearExplorePath();
    }

    private void GenerateNewFloor()
    {
        var (newMap, rooms) = MapGenerator.GenerateFloor(CurrentFloor);
        _map = newMap;
        MapGenerator.PopulateFloor(newMap, rooms, _player, CurrentFloor, _diffTier.MobStatPercent, DefeatedFieldBosses);
        PartySystem.ScaleToPlayer(_player.Level);
        // Revive KO'd allies with half HP between floors.
        foreach (var ally in PartySystem.Members)
            if (ally.IsDefeated) ally.Revive(ally.MaxHealth / 2);
        PartySystem.PlaceAllies(newMap, _player.X, _player.Y);
        foreach (var entity in newMap.Entities)
            entity.SetLog(_log);
    }

    private void RevealStairs()
    {
        for (int x = 0; x < _map.Width; x++)
        for (int y = 0; y < _map.Height; y++)
        {
            if (_map.GetTile(x, y).Type == TileType.StairsUp)
            {
                for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    _map.SetExplored(x + dx, y + dy);

                if (!_stairsDiscovered)
                {
                    _stairsDiscovered = true;
                    _log.LogSystem("  The path to the stairs is revealed on the minimap!");
                }
                return;
            }
        }
    }
}
