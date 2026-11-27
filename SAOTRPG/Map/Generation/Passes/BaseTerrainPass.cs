using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation.Passes;

// Biome-native base terrain. Per-tile dispatch; pocket-biome tiles pull their own
// BiomeGenConfig so pocket thresholds/tile palette match the pocket biome.
public sealed class BaseTerrainPass : IGenerationPass
{
    public string Name => "BaseTerrain";
    public bool ShouldRun(WorldContext ctx) => true;

    // Everything the per-tile stamp would otherwise recompute out of the biome config on every
    // cell: the weighted pick's running totals and the two height thresholds. They are pure
    // functions of the config, and the loop runs once per cell of the floor.
    private readonly struct Stamp
    {
        public readonly BiomeType Biome;
        public readonly int TotalWeight;        // Math.Max(1, sum of the four grass weights)
        public readonly int CumGrass;           // BaseGrassWeight
        public readonly int CumTall;            // + GrassTallWeight
        public readonly int CumSparse;          // + SparseWeight
        public readonly float TLow, TMid;

        public Stamp(BiomeGenConfig cfg, BiomeType biome, int flowersWeight)
        {
            Biome = biome;
            CumGrass  = cfg.BaseGrassWeight;
            CumTall   = CumGrass + cfg.GrassTallWeight;
            CumSparse = CumTall + cfg.SparseWeight;
            TotalWeight = Math.Max(1, CumSparse + flowersWeight);
            var th = cfg.HeightmapThresholds;
            // Per-biome fallback thresholds for a config that omits them.
            TLow = th.Length > 1 ? th[1] : DefaultLow(biome);
            TMid = th.Length > 2 ? th[2] : DefaultMid(biome);
        }

        private static float DefaultLow(BiomeType b) => b == BiomeType.Swamp ? 0.30f : 0.35f;

        private static float DefaultMid(BiomeType b) => b switch
        {
            BiomeType.Ice      => 0.50f,
            BiomeType.Volcanic => 0.60f,
            _                  => 0.70f,
        };
    }

    public void Execute(WorldContext ctx)
    {
        int w = ctx.Width, h = ctx.Height;
        var tiles = ctx.Map.Tiles;
        var mask = ctx.CircleMask;
        var pockets = ctx.PocketBiomeMap;
        var rng = ctx.Rng;
        var heights = ctx.Heights;
        var hostBiome = ctx.Biome;
        var hostCfg = ctx.Config;
        var hostStamp = new Stamp(hostCfg, hostBiome,
            ctx.FloorNumber == FloriaFloor ? FloriaFlowersWeight : 0);

        // Pocket cells pull a different biome's config. Cached per biome so the loader lookup and
        // the threshold arithmetic happen once each rather than once per pocket cell.
        Stamp[]? pocketStamps = pockets is null ? null : new Stamp[BiomeCount];
        bool[]? pocketBuilt = pockets is null ? null : new bool[BiomeCount];

        // Visit order is load-bearing: the stamps below draw from ctx.Rng per cell, so x-outer
        // y-inner is part of the floor's identity and must not be swapped for cache locality.
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            // Out-of-disk → Mountain (blocks sight/movement/path natively).
            if (mask != null && !mask[x, y]) { tiles[x, y].Type = TileType.Mountain; continue; }

            float hv = heights != null ? heights[x, y] : 0.5f;

            if (pockets is null)
            {
                tiles[x, y].Type = Pick(in hostStamp, rng, heights != null, hv);
                continue;
            }

            BiomeType effective = pockets[x, y] ?? hostBiome;
            if (effective == hostBiome)
            {
                tiles[x, y].Type = Pick(in hostStamp, rng, heights != null, hv);
                continue;
            }
            int bi = (int)effective;
            if (!pocketBuilt![bi])
            {
                // A pocket never grows flowers: the garden switch is a property of the floor, not
                // of the pocket's biome.
                pocketStamps![bi] = new Stamp(BiomeGenConfigLoader.Get(effective), effective, 0);
                pocketBuilt[bi] = true;
            }
            tiles[x, y].Type = Pick(in pocketStamps![bi], rng, heights != null, hv);
        }

