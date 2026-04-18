namespace SAOTRPG.Map;

// All tile types in the game world.
// Visual definitions for each type are in TileDefinitions.cs.
public enum TileType
{
    // Ground
    Grass,
    GrassTall,
    GrassSparse,
    Path,
    Floor,

    // Structures
    Wall,
    Door,
    StairsUp,
    StairsDown,

    // Hazards
    TrapSpike,
    TrapTeleport,
    Lava,

    // Special
    Campfire,
    Fountain,
    Shrine,
    Pillar,

    // Interactive
    Chest,
    ChestOpened,
    Journal,

    // Additional Hazards
    TrapPoison,
    TrapAlarm,

    // Hidden
    CrackedWall,

    // Environmental
    LoreStone,
    // FB-057 Monument of Swordsmen — F1 Town of Beginnings monument
    // that opens the MonumentDialog (species kill log + title browser).
    MonumentOfSwordsmen,
    DangerZone,

    // Services
    Anvil,
    BountyBoard,
    EnchantShrine,
    // Priority 5 Phase B: one-shot weapon-discovery tile. On floors listed in
    // WeaponEvolutionChains.SecretShrineByFloor, exactly one shrine is placed
    // in a random non-spawn room and grants a T1 chain weapon on step-on.
    SecretShrine,

    // Mechanical
    GasVent,
    Lever,
    PressurePlate,

    // Dungeon
    LabyrinthEntrance,

    // Nature
    Water,
    WaterDeep,
    Tree,
    TreePine,
    Bush,
    Mountain,
    Rock,
    Flowers
}
