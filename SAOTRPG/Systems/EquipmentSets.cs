using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems;

// Armour set bonuses. Four material tiers already ship a matched Chest / Helmet / Boots trio, and
// those three occupy DIFFERENT equipment slots, so a full set is wearable rather than aspirational.
// Membership is read off the pieces that exist rather than invented: no new items are added here.
//
// The bonus is deliberately a REASON TO MATCH rather than a reason to ignore better gear. Each
// tier's 2-piece is worth roughly a fifth of one piece's Defense and the 3-piece roughly a half,
// so mixing a higher-tier chest into a lower-tier set stays the right call whenever the raw stat
// gap is wide — the set is a tiebreak, not a trap.
public static class EquipmentSets
{
    public sealed record SetBonus(StatType Stat, int Value);

    public sealed record SetDef(
        string Name,
        string Flavor,
        string[] MemberDefIds,
        SetBonus[] TwoPiece,
        SetBonus[] ThreePiece);

    public static readonly SetDef[] All =
    {
        // Steel: per-piece Defense runs 15/12/10, so 3 + 8 is ~20% / ~55% of a single piece.
        new("Steel Panoply", "Plain, matched, and heavier than it looks.",
            new[] { "steel_chestplate", "steel_helmet", "steel_boots" },
            new[] { new SetBonus(StatType.Defense, 3) },
            new[] { new SetBonus(StatType.Defense, 8), new SetBonus(StatType.Endurance, 3) }),

        // Mythril: 30/24/20 Defense per piece.
        new("Mythril Weave", "Light enough to run in, cold to the touch.",
            new[] { "mythril_chestplate", "mythril_helmet", "mythril_boots" },
            new[] { new SetBonus(StatType.Defense, 6) },
            new[] { new SetBonus(StatType.Defense, 15), new SetBonus(StatType.Agility, 6),
                    new SetBonus(StatType.Speed, 4) }),

        // Adamantite: 55/42/35 Defense per piece.
        new("Adamantite Bulwark", "A wall that happens to be shaped like a person.",
            new[] { "adamantite_chestplate", "adamantite_helmet", "adamantite_boots" },
            new[] { new SetBonus(StatType.Defense, 11) },
            new[] { new SetBonus(StatType.Defense, 28), new SetBonus(StatType.Vitality, 10),
                    new SetBonus(StatType.Endurance, 8) }),

        // Celestial: 90/70/58 Defense per piece — the endgame trio.
        new("Celestial Regalia", "Worn by nobody the records name.",
            new[] { "celestial_chestplate", "celestial_helmet", "celestial_boots" },
            new[] { new SetBonus(StatType.Defense, 18) },
            new[] { new SetBonus(StatType.Defense, 45), new SetBonus(StatType.Vitality, 15),
                    new SetBonus(StatType.Intelligence, 10), new SetBonus(StatType.Speed, 8) }),
    };

    // Sets with at least two pieces worn, and how many. Callers pass the equipped items they
    // consider live — Inventory excludes anything at zero durability, because broken gear grants
    // no bonuses and must not count toward a set either.
    public static List<(SetDef Set, int Worn)> ActiveSets(IEnumerable<EquipmentBase> equipped)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var eq in equipped)
            if (!string.IsNullOrEmpty(eq.DefinitionId)) ids.Add(eq.DefinitionId);

        var active = new List<(SetDef, int)>();
        foreach (var set in All)
        {
            int worn = 0;
            foreach (string id in set.MemberDefIds)
                if (ids.Contains(id)) worn++;
            if (worn >= 2) active.Add((set, worn));
        }
        return active;
    }

    // Flat bonuses granted by everything currently active. The 3-piece REPLACES the 2-piece rather
    // than stacking on it, so each tier's full-set line reads as the whole reward.
    public static IEnumerable<SetBonus> ActiveBonuses(IEnumerable<EquipmentBase> equipped)
    {
        foreach (var (set, worn) in ActiveSets(equipped))
            foreach (var b in worn >= set.MemberDefIds.Length ? set.ThreePiece : set.TwoPiece)
                yield return b;
    }
}
