
using SAOTRPG.Systems.RunState;
using SAOTRPG.Systems.World;

namespace SAOTRPG.Map.Generation;

// The labyrinth is the one map the climb enters on EVERY floor, and it used to generate the same
// way on all 99 of them: the same BSP partition at the same 80x60, ~25 fat rooms, 41-44% open,
// zero dead ends, on floor 3 and on floor 83 alike. Only the room positions moved. So the era now
// picks a LAYOUT as well as a palette, and the palette is a second axis so two eras sharing a
// layout are still told apart by what they are made of.
//
// Every layout ends at the same contract, which the rest of the game depends on:
//   rooms[0]  is the SOUTHERNMOST room and carries LabyrinthEntrance — PopulateFloor spawns the
//             player on its centre, so that centre must be walkable.
//   rooms[^1] is the NORTHERNMOST and carries StairsUp — PopulateFloor posts the floor boss on the
//             farthest REACHABLE StairsUp, so an unreachable one means no boss AND no way up.
//   every open cell is reachable from the entrance, by construction rather than by luck.
public static partial class MapGenerator
{
    private const int LabWidth  = 80;
    private const int LabHeight = 60;
    private const int MinLeaf   = 10;

    // How the era CARVES its labyrinth.
    private enum LabLayout
    {
        Halls,    // wide BSP chambers joined by L-corridors — the original, kept as the control
        Fissure,  // a finer BSP partition with thin rooms, plus pools of the era's hazard
        Grove,    // BSP chambers broken up from the inside by scattered blockers
        Cavern,   // cellular-automata chambers, tunnelled together
        Warren,   // a braided perfect maze
    }

    // One plan per biome. Members carry a different meaning per layout — that is what a tagged
    // union costs — so each is named for the axis it moves rather than for one layout's use of it.
    private readonly record struct LabPlan(
        LabLayout Layout,
        int       Grain,    // Halls/Fissure/Grove: the BSP minimum leaf. Warren: the maze cell pitch.
        int       Density,  // Cavern: seed rock %. Warren: how many open chambers to cut into the maze.
        double    Clutter,  // Fissure/Grove: obstacle density. Warren: the braid rate.
        TileType  Hazard,   // what Fissure/Grove scatter, and what Cavern/Warren leave standing
        TileType  Accent);  // the walkable floor accent worn along this era's walls

    // EVERY biome is routed, and the default arm THROWS rather than falling back to Halls. A
    // fallback would send an unrouted biome silently back to the one shape this file exists to
    // break up, and it would look like a design choice rather than an omission. The throw is not
    // the guard, though — a cast integer is the only value that can reach it at runtime, and a
    // newly added BiomeType member would compile fine. SeedProbe walks Enum.GetValues<BiomeType>()
    // and asserts every member plans, which is the half that catches an omission before it ships.
    private static LabPlan PlanFor(BiomeType biome) => biome switch
    {
        // The two eras that keep the original generator. Clutter 0 and a Floor accent make the
        // added steps no-ops, so their floors are byte-identical to the pre-routing build and
        // serve as the control.
        BiomeType.Grassland => new(LabLayout.Halls,   MinLeaf, 0,  0.00, TileType.Floor, TileType.Floor),
        BiomeType.Urban     => new(LabLayout.Halls,   MinLeaf, 0,  0.00, TileType.Floor, TileType.Floor),

        // Hazard pools. Lava BLOCKS the connectivity audit, so a volcanic pool is a real detour;
        // cracked ice does not, so an ice fissure is the same walk with a slip risk on it.
        BiomeType.Volcanic  => new(LabLayout.Fissure, 7,       0,  0.22, TileType.Lava,       TileType.Ash),
        BiomeType.Ice       => new(LabLayout.Fissure, 7,       0,  0.30, TileType.CrackedIce, TileType.Snow),

        // Overgrowth. Dense enough to break a chamber into pockets, which is where the dead ends
        // a BSP labyrinth has never had come from.
        BiomeType.Forest    => new(LabLayout.Grove,   MinLeaf, 0,  0.26, TileType.Tree, TileType.GrassSparse),
        BiomeType.Swamp     => new(LabLayout.Grove,   MinLeaf, 0,  0.20, TileType.Rock, TileType.Reeds),

        // Caverns. Dark is the denser of the two, so its chambers are smaller and its tunnels
        // longer; an aquatic sea-cave opens out.
        BiomeType.Dark      => new(LabLayout.Cavern,  0,       52, 0.00, TileType.Rock, TileType.Basalt),
        BiomeType.Aquatic   => new(LabLayout.Cavern,  0,       48, 0.00, TileType.Rock, TileType.Mud),

        // Mazes, separated by how far they are braided open. Ruins is the biome whose own
        // description is "ancient stone corridors"; the void is the one that should not let go.
        BiomeType.Ruins     => new(LabLayout.Warren,  2,       5,  0.35, TileType.Pillar, TileType.Path),
        BiomeType.Desert    => new(LabLayout.Warren,  2,       4,  0.55, TileType.Pillar, TileType.Sand),
        BiomeType.Void      => new(LabLayout.Warren,  2,       2,  0.18, TileType.Pillar, TileType.Basalt),

        _ => throw new ArgumentOutOfRangeException(nameof(biome), biome,
                 "no labyrinth layout is routed for this biome"),
    };

