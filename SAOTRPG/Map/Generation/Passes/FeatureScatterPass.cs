using SAOTRPG.Map.Generation.Sampling;
using SAOTRPG.Systems;

namespace SAOTRPG.Map.Generation.Passes;

// Everything post-town: clearings, paths, boss room, foliage, landmarks, lava,
// quota-driven Poisson scatter, danger clusters, mid-boss, puzzles, connectivity rescue.
public sealed class FeatureScatterPass : IGenerationPass
{
    public string Name => "FeatureScatter";
    public bool ShouldRun(WorldContext ctx) => true;
    public void Execute(WorldContext ctx)
    {
        var map = ctx.Map;
        var rng = ctx.Rng;
        var rooms = ctx.Rooms;
        int width = ctx.Width, height = ctx.Height;
        int spawnX = ctx.SpawnX, spawnY = ctx.SpawnY;

        // Quadrant clearings.
        var clearings = ctx.Clearings;
        clearings.Add((spawnX, spawnY));
        var quadrants = new (int xMin, int xMax, int yMin, int yMax)[]
        {
            (15,            width / 2 - 6, 15,             height / 2 - 6),
            (width / 2 + 6, width - 15,    15,             height / 2 - 6),
            (15,            width / 2 - 6, height / 2 + 6, height - 15),
            (width / 2 + 6, width - 15,    height / 2 + 6, height - 15),
        };
        foreach (var q in quadrants)
        {
            if (q.xMax <= q.xMin || q.yMax <= q.yMin) continue;
            int countInQuad = FloorScale.ClearingsPerQuad(ctx.FloorNumber);
            for (int i = 0; i < countInQuad; i++)
            {
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    int cx = rng.Next(q.xMin, q.xMax);
                    int cy = rng.Next(q.yMin, q.yMax);
                    if (!ctx.IsInsideCircle(cx, cy)) continue;
                    if (MapGenerator.PlaceClearing(map, rooms, clearings, cx, cy, rng)) break;
                }
            }
        }

        for (int i = 0; i < clearings.Count - 1; i++)
            MapGenerator.CarvePath(map, clearings[i].x, clearings[i].y, clearings[i + 1].x, clearings[i + 1].y, rng);
        if (clearings.Count > 2)
            MapGenerator.CarvePath(map, clearings[^1].x, clearings[^1].y, clearings[0].x, clearings[0].y, rng);

        // Boss room: rotates through 4 quadrants, halfway between center and disk edge.
        // Bbox-corner fallback (towns/F100 with null mask) preserves legacy behavior.
        int bossW = Math.Min(13, width / 4);
        int bossH = Math.Min(11, height / 4);
        int bossQuadrant = (ctx.FloorNumber - 1) % 4;
        var (bossX, bossY) = PickBossRoomOrigin(ctx, spawnX, spawnY, bossW, bossH, bossQuadrant);
        MapGenerator.BuildStructure(map, bossX, bossY, bossW, bossH);
        if (bossQuadrant <= 1)
        {
            map.Tiles[bossX + bossW / 2, bossY].Type = TileType.Door;
            map.Tiles[bossX + bossW / 2, bossY + bossH - 1].Type = TileType.Wall;
        }
        var bossRoom = new Room(bossX, bossY, bossW, bossH);
        rooms.Add(bossRoom);
        ctx.BossRoom = bossRoom;
        map.Tiles[bossX + bossW / 2, bossY + bossH / 2].Type = TileType.LabyrinthEntrance;
        MapGenerator.CarveStraightPath(map, spawnX, spawnY, bossX + bossW / 2, bossY + bossH / 2);

        // Scatter foliage EARLY so later grass-targeted features don't overwrite it.
        MapGenerator.ScatterGrassFoliage(map, spawnX, spawnY,
            FloorScale.ScatteredTrees(ctx.FloorNumber),
            FloorScale.ScatteredBushes(ctx.FloorNumber), rng);

