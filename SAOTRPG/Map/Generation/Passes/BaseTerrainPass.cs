using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation.Passes;

// Biome-native base terrain. Per-tile dispatch; pocket-biome tiles pull their own
// BiomeGenConfig so pocket thresholds/tile palette match the pocket biome (Bundle 8).
public sealed class BaseTerrainPass : IGenerationPass
{
    public string Name => "BaseTerrain";
    public bool ShouldRun(WorldContext ctx) => true;

    public void Execute(WorldContext ctx)
    {
        int w = ctx.Width, h = ctx.Height;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            // Out-of-disk → Mountain (blocks sight/movement/path natively).
            if (!ctx.IsInsideCircle(x, y)) { ctx.Map.Tiles[x, y].Type = TileType.Mountain; continue; }
            BiomeType effective = ctx.PocketBiomeMap?[x, y] ?? ctx.Biome;
            StampSingleTile(ctx, x, y, effective);
        }
    }

    // Per-tile dispatcher. Aquatic falls through to default weighted grass (AquaticDepthPostPass handles
    // shore/depth); signature biomes stamp their own bedrock. cfg = pocket config if claimed, else ctx.Config.
    private static void StampSingleTile(WorldContext ctx, int x, int y, BiomeType biome)
    {
        var cfg = GetEffectiveConfig(ctx, biome);
        switch (biome)
        {
            case BiomeType.Desert:   StampDesert(ctx, cfg, x, y);   return;
            case BiomeType.Ice:      StampIce(ctx, cfg, x, y);      return;
            case BiomeType.Volcanic: StampVolcanic(ctx, cfg, x, y); return;
            case BiomeType.Swamp:    StampSwamp(ctx, cfg, x, y);    return;
            default:                 StampDefault(ctx, cfg, x, y, biome); return;
        }
    }

    // Pocket tiles pull per-biome config via loader (cached in loader). Host
    // tiles reuse ctx.Config so the common path allocates nothing.
    private static BiomeGenConfig GetEffectiveConfig(WorldContext ctx, BiomeType effective)
        => effective == ctx.Biome ? ctx.Config : BiomeGenConfigLoader.Get(effective);

    // Heightmap-driven weighted grass variant — default for non-signature
    // biomes (Grassland/Forest/Urban/Dark/Void/Ruins/Aquatic).
    private static void StampDefault(WorldContext ctx, BiomeGenConfig cfg, int x, int y, BiomeType _biome)
    {
        var rng = ctx.Rng;
        var heights = ctx.Heights;

        int totalWeight = Math.Max(1,
            cfg.BaseGrassWeight + cfg.GrassTallWeight + cfg.SparseWeight + cfg.FlowersWeight);

        float tLow = cfg.HeightmapThresholds.Length > 1 ? cfg.HeightmapThresholds[1] : 0.35f;
        float tMid = cfg.HeightmapThresholds.Length > 2 ? cfg.HeightmapThresholds[2] : 0.70f;

        int r = rng.Next(totalWeight);
        TileType pick;
        if (r < cfg.BaseGrassWeight) pick = TileType.Grass;
        else if (r < cfg.BaseGrassWeight + cfg.GrassTallWeight) pick = TileType.GrassTall;
        else if (r < cfg.BaseGrassWeight + cfg.GrassTallWeight + cfg.SparseWeight) pick = TileType.GrassSparse;
        // F47 Floria is canon flower-garden floor; other floors fall back to Grass.
        else pick = ctx.FloorNumber == 47 ? TileType.Flowers : TileType.Grass;

        if (heights != null)
        {
            float hv = heights[x, y];
            if (hv < tLow)
            {
                if (pick == TileType.GrassTall && rng.Next(2) == 0) pick = TileType.GrassSparse;
            }
            else if (hv > tMid)
            {
                if (pick == TileType.GrassSparse && rng.Next(2) == 0) pick = TileType.GrassTall;
            }
        }

        ctx.Map.Tiles[x, y].Type = pick;
    }

    // Sand/DuneSand split by heightmap ridge threshold; rare grass tuft for visual variety.
    private static void StampDesert(WorldContext ctx, BiomeGenConfig cfg, int x, int y)
    {
        var heights = ctx.Heights;
        float ridgeThresh = cfg.HeightmapThresholds.Length > 2 ? cfg.HeightmapThresholds[2] : 0.70f;
        float hv = heights != null ? heights[x, y] : 0.5f;
        TileType pick = hv > ridgeThresh ? TileType.DuneSand : TileType.Sand;
        if (ctx.Rng.Next(200) == 0) pick = TileType.GrassSparse;
        ctx.Map.Tiles[x, y].Type = pick;
    }

    // Snow above mid threshold, Ice below; CrackedIce rings near water placed post-water pass.
    private static void StampIce(WorldContext ctx, BiomeGenConfig cfg, int x, int y)
    {
        var heights = ctx.Heights;
        float midThresh = cfg.HeightmapThresholds.Length > 2 ? cfg.HeightmapThresholds[2] : 0.50f;
        float hv = heights != null ? heights[x, y] : 0.5f;
        ctx.Map.Tiles[x, y].Type = hv > midThresh ? TileType.Snow : TileType.Ice;
    }

    // Basalt peaks over Ash lowlands.
    private static void StampVolcanic(WorldContext ctx, BiomeGenConfig cfg, int x, int y)
    {
        var heights = ctx.Heights;
        float midThresh = cfg.HeightmapThresholds.Length > 2 ? cfg.HeightmapThresholds[2] : 0.60f;
        float hv = heights != null ? heights[x, y] : 0.5f;
        ctx.Map.Tiles[x, y].Type = hv > midThresh ? TileType.Basalt : TileType.Ash;
    }

    // Low band is Reeds, mid band is Mud (marshy default), peaks allow occasional
    // GrassTall tufts. LakePass tail converts any lake water to BogWater.
    private static void StampSwamp(WorldContext ctx, BiomeGenConfig cfg, int x, int y)
    {
        var rng = ctx.Rng;
        var heights = ctx.Heights;
        float lowThresh = cfg.HeightmapThresholds.Length > 1 ? cfg.HeightmapThresholds[1] : 0.30f;
        float midThresh = cfg.HeightmapThresholds.Length > 2 ? cfg.HeightmapThresholds[2] : 0.70f;
        float hv = heights != null ? heights[x, y] : 0.5f;
        TileType pick;
        if (hv < lowThresh) pick = TileType.Reeds;
        else if (hv > midThresh) pick = rng.Next(3) == 0 ? TileType.GrassTall : TileType.Mud;
        else pick = rng.Next(10) == 0 ? TileType.Grass : TileType.Mud;
        ctx.Map.Tiles[x, y].Type = pick;
    }
}
