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
    // F1 TOB monument — opens MonumentDialog (species kill log + title browser).
    MonumentOfSwordsmen,
    DangerZone,

    // Services
    Anvil,
    BountyBoard,
    EnchantShrine,
    // One-shot weapon-discovery tile (WeaponEvolutionChains.SecretShrineByFloor). Grants T1 chain weapon on step-on.
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
