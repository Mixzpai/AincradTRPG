using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Systems;

// FB-050..054 + FB-057 + FB-058 integration glue — hooks the Life Skill
// XP grants to the existing movement/rest/food pipelines and runs the
// Title unlock checks on every kill via the Bestiary.
public partial class TurnManager
{
    // Running tag-kill counter for Title System unlock checks. Recomputed
    // lazily the first time a title check fires in a session (or after
    // LoadFromSave sets it from the Bestiary snapshot).
    private readonly Dictionary<string, int> _tagKills = new();

    // Wires the life-skill milestone banner + hooks the food grant off the
    // existing Inventory.Events.ConsumableUsed subscription that was already
    // being re-used for Satiety, crystals, etc. Called from TurnManager ctor.
    private void WireLifeSkillHooks()
    {
        _player.LifeSkills.LifeSkillMilestoneReached += (skill, level) =>
        {
            string name = LifeSkillSystem.Label(skill);
            string bonus = LifeSkillSystem.MilestoneBonusDescription(skill, level);
            _log.LogSystem("══════════════════════════════════════");
            _log.LogSystem($"  ✦ {name} reaches Level {level}!  ({bonus})");
            _log.LogSystem("══════════════════════════════════════");
        };

        // Title unlock banner — reuses the Unique-Skill cadence so both
        // feel alike in the log.
        TitleSystem.TitleUnlocked += def =>
        {
            _log.LogSystem("══════════════════════════════════════");
            _log.LogSystem($"  ★ Title unlocked: {def.DisplayName}");
            _log.LogSystem($"    {def.Description}");
            _log.LogSystem("══════════════════════════════════════");
        };

        // Food consumption XP — subscribe in addition to the existing
        // ConsumableUsed handler so we don't disturb its logic. This fires
        // per consumable use regardless of food subtype.
        _player.Inventory.Events.ConsumableUsed += (_, e) =>
        {
            if (e.Consumable is Food) GrantEatingXp();
        };
    }

    // Called from ProcessRest — awards Sleep XP per rest action. A full
    // rest triggers 3 heal ticks (see ProcessRest loop) and a single grant
    // here keeps the log clean and the curve tunable.
    public void GrantRestSleepXp()
    {
        _player.LifeSkills.GrantXp(LifeSkillType.Sleep, 20);
    }

    // Called when the player spends a turn at a Campfire / Inn tile. The
    // handler lives in Tiles.cs — this keeps the XP number centralized.
    public void GrantCampfireSleepXp()
    {
        _player.LifeSkills.GrantXp(LifeSkillType.Sleep, 10);
    }

    // Called from ProcessPlayerMove's normal-step branch (NOT sprint, NOT
    // stealth). +1 XP per tile walked.
    public void GrantWalkingXp()
    {
        _player.LifeSkills.GrantXp(LifeSkillType.Walking, 1);
    }

    // Called from ProcessSprint. +2 XP per sprint step (2 tiles covered
    // per action — matches the canonical 2x-speed risk curve).
    public void GrantSprintRunningXp()
    {
        _player.LifeSkills.GrantXp(LifeSkillType.Running, 2);
    }

    // Called when any Food consumable is used. Wired via ConsumableUsed
    // in WireLifeSkillHooks so it fires automatically — call sites don't
    // need to remember it.
    public void GrantEatingXp()
    {
        _player.LifeSkills.GrantXp(LifeSkillType.Eating, 10);
    }

    // Hooked from TurnManager.Combat.HandleMonsterKill after Bestiary
    // records the kill. Recomputes the tag-kill count for the slain mob's
    // loot tag and feeds both species- and tag-kill state into the Title
    // System for unlock evaluation.
    public void CheckTitleUnlocksAfterKill(Monster monster)
    {
        // Tag kills: if this mob has a loot tag, bump the local counter.
        // Field bosses and named bosses use "generic" by default — they
        // still contribute to the TOTAL kill count but no tag bucket.
        string tag = monster is Mob mob ? mob.LootTag : "generic";
        if (!string.IsNullOrEmpty(tag))
            _tagKills[tag] = _tagKills.GetValueOrDefault(tag) + 1;

        // Species kills pulled from the Bestiary — it's authoritative
        // and already handles Elite/Champion prefix stripping via the
        // raw Name key the kill was recorded with.
        var speciesKills = Bestiary.GetAll()
            .ToDictionary(e => e.Name, e => e.TimesKilled);

        TitleSystem.CheckKillUnlocks(
            _player,
            totalKillCount: KillCount,
            speciesKills: speciesKills,
            tagKills: _tagKills);
    }

    // Rebuild the _tagKills cache from the current Bestiary after
    // LoadFromSave. MobFactory's template LootTag data doesn't live on
    // Bestiary entries, so we approximate via a species→tag map: any
    // species whose name matches a known mob template adopts that tag.
    // Missing species default to "generic" and don't count toward tag
    // milestones.
    public void RebuildTagKillsFromBestiary()
    {
        _tagKills.Clear();
        foreach (var entry in Bestiary.GetAll())
        {
            string tag = Map.MobFactory.GetLootTagForName(entry.Name) ?? "generic";
            _tagKills[tag] = _tagKills.GetValueOrDefault(tag) + entry.TimesKilled;
        }
    }
}
