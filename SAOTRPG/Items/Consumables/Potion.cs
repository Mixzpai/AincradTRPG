namespace SAOTRPG.Items.Consumables;

/// <summary>
/// Healing and buff consumables.
/// </summary>
public class Potion : Consumable
{
    public string? PotionType { get; set; }
    public int Cooldown { get; set; }
}