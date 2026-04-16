namespace SAOTRPG.Items.Consumables;

// SAO-iconic instant-effect consumables: Teleport, Corridor, Anti-Crystal,
// Mirage Sphere, Healing, Antidote, Paralysis Cure, Pneuma Flower.
// Treated separately from Potion so crystal-specific dispatchers can key off
// type cleanly in TurnManager.
public class Crystal : Consumable
{
    // Sub-type drives the dispatcher in TurnManager.Consumables:
    // "Teleport", "Corridor", "AntiCrystal", "Healing", "Mana",
    // "Antidote", "ParalysisCure", "Mirage", "Revive", "Pneuma".
    public string? CrystalType { get; set; }

    // Destination city name for Teleport Crystals (canon: each city has its own).
    public string? Destination { get; set; }

    // Flat HP/MP/status restoration magnitude (interpreted per CrystalType).
    public int Magnitude { get; set; }
}
