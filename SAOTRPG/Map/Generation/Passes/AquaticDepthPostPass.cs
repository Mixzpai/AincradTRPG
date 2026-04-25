using SAOTRPG.Systems;

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

        // Pre-count water tiles — if below threshold, stamp beach ring and bail.
        int waterCount = 0;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = map.Tiles[x, y].Type;
            if (t == TileType.Water || t == TileType.WaterDeep) waterCount++;
        }
        if (waterCount < MinWaterThreshold)
        {
            StampBeachRing(map, w, h);
            UI.DebugLogger.LogGame("AQUATIC",
                $"floor={ctx.FloorNumber} water={waterCount} < {MinWaterThreshold} — stamped beach ring fallback");
            return;
        }

        // Multi-source BFS: all land tiles seed at distance 0; water tiles get positive distance to nearest shore.
        int[,] dist = new int[w, h];
        var q = new Queue<(int x, int y)>();
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = map.Tiles[x, y].Type;
            bool isWater = t == TileType.Water || t == TileType.WaterDeep;
            dist[x, y] = isWater ? int.MaxValue : 0;
            if (!isWater) q.Enqueue((x, y));
        }

        int[] dxs = { -1, 1, 0, 0 };
        int[] dys = { 0, 0, -1, 1 };
        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();
            int cd = dist[cx, cy];
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (dist[nx, ny] <= cd + 1) continue;
                dist[nx, ny] = cd + 1;
                q.Enqueue((nx, ny));
            }
        }

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = map.Tiles[x, y].Type;
            if (t != TileType.Water && t != TileType.WaterDeep) continue;
            int d = dist[x, y];
            if (d <= 2)      map.Tiles[x, y].Type = TileType.Sand;
            else if (d <= 5) map.Tiles[x, y].Type = TileType.Water;
            else             map.Tiles[x, y].Type = TileType.WaterDeep;
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
