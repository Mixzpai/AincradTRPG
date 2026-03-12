using YourGame.Items.Consumables;

namespace YourGame.Items.Definitions;

/// <summary>
/// Static registry of all food items.
/// </summary>
public static class FoodDefinitions
{
    public static Food Bread => new()
    {
        Name = "Bread",
        Value = 10,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        ConsumableType = "Food",
        FoodType = "Bread",
        RegenerationRate = 2,
        RegenerationDuration = 10,
        EffectDescription = "Regenerates 2 HP per second for 10 seconds."
    };

    public static Food GrilledMeat => new()
    {
        Name = "Grilled Meat",
        Value = 30,
        Rarity = "Common",
        Quantity = 1,
        MaxStacks = 99,
        ConsumableType = "Food",
        FoodType = "Meat",
        RegenerationRate = 5,
        RegenerationDuration = 15,
        EffectDescription = "Regenerates 5 HP per second for 15 seconds."
    };
}