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
        // FB-051 — Sleep skill XP for resting. One grant per rest action
        // (not per heal tick) so the log/curve stays clean.
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
        // FB-053 — Running skill XP per sprint action (covers 2 tiles).
        GrantSprintRunningXp();
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
        // FB-052 — Walking skill XP per normal-step tile. Excludes sprint
        // (which has its own hook in ProcessSprint) and stealth (which may
        // be wired to its own future Stealth skill). +1 XP per tile.
        if (!_stealthActive && !_lastMoveWasStealth && (dx != 0 || dy != 0))
            GrantWalkingXp();

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
    // Logs once per floor on the first reveal; subsequent reveals are silent
    // so the log doesn't spam as the player sweeps a corridor.
    private void RevealNearbyTraps()
    {
        if (!Skills.UniqueSkillSystem.Has(Skills.UniqueSkill.ExtraSearch)) return;
        int r = Skills.UniqueSkillSystem.SearchRadius();
        int px = _player.X, py = _player.Y;
        bool revealedAny = false;
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
            revealedAny = true;
        }
        if (revealedAny && !_extraSearchRevealedThisFloor)
        {
            _extraSearchRevealedThisFloor = true;
            _log.LogSystem("Your trained eye spots a hidden trap nearby!");
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
            // Guard against double-banner: the 30-unarmed-kill milestone route may have
            // already unlocked Martial Arts. TryUnlock returns false in that case; only
            // fire the banner when this call is actually the one doing the unlocking.
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

    // Shared Divine-Object quest handler used by Sister Azariya (F50, Heaven-
    // Piercing Blade) and Selka the Novice (F65, Fragrant Olive Sword).
    // Flow: first-talk offers a Kill quest scoped to the current floor; on return
    // with quest complete, grants the Divine into inventory (or drops it at the
    // player's feet if inventory is full). Status 'TurnedIn' prevents re-grant.
    private bool HandleDivineQuest(Entities.NPC npc, string questId, string questTitle,
        string openingLine, int killCount, string divineDefId, string handOverLine,
        string inProgressLine, string postCompleteLine, int rewardCol, int rewardXp)
    {
        if (npc.Name == null) return false;

        var existing = QuestSystem.GetQuest(questId);

        // First talk — offer the trial.
        if (existing == null)
        {
            QuestSystem.ActiveQuests.Add(new Quest
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

    // Dorothy — F78 Starlight Banner (8th Divine Object) giver.
    // SAO Last Recollection canon (Underworld purification-scythe wielder).
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

    // Selka the Novice — F65 Fragrant Olive Sword giver, with a chained
    // "Unfolding Truth" awakening quest offered once the base Fragrant
    // Olive quest is turned in. Quest order:
    //   1. First talk → base Fragrant Olive kill quest.
    //   2. Base complete + turned in → "The Sword's Awakening" (30 kills
    //      on F65+, rewards ohs_unfolding_truth_fragrant_olive).
    //   3. Awakening turned in → locked; Selka flavor only.
    // Legacy saves with the base quest already in TurnedIn state trigger
    // the Awakening quest on next talk — HandleDivineQuest uses
    // QuestSystem.GetQuest which reads both active and completed lists.
    private bool HandleSelka(Entities.NPC npc)
    {
        if (npc.Name != "Selka the Novice") return false;

        var baseQuest = QuestSystem.GetQuest("divine_fragrant_olive");
        // Base quest still pending (not yet turned in) — keep the first-quest
        // flow. This covers first-talk (null) and in-progress states.
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
    // Canon-accurate placement for HF Legendary weapons gated behind NPC
    // quests. Spec: (questId, title, opening, kills, defId, handover,
    // in-progress, post-complete, col, xp).
    private record HollowWeaponQuest(string QuestId, string Title, string Opening,
        int KillCount, string RewardDefId, string HandOver, string InProgress,
        string PostComplete, int Col, int Xp);

    private static readonly Dictionary<string, HollowWeaponQuest> _hollowWeaponQuests = new()
    {
        ["Scholar Ellroy"] = new("hf_infinite_ouroboros", "The Endless Coil",
            "Fifteen serpent-beasts must fall below. When they do, the coil is yours.",
            15, "infinite_ouroboros",
            "You have broken the coil. Take it — let it remember you.",
            "The coils still turn.", "The Ouroboros answers to you now.",
            400, 300),
        ["Hunter Kojiro"] = new("hf_jato_onikirimaru", "The Oni Cutter's Trial",
            "Fifteen kills. Any mob, any make. Then the blade of oni-splitting is yours.",
            15, "jato_onikirimaru",
            "Onikiri-maru has waited long enough. Swing it well.",
            "Fifteen. No fewer.", "The blade remembers every cut now.",
            400, 300),
        ["Ranger Torva"] = new("hf_fiendblade_deathbringer", "Thinning the Grove",
            "The grove dies in cycles. Fell fifteen and the blade it buried is yours.",
            15, "fiendblade_deathbringer",
            "The grove gives up its secret. Drink deep — and quickly.",
            "The grove still breathes.", "Deathbringer bleeds in your hand.",
            450, 320),
        ["Apiarist Nell"] = new("hf_fayblade_tizona", "The Hornet's Undoing",
            "Fifteen fall, and the fay-blade remembers its owner.",
            15, "fayblade_tizona",
            "Tizona is yours. Let it sing against the wing.",
            "The hornets still hum.", "Fay-steel. Move quickly with it.",
            450, 320),
        ["Watcher Kael"] = new("hf_starmace_elysium", "The Shining Swarm",
            "Twenty of the shining ones. Elysium crowns the steady hand.",
            20, "starmace_elysium",
            "Elysium answers. You will not be moved again.",
            "The swarm returns each turn.", "Elysium stands with you.",
            600, 450),
        ["High Priestess Sola"] = new("hf_eurynomes_holy_sword", "The Holy Trial",
            "Twenty of the fallen. Then and only then does the holy sword judge you.",
            20, "eurynomes_holy_sword",
            "Eurynome's blessing is yours. The blade obeys the worthy alone.",
            "The trial is not complete.", "Walk in the light, champion.",
            650, 480),
        ["Torchbearer Meir"] = new("hf_saintspear_rhongomyniad", "The Dark Lanterns",
            "Twenty lanterns must be broken. The saint's spear judges the rest.",
            20, "saintspear_rhongomyniad",
            "Rhongomyniad lights again. Hold it high.",
            "The dark still pools.", "The spear carries your will now.",
            700, 520),
        ["Elder Beastkeeper"] = new("hf_shinto_ama_no_murakumo", "The Restless Herd",
            "Twenty-five. Still them, and the cloud-splitter is yours.",
            25, "shinto_ama_no_murakumo",
            "Ama-no-Murakumo answers the quiet hand. Carry it so.",
            "My charges still rage.", "The cloud parts for you.",
            800, 600),
        ["Sentinel Captain"] = new("hf_godspear_gungnir", "The Broken Line",
            "Twenty-five. Hold the line no one else held. Gungnir is its own reward.",
            25, "godspear_gungnir",
            "Gungnir returns to a worthy grip. Strike true.",
            "The line still bleeds.", "Odin's spear rests with you now.",
            800, 600),

        // ── HF Endgame Expansion Implement System questgivers (4) ──
        // F84, F85, F92, F99 — quest-only Implement System weapons.
        ["Spiralist Vey"] = new("hf_spiralblade_rendering_fail", "The Spiral That Fails",
            "Ten break the pattern. The rapier answers only the spiral that fails.",
            10, "rap_spiralblade_rendering_fail",
            "Rendering Fail is yours. It knows imperfect geometry now.",
            "The spiral still turns true.", "The rapier rests with you.",
            500, 360),
        ["Crusher Drago"] = new("hf_crusher_bond_cyclone", "The Iron Cyclone",
            "Ten storms. Break them all, and the cyclone axe is yours.",
            10, "axe_crusher_bond_cyclone",
            "Bond Cyclone answers the steady haft. Heft it well.",
            "The storm still churns.", "The axe is yours, wielder.",
            550, 380),
        ["Auric Knight Halric"] = new("hf_aurumbrand_hauteclaire", "The Golden Shroud",
            "Fifteen fall to prove the shroud will not shroud a coward. Go.",
            15, "ohs_aurumbrand_hauteclaire",
            "Hauteclaire's gold recognises you. Be as steady as its edge.",
            "The shroud waits on.", "Hauteclaire shines in your hand.",
            700, 500),
        ["Last Herald Xiv"] = new("hf_deathglutton_epetamu", "The Last Hollow Glutton",
            "Twenty. The floor before the top. The blade that feeds on its wielder asks for proof.",
            20, "sci_deathglutton_epetamu",
            "Epetamu will feed. It asks only that you feed it well.",
            "The pact is not yet written.", "The hollow-blade answers no other now.",
            900, 700),

        // FD Canon Field-Boss Wiring pass — F55 Agil apprentice NPC, the
        // lone quest-NPC entry added to pull axe_ground_gorge off the
        // floor-banded pool and onto a dedicated canon source.
        ["Agil's Apprentice"] = new("fd_agils_apprentice_ground_gorge", "The Apprentice's Ground Gorge",
            "Fifteen felled on this floor, and Agil says I can let the axe go. Show me fifteen.",
            15, "axe_ground_gorge",
            "Fifteen it was. Ground Gorge is yours — treat it like it cleaves the earth, because it does.",
            "Not yet. Agil was clear — fifteen, no fewer.",
            "May its bite never dull.",
            500, 400),
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
            rewardXp:         q.Xp);
    }

    // ── FB-063 Guild Recruiter dispatcher ────────────────────────────────
    // Recruitment flow:
    //   1. First talk → offer "Prove yourself" quest (10 kills).
    //   2. Quest complete + turn in → induct into guild, seed +10 rep,
    //      apply flat perk. If player already in THIS guild, offer the
    //      signature quest. If in a DIFFERENT guild, warn (join requires
    //      leaving the current one, which is done automatically via Join).
    //   3. Signature quest post-join: kill-count themed quest from
    //      GuildSystem.SignatureQuests. Turn in → +rep / +col / +xp.
    //
    // Laughing Coffin's Herald is a special case: it refuses to deal until
    // the player's karma is Outlaw (≤-50).
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

            QuestSystem.ActiveQuests.Add(new Quest
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
            QuestSystem.ActiveQuests.Add(new Quest
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
            // FB-063 — signature quest karma. LC's Crimson Letter is
            // intentionally excluded from the +3 bonus (the 5 NPC kills it
            // demands already drained -100 karma; no +3 redemption).
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

    // Lisbeth at Lindarth (F48) — opens the Rarity 6 craft dialog in place
    // of the generic NPC dialog/quest flow. Only fires when we're on the
    // canon Lindarth floor so the Town-of-Beginnings Lisbeth still greets
    // the player normally on F1.
    private bool HandleLisbethLindarth(Entities.NPC npc)
    {
        if (npc.Name != "Lisbeth") return false;
        if (CurrentFloor != 48) return false;
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
            // Sister Azariya (F50): Heaven-Piercing Blade quest.
            bool handledByAzariya = HandleSisterAzariya(npc);
            // Selka the Novice (F65): Fragrant Olive Sword quest.
            bool handledBySelka = HandleSelka(npc);
            // Dorothy (F78): Starlight Banner (8th Divine) quest.
            bool handledByDorothy = HandleDorothy(npc);
            // Hollow Fragment HNM quests (9 NPCs F79-F98).
            bool handledByHollowNpc = HandleHollowWeaponNpc(npc);
            // Lisbeth at Lindarth (F48) — opens Rarity 6 craft dialog.
            bool handledByLisbeth = HandleLisbethLindarth(npc);
            // FB-063 Guild recruiters — 8 canon guild recruitment NPCs.
            bool handledByGuildRecruiter = HandleGuildRecruiter(npc);
            bool handledByDivineNpc = handledByRan || handledByAzariya || handledBySelka || handledByDorothy || handledByHollowNpc || handledByLisbeth || handledByGuildRecruiter;

            // Turn in completed quests
            QuestSystem.OnNpcTalk(_log);
            // Count how many will turn in so FB-063 karma grants scale with
            // the actual number of completions (each completed quest = +3 karma).
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
                // FB-063 — +3 karma per quest completed (generic turn-ins).
                for (int k = 0; k < turnInCount; k++)
                    KarmaSystem.Adjust(_player, KarmaSystem.DeltaQuestComplete, "quest complete", _log);
            }

            // Offer a new quest if the player has room — Divine-quest NPCs never
            // give random quests layered on top of their canonical handler.
            if (!handledByDivineNpc
                && QuestSystem.ActiveQuests.Count < QuestSystem.MaxActiveQuests
                && npc.CanInteract && Random.Shared.Next(3) == 0)
            {
                var quest = QuestSystem.GenerateQuest(CurrentFloor, npc.Name);
                QuestSystem.ActiveQuests.Add(quest);
                _log.LogSystem($"  [QUEST] New quest from {npc.Name}: '{quest.Title}'");
                _log.Log($"  {quest.Description}");
                _log.Log($"  Reward: {quest.RewardCol} Col, {quest.RewardXp} XP");
            }

            // Lisbeth at Lindarth already opens the craft dialog and logs her
            // line; skip the generic dialogue + recruitment offer to avoid
            // layering an "offer to join party" over the forge UI.
            if (!handledByLisbeth)
            {
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
