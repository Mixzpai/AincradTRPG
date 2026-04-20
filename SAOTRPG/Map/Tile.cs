using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Map;

// Single map cell: terrain, optional occupant, ground items. Movement + rendering read from Tile props.
public class Tile
{
    public TileType Type { get; set; }
    public Entity? Occupant { get; set; }
    public List<BaseItem> Items { get; } = new();

    // Water + WaterDeep block at tile-level; player bypasses via Swimming skill, aquatic mobs via Monster.CanSwim.
    public bool BlocksMovement => Type is TileType.Wall or TileType.CrackedWall or TileType.Water
                                    or TileType.WaterDeep or TileType.Mountain or TileType.Tree
                                    or TileType.TreePine or TileType.Rock;
    public bool IsWalkable => !BlocksMovement && Occupant == null;

    // Min Swimming life-skill level to enter: -1 non-water, 1 shallow Water, 25 deep WaterDeep.
    public int RequiresSwimmingLevel => Type switch
    {
        TileType.Water     => 1,
        TileType.WaterDeep => 25,
        _                  => -1,
    };
    public bool HasItems => Items.Count > 0;
    // Trap tiles render as Floor until revealed by DEX check or trigger.
    public bool TrapHidden { get; set; } = true;
    // Lever/pressure plate → linked door (null if unlinked).
    public (int X, int Y)? LinkedDoor { get; set; }

    public Tile(TileType type)
    {
        Type = type;
    }
}
