using SAOTRPG.Map.Generation;
using SAOTRPG.UI;

namespace SAOTRPG.Map.Generation.Prefabs;

// Stamps a PrefabDefinition onto a GameMap. Fit-into-room iterates rooms in
// random order; for each, enumerates rotation/mirror variants until one fits.
public static class PrefabPlacer
{
    // Tries each shuffled room until a rotation/mirror variant fits with 1-tile inner margin.
    // chosenRoom = the Room that received the prefab; spawnQueue collects reserved-slot glyphs.
    public static bool TryPlaceInRoom(
        GameMap map,
        IReadOnlyList<Room> rooms,
        PrefabDefinition def,
        Random rng,
        out Room? chosenRoom,
        WorldContext? ctx = null,
        List<PrefabSpawnRequest>? spawnQueue = null)
    {
        chosenRoom = null;
        if (rooms.Count == 0) return false;

        // Shuffle via Durstenfeld — avoid OrderBy allocation pitfalls on big lists.
        var order = new int[rooms.Count];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (int idx in order)
        {
            var room = rooms[idx];
            // Require interior of room.Width-2 x room.Height-2 to leave a wall/floor margin.
            int maxW = room.Width  - 2;
            int maxH = room.Height - 2;
            if (maxW <= 0 || maxH <= 0) continue;

            // REQUIRES gate — retry next room if this one fails any clause.
            if (ctx != null && !EvaluateRequires(def, ctx, room)) continue;

            foreach (var variant in EnumerateOrientations(def, rng))
            {
                if (variant.Width > maxW || variant.Height > maxH) continue;
                int ox = room.X + 1 + (maxW - variant.Width)  / 2;
                int oy = room.Y + 1 + (maxH - variant.Height) / 2;
                var resolved = variant.Resolved(rng);
                if (!VariantFitsInDisk(resolved, ox, oy, ctx?.CircleMask)) continue;
                Stamp(map, resolved, ox, oy, def.KFeat, spawnQueue, ctx);
                chosenRoom = room;
                DebugLogger.LogGame("PREFAB",
                    $"placed '{def.Name}' at ({ox},{oy}) {resolved.Width}x{resolved.Height} in room({room.X},{room.Y})");
                return true;
            }
        }
        return false;
    }

    // ORIENT=encompass — stamps at map center (unrotated variant first). Used for boss arenas.
    public static bool TryPlaceEncompass(GameMap map, PrefabDefinition def, Random rng,
        WorldContext? ctx = null, List<PrefabSpawnRequest>? spawnQueue = null)
    {
        // Encompass has no "room" — pass null so room-scoped clauses soft-pass sanely.
        if (ctx != null && !EvaluateRequires(def, ctx, null))
        {
            DebugLogger.LogGame("PREFAB", $"'{def.Name}' encompass skipped — REQUIRES failed");
            return false;
        }
        var variants = EnumerateOrientations(def, rng).ToList();
        foreach (var variant in variants)
        {
            if (variant.Width > map.Width - 2 || variant.Height > map.Height - 2) continue;
            int ox = (map.Width  - variant.Width)  / 2;
            int oy = (map.Height - variant.Height) / 2;
            var resolved = variant.Resolved(rng);
            if (!VariantFitsInDisk(resolved, ox, oy, ctx?.CircleMask)) continue;
            Stamp(map, resolved, ox, oy, def.KFeat, spawnQueue, ctx);
            DebugLogger.LogGame("PREFAB",
                $"encompass-placed '{def.Name}' at ({ox},{oy}) {resolved.Width}x{resolved.Height}");
            return true;
        }
        DebugLogger.LogGame("PREFAB",
            $"'{def.Name}' encompass failed — prefab {def.Width}x{def.Height} too big for map {map.Width}x{map.Height}");
        return false;
    }

