using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Systems;

// Life Skill XP grants hooked to movement/rest/food pipelines. Kill-driven
// milestone unlocks run inside MilestoneSystem.CheckCombat directly.
public partial class TurnManager
{
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
            MilestoneSystem.CheckLifeSkillLevel(_player, skill, level);
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

}
