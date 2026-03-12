namespace YourGame.Items;

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
    public void ApplyTo(Player player)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(player, effect, add: true);
        }
    }

    /// <summary>
    /// Remove all effects from the player.
    /// </summary>
    public void RemoveFrom(Player player)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(player, effect, add: false);
        }
    }

    private static void ApplyStat(Player player, StatEffect effect, bool add)
    {
        int value = add ? effect.Potency : -effect.Potency;

        switch (effect.Type)
        {
            case StatType.Health: player.CurrentHealth += value; break;
            case StatType.Attack: player.BaseAttack += value; break;
            case StatType.Defense: player.BaseDefense += value; break;
            case StatType.Speed: player.BaseSpeed += value; break;
            case StatType.Strength: player.Strength += value; break;
            case StatType.Vitality: player.Vitality += value; break;
            case StatType.Endurance: player.Endurance += value; break;
            case StatType.Dexterity: player.Dexterity += value; break;
            case StatType.Agility: player.Agility += value; break;
            case StatType.Intelligence: player.Intelligence += value; break;
        }
    }
}