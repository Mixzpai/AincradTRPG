using System.Text;
using SAOTRPG.Entities;
using SAOTRPG.Items;
using SAOTRPG.Map.Generation.Prefabs;
using SAOTRPG.UI;

namespace SAOTRPG.Map.Generation.Passes;

// Stamps boss arena (ORIENT=encompass, TAGS has "boss") + per-room prefabs (30%).
// Runs between SpecialArea and FeatureScatter. F100 bypasses.
public sealed class PrefabPlacementPass : IGenerationPass
{
    public string Name => "PrefabPlacement";

    // Skip F100 (Ruby Palace bypasses pipeline) and any hand-authored SafeZone floor
    // (TOB on F1 — prefabs would stamp over the town's hand-built structures and sever boulevards).
    public bool ShouldRun(WorldContext ctx) => ctx.FloorNumber != 100 && ctx.Map.SafeZone == null;

    public void Execute(WorldContext ctx)
    {
        PrefabLibrary.Shared.LoadAll();
        if (PrefabLibrary.Shared.Count == 0) return;

        string biome = ctx.Biome.ToString().ToLowerInvariant();

        // 1) Boss-arena placement — prefer canon-boss slug tag; fallback to generic.
        TryPlaceBossArena(ctx, biome);

        // 2) Room-embedding — skip first (spawn) and last (boss) rooms.
        TryEmbedRoomPrefabs(ctx, biome);

        // 3) Open-ground landmarks. Appended last so every draw above keeps its position in the
        //    stream; inserting a step would renumber every later pass.
        TryPlaceOpenGroundPrefabs(ctx, biome);
    }

    // Rooms are wide and shallow — the largest interior any floor produces is 23x6 — so a prefab
    // taller than that has no room to embed in, and only boss-tagged prefabs reach the encompass
    // route. That left 44 of 82 non-boss prefabs with NO PLACEMENT ROUTE: every shrine, every
    // vault, every treasure room, every merchant camp, all eleven biome signature pieces. They
    // stamp onto open wilderness ground now, which is also where a player would expect to stumble
    // on one.
    private static void TryPlaceOpenGroundPrefabs(WorldContext ctx, string biome)
    {
        var pool = PrefabLibrary.Shared.CandidatesFor(biome, ctx.FloorNumber)
            .Where(p => !p.Tags.Contains("boss"))
            .Where(p => p.Orient != PrefabOrient.Encompass)
            .ToList();
        if (pool.Count == 0) return;

        // Scale with the disk, not with an absolute count — floors run 1000x1000 down to 100x100.
        int diskRadius = Math.Min(ctx.Width, ctx.Height) / 2;
        int budget = Math.Clamp(diskRadius / 70, 2, 8);

        var anchors = CollectOpenGroundAnchors(ctx);
        if (anchors.Count == 0)
        {
            DebugLogger.LogGame("PREFAB",
                $"floor={ctx.FloorNumber} no open-ground anchors — skipped landmark placement");
            return;
        }

        int placed = 0;
        for (int i = 0; i < budget; i++)
        {
            var pick = WeightedPick(pool, ctx.Rng);
            if (pick == null) break;
            if (ctx.Rng.NextDouble() > pick.Chance) continue;
            if (!MapGenerator.TryIncrementPrefabUse(pick.Name, pick.MaxPerGame)) continue;

            var spawnQueue = new List<PrefabSpawnRequest>();
            if (PrefabPlacer.TryPlaceFloating(ctx.Map, pick, ctx.Rng, anchors, ctx.Rooms, ctx, spawnQueue))
            {
                DrainSpawnQueue(ctx, pick, spawnQueue);
                placed++;
            }
            else RollbackPrefabUse(pick.Name);
        }

        if (placed > 0)
            DebugLogger.LogGame("PREFAB",
                $"floor={ctx.FloorNumber} placed {placed} open-ground landmarks "
                + $"(budget {budget}, {anchors.Count} anchors left)");
    }

    // One scan per floor, sampled on a stride: a landmark is a large footprint, so anchors a few
    // tiles apart are redundant and a full-resolution list is megabytes for nothing. Measuring the
    // size of the target BEFORE tuning the search is the ore-vein lesson.
    private static List<(int X, int Y)> CollectOpenGroundAnchors(WorldContext ctx)
    {
        const int Stride = 6;
        var map = ctx.Map;
        var anchors = new List<(int X, int Y)>();
        for (int y = 8; y < ctx.Height - 8; y += Stride)
        for (int x = 8; x < ctx.Width - 8; x += Stride)
        {
            if (!ctx.IsInsideCircle(x, y)) continue;
            if (!PrefabPlacer.IsClearGround(map.Tiles[x, y].Type)) continue;
            anchors.Add((x, y));
        }
        return anchors;
    }

