using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

/// <summary>
/// Static registry of all throwable damage items.
/// </summary>
public static class DamageItemDefinitions
{
    public static DamageItem CreateFireBomb() => new()
    {
        DefinitionId = "fire_bomb",
        Name = "Fire Bomb",
        Value = 50,
        Rarity = "Uncommon",
        Quantity = 1,
        MaxStacks = 20,
        ConsumableType = "Throwable",
        DamageType = "Fire",
        BaseDamage = 30,
        AreaOfEffect = 3,
        EffectDescription = "Deals 30 fire damage in a 3-unit radius."
    };

    public static DamageItem CreatePoisonVial() => new()
    {
        DefinitionId = "poison_vial",
        Name = "Poison Vial",
        Value = 40,
        Rarity = "Uncommon",
        Quantity = 1,
        MaxStacks = 20,
        ConsumableType = "Throwable",
        DamageType = "Poison",
        BaseDamage = 10,
        AreaOfEffect = 2,
        EffectDescription = "Deals 10 poison damage and applies poison effect."
    };
}