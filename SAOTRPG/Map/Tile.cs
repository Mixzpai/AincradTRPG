using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Map;

public class Tile
{
    public TileType Type { get; set; }
    public Entity? Occupant { get; set; }
    public List<BaseItem> Items { get; } = new();

    public bool BlocksMovement => Type is TileType.Wall or TileType.Water or TileType.WaterDeep
                                    or TileType.Mountain or TileType.Tree or TileType.TreePine or TileType.Rock;
    public bool IsWalkable => !BlocksMovement && Occupant == null;
    public bool HasItems => Items.Count > 0;

    public Tile(TileType type)
    {
        Type = type;
    }
}
