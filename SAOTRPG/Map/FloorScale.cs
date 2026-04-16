namespace SAOTRPG.Map;

// Aincrad's inverted-cone geometry: Floor 1 is the massive base (~10 km
// across in lore), each floor above is progressively smaller, until
// Floor 100 is the tiny Ruby Palace — just a castle with a throne room.
//
// All map dimensions, feature counts, and population densities derive
// from this single scaling table so the feel changes naturally as the
// player climbs.
public static class FloorScale
{
    // ── Dimension curve ──────────────────────────────────────────────
    // Floor  1: 500 x 250  (vast wilderness + Town of Beginnings)
    // Floor  5: 440 x 220
    // Floor 10: 390 x 195
    // Floor 25: 290 x 145  (large mid-game)
    // Floor 50: 200 x 100
    // Floor 75: 120 x  60
    // Floor 90:  80 x  40
    // Floor 99:  50 x  28
    // Floor 100: special — tiny castle, handled separately
    //
    // Uses a power-curve: size = max * (1 - t)^exp + min

    private const int MaxW = 500, MaxH = 250;
    private const int MinW = 50,  MinH = 28;
    // Exponent < 1 gives a concave curve: early floors shrink slowly,
    // upper floors shrink rapidly -- matches Aincrad's inverted cone shape.
    private const double Exponent = 0.55;

    public static (int Width, int Height) GetDimensions(int floor)
    {
        if (floor >= 100) return (50, 35); // Ruby Palace
        double t = (floor - 1) / 99.0;
        double scale = Math.Pow(1.0 - t, Exponent);
        int w = MinW + (int)((MaxW - MinW) * scale);
        int h = MinH + (int)((MaxH - MinH) * scale);
        // Round to even for nice centering
        if (w % 2 != 0) w++;
        if (h % 2 != 0) h++;
        return (w, h);
    }

    // ── Feature scaling ──────────────────────────────────────────────
    // Returns a multiplier (0.0 – 1.0+) for how many terrain features,
    // mobs, chests etc. to place. Floor 1 = 1.0, scales down with area.
    // Reference area = Floor 1 (500*250 = 125000).
    private const double ReferenceArea = MaxW * (double)MaxH;

    public static double AreaRatio(int floor)
    {
        var (w, h) = GetDimensions(floor);
        return (w * (double)h) / ReferenceArea;
    }

    // Convenience: scale a base count by area ratio, minimum 1.
    public static int Scale(int baseCount, int floor) =>
        Math.Max(1, (int)(baseCount * AreaRatio(floor) + 0.5));

    // ── Feature counts ───────────────────────────────────────────────
    // Base counts are tuned for the Floor 1 (500x250) reference area.
    // Everything auto-scales down for smaller upper floors.
    public static int TreeClusters(int floor) => Scale(55, floor) + Random.Shared.Next(0, 8);
    public static int RockClusters(int floor) => Scale(12, floor) + Random.Shared.Next(0, 4);
    public static int LakeCount(int floor)    => Math.Max(0, Scale(6, floor) - 1) + Random.Shared.Next(0, 2);
    public static int ClearingsPerQuad(int floor)
    {
        double ratio = AreaRatio(floor);
        if (ratio > 0.7) return 4;
        if (ratio > 0.4) return 3;
        if (ratio > 0.15) return 2;
        return 1;
    }

    // Mob scaling: large maps get more wandering mobs.
    public static int WanderingMobs(int floor) => Scale(16, floor) + Random.Shared.Next(0, 4);
    public static int WildernessMobsFloor1(int floor) => Scale(16, floor) + Random.Shared.Next(0, 4);
    public static int ChestCount(int floor)    => Scale(10, floor) + Random.Shared.Next(0, 3);
    public static int TrapCount(int floor)     => Scale(14, floor) + floor * 2;
    public static int DenCount(int floor)      => Math.Max(1, Scale(4, floor));
    public static int ScatteredTrees(int floor) => Scale(80, floor);
    public static int ScatteredBushes(int floor) => Scale(50, floor);
    public static int DangerClusters(int floor) => Scale(6, floor) + Random.Shared.Next(0, 3);
    public static int LavaPools(int floor)      => Math.Max(0, floor - 1) + Random.Shared.Next(0, Scale(5, floor));
    public static int VentCount(int floor)      => Scale(6, floor) + floor;

    // ── Late-game floor type ─────────────────────────────────────────
    public static bool IsCastleFloor(int floor) => floor >= 100;
}
