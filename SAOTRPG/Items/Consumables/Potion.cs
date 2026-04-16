namespace SAOTRPG.Items.Consumables;

// Healing and buff consumables (Health Potion, Speed Potion, etc.).
public class Potion : Consumable
{
    // Sub-type label for display/sorting (e.g. "Health", "Speed", "Iron Skin").
    public string? PotionType { get; set; }
    // Turns between uses. Used by save/load serialization.
    public int Cooldown { get; set; }
}
