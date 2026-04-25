using SAOTRPG.Entities;
using SAOTRPG.Items;

namespace SAOTRPG.Map;

// Single map cell: terrain, optional occupant, ground items. Movement + rendering read from Tile props.
public class Tile
{
    public TileType Type { get; set; }
    public Entity? Occupant { get; set; }
    private List<BaseItem>? _items;
    // Lazy backing: 1M empty tiles previously held 1M empty list headers.
    public List<BaseItem> Items => _items ??= new List<BaseItem>();
    public int ItemCount => _items?.Count ?? 0;

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
    public bool HasItems => _items != null && _items.Count > 0;
    // Trap tiles render as Floor until revealed by DEX check or trigger.
    public bool TrapHidden { get; set; } = true;
    // Lever/pressure plate → linked door (null if unlinked).
    public (int X, int Y)? LinkedDoor { get; set; }

    public Tile(TileType type)
    {
        Type = type;
    }
}
