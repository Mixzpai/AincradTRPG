using SAOTRPG.Map.Generation.Sampling;
using SAOTRPG.Systems.World;

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
        //
        // This route ignored the biome's own TreeDensity and BushDensity entirely, so a Volcanic
        // floor (declaring 0.2) received the same isolated-tree scatter as a Forest (2.2). It is
        // the smaller of the two foliage routes -- 640 of floor 1's 13,300 trees against
        // ClusterPass's copses -- but a single tree standing alone on ash is exactly the kind of
        // object that reads as noise rather than as landscape, so it is scaled twice: by the
        // biome that has an opinion about it, and by the clutter factor.
        MapGenerator.ScatterGrassFoliage(map, spawnX, spawnY,
            ScatterCount(FloorScale.ScatteredTrees(ctx.FloorNumber), ctx.Config.TreeDensity),
            ScatterCount(FloorScale.ScatteredBushes(ctx.FloorNumber), ctx.Config.BushDensity), rng);

        // The campfire-per-clearing roll that used to sit here is gone. It placed one fire per
        // clearing at 2-in-3 with no budget at all -- four to sixteen fires on a floor whose biome
        // declared a maximum of two -- and it duplicated what the quota scatter below already
        // does deliberately. The Lone Pillar landmark went with it for the same reason: one more
        // unbudgeted instance of a tile the quota already governs.

        int lavaPools = FloorScale.LavaPools(ctx.FloorNumber, rng);
        for (int i = 0; i < lavaPools; i++)
        {
            int lx = rng.Next(12, width - 12), ly = rng.Next(12, height - 12);
            if (!ctx.IsInsideCircle(lx, ly)) continue;
            if ((Math.Abs(lx - spawnX) < 10 && Math.Abs(ly - spawnY) < 10) || ctx.IsInTownKeepOut(lx, ly)) continue;
            MapGenerator.PlaceCluster(map, lx, ly, rng.Next(1, 3), TileType.Lava, 0.5, rng);
        }

        // Quota-driven Poisson scatter (anvils/shrines/chests/traps/vents/lore/journals/campfires/pillars).
        // Guarantees per-biome minimums first, then fills toward maxima until the point pool runs out.
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Scatter.QuotaFeatures"))
            ScatterQuotaFeatures(ctx, map, spawnX, spawnY, width, height, rng);

        int dangerClusters = FloorScale.DangerClusters(ctx.FloorNumber, rng);
        for (int i = 0; i < dangerClusters; i++)
        {
            int dx = rng.Next(15, width - 15), dy = rng.Next(15, height - 15);
            if (!ctx.IsInsideCircle(dx, dy)) continue;
            if ((Math.Abs(dx - spawnX) < 10 && Math.Abs(dy - spawnY) < 10) || ctx.IsInTownKeepOut(dx, dy)) continue;
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
                    map.SetLinkedDoor(lx, ly, dx, dy);
                    break;
                }
                break;
            }
        }

        // The biome's own campfire quota governs the wall sconces too. Without this the
        // decorator ignored it and out-produced every other campfire source combined.
        int fireBudget = ctx.Config.FeatureQuotas?.MaxCampfires ?? 3;
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Scatter.DecorateCorridors"))
            MapGenerator.DecorateCorridors(map, spawnX, spawnY, rng, fireBudget);
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Scatter.PlaceWaterFeatures"))
            MapGenerator.PlaceWaterFeatures(map, spawnX, spawnY, rng);
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Scatter.DecorateStairRooms"))
            MapGenerator.DecorateStairRooms(map, spawnX, spawnY, bossX, bossY, bossW, bossH);

        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Scatter.EnsureConnectivity"))
            MapGenerator.EnsureConnectivity(map, rooms, clearings);

        // LAST, because a budget enforced before the last thing that can spend it is a budget for
        // an intermediate map. Every route that can place a governed feature -- both prefab
        // passes, the town builders, the corridor sconces, the stair-room decorators and the
        // quota scatter above -- has run by this point.
        using (SAOTRPG.Systems.Diagnostics.Profiler.Begin("Scatter.ReconcileQuotas"))
            ReconcileFeatureQuotas(ctx, map, spawnX, spawnY, width, height);

        // Re-stamp the floor's only guaranteed exit: every stamp above may have painted over it.
        // ConnectivityAuditPass tunnels to it if it was walled off.
        map.Tiles[bossRoom.CenterX, bossRoom.CenterY].Type = TileType.LabyrinthEntrance;

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN", $"  {rooms.Count} rooms, {clearings.Count} clearings");
        // ConnectivityAuditPass runs immediately after this one and recounts, so a full sweep
        // here is thrown away — measured 14.6 ms per floor for nothing. What this pass genuinely
        // owes the map is the opacity invalidation, because it wrote tile types through the
        // indexer. Nothing reads the walkable count between the two passes.
        map.InvalidateOpacity();
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
            if (Math.Abs(x - spawnX) < SpawnHazardExclusion
                && Math.Abs(y - spawnY) < SpawnHazardExclusion) return false;
            var t = map.Tiles[x, y].Type;
            return MapGenerator.IsGrassType(t) || t == TileType.Floor || t == TileType.Path;
        }

        // Cap the pool. Uncapped this returns ~13,500 points on a 991x991 floor at 170 ms --
        // roughly 87% of this pass -- and the placements below consume a few hundred. The
        // sampler appends only, so a capped run gives byte-identical points for everything
        // actually used; verified by floor signature across floors and seeds.
        // Its OWN derived stream, not ctx.Rng. The sampler's draw count depends on how many points
        // it happens to generate, so drawing from the shared stream made every later placement on
        // the floor depend on that count -- and capping the pool therefore shifted the whole layout.
        // Derived per floor, the cap becomes free: the points consumed are identical either way.
        var poissonRng = new Random(
            SAOTRPG.Systems.RunState.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "poisson"));
        var points = PoissonDiskSampler.Sample(width, height, q.PoissonRadius, poissonRng, 30,
            AcceptTile, PoissonMaxSamples);
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
            // Drained. Callers fall back to rejection sampling, which places features differently,
            // so this is worth seeing rather than absorbing quietly.
            if (points.Count >= PoissonMaxSamples)
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                    $"FeatureScatter floor={ctx.FloorNumber} WARN poisson_pool_drained cap={PoissonMaxSamples}");
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

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
            $"FeatureQuota floor={floor} biome={ctx.Biome} pool={points.Count} " +
            $"anvils={placedAnvils} shrines={placedShrines} chests={placedChests} " +
            $"traps={placedTraps} vents={placedVents} lore={placedLore} " +
            $"journals={placedJournals} campfires={placedCampfires} pillars={placedPillars}");
    }

    // Scatter counts scaled by the biome's own density and by the shared clutter factor. Never
    // below 1 where the unscaled count was positive: a biome asking for a tenth of the trees still
    // gets trees, and a zero would read as the whole route having been switched off.
    private static int ScatterCount(int baseCount, float biomeDensity)
    {
        if (baseCount <= 0) return 0;
        return Math.Max(1, (int)Math.Round(baseCount * biomeDensity * ScatterFoliageScale));
    }

    // ── FLOOR-WIDE FEATURE BUDGET ───────────────────────────────────────────────────────────
    //
    // A per-biome quota is only a budget if it is spent against everything that can place the
    // thing it budgets. `ScatterQuotaFeatures` above governs ONE placement route; a stamped
    // prefab, a town builder, a corridor sconce and a stair-room decorator are four more, and
    // none of them could see the quota. Measured across 20 floors before this ran: campfires
    // 10-38 against declared maxima of 0-3, pillars 12-48 against 1-3, anvils 1-6 against 0-2.
    // Every floor sampled breached on campfires and on pillars.
    //
    // Two boundaries, both deliberate and both LOUD when they bind:
    //
    //   * A SAFE-ZONE town is furniture, not budget. F1's Town of Beginnings and F48's Lindarth
    //     are hand-authored, their hearths and anvils are part of the building, and the player
    //     cannot be attacked standing on them. Cells inside `map.SafeZone` are neither counted
    //     nor removed.
    //   * OPEN-GROUND instances go first — nothing with a wall or a door in its eight-neighbour
    //     ring. That is what separates scatter from architecture: a sconce is against a wall and
    //     a colonnade is inside a prefab. What happens when the open ground runs out depends on
    //     WHAT THE FEATURE IS, not on where it sits. A campfire, anvil, shrine, lore stone or
    //     journal is a single-tile resource: lifting one out of a room leaves the room intact, so
    //     the budget may reach in. A PILLAR is architecture — a colonnade with two columns missing
    //     reads as damage, and the boss arena and spawn chamber each frame themselves with four —
    //     so pillar removal stops at the open ground and the remainder is logged.
    //
    // The distinction matters most in a cave biome, where almost every cell has rock in its ring:
    // Dark carried 11 campfires against a maximum of 4 with open-ground removal alone.
    private static void ReconcileFeatureQuotas(WorldContext ctx, GameMap map,
        int spawnX, int spawnY, int width, int height)
    {
        FeatureQuotas q = ctx.Config.FeatureQuotas ?? new FeatureQuotas();
        var budgets = new (TileType Type, int Min, int Max, bool TrimsArchitecture)[]
        {
            (TileType.Anvil,         q.MinAnvils,     q.MaxAnvils,     true),
            (TileType.EnchantShrine, q.MinShrines,    q.MaxShrines,    true),
            (TileType.LoreStone,     q.MinLoreStones, q.MaxLoreStones, true),
            (TileType.Journal,       q.MinJournals,   q.MaxJournals,   true),
            (TileType.Campfire,      q.MinCampfires,  q.MaxCampfires,  true),
            (TileType.Pillar,        q.MinPillars,    q.MaxPillars,    false),
        };

        // Its own derived stream, for the same reason the Poisson sampler has one: how many draws
        // a top-up makes depends on how crowded the floor turned out, so drawing from ctx.Rng
        // would make the rest of the floor depend on the reconciliation's own workload.
        var rng = new Random(SAOTRPG.Systems.RunState.RunRng.StableHash(
            SAOTRPG.Systems.RunState.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber), "quota-reconcile", 0));

        Room? safe = map.SafeZone;
        var report = new System.Text.StringBuilder();

        foreach (var (type, min, max, trimsArchitecture) in budgets)
        {
            var cells = new List<(int X, int Y)>();
            var open = new List<bool>();
            int inTown = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (map.Tiles[x, y].Type != type) continue;
                if (safe.HasValue && safe.Value.Contains(x, y)) { inTown++; continue; }
                cells.Add((x, y));
                open.Add(IsOpenGround(map, x, y));
            }

            int before = cells.Count;
            var alive = new bool[before];
            Array.Fill(alive, true);
            int surviving = before;

            // Thin the densest first: drop whichever removable instance sits closest to another
            // surviving one, so what is left keeps its spread across the floor. Ties go to the
            // lowest row-major index, which makes the choice a pure function of the map.
            //
            // Open ground is exhausted before architecture is touched at all, so a floor with
            // enough scatter to pay the excess never loses an authored instance.
            bool reachIntoArchitecture = false;
            while (surviving > max)
            {
                int victim = -1;
                long bestDist = long.MaxValue;
                for (int i = 0; i < before; i++)
                {
                    if (!alive[i] || (!open[i] && !reachIntoArchitecture)) continue;
                    long near = long.MaxValue;
                    for (int j = 0; j < before; j++)
                    {
                        if (j == i || !alive[j]) continue;
                        long dx = cells[i].X - cells[j].X, dy = cells[i].Y - cells[j].Y;
                        long d = dx * dx + dy * dy;
                        if (d < near) near = d;
                    }
                    if (victim < 0 || near < bestDist) { bestDist = near; victim = i; }
                }
                if (victim < 0)
                {
                    if (!trimsArchitecture || reachIntoArchitecture) break;
                    reachIntoArchitecture = true;
                    continue;
                }
                alive[victim] = false;
                surviving--;
                var (vx, vy) = cells[victim];
                map.Tiles[vx, vy].Type = GroundUnder(map, vx, vy);
            }

            int afterTrim = surviving;
            // Rejection-sampled top-up. Bounded: a crowded or tiny floor may simply have nowhere
            // left, and looping forever to satisfy a minimum is worse than reporting the shortfall.
            for (int attempt = 0; surviving < min && attempt < TopUpAttempts; attempt++)
            {
                int x = rng.Next(8, Math.Max(9, width - 8));
                int y = rng.Next(8, Math.Max(9, height - 8));
                if (!ctx.IsInsideCircle(x, y)) continue;
                if (Math.Abs(x - spawnX) < SpawnHazardExclusion
                    && Math.Abs(y - spawnY) < SpawnHazardExclusion) continue;
                if (safe.HasValue && safe.Value.Contains(x, y)) continue;
                if (!IsPlainGround(map.Tiles[x, y].Type)) continue;
                if (HasNeighbourOfType(map, x, y, type)) continue;
                map.Tiles[x, y].Type = type;
                surviving++;
            }

            if (before != surviving || surviving > max || surviving < min)
                report.Append($" {type}={before}->{surviving}[{min}-{max}]");
            // Fail loud: a floor left outside its own declared budget is a fact about the content,
            // not something to absorb. Over-budget means authored architecture accounts for more
            // than the biome allows; under means the floor had nowhere to put the minimum.
            if (surviving > max)
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                    $"FeatureQuota floor={ctx.FloorNumber} biome={ctx.Biome} OVER {type} "
                    + $"{surviving}>{max} — {before - afterTrim} removed, the rest is wall-adjacent "
                    + $"or authored ({inTown} more inside the safe zone, exempt)");
            else if (surviving < min)
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                    $"FeatureQuota floor={ctx.FloorNumber} biome={ctx.Biome} UNDER {type} "
                    + $"{surviving}<{min} — no accepting ground found in {TopUpAttempts} attempts");
        }

        if (report.Length > 0)
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"FeatureReconcile floor={ctx.FloorNumber} biome={ctx.Biome}{report}");
    }

    // Open ground = nothing structural in the eight-neighbour ring. A feature here was scattered;
    // one against a wall or a door is part of a room, a prefab or a corridor.
    private static bool IsOpenGround(GameMap map, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) return false;
            var t = map.Tiles[nx, ny].Type;
            if (t is TileType.Wall or TileType.CrackedWall or TileType.Door) return false;
        }
        return true;
    }

    private static bool HasNeighbourOfType(GameMap map, int x, int y, TileType type)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            if (map.InBounds(x + dx, y + dy) && map.Tiles[x + dx, y + dy].Type == type) return true;
        }
        return false;
    }

    // What a removed feature leaves behind. Taken from the MAJORITY of its walkable neighbours
    // rather than stamped as Floor, because a fire lifted out of a meadow should leave meadow and
    // one lifted off a flagstone room should leave flagstone — and an unrecognised type stamped as
    // Floor is how a wall becomes walkable ground elsewhere in this pipeline.
    private static TileType GroundUnder(GameMap map, int x, int y)
    {
        TileType best = TileType.Floor;
        int bestCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny)) continue;
            var t = map.Tiles[nx, ny].Type;
            if (!IsPlainGround(t)) continue;
            int count = 0;
            for (int ex = -1; ex <= 1; ex++)
            for (int ey = -1; ey <= 1; ey++)
            {
                if (ex == 0 && ey == 0) continue;
                if (map.InBounds(x + ex, y + ey) && map.Tiles[x + ex, y + ey].Type == t) count++;
            }
            if (count > bestCount) { bestCount = count; best = t; }
        }
        return best;
    }

    // Ground a feature may be placed on or dissolved back into: terrain with no meaning of its
    // own. Deliberately excludes water, hazards, ore and every interactive tile.
    private static bool IsPlainGround(TileType t) =>
        t is TileType.Floor or TileType.Path or TileType.Grass or TileType.GrassTall
          or TileType.GrassSparse or TileType.Sand or TileType.DuneSand or TileType.Snow
          or TileType.Ice or TileType.Basalt or TileType.Ash or TileType.Mud or TileType.Reeds;

    // Bounded so a floor with nowhere left to put its minimum reports a shortfall instead of
    // spinning. 400 darts find accepting ground on every floor that has any.
    private const int TopUpAttempts = 400;

    // How hard the two unscaled foliage-scatter counts are cut, on top of the biome's own
    // density. Kept as one named factor so the clutter dial is a single number.
    private const double ScatterFoliageScale = 0.5;

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

    // How close to spawn a scattered feature may land, as a Chebyshev box. Shared with
    // ConnectivityAuditPass, whose trap sweep used a SMALLER radius than this exclusion and could
    // therefore only ever catch hazards placed by paths that bypass AcceptTile -- which is exactly
    // what it exists for, and which it was missing.
    public const int SpawnHazardExclusion = 10;

    // MEASURED, not guessed. Across 40 floors x 2 seeds the pass consumes at most ~60 points, so
    // this is 4x headroom. Bracketed by bisection: at 30 the pool drained 22 times and the floor
    // layouts changed; at 60 it drained 0 times and every signature matched the uncapped run. The
    // drain warning validated itself in the process -- output changed EXACTLY when it fired.
    // If a config ever exceeds this the pool drains and placement falls back to rejection sampling,
    // which CHANGES the layout, so a drain is logged rather than left silent.
    private const int PoissonMaxSamples = 256;

    // Trap pools, shared by every biome that draws the same hazards in the same ORDER. Order is
    // load-bearing: the draw is rng.Next(pool.Length), so {Spike, Alarm} and {Alarm, Spike} consume
    // the same value and place a different trap. Ice/Desert/Grassland and the fallback share one
    // array only because all four are literally {Spike, Alarm}; splitting a pool means giving it
    // its own array, never reordering a shared one.
    private static readonly TileType[] TrapsSpikeAlarm     = { TileType.TrapSpike, TileType.TrapAlarm };
    private static readonly TileType[] TrapsSpikeTeleport  = { TileType.TrapSpike, TileType.TrapTeleport };
    private static readonly TileType[] TrapsAlarmOnly      = { TileType.TrapAlarm };

    // Pools for the biomes that gain one of the later hazards. Each APPENDS to the sequence that
    // biome already drew, so the earlier entries keep their index and only the added tail is new.
    //
    // AND WHEN A BIOME MOVES TO AN APPENDED POOL, ITS OLD ARRAY MUST GO WITH IT. Six superseded
    // pools were left behind here and sat unreferenced — inert, but a trap: giving Swamp another
    // hazard by editing the shorter TrapsPoisonAlarm would have changed nothing at all, silently,
    // because Swamp draws from TrapsPoisonAlarmWeb. A repeated entry is the weight — Void draws
    // Teleport twice as often as Alarm.
    private static readonly TileType[] TrapsPoisonSpikeWeb =
        { TileType.TrapPoison, TileType.TrapSpike, TileType.TrapWeb };
    private static readonly TileType[] TrapsPoisonAlarmWeb =
        { TileType.TrapPoison, TileType.TrapAlarm, TileType.TrapWeb };
    private static readonly TileType[] TrapsAlarmSpikeMagnet =
        { TileType.TrapAlarm, TileType.TrapSpike, TileType.TrapMagnet };
    private static readonly TileType[] TrapsSpikeAlarmPoisonRune =
        { TileType.TrapSpike, TileType.TrapAlarm, TileType.TrapPoison, TileType.TrapRune };
    private static readonly TileType[] TrapsTeleportHeavyRune =
        { TileType.TrapTeleport, TileType.TrapTeleport, TileType.TrapAlarm, TileType.TrapRune };
    private static readonly TileType[] TrapsTeleportAlarmWebRune =
        { TileType.TrapTeleport, TileType.TrapAlarm, TileType.TrapWeb, TileType.TrapRune };

    // Per-biome weighted trap pool. Biases toward signature hazards.
    private static TileType PickBiomeTrap(BiomeType biome, Random rng)
    {
        TileType[] pool = biome switch
        {
            BiomeType.Ice       => TrapsSpikeAlarm,
            BiomeType.Swamp     => TrapsPoisonAlarmWeb,
            BiomeType.Volcanic  => TrapsSpikeTeleport,
            BiomeType.Desert    => TrapsSpikeAlarm,
            BiomeType.Dark      => TrapsTeleportAlarmWebRune,
            BiomeType.Ruins     => TrapsSpikeAlarmPoisonRune,
            BiomeType.Urban     => TrapsAlarmSpikeMagnet,
            BiomeType.Forest    => TrapsPoisonSpikeWeb,
            BiomeType.Grassland => TrapsSpikeAlarm,
            BiomeType.Aquatic   => TrapsAlarmOnly,
            BiomeType.Void      => TrapsTeleportHeavyRune,
            _                   => TrapsSpikeAlarm,
        };
        return pool[rng.Next(pool.Length)];
    }
}
