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
    DangerZone,

    // Services
    Anvil,
    BountyBoard,
    EnchantShrine,

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