        // Campfires near clearings.
        for (int i = 1; i < clearings.Count; i++)
        {
            if (rng.Next(3) == 0) continue;
            int fx = clearings[i].x + rng.Next(-2, 3);
            int fy = clearings[i].y + rng.Next(-2, 3);
            if (map.InBounds(fx, fy) && MapGenerator.IsGrassType(map.Tiles[fx, fy].Type))
                map.Tiles[fx, fy].Type = TileType.Campfire;
        }

        // Lone Pillar landmark, far from town.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int lx = rng.Next(15, width - 15), ly = rng.Next(15, height - 15);
            if (!ctx.IsInsideCircle(lx, ly)) continue;
            if (MapGenerator.IsGrassType(map.Tiles[lx, ly].Type)
                && Math.Abs(lx - spawnX) > 20 && Math.Abs(ly - spawnY) > 20)
            { map.Tiles[lx, ly].Type = TileType.Pillar; break; }
        }

        int lavaPools = FloorScale.LavaPools(ctx.FloorNumber, rng);
        for (int i = 0; i < lavaPools; i++)
        {
            int lx = rng.Next(12, width - 12), ly = rng.Next(12, height - 12);
            if (!ctx.IsInsideCircle(lx, ly)) continue;
            if (Math.Abs(lx - spawnX) < 10 && Math.Abs(ly - spawnY) < 10) continue;
            MapGenerator.PlaceCluster(map, lx, ly, rng.Next(1, 3), TileType.Lava, 0.5, rng);
        }

        // Quota-driven Poisson scatter (anvils/shrines/chests/traps/vents/lore/journals/campfires/pillars).
        // Guarantees per-biome minimums first, then fills toward maxima until the point pool runs out.
        ScatterQuotaFeatures(ctx, map, spawnX, spawnY, width, height, rng);

        int dangerClusters = FloorScale.DangerClusters(ctx.FloorNumber, rng);
        for (int i = 0; i < dangerClusters; i++)
        {
            int dx = rng.Next(15, width - 15), dy = rng.Next(15, height - 15);
            if (!ctx.IsInsideCircle(dx, dy)) continue;
            if (Math.Abs(dx - spawnX) < 10 && Math.Abs(dy - spawnY) < 10) continue;
            MapGenerator.PlaceCluster(map, dx, dy, 2, TileType.DangerZone, 0.5, rng);
        }

        // Mid-boss structure near the median clearing.
        if (clearings.Count > 3)
        {
            var (mbx, mby) = clearings[clearings.Count / 2];
            int mbRoomX = mbx - 4, mbRoomY = mby - 3;
            MapGenerator.BuildStructure(map, mbRoomX, mbRoomY, 9, 7);
            rooms.Add(new Room(mbRoomX, mbRoomY, 9, 7));
        }

        // Secret rooms behind cracked walls.
        int secretCount = 1 + rng.Next(0, 2);
        for (int si = 0; si < secretCount && clearings.Count > 2; si++)
        {
            int ci = rng.Next(1, clearings.Count);
            var (scx, scy) = clearings[ci];
            int side = rng.Next(4);
            int srX = scx + (side == 0 ? 6 : side == 1 ? -9 : -2);
            int srY = scy + (side == 2 ? 6 : side == 3 ? -7 : -2);
            if (srX < 3 || srX + 5 >= width - 3 || srY < 3 || srY + 5 >= height - 3) continue;
            // Reject any secret room whose 5x5 footprint clips out of the disk.
            if (!ctx.IsInsideCircle(srX, srY) || !ctx.IsInsideCircle(srX + 4, srY + 4)
                || !ctx.IsInsideCircle(srX, srY + 4) || !ctx.IsInsideCircle(srX + 4, srY)) continue;
            for (int sx = srX; sx < srX + 5; sx++)
                for (int sy = srY; sy < srY + 5; sy++)
                    map.Tiles[sx, sy].Type = (sx == srX || sx == srX + 4 || sy == srY || sy == srY + 4)
                        ? TileType.Wall : TileType.Floor;
            int crackX = side switch { 0 => srX, 1 => srX + 4, _ => srX + 2 };
            int crackY = side switch { 2 => srY, 3 => srY + 4, _ => srY + 2 };
            map.Tiles[crackX, crackY].Type = TileType.CrackedWall;
            map.Tiles[srX + 2, srY + 2].Type = TileType.Chest;
        }

        // Hazards + vents are now handled by ScatterQuotaFeatures above.

        // Lever/pressure plate puzzles linked to sealed doors.
        int puzzleCount = 1 + rng.Next(0, 2);
        for (int pi = 0; pi < puzzleCount; pi++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int dx = rng.Next(15, width - 15), dy = rng.Next(15, height - 15);
                if (!ctx.IsInsideCircle(dx, dy)) continue;
                if (Math.Abs(dx - spawnX) < 12 && Math.Abs(dy - spawnY) < 12) continue;
                if (map.Tiles[dx, dy].Type != TileType.Wall) continue;
                bool hasFloor = (map.InBounds(dx - 1, dy) && map.Tiles[dx - 1, dy].Type == TileType.Floor)
                             || (map.InBounds(dx + 1, dy) && map.Tiles[dx + 1, dy].Type == TileType.Floor)
                             || (map.InBounds(dx, dy - 1) && map.Tiles[dx, dy - 1].Type == TileType.Floor)
                             || (map.InBounds(dx, dy + 1) && map.Tiles[dx, dy + 1].Type == TileType.Floor);
                if (!hasFloor) continue;
                bool useLever = rng.Next(2) == 0;
                for (int la = 0; la < 20; la++)
                {
                    int lx = dx + rng.Next(-8, 9), ly = dy + rng.Next(-8, 9);
                    if (!map.InBounds(lx, ly)) continue;
                    if (!ctx.IsInsideCircle(lx, ly)) continue;
                    var lt = map.Tiles[lx, ly].Type;
                    if (lt != TileType.Floor && !MapGenerator.IsGrassType(lt)) continue;
                    map.Tiles[lx, ly].Type = useLever ? TileType.Lever : TileType.PressurePlate;
                    map.Tiles[lx, ly].LinkedDoor = (dx, dy);
                    break;
                }
                break;
            }
        }

        MapGenerator.DecorateCorridors(map, spawnX, spawnY, rng);
        MapGenerator.PlaceWaterFeatures(map, spawnX, spawnY, rng);
        MapGenerator.DecorateStairRooms(map, spawnX, spawnY, bossX, bossY, bossW, bossH);

        MapGenerator.EnsureConnectivity(map, rooms, clearings);

        UI.DebugLogger.LogGame("MAPGEN", $"  {rooms.Count} rooms, {clearings.Count} clearings");
        map.RecountWalkableTiles();
    }

    // Quota-driven Poisson scatter: shared point pool per floor, popped per feature category.
    // Rejection-sampling fallback fills guaranteed minimums when the Poisson pool runs dry.
    private static void ScatterQuotaFeatures(WorldContext ctx, GameMap map,
        int spawnX, int spawnY, int width, int height, Random rng)
    {
        FeatureQuotas q = ctx.Config.FeatureQuotas ?? new FeatureQuotas();
        int floor = ctx.FloorNumber;

        // Floor-depth scaling: deeper floors see slightly more traps + vents.
        int trapMin = Math.Max(q.MinTraps, floor / 3);
        int trapMax = Math.Max(q.MaxTraps, floor / 2);
        int ventMin = q.MinGasVents;
        int ventMax = Math.Max(q.MaxGasVents, floor / 4);

        // Poisson acceptance: walkable (grass/floor/path), inside disk, off-edge, off-spawn.
        bool AcceptTile(int x, int y)
        {
            if (x < 8 || y < 8 || x >= width - 8 || y >= height - 8) return false;
            if (!ctx.IsInsideCircle(x, y)) return false;
            if (Math.Abs(x - spawnX) < 10 && Math.Abs(y - spawnY) < 10) return false;
            var t = map.Tiles[x, y].Type;
            return MapGenerator.IsGrassType(t) || t == TileType.Floor || t == TileType.Path;
        }

        var points = PoissonDiskSampler.Sample(width, height, q.PoissonRadius, rng, 30, AcceptTile);
        int idx = 0;

        int AssignCount(int min, int max)
        {
            if (max < min) max = min;
            return min + rng.Next(max - min + 1);
        }

        // Placements recorded for debug logging.
        int placedAnvils = 0, placedShrines = 0, placedChests = 0, placedTraps = 0;
        int placedVents = 0, placedLore = 0, placedJournals = 0, placedCampfires = 0, placedPillars = 0;

        // Pop the next Poisson point, re-checking acceptance because earlier
        // placements may have mutated tiles. Returns false when pool is drained.
        bool NextPoint(out int px, out int py)
        {
            while (idx < points.Count)
            {
                var (x, y) = points[idx++];
                if (AcceptTile(x, y)) { px = x; py = y; return true; }
            }
            px = py = -1;
            return false;
        }

        // Rejection-sampling fallback for guaranteed minimums when Poisson dries up.
        bool FallbackPoint(out int px, out int py)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int x = rng.Next(8, width - 8);
                int y = rng.Next(8, height - 8);
                if (AcceptTile(x, y)) { px = x; py = y; return true; }
            }
            px = py = -1;
            return false;
        }

        // Place `target` features; satisfy `minGuaranteed` via fallback if needed.
        int Place(TileType type, int target, int minGuaranteed)
        {
            int placed = 0;
            for (int i = 0; i < target; i++)
            {
                if (NextPoint(out int px, out int py))
                    map.Tiles[px, py].Type = type;
                else if (placed < minGuaranteed && FallbackPoint(out px, out py))
                    map.Tiles[px, py].Type = type;
                else
                    break;
                placed++;
            }
            // Still short of the minimum? Keep rejecting until satisfied or give up.
            while (placed < minGuaranteed && FallbackPoint(out int px, out int py))
            {
                map.Tiles[px, py].Type = type;
                placed++;
            }
            return placed;
        }

        // Traps placed per-point via biome pool; inlined because each point
        // samples a distinct trap type rather than a uniform TileType.
        int PlaceTraps(int target, int minGuaranteed)
        {
            int placed = 0;
            for (int i = 0; i < target; i++)
            {
                if (NextPoint(out int px, out int py))
                    map.Tiles[px, py].Type = PickBiomeTrap(ctx.Biome, rng);
                else if (placed < minGuaranteed && FallbackPoint(out px, out py))
                    map.Tiles[px, py].Type = PickBiomeTrap(ctx.Biome, rng);
                else
                    break;
                placed++;
            }
            while (placed < minGuaranteed && FallbackPoint(out int px, out int py))
            {
                map.Tiles[px, py].Type = PickBiomeTrap(ctx.Biome, rng);
                placed++;
            }
            return placed;
        }

        // Order: guaranteed minimums get first pick from the Poisson pool.
        // Shrines first (they want floor-only space) then Anvils, then bulk.
        placedShrines   = Place(TileType.EnchantShrine, AssignCount(q.MinShrines, q.MaxShrines),     q.MinShrines);
        placedAnvils    = Place(TileType.Anvil,         AssignCount(q.MinAnvils, q.MaxAnvils),       q.MinAnvils);
        placedChests    = Place(TileType.Chest,         AssignCount(q.MinChests, q.MaxChests),       q.MinChests);
        placedTraps     = PlaceTraps(AssignCount(trapMin, trapMax), trapMin);
        placedVents     = Place(TileType.GasVent,       AssignCount(ventMin, ventMax),               ventMin);
        placedLore      = Place(TileType.LoreStone,     AssignCount(q.MinLoreStones, q.MaxLoreStones), q.MinLoreStones);
        placedJournals  = Place(TileType.Journal,       AssignCount(q.MinJournals, q.MaxJournals),   q.MinJournals);
        placedCampfires = Place(TileType.Campfire,      AssignCount(q.MinCampfires, q.MaxCampfires), q.MinCampfires);
        placedPillars   = Place(TileType.Pillar,        AssignCount(q.MinPillars, q.MaxPillars),     q.MinPillars);

        UI.DebugLogger.LogGame("MAPGEN",
            $"FeatureQuota floor={floor} biome={ctx.Biome} pool={points.Count} " +
            $"anvils={placedAnvils} shrines={placedShrines} chests={placedChests} " +
            $"traps={placedTraps} vents={placedVents} lore={placedLore} " +
            $"journals={placedJournals} campfires={placedCampfires} pillars={placedPillars}");
    }

    // Picks the boss room top-left so the room sits halfway between map center and the
    // disk edge along the quadrant diagonal. Falls back to bbox-corner placement when
    // no mask (towns/F100) so legacy behavior is preserved.
    private static (int X, int Y) PickBossRoomOrigin(WorldContext ctx, int spawnX, int spawnY,
        int bossW, int bossH, int quadrant)
    {
        int width = ctx.Width, height = ctx.Height;
        int edgePad = Math.Max(6, bossW + 2);

        if (ctx.CircleMask is null)
        {
            int bx = quadrant switch
            {
                0 => Math.Max(edgePad, width - edgePad - bossW),
                1 => edgePad,
                2 => edgePad,
                _ => Math.Max(edgePad, width - edgePad - bossW),
            };
            int by = quadrant switch
            {
                0 => Math.Max(edgePad, height - edgePad - bossH),
                1 => Math.Max(edgePad, height - edgePad - bossH),
                2 => edgePad,
                _ => edgePad,
            };
            return (bx, by);
        }

        // Diagonal direction per quadrant; walk inward from the disk edge until we
        // find a position whose 4 corners all fit inside the disk.
        int sxDir = quadrant == 1 || quadrant == 2 ? -1 : 1;
        int syDir = quadrant == 2 || quadrant == 3 ? -1 : 1;
        int cx = spawnX, cy = spawnY;
        int targetX = sxDir > 0 ? width - edgePad : edgePad;
        int targetY = syDir > 0 ? height - edgePad : edgePad;
        int midX = (cx + targetX) / 2;
        int midY = (cy + targetY) / 2;

        for (int shrink = 0; shrink < 12; shrink++)
        {
            int tlX = Math.Clamp(midX - bossW / 2 - shrink * sxDir, edgePad, width - edgePad - bossW);
            int tlY = Math.Clamp(midY - bossH / 2 - shrink * syDir, edgePad, height - edgePad - bossH);
            if (ctx.IsInsideCircle(tlX, tlY)
                && ctx.IsInsideCircle(tlX + bossW - 1, tlY)
                && ctx.IsInsideCircle(tlX, tlY + bossH - 1)
                && ctx.IsInsideCircle(tlX + bossW - 1, tlY + bossH - 1))
                return (tlX, tlY);
        }
        // Hard fallback: clamp toward map center.
        return (Math.Clamp(cx + sxDir * Math.Max(8, bossW), edgePad, width - edgePad - bossW),
                Math.Clamp(cy + syDir * Math.Max(6, bossH), edgePad, height - edgePad - bossH));
    }

    // Per-biome weighted trap pool. Biases toward signature hazards.
    private static TileType PickBiomeTrap(BiomeType biome, Random rng)
    {
        TileType[] pool = biome switch
        {
            BiomeType.Ice       => new[] { TileType.TrapSpike, TileType.TrapAlarm },
            BiomeType.Swamp     => new[] { TileType.TrapPoison, TileType.TrapAlarm },
            BiomeType.Volcanic  => new[] { TileType.TrapSpike, TileType.TrapTeleport },
            BiomeType.Desert    => new[] { TileType.TrapSpike, TileType.TrapAlarm },
            BiomeType.Dark      => new[] { TileType.TrapTeleport, TileType.TrapAlarm },
            BiomeType.Ruins     => new[] { TileType.TrapSpike, TileType.TrapAlarm, TileType.TrapPoison },
            BiomeType.Urban     => new[] { TileType.TrapAlarm, TileType.TrapSpike },
            BiomeType.Forest    => new[] { TileType.TrapPoison, TileType.TrapSpike },
            BiomeType.Grassland => new[] { TileType.TrapSpike, TileType.TrapAlarm },
            BiomeType.Aquatic   => new[] { TileType.TrapAlarm },
            BiomeType.Void      => new[] { TileType.TrapTeleport, TileType.TrapTeleport, TileType.TrapAlarm },
            _                   => new[] { TileType.TrapSpike, TileType.TrapAlarm },
        };
        return pool[rng.Next(pool.Length)];
    }
}
