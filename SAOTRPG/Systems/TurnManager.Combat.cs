using Terminal.Gui;
using SAOTRPG.Entities;
using SAOTRPG.Inventory.Core;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

public partial class TurnManager
{
    private void HandleCombat(Monster monster, int hpBefore)
    {
        var wpn = _player.Inventory.GetEquipped(EquipmentSlot.Weapon) as Weapon;
        string wpnType = wpn?.WeaponType ?? "Unarmed";
        int profBonus = GetProficiencyBonus(wpnType);

        if (_comboTarget == monster.Id) _comboCount++;
        else { _comboTarget = monster.Id; _comboCount = 1; }
        int comboBonus = Math.Max(0, (_comboCount - 1) * 2);
        // SpecialEffect: ComboBonus+N increases combo damage by N%
        int comboMulPct = GetSpecialEffectValue(wpn, "ComboBonus");
        if (comboMulPct > 0 && comboBonus > 0)
            comboBonus = comboBonus * (100 + comboMulPct) / 100;
        bool isFinisher = _comboCount == 5;

        _lastCombatTurn = TurnCount;
        bool backstab = !_aggroAlerted.Contains(monster.Id);
        var (baseDmg, playerCrit) = _player.AttackMonster(monster);
        int damage = baseDmg + profBonus + comboBonus + _shrineBuff + _levelUpBuff
            + SatietyAtkBonus + FatigueAtkPenalty + BiomeSystem.AttackModifier;

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
        _log.LogCombat($"You hit {monster.Name} with {wpnName} for {damage} damage!{critTag}");
        WeaponSwing?.Invoke(_player.X, _player.Y, monster.X, monster.Y, GetSwingColor(wpn, playerCrit));
        DamageDealt?.Invoke(monster.X, monster.Y, damage, false, playerCrit);
        if (playerCrit)
        {
            CombatTextEvent?.Invoke(monster.X, monster.Y, "CRIT!", Color.BrightRed);
            TutorialSystem.ShowTip(_log, "first_crit");
        }
        DegradeEquipment(EquipmentSlot.Weapon);

        // Dual Blades offhand swing: if the OffHand holds a weapon (not a shield)
        // and the player has unlocked Dual Blades, land a bonus strike at 60% damage.
        // Skipped when the target is already dead — no swinging at corpses.
        if (!monster.IsDefeated
            && Skills.UniqueSkillSystem.HasDualBlades()
            && _player.Inventory.GetEquipped(EquipmentSlot.OffHand) is Weapon offhand)
        {
            int offhandDmg = Math.Max(1, offhand.BaseDamage * 60 / 100 + profBonus / 2);
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

        _weaponKills[wpnType] = _weaponKills.GetValueOrDefault(wpnType, 0) + 1;
        int wk = _weaponKills[wpnType];

        // Weapon-milestone unique-skill unlocks (Katana Mastery @ 100, Martial Arts @ 30, etc).
        var milestoneUnlock = Skills.UniqueSkillSystem.CheckWeaponKillMilestone(wpnType, wk);
        if (milestoneUnlock != null) NotifyUniqueSkillUnlock(milestoneUnlock.Value);
        foreach (var rank in ProficiencyRanks)
        {
            if (wk == rank.Kills)
            {
                _log.LogSystem($"  {wpnType} proficiency: {rank.Rank}! (+{rank.Bonus} damage)");
                string flavor = GetRankUpFlavor(rank.Rank);
                if (flavor.Length > 0) _log.LogSystem($"  \"{flavor}\"");
            }
        }

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
            // Seasonal one-shot marker: Nicholas beaten → mark the year's flag.
            if (fieldBoss.IsSeasonal && fieldBoss.SeasonalEventId == "christmas")
                Story.ProfileData.MarkSeen($"nicholas_christmas_{DateTime.Now.Year}");
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
