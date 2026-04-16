using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;

namespace SAOTRPG.Systems;

// Player movement processing — rest, sprint, stealth, normal movement,
// occupant interactions, stairs discovery, and sound cues.
public partial class TurnManager
{
    public void ProcessRest()
    {
        if (_player.IsDefeated) return;
        if (_player.CurrentHealth >= _player.MaxHealth)
        { _log.Log("Already at full health. Press Space to wait a turn instead."); return; }

        int nearbyCount = _map.Monsters.Count(m => !m.IsDefeated
            && Math.Abs(m.X - _player.X) <= 1 && Math.Abs(m.Y - _player.Y) <= 1);
        if (nearbyCount > 0)
        {
            string plural = nearbyCount == 1 ? "enemy is" : "enemies are";
            _log.LogCombat($"{nearbyCount} {plural} too close! Move away first.");
            return;
        }

        _log.Log("You sit down and rest...");
        int totalHealed = 0;
        for (int i = 0; i < 3; i++)
        {
            if (_player.IsDefeated) break;
            int heal = Math.Min(3, _player.MaxHealth - _player.CurrentHealth);
            if (heal > 0) { _player.CurrentHealth += heal; totalHealed += heal; }
            TurnCount++;
            TickPoison(); TickBleed(); TickSlow();
            if (_player.IsDefeated) return;
            ProcessEntityTurns();
            if (_player.IsDefeated) return;
        }
        _restCounter = 0;
        _fatiguedWarned = false;
        _exhaustedWarned = false;
        _log.LogSystem($"You feel refreshed. (+{totalHealed} HP)");
        UpdateVisibility();
        TurnCompleted?.Invoke();
    }

    public void ProcessSprint(int dx, int dy)
    {
        if (_player.IsDefeated) return;
        int x1 = _player.X + dx, y1 = _player.Y + dy;
        int x2 = _player.X + dx * 2, y2 = _player.Y + dy * 2;

        if (!_map.InBounds(x1, y1) || !_map.InBounds(x2, y2))
        { _log.Log("Can't sprint that way."); return; }
        var t1 = _map.GetTile(x1, y1);
        var t2 = _map.GetTile(x2, y2);
        if (t1.BlocksMovement || t2.BlocksMovement || t1.Occupant != null || t2.Occupant != null)
        { _log.Log("Can't sprint — path blocked!"); return; }

        _map.MoveEntity(_player, x2, y2);
        TurnCount++;
        TickPoison(); TickBleed(); TickSlow();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        if (_player.IsDefeated) return;
        UpdateVisibility();
        TurnCompleted?.Invoke();
    }

    public void EnterCounterStance()
    {
        if (_player.IsDefeated) return;
        _counterStance = true;
        _log.LogCombat("You raise your guard — ready to counter the next attack!");
        AdvanceTurn();
        TickPoison(); TickBleed(); TickSlow();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        _counterStance = false; // expires after one round
        PassiveRegen();
        TurnCompleted?.Invoke();
    }

    public void ProcessStealthMove(int dx, int dy)
    {
        _stealthActive = true;
        _lastMoveWasStealth = true;
        _log.Log("You move silently...");
        ProcessPlayerMove(dx, dy);
        _stealthActive = false;
    }

