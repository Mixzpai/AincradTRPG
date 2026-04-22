using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Systems;

// Life Skill XP grants hooked to movement/rest/food pipelines; Title unlock
// checks run on every kill via the Bestiary.
public partial class TurnManager
{
    // Tag-kill counter for Title unlocks. Rebuilt lazily from Bestiary on load.
    private readonly Dictionary<string, int> _tagKills = new();

    // Wires life-skill milestone banner + food XP grant via ConsumableUsed.
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

        // Title unlock banner — reuses Unique-Skill cadence.
        TitleSystem.TitleUnlocked += def =>
        {
            _log.LogSystem("══════════════════════════════════════");
            _log.LogSystem($"  ★ Title unlocked: {def.DisplayName}");
            _log.LogSystem($"    {def.Description}");
            _log.LogSystem("══════════════════════════════════════");
            ToastQueue.EnqueueTitle(def.DisplayName);
        };

        // Food XP — separate ConsumableUsed sub so ctor handler is untouched.
        _player.Inventory.Events.ConsumableUsed += (_, e) =>
        {
            if (e.Consumable is Food) GrantEatingXp();
        };
    }

    // Called from ProcessRest — awards Sleep XP per rest action (one grant
    // covers the 3 heal ticks so the curve stays tunable).
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

    // ProcessSprint: +2 XP per sprint step (2 tiles covered; canon 2x-speed).
    public void GrantSprintRunningXp()
    {
        _player.LifeSkills.GrantXp(LifeSkillType.Running, 2);
    }

    // Auto-fires via WireLifeSkillHooks ConsumableUsed → Food sub.
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
