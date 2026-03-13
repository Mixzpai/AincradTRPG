namespace SAOTRPG.Items.Equipment;

/// <summary>
/// Equipment that increases attack power.
/// </summary>
public class Weapon : EquipmentBase
{
    public int BaseDamage { get; set; }
    public string? WeaponType { get; set; }
    public int AttackSpeed { get; set; }
    public int Range { get; set; }
}