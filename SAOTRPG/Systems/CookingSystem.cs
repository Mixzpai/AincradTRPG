using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.UI;

namespace SAOTRPG.Systems;

// Cooking pipeline: ingredient check → consume → output. Gold/Legendary
// recipes require Asuna to be in the party (canonical Cooking Extra Skill).
public static class CookingSystem
{
    // Check whether the player can currently cook this recipe.
    public static bool CanCook(Recipe recipe, Player player, out string reason)
    {
        reason = "";
        if (recipe.Tier is RecipeTier.Gold or RecipeTier.Legendary && !HasAsuna())
        {
            reason = "Requires Asuna in the party (Cooking Extra Skill).";
            return false;
        }
        foreach (var (defId, count) in recipe.Inputs)
        {
            int have = player.Inventory.CountByDefinitionId(defId);
            if (have < count)
            {
                var sampleItem = ItemRegistry.Create(defId);
                string displayName = sampleItem?.Name ?? defId;
                reason = $"Missing: {displayName} ({have}/{count}).";
                return false;
            }
        }
        return true;
    }

    // Attempt to cook — consumes inputs and adds output to inventory. Returns
    // the crafted item (or null if it failed).
    public static BaseItem? Cook(Recipe recipe, Player player, IGameLog log)
    {
        if (!CanCook(recipe, player, out var reason))
        {
            log.Log($"Cannot cook {recipe.Name}: {reason}");
            return null;
        }
        foreach (var (defId, count) in recipe.Inputs)
            player.Inventory.ConsumeByDefinitionId(defId, count);

        var output = ItemRegistry.Create(recipe.OutputDefId);
        if (output == null)
        {
            log.Log($"Recipe output '{recipe.OutputDefId}' missing from registry.");
            return null;
        }
        player.Inventory.AddItem(output);
        log.LogLoot($"You cook {recipe.Name}! (+1 {output.Name})");
        return output;
    }

    // Asuna gating — Canon: her Cooking Extra Skill unlocks the top tiers.
    private static bool HasAsuna()
    {
        foreach (var ally in PartySystem.Members)
            if (ally.Name.Contains("Asuna", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
