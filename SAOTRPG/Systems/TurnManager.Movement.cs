using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Map;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Player movement: rest, sprint, stealth, normal moves, occupant interactions,
// stairs discovery, sound cues.
public partial class TurnManager
{
    public void ProcessRest()
    {
        if (_player.IsDefeated) return;

        // Resting is the ONLY way to clear fatigue (_restCounter is zeroed below), so refusing it
        // at full health left an exhausted player stuck with -4 ATK and -1 DEF and no way out
        // short of taking a hit first. Full health only blocks a rest that would do nothing at all.
        bool healingNeeded = _player.CurrentHealth < _player.MaxHealth;
        bool fatigueToClear = _restCounter >= FatigueThreshold;
        if (!healingNeeded && !fatigueToClear)
        { _log.Log("Already at full health and well rested. Press Space to wait a turn instead."); return; }

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
            TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
            if (_player.IsDefeated) return;
            ProcessEntityTurns();
            if (_player.IsDefeated) return;
        }
        _restCounter = 0;
        _fatiguedWarned = false;
        _exhaustedWarned = false;
        // Sleep XP: one grant per rest action (not per heal tick).
        GrantRestSleepXp();
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
        // Running skill XP per sprint action (covers 2 tiles).
        GrantSprintRunningXp();
        TurnCount++;
        TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        if (_player.IsDefeated) return;
        // Sprinting is two tiles of movement and used to skip passive regen entirely, so a player
        // who sprinted everywhere healed less than one who walked the same ground.
        PassiveRegen();
        UpdateVisibility();
        TurnCompleted?.Invoke();
    }

    public void EnterCounterStance()
    {
        if (_player.IsDefeated) return;
        _counterStance = true;
        _log.LogCombat("You raise your guard — ready to counter the next attack!");
        AdvanceTurn();
        TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
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
        using var _profileScope = Profiler.Begin("TurnManager.ProcessPlayerMove");
        if (_player.IsDefeated) return;

        if (_stunTurnsLeft > 0)
        {
            _log.LogCombat("You are stunned and cannot act!");
            AdvanceTurn();
            TickStun(); TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
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
                _log.Log(FlavorText.IdleFlavors[RunRng.Next(FlavorText.IdleFlavors.Length)]);
        }
        else _idleTurns = 0;

        int tx = _player.X + dx, ty = _player.Y + dy;
        var tile = _map.GetTile(tx, ty);

        TutorialSystem.ShowTip(_log, "first_move");

        // Swimming gate: level check bypasses water BlocksMovement.
        bool swimmingBypass = false;
        bool swimSlowPenalty = false;
        if (tile.RequiresSwimmingLevel > 0)
        {
            int swimLvl = _player.LifeSkills.SwimmingLevel;
            int req = tile.RequiresSwimmingLevel;
            if (swimLvl >= req)
            {
                swimmingBypass = true;
                // Slow below L10 (shallow) / L50 (deep); each slow step = extra tick.
                if (tile.Type == TileType.Water && swimLvl < 10) swimSlowPenalty = true;
                else if (tile.Type == TileType.WaterDeep && swimLvl < 50) swimSlowPenalty = true;
            }
        }

        // Mining bump-action diverts before the BlocksMovement gate.
        // Returns true when the action consumed the turn (or hint logged).
        if (TryHandleMiningStrike(tx, ty)) return;

        if (tile.BlocksMovement && !swimmingBypass)
        {
            TutorialSystem.ShowTip(_log, "first_wall_bump");
            if (tile.Type == TileType.CrackedWall)
            {
                _map.SetTileType(tx, ty, TileType.Floor);
                _log.LogLoot("You smash through the cracked wall! A hidden chamber lies beyond!");
                CombatTextEvent?.Invoke(tx, ty, "SECRET!", Color.BrightYellow);
                return;
            }
            // Swim-blocked (too low Swimming skill) — give the player a hint.
            if (tile.RequiresSwimmingLevel > 0)
            {
                string kind = tile.Type == TileType.WaterDeep ? "deep water" : "water";
                _log.Log($"The {kind} is too much for your swimming skill (need L{tile.RequiresSwimmingLevel}).");
                return;
            }
            if (RunRng.Next(100) < 3)
                _log.Log(FlavorText.WallBumpFlavors[RunRng.Next(FlavorText.WallBumpFlavors.Length)]);
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
        // Swim XP: +2 shallow / +3 deep. Replaces walking XP.
        if (swimmingBypass)
        {
            int swimXp = tile.Type == TileType.WaterDeep ? 3 : 2;
            _player.LifeSkills.GrantXp(LifeSkillType.Swimming, swimXp);
            if (swimSlowPenalty)
                _log.Log("You struggle through the water…");
        }
        // Walking XP +1/tile (excludes sprint, stealth, water).
        else if (!_stealthActive && !_lastMoveWasStealth && (dx != 0 || dy != 0))
            GrantWalkingXp();

        // Tip when first leaving safe zone on Floor 1
        if (_map.SafeZone.HasValue && !_map.SafeZone.Value.Contains(tx, ty))
            TutorialSystem.ShowTip(_log, "floor1_exit_town");

        // Biome movement effects: ice slip, swamp poison.
        if (BiomeSystem.SlipChance > 0 && RunRng.Next(100) < BiomeSystem.SlipChance)
        {
            _log.Log("You slip on the icy surface!");
            // Slip = lose the rest of this turn (no further processing).
            TurnCount++; TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
            if (_player.IsDefeated) return;
            ProcessEntityTurns(); PassiveRegen(); UpdateVisibility();
            TurnCompleted?.Invoke();
            return;
        }
        if (BiomeSystem.StepPoisonChance > 0 && RunRng.Next(100) < BiomeSystem.StepPoisonChance
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
            && RunRng.Next(100) < FlavorText.FootstepChance)
            _log.Log(flavors[RunRng.Next(flavors.Length)]);

        HandleTrapDetection(tx, ty);

        if (RunRng.Next(100) < FlavorText.AmbientChance)
            _log.Log(FlavorText.AmbientMessages[RunRng.Next(FlavorText.AmbientMessages.Length)]);

        CheckStairsDiscovery(tx, ty);

        if (tile.Type == TileType.LabyrinthEntrance)
        {
            // Leaving is never gated; entering is. The overworld floor boss sits away from the
            // archway, and a boss-arena prefab can stamp an entrance anywhere — including onto the
            // spawn tile — so without this a floor could be entered from the square the player
            // arrives on, skipping it entirely. Floor 1 places no overworld floor boss, so the gate
            // is deliberately inert there and the tutorial floor stays open.
            // SAFETY: this makes the boss's reachability load-bearing. Tools/SeedProbe asserts it.
            if (_inLabyrinth) { ExitLabyrinth(); return; }
            TutorialSystem.ShowTip(_log, "first_labyrinth");
            if (FloorBossAlive())
            {
                _log.LogCombat("  The way up is sealed. Defeat the Floor Boss first!");
                TurnCompleted?.Invoke();
                return;
            }
            EnterLabyrinth();
            return;
        }

        if (tile.Type == TileType.StairsUp)
        {
            TutorialSystem.ShowTip(_log, "first_stairs");
            bool bossAlive = FloorBossAlive();
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
            if (_map.HasItemsAt(_player.X, _player.Y)) PickupItems();
        }

        TurnCount++;
        _restCounter++;
        TickExhaustion();
        if (PlayerLowHp && TurnCount % 5 == 0)
            _log.LogCombat(FlavorText.LowHpEncouragements[RunRng.Next(FlavorText.LowHpEncouragements.Length)]);
        TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
        if (_player.IsDefeated) return;
        ProcessEntityTurns();
        // Swim slow penalty: extra tick + entity round (mobs get free turn).
        if (swimSlowPenalty && !_player.IsDefeated)
        {
            TurnCount++;
            TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
            if (!_player.IsDefeated) ProcessEntityTurns();
        }
        PassiveRegen();
        UpdateVisibility();
        RevealNearbyTraps();
        QuestSystem.OnExplorationUpdate(_map.GetExplorationPercent(), _log);
        CheckFloorCompletion();
        CheckSoundCues();
        TurnCompleted?.Invoke();
    }

    // Extra Skill: Search — unhides traps in r-tile radius (ExtraSearch unlocked).
    // Logs only on first reveal per floor to avoid corridor-sweep spam.
    private void RevealNearbyTraps()
    {
        if (!Skills.UniqueSkillSystem.Has(Skills.UniqueSkill.ExtraSearch)) return;
        int r = Skills.UniqueSkillSystem.SearchRadius();
        int px = _player.X, py = _player.Y;
        bool revealedAny = false, scoutedAny = false;
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int tx = px + dx, ty = py + dy;
            if (!_map.InBounds(tx, ty)) continue;
            var tile = _map.GetTile(tx, ty);
            // Chests are not hidden, so scouting one is not a reveal — it is a closer look
            // that pays off when the chest is opened. Marked here so the skill does something
            // visible on floors that happen to carry no traps at all.
            if (tile.Type == TileType.Chest && _scoutedChests.Add((tx, ty)))
            {
                scoutedAny = true;
                continue;
            }
            if (!tile.TrapHidden) continue;
            if (tile.Type is not (TileType.TrapSpike or TileType.TrapTeleport
                or TileType.TrapPoison or TileType.TrapAlarm
                or TileType.TrapWeb or TileType.TrapMagnet or TileType.TrapRune)) continue;
            _map.SetTrapHidden(tx, ty, false);
            revealedAny = true;
        }
        if (revealedAny && !_extraSearchRevealedThisFloor)
        {
            _extraSearchRevealedThisFloor = true;
            _log.LogSystem("Your trained eye spots a hidden trap nearby!");
        }
        if (scoutedAny && !_extraSearchScoutedThisFloor)
        {
            _extraSearchScoutedThisFloor = true;
            _log.LogSystem("You size up a nearby chest — you know where the good compartments are.");
        }
    }

    // Returns true if the Brawler handled the quest flow (skip generic random-quest).
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
            QuestSystem.AddQuest(new Quest
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
            // Guard against double-banner: 30-unarmed-kill milestone may have unlocked
            // first; TryUnlock returns false and suppresses the banner.
            if (Skills.UniqueSkillSystem.TryUnlock(Skills.UniqueSkill.MartialArts))
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

    // Shared Divine-Object quest handler (Azariya F50, Selka F65, Dorothy F78, HF NPCs).
    // First-talk offers floor-kill quest; complete grants Divine; TurnedIn prevents re-grant.
    // subCategory tags the quest for milestone counters; defaults None for callers that
    // don't feed IF/HF trackers.
    private bool HandleDivineQuest(Entities.NPC npc, string questId, string questTitle,
        string openingLine, int killCount, string divineDefId, string handOverLine,
        string inProgressLine, string postCompleteLine, int rewardCol, int rewardXp,
        QuestSubCategory subCategory = QuestSubCategory.None)
    {
        if (npc.Name == null) return false;

        var existing = QuestSystem.GetQuest(questId);

        // First talk — offer the trial.
        if (existing == null)
        {
            QuestSystem.AddQuest(new Quest
            {
                Id = questId,
                Title = questTitle,
                Description = $"Defeat {killCount} monsters on Floor {CurrentFloor}.",
                GiverName = npc.Name,
                Floor = CurrentFloor,
                Type = QuestType.Kill,
                TargetMob = "",
                TargetCount = killCount,
                Persistent = true,
                RewardCol = rewardCol,
                RewardXp = rewardXp,
                SubCategory = subCategory,
            });
            _log.Log($"{npc.Name}: \"{openingLine}\"");
            _log.LogSystem($"  [QUEST] New quest from {npc.Name}: '{questTitle}' — {killCount} kills on this floor.");
            return true;
        }

        // Already turned in — just flavor, no re-grant.
        if (existing.Status == QuestStatus.TurnedIn)
        {
            _log.Log($"{npc.Name}: \"{postCompleteLine}\"");
            return true;
        }

        // Quest complete — grant Divine and mark turned in.
        if (existing.Status == QuestStatus.Complete)
        {
            _log.Log($"{npc.Name}: \"{handOverLine}\"");
            existing.Status = QuestStatus.TurnedIn;
            _player.ColOnHand += existing.RewardCol;
            TotalColEarned += existing.RewardCol;
            _player.GainExperience(existing.RewardXp);
            QuestSystem.ActiveQuests.Remove(existing);
            QuestSystem.CompletedQuests.Add(existing);

            var divine = Items.ItemRegistry.Create(divineDefId);
            if (divine != null)
            {
                if (_player.Inventory.AddItem(divine))
                    _log.LogLoot($"  ◈ You receive {divine.Name} — Divine Object.");
                else
                {
                    _map.AddItem(_player.X, _player.Y, divine);
                    _log.LogLoot($"  ◈ {divine.Name} — Divine Object. (Inventory full — dropped at your feet.)");
                }
                // Fire DivineObtained event + set one-per-run cap when a Divine was granted.
                if (divine is Items.Equipment.Weapon divineWpn && divine.Rarity == "Divine")
                    NotifyDivineObtained(divineWpn);
            }
            return true;
        }

        // In progress.
        int remaining = existing.TargetCount - existing.CurrentCount;
        _log.Log($"{npc.Name}: \"{inProgressLine} ({remaining} more.)\"");
        return true;
    }

    // Sister Azariya — F50 Heaven-Piercing Blade giver.
    private bool HandleSisterAzariya(Entities.NPC npc)
    {
        if (npc.Name != "Sister Azariya") return false;
        return HandleDivineQuest(npc,
            questId:          "divine_heaven_piercing",
            questTitle:       "Light at the Edge of Sight",
            openingLine:      "Twenty shadows fall on this floor before the light finds its wielder. Go.",
            killCount:        20,
            divineDefId:      "heaven_piercing_blade",
            handOverLine:     "The light recognises you. Take it — and pierce what comes next.",
            inProgressLine:   "Your work is not yet done.",
            postCompleteLine: "Let its beam cut the dark for you.",
            rewardCol:        500,
            rewardXp:         400);
    }

    // Dorothy — F78 Starlight Banner (8th Divine). SAO Last Recollection canon.
    private bool HandleDorothy(Entities.NPC npc)
    {
        if (npc.Name != "Dorothy") return false;
        return HandleDivineQuest(npc,
            questId:          "dorothy_starlight_banner",
            questTitle:       "Purify the Darkness",
            openingLine:      "Twenty-two shadows walk this floor. Cut them down, and the banner of starlight is yours.",
            killCount:        22,
            divineDefId:      "scy_starlight_banner",
            handOverLine:     "The banner answers. Carry it — every swing you make now is a prayer against the dark.",
            inProgressLine:   "The dark is thicker than your count suggests.",
            postCompleteLine: "The banner walks with you. There is nothing more I can give.",
            rewardCol:        700,
            rewardXp:         550);
    }

    // Scholar Vesper — F89 Satanachia (Scimitar Divine). Goetia grimoire theme.
    private bool HandleScholarVesper(Entities.NPC npc)
    {
        if (npc.Name != "Scholar Vesper") return false;
        return HandleDivineQuest(npc,
            questId:          "divine_satanachia",
            questTitle:       "The Goetia's Seal",
            openingLine:      "Twenty-four wards fall before the seal unbinds. Break them, and the grimoire's blade is yours.",
            killCount:        24,
            divineDefId:      "satanachia",
            handOverLine:     "The seal breaks. Take Satanachia — let its edge answer what the wards could not.",
            inProgressLine:   "The seal holds. More must fall.",
            postCompleteLine: "The grimoire is silent. Its page has turned to you.",
            rewardCol:        800,
            rewardXp:         600);
    }

    // Selka awakening hook — short-circuit OR before HandleSelka. Opens dialog when base quest TurnedIn,
    // chain quest isn't mid-turn-in, and Divine is carried; otherwise returns false to fall through untouched.
    private bool HandleSelkaAwakening(Entities.NPC npc)
    {
        if (npc.Name != "Selka the Novice") return false;

        // Guard 1: base quest must be fully turned in.
        var baseQ = QuestSystem.GetQuest("divine_fragrant_olive");
        if (baseQ == null || baseQ.Status != QuestStatus.TurnedIn) return false;

        // Guard 2 (gotcha #4): chained "Sword's Awakening" must be null or TurnedIn. If InProgress/Complete,
        // HandleSelka owns the dispatch so the player can turn it in.
        var chainQ = QuestSystem.GetQuest("selka_unfolding_truth");
        if (chainQ != null && chainQ.Status != QuestStatus.TurnedIn) return false;

        // Guard 3: must carry a Divine weapon to be offered awakening.
        if (!HasDivineWeapon(_player.Inventory))
        {
            _log.Log("Selka: \"Return when you carry a blade worthy of awakening.\"");
            return false;
        }

        _log.Log("Selka: \"I can hear your blade's song. Let me help it awaken.\"");
        DivineAwakeningRequested?.Invoke(_player);
        return true;
    }

    // Returns true if any weapon in inventory OR equipped slots is Divine rarity.
    // Equip removes from Items into equipped-slot map — must scan both.
    private static bool HasDivineWeapon(Inventory.Core.Inventory inv)
    {
        foreach (var item in inv.Items)
            if (item is Items.Equipment.Weapon w && w.Rarity == "Divine") return true;
        foreach (Inventory.Core.EquipmentSlot s in Enum.GetValues(typeof(Inventory.Core.EquipmentSlot)))
            if (inv.GetEquipped(s) is Items.Equipment.Weapon we && we.Rarity == "Divine") return true;
        return false;
    }

    // Selka — F65 Fragrant Olive + chained "Unfolding Truth" (30 kills F65+).
    // GetQuest reads completed list so legacy TurnedIn saves still trigger the chain.
    private bool HandleSelka(Entities.NPC npc)
    {
        if (npc.Name != "Selka the Novice") return false;

        var baseQuest = QuestSystem.GetQuest("divine_fragrant_olive");
        // Base quest pending — first-quest flow (covers null + in-progress).
        if (baseQuest == null || baseQuest.Status != QuestStatus.TurnedIn)
        {
            return HandleDivineQuest(npc,
                questId:          "divine_fragrant_olive",
                questTitle:       "The Last Knight's Bequest",
                openingLine:      "Twenty-five monsters on this floor. Prove my sister's memory is safe with you.",
                killCount:        25,
                divineDefId:      "fragrant_olive_sword",
                handOverLine:     "Alice's blade answers to you now. Carry it well — let the petals remember her.",
                inProgressLine:   "My sister would want to see more resolve from you.",
                postCompleteLine: "Her blade is yours. Walk in the light she left behind.",
                rewardCol:        500,
                rewardXp:         400);
        }

        // Base quest has been turned in — offer the chained Awakening quest.
        return HandleDivineQuest(npc,
            questId:          "selka_unfolding_truth",
            questTitle:       "The Sword's Awakening",
            openingLine:      "The blade has been restless since you took it. It wants more than a name — it wants the truth of its wielder. Thirty more on this floor, and we will see it unfold.",
            killCount:        30,
            divineDefId:      "ohs_unfolding_truth_fragrant_olive",
            handOverLine:     "There — the petals have opened. The unfolding truth is yours now. Carry it farther than my sister could.",
            inProgressLine:   "The blade still sleeps. Keep going.",
            postCompleteLine: "The unfolding is done. Alice's light walks with you now — there is nothing more I can give.",
            rewardCol:        800,
            rewardXp:         600);
    }

    // ── Hollow Fragment Hollow Mission questgivers (9 HNM weapons) ────
    // HF Legendary weapons gated behind NPC quests. See HollowWeaponQuest.
    private record HollowWeaponQuest(string QuestId, string Title, string Opening,
        int KillCount, string RewardDefId, string HandOver, string InProgress,
        string PostComplete, int Col, int Xp,
        QuestSubCategory SubCategory = QuestSubCategory.None);

    private static readonly Dictionary<string, HollowWeaponQuest> _hollowWeaponQuests = new()
    {
        ["Scholar Ellroy"] = new("hf_infinite_ouroboros", "The Endless Coil",
            "Fifteen serpent-beasts must fall below. When they do, the coil is yours.",
            15, "infinite_ouroboros",
            "You have broken the coil. Take it — let it remember you.",
            "The coils still turn.", "The Ouroboros answers to you now.",
            400, 300, QuestSubCategory.HfMission),
        ["Hunter Kojiro"] = new("hf_jato_onikirimaru", "The Oni Cutter's Trial",
            "Fifteen kills. Any mob, any make. Then the blade of oni-splitting is yours.",
            15, "jato_onikirimaru",
            "Onikiri-maru has waited long enough. Swing it well.",
            "Fifteen. No fewer.", "The blade remembers every cut now.",
            400, 300, QuestSubCategory.HfMission),
        ["Ranger Torva"] = new("hf_fiendblade_deathbringer", "Thinning the Grove",
            "The grove dies in cycles. Fell fifteen and the blade it buried is yours.",
            15, "fiendblade_deathbringer",
            "The grove gives up its secret. Drink deep — and quickly.",
            "The grove still breathes.", "Deathbringer bleeds in your hand.",
            450, 320, QuestSubCategory.HfMission),
        ["Apiarist Nell"] = new("hf_fayblade_tizona", "The Hornet's Undoing",
            "Fifteen fall, and the fay-blade remembers its owner.",
            15, "fayblade_tizona",
            "Tizona is yours. Let it sing against the wing.",
            "The hornets still hum.", "Fay-steel. Move quickly with it.",
            450, 320, QuestSubCategory.HfMission),
        ["Watcher Kael"] = new("hf_starmace_elysium", "The Shining Swarm",
            "Twenty of the shining ones. Elysium crowns the steady hand.",
            20, "starmace_elysium",
            "Elysium answers. You will not be moved again.",
            "The swarm returns each turn.", "Elysium stands with you.",
            600, 450, QuestSubCategory.HfMission),
        ["High Priestess Sola"] = new("hf_eurynomes_holy_sword", "The Holy Trial",
            "Twenty of the fallen. Then and only then does the holy sword judge you.",
            20, "eurynomes_holy_sword",
            "Eurynome's blessing is yours. The blade obeys the worthy alone.",
            "The trial is not complete.", "Walk in the light, champion.",
            650, 480, QuestSubCategory.HfMission),
        ["Torchbearer Meir"] = new("hf_saintspear_rhongomyniad", "The Dark Lanterns",
            "Twenty lanterns must be broken. The saint's spear judges the rest.",
            20, "saintspear_rhongomyniad",
            "Rhongomyniad lights again. Hold it high.",
            "The dark still pools.", "The spear carries your will now.",
            700, 520, QuestSubCategory.HfMission),
        ["Elder Beastkeeper"] = new("hf_shinto_ama_no_murakumo", "The Restless Herd",
            "Twenty-five. Still them, and the cloud-splitter is yours.",
            25, "shinto_ama_no_murakumo",
            "Ama-no-Murakumo answers the quiet hand. Carry it so.",
            "My charges still rage.", "The cloud parts for you.",
            800, 600, QuestSubCategory.HfMission),
        ["Sentinel Captain"] = new("hf_godspear_gungnir", "The Broken Line",
            "Twenty-five. Hold the line no one else held. Gungnir is its own reward.",
            25, "godspear_gungnir",
            "Gungnir returns to a worthy grip. Strike true.",
            "The line still bleeds.", "Odin's spear rests with you now.",
            800, 600, QuestSubCategory.HfMission),

        // ── HF Endgame Implement System questgivers (F84, F85, F92, F99) ──
        ["Spiralist Vey"] = new("hf_spiralblade_rendering_fail", "The Spiral That Fails",
            "Ten break the pattern. The rapier answers only the spiral that fails.",
            10, "rap_spiralblade_rendering_fail",
            "Rendering Fail is yours. It knows imperfect geometry now.",
            "The spiral still turns true.", "The rapier rests with you.",
            500, 360, QuestSubCategory.IfImplement),
        ["Crusher Drago"] = new("hf_crusher_bond_cyclone", "The Iron Cyclone",
            "Ten storms. Break them all, and the cyclone axe is yours.",
            10, "axe_crusher_bond_cyclone",
            "Bond Cyclone answers the steady haft. Heft it well.",
            "The storm still churns.", "The axe is yours, wielder.",
            550, 380, QuestSubCategory.IfImplement),
        ["Auric Knight Halric"] = new("hf_aurumbrand_hauteclaire", "The Golden Shroud",
            "Fifteen fall to prove the shroud will not shroud a coward. Go.",
            15, "ohs_aurumbrand_hauteclaire",
            "Hauteclaire's gold recognises you. Be as steady as its edge.",
            "The shroud waits on.", "Hauteclaire shines in your hand.",
            700, 500, QuestSubCategory.IfImplement),
        ["Last Herald Xiv"] = new("hf_deathglutton_epetamu", "The Last Hollow Glutton",
            "Twenty. The floor before the top. The blade that feeds on its wielder asks for proof.",
            20, "sci_deathglutton_epetamu",
            "Epetamu will feed. It asks only that you feed it well.",
            "The pact is not yet written.", "The hollow-blade answers no other now.",
            900, 700, QuestSubCategory.IfImplement),

        // FD F55 Agil's Apprentice — moves axe_ground_gorge off floor-banded pool
        // onto a dedicated canon source.
        ["Agil's Apprentice"] = new("fd_agils_apprentice_ground_gorge", "The Apprentice's Ground Gorge",
            "Fifteen felled on this floor, and Agil says I can let the axe go. Show me fifteen.",
            15, "axe_ground_gorge",
            "Fifteen it was. Ground Gorge is yours — treat it like it cleaves the earth, because it does.",
            "Not yet. Agil was clear — fifteen, no fewer.",
            "May its bite never dull.",
            500, 400),

        // LN F40 Yulier — KoB-era Asuna friend; canon Lambent Light gift.
        // Asuna's signature rapier returns to its KoB-era floor.
        ["Yulier"] = new("ln_yulier_lambent_light", "The Lightning Flash's Memory",
            "Asuna gave me her old rapier before the Knights took her. Ten on this floor and Lambent Light is yours — she would have wanted a wielder, not a relic.",
            10, "lambent_light",
            "Asuna's light is yours now. Be the flash she was — and faster, if you can.",
            "Asuna would not have settled. Neither will I.",
            "Carry her flash. There is nothing else of her left to give.",
            450, 350),

        // LN MR-arc F76 Jun — Sleeping Knights' tribute; Mother's Rosario handover.
        // Yuuki memorial floor F76.
        ["Jun"] = new("ln_jun_mothers_rosario", "The Sleeping Knights' Tribute",
            "Yuuki left her sword to whoever would carry her family's name forward. Fifteen on this floor — prove you have the heart for Mother's Rosario.",
            15, "mothers_rosario",
            "Yuuki would have liked you. Take her sword — and the eleven sword skills it remembers. Carry the Knights with you.",
            "Yuuki took on a hundred. Fifteen is not too many.",
            "The Sleeping Knights walk with you now. Yuuki rests easier.",
            700, 550),

        // ── IF Element Research questgivers (F5 Karluin ruins) ────
        ["Archivist Fuscan"] = new("if_research_karluin_dust", "Karluin Dust Survey",
            "The catacomb dust holds the Cathedral's old elements. Bring me proof of ten cleared chambers.",
            10, "ore_crust",
            "The dust is the same as the founders' notes. The element binds — your work is logged.",
            "The cathedral keeps its silence until the chambers are truly cleared.",
            "Karluin's elements are catalogued. You walk lighter for it.",
            220, 95, QuestSubCategory.IfImplement),
        ["Relic-Keeper Mirine"] = new("if_research_pitchblack_seals", "Pitch-Black Seal Inventory",
            "Twelve lesser seals hold the cathedral's lower nave. Break them and the cipher keeps for posterity.",
            12, "",
            "The cipher resolves. Element zero — Vacant. Filed for the next archivist who climbs.",
            "The seals still answer one another. More must fall.",
            "The naves are quiet. Vacant element confirmed.",
            240, 100, QuestSubCategory.IfImplement),
        ["Sage Pellan of Karluin"] = new("if_research_undying_strain", "The Undying Strain",
            "Eight of the deathless walk this floor. Their fall is the sample we cannot collect anywhere else.",
            8, "",
            "The strain is bottled. Karluin's contribution to the element registry is recorded.",
            "The strain endures while you stand reading.",
            "The undying gave their data freely. Carry the entry forward.",
            210, 90, QuestSubCategory.IfImplement),
        ["Cathedral Scribe Vela"] = new("if_research_colossus_echo", "Echo of the Vacant Colossus",
            "Ten of the colossus's lesser kin still echo through the catacombs. Quiet them and the great echo can be measured.",
            10, "ore_crust",
            "Fuscus's echo is on the page now. The Vacant element is tabulated.",
            "The echo persists between every cleared room.",
            "The Colossus is on the page. Karluin remembers.",
            260, 110, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (F10 wetlands / Kagachi domain) ────
        ["Shrine Maiden Suzaha"] = new("if_research_kagachi_tides", "The Tides of Kagachi",
            "The samurai-lord's marsh rises and falls with element-flow. Twelve quiet kills give us a clean reading.",
            12, "ore_flowing_water",
            "The flow stabilises. Water-element sample taken — you cleared the noise from the channel.",
            "The marsh churns. The reading drifts.",
            "Kagachi's tide stays quiet because you walked it.",
            260, 110, QuestSubCategory.IfImplement),
        ["Ronin-Scholar Imai"] = new("if_research_kagachi_ten_steel", "Ten Steel for Kagachi",
            "The shrine logs ten swords broken for every element binding. Make ten kills and the log fills itself.",
            10, "",
            "The log signs itself. The samurai-lord's element series is one bind closer to whole.",
            "Ten yet, no fewer.",
            "The log accepts you. The shrine remembers a wielder.",
            230, 95, QuestSubCategory.IfImplement),
        ["Element-Hermit Joze"] = new("if_research_marsh_glow", "The Marsh-Glow Index",
            "Eight glow-creatures lit the wetlands before Kagachi quieted them. I want eight returned to the index.",
            8, "ore_flowing_water",
            "The glow returns to the page. The hermit's index is closer to closed.",
            "The glow drifts. Eight is the count.",
            "The marsh glow is filed. Walk on.",
            220, 90, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (F14 dense forest, IF Integral Series anchor) ────
        ["Druid-Researcher Lael"] = new("if_research_integral_canopy", "Integral Canopy Survey",
            "The canopy holds the first IF element-binding. Twelve deep-wood kills and the binding renews.",
            12, "ore_wind_flower",
            "Integral element binds. Lael's grove is in your debt — and your weapon is in the registry.",
            "The canopy still shifts with each step.",
            "Integral binds quietly. The grove walks easier.",
            280, 120, QuestSubCategory.IfImplement),
        ["Hermit Cassis"] = new("if_research_canopy_fauna", "Canopy Fauna Census",
            "Ten of the canopy's strangest. The census-line balances or it does not.",
            10, "",
            "The census closes. The canopy element is fixed for one more cycle.",
            "Ten is the line. It is not yet drawn.",
            "The canopy gives its census. Move on.",
            240, 100, QuestSubCategory.IfImplement),
        ["Shrine-Naturalist Yorin"] = new("if_research_dense_grove_root", "Roots of the Dense Grove",
            "The grove's deep roots feed the integral element. Ten clearings prove the roots still remember.",
            10, "ore_wind_flower",
            "The roots are recorded. Integral series binds clean tonight.",
            "The roots go deeper than the count.",
            "The grove roots are filed. Walk lightly.",
            250, 105, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (F25 twilight forest, IF Nox Series anchor) ────
        ["Twilight-Sage Orune"] = new("if_research_nox_giant_shadow", "Shadow of the Two-Headed Giant",
            "The giant's shadow carries Nox-element residue. Twelve fall in the shadow and the residue fixes.",
            12, "",
            "Nox binds to the page. The two-headed shadow is yours to cite.",
            "The shadow shifts. The residue scatters.",
            "Nox is fixed. The twilight remembers a worker.",
            300, 130, QuestSubCategory.IfImplement),
        ["Elf-Blooded Scholar Liraen"] = new("if_research_nox_dusk_bloom", "Nox Dusk-Bloom Sampling",
            "The dusk-blooms grow only where Nox-element hangs heavy. Ten kills clear the canopy enough for a clean sample.",
            10, "ore_wind_flower",
            "The blooms are pressed and labelled. Nox sampling closes for the season.",
            "The blooms only open where the canopy is silent.",
            "Nox sampling is closed. The blooms thank you.",
            290, 125, QuestSubCategory.IfImplement),
        ["Twilight-Wright Kerel"] = new("if_research_nox_giant_pulse", "Nox Pulse Reading",
            "The giant's pulse runs the ridge twice an hour. Ten kills steady the line for the reading.",
            10, "",
            "The pulse line steadies. Nox readings file at last.",
            "The pulse swallows the count.",
            "The reading is clean. The ridge holds quiet.",
            280, 120, QuestSubCategory.IfImplement),
        ["Forest-Cleric Thol"] = new("if_research_nox_canopy_dread", "Canopy Dread Census",
            "Eight of the dread-walkers haunt this ridge. Their fall is the only census-line that closes.",
            8, "",
            "The census closes. Nox dread is on the page.",
            "Dread thickens between the trees.",
            "Dread is recorded. The ridge breathes.",
            260, 110, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (F61-65 infernal era, IF Rosso Series anchor) ────
        ["Pyromancer-Archivist Tassel"] = new("if_research_rosso_forneus_ash", "Ash of Crimson Forneus",
            "The fog-lake holds Forneus's ash. Twelve kills clear the haze enough to take a sample.",
            12, "ore_crimson_flame",
            "Rosso element binds. Forneus's ash is in the cipher now.",
            "The haze does not part for half a count.",
            "Rosso is bound. The lake quiets a little.",
            320, 140, QuestSubCategory.IfImplement),
        ["Fog-Cult Researcher Pelm"] = new("if_research_rosso_selmburg_drift", "Selmburg Drift Census",
            "The drift carries Rosso residue. Ten kills steady the drift line for a cleaner read.",
            10, "ore_crimson_flame",
            "The drift stabilises. Rosso reads clean against Selmburg fog.",
            "The drift moves around the count.",
            "The drift is logged. Selmburg breathes.",
            300, 130, QuestSubCategory.IfImplement),
        ["Volcano-Cult Mage Jiren"] = new("if_research_rosso_caldera_pulse", "Caldera Pulse Survey",
            "The caldera pulses every dozen ash-falls. Twelve kills sync the pulse to a measurable beat.",
            12, "ore_crimson_flame",
            "The pulse syncs. Rosso element binds against the volcano-line.",
            "The pulse drifts. The line stays loose.",
            "The caldera is on the page. Rosso reads sharp.",
            330, 145, QuestSubCategory.IfImplement),
        ["Smoke-Reader Vail"] = new("if_research_rosso_fog_seal", "The Fog-Seal of Rosso",
            "Ten of the fog-walkers must fall to break the lesser seal. Then the index unbinds for the season.",
            10, "",
            "The seal breaks. The fog index is open at last.",
            "The seal holds while the count holds short.",
            "The seal is broken. Walk on.",
            290, 125, QuestSubCategory.IfImplement),
        ["Ashbinder Cren"] = new("if_research_infernal_residue", "Infernal Residue Tally",
            "The ash carries residue from every infernal element. Ten kills give a tally clean enough to file.",
            10, "ore_crimson_flame",
            "The tally signs itself. Infernal residue is in the cipher.",
            "The residue keeps shifting under the ash.",
            "The tally is sealed. The ash quiets.",
            300, 130, QuestSubCategory.IfImplement),
        ["Volcanic Hermit Rust"] = new("if_research_rosso_spire_pulse", "Spire Pulse Reading",
            "The infernal spire pulses with Rosso when the heat is steady. Ten kills steady it for a reading.",
            10, "",
            "The spire reads. Rosso is bound to the spire-line.",
            "The pulse is irregular while the count holds short.",
            "The spire is read. The infernal cipher closes.",
            290, 125, QuestSubCategory.IfImplement),
        ["Crimson-Cult Scholar Devra"] = new("if_research_forneus_lesser_kin", "Lesser Kin of Forneus",
            "Eight of Crimson Forneus's lesser kin must fall before the ash-line clears.",
            8, "",
            "The lesser kin are noted. The ash-line is open for greater work.",
            "The kin keep returning between counts.",
            "The kin are logged. The line is open.",
            270, 115, QuestSubCategory.IfImplement),
        ["Forge-Cult Reader Hael"] = new("if_research_rosso_obsidian_run", "The Obsidian Run",
            "The obsidian fields run with Rosso when the crater settles. Ten kills settle it.",
            10, "ore_crimson_flame",
            "The crater settles. The obsidian run is logged.",
            "The crater shifts. The run stays untraced.",
            "The run is logged. The crater breathes easier.",
            310, 135, QuestSubCategory.IfImplement),
        ["Caldera Anchorite Sym"] = new("if_research_rosso_caldera_silence", "Caldera Silence Audit",
            "Twelve must fall before the caldera silences enough to audit.",
            12, "",
            "The caldera silences. The audit closes clean.",
            "The caldera complains while the count holds short.",
            "The audit is closed. The caldera rests.",
            320, 140, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (F84-90 demigod era, Yasha / Gaou anchors) ────
        ["Yasha-Anchorite Lien"] = new("if_research_yasha_spiral", "Yasha Spiral Audit",
            "The spiral on this floor is Yasha's index point. Ten failed spirals, and the index closes.",
            10, "ore_ash_white",
            "The spiral closes. Yasha element binds to the audit page.",
            "The spiral is open while the count holds short.",
            "The audit closes. Yasha is bound.",
            380, 165, QuestSubCategory.IfImplement),
        ["Demigod-Scholar Iren"] = new("if_research_yasha_bond_storm", "Yasha Storm-Bond Reading",
            "The storm-bond on this floor is the rarest in the index. Ten kills calm the bond enough to read it.",
            10, "ore_adamant",
            "The bond reads. Yasha storm element is in the cipher.",
            "The storm holds against the count.",
            "Yasha storm is in the cipher. The audit closes.",
            400, 175, QuestSubCategory.IfImplement),
        ["Demigod-Reader Avos"] = new("if_research_yasha_corrupted_pulse", "Corrupted Pulse Reading",
            "Eight of the corrupted-walkers must fall before the pulse stabilises for a reading.",
            8, "",
            "The pulse reads. Yasha corruption indexes clean.",
            "The pulse skips while the count holds short.",
            "The reading closes. The corruption is filed.",
            370, 160, QuestSubCategory.IfImplement),
        ["Gaou-Anchorite Quen"] = new("if_research_gaou_yasha_seal", "Yasha-Gaou Boundary Seal",
            "Ten kills break the lesser seal between Yasha and Gaou series. Then both indices open.",
            10, "ore_adamant",
            "The boundary breaks. Both series open to one cipher tonight.",
            "The boundary holds against the count.",
            "The boundary is gone. The cipher is whole.",
            420, 185, QuestSubCategory.IfImplement),
        ["Gaou-Reader Nimue"] = new("if_research_gaou_corruption", "Gaou Corruption Sweep",
            "Twelve corruption-bearers must fall on this floor. Their fall is the cipher's last entry.",
            12, "ore_ash_white",
            "Gaou corruption is on the page. The cipher closes for the season.",
            "The corruption sweeps round. Twelve, no fewer.",
            "Gaou is filed. The cipher is closed.",
            440, 195, QuestSubCategory.IfImplement),
        ["Gaou-Sealer Roen"] = new("if_research_gaou_radiance", "Radiance-Eater Audit",
            "Eight of the radiance-eaters must fall before the audit can close.",
            8, "",
            "The radiance is filed. Gaou indexes clean for the demigod cycle.",
            "Radiance flickers between the count.",
            "Gaou audit closes. The radiance keeps for next cycle.",
            410, 180, QuestSubCategory.IfImplement),
        ["Garden-Sage Calor"] = new("if_research_celestial_garden", "Celestial Garden Catalogue",
            "The garden flowers bloom by Gaou-element flow. Ten kills steady the flow for a clean catalogue.",
            10, "",
            "The catalogue closes. Garden flowers index by Gaou for the year.",
            "The flow drifts. The flowers do not bloom on the count.",
            "The garden is catalogued. Gaou breathes.",
            390, 170, QuestSubCategory.IfImplement),
        ["Apex-Reader Nuvo"] = new("if_research_apex_element_pulse", "Apex Element Pulse",
            "The apex pulse runs once a turn at this altitude. Twelve kills steady it for a reading.",
            12, "ore_adamant",
            "The apex reads. The cipher takes its last entry of the demigod cycle.",
            "The pulse drifts at this altitude.",
            "The reading is closed. The apex is filed.",
            420, 185, QuestSubCategory.IfImplement),
        ["Ascendant-Sage Maren"] = new("if_research_demigod_ascension", "Demigod Ascension Trial",
            "Ten of the ascendant must fall before the trial holds clean.",
            10, "",
            "The trial closes. The ascendant element binds.",
            "The ascendant still walks.",
            "The ascendant is bound. Walk on.",
            400, 175, QuestSubCategory.IfImplement),
        ["Element-Prophet Sorin"] = new("if_research_yasha_prophecy_close", "Yasha Prophecy Close",
            "The prophecy closes only when twelve fall on the floor of its bind.",
            12, "ore_ash_white",
            "The prophecy closes. Yasha element binds at last.",
            "The prophecy holds open.",
            "The prophecy is closed. Walk on.",
            430, 190, QuestSubCategory.IfImplement),
        ["Voidbinder Kael-Sora"] = new("if_research_void_element_audit", "Void Element Audit",
            "Twelve void-walkers fall before the audit closes on the apex floors.",
            12, "ore_adamant",
            "The audit closes. Void element binds for the cipher.",
            "The void walks between counts.",
            "The audit closes. Void is bound.",
            440, 195, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (additional F61-65 infernal coverage) ────
        ["Ash-Cantor Velin"] = new("if_research_infernal_chant_close", "Infernal Chant Close",
            "Ten of the infernal-walkers must fall to close the chant for the season.",
            10, "ore_crimson_flame",
            "The chant closes. The infernal cipher signs itself.",
            "The chant holds open.",
            "The chant is closed. The cipher rests.",
            300, 130, QuestSubCategory.IfImplement),
        ["Crimson-Anchorite Renja"] = new("if_research_rosso_lesser_pulse", "Rosso Lesser Pulse",
            "Eight kills are enough to read the lesser pulse against Rosso element.",
            8, "",
            "The lesser pulse reads. Rosso indexes one mark cleaner.",
            "The pulse skips while the count holds short.",
            "The pulse is read. Walk on.",
            280, 120, QuestSubCategory.IfImplement),
        ["Forge-Sage Tehan"] = new("if_research_infernal_anvil_call", "Infernal Anvil Call",
            "Twelve fall before the anvil's call holds steady. The forge does not lie.",
            12, "ore_crimson_flame",
            "The anvil holds. The forge writes you in.",
            "The anvil rings off-rhythm.",
            "The anvil is steady. The forge remembers.",
            320, 140, QuestSubCategory.IfImplement),

        // ── IF Element Research questgivers (additional early-floor coverage) ────
        ["Ruins-Cipher Aelis"] = new("if_research_karluin_cipher_close", "Karluin Cipher Close",
            "Eight of the cathedral's bound must fall before the cipher closes for the night.",
            8, "",
            "The cipher closes. The cathedral signs the night's work.",
            "The cipher holds while the count holds short.",
            "The cipher is closed. Walk on.",
            230, 100, QuestSubCategory.IfImplement),
        ["Forest-Element Hermit Pell"] = new("if_research_integral_quiet_grove", "Integral Quiet Grove",
            "The grove quiets at ten, no fewer. The Integral element listens only to a quiet grove.",
            10, "ore_wind_flower",
            "The grove quiets. Integral listens. Your work is logged.",
            "The grove still murmurs.",
            "The grove is quiet. Walk lightly.",
            260, 110, QuestSubCategory.IfImplement),
        ["Twilight-Cipher Senne"] = new("if_research_nox_lesser_audit", "Nox Lesser Audit",
            "Ten of the lesser dread fall, and the audit closes for the season.",
            10, "",
            "The audit closes. Nox indexes one mark cleaner.",
            "The dread still drifts.",
            "The audit is sealed. Walk on.",
            280, 120, QuestSubCategory.IfImplement),

        // ── HF Hollow Mission gap-fillers (F77 hill zone, Crystalize Claw band) ────
        ["Hollow-Knight Aron"] = new("hf_mission_crystalize_claw_audit", "The Crystalize Claw Audit",
            "Twelve fall on the hills before the Claw stops feeding. Make twelve quiet.",
            12, "",
            "The hills quiet. The Claw can be approached now — though that is not my work to take.",
            "The Claw still feeds.",
            "The hills are quiet. You walked them well.",
            340, 150, QuestSubCategory.HfMission),
        ["Hill-Hunter Kessen"] = new("hf_mission_f77_hill_clearance", "Hill Clearance Run",
            "The hill-zone needs ten quiet to break the Claw's lesser kin.",
            10, "ore_adamant",
            "The kin are broken. Carry the clearance up the climb.",
            "The kin still walk between counts.",
            "The hill is clear. Walk on.",
            320, 140, QuestSubCategory.HfMission),
        ["Crystal-Veil Anya"] = new("hf_mission_crystalize_lesser", "The Lesser Crystal Veil",
            "Eight crystal-walkers fall to break the Claw's outer veil.",
            8, "",
            "The veil breaks. The Claw is reachable.",
            "The veil holds.",
            "The veil is broken. Walk on.",
            300, 130, QuestSubCategory.HfMission),

        // ── HF Hollow Mission gap-fillers (F82 nightmare swamp) ────
        ["Crypt-Knight Vorr"] = new("hf_mission_swamp_undead_purge", "Swamp Undead Purge",
            "Fifteen of the swamp's risen must fall before I can carry the lantern further.",
            15, "ore_adamant",
            "The purge closes. The lantern walks on with you.",
            "The risen still walk.",
            "The swamp is quiet. Carry the lantern up.",
            380, 165, QuestSubCategory.HfMission),
        ["Undead-Hunter Mosca"] = new("hf_mission_nightmare_fen_cull", "Nightmare Fen Cull",
            "Twelve fen-stalkers fall to clear the lantern's last circle.",
            12, "",
            "The fen quiets. The circle closes.",
            "The fen-stalkers still circle.",
            "The fen is quiet. Walk on.",
            350, 155, QuestSubCategory.HfMission),

        // ── HF Hollow Mission gap-fillers (F86 corrupted abyss, King of Skeleton band) ────
        ["Bone-Champion Karth"] = new("hf_mission_skeleton_lesser_court", "Lesser Court of Skeletons",
            "Twelve of the lesser court must fall before the King will hold court.",
            12, "ore_adamant",
            "The court quiets. The King will see the next climber clean.",
            "The court still gathers.",
            "The court is quiet. Walk to the throne.",
            420, 180, QuestSubCategory.HfMission),
        ["Skeleton-Sage Wrein"] = new("hf_mission_abyss_bone_pulse", "Abyssal Bone Pulse",
            "Ten bone-walkers must fall before the pulse steadies enough to walk past.",
            10, "",
            "The pulse steadies. The path opens.",
            "The pulse swallows the count.",
            "The path is open. Walk it.",
            400, 170, QuestSubCategory.HfMission),

        // ── HF Hollow Mission gap-fillers (F87 corrupted abyss, Radiance Eater band) ────
        ["Radiance-Hunter Jarn"] = new("hf_mission_radiance_eater_audit", "Radiance Eater Audit",
            "Twelve of the lesser eaters must fall before the audit closes on the great one.",
            12, "ore_ash_white",
            "The audit closes. The eater can be approached.",
            "The eaters still feed.",
            "The audit is sealed. Walk on.",
            440, 190, QuestSubCategory.HfMission),
        ["Lightless-Cleric Pir"] = new("hf_mission_radiance_lesser_circle", "Lesser Circle of Light",
            "Ten of the lesser circle must fall before the abyss accepts the climber's mark.",
            10, "",
            "The circle is broken. The mark holds.",
            "The circle still binds.",
            "The mark holds. Walk on.",
            410, 175, QuestSubCategory.HfMission),

        // ── HF Hollow Mission gap-fillers (F89 corrupted abyss, Murderer Fang band) ────
        ["Fang-Hunter Eden"] = new("hf_mission_murderer_fang_audit", "Murderer Fang Audit",
            "Twelve fall before the Fang's circle quiets. The audit is the only path past.",
            12, "ore_ash_white",
            "The circle quiets. The audit closes for the climb.",
            "The Fang still feeds.",
            "The audit is closed. Walk on.",
            450, 195, QuestSubCategory.HfMission),
        ["Veil-Breaker Sahn"] = new("hf_mission_abyss_veil_breach", "Abyssal Veil Breach",
            "Ten veil-walkers must fall before the breach holds open long enough to pass.",
            10, "",
            "The breach holds. Carry the mark through.",
            "The veil heals between counts.",
            "The breach is yours. Walk it.",
            420, 180, QuestSubCategory.HfMission),

        // ── HF Hollow Mission gap-fillers (F93-94 crystal void) ────
        ["Crystal-Warden Aelis"] = new("hf_mission_crystal_warden_trial", "Crystal Warden's Trial",
            "Fifteen fall in the crystal cavern. Reflections multiply only true work.",
            15, "ore_ash_white",
            "The reflections still. The trial passes through you.",
            "The reflections still multiply.",
            "The trial closes. The cavern remembers.",
            480, 210, QuestSubCategory.HfMission),
        ["Mirror-Knight Vasa"] = new("hf_mission_void_mirror_clearance", "Void Mirror Clearance",
            "Twelve mirror-walkers must fall before the path through the void steadies.",
            12, "",
            "The mirrors steady. The path is yours.",
            "The mirrors keep multiplying.",
            "The path holds. Walk it.",
            460, 200, QuestSubCategory.HfMission),
        ["Void-Cantor Iset"] = new("hf_mission_void_chant_silencer", "The Chant That Silences",
            "Twelve void-singers must fall before the chant breaks.",
            12, "ore_ash_white",
            "The chant breaks. The void quiets for one climber.",
            "The chant fills the void each turn.",
            "The chant is broken. Walk on.",
            470, 205, QuestSubCategory.HfMission),
        ["Reflection-Hunter Pollin"] = new("hf_mission_void_reflection_cull", "Reflection Cull",
            "Ten reflections must be cut down before the path opens to the next chamber.",
            10, "",
            "The reflections fall. The chamber opens.",
            "The reflections answer their own count.",
            "The chamber is open. Walk on.",
            440, 190, QuestSubCategory.HfMission),
        ["Crystal-Cantor Reso"] = new("hf_mission_void_crystal_resonance", "Crystal Resonance Quiet",
            "Ten of the resonant must fall to quiet the chamber for passage.",
            10, "",
            "The resonance quiets. Pass through.",
            "The resonance fills the chamber again.",
            "The chamber is quiet. Walk on.",
            450, 195, QuestSubCategory.HfMission),

        // ── HF Hollow Mission gap-fillers (F96-97 ruby palace approach, divine ascension) ────
        ["Heathcliff-Loyalist Ashe"] = new("hf_mission_ruby_approach_purge", "Ruby Approach Purge",
            "Fifteen kills clear the path before the throne. The Commander would have wanted no less.",
            15, "ore_ash_white",
            "The path is clear. Heathcliff would have noted you.",
            "The path is not yet clear.",
            "The path is yours. The throne waits.",
            520, 230, QuestSubCategory.HfMission),
        ["Cloud-Palace Sentinel Rion"] = new("hf_mission_cloud_palace_clearance", "Cloud Palace Clearance",
            "Twelve fall in the cloud halls. The sentinels do not pass cowards through.",
            12, "",
            "The halls quiet. Walk through.",
            "The halls do not yet recognise you.",
            "The halls are yours. Walk to the throne.",
            500, 220, QuestSubCategory.HfMission),
        ["Deicide-Cult Reader Kolm"] = new("hf_mission_deicide_audit", "Deicide Audit",
            "Twelve fall before the audit can close. The cult will not write you in until the count is clean.",
            12, "ore_adamant",
            "The audit signs your name. The cult notes you for the throne.",
            "The audit holds short.",
            "The audit is closed. The throne is open.",
            510, 225, QuestSubCategory.HfMission),
        ["Throne-Approach Vassal Hesper"] = new("hf_mission_throne_approach_clear", "Throne Approach Clearance",
            "Ten fall on the last sky-stair. The vassals walk only with proven climbers.",
            10, "",
            "The stair is clear. The vassals walk with you to the throne.",
            "The stair is not yet quiet.",
            "The stair is yours. The throne is one step further.",
            490, 215, QuestSubCategory.HfMission),
    };

    // Generic dispatcher for all Hollow Fragment quest NPCs.
    private bool HandleHollowWeaponNpc(Entities.NPC npc)
    {
        if (npc.Name == null) return false;
        if (!_hollowWeaponQuests.TryGetValue(npc.Name, out var q)) return false;
        return HandleDivineQuest(npc,
            questId:          q.QuestId,
            questTitle:       q.Title,
            openingLine:      q.Opening,
            killCount:        q.KillCount,
            divineDefId:      q.RewardDefId,
            handOverLine:     q.HandOver,
            inProgressLine:   q.InProgress,
            postCompleteLine: q.PostComplete,
            rewardCol:        q.Col,
            rewardXp:         q.Xp,
            subCategory:      q.SubCategory);
    }

    // Guild Recruiter: trial (10 kills) → induct + rep + perk → signature quest.
    // Laughing Coffin gated behind karma ≤-50 (Outlaw).
    private bool HandleGuildRecruiter(Entities.NPC npc)
    {
        if (npc.Name == null) return false;
        if (!GuildSystem.RecruiterToGuild.TryGetValue(npc.Name, out var guildId)) return false;
        if (!GuildSystem.Guilds.TryGetValue(guildId, out var def)) return false;

        // Laughing Coffin gate — refuses conversation above karma threshold.
        if (guildId == Story.Faction.LaughingCoffin && _player.Karma > -50)
        {
            _log.Log($"{npc.Name}: \"You carry too much light for our kind. Come back when the world has taken more from you.\"");
            return true;
        }

        bool alreadyMember = _player.ActiveGuildId == guildId;

        // Already a member → offer signature quest (or completed-flavor).
        if (alreadyMember) return HandleGuildSignatureQuest(npc, def);

        string questId = $"guild_join_{guildId}";
        var existing = QuestSystem.GetQuest(questId);

        // First talk — gate on requirements.
        if (existing == null)
        {
            var (ok, reason) = GuildSystem.CanJoin(_player, def);
            if (!ok)
            {
                _log.Log($"{npc.Name}: \"{def.DisplayName} is not for you — yet.\"");
                _log.Log($"  {reason}");
                return true;
            }
            // Warn if player is currently in another guild.
            if (_player.ActiveGuildId != Story.Faction.None)
            {
                _log.Log($"{npc.Name}: \"You already wear another guild's colors. Finish our trial and we'll sort that out.\"");
            }

            QuestSystem.AddQuest(new Quest
            {
                Id = questId,
                Title = $"Prove yourself to {def.DisplayName}",
                Description = $"Slay 10 monsters to earn {def.DisplayName}'s trust.",
                GiverName = npc.Name,
                Floor = CurrentFloor,
                Type = QuestType.Kill,
                TargetMob = "",
                TargetCount = 10,
                Persistent = true,
                RewardCol = 300,
                RewardXp = 200,
            });
            _log.Log($"{npc.Name}: \"Ten kills. Any mob. Come back and we'll talk about joining.\"");
            _log.LogSystem($"  [QUEST] '{def.DisplayName}' recruitment quest added.");
            return true;
        }

        // Quest complete — induct.
        if (existing.Status == QuestStatus.Complete)
        {
            existing.Status = QuestStatus.TurnedIn;
            QuestSystem.ActiveQuests.Remove(existing);
            QuestSystem.CompletedQuests.Add(existing);
            _player.ColOnHand += existing.RewardCol;
            TotalColEarned += existing.RewardCol;
            int lvlBefore = _player.Level;
            _player.GainExperience(existing.RewardXp);
            if (_player.Level > lvlBefore) LeveledUp?.Invoke();
            KarmaSystem.Adjust(_player, KarmaSystem.DeltaQuestComplete, "guild recruitment quest", _log);

            _log.Log($"{npc.Name}: \"{def.DisplayName} accepts you.\"");
            GuildSystem.Join(_player, guildId, _log);
            return true;
        }

        // Already turned in (stale path) — treat as member.
        if (existing.Status == QuestStatus.TurnedIn)
            return HandleGuildSignatureQuest(npc, def);

        // In progress.
        int remaining = existing.TargetCount - existing.CurrentCount;
        _log.Log($"{npc.Name}: \"{remaining} more, then we'll talk.\"");
        return true;
    }

    private bool HandleGuildSignatureQuest(Entities.NPC npc, GuildSystem.GuildDef def)
    {
        if (!GuildSystem.SignatureQuests.TryGetValue(def.Id, out var sig))
        {
            _log.Log($"{npc.Name}: \"Walk tall under our banner.\"");
            return true;
        }
        var existing = QuestSystem.GetQuest(sig.QuestId);

        // First talk while a member — offer signature quest.
        if (existing == null)
        {
            QuestSystem.AddQuest(new Quest
            {
                Id = sig.QuestId,
                Title = sig.Title,
                Description = sig.Description,
                GiverName = npc.Name!,
                Floor = CurrentFloor,
                Type = QuestType.Kill,
                TargetMob = sig.MobNameFragment ?? "",
                TargetCount = sig.KillCount,
                Persistent = true,
                RewardCol = sig.RewardCol,
                RewardXp = sig.RewardXp,
                RequiresWeaponType = sig.WeaponType,
            });
            _log.Log($"{npc.Name}: \"{sig.Description}\"");
            _log.LogSystem($"  [QUEST] Signature quest: '{sig.Title}'.");
            return true;
        }
        if (existing.Status == QuestStatus.Complete)
        {
            existing.Status = QuestStatus.TurnedIn;
            QuestSystem.ActiveQuests.Remove(existing);
            QuestSystem.CompletedQuests.Add(existing);
            _player.ColOnHand += existing.RewardCol;
            TotalColEarned += existing.RewardCol;
            int lvlBefore = _player.Level;
            _player.GainExperience(existing.RewardXp);
            if (_player.Level > lvlBefore) LeveledUp?.Invoke();
            Story.StorySystem.AdjustRep(def.Id, 30);
            // LC Crimson Letter excluded (its 5 NPC kills already took -100).
            if (def.Id != Story.Faction.LaughingCoffin)
                KarmaSystem.Adjust(_player, KarmaSystem.DeltaQuestComplete, $"{def.DisplayName} signature quest", _log);
            _log.LogSystem($"  [QUEST] '{sig.Title}' turned in! +30 {def.DisplayName} rep, +{existing.RewardCol} Col, +{existing.RewardXp} XP.");
            return true;
        }
        if (existing.Status == QuestStatus.TurnedIn)
        {
            _log.Log($"{npc.Name}: \"Your deeds are remembered. {def.DisplayName} walks with you.\"");
            return true;
        }
        int remaining = existing.TargetCount - existing.CurrentCount;
        _log.Log($"{npc.Name}: \"{remaining} more. We'll see it through.\"");
        return true;
    }

    // Lisbeth at Lindarth (F48) — Rarity 6 craft dialog in place of generic flow.
    // Gated on floor 48 so Town-of-Beginnings Lisbeth (F1) stays normal.
    // F55-boss-cleared one-time Dark Repulser handover (LN canon Lisbeth gift).
    private bool HandleLisbethLindarth(Entities.NPC npc)
    {
        if (npc.Name != "Lisbeth") return false;
        if (CurrentFloor != 48) return false;

        // LN canon: Lisbeth crafts Dark Repulser after F55 dragon-ore quest. Gate on F55 boss
        // clear (HighestFloorBossCleared >= 55). One-time per save via TurnedIn quest flag.
        const string DrQuestId = "lisbeth_dark_repulser_gift";
        if (ShopTierSystem.HighestFloorBossCleared >= 55
            && QuestSystem.GetQuest(DrQuestId) == null)
        {
            var drQuest = new Quest
            {
                Id = DrQuestId,
                Title = "Lisbeth's Gift",
                Description = "Lisbeth's one-time Dark Repulser gift.",
                GiverName = npc.Name,
                Floor = CurrentFloor,
                Type = QuestType.Kill,
                TargetMob = "",
                TargetCount = 0,
                Persistent = true,
                Status = QuestStatus.TurnedIn,
                RewardCol = 0,
                RewardXp = 0,
            };
            QuestSystem.CompletedQuests.Add(drQuest);

            _log.Log($"{npc.Name}: \"Hey — remember that crystallite ore from the F55 dragon? I finally finished it. Here. I made it for you.\"");
            var dr = Items.ItemRegistry.Create("dark_repulser");
            if (dr != null)
            {
                if (_player.Inventory.AddItem(dr))
                    _log.LogLoot($"  ◈ You receive {dr.Name} — Lisbeth's gift.");
                else
                {
                    _map.AddItem(_player.X, _player.Y, dr);
                    _log.LogLoot($"  ◈ {dr.Name} — Lisbeth's gift. (Inventory full — dropped at your feet.)");
                }
            }
            // Fall through to the normal forge dialog so player can still craft on the same visit.
        }

        _log.Log($"{npc.Name}: \"Welcome to the Lindarth forge. Show me rare steel and I'll show you rare work.\"");
        LisbethInteraction?.Invoke();
        return true;
    }

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
            TickPoison(); TickBleed(); TickSlow(); TickPerTurnTimers();
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
                FlavorText.VendorGreetings[RunRng.Next(FlavorText.VendorGreetings.Length)],
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

            bool handledByRan = HandleRanTheBrawler(npc);
            bool handledByAzariya = HandleSisterAzariya(npc);
            // Awakening hook runs BEFORE HandleSelka; short-circuit OR falls through
            // to HandleSelka (preserving base + chain dialogue) when awakening conditions fail.
            bool handledBySelka = HandleSelkaAwakening(npc) || HandleSelka(npc);
            bool handledByDorothy = HandleDorothy(npc);
            bool handledByVesper = HandleScholarVesper(npc);
            bool handledByHollowNpc = HandleHollowWeaponNpc(npc);
            bool handledByLisbeth = HandleLisbethLindarth(npc);
            bool handledByGuildRecruiter = HandleGuildRecruiter(npc);
            bool handledByDivineNpc = handledByRan || handledByAzariya || handledBySelka || handledByDorothy || handledByVesper || handledByHollowNpc || handledByLisbeth || handledByGuildRecruiter;

            QuestSystem.OnNpcTalk(_log);
            // Karma scales with completion count (+3 per turn-in).
            int turnInCount = QuestSystem.ActiveQuests.Count(q => q.Status == QuestStatus.Complete);
            var (qCol, qXp) = QuestSystem.TurnInCompleted();
            if (qCol > 0 || qXp > 0)
            {
                _player.ColOnHand += qCol;
                TotalColEarned += qCol;
                int lvlBefore = _player.Level;
                _player.GainExperience(qXp);
                _log.LogSystem($"  [QUEST] Rewards: +{qCol} Col, +{qXp} XP!");
                if (_player.Level > lvlBefore) LeveledUp?.Invoke();
                // +3 karma per quest completed (generic turn-ins).
                for (int k = 0; k < turnInCount; k++)
                    KarmaSystem.Adjust(_player, KarmaSystem.DeltaQuestComplete, "quest complete", _log);
            }

            // Offer random quest if room — Divine-quest NPCs skip (no layering).
            if (!handledByDivineNpc
                && QuestSystem.ActiveQuests.Count < QuestSystem.MaxActiveQuests
                && RunRng.Next(3) == 0)
            {
                var quest = QuestSystem.GenerateQuest(CurrentFloor, npc.Name);

                // IF Implement quests are tagged on canon floor bands (Integral / Nox /
                // Rosso / Yasha / Gaou anchors); HF Missions are tagged from the broader
                // Hollow Fragment domain (F76+). IF wins on overlap.
                int floor = CurrentFloor;
                if (floor == 14 || floor == 25
                    || (floor >= 61 && floor <= 65)
                    || (floor >= 84 && floor <= 90))
                {
                    quest.SubCategory = QuestSubCategory.IfImplement;
                }
                else if (floor >= 76)
                {
                    quest.SubCategory = QuestSubCategory.HfMission;
                }

                QuestSystem.AddQuest(quest);
                _log.LogSystem($"  [QUEST] New quest from {npc.Name}: '{quest.Title}'");
                _log.Log($"  {quest.Description}");
                _log.Log($"  Reward: {quest.RewardCol} Col, {quest.RewardXp} XP");
            }

            // Lindarth Lisbeth: skip generic dialog + recruitment (avoids layering
            // "join party" over the forge UI).
            if (!handledByLisbeth)
            {
                if (npc.DialogueLines != null && npc.DialogueLines.Length > 0)
                    NpcDialogRequested?.Invoke(npc);
                else
                {
                    string dialogue = npc.Dialogue
                        ?? FlavorText.NpcFallbackDialogue[RunRng.Next(FlavorText.NpcFallbackDialogue.Length)];
                    _log.Log($"{npc.Name}: \"{dialogue}\"");
                }

                // Offer recruitment for named SAO characters.
                TryRecruitNpc(npc);
            }

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
                _log.LogSystem(FlavorText.StairsDiscoveryMessages[RunRng.Next(FlavorText.StairsDiscoveryMessages.Length)]);
                return;
            }
        }
    }

    // Recruitble NPCs: named SAO characters that can join the party.
    private static readonly Dictionary<string, (char Sym, Color Col, string Wpn, string Title)> RecruitableNpcs = new()
    {
        { "Klein",          ('K', Color.BrightRed,     "Katana",            "Samurai") },
        { "Asuna",          ('A', Color.BrightYellow,  "Rapier",            "The Flash") },
        { "Agil",           ('G', Color.BrightGreen,   "Axe",               "Axe Fighter") },
        { "Silica",         ('S', Color.BrightCyan,    "Dagger",            "Dragon Tamer") },
        { "Lisbeth",        ('L', Color.BrightMagenta, "Mace",              "Blacksmith") },
    };

    // PATH-D-PORT: recruit-confirm dialog routes through this event so game logic
    // never calls a TG widget directly. UI subscriber renders the prompt with the
    // current backend (TG MessageBox today; Path D port replaces the subscriber)
    // and calls Respond(accepted) synchronously with the user's choice. Synchronous
    // modal semantics are preserved — the subscriber blocks inside its own modal,
    // then invokes Respond before returning.
    public event Action<RecruitDialogContext>? RecruitDialogRequested;

    // No subscriber means no prompt, so the absence is logged as an error rather than letting
    // the recruit offer vanish silently.
    private void TryRecruitNpc(NPC npc)
    {
        if (!RecruitableNpcs.TryGetValue(npc.Name, out var info)) return;
        if (PartySystem.Members.Any(a => a.Name == npc.Name)) return;
        if (PartySystem.Members.Count >= PartySystem.MaxPartySize) return;
        if (RunModifiers.IsActive(RunModifier.Solo))
        {
            _log.Log($"{npc.Name} offers to join you, but Solo modifier forbids company.");
            return;
        }

        var ctx = new RecruitDialogContext(
            NpcName: npc.Name,
            Title: info.Title,
            Weapon: info.Wpn,
            PartySize: PartySystem.Members.Count,
            PartyMax: PartySystem.MaxPartySize,
            Respond: accepted =>
            {
                if (!accepted) return;
                PartySystem.TryRecruit(npc.Name, info.Sym, info.Col, info.Wpn, info.Title, _player.Level, _log);

                var ally = PartySystem.Members.LastOrDefault();
                if (ally == null) return;
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
            });

        if (RecruitDialogRequested == null)
        {
            DebugLogger.LogError("TurnManager.RecruitDialog",
                new InvalidOperationException(
                    $"RecruitDialogRequested has no subscriber — recruit prompt for {npc.Name} skipped."));
            return;
        }
        RecruitDialogRequested.Invoke(ctx);
    }

    private void PushNpcAside(NPC npc, int playerTargetX, int playerTargetY)
    {
        // Walk to adjacent walkable, preferring away-from-player direction.
        int awayX = Math.Sign(npc.X - _player.X);
        int awayY = Math.Sign(npc.Y - _player.Y);

        // Candidates: away first, perpendiculars, then others.
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
        _log.Log(cues[RunRng.Next(cues.Length)]);
        _lastSoundCueTurn = TurnCount;
    }

    // The floor boss only. FieldBoss derives from Boss and lands in GameMap.Bosses too, but
    // wilderness named elites are OPTIONAL content -- gating the way up on them would demand
    // clearing every one before a floor could be left.
    private bool FloorBossAlive() =>
        _map.Bosses.Any(b => b is not FieldBoss && !b.IsDefeated);
}

// PATH-D-PORT: payload for RecruitDialogRequested. UI subscriber synthesizes a
// modal accept/decline prompt and calls Respond(true) on accept, Respond(false)
// (or simply returns without calling) on decline. Respond is invoked synchronously.
public sealed record RecruitDialogContext(
    string NpcName,
    string Title,
    string Weapon,
    int PartySize,
    int PartyMax,
    Action<bool> Respond);
