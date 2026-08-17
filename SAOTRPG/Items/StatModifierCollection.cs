using SAOTRPG.Entities;

namespace SAOTRPG.Items;

// Collection of effects that can be applied/removed.
public class StatModifierCollection
{
    public List<StatEffect> Effects { get; set; } = [];

    // Fluent API for adding effects.
    public StatModifierCollection Add(StatType type, int potency, int duration = 0, bool isPercentage = false)
    {
        Effects.Add(new StatEffect(type, potency, duration, isPercentage));
        return this;
    }

    // Effects that last a fixed number of turns rather than applying once. Equipment bonuses all
    // declare duration 0 and are unaffected; only the buff potions use this.
    public IEnumerable<StatEffect> TimedEffects => Effects.Where(e => e.Duration > 0);

    // Apply the INSTANT effects to the player — a duration is not this type's business.
    //
    // This used to apply everything, duration and all, and nothing ever took a timed effect back:
    // an Iron Skin Potion reading "Increases Defense by 10 for 30 turns" added 10 Defense
    // PERMANENTLY, and it stacks to 20 in a single inventory slot. TurnManager owns timed buffs
    // now and applies them itself; leaving them here would double-apply and never expire.
    public void ApplyTo(IStatModifiable target)
    {
        foreach (var effect in Effects)
        {
            if (effect.Duration > 0) continue;
            ApplyStat(target, effect, add: true);
        }
    }

    // Remove the instant effects, mirroring ApplyTo so Equip/Unequip stay symmetric.
    public void RemoveFrom(IStatModifiable target)
    {
        foreach (var effect in Effects)
        {
            if (effect.Duration > 0) continue;
            ApplyStat(target, effect, add: false);
        }
    }

    // Apply or take back one effect, for a caller that owns the timing.
    public static void ApplySingle(IStatModifiable target, StatType type, int potency, bool add)
        => ApplyStat(target, new StatEffect(type, potency, 0, false), add);

    private static void ApplyStat(IStatModifiable target, StatEffect effect, bool add)
    {
        int value = add ? effect.Potency : -effect.Potency;

        switch (effect.Type)
        {
            case StatType.Health: target.CurrentHealth += value; break;
            case StatType.Attack: target.BaseAttack += value; break;
            case StatType.Defense: target.BaseDefense += value; break;
            case StatType.Speed: target.BaseSpeed += value; break;
            case StatType.Strength: target.Strength += value; break;
            case StatType.Vitality: target.Vitality += value; break;
            case StatType.Endurance: target.Endurance += value; break;
            case StatType.Dexterity: target.Dexterity += value; break;
            case StatType.Agility: target.Agility += value; break;
            case StatType.Intelligence: target.Intelligence += value; break;
            // AttackSpeed/BlockChance resolve at weapon/shield sites in TurnManager
            // (no-op here so collection still tallies into _statBonusCache).
            case StatType.CritRate:       target.BaseCriticalRate += value; break;
            case StatType.AttackSpeed:    break;
            case StatType.BlockChance:    break;
            case StatType.HPRegen:        target.BaseHpRegenPerTick += value; break;
            case StatType.SkillCooldown:  target.SkillCooldownReduction += value; break;
        }
    }
}