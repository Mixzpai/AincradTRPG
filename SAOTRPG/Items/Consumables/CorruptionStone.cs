namespace SAOTRPG.Items.Consumables;

// HF canon corruption: consumable-on-weapon transformation (post-F100 boss unreachable in our run).
// Drops from F95+ field bosses at 10%; dispatched via TurnManager.HandleCorruptionStone.
public class CorruptionStone : Consumable
{
    // Target base weapon DefId (e.g. "elucidator").
    public string? TargetWeaponDefId { get; set; }

    // Resulting Corrupted weapon DefId (e.g. "ohs_corrupted_elucidator").
    public string? CorruptedWeaponDefId { get; set; }
}
