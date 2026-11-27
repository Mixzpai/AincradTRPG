
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation.Passes;

// Ice-only post-water conversion. Any Water/WaterDeep tiles placed by LakePass/RiverPass
// become frozen surfaces: CrackedIce on the water-edge ring (adjacent to land), Ice in the interior.
public sealed class IcePostWaterPass : IGenerationPass
{
    public string Name => "IcePostWater";
    public bool ShouldRun(WorldContext ctx) => ctx.Biome == BiomeType.Ice;

    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        var tiles = map.Tiles;
        int w = ctx.Width, h = ctx.Height;

        // This pass draws no rng, so the visit order is free — everything below runs row-major,
        // which is the order GameMap stores tiles in. The water test is read once into a flat
        // table because the edge scan asks it nine times per water tile, and a probe through the
        // tile grid is a 16-byte struct behind a 2D indexer.
        var water = new bool[w * h];
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                var t = tiles[x, y].Type;
                water[rowBase + x] = t == TileType.Water || t == TileType.WaterDeep;
            }
        }

        // First pass: record which water tiles border land so the edge becomes CrackedIce.
        // Out-of-bounds neighbours do not count as land.
        var isEdge = new bool[w * h];
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                if (!water[rowBase + x]) continue;
                bool edge = false;
                for (int dy = -1; dy <= 1 && !edge; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= h) continue;
                    int nb = ny * w;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        if (nx < 0 || nx >= w) continue;
                        if (!water[nb + nx]) { edge = true; break; }
                    }
                }
                isEdge[rowBase + x] = edge;
            }
        }

        // Second pass: stamp Ice/CrackedIce.
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                if (!water[rowBase + x]) continue;
                tiles[x, y].Type = isEdge[rowBase + x] ? TileType.CrackedIce : TileType.Ice;
            }
        }
    }
}
