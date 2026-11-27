namespace SAOTRPG.Systems.World;

// Floor→biome map with passive effects.
// Effects apply per-turn + on floor entry.

public enum BiomeType
{
    Grassland,  // safe, natural, no penalties
    Forest,     // reduced visibility, ambush chance
    Swamp,      // poison chance per step, slow movement
    Desert,     // thirst drain (satiety loss), sandstorm visibility
    Volcanic,   // heat damage per turn, fire weapon bonus
    Ice,        // slip chance (lose turn), cold damage if low HP
    Aquatic,    // reduced attack speed, water tiles more common
    Dark,       // severely reduced vision, stress (extra hunger)
    Ruins,      // trap density increased, better loot
    Urban,      // more vendors/NPCs, no natural hazards
    Void,       // random status effects, reality warping
}

// Per-biome configuration record. All per-biome fields live here, so each
// public property on BiomeSystem becomes a one-line lookup in _configs.
public sealed record BiomeConfig(
    string DisplayName,
    int SatietyDrainBonus,
    int SlipChance,
    int StepPoisonChance,
    (int Damage, int Interval) EnvironmentDamage,
    int VisionModifier,
    int AttackModifier,
    string EntryMessage);

public static class BiomeSystem
{
    // Current floor's biome. Set on floor entry.
    public static BiomeType Current { get; private set; } = BiomeType.Grassland;

    // All per-biome data in one table. Add a field via BiomeConfig extension.
    private static readonly Dictionary<BiomeType, BiomeConfig> _configs = new()
    {
        [BiomeType.Grassland] = new("Grassland",       0, 0,  0, (0, 0),   0,  0, "A gentle breeze carries the scent of grass and wildflowers."),
        [BiomeType.Forest]    = new("Forest",          0, 0,  0, (0, 0),  -8,  0, "Thick canopy blocks most of the light. Watch for ambushes."),
        [BiomeType.Swamp]     = new("Toxic Swamp",     0, 0,  8, (0, 0),  -5,  0, "The air is thick with toxic fumes. Watch your step -- poison lurks everywhere."),
        [BiomeType.Desert]    = new("Desert",          1, 0,  0, (0, 0),  -5,  0, "Scorching heat radiates from the sand. Water is precious here."),
        [BiomeType.Volcanic]  = new("Volcanic",        1, 0,  0, (2, 8),   0,  0, "The ground trembles. Lava flows nearby -- the heat is oppressive."),
        [BiomeType.Ice]       = new("Frozen",          0, 12, 0, (0, 0),   0, -2, "Frost coats every surface. The cold bites through your armor."),
        [BiomeType.Aquatic]   = new("Aquatic",         0, 5,  0, (0, 0),   0, -3, "Water everywhere. Movement feels sluggish in the damp air."),
        [BiomeType.Dark]      = new("Darkness",        0, 0,  0, (0, 0), -20,  0, "Darkness presses in from all sides. Your torch flickers weakly."),
        [BiomeType.Ruins]     = new("Ancient Ruins",   0, 0,  0, (0, 0),   0,  0, "Ancient stone corridors. Hidden traps and forgotten treasure await."),
        [BiomeType.Urban]     = new("Settlement",      0, 0,  0, (0, 0),   0,  0, "A settlement on this floor. Merchants and NPCs may offer help."),
        [BiomeType.Void]      = new("The Void",        1, 0,  0, (1, 10),  0,  0, "Reality warps around you. Nothing here follows natural law."),
    };

    // Fallback for an unmapped BiomeType (every member is mapped above).
    private static readonly BiomeConfig _fallback = new(
        DisplayName: "Unknown",
        SatietyDrainBonus: 0,
        SlipChance: 0,
        StepPoisonChance: 0,
        EnvironmentDamage: (0, 0),
        VisionModifier: 0,
        AttackModifier: 0,
        EntryMessage: "");

    private static BiomeConfig Config => _configs.TryGetValue(Current, out var c) ? c : _fallback;

