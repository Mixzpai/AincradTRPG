namespace SAOTRPG.Map.Generation.Passes;

// Bundle 10 — seed-grow ore vein clustering. Slots between Cluster and Lake
// so terrain rocks already exist (avoid overlap) and water hasn't washed walls.
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

        for (int i = 0; i < veinCount; i++)
        {
            if (!TryPickSeed(ctx, out int cx, out int cy)) continue;
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

    // Seed must be a Wall in interior, outside SafeZone, and >=4 tiles from any room.
    private static bool TryPickSeed(WorldContext ctx, out int cx, out int cy)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        const int Attempts = 40;
        for (int i = 0; i < Attempts; i++)
        {
            int x = rng.Next(4, Math.Max(5, ctx.Width - 4));
            int y = rng.Next(4, Math.Max(5, ctx.Height - 4));
            if (!map.InInterior(x, y)) continue;
            if (map.Tiles[x, y].Type != TileType.Wall) continue;
            if (IsTooCloseToRoom(ctx.Rooms, x, y, 4)) continue;
            cx = x; cy = y;
            return true;
        }
        cx = cy = 0;
        return false;
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
            if (map.Tiles[x, y].Type != TileType.Wall) continue;
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
