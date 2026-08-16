using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;
using SAOTRPG.Systems.Skills;
using SAOTRPG.Systems.Story;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Unified milestone dispatcher. Owns trigger evaluation + reward delivery + active-title
// slot. Persistent state lives in LifetimeStats.UnlockedMilestones (survives permadeath).
public static class MilestoneSystem
{
    private static bool _eventsWired;

    // Active player reference. Set when a TurnManager hooks; static-event handlers
    // (KarmaChanged/GuildJoined/etc.) read it to dispatch rewards. Null between runs.
    private static Player? _activePlayer;
    private static TurnManager? _activeTm;

    // Read-only accessor for UI surfaces that need the active per-run TurnManager
    // (e.g. Discovery-tab progress bars reading IfImplementQuestsCompletedThisRun).
    // Null between runs.
    public static TurnManager? ActiveTurnManager => _activeTm;

    // Tracks turn at which the player most recently leveled up. Used by the Hubris
    // death milestone (death within 5 turns of level-up).
    private static int _lastLevelUpTurn = -1000;

    // Per-process subscription guard. Idempotent so repeated calls (load + new game) are safe.
    public static void Initialize()
    {
        if (_eventsWired) return;
        _eventsWired = true;
        KarmaSystem.KarmaChanged += OnKarmaChanged;
        GuildSystem.GuildJoined += OnGuildJoined;
        UniqueSkillSystem.UniqueSkillUnlocked += OnUniqueSkillUnlocked;
        QuestSystem.QuestTurnedIn += OnQuestTurnedIn;
        PartySystem.AllyRecruited += OnAllyRecruited;
        DivineAwakening.WeaponAwakened += OnWeaponAwakened;
    }

    // Subscribe to a TurnManager instance for run-scoped events (PlayerDiedDetailed).
    public static void HookTurnManager(TurnManager tm)
    {
        _activeTm = tm;
        _activePlayer = tm.Player;
        tm.PlayerDiedDetailed += OnPlayerDied;
        tm.LeveledUp += () =>
        {
            _lastLevelUpTurn = tm.TurnCount;
            if (tm.Player.Level >= 100) TryUnlock("if_power_player", tm.Player);
        };
    }

    // ── Public entry points called from gameplay code ─────────────────

    public static void CheckCombat(TurnManager tm, Player player, Monster monster)
    {
        // First-event triggers.
        if (tm.KillCount == 1) TryUnlock("first_kill", player);
        if (monster is Boss) TryUnlock("first_boss", player);
        if (tm.KillStreak >= 5) TryUnlock("perfect_streak_5", player);

        // "Criminal". Its trigger is Conditional, which nothing evaluates, and no caller unlocked
        // it by id — so the milestone was displayed and could never be earned. Town Guards are
        // real: PopulateTownOfBeginnings posts a patrol once karma drops to -50 or below.
        if (monster.Name == "Town Guard") TryUnlock("karma_town_guard_kill", player);

        // "Last-Attack Beta Tester" — 25 floor bosses. It was typed KillCountByTag, which counts
        // mob kills by LOOT TAG, against a key no mob carries, so it could never fire. FieldBoss
        // is excluded for the same reason FloorBossAlive excludes it: wilderness elites are
        // optional content. The player landed the killing blow by definition — this runs on kill.
        if (monster is Boss and not FieldBoss)
        {
            var lt = LifetimeStats.Load();
            lt.FloorBossLastHits++;
            LifetimeStats.Save(lt);
            if (lt.FloorBossLastHits >= 25) TryUnlock("if_last_attack_beta_tester", player);
        }

        // Total-kill thresholds.
        EvaluateKillThresholds(tm.KillCount, player);

        // Tag- and species-kill thresholds.
        string speciesName = monster.Name;
        string tag = monster is Mob mob ? mob.LootTag : "generic";
        EvaluateKillByTagAndSpecies(player, tag, speciesName);

        EvaluateColThreshold(tm.TotalColEarned, player);
    }

    public static void CheckFloor(TurnManager tm, Player player, bool speedClear = false)
    {
        if (speedClear) TryUnlock("speed_clear", player);
        EvaluateFloorThresholds(tm.CurrentFloor, player);
        if (tm.BountyComplete) TryUnlock("bounty_complete", player);
        if (tm.DiscoveredLore.Count >= FlavorText.LoreStoneEntries.Length)
            TryUnlock("all_lore", player);
        EvaluateColThreshold(tm.TotalColEarned, player);
    }

