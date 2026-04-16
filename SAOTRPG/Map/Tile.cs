using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Map;

// Single map cell — holds terrain type, an optional entity occupant, and ground items.
// Movement checks and rendering both read from Tile properties.
public class Tile
{
    // Terrain type that determines rendering, passability, and LOS blocking.
    public TileType Type { get; set; }
    // Entity standing on this tile, or null if empty.
    public Entity? Occupant { get; set; }
    // Items lying on the ground at this tile (picked up on walk-over).
    public List<BaseItem> Items { get; } = new();

    // True if this terrain type blocks entity movement.
    public bool BlocksMovement => Type is TileType.Wall or TileType.CrackedWall or TileType.Water
                                    or TileType.WaterDeep or TileType.Mountain or TileType.Tree
                                    or TileType.TreePine or TileType.Rock;
    // True if the tile is passable and unoccupied.
    public bool IsWalkable => !BlocksMovement && Occupant == null;
    // True if one or more items are on the ground here.
    public bool HasItems => Items.Count > 0;
    // If true, this trap tile renders as Floor until revealed by DEX check or triggering.
    public bool TrapHidden { get; set; } = true;
    // For levers/pressure plates: position of the door they toggle. Null if not linked.
    public (int X, int Y)? LinkedDoor { get; set; }

    public Tile(TileType type)
    {
        Type = type;
    }
}
