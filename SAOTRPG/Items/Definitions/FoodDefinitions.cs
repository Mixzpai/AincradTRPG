using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

// Static registry of all food items.
public static class FoodDefinitions
{
    private static Food Make(string id, string name, int value, string rarity,
        string foodType, int regenRate, int regenDuration, string effect,
        int maxStacks = 99)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = maxStacks,
            ConsumableType = "Food", FoodType = foodType,
            RegenerationRate = regenRate, RegenerationDuration = regenDuration,
            EffectDescription = effect,
        };

    public static Food CreateBread() => Make("bread", "Bread", 10, "Common",
        "Bread", 2, 10, "Regenerates 2 HP per second for 10 seconds.");

    public static Food CreateGrilledMeat() => Make("grilled_meat", "Grilled Meat", 30, "Common",
        "Meat", 5, 15, "Regenerates 5 HP per second for 15 seconds.");

    public static Food CreateHoneyBread() => Make("honey_bread", "Honey Bread", 15, "Common",
        "Bread", 1, 20, "Regenerates 1 HP per turn for 20 turns.");

    public static Food CreateFishStew() => Make("fish_stew", "Fish Stew", 45, "Uncommon",
        "Stew", 8, 8, "Regenerates 8 HP per turn for 8 turns.");

    public static Food CreateElvenWaybread() => Make("elven_waybread", "Elven Waybread", 60, "Uncommon",
        "Bread", 3, 30, "Regenerates 3 HP per turn for 30 turns.");

    public static Food CreateSpicedJerky() => Make("spiced_jerky", "Spiced Jerky", 20, "Common",
        "Meat", 4, 8, "Regenerates 4 HP per turn for 8 turns.");

    // Kirito's cheap staple from the Tolbana bakery. 5 Col a piece, canon.
    public static Food CreateCreamFilledBread() => Make("cream_filled_bread", "Cream-Filled Bread", 5, "Common",
        "Bread", 3, 10, "Tolbana bakery staple. Regenerates 3 HP per turn for 10 turns.");

    // F22 dairy quest canon — black bread dipped in high-class cream.
    public static Food CreateBlackBreadWithCream() => Make("black_bread_cream", "Black Bread with High-Class Cream", 120, "Rare",
        "Bread", 6, 25, "F22 cottage breakfast. Regenerates 6 HP per turn for 25 turns.", maxStacks: 20);

    // F22 dairy-floor dessert.
    public static Food CreateHoneyPie() => Make("honey_pie", "Honey Pie", 55, "Uncommon",
        "Pie", 4, 18, "Sweet honey pie from the F22 meadows. Regenerates 4 HP for 18 turns.", maxStacks: 20);

    // Wild greens mix — Asuna's cooking-skill starter recipe.
    public static Food CreateAincradSalad() => Make("aincrad_salad", "Aincrad Salad", 25, "Common",
        "Salad", 2, 12, "Fresh greens from the wilds of Aincrad. Regenerates 2 HP for 12 turns.");

    // Asuna's F74 field lunch (anime S1E9).
    public static Food CreateAsunasSandwich() => Make("asunas_sandwich", "Asuna's Sandwich", 80, "Uncommon",
        "Sandwich", 5, 15, "Wrapped in cloth with a ribbon. Regenerates 5 HP for 15 turns.", maxStacks: 20);

    // Legendary — S-class ingredient. Asuna's iconic F35 dish (LN V1 Aincrad).
    public static Food CreateRagoutRabbitStew() => Make("ragout_rabbit_stew", "Ragout Rabbit Stew", 3000, "Legendary",
        "Stew", 20, 40, "S-class dish. Regenerates 20 HP per turn for 40 turns.", maxStacks: 1);

    // Grilled meat on a stick — generic tavern fare.
    public static Food CreateGrilledSkewer() => Make("grilled_skewer", "Grilled Meat Skewer", 35, "Common",
        "Meat", 4, 12, "Savory skewer from the tavern grill. Regenerates 4 HP for 12 turns.");

    // Christmas-event cookies. (Nicholas the Renegade floor event.)
    public static Food CreateGingerbreadCookies() => Make("gingerbread_cookies", "Gingerbread Cookies", 60, "Uncommon",
        "Dessert", 3, 20, "Christmas Eve treat. Regenerates 3 HP for 20 turns.", maxStacks: 20);

    // ── Drinks ──────────────────────────────────────────────────────
    public static Food CreateLindasWine() => Make("lindas_wine", "Lindas Wine", 120, "Uncommon",
        "Drink", 2, 25, "F48 vintage. Regenerates 2 HP and settles the nerves for 25 turns.", maxStacks: 20);

    public static Food CreateHerbalTea() => Make("herbal_tea", "Herbal Tea", 20, "Common",
        "Drink", 2, 15, "Calming tea brewed from wild herbs. Regenerates 2 HP for 15 turns.");

    public static Food CreateWarmMilk() => Make("warm_milk", "Warm Milk", 15, "Common",
        "Drink", 3, 10, "F22 dairy. Regenerates 3 HP for 10 turns.");

    public static Food CreateHotChocolate() => Make("hot_chocolate", "Hot Chocolate", 40, "Uncommon",
        "Drink", 4, 15, "Seasonal drink. Regenerates 4 HP for 15 turns.", maxStacks: 20);

    public static Food CreateAle() => Make("ale", "Tavern Ale", 25, "Common",
        "Drink", 1, 20, "Cheap ale. Regenerates 1 HP for 20 turns.");

    public static Food CreateGrapeJuice() => Make("grape_juice", "Grape Juice", 18, "Common",
        "Drink", 2, 12, "Safe-zone refreshment. Regenerates 2 HP for 12 turns.");
}
