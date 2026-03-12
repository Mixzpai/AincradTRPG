namespace YourGame.Items.Consumables;

/// <summary>
/// Throwable or usable items that deal damage.
/// </summary>
public class DamageItem : Consumable
{
    public int BaseDamage { get; set; }
    public string? DamageType { get; set; }
    public int AreaOfEffect { get; set; }
}