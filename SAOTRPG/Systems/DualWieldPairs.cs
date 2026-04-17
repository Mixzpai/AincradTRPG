namespace SAOTRPG.Systems;

// FD Paired Dual-Wield registry. Canonical weapon pairs whose members are
// tuned to be wielded together. When both halves are equipped (one in
// MainHand, one in OffHand), TurnManager.Combat applies a "Pair Resonance"
// synergy: +10% total damage on the combined main+offhand swings and a
// +5% CritRate nudge (implemented as a crit re-roll bump when the base
// swing did not crit). The first synergy proc of each encounter logs a
// banner line so the player can verify the effect is firing.
//
// Solo "Dual"-flavored canon weapons (Chaos Raider, Murasama G4, Lightning
// Divider Dual, etc.) set IsDualWieldPaired = true on Weapon itself to
// bypass the DualBlades-unlock gate — they are offhand-legal without
// needing a partner — but they do NOT appear here because they have no
// canonical pair and therefore grant no synergy.
public static class DualWieldPairs
{
    // Canonical pairs — key + value are symmetric partners. When both are
    // equipped (one MainHand, one OffHand, order-independent), apply the
    // Pair Resonance synergy in TurnManager.Combat.HandleCombat.
    public static readonly Dictionary<string, string> Pairs = new()
    {
        // Kirito canon — Elucidator + Dark Repulser. The original SAO
        // Aincrad-arc dual wield.
        ["elucidator"] = "dark_repulser",
        ["dark_repulser"] = "elucidator",

        // FD fire variant — Elucidator Rouge + Flare Pulsar. Kirito's
        // fire-aligned Fractured Daydream pair.
        ["ohs_elucidator_rouge"] = "ohs_flare_pulsar",
        ["ohs_flare_pulsar"] = "ohs_elucidator_rouge",

        // Alicization Underworld Kirito — Black Iron Dual Sword A/B.
        ["ohs_black_iron_dual_sword_a"] = "ohs_black_iron_dual_sword_b",
        ["ohs_black_iron_dual_sword_b"] = "ohs_black_iron_dual_sword_a",
    };

    // True when the given main-hand and off-hand DefinitionIds form a
    // canonical pair (order-independent — both (a,b) and (b,a) return true
    // because the dictionary stores both directions).
    public static bool IsCanonicalPair(string? main, string? off)
        => main != null && off != null
           && Pairs.TryGetValue(main, out var partner) && partner == off;
}