    public static void CheckLifeSkillLevel(Player player, LifeSkillType skill, int newLevel)
    {
        string skillName = skill.ToString();
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.LifeSkillLevel) continue;
            if (m.TriggerArg != skillName) continue;
            if (newLevel < m.TriggerThreshold) continue;
            TryUnlock(m.Id, player);
        }
    }

    // Bestiary discovery — fires the four BestiaryCount thresholds against current
    // discovered count. Idempotent via TryUnlock's UnlockedMilestones guard.
    public static void OnBestiaryDiscovered(Player player)
    {
        int discovered = Bestiary.DiscoveredCount();
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.BestiaryCount) continue;
            if (discovered < m.TriggerThreshold) continue;
            TryUnlock(m.Id, player);
        }
    }

    // Equip-time conditional checks. weapon may be null for non-weapon slots; the
    // pair check still inspects both slots of the inventory.
    public static void OnItemEquipped(Player player, Items.Equipment.EquipmentBase equipped)
    {
        if (equipped is Items.Equipment.Weapon w)
        {
            if (w.Rarity == "Legendary")           TryUnlock("eq_legendary_equipped", player);
            if (w.Rarity == "Divine")              TryUnlock("eq_divine_equipped", player);
            if (!w.IsEnhanceable)                  TryUnlock("eq_sealed_wielder", player);
        }

        // Kirito dual: both Elucidator AND Dark Repulser equipped (any slot order).
        var mh = player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.Weapon) as Items.Equipment.Weapon;
        var oh = player.Inventory.GetEquipped(Inventory.Core.EquipmentSlot.OffHand) as Items.Equipment.Weapon;
        string? mhId = mh?.DefinitionId, ohId = oh?.DefinitionId;
        bool hasElu = mhId == "elucidator" || ohId == "elucidator";
        bool hasDr  = mhId == "dark_repulser" || ohId == "dark_repulser";
        if (hasElu && hasDr) TryUnlock("eq_black_swordsman", player);
    }

    // Anvil enhance success path — fires when any equipment crosses +10.
    public static void OnRefinementApplied(Player player, Items.Equipment.EquipmentBase eq)
    {
        if (eq.EnhancementLevel >= 10) TryUnlock("eq_refined_plus_10", player);
    }

    // Floor-clear hooks. F1 ascend with empty party = Beater; F100 victory always
    // implies no run-deaths (permadeath wipes save), so canon_iron_man fires on
    // every successful clear; if_no_ally_death gated by run-scoped KO flag.
    public static void OnFloorCleared(TurnManager tm, Player player, int clearedFloor)
    {
        if (clearedFloor == 1 && PartySystem.Members.Count == 0)
            TryUnlock("canon_beater", player);
    }

    public static void OnAincradCleared(TurnManager tm, Player player)
    {
        TryUnlock("canon_iron_man", player);
        if (!tm.AnyAllyKOdThisRun) TryUnlock("if_no_ally_death", player);
    }

    // Called from Inventory.ItemAdded handler. Caller filters Rarity == Legendary
    // before invoking so non-Legendary items never reach this path.
    public static void OnLegendaryCollected(Player player, string defId)
    {
        if (string.IsNullOrEmpty(defId)) return;
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.LegendaryCollected) continue;
            if (m.TriggerArg != defId) continue;
            TryUnlock(m.Id, player);
        }

        // Threshold milestones (CollectableCount) — count current Legendary unlocks.
        var data = LifetimeStats.Load();
        int legendaryUnlocks = 0;
        foreach (var id in data.UnlockedMilestones)
        {
            if (MilestoneRegistry.ById.TryGetValue(id, out var meta)
                && meta.Trigger == TriggerType.LegendaryCollected)
                legendaryUnlocks++;
        }
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.CollectableCount) continue;
            if (legendaryUnlocks < m.TriggerThreshold) continue;
            TryUnlock(m.Id, player);
        }
    }

    // ── Active-title slot (mirrors old TitleSystem semantics) ────────

    public static void SetActiveTitle(Player player, string? newMilestoneId)
    {
        if (player.ActiveTitleId == newMilestoneId) return;
        if (player.ActiveTitleId != null
            && MilestoneRegistry.ById.TryGetValue(player.ActiveTitleId, out var oldM)
            && oldM.Reward == RewardType.EquippableTitle)
        {
            ApplyTitleBonus(player, oldM, sign: -1);
        }
        player.ActiveTitleId = newMilestoneId;
        if (newMilestoneId != null
            && MilestoneRegistry.ById.TryGetValue(newMilestoneId, out var newM)
            && newM.Reward == RewardType.EquippableTitle)
        {
            ApplyTitleBonus(player, newM, sign: +1);
        }
    }

    public static void ClearActiveTitle(Player player) => SetActiveTitle(player, null);

    // ── Subscribed handlers ──────────────────────────────────────────

    // Karma-threshold milestones use TriggerArg "ge" (>= threshold) or "le" (<= threshold).
    // Atonement (Outlaw -> Neutral recovery) is a separate Conditional unlock.
    private static void OnKarmaChanged(int oldKarma, int newKarma,
        KarmaSystem.Tier oldTier, KarmaSystem.Tier newTier)
    {
        var player = _activePlayer;
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.KarmaThreshold) continue;
            bool crossed = m.TriggerArg switch
            {
                "ge" => newKarma >= m.TriggerThreshold && oldKarma < m.TriggerThreshold,
                "le" => newKarma <= m.TriggerThreshold && oldKarma > m.TriggerThreshold,
                _    => false,
            };
            if (crossed) TryUnlock(m.Id, player);
        }
        // Atonement: Outlaw tier left for Neutral or better.
        if (oldTier == KarmaSystem.Tier.Outlaw && newTier != KarmaSystem.Tier.Outlaw
            && newKarma >= 0)
            TryUnlock("karma_atonement", player);
    }

    // "Lisbeth's Best Customer" — 200 successful weapon upgrades. Also typed KillCountByTag
    // against a key no mob carries. Called from the enhancement success branch only: the
    // milestone's own text says "Successfully upgrade", so a failed roll must not count.
    public static void OnWeaponUpgraded(Player? player)
    {
        var lt = LifetimeStats.Load();
        lt.WeaponUpgrades++;
        LifetimeStats.Save(lt);
        if (lt.WeaponUpgrades >= 200) TryUnlock("if_lisbeth_customer", player);
    }

    private static void OnGuildJoined(Faction faction)
    {
        var player = _activePlayer;
        string key = faction.ToString();
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.GuildJoined) continue;
            if (m.TriggerArg != key) continue;
            TryUnlock(m.Id, player);
        }
    }

    private static void OnUniqueSkillUnlocked(UniqueSkill skill)
    {
        var player = _activePlayer;
        string key = skill.ToString();
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.UniqueSkillUnlocked) continue;
            if (m.TriggerArg != key) continue;
            TryUnlock(m.Id, player);
        }
    }

    // QuestCompleted milestones match by Quest.Id; the special "_first_quest" entry
    // unlocks on the first turn-in regardless of which quest it was. SubCategory
    // dispatch feeds the per-run counters used by hf_element_researcher /
    // hf_debug_field_tester.
    private static void OnQuestTurnedIn(Quest quest)
    {
        var player = _activePlayer;
        if (quest == null) return;
        string id = quest.Id ?? "";
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.QuestCompleted) continue;
            if (m.TriggerArg == "_first_quest") { TryUnlock(m.Id, player); continue; }
            if (m.TriggerArg != id) continue;
            TryUnlock(m.Id, player);
        }

        if (player == null) return;
        switch (quest.SubCategory)
        {
            case QuestSubCategory.IfImplement: OnIfImplementQuestCompleted(player); break;
            case QuestSubCategory.HfMission:   OnHfMissionCompleted(player); break;
        }
    }

    // Increment per-run IF Implement counter, refresh lifetime hi-water mark, fire
    // progressive toasts at quartile thresholds, and unlock the milestone at 100.
    public static void OnIfImplementQuestCompleted(Player player)
    {
        if (_activeTm == null) return;
        _activeTm.IfImplementQuestsCompletedThisRun++;
        int count = _activeTm.IfImplementQuestsCompletedThisRun;

        var lifetime = LifetimeStats.Load();
        if (count > lifetime.IfImplementHighWaterMark)
        {
            lifetime.IfImplementHighWaterMark = count;
            LifetimeStats.Save(lifetime);
        }

        EvaluateProgressiveToast("if_implement", count, threshold: 100);
        if (count >= 100) TryUnlock("hf_element_researcher", player);
    }

    // HF Hollow Mission counterpart: same flow against the 80-mission threshold.
    public static void OnHfMissionCompleted(Player player)
    {
        if (_activeTm == null) return;
        _activeTm.HfMissionsCompletedThisRun++;
        int count = _activeTm.HfMissionsCompletedThisRun;

        var lifetime = LifetimeStats.Load();
        if (count > lifetime.HfMissionHighWaterMark)
        {
            lifetime.HfMissionHighWaterMark = count;
            LifetimeStats.Save(lifetime);
        }

        EvaluateProgressiveToast("hf_mission", count, threshold: 80);
        if (count >= 80) TryUnlock("hf_debug_field_tester", player);
    }

    // Fires a single progress toast at exact 25/50/75% milestones. The 100% unlock
    // toast is emitted by TryUnlock's standard reward dispatch — don't duplicate.
    private static void EvaluateProgressiveToast(string trackerKey, int count, int threshold)
    {
        int q1 = threshold / 4, q2 = threshold / 2, q3 = threshold * 3 / 4;
        if (count != q1 && count != q2 && count != q3) return;

        string label = trackerKey switch
        {
            "if_implement" => "IF Implement Research",
            "hf_mission"   => "HF Missions",
            _              => trackerKey,
        };
        ToastQueue.EnqueueProgress(label, count, threshold);
    }

    private static void OnAllyRecruited(Ally ally)
    {
        var player = _activePlayer;
        if (ally == null) return;
        string name = ally.Name ?? "";
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.AllyRecruited) continue;
            if (m.TriggerArg != name) continue;
            TryUnlock(m.Id, player);
        }
    }

    // Divine awakening milestones use sentinel TriggerArgs: "first" for any Lv1
    // awakening; "lv3" for the first Lv3 reached. Per-weapon-Divine entries can
    // be added later as DivineAwakened with TriggerArg = weapon.DefinitionId.
    private static void OnWeaponAwakened(Weapon weapon, int newLevel)
    {
        var player = _activePlayer;
        if (weapon == null) return;
        if (newLevel >= 1) TryUnlock("first_awakening", player);
        if (newLevel >= 3) TryUnlock("first_lv3_awakening", player);
        string defId = weapon.DefinitionId ?? "";
        if (!string.IsNullOrEmpty(defId))
        {
            foreach (var m in MilestoneRegistry.All)
            {
                if (m.Trigger != TriggerType.DivineAwakened) continue;
                if (m.TriggerArg != defId) continue;
                if (newLevel < m.TriggerThreshold) continue;
                TryUnlock(m.Id, player);
            }
        }
    }

    // Death-category milestones. Cause-derived TriggerArgs match canonical death
    // tags ("death_poison", "death_bleeding", etc.). Ill-Prepared fires on F1 deaths;
    // Hubris fires when death lands within five turns of the most recent level-up.
    private static void OnPlayerDied(DeathContext ctx)
    {
        var player = _activePlayer;
        TryUnlock("first_death", player);

        string causeKey = $"death_{(ctx.Cause ?? "").ToLowerInvariant()}";
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.FirstEvent) continue;
            if (m.TriggerArg != causeKey) continue;
            TryUnlock(m.Id, player);
        }

        if (ctx.FloorAtDeath == 1) TryUnlock("ill_prepared", player);
        if (ctx.TurnAtDeath - _lastLevelUpTurn < 5) TryUnlock("hubris", player);

        // Boss kill detection — KillerName matches the canonical "boss" tag if the
        // killer was a floor boss. No reliable boss-flag on DeathContext yet, so
        // KillerName fallback is the broad heuristic until a richer Cause is wired.
        if (!string.IsNullOrEmpty(ctx.KillerName)
            && ctx.KillerName.Contains("Boss", StringComparison.OrdinalIgnoreCase))
            TryUnlock("death_by_boss", player);
    }

    // ── Internals ────────────────────────────────────────────────────

    // Direct unlock entry — used by Conditional triggers (hemorrhage survival,
    // any other one-shot story-ish unlock) where the caller knows the milestone Id.
    public static bool TryUnlock(string id, Player? player)
    {
        if (!MilestoneRegistry.ById.TryGetValue(id, out var milestone)) return false;
        var data = LifetimeStats.Load();
        if (!data.UnlockedMilestones.Contains(id))
            data.UnlockedMilestones.Add(id);
        else
            return false;
        LifetimeStats.Save(data);

        DispatchReward(milestone, player);
        EnqueueToast(milestone);
        return true;
    }

    private static void EnqueueToast(Milestone m)
    {
        if (m.Reward == RewardType.Col && m.ColReward > 0)
            ToastQueue.EnqueueAchievement($"{m.Name} (+{m.ColReward} Col)");
        else if (m.Reward == RewardType.EquippableTitle)
            ToastQueue.EnqueueTitle(m.Name);
        else
            ToastQueue.EnqueueAchievement(m.Name);
    }

    private static void DispatchReward(Milestone m, Player? player)
    {
        if (player == null) return;
        switch (m.Reward)
        {
            case RewardType.Col:
                player.ColOnHand += m.ColReward;
                break;
            case RewardType.EquippableTitle:
                if (player.ActiveTitleId == null)
                    SetActiveTitle(player, m.Id);
                break;
            case RewardType.AutoPassive:
                if (m.BonusStat is StatType stat)
                    PokeStat(player, stat, m.BonusValue);
                break;
            case RewardType.DisplayOnly:
                break;
        }
    }

    // Flat base-stat poke for active titles. Health bonus bumps Vitality
    // (1 Vit ≈ 10 HP) so derived MaxHealth picks it up.
    private static void ApplyTitleBonus(Player player, Milestone m, int sign)
    {
        if (m.BonusStat is not StatType stat) return;
        int v = m.BonusValue * sign;
        PokeStat(player, stat, v);

        // Beginner Slayer: +1 to the other five attributes atop the registered
        // +1 Strength, keeping the def a single StatType.
        if (m.Id == "title_beginner_slayer")
        {
            player.Vitality     += sign;
            player.Endurance    += sign;
            player.Dexterity    += sign;
            player.Agility      += sign;
            player.Intelligence += sign;
        }
    }

    private static void PokeStat(Player player, StatType stat, int v)
    {
        switch (stat)
        {
            case StatType.Attack:       player.BaseAttack       += v; break;
            case StatType.Defense:      player.BaseDefense      += v; break;
            case StatType.Speed:        player.BaseSpeed        += v; break;
            case StatType.SkillDamage:  player.BaseSkillDamage  += v; break;
            case StatType.Strength:     player.Strength         += v; break;
            case StatType.Vitality:     player.Vitality         += v; break;
            case StatType.Endurance:    player.Endurance        += v; break;
            case StatType.Dexterity:    player.Dexterity        += v; break;
            case StatType.Agility:      player.Agility          += v; break;
            case StatType.Intelligence: player.Intelligence     += v; break;
            case StatType.Health:
                player.Vitality += Math.Max(1, v / 10);
                if (v > 0)
                    player.CurrentHealth = Math.Min(player.CurrentHealth + v, player.MaxHealth);
                break;
        }
    }

    private static void EvaluateKillThresholds(int totalKills, Player player)
    {
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.KillCountTotal) continue;
            if (totalKills < m.TriggerThreshold) continue;
            TryUnlock(m.Id, player);
        }
    }

    private static void EvaluateFloorThresholds(int floor, Player player)
    {
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger != TriggerType.FloorReached) continue;
            if (floor < m.TriggerThreshold) continue;
            TryUnlock(m.Id, player);
        }
    }

    // Cumulative-Col threshold (hf_tycoon: 1,000,000 in a single run).
    private static void EvaluateColThreshold(int totalColEarned, Player player)
    {
        if (totalColEarned >= 1_000_000) TryUnlock("hf_tycoon", player);
    }

    // Tag/species kill check — uses Bestiary as the species kill source of truth.
    // The TriggerArg matches if the mob name CONTAINS the arg string (so
    // "Kobold" matches "Kobold Sentinel" and "Ruin Kobold Trooper").
    private static void EvaluateKillByTagAndSpecies(Player player, string tag, string speciesName)
    {
        foreach (var m in MilestoneRegistry.All)
        {
            if (m.Trigger == TriggerType.KillCountByTag)
            {
                if (m.TriggerArg != tag) continue;
                int tagKills = CountKillsByTag(tag);
                if (tagKills >= m.TriggerThreshold) TryUnlock(m.Id, player);
            }
            else if (m.Trigger == TriggerType.KillCountBySpecies)
            {
                if (string.IsNullOrEmpty(m.TriggerArg)) continue;
                int speciesKills = CountKillsBySpeciesContaining(m.TriggerArg);
                if (speciesKills >= m.TriggerThreshold) TryUnlock(m.Id, player);
            }
        }
    }

    private static int CountKillsByTag(string tag)
    {
        int total = 0;
        foreach (var entry in Bestiary.GetAll())
        {
            if (string.Equals(entry.LootTag, tag, StringComparison.OrdinalIgnoreCase))
                total += entry.TimesKilled;
        }
        return total;
    }

    private static int CountKillsBySpeciesContaining(string fragment)
    {
        int total = 0;
        foreach (var entry in Bestiary.GetAll())
        {
            if (entry.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                total += entry.TimesKilled;
        }
        return total;
    }
}
