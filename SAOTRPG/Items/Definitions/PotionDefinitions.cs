using YourGame.Items.Consumables;

namespace YourGame.Items.Definitions;

/// <summary>
/// Static registry of all potions.
/// </summary>
public static class PotionDefinitions
{
    public static Potion HealthPotion => new()
    {
        Name = "Health Potion",
        Value = 25,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        ConsumableType = "Potion",
        PotionType = "Healing",
        Cooldown = 5,
        EffectDescription = "Restores 50 HP instantly.",
        Effects = new StatModifierCollection()
            .Add(StatType.Health, 50)
    };

    public static Potion GreaterHealthPotion => new()
    {
        Name = "Greater Health Potion",
        Value = 75,
        Rarity = "Uncommon",
        Quantity = 1,
        MaxStacks = 99,
        ConsumableType = "Potion",
        PotionType = "Healing",
        Cooldown = 5,
        EffectDescription = "Restores 150 HP instantly.",
        Effects = new StatModifierCollection()
            .Add(StatType.Health, 150)
    };

    public static Potion BattleElixir => new()
    {
        Name = "Battle Elixir",
        Value = 150,
        Rarity = "Rare",
        Quantity = 1,
        MaxStacks = 20,
        ConsumableType = "Potion",
        PotionType = "Buff",
        Cooldown = 60,
        EffectDescription = "Increases Attack and Speed for 60 seconds.",
        Effects = new StatModifierCollection()
            .Add(StatType.Attack, 15, duration: 60)
            .Add(StatType.Speed, 10, duration: 60)
    };
}   