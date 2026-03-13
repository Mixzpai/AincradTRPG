using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Items.Definitions.Weapons;

/// <summary>
/// Convenience wrapper that delegates to the per-weapon-class definition files.
/// Use the individual classes (e.g. OneHandedSwordDefinitions) for new weapons.
/// </summary>
public static class WeaponDefinitions
{
    public static Weapon CreateIronSword() => OneHandedSwordDefinitions.CreateIronSword();
    public static Weapon CreateSteelSword() => OneHandedSwordDefinitions.CreateSteelSword();
}
