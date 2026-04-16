namespace SAOTRPG.Systems;

// Floor biome system -- maps each floor to a biome type that applies
// passive gameplay effects. Based on SAO canon floor themes where known,
// with original designs filling the gaps.
// Effects are checked per-turn in TurnManager and on floor entry.

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

    // All per-biome data in one table. To add a field, extend BiomeConfig
    // and populate it here -- no new switches required.
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

    // Fallback used when a BiomeType has no entry (shouldn't happen, but
    // preserves the original switch defaults for safety).
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

    // Map floor number to biome using SAO canon + original designs.
    public static BiomeType GetBiome(int floor) => floor switch
    {
        // SAO canon floors
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
        11 => BiomeType.Desert,      // desert
        12 => BiomeType.Aquatic,     // seashore
        13 => BiomeType.Volcanic,    // volcano
        14 => BiomeType.Forest,      // dense forest
        15 => BiomeType.Volcanic,    // crimson era peak

        // Cycle through biomes in 5-floor brackets for mid-game
        >= 16 and <= 20 => BiomeType.Ice,       // crystal era
        >= 21 and <= 25 => BiomeType.Dark,      // twilight era
        >= 26 and <= 30 => BiomeType.Forest,    // jungle era
        >= 31 and <= 35 => BiomeType.Ruins,     // fortress era
        >= 36 and <= 40 => BiomeType.Volcanic,  // volcanic era
        >= 41 and <= 45 => BiomeType.Aquatic,   // ocean era
        >= 46 and <= 50 => BiomeType.Dark,      // nightmare era
        >= 51 and <= 55 => BiomeType.Forest,    // ancient forest
        >= 56 and <= 60 => BiomeType.Ruins,     // mountain era
        >= 61 and <= 65 => BiomeType.Volcanic,  // infernal era
        >= 66 and <= 70 => BiomeType.Void,      // cosmic era
        >= 71 and <= 75 => BiomeType.Ice,       // legendary era
        >= 76 and <= 80 => BiomeType.Dark,      // demigod era
        >= 81 and <= 85 => BiomeType.Swamp,     // nightmare difficulty
        >= 86 and <= 90 => BiomeType.Void,      // godlike era
        >= 91 and <= 95 => BiomeType.Volcanic,  // impossible era
        >= 96 and <= 99 => BiomeType.Void,      // ruby palace approach
        100 => BiomeType.Urban,                  // Ruby Palace -- stone castle
        _ => BiomeType.Grassland,
    };

    // Set biome for current floor. Called on floor entry.
    public static void SetFloor(int floor) => Current = GetBiome(floor);

    // Display name for the biome.
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

    // Floor entry flavor message. `floor` reserved for future floor-specific
    // overrides; current behavior is purely biome-driven.
    public static string GetEntryMessage(int floor) => Config.EntryMessage;
}
