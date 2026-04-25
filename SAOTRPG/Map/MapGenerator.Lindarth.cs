namespace SAOTRPG.Map;

// F48 Lindarth — Lisbeth's smithing hub town. Walled mountain forge-city, larger
// than F1 TOB (60x35 vs 51x29). Anchors the Bundle 13 Lisbeth Workshop overhaul:
// central Forge, anvil cluster, Crystallite Refinery (Frost Dragon tie), Mithril
// Smelter, Material Vendor, Lisbeth's quarters, Lindarth Inn. Lisbeth NPC spawns
// at deterministic Forge interior coord (Population.cs:434).
public static partial class MapGenerator
{
    private const int LindarthHalfW = 30;
    private const int LindarthHalfH = 17;
    private const int LindarthFullW = LindarthHalfW * 2 + 1; // 61
    private const int LindarthFullH = LindarthHalfH * 2 + 1; // 35

    // Forge interior anchor offset relative to (sx,sy) — used by Population.cs to
    // place Lisbeth deterministically inside the Forge instead of a random room.
    public const int LindarthForgeOffsetX = 0;
    public const int LindarthForgeOffsetY = -3;

    // Builds Lindarth centered on (sx,sy); returns bounding rect for GameMap.SafeZone.
    internal static Room BuildLindarth(GameMap map, List<Room> rooms, int sx, int sy)
    {
        int tx = sx - LindarthHalfW;
        int ty = sy - LindarthHalfH;

        // 1. Clear interior to Floor.
        for (int x = tx; x < tx + LindarthFullW; x++)
        for (int y = ty; y < ty + LindarthFullH; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;

        // 2. Perimeter wall + N/S gates.
        StampWallRect(map, tx, ty, LindarthFullW, LindarthFullH);
        SetGate(map, sx, ty);
        SetGate(map, sx, ty + LindarthFullH - 1);

        // 3. Central Forge — large 13x9 anchor building, 3 tiles north of plaza.
        int forgeW = 13, forgeH = 9;
        int forgeX = sx - forgeW / 2;
        int forgeY = sy - forgeH / 2 + LindarthForgeOffsetY;
        BuildStructure(map, forgeX, forgeY, forgeW, forgeH);
        // Primary anvil at forge centerline; flanked by campfires for forge-fire flavor.
        int forgeAnvilX = sx, forgeAnvilY = forgeY + forgeH / 2;
        SetTileSafe(map, forgeAnvilX, forgeAnvilY, TileType.Anvil);
        SetTileSafe(map, forgeAnvilX - 2, forgeAnvilY, TileType.Campfire, TileType.Floor);
        SetTileSafe(map, forgeAnvilX + 2, forgeAnvilY, TileType.Campfire, TileType.Floor);
        rooms.Add(new Room(forgeX, forgeY, forgeW, forgeH));

        // 4. Anvil cluster — 4 anvils for player crafting, west of forge.
        int clusterX = sx - 18, clusterY = sy - 1;
        BuildStructure(map, clusterX - 1, clusterY - 2, 7, 7);
        SetTileSafe(map, clusterX,     clusterY,     TileType.Anvil);
        SetTileSafe(map, clusterX + 2, clusterY,     TileType.Anvil);
        SetTileSafe(map, clusterX,     clusterY + 2, TileType.Anvil);
        SetTileSafe(map, clusterX + 2, clusterY + 2, TileType.Anvil);
        rooms.Add(new Room(clusterX - 1, clusterY - 2, 7, 7));

        // 5. Crystallite Refinery — east of forge, canon tie to Bundle 11 Frost Dragon.
        int refW = 8, refH = 6;
        int refX = sx + 6, refY = sy - 5;
        BuildStructure(map, refX, refY, refW, refH);
        SetTileSafe(map, refX + refW / 2, refY + refH / 2, TileType.Fountain);
        SetTileSafe(map, refX + 2, refY + refH / 2, TileType.Pillar, TileType.Floor);
        SetTileSafe(map, refX + refW - 3, refY + refH / 2, TileType.Pillar, TileType.Floor);
        rooms.Add(new Room(refX, refY, refW, refH));

        // 6. Mithril Smelter — south-east of plaza, mithril work canon.
        int smW = 8, smH = 6;
        int smX = sx + 6, smY = sy + 4;
        BuildStructure(map, smX, smY, smW, smH);
        SetTileSafe(map, smX + smW / 2, smY + smH / 2, TileType.Anvil);
        SetTileSafe(map, smX + 1, smY + 1, TileType.Campfire, TileType.Floor);
        SetTileSafe(map, smX + smW - 2, smY + smH - 2, TileType.Campfire, TileType.Floor);
        rooms.Add(new Room(smX, smY, smW, smH));

        // 7. Material Vendor stall — small kiosk south-west.
        int vsX = sx - 12, vsY = sy + 6;
        BuildStructure(map, vsX, vsY, 6, 5);
        SetTileSafe(map, vsX + 3, vsY + 2, TileType.EnchantShrine);
        rooms.Add(new Room(vsX, vsY, 6, 5));

        // 8. Lisbeth's quarters — small residential, attached to forge south face.
        int lqX = forgeX + forgeW + 1, lqY = forgeY + forgeH - 5;
        BuildStructure(map, lqX, lqY, 5, 5);
        SetTileSafe(map, lqX + 2, lqY + 2, TileType.Campfire);
        rooms.Add(new Room(lqX, lqY, 5, 5));

        // 9. Lindarth Inn — north-west, rest stop for adventurers.
        int innW = 9, innH = 6;
        int innX = tx + 3, innY = ty + 3;
        BuildStructure(map, innX, innY, innW, innH);
        SetTileSafe(map, innX + innW / 2, innY + 2, TileType.Fountain);
        SetTileSafe(map, innX + 2, innY + 4, TileType.Campfire, TileType.Floor);
        rooms.Add(new Room(innX, innY, innW, innH));

        // 10. Plaza fountain at spawn + flower ring.
        map.Tiles[sx, sy].Type = TileType.Fountain;
        (int dx, int dy)[] ring = { (-2,-2),(0,-2),(2,-2), (-2,0),(2,0), (-2,2),(0,2),(2,2) };
        foreach (var (dx, dy) in ring)
            SetTileSafe(map, sx + dx, sy + dy, TileType.Flowers, TileType.Floor);

        // 11. Boulevard pavement — N gate to plaza, plaza to S gate.
        PaveVertical(map, sx, ty + 1, sy);
        PaveVertical(map, sx, sy + 1, ty + LindarthFullH - 1);

        // 12. E-W cross-streets connecting workshops.
        PaveHorizontal(map, tx + 1, tx + LindarthFullW - 1, sy - 6);
        PaveHorizontal(map, tx + 1, tx + LindarthFullW - 1, sy + 7);

        // 13. Street lamps — boulevard every 6 tiles + cross-street lamps.
        PlaceStreetLamps(map, sx, ty, LindarthFullW, LindarthFullH, sy);

        // 14. South park — patch of grass + flowers near south wall.
        int parkCx = sx, parkCy = sy + LindarthHalfH - 3;
        for (int dx = -4; dx <= 4; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            int px = parkCx + dx, py = parkCy + dy;
            if (!map.InBounds(px, py) || map.Tiles[px, py].Type != TileType.Floor) continue;
            map.Tiles[px, py].Type = (dx + dy) % 2 == 0 ? TileType.Grass : TileType.Flowers;
        }

        return new Room(tx, ty, LindarthFullW, LindarthFullH);
    }
}
