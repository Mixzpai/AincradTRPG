using SAOTRPG.Systems;

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
        int w = ctx.Width, h = ctx.Height;

        // First pass: record which water tiles border land so the edge becomes CrackedIce.
        bool[,] isEdge = new bool[w, h];
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = map.Tiles[x, y].Type;
            if (t != TileType.Water && t != TileType.WaterDeep) continue;
            for (int dy = -1; dy <= 1 && !isEdge[x, y]; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                var nt = map.Tiles[nx, ny].Type;
                if (nt != TileType.Water && nt != TileType.WaterDeep)
                {
                    isEdge[x, y] = true;
                    break;
                }
            }
        }

        // Second pass: stamp Ice/CrackedIce.
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = map.Tiles[x, y].Type;
            if (t != TileType.Water && t != TileType.WaterDeep) continue;
            map.Tiles[x, y].Type = isEdge[x, y] ? TileType.CrackedIce : TileType.Ice;
        }
    }
}
