using SAOTRPG.Entities;

namespace SAOTRPG.Map;

// Value-type cell: terrain + optional occupant + trap-hidden flag inline. Items + LinkedDoor on GameMap sparse dicts.
// Structs returned from GetTile() are copies — mutate via GameMap.RefTile / SetTileType / Set* helpers.
public struct Tile
{
    public TileType Type;
    public Entity? Occupant;
    public bool TrapHidden;

    public Tile(TileType type)
    {
        Type = type;
        Occupant = null;
        TrapHidden = true;
    }

    // Water + WaterDeep block at tile-level; player bypasses via Swimming skill, aquatic mobs via Monster.CanSwim.
    // Bundle 10: ore veins block until depleted (mining bump-action diverts before this gate).
    public bool BlocksMovement => Type is TileType.Wall or TileType.CrackedWall or TileType.Water
                                    or TileType.WaterDeep or TileType.Mountain or TileType.Tree
                                    or TileType.TreePine or TileType.Rock
                                    or TileType.BogWater
                                    or TileType.OreVeinIron or TileType.OreVeinMithril
                                    or TileType.OreVeinDivine;
    public bool IsWalkable => !BlocksMovement && Occupant == null;

    // Min Swimming life-skill level to enter: -1 non-water, 1 shallow Water/BogWater, 25 deep WaterDeep.
    public int RequiresSwimmingLevel => Type switch
    {
        TileType.Water     => 1,
        TileType.WaterDeep => 25,
        TileType.BogWater  => 1,
        _                  => -1,
    };
}