    public void ProcessPlayerMove(int dx, int dy)
    {
        if (_player.IsDefeated) return;

        if (_stunTurnsLeft > 0)
        {
            _log.LogCombat("You are stunned and cannot act!");
            AdvanceTurn();
            TickStun(); TickPoison(); TickBleed(); TickSlow();
            if (_player.IsDefeated) return;
            ProcessEntityTurns();
            PassiveRegen();
            TurnCompleted?.Invoke();
            return;
        }

        if (!_stealthActive) _lastMoveWasStealth = false;

        if (dx == 0 && dy == 0)
        {
            _idleTurns++;
            if (_idleTurns >= FlavorText.IdleThreshold && _idleTurns % FlavorText.IdleRepeatInterval == 0)
                _log.Log(FlavorText.IdleFlavors[Random.Shared.Next(FlavorText.IdleFlavors.Length)]);
        }
        else _idleTurns = 0;

        int tx = _player.X + dx, ty = _player.Y + dy;
        var tile = _map.GetTile(tx, ty);

        TutorialSystem.ShowTip(_log, "first_move");

        if (tile.BlocksMovement)
        {
            TutorialSystem.ShowTip(_log, "first_wall_bump");
            if (tile.Type == TileType.CrackedWall)
            {
                _map.SetTileType(tx, ty, TileType.Floor);
                _log.LogLoot("You smash through the cracked wall! A hidden chamber lies beyond!");
                CombatTextEvent?.Invoke(tx, ty, "SECRET!", Color.BrightYellow);
                return;
            }
            if (Random.Shared.Next(100) < 3)
                _log.Log(FlavorText.WallBumpFlavors[Random.Shared.Next(FlavorText.WallBumpFlavors.Length)]);
            return;
        }

        if (tile.Occupant != null)
        {
            if (tile.Occupant is Monster) TutorialSystem.ShowTip(_log, "first_combat");
            HandleOccupantInteraction(tile, tx, ty);
            return;
        }

        _map.MoveEntity(_player, tx, ty);
        _map.IncrementVisit(tx, ty);

        // Tip when first leaving safe zone on Floor 1
        if (_map.SafeZone.HasValue && !_map.SafeZone.Value.Contains(tx, ty))
            TutorialSystem.ShowTip(_log, "floor1_exit_town");

        // Biome movement effects: ice slip, swamp poison.
        if (BiomeSystem.SlipChance > 0 && Random.Shared.Next(100) < BiomeSystem.SlipChance)
        {
            _log.Log("You slip on the icy surface!");
            // Slip = lose the rest of this turn (no further processing).
            TurnCount++; TickPoison(); TickBleed(); TickSlow();
            if (_player.IsDefeated) return;
            ProcessEntityTurns(); PassiveRegen(); UpdateVisibility();
            TurnCompleted?.Invoke();
            return;
        }
        if (BiomeSystem.StepPoisonChance > 0 && Random.Shared.Next(100) < BiomeSystem.StepPoisonChance
            && _poisonTurnsLeft <= 0)
        {
            _poisonTurnsLeft = 3;
            _poisonDamagePerTick = 1 + CurrentFloor / 10;
            _log.LogCombat("The swamp's toxins seep into your wounds! Poisoned!");
        }

        if (HandleTallGrassAmbush(tile, tx, ty)) return;
        if (HandleTrapEffects(tile, tx, ty)) return;
        if (HandleTileInteraction(tile, tx, ty)) return;

        if (FlavorText.TerrainFlavors.TryGetValue(tile.Type, out var flavors)
            && Random.Shared.Next(100) < FlavorText.FootstepChance)
            _log.Log(flavors[Random.Shared.Next(flavors.Length)]);

        HandleTrapDetection(tx, ty);

        if (Random.Shared.Next(100) < FlavorText.AmbientChance)
            _log.Log(FlavorText.AmbientMessages[Random.Shared.Next(FlavorText.AmbientMessages.Length)]);

        CheckStairsDiscovery(tx, ty);

        if (tile.Type == TileType.LabyrinthEntrance)
        {
            if (!_inLabyrinth) TutorialSystem.ShowTip(_log, "first_labyrinth");
            if (_inLabyrinth) ExitLabyrinth();
            else EnterLabyrinth();
            return;
        }

        if (tile.Type == TileType.StairsUp)
        {
            TutorialSystem.ShowTip(_log, "first_stairs");
            bool bossAlive = _map.Bosses.Any(b => !b.IsDefeated);
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

        if (UserSettings.Current.AutoPickup)
        {
            var stepTile = _map.GetTile(_player.X, _player.Y);
            if (stepTile.HasItems) PickupItems();
        }

        TurnCount++;
        _restCounter++;
        TickExhaustion();
        if (PlayerLowHp && TurnCount % 5 == 0)
            _log.LogCombat(FlavorText.LowHpEncouragements[Random.Shared.Next(FlavorText.LowHpEncouragements.Length)]);
        TickPoison(); TickBleed(); TickSlow();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        PassiveRegen();
        UpdateVisibility();
        RevealNearbyTraps();
        QuestSystem.OnExplorationUpdate(_map.GetExplorationPercent(), _log);
        CheckFloorCompletion();
        CheckSoundCues();
        TurnCompleted?.Invoke();
    }

    // Extra Skill: Search — scans a 3-tile radius around the player and unhides
    // any trap tiles. Only active when UniqueSkill.ExtraSearch is unlocked.
    private void RevealNearbyTraps()
    {
        if (!Skills.UniqueSkillSystem.Has(Skills.UniqueSkill.ExtraSearch)) return;
        int r = Skills.UniqueSkillSystem.SearchRadius();
        int px = _player.X, py = _player.Y;
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int tx = px + dx, ty = py + dy;
            if (!_map.InBounds(tx, ty)) continue;
            var tile = _map.GetTile(tx, ty);
            if (!tile.TrapHidden) continue;
            if (tile.Type is not (TileType.TrapSpike or TileType.TrapTeleport
                or TileType.TrapPoison or TileType.TrapAlarm)) continue;
            tile.TrapHidden = false;
            _log.Log($"Your trained eye spots a {TrapLabel(tile.Type)} nearby.");
        }
    }

