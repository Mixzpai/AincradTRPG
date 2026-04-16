namespace SAOTRPG.Items.Consumables;

// Throwable items that deal damage (Fire Bomb, Poison Vial, etc.).
public class DamageItem : Consumable
{
    // Raw damage dealt on impact before target defense.
    public int BaseDamage { get; set; }
    // Element or damage category (e.g. "Fire", "Physical").
    public string? DamageType { get; set; }
    // Blast radius in tiles. 0 = single target, 1+ = splash.
    public int AreaOfEffect { get; set; }
}