    // True when every non-noop tile in `v` lands inside the disk. Mask null = no constraint.
    private static bool VariantFitsInDisk(ParsedPrefab v, int ox, int oy, bool[,]? mask)
    {
        if (mask is null) return true;
        var grid = v.ActiveMap;
        int w = v.Width, h = v.Height;
        int mw = mask.GetLength(0), mh = mask.GetLength(1);
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (grid[x, y] == PrefabGlyphMapping.NoopGlyph) continue;
            int tx = ox + x, ty = oy + y;
            if ((uint)tx >= (uint)mw || (uint)ty >= (uint)mh) return false;
            if (!mask[tx, ty]) return false;
        }
        return true;
    }

    // Stamps a resolved variant. KFEAT overrides default glyph→TileType; ' ' leaves tile untouched;
    // '@' stamps Floor; reserved slot glyphs (1-7, a-d) stamp Floor AND push spawn request.
    public static void Stamp(
        GameMap map,
        ParsedPrefab variant,
        int ox,
        int oy,
        IReadOnlyDictionary<char, TileType> kfeat,
        List<PrefabSpawnRequest>? spawnQueue = null,
        WorldContext? ctx = null)
    {
        var grid = variant.ActiveMap;
        int w = variant.Width, h = variant.Height;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            char glyph = grid[x, y];
            if (glyph == PrefabGlyphMapping.NoopGlyph) continue;
            int tx = ox + x, ty = oy + y;
            if (!map.InBounds(tx, ty)) continue;
            // Soft-skip per-cell out-of-disk tiles — the variant fit-check above
            // should have caught full overruns, but soft-gate keeps the stamp safe.
            if (ctx?.CircleMask != null && !ctx.CircleMask[tx, ty]) continue;

            TileType tt;
            if (kfeat.TryGetValue(glyph, out var kt)) tt = kt;
            else if (glyph == PrefabGlyphMapping.AnchorGlyph) tt = TileType.Floor;
            else if (PrefabGlyphMapping.IsReservedSlot(glyph))
            {
                tt = TileType.Floor;
                if (spawnQueue != null)
                {
                    string kind = (glyph >= '1' && glyph <= '7') ? "mons" : "item";
                    spawnQueue.Add(new PrefabSpawnRequest(glyph, tx, ty, kind));
                }
            }
            else if (PrefabGlyphMapping.Default.TryGetValue(glyph, out var dt)) tt = dt;
            else
            {
                DebugLogger.LogGame("PREFAB", $"unknown glyph '{glyph}' at ({tx},{ty}) — stamped as Floor");
                tt = TileType.Floor;
            }

            map.SetTileType(tx, ty, tt);
        }
    }

    // Emits ParsedPrefab variants in a randomized order. Base first, then rotations + mirrors
    // per def.Rotate / def.Mirror flags. Duplicates suppressed on tiny symmetric prefabs.
    public static IEnumerable<ParsedPrefab> EnumerateOrientations(PrefabDefinition def, Random rng)
    {
        var baseP = new ParsedPrefab(def);
        var variants = new List<ParsedPrefab> { baseP };

        if (def.Rotate)
        {
            variants.Add(baseP.WithRotation(1));
            variants.Add(baseP.WithRotation(2));
            variants.Add(baseP.WithRotation(3));
        }
        if (def.Mirror)
        {
            int count = variants.Count;
            for (int i = 0; i < count; i++) variants.Add(variants[i].Mirrored());
        }

        // Shuffle in place.
        for (int i = variants.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (variants[i], variants[j]) = (variants[j], variants[i]);
        }
        return variants;
    }

    // All REQUIRES clauses are ANDed. Malformed clauses soft-pass (log + return true)
    // so a typo never blocks placement outright.
    private static bool EvaluateRequires(PrefabDefinition def, WorldContext ctx, Room? room)
    {
        foreach (var clause in def.Requires)
            if (!EvaluateClause(clause, ctx, room)) return false;
        return true;
    }

    // Supported: open_room(min=WxH), biome(name), floor(A-B) or floor(N). Bare flags soft-pass.
    private static bool EvaluateClause(string clause, WorldContext ctx, Room? room)
    {
        int lp = clause.IndexOf('('), rp = clause.LastIndexOf(')');
        if (lp <= 0 || rp <= lp) return true;  // bare / malformed — soft-pass
        string fn = clause[..lp].Trim().ToLowerInvariant();
        string arg = clause[(lp + 1)..rp].Trim();
        switch (fn)
        {
            case "open_room":
            {
                if (room is not Room r) return false;
                int xi = arg.IndexOf('=');
                string dims = xi >= 0 ? arg[(xi + 1)..].Trim() : arg;
                int sep = dims.IndexOf('x');
                if (sep < 0) return true;
                if (!int.TryParse(dims[..sep], out int rw)) return true;
                if (!int.TryParse(dims[(sep + 1)..], out int rh)) return true;
                return r.Width - 2 >= rw && r.Height - 2 >= rh;
            }
            case "biome":
                return string.Equals(arg, ctx.Biome.ToString(), StringComparison.OrdinalIgnoreCase);
            case "floor":
            {
                int dash = arg.IndexOf('-');
                if (dash < 0) return int.TryParse(arg, out int single) && ctx.FloorNumber == single;
                if (!int.TryParse(arg[..dash], out int lo)) return true;
                if (!int.TryParse(arg[(dash + 1)..], out int hi)) return true;
                return ctx.FloorNumber >= lo && ctx.FloorNumber <= hi;
            }
            default:
                DebugLogger.LogGame("PREFAB", $"unknown REQUIRES clause '{fn}' — soft-passed");
                return true;
        }
    }
}
