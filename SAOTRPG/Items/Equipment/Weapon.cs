namespace YourGame.Items.Equipment;

/// <summary>
/// Equipment that increases attack power.
/// </summary>
public class Weapon : Equipment
{
    public int BaseDamage { get; set; }
    public string? WeaponType { get; set; }
    public int AttackSpeed { get; set; }
    public int Range { get; set; }
}