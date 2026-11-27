
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation.Passes;

// Aquatic-only post-water depth gradient. BFS from land produces a distance field;
// water tiles within 2 of shore become Sand (beach), 3-5 stay Water, 6+ become WaterDeep.
public sealed class AquaticDepthPostPass : IGenerationPass
{
    public string Name => "AquaticDepthPost";
    public bool ShouldRun(WorldContext ctx) => ctx.Biome == BiomeType.Aquatic;

    // If the floor ended up with < 50 water tiles (LakePass gated off on narrow maps,
    // no-river roll), stamp a 2-tile Sand perimeter so Aquatic at least reads coastal.
    private const int MinWaterThreshold = 50;

    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        int w = ctx.Width, h = ctx.Height;

        // This pass draws no rng, so the visit order is free — everything below is row-major, the
        // order GameMap stores tiles in, and the distance field a multi-source BFS produces is the
        // true distance to the nearest source whatever order the sources were enqueued in.
        var tiles = map.Tiles;

        // Pre-count water tiles — if below threshold, stamp beach ring and bail. The water test is
        // kept because the seed loop below asks it again.
        var isWater = new bool[w * h];
        int waterCount = 0;
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                var t = tiles[x, y].Type;
                bool water = t == TileType.Water || t == TileType.WaterDeep;
                isWater[rowBase + x] = water;
                if (water) waterCount++;
            }
        }
        if (waterCount < MinWaterThreshold)
        {
            StampBeachRing(map, w, h);
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("AQUATIC",
                $"floor={ctx.FloorNumber} water={waterCount} < {MinWaterThreshold} — stamped beach ring fallback");
            return;
        }

        // Multi-source BFS: all land tiles seed at distance 0; water tiles get positive distance to
        // nearest shore. A flat int grid and a flat int queue rather than int[w, h] plus a
        // Queue<(int, int)>: a large Aquatic floor enqueues most of its cells, and a 2D indexer
        // per probe adds up. The frontier is monotonic, so each cell is enqueued exactly once and
        // w*h slots is an exact bound.
        var dist = new int[w * h];
        var q = new int[w * h];
        int head = 0, tail = 0;
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                int i = rowBase + x;
                if (isWater[i]) { dist[i] = int.MaxValue; continue; }
                dist[i] = 0;
                q[tail++] = i;
            }
        }

        int hLimit = w * (h - 1);
        while (head < tail)
        {
            int cur = q[head++];
            int cy = cur / w, cx = cur - cy * w;
            int nd = dist[cur] + 1;
            if (cx > 0)       Relax(dist, q, ref tail, cur - 1, nd);
            if (cx < w - 1)   Relax(dist, q, ref tail, cur + 1, nd);
            if (cur >= w)     Relax(dist, q, ref tail, cur - w, nd);
            if (cur < hLimit) Relax(dist, q, ref tail, cur + w, nd);
        }

        static void Relax(int[] dist, int[] q, ref int tail, int ni, int nd)
        {
            if (dist[ni] <= nd) return;
            dist[ni] = nd;
            q[tail++] = ni;
        }

        // Bands scale with the floor, like the lake radius that feeds them. With fixed bands a
        // large lake would be WaterDeep almost everywhere past the shore — a Swimming L25 gate, so
        // an early Aquatic floor would wall off most of its own water from the player standing on
        // it. Proportional bands keep the beach-shallow-deep gradient intact at any floor size,
        // and keep the deep core a minority of each lake rather than the whole of it.
        //
        // Same reasoning the river stamper follows: rivers place only shallow water,
        // because life skills start at L1 and WaterDeep is the audit-blocking one.
        int lakeUnit = Math.Max(6, Math.Min(w, h) / 40);
        int beachBand = Math.Max(2, lakeUnit / 3);
        int shallowBand = beachBand + Math.Max(3, lakeUnit);

        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                int i = rowBase + x;
                if (!isWater[i]) continue;
                int d = dist[i];
                if (d <= beachBand)        tiles[x, y].Type = TileType.Sand;
                else if (d <= shallowBand) tiles[x, y].Type = TileType.Water;
                else                       tiles[x, y].Type = TileType.WaterDeep;
            }
        }
    }

    // Stamps Sand along the outermost 2-tile walkable ring. Gated by IsConvertible
    // so Walls / Mountains / existing prefab content survive.
    private static void StampBeachRing(GameMap map, int w, int h)
    {
        for (int x = 0; x < w; x++)
            for (int d = 0; d < 2; d++)
            {
                if (map.InBounds(x, d) && IsConvertible(map.Tiles[x, d].Type))
                    map.Tiles[x, d].Type = TileType.Sand;
                if (map.InBounds(x, h - 1 - d) && IsConvertible(map.Tiles[x, h - 1 - d].Type))
                    map.Tiles[x, h - 1 - d].Type = TileType.Sand;
            }
        for (int y = 0; y < h; y++)
            for (int d = 0; d < 2; d++)
            {
                if (map.InBounds(d, y) && IsConvertible(map.Tiles[d, y].Type))
                    map.Tiles[d, y].Type = TileType.Sand;
                if (map.InBounds(w - 1 - d, y) && IsConvertible(map.Tiles[w - 1 - d, y].Type))
                    map.Tiles[w - 1 - d, y].Type = TileType.Sand;
            }
    }

    private static bool IsConvertible(TileType t) =>
        MapGenerator.IsGrassType(t) || t == TileType.Floor;
}
