namespace SAOTRPG.Map;

// Aincrad's inverted-cone: F1 1000x1000 disk shrinking linearly to F99 100x100;
// F100 Ruby Palace keeps bespoke compact dims. Towns (F1/F48) inherit linear disk
// dims with their hand-built generators stamped at the disk center.
public static class FloorScale
{
    // F1=1000, F99=100; linear delta 900/98 ≈ 9.18 per floor. Square bbox; the
    // circular mask carves the disk inside.
    public const int MaxDim = 1000;
    public const int MinDim = 100;

    public static (int Width, int Height) GetDimensions(int floor)
    {
        if (floor <= 0) return (50, 28);
        if (floor == 100) return GetCastleDimensions();

        int dim = (int)Math.Round(MaxDim - (floor - 1) * ((double)(MaxDim - MinDim) / 98.0));
        dim = Math.Max(MinDim, Math.Min(MaxDim, dim));
        return (dim, dim);
    }

    // Deprecated — towns now share linear disk dims; SpecialAreaPass still stamps
    // the F1/F48 generators inside the disk via direct floor==1/48 checks.
    public static bool IsHandBuiltTownFloor(int floor) => false;

    // F100 Ruby Palace — bespoke compact throne dims; bypasses the pipeline.
    private static (int Width, int Height) GetCastleDimensions() => (50, 35);

    // ── Feature scaling: F1 ref area for legacy formulas. Width-squared dropoff
    // means floors thin out smoothly as the disk shrinks.
    private const int LegacyRefW = 500, LegacyRefH = 250;
    private const double ReferenceArea = LegacyRefW * (double)LegacyRefH;

    public static double AreaRatio(int floor)
    {
        var (w, h) = GetDimensions(floor);
        return (w * (double)h) / ReferenceArea;
    }

    public static int Scale(int baseCount, int floor) =>
        Math.Max(1, (int)(baseCount * AreaRatio(floor) + 0.5));

    // ── Feature counts, tuned to F1 ref area; auto-scales with disk shrink.
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

    public static bool IsCastleFloor(int floor) => floor >= 100;
}
