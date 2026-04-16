namespace SAOTRPG.Systems;

// Cooking recipe tiers. Gold/Legendary gated behind having Asuna in the party
// (her canonical Extra Skill: Cooking).
public enum RecipeTier { Bronze, Silver, Gold, Legendary }

// A cooking recipe: named inputs → single output. Output is also a
// DefinitionId resolved through ItemRegistry. Inputs are (DefId, count) pairs.
public record Recipe(
    string Id,
    string Name,
    (string DefId, int Count)[] Inputs,
    string OutputDefId,
    RecipeTier Tier,
    string Description
);

// Static recipe registry. Grouped by tier; Gold/Legendary require Asuna.
// All canonical where possible — see DEFERRED.md for the research sourcing.
public static class RecipeData
{
    public static readonly Recipe[] All =
    {
        // ── Bronze — low-skill kitchen basics ────────────────────────
        new("black_bread",  "Black Bread",  new[] { ("flour", 2) },
            "bread", RecipeTier.Bronze, "Flour + water basics. The cheapest meal in Aincrad."),
        new("wild_salad",   "Wild Salad",   new[] { ("wild_greens", 3) },
            "aincrad_salad", RecipeTier.Bronze, "Fresh greens mixed with a simple dressing."),
        new("herbal_tea",   "Herbal Tea",   new[] { ("tea_leaves", 1) },
            "herbal_tea", RecipeTier.Bronze, "A calming brew from wild herbs."),
        new("honey_toast",  "Honey Toast",  new[] { ("bread", 1), ("honey", 1) },
            "honey_bread", RecipeTier.Bronze, "Warm bread drizzled with honey."),
        new("grilled_skewer","Grilled Skewer", new[] { ("raw_meat", 1), ("salt", 1) },
            "grilled_skewer", RecipeTier.Bronze, "Savory skewer from the tavern grill."),

        // ── Silver — mid-tier, 3-4 ingredients ──────────────────────
        new("cream_filled", "Cream-Filled Bread", new[] { ("bread", 1), ("cream", 1) },
            "cream_filled_bread", RecipeTier.Silver, "Kirito's Tolbana staple."),
        new("fish_stew_rec","Fish Stew",   new[] { ("fish", 2), ("wild_greens", 1) },
            "fish_stew", RecipeTier.Silver, "Hearty stew from lake-caught fish."),
        new("honey_pie",    "Honey Pie",   new[] { ("flour", 2), ("honey", 2), ("fruit", 1) },
            "honey_pie", RecipeTier.Silver, "Sweet pie from the F22 meadows."),
        new("spiced_jerky", "Spiced Jerky", new[] { ("raw_meat", 2), ("spice", 1) },
            "spiced_jerky", RecipeTier.Silver, "Portable field ration."),
        new("warm_milk",    "Warm Milk",   new[] { ("milk", 2) },
            "warm_milk", RecipeTier.Silver, "F22 dairy. Calming, simple."),
        new("grilled_meat", "Grilled Meat",new[] { ("raw_meat", 1), ("salt", 1), ("spice", 1) },
            "grilled_meat", RecipeTier.Silver, "A well-seasoned cut off the fire."),

        // ── Gold — Asuna-gated, legendary skill tier ────────────────
        new("elven_waybread","Elven Waybread", new[] { ("flour", 2), ("elven_herb", 1), ("honey", 1) },
            "elven_waybread", RecipeTier.Gold, "Dark Elf travel bread. Long regen."),
        new("hot_chocolate", "Hot Chocolate", new[] { ("cocoa", 1), ("milk", 1), ("sugar", 1) },
            "hot_chocolate", RecipeTier.Gold, "Christmas-event warmth."),
        new("gingerbread",  "Gingerbread Cookies", new[] { ("flour", 1), ("ginger", 1), ("honey", 1), ("eggs", 1) },
            "gingerbread_cookies", RecipeTier.Gold, "Christmas Eve treat."),
        new("asunas_sandwich","Asuna's Sandwich Set", new[] { ("bread", 2), ("eggs", 2), ("raw_meat", 1), ("wild_greens", 2) },
            "asunas_sandwich", RecipeTier.Gold, "Wrapped in cloth with a ribbon. Party buff."),
        new("black_bread_cream", "Black Bread with High-Class Cream", new[] { ("bread", 1), ("high_cream", 1) },
            "black_bread_cream", RecipeTier.Gold, "F22 cottage breakfast. Near-full heal."),

        // ── Legendary — S-class ingredient gated ────────────────────
        new("ragout_rabbit", "Ragout Rabbit Stew", new[] { ("ragout_rabbit_meat", 1), ("high_cream", 1), ("spice", 1), ("wild_greens", 2) },
            "ragout_rabbit_stew", RecipeTier.Legendary, "Asuna's masterpiece. S-class."),
    };

    public static Recipe? Get(string id) => All.FirstOrDefault(r => r.Id == id);

    public static IEnumerable<Recipe> ByTier(RecipeTier tier) => All.Where(r => r.Tier == tier);
}
