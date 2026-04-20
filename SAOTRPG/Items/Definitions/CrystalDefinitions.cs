using SAOTRPG.Items.Consumables;

namespace SAOTRPG.Items.Definitions;

// SAO-iconic instant crystals; CrystalType drives TurnManager dispatch. Destination used by Teleport only.
public static class CrystalDefinitions
{
    private static Crystal Make(string id, string name, int value, string rarity,
        int maxStacks, string crystalType, string effect,
        string? destination = null, int magnitude = 0,
        StatModifierCollection? effects = null)
        => new()
        {
            DefinitionId = id, Name = name, Value = value, Rarity = rarity,
            Quantity = 1, MaxStacks = maxStacks,
            ConsumableType = "Crystal", CrystalType = crystalType,
            Destination = destination, Magnitude = magnitude,
            EffectDescription = effect,
            Effects = effects ?? new StatModifierCollection(),
        };

    public static Crystal CreateTeleportCrystal(string city) => Make(
        $"teleport_crystal_{city.ToLowerInvariant().Replace(' ', '_')}",
        $"Teleport Crystal ({city})", 400, "Uncommon", 10,
        "Teleport", $"Instantly return to {city}.", destination: city);

    public static Crystal CreateCorridorCrystal() => Make("corridor_crystal", "Corridor Crystal", 1200, "Rare", 5,
        "Corridor", "Opens a portal back to the entrance of this floor for 60 seconds.");

    public static Crystal CreateAntiCrystal() => Make("anti_crystal", "Anti-Crystal", 2000, "Rare", 3,
        "AntiCrystal", "Suppresses teleport crystals in a wide area. A Laughing Coffin favourite.");

    public static Crystal CreateHealingCrystal() => Make("healing_crystal", "Healing Crystal", 200, "Uncommon", 20,
        "Healing", "Voice command. Restores 100 HP instantly.",
        magnitude: 100,
        effects: new StatModifierCollection().Add(StatType.Health, 100));

    public static Crystal CreateHighHealingCrystal() => Make("high_healing_crystal", "High Healing Crystal", 600, "Rare", 10,
        "Healing", "Voice command. Restores 300 HP instantly.",
        magnitude: 300,
        effects: new StatModifierCollection().Add(StatType.Health, 300));

    public static Crystal CreateAntidoteCrystal() => Make("antidote_crystal", "Antidote Crystal", 180, "Uncommon", 10,
        "Antidote", "Voice command. Cures poison and bleed immediately.");

    public static Crystal CreateParalysisCureCrystal() => Make("paralysis_cure_crystal", "Paralysis Cure Crystal", 250, "Uncommon", 10,
        "ParalysisCure", "Voice command. Cures stun and slow immediately.");

    public static Crystal CreateMirageSphere() => Make("mirage_sphere", "Mirage Sphere", 2500, "Epic", 3,
        "Mirage", "Records the next combat encounter for later playback. Evidence against PKers.");

    public static Crystal CreatePneumaFlower() => Make("pneuma_flower", "Pneuma Flower", 5000, "Legendary", 1,
        "Revive", "Revives a fallen ally to full health. Must be used within 10 turns of death.",
        magnitude: 9999,
        effects: new StatModifierCollection().Add(StatType.Health, 9999));

    public static Crystal CreateDivineStone() => Make("divine_stone_of_returning_soul", "Divine Stone of Returning Soul", 9999, "Legendary", 1,
        "Revive", "Revives a player within 10 seconds of death. Dropped by Nicholas the Renegade on Christmas Eve.",
        magnitude: 9999);
}