    // Map floor number to biome: named floors first, then banded eras.
    public static BiomeType GetBiome(int floor) => floor switch
    {
        // Floors 1-15, each named
        1 => BiomeType.Grassland,
        2 => BiomeType.Grassland,    // savanna/mesa
        3 => BiomeType.Forest,       // dark ancient forest
        4 => BiomeType.Aquatic,      // waterways/lake
        5 => BiomeType.Ruins,        // ancient ruins
        6 => BiomeType.Swamp,        // swamp
        7 => BiomeType.Forest,       // rainforest
        8 => BiomeType.Ice,          // winter/flooded forest
        9 => BiomeType.Forest,       // enchanted forest
        10 => BiomeType.Urban,       // Japanese town
        11 => BiomeType.Ice,         // Taft — mountain trails, alpine dusk, mine labyrinth
        12 => BiomeType.Aquatic,     // seashore
        13 => BiomeType.Volcanic,    // volcano
        14 => BiomeType.Forest,      // dense forest
        15 => BiomeType.Volcanic,    // crimson era peak

        // Per-floor overrides inside otherwise-banded ranges, for named floors whose setting
        // differs from the surrounding band.
        //
        // The Dark and Ruins bands carry CaveMode, so a floor left in one of them does not merely
        // wear the wrong palette — it generates as a cellular-automata cavern, which contradicts a
        // floor that is a city, a lake resort or a forest. Several overrides below lift a floor
        // out of a Dark or Ruins band for that reason.
        22 => BiomeType.Forest,      // Coral Village — lakeside forest, Forest of Wandering, Yui found here
        24 => BiomeType.Aquatic,     // Panareze — lake resort; the same lake-town reading as Selmburg
        35 => BiomeType.Grassland,   // Mishe — gentle hills, Hill of Memories, pastoral spring
        48 => BiomeType.Ice,         // Lindarth — ice canyon, Lisbeth's smithy, snowstorms
        49 => BiomeType.Urban,       // Myujen — white-stone city, marble architecture
        50 => BiomeType.Urban,       // Algade — the commercial hub of the KoB ascension era
        55 => BiomeType.Urban,       // Granzam — iron fortress-city, Knights of the Blood HQ
        56 => BiomeType.Forest,      // Pani — forest outpost, the purchasable Forest House
        61 => BiomeType.Aquatic,     // Selmburg — mist-shrouded lake town, perpetual fog
        74 => BiomeType.Grassland,   // Collinia — grand plains, the Gleam Eyes arena
        75 => BiomeType.Dark,        // Collinia Labyrinth — bone dungeon, Skull Reaper, red gloom
        76 => BiomeType.Urban,       // Algade — gambling city, casino district and arena
        78 => BiomeType.Forest,      // forest floor of the demigod era
        79 => BiomeType.Ice,         // ice plains
        // There is no sky biome, so a sanctum above the cloud line takes the same reading as the
        // celestial garden (F90) and the palace approach (F96-99).
        80 => BiomeType.Void,        // sky sanctum
        85 => BiomeType.Volcanic,    // volcanic ruin

        // Cycle through biomes in 5-floor brackets for mid-game
        >= 16 and <= 20 => BiomeType.Ice,       // crystal era
        >= 21 and <= 25 => BiomeType.Dark,      // twilight era (F22, F24 overridden above)
        >= 26 and <= 30 => BiomeType.Forest,    // jungle era
        >= 31 and <= 35 => BiomeType.Ruins,     // fortress era (F35 overridden above)
        >= 36 and <= 40 => BiomeType.Volcanic,  // volcanic era
        >= 41 and <= 45 => BiomeType.Aquatic,   // ocean era
        >= 46 and <= 50 => BiomeType.Dark,      // nightmare era (F48-F50 overridden above)
        >= 51 and <= 55 => BiomeType.Forest,    // ancient forest (F55 overridden above)
        >= 56 and <= 57 => BiomeType.Ruins,     // mountain era (F56 overridden above)
        // The weather table puts a sandstorm on F59, and F58-60 is a frontier band with no named
        // floor, so a desert sits here without displacing one. This is the only Desert band.
        >= 58 and <= 60 => BiomeType.Desert,    // rugged frontier — F59 sandstorm
        >= 61 and <= 65 => BiomeType.Volcanic,  // infernal era (F61 overridden above)
        >= 66 and <= 70 => BiomeType.Void,      // cosmic era
        >= 71 and <= 75 => BiomeType.Ice,       // legendary era (F74-75 overridden above)
        >= 76 and <= 80 => BiomeType.Dark,      // demigod era (F76, F78-F80 overridden above)
        >= 81 and <= 85 => BiomeType.Swamp,     // nightmare difficulty (F85 overridden above)
        >= 86 and <= 90 => BiomeType.Void,      // godlike era
        >= 91 and <= 95 => BiomeType.Volcanic,  // impossible era
        >= 96 and <= 99 => BiomeType.Void,      // ruby palace approach
        100 => BiomeType.Urban,                  // Ruby Palace -- stone castle
        _ => BiomeType.Grassland,
    };

    // Sub-variant of the floor's biome, or null to use the biome's own config.
    //
    // A biome that holds a long band would generate the same floor over and over. A variant names
    // a Content/Biomes/<biome>_<variant>.json that replaces the base config for those floors, so
    // the band splits into recognisably different places without a second BiomeType or a second
    // code path.
    //
    // Keyed off GetBiome, so a floor that is not Forest can never carry a Forest variant.
    public static string? GetVariant(int floor) => GetBiome(floor) switch
    {
        // Ice holds two very different places: the frozen-lake band F16-20 and F48's ice
        // canyon on one side, and Taft's mountain trails on the other. The base config drowns a
        // trail floor in frozen pools, so Taft takes a variant that trades the water for relief.
        BiomeType.Ice => floor == 11 ? "alpine" : null,
        BiomeType.Forest => floor switch
        {
            3 => "ancient",                 // Zumfut — Elf War, perpetual canopy twilight
            7 => "jungle",                  // Lectea — rainforest tier
            9 => "enchanted",               // Stachion — enchanted forest
            22 => "enchanted",              // Coral Village — Forest of Wandering, golden hour
            >= 26 and <= 30 => "jungle",    // jungle era
            >= 51 and <= 54 => "ancient",   // ancient forest, deep canopy
            // F14 stays on the base config deliberately, so the base Forest file still generates
            // a floor of its own.
            _ => null,
        },
        _ => null,
    };

