namespace SAOTRPG.Map.Generation.Passes;

// Seed-grow ore vein clustering. Slots between Cluster and Lake so terrain rocks
// already exist (avoid overlap) and water hasn't washed walls.
public sealed class OreVeinPlacementPass : IGenerationPass
{
    public string Name => "OreVeinPlacement";

    // F1 (TOB) and F100 (Ruby Palace) bypass — TOB is hand-built SafeZone, F100 isn't run anyway.
    // Mirror PrefabPlacementPass.ShouldRun for the SafeZone gate.
    public bool ShouldRun(WorldContext ctx) =>
        ctx.FloorNumber != 100 && ctx.Map.SafeZone == null;

    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        var cfg = ctx.Config;
        int floor = ctx.FloorNumber;
        int width = ctx.Width, height = ctx.Height;

        // Per-biome density multiplier from cfg, plus floor-band base count.
        int baseCount = BaseVeinsForFloor(floor, rng);
        float biomeDensity = cfg.OreVeinDensity;
        int veinCount = (int)Math.Round(baseCount * biomeDensity);
        if (veinCount <= 0) return;

        var candidates = CollectSeedCandidates(ctx);
        if (candidates.Count == 0)
        {
            UI.DebugLogger.LogGame("MAPGEN",
                $"OreVein: floor {floor} has no viable host tile — no veins placed");
            return;
        }