    private static void TryPlaceBossArena(WorldContext ctx, string biome)
    {
        string bossTag = SlugifyBossName(BossFactory.GetBossName(ctx.FloorNumber));
        var bossPool = PrefabLibrary.Shared
            .CandidatesFor(biome, ctx.FloorNumber, new[] { "boss" })
            .ToList();
        if (bossPool.Count == 0) return;

        var canonHit = bossPool.FirstOrDefault(p => p.Tags.Contains(bossTag));
        if (TryPlaceCandidate(ctx, canonHit)) return;

        // Canon fell out (typically encompass disk-fit failure on small late floors).
        // Walk the generic encompass arenas large→small before giving up.
        var genericEncompass = bossPool
            .Where(p => p.Tags.Contains("generic") && p.Orient == PrefabOrient.Encompass)
            .ToList();
        // These are TAGS, not prefab names. They used to read "arena_large"/"arena_medium"/
        // "arena_small" — the files' NAME field — while arena_large.prefab actually carries
        // "TAGS: boss generic large any". So the fallback matched nothing and had NEVER once
        // fired: measured, 60 of 75 sampled floors reached the give-up log, and every one of
        // them has 3-5 encompass arenas available whose largest is 25x19, which fits any floor's
        // disk with room to spare.
        foreach (var sizeTag in new[] { "large", "medium", "small" })
        {
            var def = genericEncompass.FirstOrDefault(p => p.Tags.Contains(sizeTag));
            if (TryPlaceCandidate(ctx, def)) return;
        }

        // Last resort: weighted pick across everything else (room-anchored boss prefabs).
        var fallback = WeightedPick(bossPool.Where(p => p.Orient != PrefabOrient.Encompass).ToList(), ctx.Rng);
        if (TryPlaceCandidate(ctx, fallback)) return;

        // Says what actually happened rather than guessing at a cause. The old wording blamed
        // the disk, which was never the reason and sent one investigation down the wrong path.
        DebugLogger.LogGame("PREFAB",
            $"floor={ctx.FloorNumber} no boss arena placed (canon, generic and room-anchored all "
            + "declined); boss spawning into the procedural boss room only");
    }

    // Increment-then-place; rolls back the use counter on placement failure.
    private static bool TryPlaceCandidate(WorldContext ctx, PrefabDefinition? chosen)
    {
        if (chosen == null) return false;
        if (!MapGenerator.TryIncrementPrefabUse(chosen.Name, chosen.MaxPerGame))
        {
            DebugLogger.LogGame("PREFAB", $"'{chosen.Name}' skipped — MAX_PER_GAME={chosen.MaxPerGame} reached");
            return false;
        }
        var spawnQueue = new List<PrefabSpawnRequest>();
        bool placed;
        if (chosen.Orient == PrefabOrient.Encompass)
            placed = PrefabPlacer.TryPlaceEncompass(ctx.Map, chosen, ctx.Rng, ctx, spawnQueue);
        else
            placed = PrefabPlacer.TryPlaceInRoom(ctx.Map, ctx.Rooms, chosen, ctx.Rng, out _, ctx, spawnQueue);
        if (placed) DrainSpawnQueue(ctx, chosen, spawnQueue);
        else RollbackPrefabUse(chosen.Name);
        return placed;
    }

    private static void TryEmbedRoomPrefabs(WorldContext ctx, string biome)
    {
        if (ctx.Rooms.Count < 3) return;

        // Middle rooms only — skip spawn (index 0) and boss (last).
        var midRooms = new List<(int Idx, Room Room)>();
        for (int i = 1; i < ctx.Rooms.Count - 1; i++) midRooms.Add((i, ctx.Rooms[i]));
        // Shuffle.
        for (int i = midRooms.Count - 1; i > 0; i--)
        {
            int j = ctx.Rng.Next(i + 1);
            (midRooms[i], midRooms[j]) = (midRooms[j], midRooms[i]);
        }

        int placed = 0;
        foreach (var (_, room) in midRooms)
        {
            if (ctx.Rng.NextDouble() > 0.30) continue;
            int maxW = room.Width  - 2;
            int maxH = room.Height - 2;
            if (maxW <= 0 || maxH <= 0) continue;

            var pool = PrefabLibrary.Shared.CandidatesFor(biome, ctx.FloorNumber)
                .Where(p => !p.Tags.Contains("boss"))
                .Where(p => FitsEitherRotation(p, maxW, maxH))
                .ToList();
            var pick = WeightedPick(pool, ctx.Rng);
            if (pick == null) continue;
            if (ctx.Rng.NextDouble() > pick.Chance) continue;
            // MAX_PER_GAME gate — increment only when pick commits to placement.
            if (!MapGenerator.TryIncrementPrefabUse(pick.Name, pick.MaxPerGame))
            {
                DebugLogger.LogGame("PREFAB", $"'{pick.Name}' skipped — MAX_PER_GAME={pick.MaxPerGame} reached");
                continue;
            }
            var spawnQueue = new List<PrefabSpawnRequest>();
            if (PrefabPlacer.TryPlaceInRoom(ctx.Map, new[] { room }, pick, ctx.Rng, out _, ctx, spawnQueue))
            {
                DrainSpawnQueue(ctx, pick, spawnQueue);
                placed++;
            }
            else
            {
                // Placement failed (e.g. REQUIRES) — roll back the counter increment.
                RollbackPrefabUse(pick.Name);
            }
        }

        if (placed > 0)
            DebugLogger.LogGame("PREFAB", $"floor={ctx.FloorNumber} embedded {placed} room prefabs");
    }

