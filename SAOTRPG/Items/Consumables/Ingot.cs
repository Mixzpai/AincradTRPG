using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Consumables;

// IF-canon Refinement Ingot. Stackable, socketable "consumable" — consumed by
// being socketed into one of an equipment's 3 RefinementSlots (see
// Refinement.Socket). Unlike Potion/Crystal it is never Use()'d directly — the
// refinement helper reads the primary/secondary/third stat bonuses below and
// folds them into the target equipment's Bonuses collection.
//
// Extends Material (not Consumable) because ingots have no per-use effect
// dispatch — they are raw crafting-stat inputs, similar to EvolutionMaterial
// but routed through the Refinement system instead of Evolve recipes.
public class Ingot : Material
{
    // Positive effect (what the player wants).
    public StatType PrimaryStat { get; set; }
    public int PrimaryBonus { get; set; }

    // Negative tradeoff. Typically small for rare+ ingots.
    public StatType SecondaryStat { get; set; }
    public int SecondaryBonus { get; set; }   // stored negative

    // Multi-stat ingots (Rare/Epic/Legendary). Null for simple Common ingots.
    public StatType? ThirdStat { get; set; }
    public int ThirdBonus { get; set; }       // can be + or -

    // Optional fourth stat for top-tier legendaries.
    public StatType? FourthStat { get; set; }
    public int FourthBonus { get; set; }

    // Human-readable one-line summary (e.g. "Attack +10 / Speed -3").
    public string? EffectDescription { get; set; }
}
