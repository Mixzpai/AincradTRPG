using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all potions.
/// </summary>
public static class PotionDefinitions
{
    public static Potion CreateHealthPotion() => new()
    {
        DefinitionId = "health_potion",
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

    public static Potion CreateGreaterHealthPotion() => new()
    {
        DefinitionId = "greater_health_potion",
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

    public static Potion CreateAntidote() => new()
    {
        DefinitionId = "antidote",
        Name = "Antidote",
        Value = 30,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        ConsumableType = "Potion",
        PotionType = "Antidote",
        Cooldown = 0,
        EffectDescription = "Cures poison and bleed."
    };

    public static Potion CreateBattleElixir() => new()
    {
        DefinitionId = "battle_elixir",
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