using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    // Encounter-scoped set of monster IDs for which we've already logged
    // the Pair Resonance banner. Reset when the combo target flips, so
    // switching targets shows the banner again on the first strike.
    private readonly HashSet<int> _pairResonanceLogged = new();

    private void HandleCombat(Monster monster, int hpBefore)
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        string wpnType = wpn?.WeaponType ?? "Unarmed";
        int profBonus = GetProficiencyBonus(wpnType);

        if (_comboTarget == monster.Id) _comboCount++;
        else { _comboTarget = monster.Id; _comboCount = 1; _pairResonanceLogged.Clear(); }
        int comboBonus = Math.Max(0, (_comboCount - 1) * 2);
        // SpecialEffect: ComboBonus+N increases combo damage by N%
        int comboMulPct = GetSpecialEffectValue(wpn, "ComboBonus");
        if (comboMulPct > 0 && comboBonus > 0)
            comboBonus = comboBonus * (100 + comboMulPct) / 100;
        bool isFinisher = _comboCount == 5;

        _lastCombatTurn = TurnCount;
        bool backstab = !_aggroAlerted.Contains(monster.Id);
        var (baseDmg, playerCrit) = _player.AttackMonster(monster);

        // FD Pair Resonance: if MainHand + OffHand form a canonical pair
        // (Systems.DualWieldPairs), apply +10% total damage on the combined
        // swings and a +5% CritRate re-roll bump when the base swing did
        // not crit. First hit of the encounter logs a banner.
        var offHandWeapon = _player.Inventory.GetEquipped(EquipmentSlot.OffHand) as Weapon;
        bool pairResonance = wpn != null && offHandWeapon != null
            && DualWieldPairs.IsCanonicalPair(wpn.DefinitionId, offHandWeapon.DefinitionId);
        if (pairResonance && !playerCrit && Random.Shared.Next(100) < 5)
        {
            playerCrit = true;
        }

        int damage = baseDmg + profBonus + comboBonus + _shrineBuff + _levelUpBuff
            + SatietyAtkBonus + FatigueAtkPenalty + BiomeSystem.AttackModifier;

        // FB-063 Guild combat bonuses — Fuurinkazan: +10 ATK w/ Katana equipped.
        // Legend Braves: +15 ATK vs Laughing Coffin PKer mobs.
        damage += GuildSystem.KatanaAttackBonus(_player, wpnType);
        damage += GuildSystem.LegendBravesVsLcBonus(_player, monster.Name);
        if (pairResonance)
            damage = damage * 110 / 100;

        // Unique-skill passive damage modifiers.
        // A Dual-Blades second sword in the OffHand slot is NOT a shield —
        // Holy Sword's shield-gated bonus should only apply for true shields.
        var offHandItem = _player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.OffHand);
        bool hasShield = offHandItem != null && offHandItem is not Weapon;
        int uniqueBonusPct = Skills.UniqueSkillSystem.DamageBonusPercent(wpnType, hasShield)
                           + Skills.UniqueSkillSystem.ElementalBonusPercent(monster.Name);
        if (uniqueBonusPct > 0)
            damage = damage * (100 + uniqueBonusPct) / 100;
        if (backstab)
        {
            int backstabMul = 2 + GetSpecialEffectValue(wpn, "BackstabDmg") / 50; // +50% → x3
            damage *= backstabMul;
        }

        if (isFinisher)
        {
            damage *= 2;
            string finisher = FlavorText.ComboFinisherFlavors[Random.Shared.Next(FlavorText.ComboFinisherFlavors.Length)];
            _log.LogCombat(finisher);
            _log.LogCombat($"  5-hit combo finisher! (x2 damage = {damage})");
            _comboCount = 0;
            _comboTarget = -1;
        }
        else if (_comboCount >= 2)
        {
            TutorialSystem.ShowTip(_log, "first_combo");
            string comboFlavor = _comboCount switch { 2 => "Double strike!", 3 => "Triple strike!", 4 => "Quad strike!", _ => "" };
            _log.LogCombat($"  {_comboCount}-hit combo! {comboFlavor} (+{comboBonus} bonus damage)");
        }

        if (backstab)
        {
            _log.LogCombat($"You strike {monster.Name} from the shadows! BACKSTAB! (x2 damage)");
            CombatTextEvent?.Invoke(monster.X, monster.Y, "BACKSTAB!", Color.BrightMagenta);
        }

        var reward = monster.TakeDamage(damage);
        string wpnName = wpn?.Name ?? "Fists";
        string critTag = playerCrit ? " CRITICAL!" : "";
        if (pairResonance && _pairResonanceLogged.Add(monster.Id))
            _log.LogCombat($"  ◆ Pair Resonance! {wpn!.Name} and {offHandWeapon!.Name} sing together (+10% damage, +5% crit).");
        _log.LogCombat($"You hit {monster.Name} with {wpnName} for {damage} damage!{critTag}");
        WeaponSwing?.Invoke(_player.X, _player.Y, monster.X, monster.Y, GetSwingColor(wpn, playerCrit));
        DamageDealt?.Invoke(monster.X, monster.Y, damage, false, playerCrit);
        if (playerCrit)
        {
            CombatTextEvent?.Invoke(monster.X, monster.Y, "CRIT!", Color.BrightRed);
            TutorialSystem.ShowTip(_log, "first_crit");
        }
        DegradeEquipment(EquipmentSlot.Weapon);

        // Dual Blades / FD Paired offhand swing: if the OffHand holds a
        // weapon (not a shield) AND either Dual Blades is unlocked OR the
        // offhand weapon is an FD canon Paired weapon, land a bonus strike
        // at 60% damage. Paired weapons bypass the DualBlades unlock — they
        // are pre-tuned for dual-wield. Pair Resonance grants +10% on the
        // offhand swing as well. Skipped when the target is already dead.
        if (!monster.IsDefeated
            && _player.Inventory.GetEquipped(EquipmentSlot.OffHand) is Weapon offhand
            && (Skills.UniqueSkillSystem.HasDualBlades() || offhand.IsDualWieldPaired))
        {
            int offhandDmg = Math.Max(1, offhand.BaseDamage * 60 / 100 + profBonus / 2);
            if (pairResonance) offhandDmg = offhandDmg * 110 / 100;
            monster.TakeDamage(offhandDmg);
            _log.LogCombat($"You strike again with {offhand.Name} for {offhandDmg} damage!");
            WeaponSwing?.Invoke(_player.X, _player.Y, monster.X, monster.Y, GetSwingColor(offhand, false));
            DamageDealt?.Invoke(monster.X, monster.Y, offhandDmg, false, false);
            DegradeEquipment(EquipmentSlot.OffHand);
        }

        // SpecialEffect: CritHeal — heal a % of damage on crit
        int critHealPct = GetSpecialEffectValue(wpn, "CritHeal");
        if (playerCrit && critHealPct > 0)
        {
            int heal = Math.Max(1, damage * critHealPct / 100);
            _player.CurrentHealth = Math.Min(_player.CurrentHealth + heal, _player.MaxHealth);
            _log.LogCombat($"  {wpn!.Name} drains {heal} HP from the critical hit!");
        }

        // SpecialEffect: Bleed — chance to apply bleed on normal attacks
        int bleedChance = GetSpecialEffectValue(wpn, "Bleed");
        if (bleedChance > 0 && !monster.IsDefeated && Random.Shared.Next(100) < bleedChance)
        {
            _burningMobs[monster.Id] = (3, 1 + CurrentFloor);
            _log.LogCombat($"  {monster.Name} is bleeding from {wpn!.Name}!");
        }

        if (!monster.IsDefeated)
        {
            int hpPct = monster.CurrentHealth * 100 / monster.MaxHealth;
            _log.LogCombat($"  {monster.Name}: {hpPct}% HP remaining");
        }

        if (monster.IsDefeated && reward != null)
        {
            TutorialSystem.ShowTip(_log, "first_kill");
            HandleMonsterKill(monster, reward, wpnType, hpBefore);
        }
        else if (!monster.IsDefeated) ShowMonsterCondition(monster);
    }

    private void HandleMonsterKill(Monster monster, Monster.DefeatReward reward, string wpnType, int hpBefore)
    {
        Bestiary.RecordKill(monster.Name);
        QuestSystem.OnMobKilled(monster.Name, _log, wpnType);

        // FB-063 Karma — adjust on kill. PKer human mobs gain karma; peaceful
        // mob kills drain it; hostile non-human mobs are neutral. Town Guard
        // kills additionally seed +20 Laughing Coffin rep so LC members
        // earn standing with every outlaw encounter cleared.
        string lootTag = monster is Mob mob ? mob.LootTag : "generic";
        int karmaDelta = KarmaSystem.DeltaForMobKill(monster.Name, lootTag);
        if (karmaDelta != 0)
            KarmaSystem.Adjust(_player, karmaDelta, $"slew {monster.Name}", _log);
        if (monster.Name == "Town Guard" && _player.ActiveGuildId == Story.Faction.LaughingCoffin)
        {
            Story.StorySystem.AdjustRep(Story.Faction.LaughingCoffin, 20);
            _log.Log("  Laughing Coffin notes your handiwork. (+20 LC rep)");
        }
        // FB-058 Title System — check unlocks after each kill so the
        // banner fires immediately on milestone crossings.
        CheckTitleUnlocksAfterKill(monster);
        // Agent 2: Player Guide known/unknown gating — record kill so the
        // corresponding "Monster: <name>" / "Boss: ..." / "Field Boss: ..."
        // guide entry unlocks from the "??? (Unknown)" mask.
        if (monster is FieldBoss fb)
            Story.PlayerGuideKnowledge.MarkKnown("Field Boss: " + fb.Name);
        else if (monster is Boss b)
            Story.PlayerGuideKnowledge.MarkKnown("Boss: " + b.Name);
        else
            Story.PlayerGuideKnowledge.MarkKnown(
                "Monster: " + Story.PlayerGuideKnowledge.StripAffix(monster.Name));
        KillCount++;
        _killStreak++;
        _killsByName[monster.Name] = _killsByName.GetValueOrDefault(monster.Name) + 1;

        if (_bountyTarget != null && !_bountyComplete && monster.Name.Contains(_bountyTarget))
        {
            _bountyKillsCurrent++;
            if (_bountyKillsCurrent >= _bountyKillsNeeded)
            {
                _bountyComplete = true;
                _player.ColOnHand += _bountyRewardCol;
                TotalColEarned += _bountyRewardCol;
                int bLvl = _player.Level;
                _player.GainExperience(_bountyRewardXp);
                if (_player.Level > bLvl) LeveledUp?.Invoke();
                _log.LogSystem($"  **BOUNTY COMPLETE! +{_bountyRewardCol} Col, +{_bountyRewardXp} EXP!");
            }
            else
                _log.Log($"  Bounty progress: {_bountyKillsCurrent}/{_bountyKillsNeeded} {_bountyTarget}");
        }

        int wkBefore = _weaponKills.GetValueOrDefault(wpnType, 0);
        int profLvlBefore = ComputeLevel(wkBefore);
        _weaponKills[wpnType] = wkBefore + 1;
        int wk = _weaponKills[wpnType];
        int profLvlAfter = ComputeLevel(wk);

        // Weapon-milestone unique-skill unlocks (Katana Mastery @ 100, Martial Arts @ 30, etc).
        var milestoneUnlock = Skills.UniqueSkillSystem.CheckWeaponKillMilestone(wpnType, wk);
        if (milestoneUnlock != null) NotifyUniqueSkillUnlock(milestoneUnlock.Value);
        foreach (var rank in ProficiencyRanks)
        {
            if (wk == rank.Kills)
            {
                _log.LogSystem($"  {wpnType} proficiency: {rank.Rank}! (L{profLvlAfter}/{MaxProfLevel})");
                string flavor = GetRankUpFlavor(rank.Rank);
                if (flavor.Length > 0) _log.LogSystem($"  \"{flavor}\"");
            }
        }
        // Log plain numeric level-ups between cosmetic rank thresholds
        // (only when the level actually ticked and no rank message fired).
        if (profLvlAfter > profLvlBefore
            && !ProficiencyRanks.Any(r => r.Kills == wk))
        {
            _log.Log($"  {wpnType} proficiency L{profLvlAfter}/{MaxProfLevel}");
        }
        // Fork threshold crossing (L25/50/75/100) — fires the picker event.
        CheckForkThresholdOnKill(wpnType, profLvlBefore, profLvlAfter);

        // Check for newly unlocked sword skills at this kill count.
        // FB-564 Hollow Ingress modifier doubles required kills.
        int killMult = RunModifiers.IsActive(RunModifier.HollowIngress) ? 2 : 1;
        foreach (var skill in SwordSkillDatabase.ForWeapon(wpnType))
        {
            if (skill.RequiredProfKills * killMult == wk)
            {
                _log.LogSystem($"  **Sword Skill Unlocked: {skill.Name}! Press F to equip it.");
                TutorialSystem.ShowTip(_log, "first_skill_unlock");
                CombatTextEvent?.Invoke(_player.X, _player.Y, $"NEW SKILL!", Color.BrightCyan);
                // Auto-equip to first empty slot
                for (int i = 0; i < EquippedSkills.Length; i++)
                {
                    if (EquippedSkills[i] == null) { EquippedSkills[i] = skill; break; }
                }
            }
        }

        string defeatMsg = string.Format(
            FlavorText.DefeatFlavors[Random.Shared.Next(FlavorText.DefeatFlavors.Length)], monster.Name);
        _log.LogCombat(defeatMsg);

        if (KillCount == 1) _log.LogSystem("Your first kill in Aincrad! The journey begins.");

        HandleKillMilestone();
        HandleKillStreakCallout();

        int xp = CalcXpReward(monster, reward);
        int col = reward.Col;
        int streakTier = Math.Min(_killStreak / 5, 3);
        int streakPct = streakTier * _diffTier.ColStreakBonus;
        int streakBonus = col * streakPct / 100;
        bool perfectKill = _player.CurrentHealth == hpBefore;
        int perfectBonus = perfectKill ? col / 2 : 0;

        int lvlBefore = _player.Level;
        _player.GainExperience(xp);
        if (_player.Level > lvlBefore)
        {
            _levelUpBuff = 3 + _player.Level;
            _levelUpBuffTurns = 10;
            _log.LogSystem($"  Level-up surge! +{_levelUpBuff} ATK for 10 turns!");
            TutorialSystem.ShowTip(_log, "first_level_up");
            LeveledUp?.Invoke();
        }

        int totalCol = col + perfectBonus + streakBonus;
        _player.ColOnHand += totalCol;
        TotalColEarned += totalCol;

        string tierTag = monster is Mob { Variant: not "" } vm ? $"[{vm.Variant}] " : "";
        _log.LogCombat($"  >> {tierTag}{monster.Name} slain! +{xp} XP  +{totalCol} Col");
        if (perfectKill) _log.LogCombat("     PERFECT KILL! (+50% Col)");
        if (streakBonus > 0) _log.LogCombat($"     Streak bonus: +{streakBonus} Col");
        if (reward.WasOverkill)
        {
            string okMsg = string.Format(
                FlavorText.OverkillFlavors[Random.Shared.Next(FlavorText.OverkillFlavors.Length)],
                reward.OverkillDamage * 2);
            _log.LogCombat(okMsg);
        }

        if (monster is FieldBoss fieldBoss)
        {
            _log.LogSystem("════════════════════════════════════");
            _log.LogSystem($"  FIELD BOSS DEFEATED: {fieldBoss.Name}");
            _log.LogSystem("════════════════════════════════════");
            DefeatedFieldBosses.Add(fieldBoss.FieldBossId);
            if (!string.IsNullOrEmpty(fieldBoss.GuaranteedDropId))
            {
                var drop = Items.ItemRegistry.Create(fieldBoss.GuaranteedDropId);
                if (drop != null)
                {
                    _map.AddItem(fieldBoss.X, fieldBoss.Y, drop);
                    _log.LogLoot($"  {fieldBoss.Name} drops: {drop.Name}!");
                }
            }
            // Secondary guaranteed drop — IF series bosses drop their
            // matching series shield alongside the primary weapon.
            if (!string.IsNullOrEmpty(fieldBoss.FieldBossId)
                && LootGenerator.FieldBossSecondaryDrops.TryGetValue(fieldBoss.FieldBossId, out var secondaryId))
            {
                var secondary = Items.ItemRegistry.Create(secondaryId);
                if (secondary != null)
                {
                    _map.AddItem(fieldBoss.X, fieldBoss.Y, secondary);
                    _log.LogLoot($"  {fieldBoss.Name} also drops: {secondary.Name}!");
                }
            }
            // HF Last-Attack Bonus — F70+ field bosses have a small chance
            // to drop the Avatar Weapon matching the killer's weapon type.
            // 2% base rate, 10% on canon HNM bosses. OHS has no canon Avatar.
            if (CurrentFloor >= 70
                && LootGenerator.AvatarWeaponByWeaponType.TryGetValue(wpnType, out var avatarDefId))
            {
                bool isHnm = !string.IsNullOrEmpty(fieldBoss.FieldBossId)
                    && LootGenerator.CanonHnmBosses.Contains(fieldBoss.FieldBossId);
                int rollThreshold = isHnm ? 10 : 2;
                if (Random.Shared.Next(100) < rollThreshold)
                {
                    var avatar = Items.ItemRegistry.Create(avatarDefId);
                    if (avatar != null)
                    {
                        _map.AddItem(fieldBoss.X, fieldBoss.Y, avatar);
                        _log.LogLoot($"  ◈ Last-Attack Bonus! {fieldBoss.Name} drops: {avatar.Name}!");
                    }
                }
            }
            // Seasonal one-shot marker: Nicholas beaten → mark the year's flag.
            if (fieldBoss.IsSeasonal && fieldBoss.SeasonalEventId == "christmas")
                Story.ProfileData.MarkSeen($"nicholas_christmas_{DateTime.Now.Year}");

            // IM rare-boss ore drop — field boss has a chance to drop 1-2 random ores.
            RollBossOreDrops(fieldBoss.X, fieldBoss.Y, fieldBoss.Name);

            // Corruption Stone drop — F95+ field bosses have a 10% chance to
            // drop one of the two Corruption Stones. Canon workaround: Hollow
            // Fragment distributes Corrupted Elucidator/Dark Repulser via the
            // post-F100 boss which our F100 ending forecloses, so we route
            // the corruption mechanic through rare endgame stones instead.
            if (CurrentFloor >= 95 && Random.Shared.Next(100) < 10)
            {
                string stoneId = Random.Shared.Next(2) == 0
                    ? "night_corruption_stone" : "shadow_corruption_stone";
                var stoneDrop = Items.ItemRegistry.Create(stoneId);
                if (stoneDrop != null)
                {
                    _map.AddItem(fieldBoss.X, fieldBoss.Y, stoneDrop);
                    _log.LogLoot($"  ◆ {fieldBoss.Name} drops: {stoneDrop.Name}!");
                }
            }
        }
        else if (monster is Boss boss)
        {
            _log.LogSystem("====================================");
            _log.LogSystem($"  FLOOR BOSS DEFEATED: {boss.Name}!");
            _log.LogSystem("  The stairs to the next floor are now open!");
            _log.LogSystem("====================================");
            for (int rx = 0; rx < _map.Width; rx++)
            for (int ry = 0; ry < _map.Height; ry++)
                _map.SetExplored(rx, ry);
            _log.LogSystem("  The floor's layout is revealed on the minimap!");
            Story.StorySystem.TryFire(Story.StoryTrigger.BossDefeat,
                new Story.StoryContext(CurrentFloor, KillCount, _player, boss));

            // Guaranteed floor-boss drop (Divine Objects + P4 Alicization
            // Lycoris Divine Beast rewards on non-canon floor bosses).
            // DropItem formats Divine with the bespoke ◈ line, Legendary with
            // the standard [Legendary] format.
            if (LootGenerator.FloorBossGuaranteedDrops.TryGetValue(CurrentFloor, out var dropId))
            {
                var drop = Items.ItemRegistry.Create(dropId);
                if (drop != null) DropItem(boss.X, boss.Y, drop, boss.Name);
            }

            // IM Last-Attack Bonus floor-boss drops. F85/F92-F96/F98/F99 each
            // drop a guaranteed non-enhanceable Legendary on the player's
            // killing blow. Both this AND the FloorBossGuaranteedDrops entry
            // fire — e.g. F99 drops Night Sky Sword (Divine) AND Artemis (LAB).
            // Kill-credit assumption: HandleMonsterKill only runs after the
            // player's attack resolves, so any boss reaching this branch had
            // its killing blow from the player.
            if (LootGenerator.FloorBossLastAttackDrops.TryGetValue(CurrentFloor, out var labDropId))
            {
                var labDrop = Items.ItemRegistry.Create(labDropId);
                if (labDrop != null)
                {
                    _map.AddItem(boss.X, boss.Y, labDrop);
                    _log.LogLoot($"  ◈ Last-Attack Bonus! {boss.Name} drops: {labDrop.Name}!");
                }
            }

            // ShopTierSystem: floor-boss clear at F50+ unlocks the next tier
            // of late-game stock for all shops. Additive only — never shrinks.
            ShopTierSystem.RegisterFloorBossClear(CurrentFloor, _log);

            // IM rare-boss ore drop — floor boss has a chance to drop 1-2
            // random enhancement ores.
            RollBossOreDrops(boss.X, boss.Y, boss.Name);

            // Boss-kill unique-skill unlocks (Darkness Blade at Night, Blazing/Frozen by biome).
            var bossUnlock = Skills.UniqueSkillSystem.CheckBossKillUnlock(BiomeSystem.DisplayName);
            if (bossUnlock != null) NotifyUniqueSkillUnlock(bossUnlock.Value);
        }

        Story.StorySystem.TryFire(Story.StoryTrigger.KillCount,
            new Story.StoryContext(CurrentFloor, KillCount, _player, monster));

        DropLoot(monster);
        MonsterKilled?.Invoke(monster.X, monster.Y);
        CleanupMobStatus(monster.Id);
        _map.RemoveEntity(monster);

        // Check achievements
        foreach (var ach in Achievements.CheckCombat(this, _player, monster))
        {
            _player.ColOnHand += ach.ColReward;
            TotalColEarned += ach.ColReward;
            _log.LogSystem($"  **ACHIEVEMENT: {ach.Name} — {ach.Description} (+{ach.ColReward} Col)");
        }

        if (GetMonsterCount() == 0)
        {
            _log.LogSystem(FlavorText.FloorClearedMessages[Random.Shared.Next(FlavorText.FloorClearedMessages.Length)]);
            RevealStairs();
        }
    }

    private int CalcXpReward(Monster monster, Monster.DefeatReward reward)
    {
        int levelDiff = _player.Level - monster.Level;
        int xp = reward.Experience;
        if (levelDiff >= 5) xp /= 4;
        else if (levelDiff >= 3) xp /= 2;
        xp = xp * _diffTier.XpPercent / 100;
        // FB-564 Solo modifier +10% XP (compensation for no party).
        if (RunModifiers.IsActive(RunModifier.Solo)) xp = xp * 110 / 100;
        return Math.Max(1, xp);
    }

    private void HandleKillMilestone()
    {
        (int threshold, int bonus, string msg) milestone = KillCount switch
        {
            25  => (25,  100,  "25 monsters felled! Word spreads of your deeds."),
            50  => (50,  250,  "50 kills! The clearing guilds take notice."),
            100 => (100, 500,  "100 monsters slain! You are a force to be reckoned with."),
            250 => (250, 1000, "250 kills! Your name echoes through Aincrad."),
            500 => (500, 2500, "500 kills! A legend walks these halls."),
            _   => (0, 0, "")
        };
        if (milestone.threshold > 0)
        {
            _player.ColOnHand += milestone.bonus;
            TotalColEarned += milestone.bonus;
            _log.LogSystem($"  **MILESTONE: {milestone.msg} +{milestone.bonus} Col!");
        }
    }

    private void HandleKillStreakCallout()
    {
        string? streakMsg = _killStreak switch
        {
            2 => "Double Kill!", 3 => "Triple Kill!", 4 => "Quad Kill!",
            5 => "Rampage! (Col bonus active)", 10 => "GODLIKE! (Col bonus x2)",
            15 => "LEGENDARY! (Col bonus x3)", >= 6 => $"UNSTOPPABLE! ({_killStreak} streak)",
            _ => null
        };
        if (streakMsg != null) _log.LogCombat($"*** {streakMsg} ***");
    }

    // Each weapon type has a signature swing color for visual identity.
    private static Color GetSwingColor(Weapon? wpn, bool crit)
    {
        if (crit) return Color.BrightCyan;
        if (wpn == null) return Color.Gray;
        return wpn.WeaponType switch
        {
            "One-Handed Sword" => Color.BrightYellow,
            "Two-Handed Sword" => Color.BrightRed,
            "Rapier"           => Color.BrightCyan,
            "Dagger"           => Color.BrightGreen,
            "Katana"           => Color.BrightMagenta,
            "Axe"              => Color.BrightRed,
            "Mace"             => Color.White,
            "Spear"            => Color.BrightCyan,
            "Bow"              => Color.BrightYellow,
            "Scimitar"         => Color.Yellow,
            "Claws"            => Color.BrightRed,
            "Scythe"           => Color.BrightMagenta,
            _                  => Color.White,
        };
    }

    private void NotifyUniqueSkillUnlock(Skills.UniqueSkill skill)
    {
        var def = Skills.UniqueSkillSystem.Definitions[skill];
        _log.LogSystem("══════════════════════════════════════");
        _log.LogSystem($"  ★ UNIQUE SKILL AWAKENED: {def.Name}");
        _log.LogSystem($"    {def.Description}");
        _log.LogSystem("══════════════════════════════════════");
        CombatTextEvent?.Invoke(_player.X, _player.Y, def.Name.ToUpper(), def.DisplayColor);
    }

    private void ShowMonsterCondition(Monster monster)
    {
        int pct = (int)Math.Round(100.0 * monster.CurrentHealth / monster.MaxHealth);
        string hpBar = BuildMobHpBar(monster.CurrentHealth, monster.MaxHealth, 12);
        string condition = pct > 75 ? "healthy" : pct > 50 ? "wounded" : pct > 25 ? "badly hurt" : "near death";
        _log.LogCombat($"  {monster.Name} {hpBar} {monster.CurrentHealth}/{monster.MaxHealth} ({condition})");
        if (pct <= 10) _log.LogCombat($"  >> {monster.Name} staggers! One more hit! <<");
        else if (pct <= 25) _log.LogCombat($"  {monster.Name} is barely standing...");
    }
}
