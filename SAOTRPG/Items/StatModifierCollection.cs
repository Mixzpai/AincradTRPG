using SAOTRPG.Entities;

namespace SAOTRPG.Items;

/// <summary>
/// Collection of effects that can be applied/removed.
/// </summary>
public class StatModifierCollection
{
    public List<StatEffect> Effects { get; set; } = [];

    /// <summary>
    /// Fluent API for adding effects.
    /// </summary>
    public StatModifierCollection Add(StatType type, int potency, int duration = 0, bool isPercentage = false)
    {
        Effects.Add(new StatEffect(type, potency, duration, isPercentage));
        return this;
    }

    /// <summary>
    /// Apply all effects to the player.
    /// </summary>
    public void ApplyTo(IStatModifiable target)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(target, effect, add: true);
        }
    }

    /// <summary>
    /// Remove all effects from the target.
    /// </summary>
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
        }
    }
}