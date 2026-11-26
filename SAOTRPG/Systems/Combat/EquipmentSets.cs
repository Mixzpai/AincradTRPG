using SAOTRPG.Items;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Systems.Combat;

// Armour set bonuses. Four material tiers already ship a matched Chest / Helmet / Boots trio, and
// those three occupy DIFFERENT equipment slots, so a full set is wearable rather than aspirational.
// Membership is read off the pieces that exist rather than invented: no new items are added here.
//
// The bonus is deliberately a REASON TO MATCH rather than a reason to ignore better gear. Each
// tier's 2-piece is worth roughly a fifth of one piece's Defense and the full set roughly a half,
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
            new[] { "steel_chestplate", "steel_helmet", "steel_legs", "steel_boots" },
            new[] { new SetBonus(StatType.Defense, 3) },
            new[] { new SetBonus(StatType.Defense, 8), new SetBonus(StatType.Endurance, 3) }),

        // Mythril: 30/24/20 Defense per piece.
        new("Mythril Weave", "Light enough to run in, cold to the touch.",
            new[] { "mythril_chestplate", "mythril_helmet", "mythril_legs", "mythril_boots" },
            new[] { new SetBonus(StatType.Defense, 6) },
            new[] { new SetBonus(StatType.Defense, 15), new SetBonus(StatType.Agility, 6),
                    new SetBonus(StatType.Speed, 4) }),

        // Adamantite: 55/42/35 Defense per piece.
        new("Adamantite Bulwark", "A wall that happens to be shaped like a person.",
            new[] { "adamantite_chestplate", "adamantite_helmet", "adamantite_legs", "adamantite_boots" },
            new[] { new SetBonus(StatType.Defense, 11) },
            new[] { new SetBonus(StatType.Defense, 28), new SetBonus(StatType.Vitality, 10),
                    new SetBonus(StatType.Endurance, 8) }),

        // Celestial: 90/70/58 Defense per piece — the endgame trio.
        new("Celestial Regalia", "Worn by nobody the records name.",
            new[] { "celestial_chestplate", "celestial_helmet", "celestial_legs", "celestial_boots" },
            new[] { new SetBonus(StatType.Defense, 18) },
            new[] { new SetBonus(StatType.Defense, 45), new SetBonus(StatType.Vitality, 15),
                    new SetBonus(StatType.Intelligence, 10), new SetBonus(StatType.Speed, 8) }),
    };

    // Every member DefinitionId → (which set, a flat index over all members). Built once so
    // counting worn pieces is one dictionary probe per equipped item instead of a fresh HashSet
    // of the wearer's ids plus a lookup per member.
    private static readonly Dictionary<string, (int SetIdx, int Flat)> MemberIndex = BuildMemberIndex();
    private static readonly int MemberCount = MemberIndex.Count;

    private static Dictionary<string, (int, int)> BuildMemberIndex()
    {
        var map = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        int flat = 0;
        for (int s = 0; s < All.Length; s++)
            foreach (string id in All[s].MemberDefIds)
                map[id] = (s, flat++);
        return map;
    }

    // Distinct member pieces worn, per set, written into a caller-owned buffer indexed the same
    // way as All. Allocation-free: the stat-bonus cache rebuilds through here on every equip,
    // unequip, break and refinement.
    //
    // Counts DISTINCT DefinitionIds, matching the set-of-ids this replaced — wearing the same
    // piece twice must not count twice. Callers pass the items they consider live; Inventory
    // excludes anything at zero durability, because broken gear grants no bonuses and must not
    // count toward a set either.
    public static void CountWorn(List<EquipmentBase> equipped, Span<int> counts)
    {
        counts.Clear();
        Span<bool> seen = stackalloc bool[MemberCount];
        for (int i = 0; i < equipped.Count; i++)
        {
            string? id = equipped[i].DefinitionId;
            if (string.IsNullOrEmpty(id)) continue;
            if (!MemberIndex.TryGetValue(id, out var where)) continue;
            if (seen[where.Flat]) continue;
            seen[where.Flat] = true;
            counts[where.SetIdx]++;
        }
    }

    // The bonuses a set grants at `worn` pieces, or null below the two-piece threshold. The 3-piece
    // REPLACES the 2-piece rather than stacking on it, so each tier's full-set line reads as the
    // whole reward. Both consumers below route through here, so that rule lives in one place.
    public static SetBonus[]? BonusesFor(SetDef set, int worn) =>
        worn < 2 ? null : (worn >= set.MemberDefIds.Length ? set.ThreePiece : set.TwoPiece);

    // Sets with at least two pieces worn, and how many.
    public static List<(SetDef Set, int Worn)> ActiveSets(IEnumerable<EquipmentBase> equipped)
    {
        var list = equipped as List<EquipmentBase> ?? new List<EquipmentBase>(equipped);
        Span<int> worn = stackalloc int[All.Length];
        CountWorn(list, worn);

        var active = new List<(SetDef, int)>();
        for (int i = 0; i < All.Length; i++)
            if (worn[i] >= 2) active.Add((All[i], worn[i]));
        return active;
    }

    // Flat bonuses granted by everything currently active.
    public static IEnumerable<SetBonus> ActiveBonuses(IEnumerable<EquipmentBase> equipped)
    {
        foreach (var (set, worn) in ActiveSets(equipped))
            foreach (var b in BonusesFor(set, worn)!)
                yield return b;
    }
}
