using SAOTRPG.Map.Generation.Prefabs;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.RunState;

namespace SAOTRPG.Map.Generation.Passes;

// Places the landmark authored for this floor's biome, before the general prefab pass runs.
//
// The general pass draws its open-ground landmarks from every prefab whose floor range covers the
// floor, with a small budget of picks, so a biome's own signature piece is a few percent of the
// weighted draw and most floors would never carry theirs.
//
// Two things this pass does that the weighted route cannot:
//   * it selects from the biome's own signature pool rather than the whole library, and
//   * it tests anchors until it has enough host sites rather than throwing a fixed number of
//     random darts. On a cave floor the sites that can host a landmark are a tiny fraction of the
//     open ground, so it ends up sweeping the whole floor there and stopping after a handful of
//     tests on open terrain -- the same reason ore-vein seeding scans rather than throws darts.
public sealed class SignaturePrefabPass : IGenerationPass
{
    public string Name => "SignaturePrefab";

    // Anchors are sampled on a stride; a landmark is a large footprint, so sites a few tiles apart
    // are redundant. The fine pass is a fallback for a floor whose only host sites fall between the
    // coarse samples, which happens on cave floors.
    private const int CoarseStride = 6;
    private const int FineStride = 2;

    // How many host sites to collect per candidate before choosing. The site test is a necessary
    // condition, not a sufficient one — the disk mask and the room rects still get a veto — so the
    // sample has to be deep enough that the placer is not left with a handful of near-misses.
    private const int SiteSample = 128;

    private static readonly string[] SignatureTag = { PrefabLibrary.SignatureTag };

    // Matches PrefabPlacementPass: F100 bypasses the pipeline entirely, and a hand-authored
    // SafeZone floor (F1 Town of Beginnings, F48 Lindarth) must not be stamped over. Reads state
    // SpecialAreaPass has already written and draws no rng, so it stays pure.
    public bool ShouldRun(WorldContext ctx) => ctx.FloorNumber != 100 && ctx.Map.SafeZone == null;

    public void Execute(WorldContext ctx)
    {
        PrefabLibrary.Shared.LoadAll();
        if (PrefabLibrary.Shared.Count == 0) return;

        string biome = ctx.Biome.ToString().ToLowerInvariant();
        var pool = PrefabLibrary.Shared.CandidatesFor(biome, ctx.FloorNumber, SignatureTag)
            .Where(p => p.Orient != PrefabOrient.Encompass)
            .ToList();
        if (pool.Count == 0)
        {
            // Loud rather than silent: a biome with no signature piece for this floor is a content
            // hole, and the floor generates perfectly well without one.
            DebugLogger.LogGame("PREFAB",
                $"floor={ctx.FloorNumber} biome={biome} has no signature prefab — no landmark guaranteed");
            return;
        }

        // Its own stream, derived from (globalSeed, floor) the same way SeedDerivationPass and the
        // prefab vendor stock derive theirs. Two reasons it is not ctx.Rng: a draw here would
        // renumber every later pass and move the whole floor, and the seed must come from the
        // context rather than RunRng.Seed, which is the run's seed -- a floor built ahead of the
        // player runs on a worker and can be handed a different one.
        var rng = new Random(RunRng.StableHash(
            RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber), "signature-prefab", 0));

        if (TryPlaceFrom(ctx, pool, rng, CoarseStride)) return;
        if (TryPlaceFrom(ctx, pool, rng, FineStride)) return;

        DebugLogger.LogGame("PREFAB",
            $"floor={ctx.FloorNumber} biome={biome} no site for any of {pool.Count} signature "
            + "prefab(s) — floor has no biome landmark");
    }

    private static bool TryPlaceFrom(WorldContext ctx, List<PrefabDefinition> pool, Random rng, int stride)
    {
        var anchors = PrefabPlacer.CollectOpenGroundAnchors(ctx, stride);
        if (anchors.Count == 0) return false;

        // Shuffled once, so the capped scan below is a uniform sample of the floor rather than its
        // top-left corner. Durstenfeld on the list the pass already owns; no allocation per pick.
        for (int i = anchors.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (anchors[i], anchors[j]) = (anchors[j], anchors[i]);
        }

        // Filter per candidate first, then choose among what the terrain can actually take. The
        // order matters: choosing first and discovering the terrain second means a cave floor
        // pays a full failed search for a large piece that never fits on it.
        //
        // The scan stops at SiteSample hits. An early floor carries tens of thousands of anchors
        // and the footprint test does not short-circuit on open ground, while the placer takes
        // one entry from the list.
        var feasible = new List<(PrefabDefinition Def, List<(int X, int Y)> Sites)>();
        foreach (var def in pool)
        {
            int side = Math.Min(def.Width, def.Height);
            var sites = new List<(int X, int Y)>();
            foreach (var a in anchors)
            {
                if (!PrefabPlacer.CouldHostFootprint(ctx.Map, a.X, a.Y, side)) continue;
                sites.Add(a);
                if (sites.Count >= SiteSample) break;
            }
            if (sites.Count > 0) feasible.Add((def, sites));
        }
        if (feasible.Count == 0) return false;

        // Weighted draw without replacement, so a biome with two signature pieces gets both across
        // a climb rather than always the same one.
        while (feasible.Count > 0)
        {
            int idx = WeightedIndex(feasible, rng);
            var (def, sites) = feasible[idx];
            feasible.RemoveAt(idx);

            if (!ctx.TryUsePrefab(def.Name, def.MaxPerGame)) continue;
            var spawnQueue = new List<PrefabSpawnRequest>();
            if (PrefabPlacer.TryPlaceFloating(ctx.Map, def, rng, sites, ctx.Rooms, ctx, spawnQueue,
                                              attempts: sites.Count))
            {
                DebugLogger.LogGame("PREFAB",
                    $"floor={ctx.FloorNumber} signature landmark '{def.Name}' placed "
                    + $"(stride {stride}, {sites.Count} candidate sites)");
                // No signature prefab declares a MONS/ITEMS slot, and this pass has no spawn
                // drain. Say so rather than dropping the request, which is how a guardian that
                // never spawns looks exactly like bad luck.
                if (spawnQueue.Count > 0)
                    DebugLogger.LogGame("PREFAB",
                        $"'{def.Name}' declared {spawnQueue.Count} spawn slot(s) — the signature "
                        + "pass does not drain them, so the floor is left bare there");
                return true;
            }
            ctx.RefundPrefab(def.Name);
        }
        return false;
    }

    private static int WeightedIndex(
        List<(PrefabDefinition Def, List<(int X, int Y)> Sites)> pool, Random rng)
    {
        int total = 0;
        foreach (var e in pool) total += Math.Max(1, e.Def.Weight);
        int r = rng.Next(total);
        for (int i = 0; i < pool.Count; i++)
        {
            r -= Math.Max(1, pool[i].Def.Weight);
            if (r < 0) return i;
        }
        return pool.Count - 1;
    }
}
