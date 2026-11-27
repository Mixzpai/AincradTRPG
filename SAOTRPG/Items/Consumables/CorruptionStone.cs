namespace SAOTRPG.Items.Consumables;

// Consumable that transforms one target weapon into its Corrupted variant.
// Drops from F95+ field bosses at 10%; dispatched via TurnManager.HandleCorruptionStone.
public class CorruptionStone : Consumable
{
    // Target base weapon DefId (e.g. "elucidator").
    public string? TargetWeaponDefId { get; set; }

    // Resulting Corrupted weapon DefId (e.g. "ohs_corrupted_elucidator").
    public string? CorruptedWeaponDefId { get; set; }
}
