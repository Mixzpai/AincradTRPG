namespace SAOTRPG.Items.Consumables;

// SAO Hollow Fragment canon corruption mechanic. Applied to a specific base
// weapon in the player's inventory, transforms it into the matching Corrupted
// variant. Because our F100 ends the game, the post-F100 boss that canonically
// distributes Corrupted Elucidator / Dark Repulser is unreachable — so
// corruption is modelled as a consumable-on-weapon transformation instead.
//
// Drop placement: F95+ field bosses at 10% rate (see TurnManager.Combat).
// Dispatcher: TurnManager.ConsumableUsed → HandleCorruptionStone.
public class CorruptionStone : Consumable
{
    // DefId of the base weapon this stone can transform (e.g. "elucidator").
    public string? TargetWeaponDefId { get; set; }

    // DefId of the resulting Corrupted weapon after transformation
    // (e.g. "ohs_corrupted_elucidator").
    public string? CorruptedWeaponDefId { get; set; }
}
