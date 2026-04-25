using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation.Passes;

// Town overlay. F1 stamps Town of Beginnings (load-bearing prefab); F2+ clears
// a small plaza around spawn and builds a 4-cardinal town around it.
public sealed class SpecialAreaPass : IGenerationPass
{
    public string Name => "SpecialArea";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        int spawnX = ctx.Width / 2, spawnY = ctx.Height / 2;
        ctx.SpawnX = spawnX;
        ctx.SpawnY = spawnY;

        if (ctx.FloorNumber == 1)
        {
            var townRect = MapGenerator.BuildTownOfBeginnings(map, ctx.Rooms, spawnX, spawnY);
            map.SafeZone = townRect;
            ctx.Rooms.Insert(0, new Room(spawnX - 4, spawnY - 4, 9, 9));
        }
        else
        {
            MapGenerator.ClearArea(map, spawnX, spawnY, 12, TileType.Grass);
            for (int x = spawnX - 3; x <= spawnX + 3; x++)
                for (int y = spawnY - 3; y <= spawnY + 3; y++)
                    if (map.InBounds(x, y) && rng.Next(5) == 0)
                        map.Tiles[x, y].Type = TileType.Flowers;
            ctx.Rooms.Add(new Room(spawnX - 5, spawnY - 5, 11, 11));
            MapGenerator.BuildTown(map, spawnX, spawnY, ctx.Rooms);
        }

        // Bundle 5: Ruins biome decays room walls into overgrowth. F1 is immune (town prefab is load-bearing).
        if (ctx.Biome == BiomeType.Ruins && ctx.FloorNumber != 1)
            ApplyRuinsDecay(ctx);
    }

    // Walks every room rect; destroys 40-60% of walls into Grass and scatters Bush overgrowth on floors.
    // Dedupe guard: overlapping rooms would otherwise re-roll decay on shared tiles.
    private static void ApplyRuinsDecay(WorldContext ctx)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        var decayed = new HashSet<(int, int)>();
        foreach (var room in ctx.Rooms)
        {
            float decay = 0.40f + (float)rng.NextDouble() * 0.20f;
            int x0 = room.X, y0 = room.Y;
            int x1 = room.X + room.Width, y1 = room.Y + room.Height;
            for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
            {
                if (!map.InBounds(x, y)) continue;
                if (!decayed.Add((x, y))) continue;
                var t = map.Tiles[x, y].Type;
                if (t == TileType.Wall && rng.NextDouble() < decay)
                    map.Tiles[x, y].Type = TileType.Grass;
                else if (t == TileType.Floor && rng.Next(8) == 0)
                    map.Tiles[x, y].Type = TileType.Bush;
            }
        }
    }
}
