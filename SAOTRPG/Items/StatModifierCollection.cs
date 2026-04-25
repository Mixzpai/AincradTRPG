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

    // Apply all effects to the player.
    public void ApplyTo(IStatModifiable target)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(target, effect, add: true);
        }
    }

    // Remove all effects from the target.
    public void RemoveFrom(IStatModifiable target)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(target, effect, add: false);
        }
    }

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
            // Bundle 10 (B13) — see scout 2.2. AttackSpeed/BlockChance resolve at weapon/shield
            // sites in TurnManager (no-op here so collection still tallies into _statBonusCache).
            case StatType.CritRate:       target.BaseCriticalRate += value; break;
            case StatType.AttackSpeed:    break;
            case StatType.BlockChance:    break;
            case StatType.HPRegen:        target.BaseHpRegenPerTick += value; break;
            case StatType.SkillCooldown:  target.SkillCooldownReduction += value; break;
        }
    }
}