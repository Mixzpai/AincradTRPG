using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation;

// River placement algorithm — chosen per biome, loaded from biome JSON.
public enum RiverAlgorithm { None, DrunkardWalk, DownslopeTrace }

// Particle hook for ambient biome signals. Consumed by AmbientOverlayPass
// to seed per-floor atmospheric particles via ParticleQueue.SeedAmbient.
public sealed record AmbientOverlayStub(string? ParticleId, int DensityPermille);

// Per-biome min/max counts for Poisson-scattered features. Consumed by
// FeatureScatterPass. All bounds inclusive; Poisson spacing is PoissonRadius tiles.
public sealed record FeatureQuotas(
    int MinAnvils = 0,     int MaxAnvils = 1,
    int MinShrines = 1,    int MaxShrines = 2,
    int MinChests = 3,     int MaxChests = 6,
    int MinTraps = 2,      int MaxTraps = 8,
    int MinGasVents = 0,   int MaxGasVents = 3,
    int MinLoreStones = 1, int MaxLoreStones = 2,
    int MinJournals = 1,   int MaxJournals = 3,
    int MinCampfires = 1,  int MaxCampfires = 3,
    int MinPillars = 0,    int MaxPillars = 2,
    float PoissonRadius = 6.0f);

// Per-biome generation knobs. Loaded from Content/Biomes/*.json; `CreateDefault`
// returns an in-memory fallback when JSON is missing or malformed.
public sealed record BiomeGenConfig(
    string BiomeId,
    int ConfigVersion,
    float TreeDensity,
    float RockDensity,
    float BushDensity,
    int WaterSeedPct,
    int WaterCAPasses,
    bool WaterLakesEnabled,
    RiverAlgorithm RiverAlgorithm,
    int RiverMinWidth,
    float RiverSpawnChance,
    int GrassTallWeight,
    int SparseWeight,
    int FlowersWeight,
    int BaseGrassWeight,
    float[] HeightmapThresholds,
    bool RidgeNoiseEnabled,
    float RidgeNoiseStrength,
    AmbientOverlayStub AmbientOverlay,
    string[] PocketCandidates,
    string PrimaryPaletteId,
    string? TintColor = null,
    string? FloorEntryText = null,
    char TreeGlyph = '♣',
    FeatureQuotas? FeatureQuotas = null,
    // Bundle 10 — ore vein placement multiplier + per-biome ore loot table key.
    float OreVeinDensity = 1.0f,
    string OreTableId = "default")
{
    // Parse TintColor ("#RRGGBB" or "#RRGGBBAA") into Color + alpha byte; returns null
    // when unset/malformed. Alpha defaults to 160 (~63%) when only 6 hex digits given.
    public Terminal.Gui.Color? GetTintColor(out byte alpha)
    {
        alpha = 0;
        if (string.IsNullOrWhiteSpace(TintColor) || !TintColor.StartsWith('#')) return null;
        string s = TintColor.Substring(1);
        if (s.Length != 6 && s.Length != 8) return null;
        try
        {
            byte r = Convert.ToByte(s.Substring(0, 2), 16);
            byte g = Convert.ToByte(s.Substring(2, 2), 16);
            byte b = Convert.ToByte(s.Substring(4, 2), 16);
            alpha = s.Length == 8 ? Convert.ToByte(s.Substring(6, 2), 16) : (byte)160;
            return new Terminal.Gui.Color(r, g, b);
        }
        catch { return null; }
    }

    public static BiomeGenConfig CreateDefault(BiomeType biome) => biome switch
    {
        BiomeType.Grassland => Build(biome, 1.0f, 0.8f, 1.0f, 45, 4, true, RiverAlgorithm.DrunkardWalk,
            2, 0.6f, 20, 10, 5, 65, new[] { 0.15f, 0.35f, 0.70f, 0.90f }, false, 0f,
            "wind_motes", 3, new[] { "Forest", "Desert" }, "Body",
            "#8CC85080", "A breeze stirs the tall grass. The sky is wide and unhurried.", '♣',
            new FeatureQuotas(0, 1, 1, 2, 3, 5, 2, 4, 0, 1, 1, 2, 1, 2, 1, 2, 0, 1, 6.0f),
            0.8f, "default"),
        BiomeType.Forest => Build(biome, 2.2f, 0.6f, 1.6f, 45, 4, true, RiverAlgorithm.DrunkardWalk,
            2, 0.55f, 40, 5, 3, 52, new[] { 0.10f, 0.30f, 0.70f, 0.92f }, false, 0f,
            "firefly", 4, new[] { "Grassland", "Swamp" }, "Success",
            "#2E5B2E90", "Dense canopy swallows the light. Every rustle might be teeth.", 'T',
            new FeatureQuotas(0, 1, 1, 2, 3, 6, 3, 6, 0, 2, 1, 3, 1, 3, 1, 3, 0, 1, 6.0f),
            0.6f, "default"),
        BiomeType.Swamp => Build(biome, 1.6f, 0.4f, 1.3f, 55, 3, true, RiverAlgorithm.DrunkardWalk,
            1, 0.4f, 30, 25, 2, 43, new[] { 0.05f, 0.20f, 0.60f, 0.85f }, false, 0f,
            "fog_mote", 8, new[] { "Forest", "Aquatic" }, "Dim",
            "#40385080", "The bog exhales. Your boots find wet ground before you see it.", 'Y',
            new FeatureQuotas(0, 0, 0, 2, 2, 5, 4, 8, 2, 5, 1, 2, 1, 2, 0, 2, 0, 1, 7.0f),
            0.4f, "swamp"),
        BiomeType.Desert => Build(biome, 0.15f, 1.8f, 0.2f, 0, 0, false, RiverAlgorithm.None,
            0, 0f, 2, 80, 1, 17, new[] { 0.30f, 0.50f, 0.75f, 0.88f }, true, 0.65f,
            "sand_drift", 7, new[] { "Grassland", "Volcanic" }, "Gold",
            "#C8A05080", "Heat ripples distort the horizon. Your canteen feels too light.", '♠',
            new FeatureQuotas(0, 1, 0, 1, 2, 4, 2, 6, 0, 2, 1, 2, 0, 2, 0, 1, 0, 2, 8.0f),
            1.2f, "desert"),
        BiomeType.Ice => Build(biome, 0.9f, 1.4f, 0.3f, 50, 4, true, RiverAlgorithm.DrunkardWalk,
            2, 0.5f, 5, 55, 1, 39, new[] { 0.15f, 0.35f, 0.70f, 0.88f }, true, 0.35f,
            "snowflake", 6, new[] { "Aquatic" }, "Title",
            "#88B0D890", "The air bites. Every breath glitters for a heartbeat then vanishes.", '▲',
            new FeatureQuotas(0, 1, 1, 2, 3, 5, 2, 5, 0, 1, 1, 2, 1, 2, 1, 2, 0, 1, 6.0f),
            1.3f, "ice"),
        BiomeType.Volcanic => Build(biome, 0.2f, 2.2f, 0.2f, 0, 0, false, RiverAlgorithm.DownslopeTrace,
            2, 0.7f, 2, 70, 0, 28, new[] { 0.20f, 0.40f, 0.65f, 0.82f }, true, 0.80f,
            "ember", 8, new[] { "Desert" }, "Danger",
            "#CC402090", "The ground trembles underfoot. Somewhere below, something old is awake.", '¥',
            new FeatureQuotas(0, 1, 0, 1, 2, 4, 4, 8, 3, 6, 1, 2, 0, 2, 0, 1, 0, 2, 7.0f),
            1.5f, "volcanic"),
        BiomeType.Aquatic => Build(biome, 0.5f, 0.6f, 0.8f, 70, 6, true, RiverAlgorithm.DrunkardWalk,
            3, 0.85f, 15, 30, 2, 53, new[] { 0.40f, 0.55f, 0.75f, 0.92f }, false, 0f,
            "rain_mote", 5, new[] { "Ice", "Swamp" }, "Dialog",
            "#4080C080", "Sea-spray dusts the stones. The horizon is water all the way to the wall.", '♣',
            new FeatureQuotas(0, 1, 0, 1, 2, 4, 0, 2, 0, 1, 1, 2, 1, 2, 0, 2, 0, 1, 8.0f),
            0.2f, "aquatic"),
        BiomeType.Ruins => Build(biome, 0.8f, 1.6f, 1.1f, 30, 3, true, RiverAlgorithm.DrunkardWalk,
            1, 0.25f, 18, 40, 2, 40, new[] { 0.15f, 0.35f, 0.70f, 0.90f }, false, 0f,
            "dust_mote", 5, new[] { "Urban", "Forest" }, "Dim",
            "#806040A0", "Cracked flagstones underfoot. Someone lived here once. No one does now.", '♣',
            new FeatureQuotas(0, 1, 1, 2, 3, 6, 3, 7, 0, 3, 2, 4, 2, 4, 0, 2, 1, 3, 6.0f),
            0.5f, "default"),
        BiomeType.Dark => Build(biome, 0.3f, 1.4f, 0.4f, 20, 3, true, RiverAlgorithm.None,
            0, 0f, 3, 60, 0, 37, new[] { 0.10f, 0.30f, 0.65f, 0.88f }, false, 0f,
            "shadow_wisp", 2, new[] { "Void" }, "Dim",
            "#2010306A", "The torch sputters. Shadows fold in on themselves at the edge of sight.", '♣',
            new FeatureQuotas(0, 0, 0, 1, 2, 4, 3, 7, 1, 3, 1, 2, 1, 2, 0, 1, 0, 2, 7.0f),
            1.0f, "dark"),
        BiomeType.Urban => Build(biome, 0.3f, 0.4f, 0.5f, 25, 3, false, RiverAlgorithm.DrunkardWalk,
            1, 0.3f, 5, 20, 5, 70, new[] { 0.15f, 0.35f, 0.70f, 0.90f }, false, 0f,
            "lantern_glow", 3, new[] { "Ruins" }, "Gold",
            "#B0A8904A", "Lantern light spills from shuttered windows. The streets remember footsteps.", '♣',
            new FeatureQuotas(1, 2, 1, 2, 2, 4, 1, 3, 0, 1, 1, 2, 2, 3, 1, 2, 1, 3, 5.0f),
            0.5f, "default"),
        BiomeType.Void => Build(biome, 0.1f, 0.6f, 0.2f, 10, 2, false, RiverAlgorithm.None,
            0, 0f, 2, 75, 1, 22, new[] { 0.05f, 0.25f, 0.60f, 0.85f }, true, 0.45f,
            "void_spark", 4, new[] { "Dark" }, "TierRadio",
            "#5020A080", "Reality hiccups. For one breath, the walls aren't where they were.", '*',
            new FeatureQuotas(0, 0, 0, 2, 1, 3, 4, 9, 2, 5, 1, 2, 0, 1, 0, 0, 0, 1, 9.0f),
            1.4f, "void"),
        _ => Build(biome, 1.0f, 0.8f, 1.0f, 45, 4, true, RiverAlgorithm.DrunkardWalk,
            2, 0.6f, 20, 10, 5, 65, new[] { 0.15f, 0.35f, 0.70f, 0.90f }, false, 0f,
            "wind_motes", 3, new[] { "Forest", "Desert" }, "Body",
            null, null, '♣', new FeatureQuotas()),
    };

    private static BiomeGenConfig Build(BiomeType b, float treeDensity, float rockDensity, float bushDensity,
        int waterSeedPct, int caPasses, bool lakesEnabled, RiverAlgorithm river, int minWidth, float spawnChance,
        int tall, int sparse, int flowers, int baseGrass, float[] thresholds, bool ridge, float ridgeStr,
        string? particle, int permille, string[] pockets, string palette,
        string? tint = null, string? entryText = null, char treeGlyph = '♣',
        FeatureQuotas? quotas = null, float oreVeinDensity = 1.0f, string oreTableId = "default")
        => new(b.ToString(), 1, treeDensity, rockDensity, bushDensity, waterSeedPct, caPasses, lakesEnabled,
            river, minWidth, spawnChance, tall, sparse, flowers, baseGrass, thresholds, ridge, ridgeStr,
            new AmbientOverlayStub(particle, permille), pockets, palette, tint, entryText, treeGlyph, quotas,
            oreVeinDensity, oreTableId);
}