    // Returns true if Ran the Brawler handled the quest flow (so no generic
    // random-quest offer is layered on top of his canonical trial).
    private bool HandleRanTheBrawler(Entities.NPC npc)
    {
        if (npc.Name != "Ran the Brawler") return false;
        const string QuestId = "progressive_martial_arts";

        if (Skills.UniqueSkillSystem.Has(Skills.UniqueSkill.MartialArts))
        {
            _log.Log($"{npc.Name}: \"Your body remembers the lesson. Strike well.\"");
            return true;
        }

        var existing = QuestSystem.GetQuest(QuestId);
        if (existing == null)
        {
            QuestSystem.ActiveQuests.Add(new Quest
            {
                Id = QuestId,
                Title = "Ran's Trial",
                Description = "Defeat 5 beasts on this floor using only your bare fists.",
                GiverName = npc.Name,
                Floor = CurrentFloor,
                Type = QuestType.Kill,
                TargetMob = "",
                TargetCount = 5,
                RequiresWeaponType = "Unarmed",
                Persistent = true,
                RewardCol = 200,
                RewardXp = 150,
            });
            _log.LogSystem($"  [QUEST] New quest from {npc.Name}: 'Ran's Trial' — 5 unarmed kills.");
            return true;
        }

        if (existing.Status == QuestStatus.Complete)
        {
            _log.LogSystem($"{npc.Name}: \"You passed. Now feel what your body is capable of.\"");
            Skills.UniqueSkillSystem.TryUnlock(Skills.UniqueSkill.MartialArts);
            NotifyUniqueSkillUnlock(Skills.UniqueSkill.MartialArts);
            existing.Status = QuestStatus.TurnedIn;
            _player.ColOnHand += existing.RewardCol;
            TotalColEarned += existing.RewardCol;
            _player.GainExperience(existing.RewardXp);
            QuestSystem.ActiveQuests.Remove(existing);
            QuestSystem.CompletedQuests.Add(existing);
            return true;
        }

        _log.Log($"{npc.Name}: \"Not yet done. {existing.TargetCount - existing.CurrentCount} more with bare hands.\"");
        return true;
    }

    private static string TrapLabel(TileType t) => t switch
    {
        TileType.TrapSpike    => "spike trap",
        TileType.TrapTeleport => "teleport trap",
        TileType.TrapPoison   => "poison trap",
        TileType.TrapAlarm    => "alarm wire",
        _                         => "trap",
    };

