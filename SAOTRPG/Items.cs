/****************************************************************************************/
//  This file defines the base classes for items, including equipment and consumables,
//  along with a system for applying stat modifiers to the player character.

/****************************************************************************************/
// Base class for all items
public abstract class BaseItem
{
    public string? Name { get; set; }
    public int Value { get; set; }
    public string? Rarity { get; set; }
    public int ItemDurability { get; set; }
}

/****************************************************************************************/
// Individual stat effect with potency and duration
public class StatEffect
{
    public StatType Type { get; set; }
    public int Potency { get; set; }
    public int Duration { get; set; } // 0 = permanent (for equipment)
    public bool IsPercentage { get; set; } // flat vs percentage bonus

    // Constructor for easy creation of stat effects
    public StatEffect(StatType type, int potency, int duration = 0, bool isPercentage = false)
    {
        Type = type;
        Potency = potency;
        Duration = duration;
        IsPercentage = isPercentage;
    }
}

// Enumeration of all possible stats that can be modified
public enum StatType
{
    // Core stats
    Health,
    Attack,
    Defense,
    Speed,
    SkillDamage,

    // Attributes
    Vitality,
    Strength,
    Endurance,
    Dexterity,
    Agility,
    Intelligence
}

/****************************************************************************************/
// Collection of effects that can be applied/removed
public class StatModifierCollection
{
    // List of all stat effects in this collection
    public List<StatEffect> Effects { get; set; } = [];
    
    // Fluent API for adding effects
    public StatModifierCollection Add(StatType type, int potency, int duration = 0, bool isPercentage = false)
    {
        Effects.Add(new StatEffect(type, potency, duration, isPercentage));
        return this; // fluent API
    }

    // Apply all effects to the player (for equipment or consumables)
    public void ApplyTo(Player player)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(player, effect, add: true);
        }   
    }

    // Remove all effects from the player (for unequipping items)
    public void RemoveFrom(Player player)
    {
        foreach (var effect in Effects)
        {
            ApplyStat(player, effect, add: false);
        }
    }

    // Helper method to apply or remove a single stat effect
    private static void ApplyStat(Player player, StatEffect effect, bool add)
    {
        int value = add ? effect.Potency : -effect.Potency;

        // If it's a percentage bonus, calculate the actual value based on the player's current stat
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

/****************************************************************************************/
// Stackable items like potions or crafting materials
public abstract class StackableItem : BaseItem
{
    public int Quantity { get; set; }
    public int MaxStacks { get; set; }

    // Method to stack another item of the same type onto this one
    public int Stack(StackableItem other)
    {
        if (other == null) return 0;

        int space = MaxStacks - Quantity;
        if (space <= 0) return other.Quantity;

        int toMove = Math.Min(space, other.Quantity);
        Quantity += toMove;
        other.Quantity -= toMove;
        return other.Quantity;
    }
}

/****************************************************************************************/
// Equipment items that provide stat bonuses when equipped
public abstract class Equipment : BaseItem
{
    public int RequiredLevel { get; set; }
    public string? EquipmentType { get; set; }
    public StatModifierCollection Bonuses { get; set; } = new();
}

/****************************************************************************************/
// Consumable items that apply temporary or instant effects when used
public abstract class Consumable : StackableItem
{
    public string? ConsumableType { get; set; }
    public string? EffectDescription { get; set; }
    public StatModifierCollection Effects { get; set; } = new();

    public virtual void Use(Player player)
    {
        if (Quantity <= 0) return;

        Effects.ApplyTo(player);
        Quantity--;
    }
}

/****************************************************************************************/