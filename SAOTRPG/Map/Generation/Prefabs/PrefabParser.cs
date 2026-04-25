using System.Globalization;
using SAOTRPG.UI;

namespace SAOTRPG.Map.Generation.Prefabs;

// .prefab text → PrefabDefinition. Fail-soft: malformed files log + return null;
// PrefabLibrary skips them. Directive order free except MAP/ENDMAP must be last.
public static class PrefabParser
{
    // Returns null on any parse error. Never throws on malformed input.
    public static PrefabDefinition? ParseFile(string path)
    {
        try
        {
            string text = File.ReadAllText(path);
            return ParseText(text, path);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"PrefabParser.ParseFile {path}", ex);
            return null;
        }
    }

    public static PrefabDefinition? ParseText(string text, string sourcePath = "")
    {
        string? name = null;
        string desc = "";
        var tags     = new List<string>();
        var floors   = new List<int>();
        var biomes   = new List<string>();
        int weight      = 10;
        float chance    = 1.0f;
        int maxPerFloor = 1;
        int maxPerGame  = 0;
        bool rotate = false, mirror = false;
        var orient = PrefabOrient.Float;
        var requires = new List<string>();
        var mons  = new Dictionary<char, string>();
        var items = new Dictionary<char, string>();
        var kfeat = new Dictionary<char, TileType>();
        var subst  = new List<SubstRule>();
        var nsubst = new List<NSubstRule>();
        var shuffle = new List<string>();

        var mapRows = new List<string>();
        bool inMap   = false;
        bool seenEnd = false;

        string[] lines = text.Replace("\r", "").Split('\n');
        for (int li = 0; li < lines.Length; li++)
        {
            string rawLine = lines[li];

            if (inMap)
            {
                if (rawLine.TrimEnd().Equals("ENDMAP", StringComparison.Ordinal))
                {
                    seenEnd = true;
                    inMap   = false;
                    continue;
                }
                mapRows.Add(rawLine);
                continue;
            }

            string line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('#') || line.StartsWith("//")) continue;

            if (line.Equals("MAP", StringComparison.Ordinal))
            {
                inMap = true;
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(sourcePath)} L{li + 1}: expected DIRECTIVE: value, got '{line}'");
                continue;
            }

            string directive = line[..colon].Trim().ToUpperInvariant();
            string value     = line[(colon + 1)..].Trim();

            switch (directive)
            {
                case "NAME":   name = value; break;
                case "DESC":   desc = value; break;
                case "TAGS":   tags.AddRange(SplitWs(value)); break;
                case "FLOORS": floors.AddRange(ParseFloors(value)); break;
                case "BIOME":
                case "BIOMES": biomes.AddRange(SplitWs(value).Select(s => s.ToLowerInvariant())); break;
                case "WEIGHT":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int w)) weight = w;
                    break;
                case "CHANCE":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float c))
                        chance = Math.Clamp(c, 0f, 1f);
                    break;
                case "MAX_PER_FLOOR":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mpf)) maxPerFloor = mpf;
                    break;
                case "MAX_PER_GAME":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mpg)) maxPerGame = mpg;
                    break;
                case "ROTATE": rotate = ParseBool(value); break;
                case "MIRROR": mirror = ParseBool(value); break;
                case "ORIENT":
                    orient = ParseOrient(value, sourcePath, li + 1);
                    break;
                case "REQUIRES": requires.AddRange(SplitWs(value)); break;
                case "MONS":  ParseGlyphStringPairs(value, mons); break;
                case "ITEM":
                case "ITEMS": ParseGlyphStringPairs(value, items); break;
                case "KFEAT": ParseKFeat(value, kfeat, sourcePath, li + 1); break;
                case "SUBST": ParseSubst(value, subst, sourcePath, li + 1); break;
                case "NSUBST": ParseNSubst(value, nsubst, sourcePath, li + 1); break;
                case "SHUFFLE":
                    // Each whitespace-delimited token = one glyph group permuted at placement.
                    // Single-glyph groups are no-ops; silently skipped to keep author intent soft.
                    foreach (string grp in SplitWs(value))
                        if (grp.Length >= 2) shuffle.Add(grp);
                    break;
                default:
                    DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(sourcePath)} L{li + 1}: unknown directive '{directive}' — ignored");
                    break;
            }
        }

        if (name == null)
        {
            DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(sourcePath)}: missing NAME — skipped");
            return null;
        }
        if (mapRows.Count == 0)
        {
            DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(sourcePath)}: missing MAP block — skipped");
            return null;
        }
        if (inMap && !seenEnd)
        {
            DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(sourcePath)}: MAP never closed with ENDMAP — skipped");
            return null;
        }

        // Validate MAP rectangle — all rows same width.
        int rowW = mapRows[0].Length;
        for (int i = 1; i < mapRows.Count; i++)
        {
            if (mapRows[i].Length != rowW)
            {
                DebugLogger.LogGame("PREFAB",
                    $"{Path.GetFileName(sourcePath)}: MAP row {i + 1} width {mapRows[i].Length} != expected {rowW} — skipped");
                return null;
            }
        }
        if (rowW == 0)
        {
            DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(sourcePath)}: MAP block has zero width — skipped");
            return null;
        }

        int h = mapRows.Count;
        var map = new char[rowW, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < rowW; x++)
                map[x, y] = mapRows[y][x];

        return new PrefabDefinition
        {
            Name    = name,
            Desc    = desc,
            Tags    = tags,
            Floors  = floors,
            Biomes  = biomes,
            Weight  = weight,
            Chance  = chance,
            MaxPerFloor = maxPerFloor,
            MaxPerGame  = maxPerGame,
            Rotate = rotate,
            Mirror = mirror,
            Orient = orient,
            Requires = requires,
            Mons  = mons,
            Items = items,
            KFeat = kfeat,
            Subst  = subst,
            NSubst = nsubst,
            Shuffle = shuffle,
            Map    = map,
            SourceFile = sourcePath,
        };
    }

    private static IEnumerable<string> SplitWs(string value) =>
        value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool ParseBool(string value)
    {
        string v = value.Trim().ToLowerInvariant();
        return v == "true" || v == "yes" || v == "1";
    }

    // FLOORS:  A-B  |  n,m,k  |  *  — expanded to explicit ints (empty list means "any").
    private static IEnumerable<int> ParseFloors(string value)
    {
        string v = value.Trim();
        if (v.Length == 0 || v == "*") yield break;
        foreach (string part in v.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int dash = part.IndexOf('-');
            if (dash > 0)
            {
                if (int.TryParse(part[..dash], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) &&
                    int.TryParse(part[(dash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
                {
                    if (a > b) (a, b) = (b, a);
                    for (int i = a; i <= b; i++) yield return i;
                }
            }
            else if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            {
                yield return n;
            }
        }
    }

    private static PrefabOrient ParseOrient(string value, string src, int lineNo)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "float"     => PrefabOrient.Float,
            "encompass" => PrefabOrient.Encompass,
            "north"  => Warn(PrefabOrient.North, src, lineNo),
            "south"  => Warn(PrefabOrient.South, src, lineNo),
            "east"   => Warn(PrefabOrient.East, src, lineNo),
            "west"   => Warn(PrefabOrient.West, src, lineNo),
            "center" => Warn(PrefabOrient.Center, src, lineNo),
            _        => PrefabOrient.Float,
        };

        static PrefabOrient Warn(PrefabOrient o, string src, int lineNo)
        {
            DebugLogger.LogGame("PREFAB",
                $"{Path.GetFileName(src)} L{lineNo}: ORIENT={o} not yet implemented — treated as float");
            return o;
        }
    }

    // `g1 = id1 ; g2 = id2` — used by MONS and ITEM.
    private static void ParseGlyphStringPairs(string value, Dictionary<char, string> dst)
    {
        foreach (string entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = entry.IndexOf('=');
            if (eq <= 0) continue;
            string left  = entry[..eq].Trim();
            string right = entry[(eq + 1)..].Trim();
            if (left.Length != 1 || right.Length == 0) continue;
            dst[left[0]] = right;
        }
    }

    // KFEAT: `g = tile_type_name` — value mapped to TileType via Enum.TryParse (case-insensitive).
    private static void ParseKFeat(string value, Dictionary<char, TileType> dst, string src, int lineNo)
    {
        foreach (string entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = entry.IndexOf('=');
            if (eq <= 0) continue;
            string left  = entry[..eq].Trim();
            string right = entry[(eq + 1)..].Trim();
            if (left.Length != 1 || right.Length == 0) continue;
            if (TryResolveTileType(right, out var tt)) dst[left[0]] = tt;
            else DebugLogger.LogGame("PREFAB",
                $"{Path.GetFileName(src)} L{lineNo}: KFEAT target '{right}' unknown — skipped");
        }
    }

    // SUBST: `src = dst1:w1 dst2:w2 ...`  (weight optional, defaults to 1).
    private static void ParseSubst(string value, List<SubstRule> dst, string src, int lineNo)
    {
        int eq = value.IndexOf('=');
        if (eq <= 0) return;
        string left  = value[..eq].Trim();
        string right = value[(eq + 1)..].Trim();
        if (left.Length != 1 || right.Length == 0) return;
        char source = left[0];
        var opts = new List<(char Dst, int Weight)>();
        foreach (string tok in right.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int col = tok.IndexOf(':');
            char dstGlyph;
            int w = 1;
            if (col > 0)
            {
                if (tok[..col].Length != 1) continue;
                dstGlyph = tok[0];
                int.TryParse(tok[(col + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out w);
                if (w <= 0) w = 1;
            }
            else
            {
                if (tok.Length != 1) continue;
                dstGlyph = tok[0];
            }
            opts.Add((dstGlyph, w));
        }
        if (opts.Count == 0)
        {
            DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(src)} L{lineNo}: SUBST rule empty — ignored");
            return;
        }
        dst.Add(new SubstRule(source, opts.ToArray()));
    }

    // NSUBST: `g = N:dstA / rest:dstB`.
    private static void ParseNSubst(string value, List<NSubstRule> dst, string src, int lineNo)
    {
        int eq = value.IndexOf('=');
        if (eq <= 0) return;
        string left  = value[..eq].Trim();
        string right = value[(eq + 1)..].Trim();
        if (left.Length != 1) return;
        char source = left[0];

        string[] halves = right.Split('/', 2, StringSplitOptions.TrimEntries);
        if (halves.Length != 2)
        {
            DebugLogger.LogGame("PREFAB", $"{Path.GetFileName(src)} L{lineNo}: NSUBST missing '/' — ignored");
            return;
        }
        if (!TrySplitCount(halves[0], out int countA, out char dstA)) return;
        // Right half: "rest:X" or just "X".
        int col = halves[1].IndexOf(':');
        string rhsGlyph = col >= 0 ? halves[1][(col + 1)..].Trim() : halves[1].Trim();
        if (rhsGlyph.Length != 1) return;

        dst.Add(new NSubstRule(source, countA, dstA, rhsGlyph[0]));
    }

    private static bool TrySplitCount(string half, out int count, out char glyph)
    {
        count = 0;
        glyph = '\0';
        int col = half.IndexOf(':');
        if (col <= 0) return false;
        if (!int.TryParse(half[..col].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) return false;
        string g = half[(col + 1)..].Trim();
        if (g.Length != 1) return false;
        glyph = g[0];
        return true;
    }

    // KFEAT target string → TileType. Accepts enum name or snake_case alias (trap_spike → TrapSpike).
    private static bool TryResolveTileType(string value, out TileType tt)
    {
        if (Enum.TryParse<TileType>(value, ignoreCase: true, out tt)) return true;
        string pascal = SnakeToPascal(value);
        return Enum.TryParse<TileType>(pascal, ignoreCase: true, out tt);
    }

    private static string SnakeToPascal(string s)
    {
        var parts = s.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        foreach (var p in parts)
            if (p.Length > 0) sb.Append(char.ToUpperInvariant(p[0])).Append(p[1..].ToLowerInvariant());
        return sb.ToString();
    }
}
