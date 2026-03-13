namespace SAOTRPG.Items.Consumables;

/// <summary>
/// Consumables that provide regeneration or temporary buffs.
/// </summary>
public class Food : Consumable
{
    public int RegenerationRate { get; set; }
    public int RegenerationDuration { get; set; }
    public string? FoodType { get; set; }
}