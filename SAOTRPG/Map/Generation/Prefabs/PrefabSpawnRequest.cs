namespace SAOTRPG.Map.Generation.Prefabs;

// Runtime-only spawn descriptor emitted by PrefabPlacer.Stamp and drained by
// PrefabPlacementPass. Not serialized. Kind is "mons" or "item".
public sealed record PrefabSpawnRequest(char Glyph, int X, int Y, string Kind);
