namespace SAOTRPG.Items.Consumables;

// SAO instant-effect consumables (Teleport/Corridor/AntiCrystal/Mirage/Heal/Antidote/ParalysisCure/Pneuma).
// Distinct from Potion so crystal-specific dispatchers can key off type cleanly.
public class Crystal : Consumable
{
    // Dispatch key in TurnManager.Consumables: Teleport/Corridor/AntiCrystal/Healing/Mana/Antidote/ParalysisCure/Mirage/Revive/Pneuma.
    public string? CrystalType { get; set; }

    // Destination city name for Teleport Crystals (canon: each city has its own).
    public string? Destination { get; set; }

    // Flat HP/MP/status restoration magnitude (interpreted per CrystalType).
    public int Magnitude { get; set; }
}
