namespace SAOTRPG.Items.Equipment;

// Tool-slot equipment, swung by bump-action against ore tiles.
// Durability ticks per strike; broken Pickaxe destroys, mining halts.
public class Pickaxe : EquipmentBase
{
    // Strikes-per-vein delta — subtracts from default vein strikes (clamps at tier min).
    public int MiningPower { get; set; }
    // % drop chance bonus added to vein loot rolls.
    public int OreQualityBonus { get; set; }

    public Pickaxe()
    {
        EquipmentType = "pickaxe";
        // Pickaxes don't grant combat stats — Bonuses left empty by default.
    }
}
