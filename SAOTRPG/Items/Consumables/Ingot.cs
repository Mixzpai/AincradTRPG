using SAOTRPG.Items.Materials;

namespace SAOTRPG.Items.Consumables;

// IF Refinement Ingot — socketable into equipment's 3 RefinementSlots via Refinement.Socket (never Use()'d).
// Extends Material (not Consumable) — raw crafting-stat input, routed through Refinement vs Evolve.
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