    // Every (biome, variant) pair the table above can produce, derived by sweeping the climb
    // rather than hand-listed beside it — a hand-written copy of a table is the thing that drifts.
    // The loader validates each one at load, so a declared variant with no file fails loudly.
    public static IReadOnlyList<(BiomeType Biome, string Variant)> DeclaredVariants()
    {
        var seen = new List<(BiomeType, string)>();
        for (int f = 1; f <= 100; f++)
        {
            string? v = GetVariant(f);
            if (v == null) continue;
            var pair = (GetBiome(f), v);
            if (!seen.Contains(pair)) seen.Add(pair);
        }
        return seen;
    }

    // Cached BiomeGenConfig for the current floor. Populated by SetFloor so
    // rendering + ambient particle passes can read tint/entry/tree-glyph data.
    public static Map.Generation.BiomeGenConfig? CurrentGenConfig { get; private set; }

    // Set biome for current floor. Called on floor entry.
    public static void SetFloor(int floor)
    {
        Current = GetBiome(floor);
        // The variant, when the floor declares one, is what generation used — so the render side
        // has to resolve the same one or the tint, the tree glyph and the atmosphere would all
        // describe a floor that was not built.
        CurrentGenConfig = Map.Generation.BiomeGenConfigLoader.VariantFor(Current, floor)
                           ?? Map.Generation.BiomeGenConfigLoader.Get(Current);
        // Per-biome tree glyph override so Forest/Swamp/Ice trees differ visibly.
        Map.TileDefinitions.CurrentTreeGlyph = CurrentGenConfig?.TreeGlyph ?? '♣';
    }

    // The current biome's passive effect in one short phrase, for the sidebar, so the player can
    // connect a penalty (a Darkness floor's sight loss, say) to the floor rather than finding the
    // number only in the Player Guide. Built from the same config the Guide's biome table is
    // generated from, so the two cannot disagree.
    // Kept per config, which is immutable: the sidebar reads this on every redraw.
    public static string EffectSummary
    {
        get
        {
            var c = Config;
            if (!ReferenceEquals(c, _summaryOf)) { _summary = Summarize(c); _summaryOf = c; }
            return _summary;
        }
    }
    private static BiomeConfig? _summaryOf;
    private static string _summary = "";

    private static string Summarize(BiomeConfig c)
    {
        if (c.VisionModifier != 0) return $"{c.VisionModifier} vis";
        if (c.EnvironmentDamage.Interval > 0)
            return $"{c.EnvironmentDamage.Damage}dmg/{c.EnvironmentDamage.Interval}t";
        if (c.AttackModifier != 0) return $"{c.AttackModifier} ATK";
        if (c.StepPoisonChance > 0) return $"{c.StepPoisonChance}% psn";
        if (c.SlipChance > 0) return $"{c.SlipChance}% slip";
        if (c.SatietyDrainBonus > 0) return $"+{c.SatietyDrainBonus} hunger";
        return string.Empty;
    }

    // Read-only access to any biome's config, not just the current floor's. The Player Guide's
    // biome table is generated from it.
    public static BiomeConfig ConfigFor(BiomeType biome) =>
        _configs.TryGetValue(biome, out var c) ? c : _fallback;

    // Display name for the current biome.
    public static string DisplayName => Config.DisplayName;

    // -- Per-turn passive effects --

    // Extra satiety drain per turn (desert = thirst, void = corruption).
    public static int SatietyDrainBonus => Config.SatietyDrainBonus;

    // Chance (%) of slipping on ice per movement step (lose the turn).
    public static int SlipChance => Config.SlipChance;

    // Poison chance (%) per step in swamp.
    public static int StepPoisonChance => Config.StepPoisonChance;

    // Passive damage per N turns from the environment.
    // Returns (damage, interval). 0 damage = no effect.
    public static (int Damage, int Interval) EnvironmentDamage => Config.EnvironmentDamage;

    // Vision radius modifier (negative = reduced).
    public static int VisionModifier => Config.VisionModifier;

    // Combat modifiers.
    public static int AttackModifier => Config.AttackModifier;

    // Floor entry flavor message. A floor may override the biome's line with one of its own from
    // Content/FloorEntries.json; a biome's message otherwise describes every floor in its band
    // identically.
    public static string GetEntryMessage(int floor) => FloorEntries.For(floor) ?? Config.EntryMessage;
}