    // MONS/ITEM directives fire here — consume queued slot positions into live mobs + ground items.
    // The friendly occupants a prefab may declare. Kept small and explicit — an unknown key
    // logs and leaves the tile bare rather than guessing at something hostile.
    private static NPC? BuildPrefabNpc(string key, int floor) => key.Trim().ToLowerInvariant() switch
    {
        "vendor" or "merchant" or "shopkeeper" => NewVendor(floor, "Travelling Merchant", "Wayside Stock"),
        "trader" => NewVendor(floor, "Caravan Trader", "Caravan Goods"),
        "guide" => NewVendor(floor, "Ranger Guide", "Ranger's Supplies"),
        _ => null,
    };

    private static Vendor NewVendor(int floor, string name, string shop)
    {
        var v = new Vendor { Name = name, ShopName = shop };
        v.GenerateStock(floor);
        return v;
    }

    private static void DrainSpawnQueue(WorldContext ctx, PrefabDefinition def, List<PrefabSpawnRequest> queue)
    {
        if (queue.Count == 0) return;
        foreach (var req in queue)
        {
            // A slot the prefab declares as an NPC takes precedence: the glyph classes overlap
            // (both use 1-7), and a stall's keeper must not come out as a bandit.
            if (req.Kind == "mons" && def.Npcs.TryGetValue(req.Glyph, out var npcKey))
            {
                var npc = BuildPrefabNpc(npcKey, ctx.FloorNumber);
                if (npc == null)
                {
                    DebugLogger.LogGame("PREFAB", $"'{def.Name}' unknown NPCS key '{npcKey}' — floor left bare");
                    continue;
                }
                ctx.Map.PlaceEntity(npc, req.X, req.Y);
            }
            else if (req.Kind == "mons")
            {
                if (!def.Mons.TryGetValue(req.Glyph, out var key))
                {
                    DebugLogger.LogGame("PREFAB", $"'{def.Name}' MONS slot '{req.Glyph}' unmapped — floor left bare");
                    continue;
                }
                var mob = MobFactory.CreateByKey(key, ctx.FloorNumber, 100, ctx.Rng);
                if (mob == null)
                {
                    DebugLogger.LogGame("PREFAB", $"'{def.Name}' unknown MONS key '{key}' — floor left bare");
                    continue;
                }
                ctx.Map.PlaceEntity(mob, req.X, req.Y);
            }
            else if (req.Kind == "item")
            {
                if (!def.Items.TryGetValue(req.Glyph, out var key))
                {
                    DebugLogger.LogGame("PREFAB", $"'{def.Name}' ITEM slot '{req.Glyph}' unmapped — floor left bare");
                    continue;
                }
                var item = ItemRegistry.Create(key);
                if (item == null)
                {
                    DebugLogger.LogGame("PREFAB", $"'{def.Name}' unknown ITEM key '{key}' — floor left bare");
                    continue;
                }
                ctx.Map.AddItem(req.X, req.Y, item);
            }
        }
    }

    // Mirror of TryIncrementPrefabUse: called only when the increment optimistically ran but
    // placement ultimately failed (e.g. all rooms rejected by REQUIRES). Keeps counter honest.
    private static void RollbackPrefabUse(string name)
    {
        var counts = MapGenerator.GetCurrentPrefabUseCounts();
        if (!counts.TryGetValue(name, out int v) || v <= 0) return;
        counts[name] = v - 1;
        if (counts[name] == 0) counts.Remove(name);
        MapGenerator.SetPrefabUseCounts(counts);
    }

    // Candidate fits if base or rotated dims fit the interior. Mirror doesn't change dims.
    private static bool FitsEitherRotation(PrefabDefinition def, int maxW, int maxH)
    {
        if (def.Width <= maxW && def.Height <= maxH) return true;
        if (def.Rotate && def.Height <= maxW && def.Width <= maxH) return true;
        return false;
    }

    private static PrefabDefinition? WeightedPick(IList<PrefabDefinition> pool, Random rng)
    {
        if (pool.Count == 0) return null;
        int total = 0;
        foreach (var p in pool) total += Math.Max(1, p.Weight);
        int r = rng.Next(total);
        foreach (var p in pool)
        {
            r -= Math.Max(1, p.Weight);
            if (r < 0) return p;
        }
        return pool[^1];
    }

    // "Illfang the Kobold Lord" → "illfang_the_kobold_lord" — canon boss tag convention.
    private static string SlugifyBossName(string name)
    {
        var sb = new StringBuilder(name.Length);
        bool lastUnderscore = false;
        foreach (char c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastUnderscore = false;
            }
            else if (!lastUnderscore && sb.Length > 0)
            {
                sb.Append('_');
                lastUnderscore = true;
            }
        }
        // Trim trailing underscore.
        if (sb.Length > 0 && sb[^1] == '_') sb.Length--;
        return sb.ToString();
    }
}
