namespace SAOTRPG.Items.Consumables;

// Consumables that provide regeneration or temporary buffs.
public class Food : Consumable
{
    // HP restored per tick while the regeneration buff is active.
    public int RegenerationRate { get; set; }
    // Number of turns the regeneration buff lasts after consumption.
    public int RegenerationDuration { get; set; }
    // Food category (e.g. "Bread", "Meat") — cosmetic label for UI.
    public string? FoodType { get; set; }
}