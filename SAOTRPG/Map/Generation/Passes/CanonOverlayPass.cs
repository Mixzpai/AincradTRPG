using SAOTRPG.Map.Generation.Prefabs;
using SAOTRPG.Systems.Diagnostics;
using SAOTRPG.Systems.RunState;

namespace SAOTRPG.Map.Generation.Passes;

// Places the set-piece authored for this floor, ahead of the biome landmark and the general
// prefab draw.
//
// The distinction from SignaturePrefabPass is the subject, not the mechanism: a signature piece
// says "this is a Forest floor", a canon-overlay piece says "this is Coral Village". Most floors
// have no such piece, so an empty pool is the ordinary case and is silent.
//
// It runs first of the three prefab passes because it is the most specific claim about the floor
// and because open ground is not evenly available: a cave biome leaves little of the disk clear,
// and a wide footprint can have no host sites there at all. Where ground is scarce, the piece
// that names the floor gets the pick of it.
//
// The pieces are data, not stamp routines in C#. A hand-coded routine would bypass the parser's
// glyph validation, and it would have to re-derive the one-tile skirt that is the reason a stamp
// cannot disconnect a floor.
public sealed class CanonOverlayPass : IGenerationPass
{
    public string Name => "CanonOverlay";

    // Anchors are sampled on a stride, with a finer retry for a floor whose only host sites fall
    // between the coarse samples. Same two constants as the signature pass, for the same reason:
    // on a cave floor a small footprint's only sites can all fall between coarse samples.
    private const int CoarseStride = 6;
    private const int FineStride = 2;

    // Host sites to collect per candidate before choosing. The site test is necessary, not
    // sufficient -- the disk mask and the room rects still get a veto -- so the sample has to be
    // deep enough that the placer is not left holding near-misses.
    private const int SiteSample = 128;

    private static readonly string[] CanonTag = { PrefabLibrary.CanonOverlayTag };

    // F100 bypasses the pipeline entirely, and a hand-authored SafeZone floor (F1 Town of
    // Beginnings, F48 Lindarth) must not be stamped over -- its boulevards are load-bearing.
    // Reads state SpecialAreaPass has already written and draws no rng, so it stays pure.
    public bool ShouldRun(WorldContext ctx) => ctx.FloorNumber != 100 && ctx.Map.SafeZone == null;

    public void Execute(WorldContext ctx)
    {
        PrefabLibrary.Shared.LoadAll();
        if (PrefabLibrary.Shared.Count == 0) return;

        string biome = ctx.Biome.ToString().ToLowerInvariant();
        var pool = PrefabLibrary.Shared.CandidatesFor(biome, ctx.FloorNumber, CanonTag)
            .Where(p => p.Orient != PrefabOrient.Encompass)
            .ToList();
        // No set-piece for this floor is the normal case for most floors. Silent on
        // purpose: a line per floor per run would be noise, and the loud case below is the one
        // that means something went wrong.
        if (pool.Count == 0) return;

        // Its own stream, derived from (globalSeed, floor) exactly as SeedDerivationPass, the
        // signature pass and the prefab vendor stock derive theirs. Two reasons it is not
        // ctx.Rng: a draw here would renumber every later pass and move the whole floor, and the
        // seed must come from the context rather than from RunRng.Seed -- that is the run's seed,
        // read from a mutable static, and a floor built ahead of the player runs on a worker that
        // can be handed a different one.
        var rng = new Random(RunRng.StableHash(
            RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber), "canon-overlay", 0));

        if (TryPlaceFrom(ctx, pool, rng, CoarseStride, out bool coarseCapped)) return;
        if (TryPlaceFrom(ctx, pool, rng, FineStride, out bool fineCapped)) return;

        // Loud, and it names what actually happened rather than guessing: a cap refusal and a
        // terrain refusal look identical from the finished map and lead opposite investigations.
        DebugLogger.LogGame("PREFAB", coarseCapped || fineCapped
            ? $"floor={ctx.FloorNumber} biome={biome} canon set-piece refused by its MAX_PER_GAME "
              + "cap — this floor has been generated before in this game, so it comes back without it"
            : $"floor={ctx.FloorNumber} biome={biome} no site for any of {pool.Count} canon "
              + "set-piece(s) — floor keeps its generic character");
    }

    private static bool TryPlaceFrom(WorldContext ctx, List<PrefabDefinition> pool, Random rng,
                                     int stride, out bool refusedByCap)
    {
        refusedByCap = false;
        var anchors = PrefabPlacer.CollectOpenGroundAnchors(ctx, stride);
        if (anchors.Count == 0) return false;

        // Shuffled once, so the capped scan below samples the whole floor rather than its
        // top-left corner. Durstenfeld on the list the pass already owns.
        for (int i = anchors.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (anchors[i], anchors[j]) = (anchors[j], anchors[i]);
        }

        // Filter per candidate first, then choose among what the terrain can take. Choosing first
        // and discovering the terrain second means a floor pays a full failed search for a
        // footprint that has never fitted on it.
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

        // Weighted draw without replacement, so a floor carrying two set-pieces still lands one
        // when the first has no seat.
        while (feasible.Count > 0)
        {
            int idx = WeightedIndex(feasible, rng);
            var (def, sites) = feasible[idx];
            feasible.RemoveAt(idx);

            if (!ctx.TryUsePrefab(def.Name, def.MaxPerGame)) { refusedByCap = true; continue; }
            var spawnQueue = new List<PrefabSpawnRequest>();
            if (PrefabPlacer.TryPlaceFloating(ctx.Map, def, rng, sites, ctx.Rooms, ctx, spawnQueue,
                                              attempts: sites.Count))
            {
                DebugLogger.LogGame("PREFAB",
                    $"floor={ctx.FloorNumber} canon set-piece '{def.Name}' placed (stride {stride})");
                // This pass has no spawn drain. Say so rather than dropping the request in
                // silence, which is how a guardian that never spawns reads as bad luck.
                if (spawnQueue.Count > 0)
                    DebugLogger.LogGame("PREFAB",
                        $"'{def.Name}' declared {spawnQueue.Count} spawn slot(s) — the canon "
                        + "overlay does not drain them, so the floor is left bare there");
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