    private void HandleOccupantInteraction(Map.Tile tile, int tx, int ty)
    {
        var occupant = tile.Occupant;

        // Bump into an ally: swap positions (SAO Switch).
        if (occupant is Ally ally)
        {
            _map.MoveEntity(ally, _player.X, _player.Y);
            _map.MoveEntity(_player, tx, ty);
            _log.LogCombat($"Switch! You swap positions with {ally.Name}.");
            return;
        }

        if (occupant is Monster monster && !monster.IsDefeated)
        {
            int hpBefore = _player.CurrentHealth;
            HandleCombat(monster, hpBefore);
            AdvanceTurn();
            TickPoison(); TickBleed(); TickSlow();
            if (_player.IsDefeated) return;
            ProcessEntityTurns();
            PassiveRegen();
            TurnCompleted?.Invoke();
            return;
        }

        if (occupant is Vendor vendor)
        {
            TutorialSystem.ShowTip(_log, "first_vendor");
            string greeting = string.Format(
                FlavorText.VendorGreetings[Random.Shared.Next(FlavorText.VendorGreetings.Length)],
                vendor.ShopName ?? "my shop");
            _log.Log($"{vendor.Name}: \"{greeting}\"");
            if (vendor.ShopStock.Count > 0)
            {
                int minPrice = vendor.ShopStock.Min(i => i.Value);
                int maxPrice = vendor.ShopStock.Max(i => i.Value);
                _log.Log($"  {vendor.ShopStock.Count} items available ({minPrice}–{maxPrice} Col)");
            }
            VendorInteraction?.Invoke(vendor);
            return;
        }

        if (occupant is NPC npc && occupant is not Ally)
        {
            TutorialSystem.ShowTip(_log, "first_npc_talk");

            // Ran the Brawler: Martial Arts trial (Progressive canon F2 quest).
            bool handledByRan = HandleRanTheBrawler(npc);

            // Turn in completed quests
            QuestSystem.OnNpcTalk(_log);
            var (qCol, qXp) = QuestSystem.TurnInCompleted();
            if (qCol > 0 || qXp > 0)
            {
                _player.ColOnHand += qCol;
                TotalColEarned += qCol;
                int lvlBefore = _player.Level;
                _player.GainExperience(qXp);
                _log.LogSystem($"  [QUEST] Rewards: +{qCol} Col, +{qXp} XP!");
                if (_player.Level > lvlBefore) LeveledUp?.Invoke();
            }

            // Offer a new quest if the player has room — Ran never gives random quests.
            if (!handledByRan
                && QuestSystem.ActiveQuests.Count < QuestSystem.MaxActiveQuests
                && npc.CanInteract && Random.Shared.Next(3) == 0)
            {
                var quest = QuestSystem.GenerateQuest(CurrentFloor, npc.Name);
                QuestSystem.ActiveQuests.Add(quest);
                _log.LogSystem($"  [QUEST] New quest from {npc.Name}: '{quest.Title}'");
                _log.Log($"  {quest.Description}");
                _log.Log($"  Reward: {quest.RewardCol} Col, {quest.RewardXp} XP");
            }

            if (npc.DialogueLines != null && npc.DialogueLines.Length > 0)
                NpcDialogRequested?.Invoke(npc);
            else
            {
                string dialogue = npc.Dialogue
                    ?? FlavorText.NpcFallbackDialogue[Random.Shared.Next(FlavorText.NpcFallbackDialogue.Length)];
                _log.Log($"{npc.Name}: \"{dialogue}\"");
            }

            // Offer recruitment for named SAO characters.
            TryRecruitNpc(npc);

            // Push the NPC aside so the player can pass through.
            PushNpcAside(npc, tx, ty);
        }
    }

    private void CheckStairsDiscovery(int tx, int ty)
    {
        if (_stairsDiscovered) return;
        for (int sdx = -1; sdx <= 1; sdx++)
        for (int sdy = -1; sdy <= 1; sdy++)
        {
            if (_map.InBounds(tx + sdx, ty + sdy)
                && _map.GetTile(tx + sdx, ty + sdy).Type == TileType.StairsUp)
            {
                _stairsDiscovered = true;
                _log.LogSystem(FlavorText.StairsDiscoveryMessages[Random.Shared.Next(FlavorText.StairsDiscoveryMessages.Length)]);
                return;
            }
        }
    }

    // Recruitble NPCs: named SAO characters that can join the party.
    private static readonly Dictionary<string, (char Sym, Terminal.Gui.Color Col, string Wpn, string Title)> RecruitableNpcs = new()
    {
        { "Klein",          ('K', Terminal.Gui.Color.BrightRed,     "Katana",            "Samurai") },
        { "Asuna",          ('A', Terminal.Gui.Color.BrightYellow,  "Rapier",            "The Flash") },
        { "Agil",           ('G', Terminal.Gui.Color.BrightGreen,   "Axe",               "Axe Fighter") },
        { "Silica",         ('S', Terminal.Gui.Color.BrightCyan,    "Dagger",            "Dragon Tamer") },
        { "Lisbeth",        ('L', Terminal.Gui.Color.BrightMagenta, "Mace",              "Blacksmith") },
    };

