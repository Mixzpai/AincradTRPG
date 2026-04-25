namespace SAOTRPG.Map.Generation.Prefabs;

// Runtime view of a PrefabDefinition: carries an ActiveMap that may be rotated,
// mirrored, and Subst/NSubst-resolved. Original def map is never mutated.
public sealed class ParsedPrefab
{
    public PrefabDefinition Source { get; }
    public char[,] ActiveMap { get; private set; }
    public int Width  => ActiveMap.GetLength(0);
    public int Height => ActiveMap.GetLength(1);

    public ParsedPrefab(PrefabDefinition def)
    {
        Source    = def;
        ActiveMap = Clone(def.Map);
    }

    private ParsedPrefab(PrefabDefinition def, char[,] map)
    {
        Source    = def;
        ActiveMap = map;
    }

    // Clockwise quarter-turns (0..3). 1 = +90° CW, 2 = 180°, 3 = +270° CW.
    public ParsedPrefab WithRotation(int quarterTurns)
    {
        int turns = ((quarterTurns % 4) + 4) % 4;
        var m = ActiveMap;
        for (int i = 0; i < turns; i++) m = RotateCw(m);
        return new ParsedPrefab(Source, m);
    }

    // Horizontal mirror (flips left/right).
    public ParsedPrefab Mirrored()
    {
        int w = Width, h = Height;
        var dst = new char[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                dst[x, y] = ActiveMap[w - 1 - x, y];
        return new ParsedPrefab(Source, dst);
    }

    // Resolves SUBST + NSUBST into concrete glyphs using the placement rng.
    public ParsedPrefab Resolved(Random rng)
    {
        int w = Width, h = Height;
        var dst = Clone(ActiveMap);

        // SUBST: per-cell weighted pick. Each matching cell rolls independently.
        foreach (var rule in Source.Subst)
        {
            int totalWeight = 0;
            foreach (var o in rule.Options) totalWeight += Math.Max(1, o.Weight);
            if (totalWeight <= 0) continue;
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (dst[x, y] != rule.Source) continue;
                int r = rng.Next(totalWeight);
                foreach (var o in rule.Options)
                {
                    r -= Math.Max(1, o.Weight);
                    if (r < 0) { dst[x, y] = o.Dst; break; }
                }
            }
        }

        // NSUBST: collect all positions, shuffle, first N → DstA, rest → DstBForRest.
        foreach (var rule in Source.NSubst)
        {
            var positions = new List<(int X, int Y)>();
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (dst[x, y] == rule.Source) positions.Add((x, y));
            // Fisher-Yates shuffle.
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }
            int n = Math.Min(rule.CountA, positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                var (px, py) = positions[i];
                dst[px, py] = i < n ? rule.DstA : rule.DstBForRest;
            }
        }

        // SHUFFLE: Fisher-Yates permute positions of each group's glyphs. Order
        // matters — runs AFTER SUBST/NSUBST so final glyphs shuffle, not placeholders.
        foreach (string group in Source.Shuffle)
        {
            if (group.Length < 2) continue;
            var positions = new List<(int X, int Y)>();
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (group.IndexOf(dst[x, y]) >= 0) positions.Add((x, y));
            if (positions.Count < 2) continue;
            var glyphs = new char[positions.Count];
            for (int i = 0; i < positions.Count; i++) glyphs[i] = dst[positions[i].X, positions[i].Y];
            for (int i = glyphs.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (glyphs[i], glyphs[j]) = (glyphs[j], glyphs[i]);
            }
            for (int i = 0; i < positions.Count; i++)
                dst[positions[i].X, positions[i].Y] = glyphs[i];
        }

        return new ParsedPrefab(Source, dst);
    }

    // Transpose + reverse-rows = +90° CW rotation for [w,h] char grid.
    private static char[,] RotateCw(char[,] src)
    {
        int w = src.GetLength(0), h = src.GetLength(1);
        var dst = new char[h, w];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                dst[h - 1 - y, x] = src[x, y];
        return dst;
    }

    private static char[,] Clone(char[,] src)
    {
        int w = src.GetLength(0), h = src.GetLength(1);
        var dst = new char[w, h];
        Array.Copy(src, dst, src.Length);
        return dst;
    }
}
