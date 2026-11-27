using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions;

// Tool-slot light sources; the player emits no light of their own. Three tiers:
// Wooden (TOB starter), Iron Lantern (F10+), Crystal Lantern (F40+ endgame).
//
// LightingSystem clamps a source at MaxSourceRadius (16), and its penumbra only softens a source
// of radius 6 or more, so the starter torch sits just above that gate and the endgame lantern
// just under the ceiling.
public static class TorchDefinitions
{
    private static Torch Make(
        string id, string name, int value, string rarity,
        int maxDurability, int lightRadius, int requiredLevel)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            ItemDurability = maxDurability, MaxDurability = maxDurability,
            LightRadius = lightRadius, RequiredLevel = requiredLevel,
        };

    // Starter light — every run begins with one equipped, and vendors stock it from floor 1.
    // Cheap and brittle, so running out is a real event.
    public static Torch CreateWoodenTorch() => Make(
        "wooden_torch", "Wooden Torch", 40, "Common",
        maxDurability: 60, lightRadius: 7, requiredLevel: 1);

    // Mid-tier. Twice the burn and a pool wide enough to read a room from its doorway.
    public static Torch CreateIronLantern() => Make(
        "iron_lantern", "Iron Lantern", 400, "Uncommon",
        maxDurability: 140, lightRadius: 11, requiredLevel: 5);

    // Endgame. Just under the renderer's own source ceiling.
    public static Torch CreateCrystalLantern() => Make(
        "crystal_lantern", "Crystal Lantern", 2200, "Rare",
        maxDurability: 300, lightRadius: 15, requiredLevel: 25);
}