        // Seeds are drawn without replacement so two veins cannot start on the same tile and
        // silently collapse into one.
        for (int i = 0; i < veinCount && candidates.Count > 0; i++)
        {
            int pick = rng.Next(candidates.Count);
            var (cx, cy) = candidates[pick];
            candidates.RemoveAt(pick);

            VeinTier tier = PickTierForFloor(floor, rng);
            int radius = 2 + rng.Next(2);                  // 2-3
            int targetCount = 3 + rng.Next(4);             // 3-6
            GrowVein(map, cx, cy, radius, targetCount, tier, rng);
        }
    }

    // Floor-band base vein counts (locked in scout 2.5).
    private static int BaseVeinsForFloor(int floor, Random rng)
    {
        if (floor <= 1) return 0;
        if (floor <= 25) return 6 + rng.Next(4);
        if (floor <= 50) return 8 + rng.Next(4);
        if (floor <= 75) return 6 + rng.Next(3);
        if (floor <= 99) return 4 + rng.Next(3);
        return 0;
    }

    private enum VeinTier { Iron, Mithril, Divine }

    // Floor-band weighted tier roll. Iron is dominant low-floor, Divine appears late.
    private static VeinTier PickTierForFloor(int floor, Random rng)
    {
        int wIron, wMithril, wDivine;
        if (floor <= 25)      { wIron = 90; wMithril = 10; wDivine = 0;  }
        else if (floor <= 50) { wIron = 50; wMithril = 45; wDivine = 5;  }
        else if (floor <= 75) { wIron = 20; wMithril = 60; wDivine = 20; }
        else                  { wIron = 5;  wMithril = 35; wDivine = 60; }

        int total = wIron + wMithril + wDivine;
        int r = rng.Next(total);
        if (r < wIron) return VeinTier.Iron;
        if (r < wIron + wMithril) return VeinTier.Mithril;
        return VeinTier.Divine;
    }

    private static int DefaultStrikes(VeinTier tier) => tier switch
    {
        VeinTier.Iron => 3,
        VeinTier.Mithril => 5,
        VeinTier.Divine => 8,
        _ => 3,
    };

    private static TileType TileForTier(VeinTier tier) => tier switch
    {
        VeinTier.Iron => TileType.OreVeinIron,
        VeinTier.Mithril => TileType.OreVeinMithril,
        VeinTier.Divine => TileType.OreVeinDivine,
        _ => TileType.OreVeinIron,
    };

    // Seed must be a Wall in interior, outside SafeZone, inside disk, and >=4 tiles from any room.
    // WHAT ROCK AN ORE VEIN MAY REPLACE.
    //
    // Both the seed picker and the grower used to require TileType.Wall, and the heightmap
    // overworld barely produces Wall: measured on floor 2, 377 Wall tiles against 216,827
    // Mountain — and the few Wall tiles that exist are prefab interiors, which IsTooCloseToRoom
    // then rejects. So all 40 seed attempts failed on every floor and NOT ONE ORE VEIN EVER
    // SPAWNED: 0 of 21 floors across 3 seeds. The whole Mining life skill — three pickaxe tiers,
    // MiningOreTables, the biome Enhancement Ore that OreTableId selects, the mining perks and
    // milestones — sat downstream of a pass that could never place anything.
    //
    // Wall stays in the set because labyrinth floors are built from it; Mountain and Rock are the
    // overworld's stone and are what the pass's own comment ("terrain rocks already exist") always
    // meant.
    private static bool IsVeinHost(TileType t) =>
        t is TileType.Wall or TileType.Mountain or TileType.Rock;

    // EVERY TILE A VEIN COULD SEED ON, collected in one scan.
    //
    // This used to throw 40 random darts at the whole map and keep the first that landed on a
    // valid host. Measured, the viable pool is 7,770 tiles of ~1,000,000 on floor 2 — 0.8% — so
    // the darts almost never landed and no floor got any ore at all. Widening the host test alone
    // moved it from 0 of 21 floors to 2 of 21: the sampling, not the predicate, was the wall.
    //
    // One scan of a 1000x1000 floor is cheap beside the passes that already walk every cell, and
    // it makes placement DETERMINISTIC in the useful sense: if hosts exist, veins are placed.
    private static List<(int X, int Y)> CollectSeedCandidates(WorldContext ctx)
    {
        var map = ctx.Map;
        var candidates = new List<(int X, int Y)>();
        for (int y = 4; y < ctx.Height - 4; y++)
        {
            for (int x = 4; x < ctx.Width - 4; x++)
            {
                if (!IsVeinHost(map.Tiles[x, y].Type)) continue;
                if (!ctx.IsInsideCircle(x, y)) continue;
                if (!map.InInterior(x, y)) continue;
                if (IsTooCloseToRoom(ctx.Rooms, x, y, 4)) continue;
                candidates.Add((x, y));
            }
        }
        return candidates;
    }

    private static bool IsTooCloseToRoom(IReadOnlyList<Room> rooms, int x, int y, int margin)
    {
        foreach (var r in rooms)
        {
            int dx = Math.Max(0, Math.Max(r.X - x, x - (r.X + r.Width - 1)));
            int dy = Math.Max(0, Math.Max(r.Y - y, y - (r.Y + r.Height - 1)));
            if (dx + dy < margin) return true;
        }
        return false;
    }

    // Seed-grow over Wall tiles within `radius`. Sets ore tile + seeds VeinStrikesRemaining.
    private static void GrowVein(GameMap map, int cx, int cy, int radius, int targetCount,
        VeinTier tier, Random rng)
    {
        var tile = TileForTier(tier);
        int strikes = DefaultStrikes(tier);

        var visited = new HashSet<(int X, int Y)> { (cx, cy) };
        var frontier = new List<(int X, int Y)> { (cx, cy) };
        int placed = 0;

        while (frontier.Count > 0 && placed < targetCount)
        {
            int idx = rng.Next(frontier.Count);
            var (x, y) = frontier[idx];
            frontier.RemoveAt(idx);

            if (!map.InInterior(x, y)) continue;
            if (!IsVeinHost(map.Tiles[x, y].Type)) continue;
            int dist = Math.Abs(x - cx) + Math.Abs(y - cy);
            if (dist > radius) continue;

            map.Tiles[x, y].Type = tile;
            map.VeinStrikesRemaining[(x, y)] = strikes;
            placed++;

            // Cardinal neighbors only — keeps clusters tight rather than diagonal-bleeding.
            TryEnqueue(visited, frontier, x + 1, y);
            TryEnqueue(visited, frontier, x - 1, y);
            TryEnqueue(visited, frontier, x, y + 1);
            TryEnqueue(visited, frontier, x, y - 1);
        }
    }

    private static void TryEnqueue(HashSet<(int X, int Y)> visited, List<(int X, int Y)> frontier,
        int x, int y)
    {
        if (visited.Add((x, y))) frontier.Add((x, y));
    }
}