    // The two tiles this floor's labyrinth is made of beyond the shared Wall/Floor skeleton, or
    // Floor for an era that adds neither. PUBLIC so an offline check can assert they actually
    // REACH THE MAP rather than reading the table that declares them.
    //
    // That is not a hypothetical audit. The first build of this file deleted every obstacle in the
    // game: SealUnreachable walled over anything the flood could not reach, and an obstacle is
    // precisely what a flood cannot reach — Lava 0, Tree 0, Rock 0, on every era that placed one.
    // Neither the map's shape nor its connectivity changed when it happened, because a wall where
    // a tree should be is only wrong to look at.
    public static (TileType Hazard, TileType Accent) LabyrinthPalette(int floor)
    {
        var plan = PlanFor(BiomeSystem.GetBiome(floor));
        return (plan.Hazard, plan.Accent);
    }

    // Returns (map, rooms) with rooms[0]=entrance, rooms[^1]=boss room.
    public static (GameMap Map, List<Room> Rooms) GenerateLabyrinth(int floor)
    {
        // Derived from the run seed and the floor, NOT drawn inline from the run stream: a floor's
        // labyrinth must be the same one every time the player steps back into it, and it must
        // reproduce for a given seed regardless of how many turns preceded entry.
        var rng = RunRng.StreamFor("labyrinth", floor);
        var plan = PlanFor(BiomeSystem.GetBiome(floor));

        var map = new GameMap(LabWidth, LabHeight);

        // Fill everything with Wall — rooms and corridors are carved out.
        for (int x = 0; x < LabWidth; x++)
        for (int y = 0; y < LabHeight; y++)
            map.Tiles[x, y].Type = TileType.Wall;

        var rooms = plan.Layout switch
        {
            LabLayout.Cavern => CarveCavern(map, rng, plan, floor),
            LabLayout.Warren => CarveWarren(map, rng, plan, floor),
            _                => CarveBsp(map, rng, plan, floor),
        };

        // Sort by Y so southernmost = entrance, northernmost = boss room.
        rooms.Sort((a, b) => b.CenterY.CompareTo(a.CenterY));

        // Entrance (south) — LabyrinthEntrance tile to exit back to overworld.
        var entrance = rooms[0];
        map.Tiles[entrance.CenterX, entrance.CenterY].Type = TileType.LabyrinthEntrance;

        // Boss room (north) — StairsUp guarded by the boss.
        var bossRoom = rooms[^1];
        map.Tiles[bossRoom.CenterX, bossRoom.CenterY].Type = TileType.StairsUp;

        // A RING OF WALL AROUND THE STAIRS SEALS THEM; IT DOES NOT GUARD THEM. Walling all eight
        // neighbours left the way up with no approach at all, SealUnreachable then erased it as an
        // unreachable walkable cell, and EVERY labyrinth in the game came out with no floor boss
        // and no way up. Wall the CORNERS only: the alcove still reads as a chamber, and the four
        // approaches stay open instead of the stairs depending on a carve that may not oblige.
        for (int ry = bossRoom.CenterY - 1; ry <= bossRoom.CenterY + 1; ry++)
        for (int rx = bossRoom.CenterX - 1; rx <= bossRoom.CenterX + 1; rx++)
        {
            if (rx == bossRoom.CenterX || ry == bossRoom.CenterY || !map.InInterior(rx, ry)) continue;
            // NEVER OVER ANOTHER ROOM'S CENTRE. An organic layout places anchor rooms on whatever
            // is open, so two centres can land a diagonal apart — and PopulateFloor spawns the
            // player, the vendor and the boss ON centres, so walling one strands whatever it was
            // going to hold. JoinRoomCentres runs BEFORE this and cannot see it happen.
            bool isCentre = false;
            for (int i = 0; i < rooms.Count && !isCentre; i++)
                isCentre = rooms[i].CenterX == rx && rooms[i].CenterY == ry;
            if (isCentre) continue;
            map.Tiles[rx, ry].Type = TileType.Wall;
        }

        // AND TWO OF THEM MUST BE OPEN, NOT ONE. The floor boss stands ON the stairs, so the first
        // walkable neighbour is spent the moment it steps aside; a single-cell alcove then leaves
        // the player nowhere to stand while the tile is occupied. A tight carve does not always
        // supply a second, so carve one — south first, which is the side the entrance lies on.
        int approaches = 0;
        foreach (var (dx, dy) in Orthogonals)
        {
            int nx = bossRoom.CenterX + dx, ny = bossRoom.CenterY + dy;
            if (map.InBounds(nx, ny)
                && !Generation.Passes.ConnectivityAuditPass.IsAuditBlocking(map.Tiles[nx, ny].Type))
                approaches++;
        }
        int doorX = -1, doorY = -1;
        foreach (var (dx, dy) in Orthogonals)
        {
            if (approaches >= 2) break;
            int nx = bossRoom.CenterX + dx, ny = bossRoom.CenterY + dy;
            if (!map.InInterior(nx, ny)) continue;
            if (!Generation.Passes.ConnectivityAuditPass.IsAuditBlocking(map.Tiles[nx, ny].Type)) continue;
            map.Tiles[nx, ny].Type = TileType.Floor;
            if (doorX < 0) { doorX = nx; doorY = ny; }
            approaches++;
        }

        // Every room centre must be walkable AND reachable from the entrance, because the player
        // spawns on one and the boss stands on another. The carvers guarantee it; this is the net
        // that keeps the guarantee true for a carver that stops honouring it later.
        //
        // IT RUNS AFTER THE ALCOVE, NOT BEFORE. Walling the boss chamber's corners can remove the
        // only four-way connectors a nearby pocket had, and SealUnreachable then walls the pocket
        // — room centre included. Running the repair first meant it could not see the damage the
        // very next lines were about to do: F13 seed 31337 stranded room 16 of 19 that way.
        JoinRoomCentres(map, rooms);
        DressWalls(map, rng, plan, entrance, bossRoom);

        // Anything the carve or the hazard left cut off becomes rock. Not tidiness: chests and
        // wandering mobs are scattered onto any walkable tile, so an isolated pocket is loot the
        // player can see the glyph of and never reach.
        int sealedCells = SealUnreachable(map, entrance.CenterX, entrance.CenterY);

        // THE WAY UP IS CHECKED, NOT ASSUMED. Every other flaw on this map costs the player a
        // detour; this one ends the run, because the floor boss stands on the stairs and both the
        // boss and the exit are gone together. An era whose hazard happens to fill the approach
        // would reproduce the sealed-stairs bug in a form no single fix above can rule out, so the
        // guarantee is re-established here from whatever state the carve and the seal left.
        //
        // TWO REACHABLE approaches, and reachable is the load-bearing word: carving a neighbour
        // open is not enough, because a neighbour carved into solid rock is a stranded nook that
        // SealUnreachable walls straight back up. Measured over 8 seeds x 14 floors, that left 7
        // of 112 labyrinths with one approach -- each of which reads to the player as a boss whose
        // corpse blocks the only tile they can stand on.
        {
            map.Tiles[bossRoom.CenterX, bossRoom.CenterY].Type = TileType.StairsUp;
            int repaired = 0;
            for (int attempt = 0; attempt < Orthogonals.Length; attempt++)
            {
                var up = FloodOpen(map, entrance.CenterX, entrance.CenterY);
                int reached = 0;
                foreach (var (dx, dy) in Orthogonals)
                {
                    int nx = bossRoom.CenterX + dx, ny = bossRoom.CenterY + dy;
                    if (map.InBounds(nx, ny) && up[ny * LabWidth + nx]) reached++;
                }
                if (reached >= 2) break;

                // Tunnel to the first side that is not yet reachable. Recomputed each lap rather
                // than batched, because one corridor can open two sides at once and a batched
                // plan would dig a second trench for nothing.
                var target = (X: -1, Y: -1);
                foreach (var (dx, dy) in Orthogonals)
                {
                    int nx = bossRoom.CenterX + dx, ny = bossRoom.CenterY + dy;
                    if (!map.InInterior(nx, ny) || up[ny * LabWidth + nx]) continue;
                    target = (nx, ny);
                    break;
                }
                if (target.X < 0) break;

                map.Tiles[target.X, target.Y].Type = TileType.Floor;
                CarveCorridorL(map, entrance.CenterX, entrance.CenterY, target.X, target.Y);
                // BOTH ENDS ARE RE-STAMPED, because the tunnel starts ON the entrance cell and
                // paves what it walks: the first cut of this net traded a missing way up for a
                // missing way out, which is the same soft-lock facing the other direction.
                map.Tiles[entrance.CenterX, entrance.CenterY].Type = TileType.LabyrinthEntrance;
                map.Tiles[bossRoom.CenterX, bossRoom.CenterY].Type = TileType.StairsUp;
                repaired++;
            }
            if (repaired > 0)
                SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                    $"Labyrinth floor={floor} WARN the way up at ({bossRoom.CenterX},"
                    + $"{bossRoom.CenterY}) had too few approaches — tunnelled {repaired} from "
                    + "the entrance");
        }

