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
    }

    private static void TryPlaceBossArena(WorldContext ctx, string biome)
    {
        string bossTag = SlugifyBossName(BossFactory.GetBossName(ctx.FloorNumber));
        var bossPool = PrefabLibrary.Shared
            .CandidatesFor(biome, ctx.FloorNumber, new[] { "boss" })
            .ToList();
        if (bossPool.Count == 0) return;

        var canonHit = bossPool.FirstOrDefault(p => p.Tags.Contains(bossTag));
        PrefabDefinition? chosen = canonHit;
        if (chosen == null)
        {
            var generic = bossPool.Where(p => p.Tags.Contains("generic")).ToList();
            chosen = WeightedPick(generic, ctx.Rng);
        }
        if (chosen == null) return;
        // MAX_PER_GAME check — skip placement if this prefab has hit its per-run cap.
        if (!MapGenerator.TryIncrementPrefabUse(chosen.Name, chosen.MaxPerGame))
        {
            DebugLogger.LogGame("PREFAB", $"'{chosen.Name}' skipped — MAX_PER_GAME={chosen.MaxPerGame} reached");
            return;
        }
        var spawnQueue = new List<PrefabSpawnRequest>();
        bool placed;
        if (chosen.Orient == PrefabOrient.Encompass)
            placed = PrefabPlacer.TryPlaceEncompass(ctx.Map, chosen, ctx.Rng, ctx, spawnQueue);
        else
            placed = PrefabPlacer.TryPlaceInRoom(ctx.Map, ctx.Rooms, chosen, ctx.Rng, out _, ctx, spawnQueue);
        if (placed) DrainSpawnQueue(ctx, chosen, spawnQueue);
        else RollbackPrefabUse(chosen.Name);
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
    private static void DrainSpawnQueue(WorldContext ctx, PrefabDefinition def, List<PrefabSpawnRequest> queue)
    {
        if (queue.Count == 0) return;
        foreach (var req in queue)
        {
            if (req.Kind == "mons")
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