        // The carve runs last and only for a biome that asks for it. It is a host-biome decision:
        // a pocket cell keeps its own palette above, but whether the floor is a cavern is a
        // property of the floor.
        //
        // Floria is exempt because the flower-garden floor sits in the Dark band, and burying an
        // open garden under rock contradicts what the floor is for. The exemption is
        // unconditional and must not depend on FloriaFlowersWeight: Floria is an open garden floor
        // whatever its ground is textured with.
        if (hostCfg.CaveMode && ctx.FloorNumber != FloriaFloor) CarveCaves(ctx, hostCfg);
    }

    // The flower-garden floor, which sits in the Dark band. Named because two separate rules in
    // Execute turn on it.
    public const int FloriaFloor = 47;

    // Flowers weight for the Floria floor; 0 turns its flower ground off.
    //
    // This is the only weight that can select the Flowers branch in PickDefault, so while it is 0
    // terrain generation places no Flowers on any floor; a positive value (30, say) makes F47, and
    // no other floor, a flower garden. The Flowers tile itself stays live: prefabs carrying a
    // 'KFEAT: f = Flowers' line give its glyph, minimap colour and flavour text a source.
    public const int FloriaFlowersWeight = 0;

    private static readonly int BiomeCount = Enum.GetValues<BiomeType>().Length;

    // Per-tile dispatcher. Aquatic falls through to default weighted grass (AquaticDepthPostPass
    // handles shore/depth); signature biomes stamp their own bedrock. The rng draw sequence, its
    // order and its short-circuits are part of the floor's identity.
    private static TileType Pick(in Stamp s, Random rng, bool hasHeights, float hv)
        => s.Biome switch
        {
            BiomeType.Desert   => PickDesert(in s, rng, hv),
            BiomeType.Ice      => hv > s.TMid ? TileType.Snow : TileType.Ice,
            BiomeType.Volcanic => hv > s.TMid ? TileType.Basalt : TileType.Ash,
            BiomeType.Swamp    => PickSwamp(in s, rng, hv),
            _                  => PickDefault(in s, rng, hasHeights, hv),
        };

    // Heightmap-driven weighted grass variant — default for non-signature
    // biomes (Grassland/Forest/Urban/Dark/Void/Ruins/Aquatic).
    private static TileType PickDefault(in Stamp s, Random rng, bool hasHeights, float hv)
    {
        int r = rng.Next(s.TotalWeight);
        TileType pick;
        if (r < s.CumGrass) pick = TileType.Grass;
        else if (r < s.CumTall) pick = TileType.GrassTall;
        else if (r < s.CumSparse) pick = TileType.GrassSparse;
        // Reachable only when the Stamp was built with a non-zero flowers weight, which only
        // BaseTerrainPass.FloriaFlowersWeight can supply.
        else pick = TileType.Flowers;

        if (hasHeights)
        {
            if (hv < s.TLow)
            {
                if (pick == TileType.GrassTall && rng.Next(2) == 0) pick = TileType.GrassSparse;
            }
            else if (hv > s.TMid)
            {
                if (pick == TileType.GrassSparse && rng.Next(2) == 0) pick = TileType.GrassTall;
            }
        }
        return pick;
    }

    // Sand/DuneSand split by heightmap ridge threshold; rare grass tuft for visual variety.
    private static TileType PickDesert(in Stamp s, Random rng, float hv)
    {
        TileType pick = hv > s.TMid ? TileType.DuneSand : TileType.Sand;
        if (rng.Next(200) == 0) pick = TileType.GrassSparse;
        return pick;
    }

    // Low band is Reeds, mid band is Mud (marshy default), peaks allow occasional
    // GrassTall tufts. LakePass tail converts any lake water to BogWater.
    private static TileType PickSwamp(in Stamp s, Random rng, float hv)
    {
        if (hv < s.TLow) return TileType.Reeds;
        if (hv > s.TMid) return rng.Next(3) == 0 ? TileType.GrassTall : TileType.Mud;
        return rng.Next(10) == 0 ? TileType.Grass : TileType.Mud;
    }

    // ── Cellular-automata caves ─────────────────────────────────────────────────────────────
    //
    // Buries most of an open disk, leaving one connected system of chambers and tunnels, so a
    // catacomb floor reads as a catacomb rather than as an open field.
    //
    // Three properties are load-bearing:
    //
    //   * It runs after the terrain stamp rather than instead of it. Every in-disk cell still gets
    //     its biome tile and still consumes the same draws, so the carve decides where the walls
    //     are and moves nothing else.
    //   * It draws from a stream of its own, derived from (seed, floor). Its draw count is a
    //     function of the floor's area, so taking it from ctx.Rng would shift every later pass by
    //     how big the disk happens to be.
    //   * It hands the rest of the pipeline one region. A pocket left sealed here is a pocket the
    //     later passes will scatter chests into and the audit will then have to tunnel to.

    // Wider than SpecialAreaPass's radius-12 spawn clear and BuildTown's ±11 campfires, so the
    // town overlay is never stamped half into rock.
    //
    // It is not what keeps the player from starting sealed in: SpecialAreaPass re-clears the
    // square anyway and the audit then drills its way out. What it buys is that the plaza is part
    // of the cavern rather than a walled box with a drilled doorway.
    private const int SpawnPlazaRadius = 14;

    // A cavern this much smaller than the disk means the smoothing sealed the floor rather than
    // carving it. Logged, not corrected: the numbers that produce it are in the biome's JSON, and
    // silently widening them would hide that the author asked for something the algorithm cannot
    // give.
    private const double MinOpenShare = 0.15;

    // How big an open pocket has to be before it earns a tunnel. A share of the disk, never an
    // absolute tile count: floors run 1000x1000 down to 100x100, and a constant would mean "every
    // three-tile nook" at one end of the climb and "nothing at all" at the other. The absolute
    // floor only takes over on the smallest disks, where the share is under a chamber's worth.
    private const double MinChamberShare = 0.0004;
    private const int MinChamberCells = 24;

    // How far a tunnel may reach through rock, as a share of the disk's radius.
    private const double MaxTunnelReachShare = 0.25;
    private const int MinTunnelReach = 20;

    private static void CarveCaves(WorldContext ctx, BiomeGenConfig cfg)
    {
        int w = ctx.Width, h = ctx.Height;
        var mask = ctx.CircleMask;

        int fill = Math.Clamp(cfg.CaveFillPct, BiomeGenConfig.MinCaveFillPct, BiomeGenConfig.MaxCaveFillPct);
        int passes = Math.Clamp(cfg.CaveSmoothPasses, 0, BiomeGenConfig.MaxCaveSmoothPasses);
        if (fill != cfg.CaveFillPct || passes != cfg.CaveSmoothPasses)
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"CaveCarve floor={ctx.FloorNumber} WARN clamped fill {cfg.CaveFillPct}->{fill} "
                + $"passes {cfg.CaveSmoothPasses}->{passes}");

        var rock = new byte[w * h];
        var scratch = new byte[w * h];
        var rng = new Random(SAOTRPG.Systems.RunState.RunRng.StableHash(ctx.GlobalSeed, ctx.FloorNumber, "cave"));

        // Seed. Out-of-disk counts as rock so the smoothing pulls inward from the boundary rather
        // than leaving a ring of open ground pressed against the mountain wall. The draw is taken
        // for every cell whether or not it is used, so the stream's position depends on the
        // floor's size alone.
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                bool seeded = rng.Next(100) < fill;
                rock[row + x] = (mask != null && !mask[x, y]) || seeded ? (byte)1 : (byte)0;
            }
        }

        // Smooth. Two rules:
        //
        //   shaping  rock if the 3x3 is majority rock, or if the 5x5 is almost entirely open
        //   polish   rock if the 3x3 is majority rock
        //
        // The 5x5 term plants rock in the middle of any wide empty space, which finishes the rock
        // masses with intrusions instead of smooth edges. It tightens the cave modestly; it is not
        // what makes the floor a cave. That is the seed fill, and the number to reach for is
        // CaveFillPct.
        //
        // The centre cell counts in both windows. Excluding it from the 3x3 — the "5 of 8
        // neighbours" rule that looks equivalent — is an erosion rule rather than a smoothing one
        // and collapses outright: for independent cells at p=0.45, P(>=5 of 8) is 0.26, so the
        // first pass halves the rock and every pass after halves what is left.
        const int PolishPasses = 2;
        var sat = new int[(w + 1) * (h + 1)];
        for (int it = 0; it < passes + PolishPasses; it++)
        {
            bool shaping = it < passes;
            BuildSat(rock, sat, w, h);
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (mask != null && !mask[x, y]) { scratch[row + x] = 1; continue; }
                    bool solid = RectRock(sat, w, h, x - 1, y - 1, x + 1, y + 1) >= 5
                                 || (shaping && RectRock(sat, w, h, x - 2, y - 2, x + 2, y + 2) <= 2);
                    scratch[row + x] = solid ? (byte)1 : (byte)0;
                }
            }
            (rock, scratch) = (scratch, rock);
        }

        // The plaza the town overlay is about to be stamped into, opened before the region scan
        // so it is part of whatever it touches rather than a hole punched through a finished
        // cavern.
        int sx = w / 2, sy = h / 2;
        OpenBox(rock, w, h, mask, sx - SpawnPlazaRadius, sy - SpawnPlazaRadius,
                sx + SpawnPlazaRadius, sy + SpawnPlazaRadius);
        if (ctx.TownKeepOutX >= 0)
            OpenBox(rock, w, h, mask, ctx.TownKeepOutX, ctx.TownKeepOutY,
                    ctx.TownKeepOutX + ctx.TownKeepOutW - 1, ctx.TownKeepOutY + ctx.TownKeepOutH - 1);

        long inDisk = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mask == null || mask[x, y]) inDisk++;

        // Label the open regions.
        var label = new int[w * h];
        var queue = new int[w * h];
        var sizes = new List<int> { 0 };          // indexed by label; 0 is "rock"
        int biggest = 0, biggestSize = 0, id = 0;
        for (int i = 0; i < rock.Length; i++)
        {
            if (rock[i] != 0 || label[i] != 0) continue;
            id++;
            int head = 0, tail = 0, size = 0;
            queue[tail++] = i;
            label[i] = id;
            while (head < tail)
            {
                int cur = queue[head++];
                size++;
                int cy = cur / w, cx = cur - cy * w;
                if (cx > 0) Push(cur - 1);
                if (cx < w - 1) Push(cur + 1);
                if (cy > 0) Push(cur - w);
                if (cy < h - 1) Push(cur + w);

                void Push(int ni)
                {
                    if (label[ni] != 0 || rock[ni] != 0) return;
                    label[ni] = id;
                    queue[tail++] = ni;
                }
            }
            sizes.Add(size);
            if (size > biggestSize) { biggestSize = size; biggest = id; }
        }

        // Keeping only the largest region is the obvious reading and it is a soft-lock waiting to
        // happen. Above the percolation threshold — which is where rock actually dominates and the
        // floor reads as a cave rather than as a boulder field — the open phase stops spanning the
        // disk, and the largest region's share of it collapses within a single point of fill.
        //
        // So: take the largest as the trunk and tunnel to every other chamber worth having, plus
        // the spawn plaza whatever its size. The result is one connected component by
        // construction rather than by luck.
        int spawnLabel = label[sy * w + sx];
        int minChamber = Math.Max(MinChamberCells, (int)(inDisk * MinChamberShare));
        var joined = new bool[id + 1];
        if (biggest > 0) joined[biggest] = true;

        // A route longer than this through solid rock would be a long straight corridor rather
        // than a tunnel. Scaled to the disk's own radius for the same reason minChamber is a share.
        int diskRadius = (int)Math.Sqrt(inDisk / Math.PI);
        int maxTunnel = Math.Max(MinTunnelReach, (int)(diskRadius * MaxTunnelReachShare));

        int tunnels = 0, tunnelCells = 0, buriedFar = 0;
        if (biggest > 0)
            (tunnels, tunnelCells, buriedFar) = ConnectChambers(rock, label, joined, sizes, w, h,
                mask, biggest, spawnLabel, minChamber, maxTunnel);

        long open = 0;
        var tiles = ctx.Map.Tiles;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (mask != null && !mask[x, y]) continue;   // the ring belongs to BorderPass
                int i = row + x;
                if (rock[i] == 0 && joined[label[i]]) { open++; continue; }
                tiles[x, y].Type = TileType.Wall;
            }
        }

        double share = inDisk > 0 ? open / (double)inDisk : 0;
        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
            $"CaveCarve floor={ctx.FloorNumber} biome={ctx.Biome} fill={fill} passes={passes} "
            + $"regions={id} kept={tunnels + 1} minChamber={minChamber} maxTunnel={maxTunnel} "
            + $"tunnels={tunnels} tunnelCells={tunnelCells} buriedFar={buriedFar} "
            + $"open={open}/{inDisk} ({share:P1})");
        if (share < MinOpenShare)
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"CaveCarve floor={ctx.FloorNumber} WARN cavern_too_small {share:P1} "
                + $"< {MinOpenShare:P0} — the biome's caveFillPct is sealing the floor");
        if (spawnLabel == 0 || !joined[spawnLabel])
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"CaveCarve floor={ctx.FloorNumber} WARN spawn_not_joined label={spawnLabel} "
                + "— the plaza did not reach the cavern and the floor may be unfinishable");
    }

    // One breadth-first sweep outward from the trunk, through rock and open alike, gives every
    // other chamber its shortest route home at once: BFS visits in non-decreasing distance, so the
    // first cell of a chamber it reaches is that chamber's nearest connection point, and walking
    // the predecessor chain back from it is the route. Carving one chamber's route cannot
    // invalidate another's, because every route ends at the trunk the sweep started from.
    //
    // The alternative — connect, re-sweep, repeat — is the same answer at O(regions) times the
    // cost, and there are hundreds of regions on a large floor.
    private static (int tunnels, int cells, int buriedFar) ConnectChambers(byte[] rock, int[] label,
        bool[] joined, List<int> sizes, int w, int h, bool[,]? mask, int trunk, int spawnLabel,
        int minChamber, int maxTunnel)
    {
        const int Unvisited = int.MinValue;
        const int Source = -1;
        int n = w * h;
        var prev = new int[n];
        Array.Fill(prev, Unvisited);
        var entry = new int[joined.Length];
        Array.Fill(entry, -1);
        var q = new int[n];
        int head = 0, tail = 0;

        for (int i = 0; i < n; i++)
        {
            if (label[i] != trunk) continue;
            prev[i] = Source;
            q[tail++] = i;
        }
        if (tail == 0) return (0, 0, 0);

        while (head < tail)
        {
            int cur = q[head++];
            int lab = label[cur];
            if (lab != 0 && lab != trunk && entry[lab] < 0) entry[lab] = cur;
            int cy = cur / w, cx = cur - cy * w;
            if (cx > 0) Step(cur - 1, cx - 1, cy);
            if (cx < w - 1) Step(cur + 1, cx + 1, cy);
            if (cy > 0) Step(cur - w, cx, cy - 1);
            if (cy < h - 1) Step(cur + w, cx, cy + 1);

            void Step(int ni, int nx, int ny)
            {
                if (prev[ni] != Unvisited) return;
                if (mask != null && !mask[nx, ny]) return;   // a tunnel never leaves the disk
                prev[ni] = cur;
                q[tail++] = ni;
            }
        }

        // Ascending label order, so which chambers get tunnels is a function of the seed and
        // nothing else. The spawn plaza is connected whatever its size or distance — it is where
        // the player stands, and burying the route to it is the one failure that ends a run on
        // arrival.
        //
        // A chamber too far through solid rock is buried rather than reached, because the
        // alternative is a corridor as long as the route, and at high fill enough of them turn the
        // disk into long straight passages. Burying costs nothing: the chamber was never
        // reachable.
        int tunnels = 0, cells = 0, buriedFar = 0;
        for (int r = 1; r < joined.Length; r++)
        {
            if (joined[r] || entry[r] < 0) continue;
            bool mustHave = r == spawnLabel;
            if (!mustHave && sizes[r] < minChamber) continue;

            int len = 0;
            for (int step = entry[r]; step != Source; step = prev[step]) len++;
            if (!mustHave && len > maxTunnel) { buriedFar++; continue; }

            for (int step = entry[r]; step != Source; step = prev[step])
                cells += Brush(rock, label, w, h, mask, step, trunk);
            joined[r] = true;
            tunnels++;
        }
        return (tunnels, cells, buriedFar);
    }

    // Opens the 3x3 around one route cell and claims it for the trunk. Three wide rather than one
    // because a single-tile thread is exactly the width a later rock cluster or lake severs, and
    // the sever would strand a chamber the floor is counting on.
    private static int Brush(byte[] rock, int[] label, int w, int h, bool[,]? mask, int at, int claim)
    {
        int cy = at / w, cx = at - cy * w;
        int opened = 0;
        for (int y = cy - 1; y <= cy + 1; y++)
        {
            if (y < 0 || y >= h) continue;
            int row = y * w;
            for (int x = cx - 1; x <= cx + 1; x++)
            {
                if (x < 0 || x >= w) continue;
                if (mask != null && !mask[x, y]) continue;
                int i = row + x;
                if (rock[i] != 0) opened++;
                rock[i] = 0;
                label[i] = claim;
            }
        }
        return opened;
    }

    // Summed-area table over the rock grid, so a window query costs four loads whatever its size.
    // The 5x5 rule would otherwise be 25 random probes per cell per pass; with the table the two
    // window sizes cost the same.
    private static void BuildSat(byte[] rock, int[] sat, int w, int h)
    {
        int stride = w + 1;
        Array.Clear(sat, 0, stride);
        for (int y = 0; y < h; y++)
        {
            int src = y * w, dst = (y + 1) * stride, prev = y * stride;
            sat[dst] = 0;
            int rowSum = 0;
            for (int x = 0; x < w; x++)
            {
                rowSum += rock[src + x];
                sat[dst + x + 1] = sat[prev + x + 1] + rowSum;
            }
        }
    }

    // Rock inside the inclusive rect, with everything off-map counted as rock — which is what
    // stops the smoothing eating outward into the border ring.
    private static int RectRock(int[] sat, int w, int h, int x0, int y0, int x1, int y1)
    {
        int total = (x1 - x0 + 1) * (y1 - y0 + 1);
        int cx0 = Math.Max(0, x0), cy0 = Math.Max(0, y0);
        int cx1 = Math.Min(w - 1, x1), cy1 = Math.Min(h - 1, y1);
        if (cx1 < cx0 || cy1 < cy0) return total;
        int stride = w + 1;
        int inside = sat[(cy1 + 1) * stride + cx1 + 1] - sat[cy0 * stride + cx1 + 1]
                   - sat[(cy1 + 1) * stride + cx0] + sat[cy0 * stride + cx0];
        return inside + total - (cx1 - cx0 + 1) * (cy1 - cy0 + 1);
    }

    // Opens an inclusive box, never outside the disk — an opened out-of-disk cell would be a hole
    // in the world boundary.
    private static void OpenBox(byte[] rock, int w, int h, bool[,]? mask, int x0, int y0, int x1, int y1)
    {
        x0 = Math.Max(0, x0); y0 = Math.Max(0, y0);
        x1 = Math.Min(w - 1, x1); y1 = Math.Min(h - 1, y1);
        for (int y = y0; y <= y1; y++)
        {
            int row = y * w;
            for (int x = x0; x <= x1; x++)
                if (mask == null || mask[x, y]) rock[row + x] = 0;
        }
    }

}
