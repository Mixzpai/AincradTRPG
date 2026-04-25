namespace SAOTRPG.Map.Generation.Prefabs;

// Parsed .prefab file — immutable record. Map[x,y] origin top-left.
// Library keys on Name; placer consumes Map + KFeat + Subst/NSubst.
public sealed record PrefabDefinition
{
    public required string Name { get; init; }
    public string Desc { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> Floors { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> Biomes { get; init; } = Array.Empty<string>();
    public int Weight { get; init; } = 10;
    public float Chance { get; init; } = 1.0f;
    public int MaxPerFloor { get; init; } = 1;
    public int MaxPerGame { get; init; } = 0;
    public bool Rotate { get; init; } = false;
    public bool Mirror { get; init; } = false;
    public PrefabOrient Orient { get; init; } = PrefabOrient.Float;
    public IReadOnlyList<string> Requires { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<char, string> Mons { get; init; } = new Dictionary<char, string>();
    public IReadOnlyDictionary<char, string> Items { get; init; } = new Dictionary<char, string>();
    public IReadOnlyDictionary<char, TileType> KFeat { get; init; } = new Dictionary<char, TileType>();
    public IReadOnlyList<SubstRule> Subst { get; init; } = Array.Empty<SubstRule>();
    public IReadOnlyList<NSubstRule> NSubst { get; init; } = Array.Empty<NSubstRule>();
    // Each entry = one group of glyphs whose positions are permuted at placement.
    public IReadOnlyList<string> Shuffle { get; init; } = Array.Empty<string>();
    public required char[,] Map { get; init; }
    public string SourceFile { get; init; } = "";
    public int Width => Map.GetLength(0);
    public int Height => Map.GetLength(1);
}

public enum PrefabOrient
{
    Float,
    Encompass,
    North,
    South,
    East,
    West,
    Center,
}

// SUBST: src glyph replaced by weighted random choice at placement time.
public sealed record SubstRule(char Source, (char Dst, int Weight)[] Options);

// NSUBST: src glyph — first N occurrences map to DstA, rest map to DstBForRest.
public sealed record NSubstRule(char Source, int CountA, char DstA, char DstBForRest);

// Default glyph → TileType. KFEAT directives override per-prefab.
// '@' = anchor (stamped as Floor), ' ' = no-op (leaves underlying tile).
public static class PrefabGlyphMapping
{
    public const char AnchorGlyph = '@';
    public const char NoopGlyph = ' ';

    public static readonly IReadOnlyDictionary<char, TileType> Default = new Dictionary<char, TileType>
    {
        ['.'] = TileType.Floor,
        [','] = TileType.Grass,
        ['#'] = TileType.Wall,
        ['+'] = TileType.Door,
        ['='] = TileType.CrackedWall,
        ['<'] = TileType.StairsUp,
        ['>'] = TileType.StairsDown,
        ['A'] = TileType.Shrine,
        ['E'] = TileType.EnchantShrine,
        ['S'] = TileType.SecretShrine,
        ['!'] = TileType.Fountain,
        ['V'] = TileType.Anvil,
        ['B'] = TileType.BountyBoard,
        ['M'] = TileType.MonumentOfSwordsmen,
        ['^'] = TileType.TrapSpike,
        ['$'] = TileType.Chest,
        ['*'] = TileType.LoreStone,
        ['J'] = TileType.Journal,
        ['F'] = TileType.Campfire,
        ['P'] = TileType.Pillar,
        ['~'] = TileType.Water,
        ['≈'] = TileType.WaterDeep,
        ['T'] = TileType.Tree,
        ['Y'] = TileType.Tree,
        ['♣'] = TileType.Bush,
        ['O'] = TileType.Rock,
        ['L'] = TileType.Lava,
        ['X'] = TileType.LabyrinthEntrance,
        ['_'] = TileType.Path,
        ['G'] = TileType.GasVent,
        ['l'] = TileType.Lever,
        ['p'] = TileType.PressurePlate,
        ['?'] = TileType.DangerZone,
    };

    // Reserved-slot glyphs (1-7 mons, a-d items). Stamp as Floor; Population consumes later.
    public static bool IsReservedSlot(char c) =>
        (c >= '1' && c <= '7') || (c >= 'a' && c <= 'd');

    // True if glyph is handled specially by placer (anchor/noop/reserved) — not via Default dict.
    public static bool IsSpecial(char c) =>
        c == AnchorGlyph || c == NoopGlyph || IsReservedSlot(c);
}
