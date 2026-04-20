using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

// Canon HF post-F100 drops (Corrupted Elucidator/Dark Repulser); our F100 ends game,
// so corruption applied via Corruption Stone consumable on base weapon.
public static class CorruptionStoneDefinitions
{
    private static CorruptionStone Make(string id, string name, int value, string rarity,
        int maxStacks, string targetDefId, string corruptedDefId, string effect)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = maxStacks,
            ConsumableType = "CorruptionStone",
            TargetWeaponDefId = targetDefId,
            CorruptedWeaponDefId = corruptedDefId,
            EffectDescription = effect,
        };

    // Night Stone: Elucidator → Corrupted Elucidator; preserves enhance level + refinement slots.
    public static CorruptionStone CreateNightCorruptionStone() => Make(
        "night_corruption_stone", "Night Corruption Stone",
        18000, "Legendary", 3,
        targetDefId: "elucidator",
        corruptedDefId: "ohs_corrupted_elucidator",
        effect: "Transform Elucidator in inventory into Corrupted Elucidator. Irreversible.");

    // Shadow Corruption Stone — transforms Dark Repulser → Corrupted Dark Repulser.
    public static CorruptionStone CreateShadowCorruptionStone() => Make(
        "shadow_corruption_stone", "Shadow Corruption Stone",
        18000, "Legendary", 3,
        targetDefId: "dark_repulser",
        corruptedDefId: "ohs_corrupted_dark_repulser",
        effect: "Transform Dark Repulser in inventory into Corrupted Dark Repulser. Irreversible.");
}
