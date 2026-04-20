namespace SAOTRPG.Systems;

// FD Paired Dual-Wield registry. Both halves equipped → Pair Resonance:
// +10% combined dmg, +5% crit re-roll. First proc per encounter logs banner.
// Solo "Dual" weapons use Weapon.IsDualWieldPaired (offhand-legal, no synergy).
public static class DualWieldPairs
{
    // Symmetric pairs (both directions stored). Checked by HandleCombat.
    public static readonly Dictionary<string, string> Pairs = new()
    {
        // Kirito canon — Elucidator + Dark Repulser (SAO Aincrad-arc).
        ["elucidator"] = "dark_repulser",
        ["dark_repulser"] = "elucidator",

        // FD fire — Elucidator Rouge + Flare Pulsar.
        ["ohs_elucidator_rouge"] = "ohs_flare_pulsar",
        ["ohs_flare_pulsar"] = "ohs_elucidator_rouge",

        // Alicization Underworld Kirito — Black Iron Dual A/B.
        ["ohs_black_iron_dual_sword_a"] = "ohs_black_iron_dual_sword_b",
        ["ohs_black_iron_dual_sword_b"] = "ohs_black_iron_dual_sword_a",

        // Corrupted Kirito — Corruption Stone transforms of Elu/DR.
        ["ohs_corrupted_elucidator"] = "ohs_corrupted_dark_repulser",
        ["ohs_corrupted_dark_repulser"] = "ohs_corrupted_elucidator",
    };

    // Order-independent pair check (both directions in the dict).
    public static bool IsCanonicalPair(string? main, string? off)
        => main != null && off != null
           && Pairs.TryGetValue(main, out var partner) && partner == off;
}
