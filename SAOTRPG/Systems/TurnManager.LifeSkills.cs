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

    // Post-kill hook (after Bestiary.RecordKill): refresh tag counts + TitleSystem.
    public void CheckTitleUnlocksAfterKill(Monster monster)
    {
        // Tag kills. Field/named bosses default "generic" (TOTAL only, no tag bucket).
        string tag = monster is Mob mob ? mob.LootTag : "generic";
        if (!string.IsNullOrEmpty(tag))
            _tagKills[tag] = _tagKills.GetValueOrDefault(tag) + 1;

        // Bestiary = authoritative species (handles Elite/Champion prefix).
        var speciesKills = Bestiary.GetAll()
            .ToDictionary(e => e.Name, e => e.TimesKilled);

        TitleSystem.CheckKillUnlocks(
            _player,
            totalKillCount: KillCount,
            speciesKills: speciesKills,
            tagKills: _tagKills);
    }

    // Post-load _tagKills rebuild. Approximates via MobFactory species→tag
    // (Bestiary doesn't persist LootTag). Missing species = "generic".
    public void RebuildTagKillsFromBestiary()
    {
        _tagKills.Clear();
        foreach (var entry in Bestiary.GetAll())
        {
            string tag = SAOTRPG.Map.MobFactory.GetLootTagForName(entry.Name) ?? "generic";
            _tagKills[tag] = _tagKills.GetValueOrDefault(tag) + entry.TimesKilled;
        }
    }
}
