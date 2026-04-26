namespace SAOTRPG.Map;

// F1 Town of Beginnings — walled safe-zone city around spawn, embedded in worldmap.
// Procgen pipeline routes around via overlap + GameMap.SafeZone. Grid feel: cross-streets, campfire lamps, corner houses, park, N+S gates.
public static partial class MapGenerator
{
    // Town rect — moderate size so quadrant clearings/boss rooms coexist.
    private const int TownHalfW = 25;
    private const int TownHalfH = 14;
    private const int TownFullW = TownHalfW * 2 + 1;
    private const int TownFullH = TownHalfH * 2 + 1;

    // Builds the town centered on (sx,sy); returns bounding rect for GameMap.SafeZone.
    internal static Room BuildTownOfBeginnings(GameMap map, List<Room> rooms, int sx, int sy)
    {
        int tx = sx - TownHalfW;   // town left edge
        int ty = sy - TownHalfH;   // town top edge

        // ── 1. Clear interior to Floor ───────────────────────────────
        for (int x = tx; x < tx + TownFullW; x++)
        for (int y = ty; y < ty + TownFullH; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;

        // ── 2. Perimeter stone wall ──────────────────────────────────
        StampWallRect(map, tx, ty, TownFullW, TownFullH);

        // ── 3. Gates (north + south) ─────────────────────────────────
        SetGate(map, sx, ty);                           // north gate
        SetGate(map, sx, ty + TownFullH - 1);           // south gate

        // ── 4. Black Iron Palace — boulevard runs through it ─────────
        int palW = 25, palH = 8;
        int palX = sx - palW / 2;
        int palY = ty + 2;
        BuildStructure(map, palX, palY, palW, palH);
        map.Tiles[sx, palY].Type = TileType.Door;       // north door
        // (BuildStructure already placed south door)
        int monX = palX + palW / 2, monY = palY + palH / 2;
        map.Tiles[monX, monY].Type = TileType.LoreStone; // Monument of Life
        for (int dx = -4; dx <= 4; dx += 4)
        {
            if (dx == 0) continue;
            SetTileSafe(map, monX + dx, monY - 2, TileType.Pillar);
            SetTileSafe(map, monX + dx, monY + 2, TileType.Pillar);
        }
        rooms.Add(new Room(palX, palY, palW, palH));

        // ── 5. Central plaza fountain (spawn) ────────────────────────
        map.Tiles[sx, sy].Type = TileType.Fountain;
        (int dx, int dy)[] ring = { (-2,-2),(0,-2),(2,-2), (-2,0),(2,0), (-2,2),(0,2),(2,2) };
        foreach (var (dx, dy) in ring)
            SetTileSafe(map, sx + dx, sy + dy, TileType.Grass, TileType.Floor);

        // ── 6. Shop buildings (7×5 each) ─────────────────────────────
        BuildShop(map, rooms, sx - 18, sy - 4, TileType.Anvil,         doorSide: DoorSide.South); // NW: Blacksmith
        BuildShop(map, rooms, sx + 12, sy - 4, TileType.BountyBoard,   doorSide: DoorSide.South); // NE: Guild Hall
        BuildShop(map, rooms, sx - 18, sy + 4, TileType.Fountain,      doorSide: DoorSide.North); // SW: Item Shop
        BuildShop(map, rooms, sx + 12, sy + 4, TileType.EnchantShrine, doorSide: DoorSide.North); // SE: Enchant

        // ── 7. Church — south of the plaza ───────────────────────────
        int chW = 11, chH = 5;
        int chX = sx - chW / 2, chY = sy + 8;
        BuildStructure(map, chX, chY, chW, chH);
        map.Tiles[sx, chY + chH / 2].Type = TileType.Shrine;
        rooms.Add(new Room(chX, chY, chW, chH));

        // ── 8. Corner houses — fill space between shops and town wall.
        BuildHouse(map, rooms, tx + 2,           ty + 2);          // NW corner
        BuildHouse(map, rooms, tx + TownFullW - 7, ty + 2);       // NE corner
        BuildHouse(map, rooms, tx + 2,           ty + TownFullH - 7); // SW corner
        BuildHouse(map, rooms, tx + TownFullW - 7, ty + TownFullH - 7); // SE corner

        // ── 9. Paved boulevard (N-S through palace to plaza) ─────────
        PaveVertical(map, sx, ty + 1, palY);            // gate → palace N door
        PaveVertical(map, sx, palY + palH, sy);         // palace S door → plaza
        PaveVertical(map, sx, sy + 1, chY);             // plaza → church
        PaveVertical(map, sx, chY + chH, ty + TownFullH - 1); // church → S gate

        // ── 10. E-W cross-streets — 2 horizontal roads linking boulevard to shop entrances on both sides.
        PaveHorizontal(map, tx + 1, tx + TownFullW - 1, sy - 2);  // upper shop row
        PaveHorizontal(map, tx + 1, tx + TownFullW - 1, sy + 6);  // lower shop row

        // ── 11. Street lamps (campfires) — RGB lighting casts warm orange pools.
        PlaceStreetLamps(map, sx, ty, TownFullW, TownFullH, sy);

        // ── 12. Grass park — small garden between church and south wall
        int parkCx = sx, parkCy = sy + TownHalfH - 2;
        for (int dx = -3; dx <= 3; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            int px = parkCx + dx, py = parkCy + dy;
            if (!map.InBounds(px, py) || map.Tiles[px, py].Type != TileType.Floor) continue;
            map.Tiles[px, py].Type = TileType.Grass;
        }

        // ── 13. Monument of Swordsmen — canon TOB placement: plaza park west side, 2 tiles off boulevard, pillar-flanked.
        int swmX = sx - 4, swmY = parkCy;
        if (map.InBounds(swmX, swmY))
        {
            map.Tiles[swmX, swmY].Type = TileType.MonumentOfSwordsmen;
            SetTileSafe(map, swmX - 1, swmY, TileType.Pillar, TileType.Grass);
            SetTileSafe(map, swmX + 1, swmY, TileType.Pillar, TileType.Grass);
            SetTileSafe(map, swmX - 1, swmY, TileType.Pillar, TileType.Flowers);
            SetTileSafe(map, swmX + 1, swmY, TileType.Pillar, TileType.Flowers);
            SetTileSafe(map, swmX - 1, swmY, TileType.Pillar, TileType.Floor);
            SetTileSafe(map, swmX + 1, swmY, TileType.Pillar, TileType.Floor);
        }

        return new Room(tx, ty, TownFullW, TownFullH);
    }

    // ── Small helpers ────────────────────────────────────────────────

    private enum DoorSide { North, South }

    private static void BuildShop(GameMap map, List<Room> rooms, int sx, int sy, TileType feature, DoorSide doorSide)
    {
        const int w = 7, h = 5;
        BuildStructure(map, sx, sy, w, h);
        if (doorSide == DoorSide.North)
        {
            map.Tiles[sx + w / 2, sy + h - 1].Type = TileType.Wall;
            map.Tiles[sx + w / 2, sy].Type = TileType.Door;
        }
        map.Tiles[sx + w / 2, sy + h / 2].Type = feature;
        rooms.Add(new Room(sx, sy, w, h));
    }

    // Tiny 5×5 walled house with a south door. No feature tile inside.
    private static void BuildHouse(GameMap map, List<Room> rooms, int sx, int sy)
    {
        BuildStructure(map, sx, sy, 5, 5);
        rooms.Add(new Room(sx, sy, 5, 5));
    }

    private static void StampWallRect(GameMap map, int sx, int sy, int w, int h)
    {
        for (int x = sx; x < sx + w; x++)
        {
            SetTileSafe(map, x, sy,         TileType.Wall);
            SetTileSafe(map, x, sy + h - 1, TileType.Wall);
        }
        for (int y = sy; y < sy + h; y++)
        {
            SetTileSafe(map, sx, y,         TileType.Wall);
            SetTileSafe(map, sx + w - 1, y, TileType.Wall);
        }
    }

    private static void SetGate(GameMap map, int x, int y)
    {
        SetTileSafe(map, x, y, TileType.Door);
        SetTileSafe(map, x - 2, y, TileType.Pillar);
        SetTileSafe(map, x + 2, y, TileType.Pillar);
    }

    private static void PaveVertical(GameMap map, int x, int yStart, int yEnd)
    {
        int step = yStart < yEnd ? 1 : -1;
        for (int y = yStart; y != yEnd; y += step)
            if (map.InBounds(x, y) && map.Tiles[x, y].Type == TileType.Floor)
                map.Tiles[x, y].Type = TileType.Path;
    }

    private static void PaveHorizontal(GameMap map, int xStart, int xEnd, int y)
    {
        for (int x = xStart; x <= xEnd; x++)
            if (map.InBounds(x, y) && map.Tiles[x, y].Type == TileType.Floor)
                map.Tiles[x, y].Type = TileType.Path;
    }

    private static void PlaceStreetLamps(GameMap map, int sx, int ty, int fullW, int fullH, int plazaY)
    {
        int tx = sx - fullW / 2;
        // Boulevard lamps every 6 tiles
        for (int y = ty + 1; y < ty + fullH - 1; y += 6)
        {
            SetTileSafe(map, sx - 1, y, TileType.Campfire, TileType.Floor);
            SetTileSafe(map, sx + 1, y, TileType.Campfire, TileType.Floor);
        }
        // Cross-street lamps
        for (int x = tx + 3; x < tx + fullW - 3; x += 8)
        {
            if (Math.Abs(x - sx) < 3) continue; // don't crowd the boulevard
            SetTileSafe(map, x, plazaY - 2, TileType.Campfire, TileType.Floor);
            SetTileSafe(map, x, plazaY + 6, TileType.Campfire, TileType.Floor);
        }
    }

    // Set a tile if in bounds and (optionally) only if it currently equals a required type.
    private static void SetTileSafe(GameMap map, int x, int y, TileType type, TileType? requiredCurrent = null)
    {
        if (!map.InBounds(x, y)) return;
        if (requiredCurrent != null && map.Tiles[x, y].Type != requiredCurrent.Value) return;
        map.Tiles[x, y].Type = type;
    }
}
