namespace SAOTRPG.Map;

// Aincrad's inverted-cone: F1 is the ~10km base, shrinking to F100 Ruby Palace (throne room only).
// Single scaling source for map dimensions, feature counts, and population densities.
public static class FloorScale
{
    // ── Dimension curve: size = max * (1 - t)^exp + min
    // F1 500x250, F25 290x145, F50 200x100, F75 120x60, F99 50x28; F100 special (Ruby Palace).

    private const int MaxW = 500, MaxH = 250;
    private const int MinW = 50,  MinH = 28;
    // Exponent < 1 → concave curve: slow shrink early, rapid shrink at top (matches inverted cone).
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

    // ── Feature scaling: multiplier (F1=1.0, down with area). Reference = F1 (500*250 = 125000).
    private const double ReferenceArea = MaxW * (double)MaxH;

    public static double AreaRatio(int floor)
    {
        var (w, h) = GetDimensions(floor);
        return (w * (double)h) / ReferenceArea;
    }

    // Convenience: scale a base count by area ratio, minimum 1.
    public static int Scale(int baseCount, int floor) =>
        Math.Max(1, (int)(baseCount * AreaRatio(floor) + 0.5));

    // ── Feature counts, tuned to F1 ref area; auto-scales down for upper floors.
    // `Random rng` overloads are used by threaded mapgen; default forms use Random.Shared.
    public static int TreeClusters(int floor) => TreeClusters(floor, Random.Shared);
    public static int TreeClusters(int floor, Random rng) => Scale(55, floor) + rng.Next(0, 8);
    public static int RockClusters(int floor) => RockClusters(floor, Random.Shared);
    public static int RockClusters(int floor, Random rng) => Scale(12, floor) + rng.Next(0, 4);
    public static int LakeCount(int floor)    => LakeCount(floor, Random.Shared);
    public static int LakeCount(int floor, Random rng) => Math.Max(0, Scale(6, floor) - 1) + rng.Next(0, 2);
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
    public static int DangerClusters(int floor) => DangerClusters(floor, Random.Shared);
    public static int DangerClusters(int floor, Random rng) => Scale(6, floor) + rng.Next(0, 3);
    public static int LavaPools(int floor)      => LavaPools(floor, Random.Shared);
    public static int LavaPools(int floor, Random rng) => Math.Max(0, floor - 1) + rng.Next(0, Scale(5, floor));
    public static int VentCount(int floor)      => Scale(6, floor) + floor;

    // ── Late-game floor type ─────────────────────────────────────────
    public static bool IsCastleFloor(int floor) => floor >= 100;
}