    private void TryRecruitNpc(NPC npc)
    {
        if (!RecruitableNpcs.TryGetValue(npc.Name, out var info)) return;
        if (PartySystem.Members.Any(a => a.Name == npc.Name)) return;
        if (PartySystem.Members.Count >= PartySystem.MaxPartySize) return;
        // FB-564 Solo modifier — no recruits allowed.
        if (RunModifiers.IsActive(RunModifier.Solo))
        {
            _log.Log($"{npc.Name} offers to join you, but Solo modifier forbids company.");
            return;
        }

        int choice = Terminal.Gui.MessageBox.Query(
            $"Recruit {npc.Name}?",
            $"{npc.Name} ({info.Title}) wants to join your party!\n" +
            $"Weapon: {info.Wpn}  |  Party: {PartySystem.Members.Count}/{PartySystem.MaxPartySize}",
            "Welcome aboard!", "Not now");

        if (choice != 0) return;

        PartySystem.TryRecruit(npc.Name, info.Sym, info.Col, info.Wpn, info.Title, _player.Level, _log);

        // Place the ally adjacent to the player and remove the NPC.
        var ally = PartySystem.Members.LastOrDefault();
        if (ally != null)
        {
            _map.RemoveEntity(npc);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = _player.X + dx, ny = _player.Y + dy;
                if (_map.InBounds(nx, ny) && !_map.GetTile(nx, ny).BlocksMovement
                    && _map.GetTile(nx, ny).Occupant == null)
                {
                    _map.PlaceEntity(ally, nx, ny);
                    return;
                }
            }
        }
    }

    private void PushNpcAside(NPC npc, int playerTargetX, int playerTargetY)
    {
        // Try to move the NPC to a walkable tile adjacent to its current position,
        // preferring the direction away from the player.
        int awayX = Math.Sign(npc.X - _player.X);
        int awayY = Math.Sign(npc.Y - _player.Y);

        // Ordered candidates: away direction first, then perpendiculars, then others.
        Span<(int dx, int dy)> dirs = stackalloc (int, int)[]
        {
            (awayX, awayY), (awayX, 0), (0, awayY),
            (-awayY, awayX), (awayY, -awayX), // perpendiculars
            (-awayX, 0), (0, -awayY), (-awayX, -awayY),
        };

        foreach (var (dx, dy) in dirs)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = npc.X + dx, ny = npc.Y + dy;
            if (!_map.InBounds(nx, ny)) continue;
            var t = _map.GetTile(nx, ny);
            if (t.BlocksMovement || t.Occupant != null) continue;
            _map.MoveEntity(npc, nx, ny);
            return;
        }
        // If completely boxed in, NPC stays put — rare edge case.
    }

    private void CheckSoundCues()
    {
        if (TurnCount - _lastSoundCueTurn < SoundCueCooldown) return;

        Monster? closest = null;
        int closestDist = int.MaxValue;
        foreach (var entity in _map.Entities)
        {
            if (entity == _player || entity.IsDefeated || entity is not Monster monster) continue;
            if (_map.IsVisible(monster.X, monster.Y)) continue;
            int dist = Math.Max(Math.Abs(monster.X - _player.X), Math.Abs(monster.Y - _player.Y));
            int aggro = monster is Mob mob ? mob.AggroRange : 6;
            if (dist <= aggro + 2 && dist < closestDist) { closest = monster; closestDist = dist; }
        }
        if (closest == null) return;

        string tag = closest is Mob m2 ? m2.LootTag : "generic";
        string[] cues = FlavorText.SoundCues.GetValueOrDefault(tag, FlavorText.GenericSoundCues)!;
        _log.Log(cues[Random.Shared.Next(cues.Length)]);
        _lastSoundCueTurn = TurnCount;
    }
}