        // Scatter campfire light sources for atmosphere.
        for (int i = 1; i < rooms.Count - 1; i++)
        {
            if (rng.Next(3) == 0)
            {
                var (cx, cy) = FirstFloorIn(map, rooms[i]);
                if (cx >= 0) map.Tiles[cx, cy].Type = TileType.Campfire;
            }
        }

        // Traps in corridors — spike traps on random floor tiles.
        int trapCount = 3 + floor * 2;
        int trapsPlaced = 0;
        for (int i = 0; i < trapCount; i++)
        {
            int tx = rng.Next(3, LabWidth - 3);
            int ty = rng.Next(3, LabHeight - 3);
            if (map.Tiles[tx, ty].Type == TileType.Floor)
            {
                // Don't trap the entrance or boss room centers.
                if (Math.Abs(tx - entrance.CenterX) < 3 && Math.Abs(ty - entrance.CenterY) < 3) continue;
                if (Math.Abs(tx - bossRoom.CenterX) < 3 && Math.Abs(ty - bossRoom.CenterY) < 3) continue;
                map.Tiles[tx, ty].Type = TileType.TrapSpike;
                trapsPlaced++;
            }
        }

        map.RecountWalkableTiles();
        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
            $"Labyrinth floor={floor} biome={BiomeSystem.GetBiome(floor)} layout={plan.Layout} "
            + $"rooms={rooms.Count} sealed={sealedCells} traps={trapsPlaced}/{trapCount}");
        return (map, rooms);
    }

    // ── BSP layouts: Halls, Fissure, Grove ───────────────────────────
    //
    // One routine for all three. Halls is the original at MinLeaf with no clutter; Fissure runs
    // the same partition finer and sizes its rooms down, so the map is corridor rather than
    // chamber; Grove keeps Halls' proportions and fills the chambers in instead.
    //
    // The clutter goes down BEFORE the corridors, so the corridor chain carves back through it.
    // That is what makes the result connected without a repair step: every room centre is on the
    // chain, and the chain is Floor.
    private static List<Room> CarveBsp(GameMap map, Random rng, LabPlan plan, int floor)
    {
        var leaves = new List<(int X, int Y, int W, int H)>();
        SplitBsp(2, 2, LabWidth - 4, LabHeight - 4, leaves, rng, plan.Grain);

        bool thin = plan.Layout == LabLayout.Fissure;
        var rooms = new List<Room>();
        foreach (var (lx, ly, lw, lh) in leaves)
        {
            int rw, rh;
            if (thin)
            {
                // The LOWER half of the leaf rather than the upper: a leaf this small would
                // otherwise still produce a chamber, and the point of a fissure is that it does not.
                rw = rng.Next(3, Math.Max(4, lw / 2));
                rh = rng.Next(3, Math.Max(4, lh / 2));
            }
            else
            {
                rw = rng.Next(Math.Max(4, lw / 2), lw - 1);
                rh = rng.Next(Math.Max(4, lh / 2), lh - 1);
            }
            int rx = lx + rng.Next(1, Math.Max(2, lw - rw));
            int ry = ly + rng.Next(1, Math.Max(2, lh - rh));
            CarveRoom(map, rx, ry, rw, rh);
            rooms.Add(new Room(rx, ry, rw, rh));
        }

        if (plan.Clutter > 0) Clutter(map, rng, rooms, plan);

        // Connect every pair of adjacent rooms with an L-shaped corridor.
        for (int i = 0; i < rooms.Count - 1; i++)
            CarveCorridorL(map, rooms[i].CenterX, rooms[i].CenterY,
                                rooms[i + 1].CenterX, rooms[i + 1].CenterY);

        // A fissure's own rooms are 3-5 cells across, and PopulateFloor treats a room as somewhere
        // several mobs AND a vendor fit: it fills rooms first and the vendor takes what is left, so
        // rooms that small placed a vendor on 0 of 8 seeds. There are also twice as many of them,
        // which would have doubled the mob budget on 26 floors. The anchor lattice is what the
        // organic layouts already hand back, and using it here decouples "what was carved" from
        // "where the run is populated".
        return thin ? AnchorRooms(map, floor) : rooms;
    }

    // The first open cell of a room, scanned from just inside its top-left corner. A Halls room is
    // a solid rectangle so this IS that corner and the original placement is unchanged; an anchor
    // room is mostly rock and its corner almost never open, which left a warren with no campfires
    // at all — and a labyrinth with no light source is a dark floor the player cannot read.
    private static (int x, int y) FirstFloorIn(GameMap map, Room room)
    {
        for (int y = room.Y + 1; y < room.Y + room.Height; y++)
        for (int x = room.X + 1; x < room.X + room.Width; x++)
            if (map.InBounds(x, y) && map.Tiles[x, y].Type == TileType.Floor) return (x, y);
        return (-1, -1);
    }

    // Fissure scatters POOLS and Grove scatters GRAINS, and that difference is why they read as
    // different places: a pool is a detour you walk around, a grain field is a room you pick your
    // way through. Both keep two cells clear of every room centre, which the corridor carve would
    // clear anyway but which also protects the centre if the carve order ever changes.
    private static void Clutter(GameMap map, Random rng, List<Room> rooms, LabPlan plan)
    {
        bool pools = plan.Layout == LabLayout.Fissure;
        foreach (var room in rooms)
        {
            if (pools)
            {
                // ONE pool per room, on a roll, rather than a count derived from the room's area.
                // A fissure room is 3-5 cells across, so `area * clutter / 12` truncated to zero
                // on every room in the game and the layout placed no hazard at all. An integer
                // division that switches a feature off is invisible in the map and in the log.
                //
                // No centre guard here either, and that was the second silent zero: a room this
                // small is entirely within two cells of its own centre, so "keep two cells clear
                // of every centre" rejected the whole room and left ONE hazard tile on the map.
                // The corridor chain runs centre to centre and is carved after this, so it is the
                // route's protection; the pool is what is left around it.
                if (rng.NextDouble() >= plan.Clutter) continue;
                int px = room.X + rng.Next(room.Width);
                int py = room.Y + rng.Next(room.Height);
                for (int y = py - 1; y <= py + 1; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if (map.InInterior(x, y)) map.Tiles[x, y].Type = plan.Hazard;
                continue;
            }

            for (int y = room.Y; y < room.Y + room.Height; y++)
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                if (!map.InInterior(x, y) || rng.NextDouble() >= plan.Clutter) continue;
                if (NearAnyCentre(rooms, x, y, 2)) continue;
                map.Tiles[x, y].Type = plan.Hazard;
            }
        }
    }

    // Drops `tile` on open cells that sit in the MIDDLE of open ground — every one of the eight
    // neighbours open — one in `oneIn` of them. That predicate is the whole safety argument: a
    // cell with eight open neighbours can be closed without disconnecting anything, because its
    // neighbours remain connected around it. Used for cavern boulders (Rock, which blocks) and
    // warren pillars (Pillar, which does not).
    private static void Sprinkle(GameMap map, Random rng, TileType tile, int oneIn)
    {
        for (int y = 2; y < LabHeight - 2; y++)
        for (int x = 2; x < LabWidth - 2; x++)
        {
            if (map.Tiles[x, y].Type != TileType.Floor) continue;
            int open = 0;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && map.Tiles[x + dx, y + dy].Type == TileType.Floor) open++;
            if (open == 8 && rng.Next(oneIn) == 0) map.Tiles[x, y].Type = tile;
        }
    }

    private static bool NearAnyCentre(List<Room> rooms, int x, int y, int pad)
    {
        foreach (var r in rooms)
            if (Math.Abs(x - r.CenterX) <= pad && Math.Abs(y - r.CenterY) <= pad) return true;
        return false;
    }

    // ── Cavern ───────────────────────────────────────────────────────
    //
    // Cellular automata, following the two things Map/Generation/Passes/BaseTerrainPass.CarveCaves
    // measured the hard way:
    //
    //   * The smoothing rule counts the CENTRE cell. "5 of 8 neighbours" looks equivalent and is
    //     an erosion rule: at 45% seed rock it falls to 4% in four passes.
    //   * "Keep the largest region" sits on a percolation cliff — the largest region collapses
    //     from a third of the map to a twentieth over one point of fill. Take the largest as a
    //     trunk and TUNNEL to the rest instead, so the result is one component by construction.
    private const int CavernPasses     = 4;
    private const int CavernMinChamber = 30;

    private static List<Room> CarveCavern(GameMap map, Random rng, LabPlan plan, int floor)
    {
        int w = LabWidth, h = LabHeight;
        var rock = new byte[w * h];
        var scratch = new byte[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool seeded = rng.Next(100) < plan.Density;
            // The border is rock whatever the roll, so the cavern pulls inward from the edge
            // rather than running open ground against the map boundary.
            rock[y * w + x] = (x < 2 || y < 2 || x >= w - 2 || y >= h - 2 || seeded) ? (byte)1 : (byte)0;
        }

        for (int it = 0; it < CavernPasses; it++)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (x < 2 || y < 2 || x >= w - 2 || y >= h - 2) { scratch[y * w + x] = 1; continue; }
                int solid = 0;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    solid += rock[(y + dy) * w + x + dx];
                scratch[y * w + x] = solid >= 5 ? (byte)1 : (byte)0;
            }
            (rock, scratch) = (scratch, rock);
        }

        var label = new int[w * h];
        var sizes = new List<int> { 0 };
        int trunk = 0, trunkSize = 0, id = 0;
        var queue = new int[w * h];
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
                if (cx > 0) PushOpen(cur - 1);
                if (cx < w - 1) PushOpen(cur + 1);
                if (cy > 0) PushOpen(cur - w);
                if (cy < h - 1) PushOpen(cur + w);

                void PushOpen(int ni)
                {
                    if (label[ni] != 0 || rock[ni] != 0) return;
                    label[ni] = id;
                    queue[tail++] = ni;
                }
            }
            sizes.Add(size);
            if (size > trunkSize) { trunkSize = size; trunk = id; }
        }

        var joined = new bool[id + 1];
        int tunnels = 0;
        if (trunk > 0)
        {
            joined[trunk] = true;
            tunnels = TunnelToTrunk(rock, label, joined, sizes, w, h, trunk);
        }

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (rock[y * w + x] == 0 && joined[label[y * w + x]])
                map.Tiles[x, y].Type = TileType.Floor;

        // Boulders standing in the open chambers. The first version looked for a WALL cell with
        // open ground on all eight sides, which is precisely the shape four passes of majority
        // smoothing exist to remove — it found none, ever, on any seed.
        Sprinkle(map, rng, plan.Hazard, 10);

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
            $"LabCavern floor={floor} fill={plan.Density} regions={id} trunk={trunkSize} tunnels={tunnels}");
        return AnchorRooms(map, floor);
    }

    // ONE breadth-first sweep outward from the trunk, through rock and open alike, gives every
    // other chamber its shortest route home at once: BFS visits in non-decreasing distance, so the
    // first cell of a chamber it reaches is that chamber's nearest connection point, and the
    // predecessor chain back from it is the route.
    private static int TunnelToTrunk(byte[] rock, int[] label, bool[] joined, List<int> sizes,
                                     int w, int h, int trunk)
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
        if (tail == 0) return 0;

        while (head < tail)
        {
            int cur = q[head++];
            int lab = label[cur];
            if (lab != 0 && lab != trunk && entry[lab] < 0) entry[lab] = cur;
            int cy = cur / w, cx = cur - cy * w;
            if (cx > 1) Step(cur - 1);
            if (cx < w - 2) Step(cur + 1);
            if (cy > 1) Step(cur - w);
            if (cy < h - 2) Step(cur + w);

            void Step(int ni)
            {
                if (prev[ni] != Unvisited) return;
                prev[ni] = cur;
                q[tail++] = ni;
            }
        }

        // Ascending label order, so which chambers earn a tunnel is a function of the seed alone.
        int tunnels = 0;
        for (int r = 1; r < joined.Length; r++)
        {
            if (joined[r] || entry[r] < 0 || sizes[r] < CavernMinChamber) continue;
            for (int step = entry[r]; step != Source; step = prev[step]) rock[step] = 0;
            joined[r] = true;
            tunnels++;
        }
        return tunnels;
    }

    // ── Warren ───────────────────────────────────────────────────────
    //
    // A recursive-backtracker perfect maze, then braided: a perfect maze is all dead ends and
    // nothing else, which is a puzzle rather than a place to fight in. The braid rate is what
    // separates the three eras that share this layout.
    private static List<Room> CarveWarren(GameMap map, Random rng, LabPlan plan, int floor)
    {
        int pitch = Math.Max(2, plan.Grain);
        int cw = (LabWidth - 3) / pitch + 1;
        int ch = (LabHeight - 3) / pitch + 1;
        int CellX(int i) => 1 + i * pitch;
        int CellY(int j) => 1 + j * pitch;

        var visited = new bool[cw * ch];
        var linked = new bool[cw * ch * 4];          // per cell: 0=W 1=E 2=N 3=S
        int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
        var candidates = new int[4];

        var stack = new List<int>();
        int start = rng.Next(ch) * cw + rng.Next(cw);
        visited[start] = true;
        stack.Add(start);
        while (stack.Count > 0)
        {
            int cur = stack[^1];
            int cy = cur / cw, cx = cur - cy * cw;
            int n = 0;
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || ny < 0 || nx >= cw || ny >= ch) continue;
                if (visited[ny * cw + nx]) continue;
                candidates[n++] = d;
            }
            if (n == 0) { stack.RemoveAt(stack.Count - 1); continue; }
            int dir = candidates[rng.Next(n)];
            int tx = cx + dxs[dir], ty = cy + dys[dir];
            linked[cur * 4 + dir] = true;
            linked[(ty * cw + tx) * 4 + (dir ^ 1)] = true;
            visited[ty * cw + tx] = true;
            stack.Add(ty * cw + tx);
        }

        // Braid. A dead end is a cell with one link; knocking a second wall turns the maze from a
        // tree into a graph with loops, which is the difference between "solve me" and "fight in
        // me". Cells in index order, so the choice is a function of the seed and nothing else.
        for (int cell = 0; cell < cw * ch; cell++)
        {
            int links = 0;
            for (int d = 0; d < 4; d++) if (linked[cell * 4 + d]) links++;
            if (links != 1 || rng.NextDouble() >= plan.Clutter) continue;
            int cy = cell / cw, cx = cell - cy * cw;
            int n = 0;
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || ny < 0 || nx >= cw || ny >= ch) continue;
                if (linked[cell * 4 + d]) continue;
                candidates[n++] = d;
            }
            if (n == 0) continue;
            int dir = candidates[rng.Next(n)];
            linked[cell * 4 + dir] = true;
            linked[((cy + dys[dir]) * cw + cx + dxs[dir]) * 4 + (dir ^ 1)] = true;
        }

        for (int j = 0; j < ch; j++)
        for (int i = 0; i < cw; i++)
        {
            int cell = j * cw + i;
            map.Tiles[CellX(i), CellY(j)].Type = TileType.Floor;
            // Only E and S are carved: every link is stored on both cells, so carving all four
            // directions would write the same run twice.
            if (linked[cell * 4 + 1])
                for (int k = 1; k < pitch; k++) map.Tiles[CellX(i) + k, CellY(j)].Type = TileType.Floor;
            if (linked[cell * 4 + 3])
                for (int k = 1; k < pitch; k++) map.Tiles[CellX(i), CellY(j) + k].Type = TileType.Floor;
        }

        // Chambers cut into the maze. A rectangle of maze cells is always joined to the rest,
        // because a spanning tree over every cell must have a link crossing the rectangle's
        // boundary — so this cannot strand anything.
        for (int c = 0; c < plan.Density; c++)
        {
            int i0 = rng.Next(cw - 2), j0 = rng.Next(ch - 2);
            int i1 = Math.Min(cw - 1, i0 + 2 + rng.Next(2));
            int j1 = Math.Min(ch - 1, j0 + 2 + rng.Next(2));
            for (int y = CellY(j0); y <= CellY(j1); y++)
            for (int x = CellX(i0); x <= CellX(i1); x++)
                map.Tiles[x, y].Type = TileType.Floor;
        }

        // Freestanding pillars, and only inside the chambers: Pillar does not block movement, so
        // one dropped into a one-wide corridor would read as a wall the player can walk through.
        Sprinkle(map, rng, plan.Hazard, 5);

        SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
            $"LabWarren floor={floor} cells={cw}x{ch} pitch={pitch} braid={plan.Clutter:F2} "
            + $"chambers={plan.Density}");
        return AnchorRooms(map, floor);
    }

    // ── Shared: rooms, connectivity, dressing ────────────────────────

    // An organic layout has no rectangles to hand back, and PopulateFloor needs some: it spawns
    // the player on rooms[0], picks a vendor room, and scatters mobs room by room. So the rooms
    // are sampled off a fixed lattice, each snapped to the nearest open cell. The pitch is chosen
    // to land near the ~25 rooms a BSP labyrinth produces, so the mob budget does not move when
    // the layout does.
    private const int AnchorPitch    = 14;
    private const int AnchorRadius   = 6;
    private const int AnchorRoomSize = 9;

    private static List<Room> AnchorRooms(GameMap map, int floor)
    {
        var rooms = new List<Room>();
        var taken = new List<(int X, int Y)>();
        for (int gy = 7; gy < LabHeight - 3; gy += AnchorPitch)
        for (int gx = 7; gx < LabWidth - 3; gx += AnchorPitch)
        {
            var best = (x: -1, y: -1, d: int.MaxValue);
            for (int y = gy - AnchorRadius; y <= gy + AnchorRadius; y++)
            for (int x = gx - AnchorRadius; x <= gx + AnchorRadius; x++)
            {
                if (!map.InInterior(x, y) || map.Tiles[x, y].Type != TileType.Floor) continue;
                int d = (x - gx) * (x - gx) + (y - gy) * (y - gy);
                if (d >= best.d) continue;
                best = (x, y, d);
            }
            if (best.x < 0) continue;
            bool crowded = false;
            foreach (var (tx, ty) in taken)
                if (Math.Abs(tx - best.x) < AnchorRoomSize && Math.Abs(ty - best.y) < AnchorRoomSize)
                { crowded = true; break; }
            if (crowded) continue;
            taken.Add((best.x, best.y));
            int half = AnchorRoomSize / 2;
            rooms.Add(new Room(best.x - half, best.y - half, AnchorRoomSize, AnchorRoomSize));
        }

        // Three is PopulateFloor's own threshold for placing a vendor, and below it there is not
        // enough map left to spawn a run on. Disclosed rather than papered over: the carve that
        // produced it is the thing to fix.
        if (rooms.Count < 3)
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"Labyrinth floor={floor} WARN only {rooms.Count} anchor room(s) — the carve left "
                + "almost nothing open and the floor will be near-empty");
        return rooms;
    }

    // Reconnects any room centre the carve left stranded, straight through the rock. Nothing
    // should reach it — every carver connects what it makes — so it logs whenever it fires.
    private static void JoinRoomCentres(GameMap map, List<Room> rooms)
    {
        if (rooms.Count == 0) return;
        int w = map.Width;
        int sx = rooms[0].CenterX, sy = rooms[0].CenterY;
        if (map.InBounds(sx, sy) && map.Tiles[sx, sy].Type == TileType.Wall)
            map.Tiles[sx, sy].Type = TileType.Floor;

        // THE TWO ENDS ARE PRESERVED ACROSS THE CARVE. CarveCorridorL starts ON the source cell
        // and paves what it walks, so tunnelling from the entrance overwrote LabyrinthEntrance
        // with Floor and 26 of 132 labyrinths lost their way back to the overworld. The
        // destination is the same hazard from the other side: rooms[^1]'s centre carries StairsUp.
        var startType = map.Tiles[sx, sy].Type;
        for (int i = 1; i < rooms.Count; i++)
        {
            int cx = rooms[i].CenterX, cy = rooms[i].CenterY;
            if (!map.InBounds(cx, cy)) continue;
            if (map.Tiles[cx, cy].Type == TileType.Wall) map.Tiles[cx, cy].Type = TileType.Floor;
            var reach = FloodOpen(map, sx, sy);
            if (reach[cy * w + cx]) continue;
            var endType = map.Tiles[cx, cy].Type;
            CarveCorridorL(map, sx, sy, cx, cy);
            map.Tiles[sx, sy].Type = startType;
            map.Tiles[cx, cy].Type = endType;
            SAOTRPG.Systems.Diagnostics.DebugLogger.LogGame("MAPGEN",
                $"Labyrinth WARN room centre ({cx},{cy}) was stranded — tunnelled from the entrance");
        }
    }

    // The era's accent, worn along the walls. Wall-adjacent rather than scattered because a rim
    // reads as a material at a glance where a sprinkle reads as noise, and because PopulateFloor
    // only spawns on Floor/Path/Door and the grass family — keeping the accent off the open middle
    // leaves the mob budget where it was.
    private const int AccentPercent = 22;

    private static void DressWalls(GameMap map, Random rng, LabPlan plan, Room entrance, Room boss)
    {
        if (plan.Accent == TileType.Floor) return;
        for (int y = 1; y < LabHeight - 1; y++)
        for (int x = 1; x < LabWidth - 1; x++)
        {
            if (map.Tiles[x, y].Type != TileType.Floor) continue;
            if (Math.Abs(x - entrance.CenterX) <= 1 && Math.Abs(y - entrance.CenterY) <= 1) continue;
            if (Math.Abs(x - boss.CenterX) <= 1 && Math.Abs(y - boss.CenterY) <= 1) continue;
            bool touchesWall = map.Tiles[x - 1, y].Type == TileType.Wall
                            || map.Tiles[x + 1, y].Type == TileType.Wall
                            || map.Tiles[x, y - 1].Type == TileType.Wall
                            || map.Tiles[x, y + 1].Type == TileType.Wall;
            if (touchesWall && rng.Next(100) < AccentPercent) map.Tiles[x, y].Type = plan.Accent;
        }
    }

    // The four sides of a cell, south first: the entrance is the southernmost room, so a tunnel
    // that has to pick a side should pick the one it will be approached from.
    private static readonly (int X, int Y)[] Orthogonals = { (0, 1), (1, 0), (-1, 0), (0, -1) };

    // Every WALKABLE cell that cannot be walked to from the entrance becomes rock. Returns how
    // many cells that was, which is the number to watch: a large one means the hazard density cut
    // the map up rather than decorated it.
    //
    // The blocking test is what makes this correct, and leaving it out deleted every obstacle in
    // the game: a lava pool, a tree and a boulder are blocking, so the flood never reaches them,
    // so a "seal everything unreached" rule turned all of them into Wall. Measured on the first
    // build — Lava 0, Tree 0, Rock 0 across every era that placed them, and the map still looked
    // plausible because a wall where a tree should be is only wrong to look at.
    private static int SealUnreachable(GameMap map, int sx, int sy)
    {
        int w = map.Width, h = map.Height;
        var reach = FloodOpen(map, sx, sy);
        int sealedCells = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (reach[y * w + x]) continue;
            if (Generation.Passes.ConnectivityAuditPass.IsAuditBlocking(map.Tiles[x, y].Type)) continue;
            map.Tiles[x, y].Type = TileType.Wall;
            sealedCells++;
        }
        return sealedCells;
    }

    // Four-way flood through the connectivity audit's OWN blocking predicate, so "reachable" here
    // and "reachable" when PopulateFloor looks for a way up cannot drift apart.
    private static bool[] FloodOpen(GameMap map, int sx, int sy)
    {
        int w = map.Width, h = map.Height;
        var seen = new bool[w * h];
        if (!map.InBounds(sx, sy)) return seen;
        if (Generation.Passes.ConnectivityAuditPass.IsAuditBlocking(map.Tiles[sx, sy].Type)) return seen;
        var queue = new int[w * h];
        int head = 0, tail = 0;
        queue[tail++] = sy * w + sx;
        seen[sy * w + sx] = true;
        while (head < tail)
        {
            int cur = queue[head++];
            int cy = cur / w, cx = cur - cy * w;
            Step(cx - 1, cy); Step(cx + 1, cy); Step(cx, cy - 1); Step(cx, cy + 1);

            void Step(int nx, int ny)
            {
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) return;
                int ni = ny * w + nx;
                if (seen[ni]) return;
                if (Generation.Passes.ConnectivityAuditPass.IsAuditBlocking(map.Tiles[nx, ny].Type)) return;
                seen[ni] = true;
                queue[tail++] = ni;
            }
        }
        return seen;
    }

    // ── BSP helpers ──────────────────────────────────────────────────

    private static void SplitBsp(int x, int y, int w, int h, List<(int, int, int, int)> leaves,
                                 Random rng, int minLeaf)
    {
        if (w < minLeaf * 2 && h < minLeaf * 2)
        {
            leaves.Add((x, y, w, h));
            return;
        }

        bool splitH = w < minLeaf * 2 ? true
                     : h < minLeaf * 2 ? false
                     : rng.Next(2) == 0;

        if (splitH)
        {
            int splitY = y + rng.Next(minLeaf, Math.Max(minLeaf + 1, h - minLeaf));
            SplitBsp(x, y, w, splitY - y, leaves, rng, minLeaf);
            SplitBsp(x, splitY, w, y + h - splitY, leaves, rng, minLeaf);
        }
        else
        {
            int splitX = x + rng.Next(minLeaf, Math.Max(minLeaf + 1, w - minLeaf));
            SplitBsp(x, y, splitX - x, h, leaves, rng, minLeaf);
            SplitBsp(splitX, y, x + w - splitX, h, leaves, rng, minLeaf);
        }
    }

    private static void CarveRoom(GameMap map, int rx, int ry, int rw, int rh)
    {
        for (int x = rx; x < rx + rw && x < map.Width; x++)
        for (int y = ry; y < ry + rh && y < map.Height; y++)
            if (map.InBounds(x, y)) map.Tiles[x, y].Type = TileType.Floor;
    }

    private static void CarveCorridorL(GameMap map, int x1, int y1, int x2, int y2)
    {
        // L-shaped: go horizontal first, then vertical.
        int cx = x1, cy = y1;
        int sx = x1 < x2 ? 1 : -1;
        while (cx != x2)
        {
            if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = TileType.Floor;
            cx += sx;
        }
        int sy = y1 < y2 ? 1 : -1;
        while (cy != y2)
        {
            if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = TileType.Floor;
            cy += sy;
        }
        if (map.InBounds(cx, cy)) map.Tiles[cx, cy].Type = TileType.Floor;
    }
}
