using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Items.Definitions;
using SAOTRPG.Items.Equipment;

namespace SAOTRPG.Map;

public static class MapGenerator
{
    public static (GameMap Map, List<Room> Rooms) GenerateFloor(int floorNumber, int width = 160, int height = 80)
    {
        var map = new GameMap(width, height);
        var rooms = new List<Room>();

        // Base layer — mixed grass
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int r = Random.Shared.Next(100);
                map.Tiles[x, y].Type = r switch
                {
                    < 60 => TileType.Grass,
                    < 80 => TileType.GrassTall,
                    < 90 => TileType.GrassSparse,
                    < 95 => TileType.Flowers,
                    _    => TileType.Grass,
                };
            }
        }

        // Border — mountain walls
        for (int x = 0; x < width; x++)
        {
            map.Tiles[x, 0].Type = TileType.Mountain;
            map.Tiles[x, height - 1].Type = TileType.Mountain;
        }
        for (int y = 0; y < height; y++)
        {
            map.Tiles[0, y].Type = TileType.Mountain;
            map.Tiles[width - 1, y].Type = TileType.Mountain;
        }

        // ── Tree clusters (forests) ──
        int treeClusters = 14 + Random.Shared.Next(0, 8);
        for (int i = 0; i < treeClusters; i++)
        {
            int cx = Random.Shared.Next(8, width - 8);
            int cy = Random.Shared.Next(8, height - 8);
            int radius = Random.Shared.Next(3, 7);
            PlaceCluster(map, cx, cy, radius, TileType.Tree, 0.55);
            PlaceCluster(map, cx, cy, radius + 1, TileType.Bush, 0.15);
        }

        // ── Rock outcroppings ──
        int rockClusters = 5 + Random.Shared.Next(0, 4);
        for (int i = 0; i < rockClusters; i++)
        {
            int cx = Random.Shared.Next(10, width - 10);
            int cy = Random.Shared.Next(10, height - 10);
            int radius = Random.Shared.Next(2, 4);
            PlaceCluster(map, cx, cy, radius, TileType.Rock, 0.4);
            PlaceCluster(map, cx, cy, Math.Max(1, radius - 1), TileType.Mountain, 0.6);
        }

        // ── Ponds with deep centers ──
        int ponds = 1 + Random.Shared.Next(0, 3);
        for (int i = 0; i < ponds; i++)
        {
            int cx = Random.Shared.Next(20, width - 20);
            int cy = Random.Shared.Next(20, height - 20);
            int radius = Random.Shared.Next(4, 8);
            PlaceCluster(map, cx, cy, radius, TileType.Water, 0.6);
            PlaceCluster(map, cx, cy, Math.Max(1, radius - 2), TileType.WaterDeep, 0.7);
        }

        // ── Player spawn clearing (center) ──
        int spawnX = width / 2;
        int spawnY = height / 2;
        ClearArea(map, spawnX, spawnY, 5, TileType.Grass);
        for (int x = spawnX - 3; x <= spawnX + 3; x++)
            for (int y = spawnY - 3; y <= spawnY + 3; y++)
                if (map.InBounds(x, y) && Random.Shared.Next(5) == 0)
                    map.Tiles[x, y].Type = TileType.Flowers;

        rooms.Add(new Room(spawnX - 5, spawnY - 5, 11, 11));

        // ── Clearings connected by dirt paths ──
        var clearings = new List<(int x, int y)> { (spawnX, spawnY) };
        int clearingCount = 6 + Random.Shared.Next(0, 4);

        for (int i = 0; i < clearingCount; i++)
        {
            int cx = Random.Shared.Next(15, width - 15);
            int cy = Random.Shared.Next(15, height - 15);

            if (Math.Abs(cx - spawnX) < 14 && Math.Abs(cy - spawnY) < 14)
                continue;

            ClearArea(map, cx, cy, 3, TileType.Grass);
            rooms.Add(new Room(cx - 3, cy - 3, 7, 7));
            clearings.Add((cx, cy));
        }

        // ── Dirt paths between clearings ──
        for (int i = 0; i < clearings.Count - 1; i++)
            CarvePath(map, clearings[i].x, clearings[i].y, clearings[i + 1].x, clearings[i + 1].y);
        if (clearings.Count > 2)
            CarvePath(map, clearings[^1].x, clearings[^1].y, clearings[0].x, clearings[0].y);

        // ── Boss structure in far corner ──
        int bossX = width - 22;
        int bossY = height - 22;
        CarvePath(map, spawnX, spawnY, bossX + 6, bossY + 10);
        BuildStructure(map, bossX, bossY, 13, 11);
        rooms.Add(new Room(bossX, bossY, 13, 11));
        map.Tiles[bossX + 6, bossY + 5].Type = TileType.StairsUp;

        // ── Campfires (one per clearing, not spawn) ──
        for (int i = 1; i < clearings.Count; i++)
        {
            if (Random.Shared.Next(3) == 0) continue; // 33% skip
            int fx = clearings[i].x + Random.Shared.Next(-2, 3);
            int fy = clearings[i].y + Random.Shared.Next(-2, 3);
            if (map.InBounds(fx, fy) && IsGrassType(map.Tiles[fx, fy].Type))
                map.Tiles[fx, fy].Type = TileType.Campfire;
        }

        // ── Landmarks — fountain, shrine, pillar (1 each, random clearings) ──
        // Add new landmark types by extending TileType + TileDefinitions + TurnManager tile effects.
        TileType[] landmarks = { TileType.Fountain, TileType.Shrine, TileType.Pillar };
        foreach (var landmark in landmarks)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int lx = Random.Shared.Next(10, width - 10);
                int ly = Random.Shared.Next(10, height - 10);
                if (IsGrassType(map.Tiles[lx, ly].Type)
                    && Math.Abs(lx - spawnX) > 10 && Math.Abs(ly - spawnY) > 10)
                {
                    map.Tiles[lx, ly].Type = landmark;
                    break;
                }
            }
        }

        // ── Lava pools — small hazardous patches, more on higher floors ──
        int lavaPools = Math.Max(0, floorNumber - 1) + Random.Shared.Next(0, 3);
        for (int i = 0; i < lavaPools; i++)
        {
            int lx = Random.Shared.Next(12, width - 12);
            int ly = Random.Shared.Next(12, height - 12);
            if (Math.Abs(lx - spawnX) < 10 && Math.Abs(ly - spawnY) < 10) continue;
            int radius = Random.Shared.Next(1, 3);
            PlaceCluster(map, lx, ly, radius, TileType.Lava, 0.5);
        }

        // ── Mini-boss room — locked structure with a strong enemy + rare loot ──
        if (clearings.Count > 3)
        {
            var (mbx, mby) = clearings[clearings.Count / 2];  // pick a middle clearing
            int mbRoomX = mbx - 4, mbRoomY = mby - 3;
            BuildStructure(map, mbRoomX, mbRoomY, 9, 7);
            rooms.Add(new Room(mbRoomX, mbRoomY, 9, 7));
        }

        // ── Scatter traps ──
        int trapCount = 4 + floorNumber * 2 + Random.Shared.Next(0, 4);
        for (int i = 0; i < trapCount; i++)
        {
            int x = Random.Shared.Next(8, width - 8);
            int y = Random.Shared.Next(8, height - 8);
            if (IsGrassType(map.Tiles[x, y].Type) || map.Tiles[x, y].Type == TileType.Path)
            {
                // Don't place traps near spawn
                if (Math.Abs(x - spawnX) < 8 && Math.Abs(y - spawnY) < 8) continue;
                map.Tiles[x, y].Type = Random.Shared.Next(3) == 0 ? TileType.TrapTeleport : TileType.TrapSpike;
            }
        }

        // ── Scatter individual trees and bushes ──
        for (int i = 0; i < 60; i++)
        {
            int x = Random.Shared.Next(3, width - 3);
            int y = Random.Shared.Next(3, height - 3);
            if (IsGrassType(map.Tiles[x, y].Type))
                map.Tiles[x, y].Type = TileType.Tree;
        }
        for (int i = 0; i < 40; i++)
        {
            int x = Random.Shared.Next(3, width - 3);
            int y = Random.Shared.Next(3, height - 3);
            if (IsGrassType(map.Tiles[x, y].Type))
                map.Tiles[x, y].Type = TileType.Bush;
        }

        return (map, rooms);
    }

    public static void PopulateFloor(GameMap map, List<Room> rooms, Player player,
        int floorNumber, int mobStatScale = 100)
    {
        if (rooms.Count == 0) return;

        map.PlaceEntity(player, rooms[0].CenterX, rooms[0].CenterY);

        for (int i = 1; i < rooms.Count - 1; i++)
        {
            int mobCount = Random.Shared.Next(2, 5);
            for (int m = 0; m < mobCount; m++)
            {
                var mob = MobFactory.CreateFloorMob(floorNumber, mobStatScale);
                var (x, y) = FindOpenSpot(map, rooms[i]);
                if (x >= 0) map.PlaceEntity(mob, x, y);
            }
        }

        int wanderingMobs = 12 + Random.Shared.Next(0, 8);
        for (int i = 0; i < wanderingMobs; i++)
        {
            var mob = MobFactory.CreateFloorMob(floorNumber, mobStatScale);
            var (x, y) = FindOpenSpotAnywhere(map);
            if (x >= 0) map.PlaceEntity(mob, x, y);
        }

        if (rooms.Count > 1)
        {
            var boss = BossFactory.CreateFloorBoss(floorNumber);
            var (bx, by) = FindOpenSpot(map, rooms[^1]);
            if (bx >= 0) map.PlaceEntity(boss, bx, by);
        }

        if (rooms.Count > 1)
        {
            var vendor = new Vendor
            {
                Name = "Agatha the Merchant",
                Id = Random.Shared.Next(10000, 19999),
                ShopName = "Agatha's Wares"
            };
            vendor.GenerateStock(floorNumber);
            int vx = rooms[0].CenterX + 4, vy = rooms[0].CenterY - 1;
            if (map.InBounds(vx, vy) && IsWalkableType(map.Tiles[vx, vy].Type) && map.Tiles[vx, vy].Occupant == null)
                map.PlaceEntity(vendor, vx, vy);
        }

        if (rooms.Count > 2)
        {
            string[] kleinDialogue =
            {
                "Hey! Watch your back out there, these mobs hit hard.",
                "I heard the boss is deep in the structure to the southeast.",
                "Man, I could really go for some pizza right about now...",
                "Stick to the paths if you don't want to get ambushed.",
            };
            var npc1 = new WorldSpawn
            {
                Name = "Klein",
                Id = Random.Shared.Next(10000, 19999),
                Dialogue = kleinDialogue[Random.Shared.Next(kleinDialogue.Length)]
            };
            var (nx, ny) = FindOpenSpot(map, rooms[1]);
            if (nx >= 0) map.PlaceEntity(npc1, nx, ny);
        }

        if (rooms.Count > 3)
        {
            string[] argoDialogue =
            {
                "Information isn't free, you know... but I'll give you this one: check the clearings for loot.",
                "The vendor near spawn sells gear if you've got the Col.",
                "Floor boss is no joke. Make sure you're geared up first.",
                "Psst... monsters drop better stuff the further you go from spawn.",
            };
            var npc2 = new WorldSpawn
            {
                Name = "Argo the Rat",
                Id = Random.Shared.Next(10000, 19999),
                Dialogue = argoDialogue[Random.Shared.Next(argoDialogue.Length)]
            };
            var (nx, ny) = FindOpenSpot(map, rooms[2]);
            if (nx >= 0) map.PlaceEntity(npc2, nx, ny);
        }

        // ── Mini-boss — spawned in the mini-boss room (second-to-last room) with rare loot ──
        if (rooms.Count > 3)
        {
            var mbRoom = rooms[rooms.Count - 2];  // mini-boss room
            var miniBoss = MobFactory.CreateFloorMob(floorNumber, mobStatScale);
            // Upgrade to mini-boss: 2x stats, guaranteed rare+ drop
            miniBoss.Name = $"Mini-Boss {miniBoss.Name}";
            miniBoss.Variant = "Champion";
            miniBoss.BaseAttack = miniBoss.BaseAttack * 2;
            miniBoss.BaseDefense = miniBoss.BaseDefense * 2;
            miniBoss.MaxHealth = miniBoss.MaxHealth * 2;
            miniBoss.CurrentHealth = miniBoss.MaxHealth;
            miniBoss.ExperienceYield = miniBoss.ExperienceYield * 3;
            miniBoss.ColYield = miniBoss.ColYield * 3;
            miniBoss.Level += 3;
            miniBoss.SetAppearance(char.ToUpper(miniBoss.Symbol), Terminal.Gui.Color.BrightMagenta);
            var (mbx, mby) = FindOpenSpot(map, mbRoom);
            if (mbx >= 0) map.PlaceEntity(miniBoss, mbx, mby);

            // Guaranteed rare+ equipment drop on the floor nearby
            var mbTile = map.Tiles[mbRoom.CenterX, mbRoom.CenterY];
            int rareRoll = Random.Shared.Next(100);
            BaseItem? reward = rareRoll < 50
                ? new Weapon
                {
                    Name = "Guardian's Blade", Value = 200 + floorNumber * 60,
                    Rarity = rareRoll < 15 ? "Epic" : "Rare",
                    ItemDurability = 60 + floorNumber * 15,
                    RequiredLevel = Math.Max(1, floorNumber), EquipmentType = "Weapon",
                    WeaponType = "One-Handed Sword",
                    BaseDamage = 8 + floorNumber * 4,
                    AttackSpeed = 1, Range = 1,
                    Bonuses = new StatModifierCollection().Add(StatType.Attack, 8 + floorNumber * 4)
                }
                : new Armor
                {
                    Name = "Guardian's Plate", Value = 180 + floorNumber * 50,
                    Rarity = rareRoll < 65 ? "Epic" : "Rare",
                    ItemDurability = 70 + floorNumber * 15,
                    RequiredLevel = Math.Max(1, floorNumber), EquipmentType = "Armor",
                    ArmorSlot = "Chest",
                    BaseDefense = 5 + floorNumber * 3, Weight = 6,
                    Bonuses = new StatModifierCollection().Add(StatType.Defense, 5 + floorNumber * 3)
                };
            if (reward != null) mbTile.Items.Add(reward);
        }

        // ── Treasure chests — loot scattered in clearings ──
        // (ChestCount, Contents) scale with floor. Add new loot by extending the chest logic below.
        int chestCount = 2 + floorNumber + Random.Shared.Next(0, 3);
        for (int i = 0; i < chestCount; i++)
        {
            var (cx, cy) = FindOpenSpotAnywhere(map);
            if (cx < 0) continue;
            var tile = map.Tiles[cx, cy];

            // Treasure room traps — 35% chance to place a spike trap adjacent to loot
            if (Random.Shared.Next(100) < 35)
            {
                int tdx = Random.Shared.Next(-1, 2), tdy = Random.Shared.Next(-1, 2);
                int tx = cx + tdx, ty = cy + tdy;
                if (map.InBounds(tx, ty) && (IsGrassType(map.Tiles[tx, ty].Type) || map.Tiles[tx, ty].Type == TileType.Path))
                    map.Tiles[tx, ty].Type = TileType.TrapSpike;
            }

            // Each chest drops 1-3 items based on a weighted roll
            int rolls = 1 + Random.Shared.Next(0, 3);
            for (int r = 0; r < rolls; r++)
            {
                int loot = Random.Shared.Next(100);
                BaseItem item = loot switch
                {
                    < 35 => PotionDefinitions.CreateHealthPotion(),
                    < 50 => FoodDefinitions.CreateBread(),
                    < 65 => PotionDefinitions.CreateAntidote(),
                    < 80 => FoodDefinitions.CreateGrilledMeat(),
                    < 92 => PotionDefinitions.CreateGreaterHealthPotion(),
                    _    => PotionDefinitions.CreateBattleElixir(),
                };
                tile.Items.Add(item);
            }
        }
    }

    private static bool IsGrassType(TileType t) =>
        t == TileType.Grass || t == TileType.GrassTall || t == TileType.GrassSparse || t == TileType.Flowers;

    private static bool IsWalkableType(TileType t) =>
        IsGrassType(t) || t == TileType.Floor || t == TileType.Path || t == TileType.Door;

    private static void PlaceCluster(GameMap map, int cx, int cy, int radius, TileType type, double density)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if (!map.InBounds(x, y) || x <= 0 || y <= 0 || x >= map.Width - 1 || y >= map.Height - 1)
                    continue;
                double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (dist <= radius && Random.Shared.NextDouble() < density)
                    map.Tiles[x, y].Type = type;
            }
    }

    private static void ClearArea(GameMap map, int cx, int cy, int radius, TileType fill)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
            for (int y = cy - radius; y <= cy + radius; y++)
                if (map.InBounds(x, y) && x > 0 && y > 0 && x < map.Width - 1 && y < map.Height - 1)
                    map.Tiles[x, y].Type = fill;
    }

    private static void CarvePath(GameMap map, int x1, int y1, int x2, int y2)
    {
        int x = x1, y = y1;
        while (x != x2 || y != y2)
        {
            SetPathTile(map, x, y);
            if (Random.Shared.Next(3) > 0) SetPathTile(map, x + 1, y);
            if (Random.Shared.Next(3) > 0) SetPathTile(map, x, y + 1);

            int dx = Math.Sign(x2 - x);
            int dy = Math.Sign(y2 - y);

            if (Random.Shared.Next(4) == 0 && dx != 0 && dy != 0)
            {
                if (Random.Shared.Next(2) == 0) dx = 0;
                else dy = 0;
            }

            if (dx != 0 && dy != 0)
            {
                if (Random.Shared.Next(2) == 0) x += dx;
                else y += dy;
            }
            else { x += dx; y += dy; }
        }
        SetPathTile(map, x2, y2);
    }

    private static void SetPathTile(GameMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        var t = map.Tiles[x, y].Type;
        if (IsGrassType(t) || t == TileType.Bush)
            map.Tiles[x, y].Type = TileType.Path;
    }

    private static void BuildStructure(GameMap map, int sx, int sy, int w, int h)
    {
        for (int x = sx; x < sx + w; x++)
            for (int y = sy; y < sy + h; y++)
                if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;

        for (int x = sx; x < sx + w; x++)
        {
            if (map.InBounds(x, sy)) map.Tiles[x, sy].Type = TileType.Wall;
            if (map.InBounds(x, sy + h - 1)) map.Tiles[x, sy + h - 1].Type = TileType.Wall;
        }
        for (int y = sy; y < sy + h; y++)
        {
            if (map.InBounds(sx, y)) map.Tiles[sx, y].Type = TileType.Wall;
            if (map.InBounds(sx + w - 1, y)) map.Tiles[sx + w - 1, y].Type = TileType.Wall;
        }

        if (map.InBounds(sx + w / 2, sy + h - 1))
            map.Tiles[sx + w / 2, sy + h - 1].Type = TileType.Door;
    }

    private static (int x, int y) FindOpenSpot(GameMap map, Room room)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = room.X + Random.Shared.Next(1, Math.Max(2, room.Width - 1));
            int y = room.Y + Random.Shared.Next(1, Math.Max(2, room.Height - 1));
            if (map.InBounds(x, y) && IsWalkableType(map.Tiles[x, y].Type) && map.Tiles[x, y].Occupant == null)
                return (x, y);
        }
        return (-1, -1);
    }

    private static (int x, int y) FindOpenSpotAnywhere(GameMap map)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = Random.Shared.Next(5, map.Width - 5);
            int y = Random.Shared.Next(5, map.Height - 5);
            if (IsWalkableType(map.Tiles[x, y].Type) && map.Tiles[x, y].Occupant == null)
                return (x, y);
        }
        return (-1, -1);
    }
}
